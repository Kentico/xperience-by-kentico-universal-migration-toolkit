// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using Kentico.Xperience.UMT;
using Kentico.Xperience.UMT.Model;
using Kentico.Xperience.UMT.Services;
using Kentico.Xperience.UMT.Templates;
using Kentico.Xperience.UMT.Walkers;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.DependencyInjection;
using NJsonSchema;

const string projectNameKenticoXperienceUmt = "Kentico.Xperience.UMT";

string solutionFilePath;
string targetDirectory;

switch (args)
{
    case [var solutionPath, var targetDir]:
    {
        solutionFilePath = solutionPath.Trim('"');
        targetDirectory = targetDir.Trim('"');
        break;
    }
    default:
    {
        if (MdHelper.LocateRepositoryRoot() is { } repoRoot)
        {
            string[] solutionFiles = Directory.GetFiles(repoRoot, "*.sln");
            if (solutionFiles.FirstOrDefault() is { } sln && sln.Contains("Kentico.Xperience.UMT", StringComparison.OrdinalIgnoreCase))
            {
                solutionFilePath = sln;
                targetDirectory = Path.Combine(repoRoot, "Docs");
                break;
            }
        }
      
        throw new InvalidOperationException($"Incorrect number of parameters. Specify solution path as first argument and target documentation directory as second");
    }
}

MSBuildLocator.RegisterDefaults();
if (MSBuildLocator.IsRegistered)
{
    Console.WriteLine("Msbuild located");
}
else
{
    Console.WriteLine("Failed to locate MSBuild");
    return;
}

var serviceProvider = Initializer.GetUmtServiceProvider();
var importService = serviceProvider.GetRequiredService<IImportService>();

Console.WriteLine("Creating workspace");
using var workspace = MSBuildWorkspace.Create();
workspace.LoadMetadataForReferencedProjects = true;

Console.WriteLine("Opening solution...");

var solution = await workspace.OpenSolutionAsync(solutionFilePath);

var sampleApp = solution.Projects.FirstOrDefault(p => p.Name == "Kentico.Xperience.UMT.Example.AdminApp");
if (sampleApp != null)
{
    var compilation = await sampleApp.GetCompilationAsync();
    if (compilation == null)
    {
        Console.WriteLine($"Cannot get project compilation '{sampleApp.Name}' => tool ended with error");
        return;
    }

    Console.WriteLine($"Inspecting sample app - contains complete XbK admin assembly references");

    string markdownFilePath = Path.Join(targetDirectory, $"Enums\\FormComponents.md");

    string[] formComponentMetaNames = { "Kentico.Xperience.Admin.Base.Forms.FormComponent`2", "Kentico.Forms.Web.Mvc.FormComponent`2", "Kentico.Xperience.Admin.Base.Forms.FormComponent`3" };
    var fcSymbols = formComponentMetaNames.Select(compilation.GetTypeByMetadataName);

    var formComponents = new ConcurrentDictionary<IModuleSymbol, List<FormComponent>>(SymbolEqualityComparer.Default);
    foreach (var fcSymbol in fcSymbols)
    {
        if (fcSymbol != null)
        {
            var foundSymbols = await SymbolFinder.FindDerivedClassesAsync(fcSymbol, solution);
            foreach (var symbol in foundSymbols)
            {
                Console.WriteLine($"FormComponent: {symbol.ToDisplayString()}");
                var list = formComponents.GetOrAdd(symbol.ContainingModule, _ => new List<FormComponent>());
                if (symbol.GetFirstBaseType(bt => bt is { Name: "FormComponent", TypeArguments.Length: >= 2 and <= 3 }) is { TypeArguments: { Length: <= 3 and >= 2 } typeArguments })
                {
                    var propertiesType = typeArguments.FirstOrDefault();
                    var valueType = typeArguments.LastOrDefault();
                    var newFormComponent = new FormComponent(symbol, null, valueType, propertiesType);

                    foreach (var member in symbol.GetMembers())
                    {
                        if (member is IFieldSymbol { IsConst: true, Name: "IDENTIFIER" } fieldSymbol)
                        {
                            newFormComponent = newFormComponent with { Identifier = fieldSymbol.ConstantValue?.ToString() };
                        }
                    }

                    list.Add(newFormComponent);
                }
            }
        }
    }
    
    var fcModel = formComponents.Select(x => new FormComponentTemplateModel(x.Key, x.Value.ToArray())).ToArray();
    await MdHelper.RenderTemplateToFile("FormComponents", fcModel, markdownFilePath);
}

