using System.Collections.Immutable;
using Converj.Generator.Models.Parameters;
using Converj.Generator.Models.Steps;
using Converj.Generator.TargetAnalysis;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Methods;

/// <summary>
/// Represents a <c>WithXs</c> method on an <see cref="AccumulatorFluentStep"/> that bulk-appends
/// an <c>IEnumerable&lt;T&gt;</c> to one of the step's collection fields and returns the same
/// accumulator step (self-return — parallel to <see cref="AccumulatorMethod"/>).
/// Implements <see cref="ISelfReturningAccumulatorMethod"/> so traversal code excludes it from
/// descendant-step walks in the same way it excludes <see cref="AccumulatorMethod"/>.
/// </summary>
internal class AccumulatorBulkMethod : IFluentMethod, ISelfReturningAccumulatorMethod
{
    /// <summary>
    /// Initialises a new <see cref="AccumulatorBulkMethod"/>.
    /// </summary>
    /// <param name="collectionParameter">The collection parameter analysis result driving this method.</param>
    /// <param name="returnStep">
    /// The <see cref="AccumulatorFluentStep"/> that this method returns — the same step instance for
    /// self-chaining (GEN-01 parallel).
    /// </param>
    /// <param name="rootNamespace">The namespace of the fluent root type.</param>
    /// <param name="availableParameterFields">
    /// The forwarded fields the accumulator step holds (non-collection threaded parameters + all
    /// collection accumulator fields).
    /// </param>
    /// <param name="valueSources">Forwarded value-source map from the preceding step.</param>
    /// <param name="bulkMethodName">
    /// The resolved method name for this bulk method (e.g., <c>WithTags</c>). Resolved by the
    /// caller via the same <c>[FluentMethod]</c> attribute resolution logic used by the regular trie.
    /// NOT the singularized <c>AddX</c> name — that belongs to <see cref="AccumulatorMethod"/>.
    /// </param>
    /// <param name="compilation">
    /// The Roslyn compilation used to construct <c>IEnumerable&lt;ElementType&gt;</c>
    /// (via <c>GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(...)</c>).
    /// </param>
    public AccumulatorBulkMethod(
        CollectionParameterInfo collectionParameter,
        AccumulatorFluentStep returnStep,
        INamespaceSymbol rootNamespace,
        ImmutableArray<FluentMethodParameter> availableParameterFields,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueSources,
        string bulkMethodName,
        Compilation compilation)
    {
        CollectionParameter = collectionParameter;
        Return = returnStep;
        RootNamespace = rootNamespace;
        AvailableParameterFields = availableParameterFields;
        ValueSources = valueSources;
        Name = bulkMethodName;

        // The bulk method accepts IEnumerable<ElementType> — never the declared collection type
        // and never the element type alone. This follows the 23-CONTEXT.md locked decision for
        // append-range semantics.
        var iEnumerableOpen = compilation.GetSpecialType(
            SpecialType.System_Collections_Generic_IEnumerable_T);
        var iEnumerableOfElement = iEnumerableOpen.Construct(collectionParameter.ElementType);

        MethodParameters =
        [
            new BulkFluentMethodParameter(
                collectionParameter.Parameter,
                iEnumerableOfElement,
                bulkMethodName)
        ];
    }

    /// <summary>Gets the method name — the caller-provided bulk method name (e.g., <c>WithTags</c>).</summary>
    public string Name { get; }

    /// <summary>
    /// Gets the original collection parameter symbol.
    /// Points to the collection parameter rather than the element, because the method models
    /// "appending to the collection identified by this parameter".
    /// </summary>
    public IParameterSymbol? SourceParameter => CollectionParameter.Parameter;

    /// <summary>Gets the accumulator step that this method returns (self-return, GEN-01 parallel).</summary>
    public IFluentReturn Return { get; }

    /// <summary>
    /// Gets a single-element array containing the <c>IEnumerable&lt;ElementType&gt;</c> bulk parameter.
    /// </summary>
    public ImmutableArray<FluentMethodParameter> MethodParameters { get; }

    /// <summary>Gets the forwarded fields on the accumulator step.</summary>
    public ImmutableArray<FluentMethodParameter> AvailableParameterFields { get; }

    /// <summary>Accumulator bulk methods introduce no new generic type parameters.</summary>
    public ImmutableArray<FluentTypeParameter> TypeParameters => [];

    /// <summary>Gets the namespace of the fluent root type.</summary>
    public INamespaceSymbol RootNamespace { get; }

    /// <summary>Gets the forwarded value-source map from the preceding step.</summary>
    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueSources { get; }

    /// <summary>
    /// Gets the collection parameter analysis result.
    /// Exposed so <c>AccumulatorStepDeclaration</c> can read <see cref="CollectionParameterInfo.ElementType"/>
    /// and the accumulator field name for emitting the <c>AddRange</c> body.
    /// </summary>
    public CollectionParameterInfo CollectionParameter { get; }

    /// <inheritdoc/>
    public string? DocumentationSummary => null;

    /// <inheritdoc/>
    public Dictionary<string, string>? ParameterDocumentation => null;

    // ── Inner type ────────────────────────────────────────────────────────────

    /// <summary>
    /// A <see cref="FluentMethodParameter"/> whose <c>SourceType</c> is
    /// <c>IEnumerable&lt;ElementType&gt;</c>. The <c>ParameterSymbol</c> is the original
    /// collection parameter (preserved for identity); the type is overridden to the constructed
    /// <c>IEnumerable&lt;T&gt;</c> so code generation emits the correct bulk-append parameter.
    /// </summary>
    private sealed class BulkFluentMethodParameter(
        IParameterSymbol collectionParameterSymbol,
        ITypeSymbol iEnumerableOfElement,
        string methodName)
        : FluentMethodParameter(
            parameterSymbol: collectionParameterSymbol,
            sourceProperty: null,
            sourceName: collectionParameterSymbol.Name,
            sourceType: iEnumerableOfElement,
            names: [methodName])
    {
    }
}
