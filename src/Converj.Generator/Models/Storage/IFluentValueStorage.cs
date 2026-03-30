using Microsoft.CodeAnalysis;

namespace Converj.Generator;

internal interface IFluentValueStorage
{
    public INamespaceSymbol ContainingNamespace { get; }
    public Accessibility Accessibility { get; }
    public string IdentifierName { get; }

    public ITypeSymbol Type { get; }

    public bool DefinitionExists { get; }
}