var schema = JsonSchema.FromType(typeof(List<UmtModel>));


var jsonSchemaModel = new UmtSchemaJsonViewModel(schema.ToJson());
string jsonSchemaMarkdownFilePath = Path.Join(targetDirectory, $"Model\\UMT.schema.json");
await MdHelper.RenderTemplateToFile("UMT.schema.json", jsonSchemaModel, jsonSchemaMarkdownFilePath);

var classesToRenderAsDoc = new List<Type>
{
    typeof(ImportStateObserver),
    typeof(IImportService),
};

var umtProject = solution.Projects.FirstOrDefault(p => p.Name == "Kentico.Xperience.UMT");
if (umtProject != null && await umtProject.GetCompilationAsync() is { } umtCompilation)
{
    foreach (var typeToRender in classesToRenderAsDoc)
    {
        var typeSymbol = umtCompilation.GetTypeByMetadataName(typeToRender.FullName!);
        var visitor = new ClassDocsVisitor();
        visitor.Visit(typeSymbol);

        string markdownFilePath = Path.Join(targetDirectory, $"Class\\{typeSymbol?.Name}.md");
        await MdHelper.RenderTemplateToFile("ClassDocs", new ClassDocsViewModel(importService, new ClassViewModel(
            visitor.Class,
            visitor.Properties,
            visitor.Methods,
            visitor.Delegates,
            visitor.Events
        )), markdownFilePath);
    }
}

foreach (var solutionProject in solution.Projects)
{
    Console.WriteLine($"Resolving project '{solutionProject.Name}'");
    
    if (solutionProject.Name == projectNameKenticoXperienceUmt)
    {
        var compilation = await solutionProject.GetCompilationAsync();
        if (compilation == null)
        {
            Console.WriteLine($"Cannot get project compilation '{solutionProject.Name}'");
            continue;
        }

        foreach (var solutionProjectDocument in solutionProject.Documents)
        {
            Console.WriteLine($"Resolving project document '{solutionProjectDocument.FilePath}'");
            if (solutionProjectDocument.SupportsSyntaxTree)
            {
                var syntaxNode = await solutionProjectDocument.GetSyntaxRootAsync();
                if (syntaxNode == null)
                {
                    Console.WriteLine($"Syntax root not available for '{solutionProjectDocument.FilePath}'");
                    continue;
                }

                var semanticModel = compilation.GetSemanticModel(syntaxNode.SyntaxTree, true);

                var umtModelWalker = new UmtModelWalker();
                umtModelWalker.Visit(syntaxNode);

                foreach (var umtModelClass in umtModelWalker.UmtModelClasses)
                {
                    if (umtModelClass is ClassDeclarationSyntax classDeclarationSyntax)
                    {
                        var modelClass = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                        Console.WriteLine($"  Model class discovered '{modelClass?.Name}'");

                        var modelVisitor = new UmtModelVisitor(importService);
                        modelVisitor.Visit(modelClass);

                        string markdownFilePath = Path.Join(targetDirectory, $"Model\\{modelClass?.Name}.md");

                        await MdHelper.RenderTemplateToFile("UmtModel", modelVisitor.ModelClasses, markdownFilePath);
                    }
                }
            }
        }
    }
}

await MdHelper.RenderTemplateToFile("SampleJson", new SampleJsonViewModel(importService), Path.Join(targetDirectory, $"Samples\\basic.json"));
