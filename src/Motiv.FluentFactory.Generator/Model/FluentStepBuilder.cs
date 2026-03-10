using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Motiv.FluentFactory.Generator.Generation;
using Motiv.FluentFactory.Generator.Model.Methods;
using Motiv.FluentFactory.Generator.Model.Steps;
using Motiv.FluentFactory.Generator.Model.Storage;
using static Motiv.FluentFactory.Generator.FluentFactoryGeneratorOptions;

namespace Motiv.FluentFactory.Generator.Model;

/// <summary>
/// Handles node-to-step conversion, value storage resolution, and descendant step traversal
/// for the fluent builder pipeline. Converts trie nodes into fluent steps that represent
/// intermediate builder types in the generated fluent API.
/// </summary>
internal class FluentStepBuilder(
    OrderedDictionary<ParameterSequence, RegularFluentStep> regularFluentSteps)
{
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
    /// <returns>A fluent step if the node has any fluent methods; null otherwise.</returns>
    public IFluentStep? ConvertNodeToFluentStep(
        INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node,
        Func<INamedTypeSymbol, Trie<FluentMethodParameter, ConstructorMetadata>.Node,
            OrderedDictionary<IParameterSymbol, IFluentValueStorage>, ImmutableArray<IFluentMethod>> getFluentMethods)
    {
        var knownConstructorParameters = new ParameterSequence(node.Key);
        var constructorMetadata = node.EndValues.FirstOrDefault();
        var useExistingTypeAsStep = UseExistingTypeAsStep();

        var valueStorages = GetValueStorages();

        var fluentMethods = getFluentMethods(rootType, node, valueStorages);
        return fluentMethods.Length > 0
            ? CreateStep(valueStorages)
            : null;

        bool UseExistingTypeAsStep()
        {
            if (constructorMetadata is null) return false;

            var containingType = constructorMetadata.Constructor.ContainingType;
            var doNotGenerateCreateMethod = constructorMetadata.Options.HasFlag(NoCreateMethod);

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
                        FluentMethods = fluentMethods,
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
                                FluentMethods = fluentMethods,
                                IsEndStep = node.IsEnd,
                                ValueStorage = storage
                            })
            };
        }

        OrderedDictionary<IParameterSymbol, IFluentValueStorage> GetValueStorages()
        {
            return (useExistingTypeAsStep, constructorMetadata) switch
            {
                (true, not null and var metadata) => metadata.ValueStorage,
                _ => CreateRegularStepValueStorage(rootType, knownConstructorParameters)
            };
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
            let fieldStorage = new FieldStorage(parameter.Name.ToParameterFieldName(), parameter.Type,
                rootType.ContainingNamespace)
            select new KeyValuePair<IParameterSymbol, IFluentValueStorage>(parameter, fieldStorage);

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
                .Select(m => m.Return)
                .OfType<IFluentStep>();

            foreach (var underlyingFluentStep in GetDescendentFluentSteps(childSteps))
                yield return underlyingFluentStep;
        }
    }
}
