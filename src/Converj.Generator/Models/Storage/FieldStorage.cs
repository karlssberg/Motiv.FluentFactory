using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Storage;

internal record FieldStorage(string IdentifierName, ITypeSymbol Type, INamespaceSymbol ContainingNamespace) : IFluentValueStorage
{
    public INamespaceSymbol ContainingNamespace { get; } = ContainingNamespace;

    public Accessibility Accessibility { get; set; } = Accessibility.Private;

    public string IdentifierName { get; } = IdentifierName;

    public ITypeSymbol Type { get; } = Type;

    public bool DefinitionExists { get; set; }

    /// <summary>
    /// Whether the field should be declared as readonly. Defaults to true.
    /// Optional parameter fields are mutable (not readonly) to allow setter methods.
    /// </summary>
    public bool IsReadOnly { get; set; } = true;

    /// <summary>
    /// Creates a FieldStorage for a constructor parameter using the standard naming convention.
    /// </summary>
    public static FieldStorage FromParameter(IParameterSymbol parameter, INamespaceSymbol containingNamespace) =>
        new(parameter.Name.ToParameterFieldName(), parameter.Type, containingNamespace);
}
