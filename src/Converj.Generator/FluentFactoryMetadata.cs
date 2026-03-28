using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Converj.Generator;

[ExcludeFromCodeCoverage]
internal record FluentFactoryMetadata(INamedTypeSymbol RootTypeSymbol)
{
    public bool AttributePresent => AttributeData is not null;
    public INamedTypeSymbol RootTypeSymbol { get; } = RootTypeSymbol;
    public string RootTypeFullName { get; set; } = string.Empty;
    public CreateMethodMode? CreateMethod { get; set; }
    public string? CreateVerb { get; set; }
    public string? MethodPrefix { get; set; }
    public INamedTypeSymbol? ReturnType { get; set; }
    public AttributeData? AttributeData { get; set; }
    public BuilderMode? BuilderMode { get; set; }
    public string? TypeFirstVerb { get; set; }

    public static FluentFactoryMetadata Invalid => new(default(INamedTypeSymbol)!);

    public void Deconstruct(out bool attributePresent, out string rootTypeFullName, out CreateMethodMode? createMethod)
    {
        attributePresent = AttributePresent;
        rootTypeFullName = RootTypeFullName;
        createMethod = CreateMethod;
    }
}
