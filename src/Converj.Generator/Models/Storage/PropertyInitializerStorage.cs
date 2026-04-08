using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Storage;

/// <summary>
/// Value storage for a property that will be set via object initializer syntax
/// in the creation method, rather than passed as a constructor argument.
/// </summary>
internal record PropertyInitializerStorage(string PropertyName, string IdentifierName, ITypeSymbol Type, INamespaceSymbol ContainingNamespace) : IFluentValueStorage
{
    /// <summary>
    /// The actual property name on the target type (used as the LHS of the initializer assignment).
    /// </summary>
    public string PropertyName { get; } = PropertyName;

    public string IdentifierName { get; } = IdentifierName;

    public ITypeSymbol Type { get; } = Type;

    public INamespaceSymbol ContainingNamespace { get; } = ContainingNamespace;

    public Accessibility Accessibility { get; set; } = Accessibility.Private;

    public bool DefinitionExists { get; set; }

    /// <summary>
    /// Whether the field should be declared as readonly. Defaults to true.
    /// Optional property fields are mutable (not readonly) to allow setter methods.
    /// </summary>
    public bool IsReadOnly { get; set; } = true;

    /// <summary>
    /// Creates a PropertyInitializerStorage for a target type property using the standard field naming convention.
    /// </summary>
    public static PropertyInitializerStorage FromProperty(IPropertySymbol property, INamespaceSymbol containingNamespace) =>
        new(property.Name, property.Name.ToParameterFieldName(), property.Type, containingNamespace);
}
