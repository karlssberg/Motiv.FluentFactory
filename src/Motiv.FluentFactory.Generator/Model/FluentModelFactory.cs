using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Motiv.FluentFactory.Generator.Analysis;
using Motiv.FluentFactory.Generator.Diagnostics;
using Motiv.FluentFactory.Generator.Generation;
using Motiv.FluentFactory.Generator.Generation.Shared;
using Motiv.FluentFactory.Generator.Model.Methods;
using Motiv.FluentFactory.Generator.Model.Steps;
using Motiv.FluentFactory.Generator.Model.Storage;
using static Motiv.FluentFactory.Generator.FluentFactoryGeneratorOptions;

namespace Motiv.FluentFactory.Generator.Model;

internal class FluentModelFactory(Compilation compilation)
{
    private readonly DiagnosticList _diagnostics = [];
    private readonly OrderedDictionary<ParameterSequence, RegularFluentStep> _regularFluentSteps = new();
    private readonly UnreachableConstructorAnalyzer _unreachableConstructorAnalyzer = new();

    private FluentMethodSelector _methodSelector = null!;

    public FluentFactoryCompilationUnit CreateFluentFactoryCompilationUnit(
        INamedTypeSymbol rootType,
        ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        _regularFluentSteps.Clear();
        _diagnostics.Clear();
        _unreachableConstructorAnalyzer.Clear();
        _unreachableConstructorAnalyzer.AddAllFluentConstructors(fluentConstructorContexts.Select(context => context.Constructor));
        _methodSelector = new FluentMethodSelector(compilation, _diagnostics, _unreachableConstructorAnalyzer);

        var usings = GetUsingStatements(fluentConstructorContexts);

        _diagnostics.AddRange(fluentConstructorContexts.GetDiagnostics());

        if (_diagnostics.Any())
        {
            return new FluentFactoryCompilationUnit(rootType) { Diagnostics = _diagnostics, Usings = usings };
        }

        var stepTrie = CreateFluentStepTrie(fluentConstructorContexts);

        var fluentRootMethods = ConvertNodeToFluentFluentMethods(rootType, stepTrie.Root, []);

        var childFluentSteps = fluentRootMethods
            .Select(m => m.Return)
            .OfType<IFluentStep>();

        var descendentFluentSteps = GetDescendentFluentSteps(childFluentSteps);
        var fluentBuilderSteps = descendentFluentSteps
            .DistinctBy(step => step.KnownConstructorParameters)
            .Select((step, index) =>
            {
                if (step is not RegularFluentStep regularFluentStep)
                    return step;

                regularFluentStep.Index = index;

                return step;
            })
            .ToImmutableArray();

        _diagnostics.AddRange(_unreachableConstructorAnalyzer.GetUnreachableConstructorsDiagnostics());
        var sampleConstructorContext = fluentConstructorContexts.First();

        return new FluentFactoryCompilationUnit(rootType)
        {
            FluentMethods = fluentRootMethods,
            FluentSteps = fluentBuilderSteps,
            Usings = usings,
            IsStatic = sampleConstructorContext.IsStatic,
            TypeKind = sampleConstructorContext.TypeKind,
            Accessibility = sampleConstructorContext.Accessibility,
            IsRecord = sampleConstructorContext.IsRecord,
            Diagnostics = _diagnostics
        };
    }

    private ImmutableArray<IFluentMethod> ConvertNodeToFluentFluentMethods(
        INamedTypeSymbol type,
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages)
    {
        ImmutableArray<IFluentMethod> fluentMethods =
        [
            .._methodSelector.ConvertNodeToFluentMethods(
                type, node, valueStorages,
                (rootType, child) => ConvertNodeToFluentStep(rootType, child)),
            ..ConvertNodeToCreationMethods(type, node, valueStorages)
        ];

        return fluentMethods;
    }

    private IFluentStep? ConvertNodeToFluentStep(
        INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node)
    {
        var knownConstructorParameters = new ParameterSequence(node.Key);
        var constructorMetadata = node.EndValues.FirstOrDefault();
        var useExistingTypeAsStep = UseExistingTypeAsStep();

        var valueStorages = GetValueStorages();

        var fluentMethods = ConvertNodeToFluentFluentMethods(rootType, node, valueStorages);
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
                    _regularFluentSteps.GetOrAdd(
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

    private static OrderedDictionary<IParameterSymbol, IFluentValueStorage> CreateRegularStepValueStorage(
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

    private IEnumerable<IFluentMethod> ConvertNodeToCreationMethods(INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueSources)
    {
        if (!node.IsEnd) yield break;

        var creationMethods =
            from value in node.Values
            where value.Constructor.Parameters.Length == node.Key.Length
            where !value.Options.HasFlag(NoCreateMethod)
            select new CreationMethod(
                rootType.ContainingNamespace,
                value,
                node.Key,
                valueSources,
                value.Context.CreateMethodName);

        foreach (var createMethod in creationMethods)
        {
            _unreachableConstructorAnalyzer.AddReachableMethod(createMethod);
            yield return createMethod;
        }
    }

    private Trie<FluentMethodParameter, ConstructorMetadata> CreateFluentStepTrie(
        ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        var trie = new Trie<FluentMethodParameter, ConstructorMetadata>();
        foreach (var constructorContext in fluentConstructorContexts)
        {
            var fluentParameters =
                constructorContext.Constructor.Parameters
                    .Select(parameter =>
                    {
                        var methodNames = compilation
                            .GetMultipleFluentMethodSymbols(parameter)
                            .Select(methodInfo => methodInfo.Method.Name)
                            .DefaultIfEmpty(parameter.GetFluentMethodName());

                        return new FluentMethodParameter(parameter, methodNames);
                    });

            trie.Insert(
                fluentParameters,
                new ConstructorMetadata(constructorContext));
        }

        return trie;
    }

    private static IEnumerable<IFluentStep> GetDescendentFluentSteps(IEnumerable<IFluentStep> fluentSteps)
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

    private static ImmutableArray<INamespaceSymbol> GetUsingStatements(
        ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        return
        [
            ..fluentConstructorContexts
                .SelectMany(ctx => ctx.Constructor.Parameters)
                .Select(parameter => parameter.Type.ContainingNamespace)
                .Concat(fluentConstructorContexts.Select(ctx => ctx.Constructor.ContainingType.ContainingNamespace))
                .Select(namespaceSymbol => (namespaceSymbol, displayString: namespaceSymbol.ToDisplayString()))
                .DistinctBy(ns => ns.displayString)
                .OrderBy(ns => ns.displayString)
                .Select(ns => ns.namespaceSymbol)
        ];
    }
}
