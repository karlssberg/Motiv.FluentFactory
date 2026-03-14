using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Motiv.FluentFactory.Generator.ConstructorAnalysis;
using Motiv.FluentFactory.Generator.Diagnostics;
using Motiv.FluentFactory.Generator.ModelBuilding;
using static Motiv.FluentFactory.Generator.FluentFactoryGeneratorOptions;

namespace Motiv.FluentFactory.Generator;

internal class FluentModelFactory(Compilation compilation)
{
    private readonly DiagnosticList _diagnostics = [];
    private readonly OrderedDictionary<ParameterSequence, RegularFluentStep> _regularFluentSteps = new();
    private readonly UnreachableConstructorAnalyzer _unreachableConstructorAnalyzer = new();

    private FluentMethodSelector _methodSelector = null!;
    private FluentStepBuilder _stepBuilder = null!;

    public FluentFactoryCompilationUnit CreateFluentFactoryCompilationUnit(
        INamedTypeSymbol rootType,
        ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        _regularFluentSteps.Clear();
        _diagnostics.Clear();
        _unreachableConstructorAnalyzer.Clear();

        var (validContexts, unsupportedModifierDiagnostics) =
            FilterUnsupportedParameterModifierConstructors(fluentConstructorContexts);

        _diagnostics.AddRange(unsupportedModifierDiagnostics);

        if (validContexts.IsEmpty)
            return new FluentFactoryCompilationUnit(rootType) { Diagnostics = _diagnostics };

        fluentConstructorContexts = validContexts;

        _unreachableConstructorAnalyzer.AddAllFluentConstructors(fluentConstructorContexts.Select(context => context.Constructor));
        _methodSelector = new FluentMethodSelector(compilation, _diagnostics, _unreachableConstructorAnalyzer);
        _stepBuilder = new FluentStepBuilder(_regularFluentSteps);

        var usings = GetUsingStatements(fluentConstructorContexts);

        _diagnostics.AddRange(fluentConstructorContexts.GetDiagnostics());

        if (_diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return new FluentFactoryCompilationUnit(rootType) { Diagnostics = _diagnostics, Usings = usings };
        }

        var stepTrie = CreateFluentStepTrie(fluentConstructorContexts);

        var fluentRootMethods = ConvertNodeToFluentFluentMethods(rootType, stepTrie.Root, []);

        var childFluentSteps = fluentRootMethods
            .Select(m => m.Return)
            .OfType<IFluentStep>();

        var descendentFluentSteps = FluentStepBuilder.GetDescendentFluentSteps(childFluentSteps);
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
                (rootType, child) => _stepBuilder.ConvertNodeToFluentStep(rootType, child, ConvertNodeToFluentFluentMethods)),
            ..ConvertNodeToCreationMethods(type, node, valueStorages)
        ];

        return fluentMethods;
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

    private static (ImmutableArray<FluentConstructorContext> Valid, IEnumerable<Diagnostic> Diagnostics)
        FilterUnsupportedParameterModifierConstructors(
            ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        var diagnostics = new List<Diagnostic>();
        var validContexts = ImmutableArray.CreateBuilder<FluentConstructorContext>(fluentConstructorContexts.Length);

        foreach (var context in fluentConstructorContexts)
        {
            var unsupportedParameter = context.Constructor.Parameters
                .FirstOrDefault(p => p.RefKind is RefKind.Ref or RefKind.Out or RefKind.RefReadOnlyParameter);

            if (unsupportedParameter is null)
            {
                validContexts.Add(context);
                continue;
            }

            var modifierText = unsupportedParameter.RefKind switch
            {
                RefKind.Ref => "ref",
                RefKind.Out => "out",
                RefKind.RefReadOnlyParameter => "ref readonly",
                _ => unsupportedParameter.RefKind.ToString().ToLowerInvariant()
            };

            var location = context.Constructor.Locations.FirstOrDefault() ?? Location.None;
            diagnostics.Add(Diagnostic.Create(
                FluentDiagnostics.UnsupportedParameterModifier,
                location,
                context.Constructor.ToDisplayString(),
                unsupportedParameter.Name,
                modifierText));
        }

        return (validContexts.ToImmutable(), diagnostics);
    }
}
