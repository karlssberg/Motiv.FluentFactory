using System.Collections.Immutable;
using Converj.Generator.ConstructorAnalysis;
using Converj.Generator.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.ModelBuilding;

/// <summary>
/// Handles node-to-step conversion, value storage resolution, and descendant step traversal
/// for the fluent builder pipeline. Converts trie nodes into fluent steps that represent
/// intermediate builder types in the generated fluent API.
/// </summary>
internal class FluentStepBuilder(
    OrderedDictionary<ParameterSequence, RegularFluentStep> regularFluentSteps,
    DiagnosticList diagnostics)
{
    private readonly Dictionary<INamedTypeSymbol, Dictionary<string, IFluentValueStorage>> _fluentStorageCache = new(SymbolEqualityComparer.Default);
    /// <summary>
    /// Converts a trie node into a fluent step by resolving value storages, obtaining fluent methods
    /// for the node via the orchestrator callback, and creating either an existing-type step or a
    /// regular generated step.
    /// </summary>
    /// <param name="rootType">The root type being built by the fluent factory.</param>
    /// <param name="node">The trie node to convert into a fluent step.</param>
    /// <param name="getFluentMethods">
    /// A callback to the orchestrator's method that returns all fluent methods for a given node.
    /// This enables the mutual recursion between step building and method selection.
    /// </param>
    /// <param name="postCreateStep">
    /// An optional callback invoked after step creation, allowing additional methods (e.g., optional parameter setters)
    /// to be appended to the step.
    /// </param>
    /// <returns>A fluent step if the node has any fluent methods; null otherwise.</returns>
    public IFluentStep? ConvertNodeToFluentStep(
        INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node,
        Func<INamedTypeSymbol, Trie<FluentMethodParameter, ConstructorMetadata>.Node,
            OrderedDictionary<IParameterSymbol, IFluentValueStorage>, ImmutableArray<IFluentMethod>> getFluentMethods,
        Action<INamedTypeSymbol, Trie<FluentMethodParameter, ConstructorMetadata>.Node,
            IFluentStep, OrderedDictionary<IParameterSymbol, IFluentValueStorage>>? postCreateStep = null)
    {
        var knownConstructorParameters = new ParameterSequence(node.Key);
        var constructorMetadata = node.EndValues.FirstOrDefault();
        var useExistingTypeAsStep = UseExistingTypeAsStep();

        var valueStorages = GetValueStorages();

        var fluentMethods = getFluentMethods(rootType, node, valueStorages);
        if (fluentMethods.Length == 0) return null;

        var step = CreateStep(valueStorages);
        ReportUnresolvableStorage(step, valueStorages);
        postCreateStep?.Invoke(rootType, node, step, valueStorages);
        return step;

        bool UseExistingTypeAsStep()
        {
            if (constructorMetadata is null) return false;

            var containingType = constructorMetadata.Constructor.ContainingType;
            var doNotGenerateCreateMethod = constructorMetadata.CreateMethod == CreateMethodMode.None;

            // FUTURE ENHANCEMENT: Create a dedicated analyzer to validate that target types are partial and instantiatable.
            // This would help avoid issues where constructors with similar build steps might be hidden.
            // Currently handled by the CanBeCustomStep() extension method.
            return containingType.CanBeCustomStep() && doNotGenerateCreateMethod;
        }

        IFluentStep CreateStep(OrderedDictionary<IParameterSymbol, IFluentValueStorage> storage)
        {
            return (useExistingTypeAsStep, constructorMetadata) switch
            {
                (true, { } metadata) =>
                    new ExistingTypeFluentStep(metadata)
                    {
                        KnownConstructorParameters = knownConstructorParameters,
                        FluentMethods = new List<IFluentMethod>(fluentMethods),
                        ValueStorage = storage,
                        CandidateConstructors =
                        [
                            ..node.Values
                                .SelectMany(value => value.CandidateConstructors)
                                .Distinct<IMethodSymbol>(SymbolEqualityComparer.Default)
                        ]
                    },
                _ =>
                    regularFluentSteps.GetOrAdd(
                        knownConstructorParameters,
                        () =>
                            new RegularFluentStep(
                                rootType,
                                node.Values
                                    .SelectMany(metadata => metadata.CandidateConstructors)
                                    .Distinct(SymbolEqualityComparer.Default)
                                    .OfType<IMethodSymbol>())
                            {
                                KnownConstructorParameters = knownConstructorParameters,
                                FluentMethods = new List<IFluentMethod>(fluentMethods),
                                IsEndStep = node.IsEnd,
                                ValueStorage = storage
                            })
            };
        }

        OrderedDictionary<IParameterSymbol, IFluentValueStorage> GetValueStorages()
        {
            return (useExistingTypeAsStep, constructorMetadata) switch
            {
                (true, not null and var metadata) => SupplementWithFluentStorage(metadata),
                _ => CreateRegularStepValueStorage(rootType, knownConstructorParameters)
            };
        }

        OrderedDictionary<IParameterSymbol, IFluentValueStorage> SupplementWithFluentStorage(
            ConstructorMetadata metadata)
        {
            var valueStorage = metadata.ValueStorage;

            var hasNullStorage = valueStorage.Any(kvp => kvp.Value is NullStorage);
            if (!hasNullStorage) return valueStorage;

            var targetType = metadata.Constructor.ContainingType;
            if (!_fluentStorageCache.TryGetValue(targetType, out var fluentStorageMap))
            {
                fluentStorageMap = FluentStorageAnalyzer.Analyze(targetType, diagnostics);
                _fluentStorageCache[targetType] = fluentStorageMap;
            }
            if (fluentStorageMap.Count == 0) return valueStorage;

            foreach (var kvp in valueStorage)
            {
                if (kvp.Value is not NullStorage) continue;

                if (fluentStorageMap.TryGetValue(kvp.Key.Name, out var storage))
                {
                    valueStorage[kvp.Key] = storage;
                }
            }

            return valueStorage;
        }
    }

    /// <summary>
    /// Reports a diagnostic (MFFG0024) for each constructor parameter on a custom intermediate step
    /// that has no accessible property or field for value storage.
    /// </summary>
    /// <param name="step">The fluent step to check.</param>
    /// <param name="valueStorages">The value storage mappings for the step.</param>
    private void ReportUnresolvableStorage(
        IFluentStep step,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages)
    {
        if (step is not ExistingTypeFluentStep) return;

        var containingTypeDisplay = step.CandidateConstructors.First().ContainingType.ToDisplayString();

        foreach (var storage in valueStorages)
        {
            if (storage.Value is not NullStorage) continue;

            var parameter = storage.Key;
            var location = parameter.Locations.FirstOrDefault() ?? Location.None;

            diagnostics.Add(Diagnostic.Create(
                FluentDiagnostics.UnresolvableCustomStepStorage,
                location,
                containingTypeDisplay,
                parameter.Name));
        }
    }

    /// <summary>
    /// Creates value storage mappings for a regular (generated) fluent step,
    /// mapping each known constructor parameter to a field storage.
    /// </summary>
    /// <param name="rootType">The root type whose containing namespace is used for field storage.</param>
    /// <param name="knownConstructorParameters">The parameters known at this step in the builder chain.</param>
    /// <returns>An ordered dictionary mapping parameters to their field storage.</returns>
    public static OrderedDictionary<IParameterSymbol, IFluentValueStorage> CreateRegularStepValueStorage(
        INamedTypeSymbol rootType,
        ParameterSequence knownConstructorParameters)
    {
        var parameterStoragePairs =
            from parameter in knownConstructorParameters
            select new KeyValuePair<IParameterSymbol, IFluentValueStorage>(
                parameter, FieldStorage.FromParameter(parameter, rootType.ContainingNamespace));

        return new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(parameterStoragePairs);
    }

    /// <summary>
    /// Recursively traverses fluent steps and their children to collect all descendant fluent steps
    /// in depth-first order.
    /// </summary>
    /// <param name="fluentSteps">The starting set of fluent steps to traverse.</param>
    /// <returns>All descendant fluent steps including the input steps.</returns>
    public static IEnumerable<IFluentStep> GetDescendentFluentSteps(IEnumerable<IFluentStep> fluentSteps)
    {
        foreach (var fluentStep in fluentSteps)
        {
            yield return fluentStep;

            var childSteps = fluentStep.FluentMethods
                .Where(m => m is not OptionalFluentMethod)
                .Select(m => m.Return)
                .OfType<IFluentStep>();

            foreach (var underlyingFluentStep in GetDescendentFluentSteps(childSteps))
                yield return underlyingFluentStep;
        }
    }
}
