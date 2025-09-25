using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator.Model.Storage;

internal interface IFluentValueStorage
{
    public INamespaceSymbol ContainingNamespace { get; }
    public Accessibility Accessibility { get; }
    public string IdentifierName { get; }

    public ITypeSymbol Type { get; }

    public bool DefinitionExists { get; }
}
