using System.Collections.Immutable;
using System.Diagnostics;
using Converj.Generator.Extensions;
using Converj.Generator.Models.Parameters;
using Converj.Generator.TargetAnalysis;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Steps;

/// <summary>
/// Represents the accumulator step in a fluent builder chain — the dedicated struct that holds
/// one or more collection fields and exposes <c>AddX</c> self-returning methods (GEN-01).
/// Distinct from <see cref="RegularFluentStep"/>; inheriting from it is explicitly prohibited
/// (RESEARCH.md Pitfall 5) because the non-readonly guard in <c>FluentStepDeclaration</c> would fire.
/// </summary>
[DebuggerDisplay("{ToString()}")]
internal class AccumulatorFluentStep(INamedTypeSymbol rootType) : IFluentStep
{
    /// <summary>Gets the root type symbol from which namespace and identifier are derived.</summary>
    public INamedTypeSymbol RootType { get; } = rootType;

    /// <summary>
    /// Gets the step name in the form <c>Accumulator_{Index}__{RootIdentifier}</c>.
    /// The <c>Accumulator_</c> prefix is distinct from <c>Step_</c> to guarantee no collision
    /// with regular steps (RESEARCH.md Pitfall 7).
    /// </summary>
    public string Name => GetStepName(RootType);

    /// <summary>Gets the fully qualified name of this step, including the containing namespace.</summary>
    public string FullName => $"{Namespace.ToDisplayString()}.{Name}";

    /// <summary>
    /// Gets or sets the zero-based index assigned by <c>FluentModelBuilder</c>.
    /// Mirrors <see cref="RegularFluentStep.Index"/>.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the fluent methods on this step.
    /// Contains <c>AccumulatorMethod</c> instances (one per collection parameter),
    /// zero or more <c>AccumulatorBulkMethod</c> instances (one per collection parameter
    /// that also carries <c>[FluentMethod]</c>), and the terminal <see cref="TerminalMethod"/>
    /// (RESEARCH.md Open Question 3; Phase 23 Plan 02 extends with bulk-append methods).
    /// </summary>
    public IList<IFluentMethod> FluentMethods { get; set; } = [];

    /// <summary>Gets or sets the declared accessibility of this step, defaulting to the root type's accessibility.</summary>
    public Accessibility Accessibility { get; set; } = rootType.DeclaredAccessibility;

    /// <summary>
    /// Gets the type kind for this step.  Accumulator steps are always structs (GEN-06).
    /// </summary>
    public TypeKind TypeKind { get; set; } = TypeKind.Struct;

    /// <summary>Gets a value indicating whether this step is a record. Always <see langword="false"/> for accumulator steps.</summary>
    public bool IsRecord => false;

    /// <summary>
    /// Gets or sets parameters threaded from the factory root type via <c>[FluentParameter]</c> bindings.
    /// Propagated by <c>ParameterBindingResolver</c>.
    /// </summary>
    public ImmutableArray<FluentParameterBinding> ThreadedParameters { get; set; } = [];

    /// <summary>
    /// Gets or sets the extension receiver parameter threaded through all steps in the chain.
    /// Propagated for extension-method targets.
    /// </summary>
    public IParameterSymbol? ReceiverParameter { get; set; }

    /// <summary>
    /// Gets or sets the non-collection constructor parameter fields forwarded from the preceding
    /// regular trie step.  Analogue of <see cref="RegularFluentStep.KnownTargetParameters"/>
    /// (RESEARCH.md Pitfall 8).
    /// </summary>
    public ParameterSequence ForwardedTargetParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the forwarded field storage from the preceding regular step.
    /// Analogue of <see cref="RegularFluentStep.ValueStorage"/>.
    /// </summary>
    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection parameters analysed during Step 2 target analysis.
    /// Consumed by Plan 22-03 to emit accumulator field declarations.
    /// </summary>
    public ImmutableArray<CollectionParameterInfo> CollectionParameters { get; set; } = [];

    // ── IFluentReturn requirements ────────────────────────────────────────────

    /// <summary>Gets the containing namespace of this step (derived from the root type).</summary>
    public INamespaceSymbol Namespace => RootType.ContainingNamespace;

    /// <summary>
    /// Gets the non-collection parameters known at this step.
    /// Returns <see cref="ForwardedTargetParameters"/> so that downstream syntax generation
    /// can reason about forwarded constructor arguments.
    /// </summary>
    public ParameterSequence KnownTargetParameters => ForwardedTargetParameters;

    /// <summary>
    /// Gets the open generic type parameters derived from threaded and forwarded parameters.
    /// Accumulator steps do not introduce new generic parameters, so this delegates to forwarded state.
    /// </summary>
    public ImmutableArray<IParameterSymbol> GenericTargetParameters =>
    [
        ..ThreadedParameters
            .Select(b => b.TargetParameter)
            .Where(parameter => parameter.Type.IsOpenGenericType()),
        ..ForwardedTargetParameters
            .Where(parameter => parameter.Type.IsOpenGenericType())
    ];

    /// <summary>
    /// Gets or sets the full set of target methods (constructors or static factory methods)
    /// associated with this accumulator step.  Populated by Plan 22-04 during pipeline wiring.
    /// </summary>
    public ImmutableArray<IMethodSymbol> CandidateTargets { get; set; } = [];

    /// <summary>
    /// Gets or sets the subset of <see cref="CandidateTargets"/> determined to be unreachable.
    /// </summary>
    public ImmutableArray<IMethodSymbol> UnavailableTargets { get; set; } = [];

    // ── IFluentReturn display string methods ─────────────────────────────────

    /// <summary>
    /// Returns the fully-qualified identifier display string for use in generated code.
    /// Accumulator steps have no generic type parameters, so this is simply
    /// <c>global::{Namespace}.{Name}</c>.
    /// </summary>
    public string IdentifierDisplayString()
    {
        var globalPrefix = Namespace.IsGlobalNamespace
            ? "global::"
            : $"global::{Namespace.ToDisplayString()}.";
        return $"{globalPrefix}{Name}";
    }

    /// <summary>
    /// Returns the fully-qualified identifier display string with generic type argument mappings applied.
    /// Accumulator steps carry no generic parameters of their own, so the map is unused and the
    /// result is identical to <see cref="IdentifierDisplayString()"/>.
    /// </summary>
    public string IdentifierDisplayString(IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap) =>
        IdentifierDisplayString();

    // ── Debugging ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ToString() =>
        $"Accumulator_{Index}__ with {CollectionParameters.Length} collection param(s)";

    // ── Private helpers ───────────────────────────────────────────────────────

    private string GetStepName(INamedTypeSymbol root) =>
        $"Accumulator_{Index}__{root.ToIdentifier()}";
}
