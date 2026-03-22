using Microsoft.CodeAnalysis;

namespace Converj.Generator;

internal record PropertyStorage(string IdentifierName, ITypeSymbol Type, INamespaceSymbol ContainingNamespace) : IFluentValueStorage
{
    public INamespaceSymbol ContainingNamespace { get; } = ContainingNamespace;
    public Accessibility Accessibility { get; set; } = Accessibility.Public;

    public string IdentifierName { get; } = IdentifierName;

    public ITypeSymbol Type { get; } = Type;

    public bool DefinitionExists { get; set; }
}
