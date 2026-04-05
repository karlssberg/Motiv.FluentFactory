using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Converj.Generator;

[ExcludeFromCodeCoverage]
internal record FluentFactoryMetadata(INamedTypeSymbol RootTypeSymbol)
{
    public bool AttributePresent => AttributeData is not null;
    public INamedTypeSymbol RootTypeSymbol { get; } = RootTypeSymbol;
    public string RootTypeFullName { get; set; } = string.Empty;
    public TerminalMethodKind? TerminalMethod { get; set; }
    public string? TerminalVerb { get; set; }
    public string? MethodPrefix { get; set; }
    public INamedTypeSymbol? ReturnType { get; set; }
    public AttributeData? AttributeData { get; set; }
    public bool HasEntryMethod { get; set; }
    public string? EntryMethodName { get; set; }

    public static FluentFactoryMetadata Invalid => new(default(INamedTypeSymbol)!);

    public void Deconstruct(out bool attributePresent, out string rootTypeFullName, out TerminalMethodKind? terminalMethod)
    {
        attributePresent = AttributePresent;
        rootTypeFullName = RootTypeFullName;
        terminalMethod = TerminalMethod;
    }
}
