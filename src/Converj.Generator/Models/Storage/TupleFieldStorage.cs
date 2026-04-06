using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Storage;

/// <summary>
/// Storage for a tuple parameter where each tuple element is stored as a separate field.
/// The element storages hold the individual field declarations, while this type
/// retains knowledge of the original tuple type for re-packing at the terminal step.
/// </summary>
internal class TupleFieldStorage : IFluentValueStorage
{
    public TupleFieldStorage(
        ImmutableArray<FieldStorage> elementStorages,
        ITypeSymbol tupleType,
        INamespaceSymbol containingNamespace)
    {
        ElementStorages = elementStorages;
        Type = tupleType;
        ContainingNamespace = containingNamespace;
    }

    public ImmutableArray<FieldStorage> ElementStorages { get; }

    public INamespaceSymbol ContainingNamespace { get; }

    public Accessibility Accessibility { get; set; } = Accessibility.Private;

    /// <summary>
    /// Not meaningful for tuple storage — use <see cref="ElementStorages"/> for individual field names.
    /// </summary>
    public string IdentifierName => ElementStorages[0].IdentifierName;

    public ITypeSymbol Type { get; }

    public bool DefinitionExists { get; set; }

    /// <summary>
    /// Creates a TupleFieldStorage for a named tuple constructor parameter.
    /// </summary>
    public static TupleFieldStorage FromTupleParameter(
        IParameterSymbol parameter,
        ImmutableArray<(string Name, ITypeSymbol Type)> elements,
        INamespaceSymbol containingNamespace) =>
        new(
            [..elements.Select(e => new FieldStorage(e.Name.ToParameterFieldName(), e.Type, containingNamespace))],
            parameter.Type,
            containingNamespace);
}
