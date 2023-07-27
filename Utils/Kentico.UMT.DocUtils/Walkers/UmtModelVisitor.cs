﻿using System.Xml.Linq;
using System.Xml.XPath;
using Kentico.Xperience.UMT.Attributes;
using Kentico.Xperience.UMT.Helpers;
using Kentico.Xperience.UMT.Services;
using Kentico.Xperience.UMT.Templates;
using Microsoft.CodeAnalysis;

namespace Kentico.Xperience.UMT.Walkers;

public class UmtModelVisitor: SymbolVisitor
{
    private readonly IImportService importService;

    public UmtModelVisitor(IImportService importService) => this.importService = importService;

    public List<ModelClass> ModelClasses { get; private set; } = new();

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        var docsXml = symbol.GetDocumentationXml();
        string summary = docsXml?.XPathSelectElement("//summary")?.Value.Trim() ?? "";
        Console.WriteLine($"    Class: {symbol.Name} docs: {summary}");

        var sampleList = new List<SerializedSampleInfo>();
        var samples = docsXml?.XPathSelectElements("//sample") ?? new List<XElement>();
        foreach (var sample in samples)
        {
            var sampleInfo = SampleProvider.GetSerializedSample(sample.Value, importService);
            if (sampleInfo != null)
            {
                sampleList.Add(sampleInfo);    
            }
        }
        
        ModelClasses.Add(new ModelClass(symbol, symbol.Name, summary, new List<ModelProperty>(), sampleList));
        
        foreach (var member in symbol.GetMembers())
        {
            member.Accept(this);
        }

        base.VisitNamedType(symbol);
    }

    public override void VisitField(IFieldSymbol symbol)
    {
        if (symbol.DeclaredAccessibility == Accessibility.Public)
        {
            var modelClass = ModelClasses.FirstOrDefault(x => symbol.ContainingSymbol?.Equals(x.Symbol, SymbolEqualityComparer.IncludeNullability) == true)
                             ?? throw new InvalidOperationException("DISCRIMINATOR exists but class not found => this is possibly error in tool");
        
            if (symbol.Name == "DISCRIMINATOR")
            {
                ModelClasses[ModelClasses.IndexOf(modelClass)] = modelClass with
                {
                    Discriminator = symbol.ConstantValue as string
                };
            }    
        }

        base.VisitField(symbol);
    }

    public override void VisitProperty(IPropertySymbol symbol)
    {
        string summary = symbol.GetDocumentationXml()?.XPathSelectElement("//summary")?.Value.Trim() ?? "";

        var docRefs = symbol.GetDocumentationXml()?
            .XPathSelectElements("//docref")
            .Select(x=> new DocRef(x.Attribute("uri")?.Value, x.Attribute("header")?.Value, x.Value))
            .ToArray() ?? Array.Empty<DocRef>();
        
        if (docRefs.Length > 0)
        {
            if (!string.IsNullOrWhiteSpace(summary))
            {
                summary += " ";
            }
            summary += $"{string.Join(", ", docRefs.Select(x => x.ToMarkdownLink()))}";    
        }
        
        
        bool isUniqueId = false;
        ModelPropertyReference? reference = null;
        var validationInfo = new ValidationInfo(false);
        foreach (var attributeData in symbol.GetAttributes())
        {
            Console.WriteLine($"      Property '{symbol.Name}' decorated with: '{attributeData.AttributeClass?.Name}'");

            if (attributeData.AttributeClass is { Name: "UniqueIdPropertyAttribute" })
            {
                isUniqueId = true;
            }

            if (attributeData.AttributeClass is { Name: "ReferencePropertyAttribute" })
            {
                reference = ReadReferencePropertyInfo(attributeData);
            }

            var validationAttribute = attributeData.AttributeClass?.GetFirstBaseType(bt => bt.ToDisplayString() == typeof(System.ComponentModel.DataAnnotations.ValidationAttribute).FullName);
            if (validationAttribute != null && attributeData.AttributeClass?.ToDisplayString() == typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).FullName)
            {
                validationInfo = new ValidationInfo(IsRequired: true);
            }
        }

        var modelClass = ModelClasses.FirstOrDefault(x => symbol.ContainingSymbol?.Equals(x.Symbol, SymbolEqualityComparer.Default) == true);
        modelClass?.Properties.Add(new ModelProperty(symbol.Name, summary, new SymbolFormatter(symbol.Type).ToNiceDisplayName(), reference, isUniqueId, validationInfo));

        if (symbol.Type is INamedTypeSymbol { TypeArguments.Length: > 0 } namedTypeSymbol)
        {
            foreach (var typeArgument in namedTypeSymbol.TypeArguments.Where(TestIfTypeBelongToUmt))
            {
                typeArgument.Accept(this);
            }
        }
        
        if (TestIfTypeBelongToUmt(symbol.Type))
        {
            symbol.Type.Accept(this);
        }
        
        base.VisitProperty(symbol);
    }

    private static ModelPropertyReference ReadReferencePropertyInfo(AttributeData attributeData)
    {
        INamedTypeSymbol? infoType = null;
        string? referencedPropertyName = null;
        for (int i = 0; i < attributeData.ConstructorArguments.Length; i++)
        {
            var value = attributeData.ConstructorArguments[i];
            var param = attributeData.AttributeConstructor?.Parameters[i];

            switch (param?.Name.ToLowerInvariant())
            {
                case "infotype" when value.Value is INamedTypeSymbol it:
                {
                    infoType = it;
                    break;
                }
                case "referencedpropertyname" when value.Value is string rpn:
                {
                    referencedPropertyName = rpn;
                    break;
                }
            }

            Console.WriteLine($"        {param?.Name}:{value.Value}");
        }

        bool isRequired = false;
        string? searchField = null;
        string? valueField = null;
        foreach ((string? key, var value) in attributeData.NamedArguments)
        {
            switch (key)
            {
                case nameof(ReferencePropertyAttribute.IsRequired) when value.Value is bool r:
                {
                    isRequired = r;
                    break;
                }
                case nameof(ReferencePropertyAttribute.SearchedField) when value.Value is string s:
                {
                    searchField = s;
                    break;
                }
                case nameof(ReferencePropertyAttribute.ValueField) when value.Value is string s:
                {
                    valueField = s;
                    break;
                }
            }
            Console.WriteLine($"        {key}:{value.Value}");
        }

        return new ModelPropertyReference(
            infoType ?? throw new InvalidOperationException($"Missing reference property info type"),
            referencedPropertyName ?? throw new InvalidOperationException($"Missing reference property name"),
            isRequired,
            searchField,
            valueField
        );
    }

    private bool TestIfTypeBelongToUmt(ITypeSymbol symbol) => symbol.ContainingNamespace.ToDisplayString().Contains("Kentico.Xperience.UMT");
}
