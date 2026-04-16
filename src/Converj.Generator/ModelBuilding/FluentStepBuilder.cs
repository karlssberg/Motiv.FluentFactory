using System.Collections.Immutable;
using Converj.Generator.Diagnostics;
using Converj.Generator.Extensions;
using Converj.Generator.Models.Methods;
using Converj.Generator.Models.Steps;
using Converj.Generator.TargetAnalysis;
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
        Trie<FluentMethodParameter, TargetMetadata>.Node node,
        Func<INamedTypeSymbol, Trie<FluentMethodParameter, TargetMetadata>.Node, 
            OrderedDictionary<IParameterSymbol, IFluentValueStorage>, ImmutableArray<IFluentMethod>> getFluentMethods,
        Action<INamedTypeSymbol, Trie<FluentMethodParameter, TargetMetadata>.Node,
            IFluentStep, OrderedDictionary<IParameterSymbol, IFluentValueStorage>>? postCreateStep = null)
    {
        var knownTargetParameters = new ParameterSequence(node.Key);
        var targetMetadata = node.EndValues.FirstOrDefault();
        var useExistingTypeAsStep = UseExistingTypeAsStep();

        var valueStorages = GetValueStorages();

        var fluentMethods = getFluentMethods(rootType, node, valueStorages);

        // For ExistingTypeFluentSteps with optional parameters, the step may start with
        // no methods but gain them via postCreateStep (AddOptionalMethodsToStep).
        // Only short-circuit for regular steps.
        var hasOptionalParams = node.IsEnd &&
                                node.EndValues.Any(v => v.OptionalParameters.Length > 0);
        if (fluentMethods.Length == 0 && !(useExistingTypeAsStep && hasOptionalParams))
            return null;

        // Hoist: if the yielded methods include AccumulatorMethod(s) whose Return is an
        // AccumulatorFluentStep, the accumulator step IS this node's step — skip RegularFluentStep
        // creation. The AccumulatorFluentStep was pre-built in BuildAccumulatorTransitions with
        // its ValueStorage / ForwardedTargetParameters / CandidateTargets already set.
        var hoistedAccumulator = fluentMethods
            .OfType<AccumulatorMethod>()
            .Select(m => m.Return as AccumulatorFluentStep)
            .FirstOrDefault(s => s is not null);
        if (hoistedAccumulator is not null)
        {
            hoistedAccumulator.FluentMethods = new List<IFluentMethod>(fluentMethods);
            return hoistedAccumulator;
        }

        var step = CreateStep(valueStorages);
        ReportUnresolvableStorage(step, valueStorages);
        postCreateStep?.Invoke(rootType, node, step, valueStorages);

        return step.FluentMethods.Count > 0
            ? step
            : null;

        bool UseExistingTypeAsStep()
        {
            if (targetMetadata is null) return false;

            var containingType = targetMetadata.Method.ContainingType;
            var doNotGenerateCreateMethod = targetMetadata.TerminalMethod == TerminalMethodKind.None;

            // FUTURE ENHANCEMENT: Create a dedicated analyzer to validate that target types are partial and instantiatable.
            // This would help avoid issues where constructors with similar build steps might be hidden.
            // Currently handled by the CanBeCustomStep() extension method.
            return containingType.CanBeCustomStep() && doNotGenerateCreateMethod;
        }

        IFluentStep CreateStep(OrderedDictionary<IParameterSymbol, IFluentValueStorage> storage)
        {
            return (useExistingTypeAsStep, targetMetadata) switch
            {
                (true, { } metadata) =>
                    new ExistingTypeFluentStep(metadata)
                    {
                        KnownTargetParameters = knownTargetParameters,
                        FluentMethods = new List<IFluentMethod>(fluentMethods),
                        ValueStorage = storage,
                        CandidateTargets =
                        [
                            ..node.Values
                                .SelectMany(value => value.CandidateTargets)
                                .Distinct<IMethodSymbol>(SymbolEqualityComparer.Default)
                        ]
                    },
                _ =>
                    regularFluentSteps.GetOrAdd(
                        knownTargetParameters,
                        () =>
                            new RegularFluentStep(
                                rootType,
                                node.Values
                                    .SelectMany(metadata => metadata.CandidateTargets)
                                    .Distinct(SymbolEqualityComparer.Default)
                                    .OfType<IMethodSymbol>())
                            {
                                KnownTargetParameters = knownTargetParameters,
                                FluentMethods = new List<IFluentMethod>(fluentMethods),
                                IsEndStep = node.IsEnd,
                                ValueStorage = storage
                            })
            };
        }

        OrderedDictionary<IParameterSymbol, IFluentValueStorage> GetValueStorages()
        {
            return (useExistingTypeAsStep, targetMetadata) switch
            {
                (true, not null and var metadata) => SupplementWithFluentStorage(metadata),
                _ => CreateRegularStepValueStorage(rootType, knownTargetParameters)
            };
        }

        OrderedDictionary<IParameterSymbol, IFluentValueStorage> SupplementWithFluentStorage(
            TargetMetadata metadata)
        {
            var valueStorage = metadata.ValueStorage;

            var hasNullStorage = valueStorage.Any(kvp => kvp.Value is NullStorage);
            if (!hasNullStorage) return valueStorage;

            var targetType = metadata.Method.ContainingType;
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
    /// Reports a diagnostic (CVJG0024) for each constructor parameter on a custom intermediate step
    /// that has no accessible property or field for value storage.
    /// </summary>
    /// <param name="step">The fluent step to check.</param>
    /// <param name="valueStorages">The value storage mappings for the step.</param>
    private void ReportUnresolvableStorage(
        IFluentStep step,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages)
    {
        if (step is not ExistingTypeFluentStep) return;

        var containingTypeDisplay = step.CandidateTargets.First().ContainingType.ToDisplayString();

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
    /// <param name="knownTargetParameters">The parameters known at this step in the builder chain.</param>
    /// <returns>An ordered dictionary mapping parameters to their field storage.</returns>
    public static OrderedDictionary<IParameterSymbol, IFluentValueStorage> CreateRegularStepValueStorage(
        INamedTypeSymbol rootType,
        ParameterSequence knownTargetParameters)
    {
        var parameterStoragePairs =
            from parameter in knownTargetParameters
            select new KeyValuePair<IParameterSymbol, IFluentValueStorage>(
                parameter,
                CreateStorageForParameter(parameter, knownTargetParameters, rootType.ContainingNamespace));

        return new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(parameterStoragePairs);
    }

    private static IFluentValueStorage CreateStorageForParameter(
        IParameterSymbol parameter,
        ParameterSequence knownTargetParameters,
        INamespaceSymbol containingNamespace)
    {
        var fluentParam = knownTargetParameters.GetFluentMethodParameter(parameter);
        if (fluentParam is TupleFluentMethodParameter tuple)
        {
            return TupleFieldStorage.FromTupleParameter(parameter, tuple.Elements, containingNamespace);
        }

        return FieldStorage.FromParameter(parameter, containingNamespace);
    }

    /// <summary>
    /// Recursively traverses fluent steps and their children to collect all descendant fluent steps
    /// in depth-first order, preserving the original traversal order for stable step index assignment.
    /// Excludes <see cref="AccumulatorMethod"/> from traversal to avoid infinite self-loops
    /// (accumulator <c>AddX</c> methods return the same step instance they live on).
    /// </summary>
    /// <param name="fluentSteps">The starting set of fluent steps to traverse.</param>
    /// <returns>All descendant fluent steps including the input steps.</returns>
    public static IEnumerable<IFluentStep> GetDescendentFluentSteps(IEnumerable<IFluentStep> fluentSteps)
    {
        foreach (var fluentStep in fluentSteps)
        {
            yield return fluentStep;

            // Exclude OptionalFluentMethod (self-returning setters) and any ISelfReturningAccumulatorMethod
            // (self-returning AddX/WithXs methods that would cause infinite recursion —
            // see Phase 22 Plan 04 STATE.md decision and Phase 23 RESEARCH.md Pitfall 8).
            var childSteps = fluentStep.FluentMethods
                .Where(m => m is not OptionalFluentMethod and not ISelfReturningAccumulatorMethod)
                .Select(m => m.Return)
                .OfType<IFluentStep>();

            foreach (var underlyingFluentStep in GetDescendentFluentSteps(childSteps))
                yield return underlyingFluentStep;
        }
    }
}
