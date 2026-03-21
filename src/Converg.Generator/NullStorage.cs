using Microsoft.CodeAnalysis;

namespace Converg.Generator;

internal record NullStorage(ITypeSymbol Type) : IFluentValueStorage
{
    public INamespaceSymbol ContainingNamespace => Type.ContainingNamespace;

    public Accessibility Accessibility => Accessibility.NotApplicable;

    public string IdentifierName => "default";

    public ITypeSymbol Type { get; } = Type;


    public bool DefinitionExists => false;
}
