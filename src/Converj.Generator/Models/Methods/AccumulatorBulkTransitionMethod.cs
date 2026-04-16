using System.Collections.Immutable;
using Converj.Generator.Models.Parameters;
using Converj.Generator.Models.Steps;
using Converj.Generator.TargetAnalysis;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Methods;

/// <summary>
/// Represents the parameterised bulk-transition method on the last regular trie step that
/// provides an <c>IEnumerable&lt;T&gt;</c> entry point directly into an
/// <see cref="AccumulatorFluentStep"/>, seeding the target collection field in one call.
/// Parallel to <see cref="AccumulatorTransitionMethod"/> (the parameterless entry), but accepts
/// a bulk collection to pre-seed the accumulator before the caller continues with
/// <c>AddX</c> or <c>WithXs</c> calls.
/// Does NOT implement <see cref="ISelfReturningAccumulatorMethod"/> — this method lives on
/// the preceding regular step and its return type is the accumulator step, not itself.
/// </summary>
internal class AccumulatorBulkTransitionMethod : IFluentMethod
{
    /// <summary>
    /// Initialises a new <see cref="AccumulatorBulkTransitionMethod"/>.
    /// </summary>
    /// <param name="name">
    /// The method name on the preceding regular step (e.g., <c>WithTags</c>).
    /// Resolved by the caller via <c>[FluentMethod]</c> attribute resolution.
    /// </param>
    /// <param name="returnStep">The <see cref="AccumulatorFluentStep"/> this method transitions to.</param>
    /// <param name="rootNamespace">The namespace of the fluent root type.</param>
    /// <param name="availableParameterFields">
    /// The fields on the preceding regular step, used by caller context during generation.
    /// </param>
    /// <param name="valueSources">Value-source map from the preceding step.</param>
    /// <param name="collectionParameter">
    /// The collection parameter analysis result identifying which accumulator field this
    /// transition seeds. Exposed as <see cref="CollectionParameter"/> for use by
    /// <c>AccumulatorStepDeclaration</c>.
    /// </param>
    /// <param name="compilation">
    /// The Roslyn compilation used to construct <c>IEnumerable&lt;ElementType&gt;</c> for the
    /// method parameter type.
    /// </param>
    public AccumulatorBulkTransitionMethod(
        string name,
        AccumulatorFluentStep returnStep,
        INamespaceSymbol rootNamespace,
        ImmutableArray<FluentMethodParameter> availableParameterFields,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueSources,
        CollectionParameterInfo collectionParameter,
        Compilation compilation)
    {
        Name = name;
        Return = returnStep;
        RootNamespace = rootNamespace;
        AvailableParameterFields = availableParameterFields;
        ValueSources = valueSources;
        CollectionParameter = collectionParameter;

        // The transition method accepts IEnumerable<ElementType> — the same bulk type as
        // AccumulatorBulkMethod on the accumulator step itself.
        var iEnumerableOpen = compilation.GetSpecialType(
            SpecialType.System_Collections_Generic_IEnumerable_T);
        var iEnumerableOfElement = iEnumerableOpen.Construct(collectionParameter.ElementType);

        MethodParameters =
        [
            new BulkFluentMethodParameter(
                collectionParameter.Parameter,
                iEnumerableOfElement,
                name)
        ];
    }

    /// <summary>Gets the name of this transition method as set by the caller.</summary>
    public string Name { get; }

    /// <summary>Gets the accumulator step that this method returns.</summary>
    public IFluentReturn Return { get; }

    /// <summary>
    /// Gets <see langword="null"/> — the transition represents the bulk-seeding of a
    /// collection parameter, not a single source parameter binding.
    /// </summary>
    public IParameterSymbol? SourceParameter => null;

    /// <summary>Gets the method parameters (a single <c>IEnumerable&lt;ElementType&gt;</c> parameter).</summary>
    public ImmutableArray<FluentMethodParameter> MethodParameters { get; }

    /// <summary>Gets the forwarded fields from the preceding regular step.</summary>
    public ImmutableArray<FluentMethodParameter> AvailableParameterFields { get; }

    /// <summary>Transition methods introduce no new generic type parameters.</summary>
    public ImmutableArray<FluentTypeParameter> TypeParameters => [];

    /// <summary>Gets the namespace of the fluent root type.</summary>
    public INamespaceSymbol RootNamespace { get; }

    /// <summary>Gets the value-source map from the preceding step.</summary>
    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueSources { get; }

    /// <summary>
    /// Gets the collection parameter that this transition seeds.
    /// Exposes <see cref="CollectionParameterInfo"/> so the syntax generator can identify
    /// which accumulator field to seed via <c>AddRange(items)</c> on entry.
    /// </summary>
    public CollectionParameterInfo CollectionParameter { get; }

    /// <inheritdoc/>
    public string? DocumentationSummary => null;

    /// <inheritdoc/>
    public Dictionary<string, string>? ParameterDocumentation => null;

    // ── Inner type ────────────────────────────────────────────────────────────

    /// <summary>
    /// A <see cref="FluentMethodParameter"/> whose <c>SourceType</c> is
    /// <c>IEnumerable&lt;ElementType&gt;</c>, matching the transition method's parameter type.
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
