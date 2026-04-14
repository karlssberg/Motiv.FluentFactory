using System.Collections.Immutable;
using Converj.Generator.Models.Parameters;
using Converj.Generator.Models.Steps;
using Converj.Generator.TargetAnalysis;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Methods;

/// <summary>
/// Represents an <c>AddX</c> method on an <see cref="AccumulatorFluentStep"/> that appends a single
/// element to one of the step's collection fields and returns the same accumulator step (GEN-01 self-return).
/// The method parameter type is the collection's element type — NOT the collection type itself (GEN-05).
/// </summary>
internal class AccumulatorMethod : IFluentMethod
{
    /// <summary>
    /// Initialises a new <see cref="AccumulatorMethod"/>.
    /// </summary>
    /// <param name="collectionParameter">The collection parameter analysis result that drives this method.</param>
    /// <param name="returnStep">
    /// The <see cref="AccumulatorFluentStep"/> that this method returns — the same step instance for self-chaining (GEN-01).
    /// </param>
    /// <param name="rootNamespace">The namespace of the fluent root type.</param>
    /// <param name="availableParameterFields">
    /// The forwarded fields the accumulator step holds (non-collection threaded parameters + all collection accumulator fields).
    /// </param>
    /// <param name="valueSources">Forwarded value-source map from the preceding step.</param>
    public AccumulatorMethod(
        CollectionParameterInfo collectionParameter,
        AccumulatorFluentStep returnStep,
        INamespaceSymbol rootNamespace,
        ImmutableArray<FluentMethodParameter> availableParameterFields,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueSources)
    {
        CollectionParameter = collectionParameter;
        Return = returnStep;
        RootNamespace = rootNamespace;
        AvailableParameterFields = availableParameterFields;
        ValueSources = valueSources;

        // GEN-05: the method parameter type is the element type, not the collection type (Pitfall 3).
        // The parameter name is derived from the collection parameter's own name (e.g. "items" → "item").
        MethodParameters =
        [
            new ElementTypeFluentMethodParameter(
                collectionParameter.Parameter,
                collectionParameter.ElementType,
                collectionParameter.MethodName)
        ];
    }

    /// <summary>Gets the method name — the resolved <c>Add{Singularized}</c> or explicit attribute value.</summary>
    public string Name => CollectionParameter.MethodName;

    /// <summary>
    /// Gets the original collection parameter symbol.
    /// <c>SourceParameter</c> points to the collection parameter rather than the element, because
    /// the plan models the method as "appending to the collection identified by this parameter".
    /// </summary>
    public IParameterSymbol? SourceParameter => CollectionParameter.Parameter;

    /// <summary>Gets the accumulator step that this method returns (self-return, GEN-01).</summary>
    public IFluentReturn Return { get; }

    /// <summary>
    /// Gets a single-element array containing the element-type method parameter (GEN-05).
    /// The parameter type is <see cref="CollectionParameterInfo.ElementType"/>, not the collection type.
    /// </summary>
    public ImmutableArray<FluentMethodParameter> MethodParameters { get; }

    /// <summary>Gets the forwarded fields on the accumulator step.</summary>
    public ImmutableArray<FluentMethodParameter> AvailableParameterFields { get; }

    /// <summary>Accumulator methods introduce no new generic type parameters.</summary>
    public ImmutableArray<FluentTypeParameter> TypeParameters => [];

    /// <summary>Gets the namespace of the fluent root type.</summary>
    public INamespaceSymbol RootNamespace { get; }

    /// <summary>Gets the forwarded value-source map from the preceding step.</summary>
    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueSources { get; }

    /// <summary>
    /// Gets the collection parameter analysis result.
    /// Exposed so Plan 22-03's syntax generator can read <see cref="CollectionParameterInfo.ElementType"/>,
    /// <see cref="CollectionParameterInfo.DeclaredCollectionType"/>, and the accumulator field name.
    /// </summary>
    public CollectionParameterInfo CollectionParameter { get; }

    /// <inheritdoc/>
    public string? DocumentationSummary => null;

    /// <inheritdoc/>
    public Dictionary<string, string>? ParameterDocumentation => null;

    // ── Inner type ────────────────────────────────────────────────────────────

    /// <summary>
    /// A <see cref="FluentMethodParameter"/> whose <c>SourceType</c> is the collection element type
    /// rather than the collection type.  The <c>ParameterSymbol</c> is preserved for identity;
    /// the type is overridden so code generation emits the element-type parameter (GEN-05, Pitfall 3).
    /// </summary>
    private sealed class ElementTypeFluentMethodParameter(
        IParameterSymbol collectionParameterSymbol,
        ITypeSymbol elementType,
        string methodName)
        : FluentMethodParameter(
            parameterSymbol: collectionParameterSymbol,
            sourceProperty: null,
            sourceName: collectionParameterSymbol.Name,
            sourceType: elementType,
            names: [methodName])
    {
    }
}
