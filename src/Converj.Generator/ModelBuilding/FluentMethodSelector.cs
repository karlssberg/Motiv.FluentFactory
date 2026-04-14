using System.Collections.Immutable;
using Converj.Generator.Models.Methods;
using Microsoft.CodeAnalysis;
using Converj.Generator.Diagnostics;

namespace Converj.Generator.ModelBuilding;

/// <summary>
/// Handles method selection and priority ordering for fluent builder methods.
/// Responsible for choosing the best candidate fluent method when multiple options exist.
/// </summary>
internal class FluentMethodSelector(
    Compilation compilation,
    DiagnosticList diagnostics,
    UnreachableTargetAnalyzer unreachableTargetAnalyzer)
{
    private readonly FluentMethodBuilder _methodFactory = new(compilation, diagnostics);

    /// <summary>
    /// Converts a trie node's children into fluent methods by creating candidate methods
    /// for each child, selecting the best candidate per signature group, and reporting
    /// diagnostics for ignored methods.
    /// </summary>
    /// <param name="rootType">The root type being built by the fluent factory.</param>
    /// <param name="node">The current trie node whose children represent next parameters.</param>
    /// <param name="valueStorages">The accumulated value storages for parameters seen so far.</param>
    /// <param name="convertNodeToStep">
    /// A delegate that converts a child trie node into a fluent step.
    /// This enables the mutual recursion between method selection and step building
    /// without creating a direct dependency on the step builder.
    /// </param>
    /// <returns>The selected fluent methods for the node's children.</returns>
    public IEnumerable<IFluentMethod> ConvertNodeToFluentMethods(
        INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, TargetMetadata>.Node node,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages,
        Func<INamedTypeSymbol, Trie<FluentMethodParameter, TargetMetadata>.Node, IFluentStep?> convertNodeToStep)
    {
        var candidateFluentMethods =
            node.Children.Values
                .SelectMany(child =>
                    _methodFactory.CreateFluentMethods(
                        rootType,
                        node,
                        child.EncounteredKeyParts,
                        convertNodeToStep(rootType, child),
                        child.Values,
                        valueStorages))
                .ToImmutableArray();

        var selectedAndIgnoredMethods = ChooseCandidateFluentMethod(candidateFluentMethods);

        var allIgnoredMethods = selectedAndIgnoredMethods
            .SelectMany(pair => pair.IgnoredMethods)
            .ToImmutableHashSet();

        var ignoredMultiMethodWarningBuilder = new IgnoredMultiMethodWarningBuilder(
            allIgnoredMethods,
            unreachableTargetAnalyzer);

        foreach (var (selectedMethod, ignoredMethods) in selectedAndIgnoredMethods)
        {
            ReconcileTargetTypeReturnMethod(selectedMethod);
            unreachableTargetAnalyzer.AddReachableMethod(selectedMethod);
            diagnostics.AddRange(
                [
                    ..ignoredMultiMethodWarningBuilder
                        .Create(
                            selectedMethod,
                            [
                                ..ignoredMethods
                                .Distinct(FluentMethodSignatureEqualityComparer.Default)
                                .OfType<MultiMethod>()
                            ])
                ]);

            yield return selectedMethod;
        }
    }

    private static ImmutableArray<SelectedFluentMethod> ChooseCandidateFluentMethod(ImmutableArray<IFluentMethod> fluentMethods) =>
    [
        ..fluentMethods
            .Distinct()
            .GroupBy(m => m, FluentMethodSignatureEqualityComparer.Default)
            .Select(fluentMethodGroup =>
            {
                var orderedMethods = fluentMethodGroup
                    .OrderByDescending(m => m is RegularMethod ? 1 : 0)
                    .ThenBy(m => m.Name);

                var selectedMethod = orderedMethods.First();

                return new SelectedFluentMethod(
                    selectedMethod,
                    [
                        ..fluentMethodGroup
                            .Where(method => selectedMethod != method)
                    ]);
            })
    ];

    /// <summary>
    /// After method selection, updates the TargetTypeReturn's Method to match
    /// the selected method's source target. MergeTargetMetadata may have stored
    /// a different target due to merge ordering, so this reconciliation ensures the
    /// generated code constructs the correct type and reachability tracking is accurate.
    /// Walks through step chains to reach the leaf TargetTypeReturn when the immediate
    /// Return is a step, but only for RegularMethod selections: an upstream RegularMethod
    /// winning its signature group is an authoritative source-target signal that
    /// should propagate downstream. A MultiMethod selected at an upstream level is merely
    /// the sole remaining candidate for that signature and must not overwrite the
    /// reconciliation already performed at the terminal step, or a sibling MultiMethod from
    /// a different source target would clobber a correct downstream write.
    /// </summary>
    private void ReconcileTargetTypeReturnMethod(IFluentMethod selectedMethod)
    {
        if (selectedMethod.SourceParameter?.ContainingSymbol is not IMethodSymbol selectedTarget) return;

        var targetTypeReturn = ResolveTargetTypeReturn(selectedMethod);
        if (targetTypeReturn is null) return;

        if (!targetTypeReturn.CandidateTargets.Contains(selectedTarget, SymbolEqualityComparer.Default)) return;

        if (SymbolEqualityComparer.Default.Equals(targetTypeReturn.Method, selectedTarget)) return;

        unreachableTargetAnalyzer.RemoveReachableTarget(targetTypeReturn.Method);
        targetTypeReturn.Method = selectedTarget;
        unreachableTargetAnalyzer.AddReachableTarget(selectedTarget);
    }

    private static TargetTypeReturn? ResolveTargetTypeReturn(IFluentMethod selectedMethod)
    {
        if (selectedMethod.Return is TargetTypeReturn directReturn)
            return directReturn;

        // Only RegularMethods may walk through intermediate steps. See the reconciler doc
        // comment for why MultiMethods must not.
        if (selectedMethod is not RegularMethod) return null;

        var current = selectedMethod.Return;
        while (current is IFluentStep step)
        {
            // Exclude self-returning methods (AccumulatorMethod) to avoid an infinite loop:
            // AccumulatorMethod.Return == step, which would re-enter the same accumulator step.
            var nextMethod = step.FluentMethods.FirstOrDefault(
                m => m is not TerminalMethod and not OptionalFluentMethod and not AccumulatorMethod);
            if (nextMethod is null) return null;
            current = nextMethod.Return;
        }

        return current as TargetTypeReturn;
    }

    private record SelectedFluentMethod(IFluentMethod SelectedMethod, ImmutableArray<IFluentMethod> IgnoredMethods)
    {
        public IFluentMethod SelectedMethod { get; } = SelectedMethod;
        public ImmutableArray<IFluentMethod> IgnoredMethods { get; } = IgnoredMethods;
    }
}
