using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Motiv.FluentFactory.Generator.Diagnostics;

namespace Motiv.FluentFactory.Generator.ModelBuilding;

/// <summary>
/// Handles method selection and priority ordering for fluent builder methods.
/// Responsible for choosing the best candidate fluent method when multiple options exist.
/// </summary>
internal class FluentMethodSelector(
    Compilation compilation,
    DiagnosticList diagnostics,
    UnreachableConstructorAnalyzer unreachableConstructorAnalyzer)
{
    private readonly FluentMethodFactory _methodFactory = new(compilation, diagnostics);

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
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages,
        Func<INamedTypeSymbol, Trie<FluentMethodParameter, ConstructorMetadata>.Node, IFluentStep?> convertNodeToStep)
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

        var ignoredMultiMethodWarningFactory = new IgnoredMultiMethodWarningFactory(allIgnoredMethods);

        foreach (var (selectedMethod, ignoredMethods) in selectedAndIgnoredMethods)
        {
            unreachableConstructorAnalyzer.AddReachableMethod(selectedMethod);
            diagnostics.AddRange(
                [
                    ..ignoredMultiMethodWarningFactory
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
                    .Select(m => (FluentMethod: m, Priority: m.SourceParameter?.GetFluentMethodPriority() ?? 0))
                    .OrderByDescending(m => m.Priority)
                    .ThenByDescending(m => m.FluentMethod is RegularMethod ? 1 : 0)
                    .ThenBy(m => m.FluentMethod.Name);

                var selectedMethod = orderedMethods.First().FluentMethod;

                return new SelectedFluentMethod(
                    selectedMethod,
                    [
                        ..fluentMethodGroup
                            .Where(method => selectedMethod != method)
                    ]);
            })
    ];

    private record SelectedFluentMethod(IFluentMethod SelectedMethod, ImmutableArray<IFluentMethod> IgnoredMethods)
    {
        public IFluentMethod SelectedMethod { get; } = SelectedMethod;
        public ImmutableArray<IFluentMethod> IgnoredMethods { get; } = IgnoredMethods;
    }
}
