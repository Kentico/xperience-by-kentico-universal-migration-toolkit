using System.Collections.Concurrent;
using System.Reflection;
using RazorLight;

namespace Kentico.Xperience.UMT;

public static class MdHelper
{
    private static ConcurrentDictionary<string, Task<ITemplatePage>> templates = new();
    public static RazorLightEngine Engine { get; }
    
    static MdHelper()
    {
        var builder = new RazorLightEngineBuilder()
            .EnableDebugMode()
            .UseEmbeddedResourcesProject(typeof(MdHelper).Assembly, "Kentico.Xperience.UMT.DocUtils.Templates")
            .UseMemoryCachingProvider();
    
        Engine = builder
            .Build();
    }
    
    public static async Task<string> RenderTemplate<TModel>(string templateKey, TModel model)
    {
        try
        {
            var templateTask = templates.GetOrAdd(templateKey, (tk) => Engine.CompileTemplateAsync(templateKey));
            var template = await templateTask;

            return await Engine.RenderTemplateAsync(template, model);
        }
        catch (TemplateNotFoundException tex)
        {
            Console.WriteLine($"Template key '{templateKey}' not found, existing:");
            foreach (string? existingTemplateKey in tex.KnownProjectTemplateKeys)
            {
                Console.WriteLine($"  {existingTemplateKey}");
            }

            throw;
        }
    }

    public static async Task RenderTemplateToFile<TModel>(string templateKey, TModel model, string filePath)
    {
        string renderResult = await MdHelper.RenderTemplate(
            templateKey,
            model
        );

        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("No directory name"));
        await File.WriteAllTextAsync(filePath, renderResult);
    }

    public static string? LocateRepositoryRoot()
    {
        if (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) is { } assemblyPath && Path.GetDirectoryName(assemblyPath) is { } assemblyDir)
        {
            string[] split = assemblyDir.Split(Path.DirectorySeparatorChar);

            for (int i = split.Length; i > 1; i--)
            {
                string current = string.Join(Path.DirectorySeparatorChar, split.Take(i).Concat(new[] { ".git" }));
                if (Directory.Exists(current))
                {
                    return string.Join(Path.DirectorySeparatorChar, split.Take(i));
                }
            }
        }

        return null;
    }
}
