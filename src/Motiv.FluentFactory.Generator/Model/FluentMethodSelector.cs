using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Motiv.FluentFactory.Generator.Diagnostics;
using Motiv.FluentFactory.Generator.Generation.Shared;
using Motiv.FluentFactory.Generator.Model.Methods;
using Motiv.FluentFactory.Generator.Model.Steps;
using Motiv.FluentFactory.Generator.Model.Storage;
using static Motiv.FluentFactory.Generator.FluentFactoryGeneratorOptions;

namespace Motiv.FluentFactory.Generator.Model;

/// <summary>
/// Handles method selection, validation, and merging for fluent builder methods.
/// Responsible for choosing the best candidate fluent method when multiple options
/// exist, creating regular and multi-methods, and validating compatibility.
/// </summary>
internal class FluentMethodSelector(
    Compilation compilation,
    DiagnosticList diagnostics,
    UnreachableConstructorAnalyzer unreachableConstructorAnalyzer)
{
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
                    CreateFluentMethods(
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

    private IEnumerable<IFluentMethod> CreateFluentMethods(
        INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node,
        ICollection<FluentMethodParameter> fluentParameterInstances,
        IFluentStep? nextStep,
        IList<ConstructorMetadata> constructorMetadataList,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages)
    {
        var constructorMetadata = MergeConstructorMetadata(node, constructorMetadataList);
        IFluentReturn methodReturn = nextStep switch
        {
            null => new TargetTypeReturn(
                constructorMetadata.Constructor,
                [..constructorMetadata.CandidateConstructors],
                new ParameterSequence(node.Key.Select(p => p.ParameterSymbol))),
            _ => nextStep
        };

        foreach (var parameter in fluentParameterInstances)
        {
            var multipleFluentMethodInfo = compilation
                .GetMultipleFluentMethodSymbols(parameter.ParameterSymbol)
                .ToList();

            ValidateMultipleFluentMethodCompatibility(parameter, multipleFluentMethodInfo);

            var normalizedFluentMethodSymbols = multipleFluentMethodInfo
                .Where(methodInfo => methodInfo.Diagnostics.Count == 0)
                .Select(methodInfo => NormalizedConverterMethod(methodInfo.Method, parameter.ParameterSymbol.Type))
                .ToImmutableArray();

            foreach (var normalizedFluentMethodSymbol in normalizedFluentMethodSymbols)
                yield return new MultiMethod(
                    parameter.ParameterSymbol,
                    methodReturn,
                    rootType.ContainingNamespace,
                    normalizedFluentMethodSymbol,
                    node.Key,
                    valueStorages,
                    normalizedFluentMethodSymbols);

            var hasMultipleFluentMethodsAttribute = parameter.ParameterSymbol
                .GetAttribute(TypeName.MultipleFluentMethodsAttribute) is not null;

            var hasFluentMethodAttribute = parameter.ParameterSymbol
                .GetAttribute(TypeName.FluentMethodAttribute) is not null;

            if (!hasFluentMethodAttribute && hasMultipleFluentMethodsAttribute) continue;

            var fluentParameter = fluentParameterInstances.First();
            foreach (var name in fluentParameter.Names)
                yield return new RegularMethod(
                    name,
                    fluentParameter.ParameterSymbol,
                    methodReturn,
                    rootType.ContainingNamespace,
                    node.Key,
                    valueStorages);
        }
    }

    private void ValidateMultipleFluentMethodCompatibility(FluentMethodParameter parameter,
        List<(IMethodSymbol Method, ICollection<Diagnostic> Diagnostics)> multipleFluentMethodInfo)
    {
        if (multipleFluentMethodInfo.Any()
            && multipleFluentMethodInfo.All(info => info.Diagnostics.Count > 0))
            diagnostics.AddRange(
            [
                Diagnostic.Create(
                    FluentDiagnostics.AllFluentMethodTemplatesIncompatible,
                    parameter.ParameterSymbol
                        .GetAttribute(TypeName.MultipleFluentMethodsAttribute)?
                        .GetLocationAtIndex(0),
                    parameter.ParameterSymbol.ToFullDisplayString()),
            ]);
        else
            diagnostics.AddRange(multipleFluentMethodInfo
                .SelectMany(info => info.Diagnostics));
    }

    private static ConstructorMetadata MergeConstructorMetadata(
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node, IList<ConstructorMetadata> constructorMetadataList)
    {
        return constructorMetadataList.Skip(1).Aggregate(constructorMetadataList.First().Clone(), (merged, metadata) =>
        {
            var mergeableConstructors = metadata.CandidateConstructors
                .Except<IMethodSymbol>(merged.CandidateConstructors, SymbolEqualityComparer.Default);

            merged.CandidateConstructors.AddRange(mergeableConstructors);
            merged.Options |= metadata.Options;
            if (metadata.Constructor.Parameters.Length - 1 != node.Key.Length)
                return merged;

            merged.Constructor = metadata.Constructor;

            return merged;
        });
    }

    private static IMethodSymbol NormalizedConverterMethod(IMethodSymbol converter, ITypeSymbol targetType)
    {
        var mapping = TypeMapper.MapGenericArguments(converter.ReturnType, targetType);

        return converter.NormalizeMethodTypeParameters(mapping);
    }

    private record SelectedFluentMethod(IFluentMethod SelectedMethod, ImmutableArray<IFluentMethod> IgnoredMethods)
    {
        public IFluentMethod SelectedMethod { get; } = SelectedMethod;
        public ImmutableArray<IFluentMethod> IgnoredMethods { get; } = IgnoredMethods;
    }
}
