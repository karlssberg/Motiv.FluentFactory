using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Converj.Generator.Diagnostics;
using Converj.Generator.Domain;
using Converj.Generator.Extensions;
using Converj.Generator.ModelBuilding;
using Converj.Generator.Models.Methods;
using Converj.Generator.Models.Steps;
using Converj.Generator.TargetAnalysis;

namespace Converj.Generator;

internal class FluentModelBuilder(Compilation compilation)
{
    private readonly DiagnosticList _diagnostics = [];
    private readonly DiagnosticList _skippedTargetDiagnostics = [];
    private readonly OrderedDictionary<ParameterSequence, RegularFluentStep> _regularFluentSteps = new();
    private readonly UnreachableTargetAnalyzer _unreachableTargetAnalyzer = new();

    private FluentMethodSelector _methodSelector = null!;
    private FluentStepBuilder _stepBuilder = null!;
    private ParameterBindingResolver _bindingResolver = null!;
    private INamedTypeSymbol _rootType = null!;

    public FluentRootCompilationUnit CreateFluentRootCompilationUnit(
        INamedTypeSymbol rootType,
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        _regularFluentSteps.Clear();
        _diagnostics.Clear();
        _skippedTargetDiagnostics.Clear();
        _unreachableTargetAnalyzer.Clear();

        // Filter out instance method targets and report diagnostics
        foreach (var instanceMethod in fluentTargetContexts.Where(c => c.IsInstanceMethodTarget))
        {
            var location = instanceMethod.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                           ?? Location.None;
            _diagnostics.Add(Diagnostic.Create(
                FluentDiagnostics.InstanceMethodTarget,
                location,
                instanceMethod.Method.Name));
        }

        _diagnostics.AddRange(TargetContextFilter.ValidateThisAttributeUsage(fluentTargetContexts));

        fluentTargetContexts = [
            ..fluentTargetContexts
                .Where(c => !c.IsInstanceMethodTarget)
        ];

        _diagnostics.AddRange(TargetContextFilter.ValidateRootForExtensionTargets(rootType, fluentTargetContexts));

        var (validContexts, unsupportedModifierDiagnostics) =
            TargetContextFilter.FilterUnsupportedParameterModifierTargets(fluentTargetContexts);

        _diagnostics.AddRange(unsupportedModifierDiagnostics);

        var (afterCollisionCheck, collisionDiagnostics) =
            TargetContextFilter.FilterCollectionAccumulatorCollisions(validContexts);
        // Store collision diagnostics separately — they describe skipped targets and must not
        // trigger the error-bail-out guard that would prevent sibling targets from being generated.
        _skippedTargetDiagnostics.AddRange(collisionDiagnostics);
        validContexts = afterCollisionCheck;

        var (accessibleContexts, inaccessibleTargetDiagnostics) =
            TargetContextFilter.FilterInaccessibleTargets(validContexts);

        _diagnostics.AddRange(inaccessibleTargetDiagnostics);
        validContexts = accessibleContexts;

        validContexts = TargetContextFilter.FilterErrorTypeTargets(validContexts);

        if (validContexts.IsEmpty)
        {
            _diagnostics.AddRange(_skippedTargetDiagnostics);
            return new FluentRootCompilationUnit(rootType) { Diagnostics = _diagnostics };
        }

        fluentTargetContexts = validContexts;

        _unreachableTargetAnalyzer.AddAllTargets(fluentTargetContexts.Select(context => context.Method));
        _methodSelector = new FluentMethodSelector(compilation, _diagnostics, _unreachableTargetAnalyzer);

        _stepBuilder = new FluentStepBuilder(_regularFluentSteps, _diagnostics);
        _rootType = rootType;
        _bindingResolver = new ParameterBindingResolver(compilation, rootType, _diagnostics);

        var fluentParameterMembers = FluentParameterAnalyzer.Analyze(rootType, _diagnostics);
        _bindingResolver.ResolveBindings(fluentParameterMembers, fluentTargetContexts);

        var usings = GetUsingStatements(fluentTargetContexts);

        _diagnostics.AddRange(fluentTargetContexts.GetDiagnostics());

        // Collect property analysis diagnostics
        foreach (var context in fluentTargetContexts)
            _diagnostics.AddRange(context.PropertyDiagnostics);

        // Collect collection parameter analysis diagnostics (CVJG0050, CVJG0051)
        foreach (var context in fluentTargetContexts)
            _diagnostics.AddRange(context.CollectionDiagnostics);

        if (_diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            _diagnostics.AddRange(_skippedTargetDiagnostics);
            return new FluentRootCompilationUnit(rootType) { Diagnostics = _diagnostics, Usings = usings };
        }

        // Split constructors: type-first ones are excluded from the parameter-first trie
        ImmutableArray<FluentTargetContext> parameterFirstContexts = [
            ..fluentTargetContexts
                .Where(c => !c.HasEntryMethod)
        ];

        var stepTrie = CreateFluentStepTrie(parameterFirstContexts);

        var fluentRootMethods = ConvertNodeToFluentFluentMethods(rootType, stepTrie.Root, [], _stepBuilder);

        var childFluentSteps = fluentRootMethods
            .Select(m => m.Return)
            .OfType<IFluentStep>();

        var descendentFluentSteps = FluentStepBuilder.GetDescendentFluentSteps(childFluentSteps);
        var fluentBuilderSteps = descendentFluentSteps
            .DistinctBy(step => step is RegularFluentStep rfs
                ? (object)rfs.KnownTargetParameters
                : step)   // AccumulatorFluentStep instances are unique per target; use reference equality
            .Select((step, index) =>
            {
                switch (step)
                {
                    case RegularFluentStep regularFluentStep:
                        regularFluentStep.Index = index;
                        break;
                    case AccumulatorFluentStep accumulatorStep:
                        accumulatorStep.Index = index;
                        break;
                }
                return step;
            })
            .ToImmutableArray();

        if (!_bindingResolver.ThreadedParameters.IsEmpty)
        {
            _bindingResolver.PropagateThreadedParametersToSteps(fluentBuilderSteps);
        }

        // Propagate extension receiver to all steps in the chain
        var receiverParameter = parameterFirstContexts
            .Select(c => c.ReceiverParameter)
            .FirstOrDefault(r => r is not null);
        if (receiverParameter is not null)
        {
            ParameterBindingResolver.PropagateReceiverToSteps(fluentBuilderSteps, receiverParameter);
        }

        // Handle multi-param all-optional constructors via post-processing
        var (gatewayMethods, gatewaySteps) = CreateAllOptionalStepsAndGatewayMethods(
            rootType, stepTrie.Root, fluentBuilderSteps.Length);

        if (!_bindingResolver.ThreadedParameters.IsEmpty && !gatewaySteps.IsEmpty)
        {
            _bindingResolver.PropagateThreadedParametersToSteps(gatewaySteps);
        }

        if (receiverParameter is not null && !gatewaySteps.IsEmpty)
        {
            ParameterBindingResolver.PropagateReceiverToSteps(gatewaySteps, receiverParameter);
        }

        fluentRootMethods = [..fluentRootMethods, ..gatewayMethods];
        fluentBuilderSteps = [..fluentBuilderSteps, ..gatewaySteps];

        // Type-first chain generation: group by target type, build per-type trie
        var typeFirstContexts = fluentTargetContexts
            .Where(c => c.HasEntryMethod)
            .ToImmutableArray();
        if (!typeFirstContexts.IsEmpty)
        {
            var (typeFirstEntryMethods, typeFirstSteps) = CreateTypeFirstChains(
                rootType, typeFirstContexts, fluentBuilderSteps.Length);

            if (!_bindingResolver.ThreadedParameters.IsEmpty && !typeFirstSteps.IsEmpty)
            {
                _bindingResolver.PropagateThreadedParametersToSteps(typeFirstSteps);
            }

            fluentRootMethods = [..fluentRootMethods, ..typeFirstEntryMethods];
            fluentBuilderSteps = [..fluentBuilderSteps, ..typeFirstSteps];
        }

        // Post-process: insert required property steps between end steps and terminal methods
        var (propertySteps, updatedRootMethods) = PropertyStepEnricher.InsertRequiredPropertySteps(rootType, fluentRootMethods, fluentBuilderSteps);
        if (propertySteps.Length > 0)
        {
            if (!_bindingResolver.ThreadedParameters.IsEmpty)
                _bindingResolver.PropagateThreadedParametersToSteps(propertySteps);

            if (receiverParameter is not null)
                ParameterBindingResolver.PropagateReceiverToSteps(propertySteps, receiverParameter);

            fluentBuilderSteps = [..fluentBuilderSteps, ..propertySteps];
        }
        fluentRootMethods = updatedRootMethods;

        MarkUnavailableTargets(fluentRootMethods, fluentBuilderSteps);

        _diagnostics.AddRange(_unreachableTargetAnalyzer.GetUnreachableTargetsDiagnostics());
        _diagnostics.AddRange(_skippedTargetDiagnostics);
        var sampleTargetContext = fluentTargetContexts.First();

        return new FluentRootCompilationUnit(rootType)
        {
            FluentMethods = fluentRootMethods,
            FluentSteps = fluentBuilderSteps,
            Usings = usings,
            IsStatic = sampleTargetContext.IsStatic && _bindingResolver.ThreadedParameters.IsEmpty,
            TypeKind = sampleTargetContext.TypeKind,
            Accessibility = sampleTargetContext.Accessibility,
            IsRecord = sampleTargetContext.IsRecord,
            ThreadedParameters = _bindingResolver.ThreadedParameters,
            Diagnostics = _diagnostics
        };
    }

    private ImmutableArray<IFluentMethod> ConvertNodeToFluentFluentMethods(
        INamedTypeSymbol type,
        Trie<FluentMethodParameter, TargetMetadata>.Node node,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages,
        FluentStepBuilder stepBuilder)
    {
        ImmutableArray<IFluentMethod> fluentMethods =
        [
            .._methodSelector.ConvertNodeToFluentMethods(
                type, node, valueStorages,
                (rootType, child) => stepBuilder.ConvertNodeToFluentStep(
                    rootType, child,
                    (t, n, vs) => ConvertNodeToFluentFluentMethods(t, n, vs, stepBuilder),
                    AddOptionalMethodsToStep)),
            ..ConvertNodeToTerminalMethods(type, node, valueStorages)
        ];

        return fluentMethods;
    }

    private void AddOptionalMethodsToStep(
        INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, TargetMetadata>.Node node,
        IFluentStep step,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages)
    {
        if (!node.IsEnd) return;

        // Exclude optional params already in the trie path (handled as regular step methods)
        var knownParamFieldNames = new HashSet<string>(
            step.KnownTargetParameters.Select(p => p.Name.ToParameterFieldName()));

        var optionalParameters = node.EndValues
            .SelectMany(v => v.OptionalParameters)
            .DistinctBy(p => p.Name.ToParameterFieldName())
            .Where(p => !knownParamFieldNames.Contains(p.Name.ToParameterFieldName()))
            .ToList();

        var methodPrefix = node.EndValues
            .Select(v => v.Context.MethodPrefix)
            .FirstOrDefault() ?? "With";

        if (optionalParameters.Count > 0)
        {
            // Add optional parameter fields to the step's value storage
            foreach (var parameter in optionalParameters)
            {
                if (!valueStorages.ContainsKey(parameter))
                {
                    var fieldStorage = FieldStorage.FromParameter(parameter, rootType.ContainingNamespace)
                        with { IsReadOnly = false };
                    valueStorages.Add(parameter, fieldStorage);
                }
            }

            // Add optional methods to the step
            foreach (var parameter in optionalParameters)
            {
                var methodName = parameter.GetFluentMethodName(methodPrefix);
                var optionalMethod = new OptionalFluentMethod(
                    methodName,
                    parameter,
                    step,
                    rootType.ContainingNamespace,
                    [..node.Key],
                    valueStorages);
                step.FluentMethods.Add(optionalMethod);
            }
        }

    }

    private IEnumerable<IFluentMethod> ConvertNodeToTerminalMethods(INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, TargetMetadata>.Node node,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueSources)
    {
        if (!node.IsEnd) yield break;

        var methodPrefix = node.EndValues
            .Select(v => v.Context.MethodPrefix)
            .FirstOrDefault() ?? "With";

        foreach (var value in node.EndValues)
        {
            var preSatisfiedNames = _bindingResolver.IsSelfReferencing(value.Method)
                ? new HashSet<string>()
                : _bindingResolver.GetPreSatisfiedParameterNames(value.Method);
            var receiverCount = value.ReceiverParameter is not null ? 1 : 0;

            // RequiredParameterCount includes collection params in the parameter list, but they've been
            // excluded from the trie. Adjust the effective required count by subtracting collection params.
            var collectionParamCount = value.CollectionParameters.Length;
            var adjustedRequiredCount = value.RequiredParameterCount - collectionParamCount;

            if (node.Key.Length < adjustedRequiredCount - preSatisfiedNames.Count - receiverCount) continue;
            if (node.Key.Length > value.Method.Parameters.Length - preSatisfiedNames.Count - receiverCount - collectionParamCount) continue;
            if (value.TerminalMethod == TerminalMethodKind.None) continue;

            // Branch: if the target has collection parameters, produce an AccumulatorTransitionMethod
            // (parameterless) plus optional AccumulatorBulkTransitionMethod(s) (parameterised, one per
            // collection param that carries [FluentMethod]) instead of a direct TerminalMethod.
            // The terminal lives on the AccumulatorFluentStep.
            if (value.CollectionParameters.Length > 0)
            {
                foreach (var transitionMethod in BuildAccumulatorTransitions(rootType, node, value, valueSources, methodPrefix))
                    yield return transitionMethod;
                continue;
            }

            var keyParamFieldNames = new HashSet<string>(
                node.Key.Select(k => k.SourceName.ToParameterFieldName()));
            var optionalParamFields = value.OptionalParameters
                .Where(p => !keyParamFieldNames.Contains(p.Name.ToParameterFieldName()))
                .Select(p => FluentMethodParameter.FromParameter(p, p.GetFluentMethodName(methodPrefix)));

            var threadedParamFields = _bindingResolver.GetThreadedParameterFields(value.Method, preSatisfiedNames);
            var receiverField = value.ReceiverParameter is { } receiver
                ? ImmutableArray.Create(FluentMethodParameter.FromParameter(receiver, receiver.Name))
                : ImmutableArray<FluentMethodParameter>.Empty;

            var hasStep = node.Key.Length > 0;
            ImmutableArray<FluentMethodParameter> allParameterFields = hasStep
                ? [..receiverField, ..threadedParamFields, ..node.Key, ..optionalParamFields]
                : [..receiverField, ..threadedParamFields, ..node.Key];

            var terminalMethod = BuildTerminalMethod(
                rootType.ContainingNamespace, value, preSatisfiedNames, allParameterFields, valueSources);

            _unreachableTargetAnalyzer.AddReachableMethod(terminalMethod);
            yield return terminalMethod;
        }
    }

    /// <summary>
    /// Constructs an <see cref="AccumulatorFluentStep"/> with its <see cref="AccumulatorMethod"/>s,
    /// optional <see cref="AccumulatorBulkMethod"/>s, and a <see cref="TerminalMethod"/>, then yields:
    /// <list type="bullet">
    ///   <item>An <see cref="AccumulatorTransitionMethod"/> (parameterless, Phase 22 entry).</item>
    ///   <item>Zero or more <see cref="AccumulatorBulkTransitionMethod"/>s — one per collection parameter
    ///   that carries <c>[FluentMethod]</c>, providing a parameterised <c>IEnumerable&lt;T&gt;</c>
    ///   entry path (Phase 23 COMP-01).</item>
    /// </list>
    /// All methods transition to the same accumulator step. The terminal lives exclusively on the
    /// accumulator step — not on any regular step (RESEARCH.md Open Question 3).
    /// </summary>
    private IEnumerable<IFluentMethod> BuildAccumulatorTransitions(
        INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, TargetMetadata>.Node node,
        TargetMetadata value,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueSources,
        string methodPrefix)
    {
        var preSatisfiedNames = _bindingResolver.IsSelfReferencing(value.Method)
            ? new HashSet<string>()
            : _bindingResolver.GetPreSatisfiedParameterNames(value.Method);

        var threadedParamFields = _bindingResolver.GetThreadedParameterFields(value.Method, preSatisfiedNames);
        var receiverField = value.ReceiverParameter is { } receiver
            ? ImmutableArray.Create(FluentMethodParameter.FromParameter(receiver, receiver.Name))
            : ImmutableArray<FluentMethodParameter>.Empty;

        var keyParamFieldNames = new HashSet<string>(
            node.Key.Select(k => k.SourceName.ToParameterFieldName()));
        var optionalParamFields = value.OptionalParameters
            .Where(p => !keyParamFieldNames.Contains(p.Name.ToParameterFieldName()))
            .Select(p => FluentMethodParameter.FromParameter(p, p.GetFluentMethodName(methodPrefix)));

        // The forwarded parameters on the accumulator step are the non-collection params already
        // captured by the preceding regular trie step (node.Key), plus threaded/receiver fields.
        var hasStep = node.Key.Length > 0;
        ImmutableArray<FluentMethodParameter> forwardedParamFields = hasStep
            ? [..receiverField, ..threadedParamFields, ..node.Key, ..optionalParamFields]
            : [..receiverField, ..threadedParamFields];

        // Build value storage for the accumulator step from the forwarded non-collection params.
        var accumulatorValueStorage = BuildAccumulatorValueStorage(node, valueSources, rootType);

        // Create the accumulator step — Index is assigned by the step-indexing pass after collection.
        var accumulatorStep = new AccumulatorFluentStep(rootType)
        {
            ForwardedTargetParameters = new ParameterSequence(node.Key),
            ValueStorage = accumulatorValueStorage,
            CollectionParameters = value.CollectionParameters,
            CandidateTargets = [value.Method],
        };

        // The available fields on the accumulator step for AddX methods:
        // forwarded non-collection fields + one per collection parameter (for the field names).
        var collectionFieldParams = value.CollectionParameters
            .Select(cp => FluentMethodParameter.FromParameter(cp.Parameter, cp.MethodName))
            .ToImmutableArray();
        ImmutableArray<FluentMethodParameter> allAccumulatorFields = [..forwardedParamFields, ..collectionFieldParams];

        // Build one AccumulatorMethod per collection parameter.
        var addMethods = value.CollectionParameters
            .Select(cp => new AccumulatorMethod(
                cp,
                accumulatorStep,
                rootType.ContainingNamespace,
                allAccumulatorFields,
                accumulatorValueStorage))
            .ToList<IFluentMethod>();

        // Build one AccumulatorBulkMethod per collection parameter that also carries [FluentMethod].
        // These are the WithXs(IEnumerable<T>) self-returning methods on the accumulator step (COMP-02).
        var bulkMethods = value.CollectionParameters
            .Where(HasFluentMethodAttribute)
            .Select(cp => new AccumulatorBulkMethod(
                cp,
                accumulatorStep,
                rootType.ContainingNamespace,
                allAccumulatorFields,
                accumulatorValueStorage,
                bulkMethodName: ResolveBulkMethodName(cp, value),
                compilation))
            .ToList<IFluentMethod>();

        // Build the terminal method on the accumulator step.
        // allParameterFields must be in constructor-parameter order: forwarded first, then collection.
        // AccumulatorStepDeclaration.BuildTerminalArgument maps by field.SourceName to collectionLookup,
        // so we include ALL constructor parameters in the original method.Parameters order.
        var allTerminalFields = BuildTerminalFieldsInParameterOrder(value, forwardedParamFields, preSatisfiedNames, rootType);

        var mergedValueSources = _bindingResolver.MergeThreadedValueSources(
            value.Method, accumulatorValueStorage, preSatisfiedNames);

        var verb = value.Context.TerminalVerb ?? "Create";
        var terminalName = value.TerminalMethod == TerminalMethodKind.FixedName
            ? verb
            : $"{verb}{value.Method.ContainingType.ToCreateMethodSuffix()}";

        var terminal = new TerminalMethod(
            rootType.ContainingNamespace,
            value,
            allTerminalFields,
            mergedValueSources,
            terminalName);

        _unreachableTargetAnalyzer.AddReachableMethod(terminal);

        accumulatorStep.FluentMethods = [..addMethods, ..bulkMethods, terminal];

        // The transition method replaces the terminal on the last regular step.
        // It is parameterless and returns the accumulator step.
        // Apply the same DynamicSuffix / FixedName logic as terminal methods to avoid name
        // collisions when multiple all-collection targets share the same root node.
        var transitionName = value.TerminalMethod == TerminalMethodKind.FixedName
            ? "Build"
            : $"Build{value.Method.ContainingType.ToCreateMethodSuffix()}";

        // Yield the parameterless transition (Phase 22 BACK-01 preserved).
        yield return new AccumulatorTransitionMethod(
            name: transitionName,
            returnStep: accumulatorStep,
            rootNamespace: rootType.ContainingNamespace,
            availableParameterFields: forwardedParamFields,
            methodParameters: ImmutableArray<FluentMethodParameter>.Empty,
            valueSources: valueSources);

        // Yield one AccumulatorBulkTransitionMethod per collection parameter carrying [FluentMethod].
        // These provide a parameterised IEnumerable<T> entry into the same accumulator step (COMP-01).
        foreach (var cp in value.CollectionParameters.Where(HasFluentMethodAttribute))
        {
            var bulkName = ResolveBulkMethodName(cp, value);
            yield return new AccumulatorBulkTransitionMethod(
                name: bulkName,
                returnStep: accumulatorStep,
                rootNamespace: rootType.ContainingNamespace,
                availableParameterFields: forwardedParamFields,
                valueSources: valueSources,
                collectionParameter: cp,
                compilation: compilation);
        }
    }

    /// <summary>
    /// Determines whether the given <see cref="CollectionParameterInfo"/> corresponds to a parameter
    /// that also carries <c>[FluentMethod]</c>, meaning it should produce a bulk transition pair.
    /// </summary>
    private static bool HasFluentMethodAttribute(CollectionParameterInfo cp) =>
        cp.Parameter.GetAttribute(TypeName.FluentMethodAttribute) is not null;

    /// <summary>
    /// Resolves the bulk method name for a collection parameter that carries <c>[FluentMethod]</c>.
    /// Uses the same resolution logic as the regular trie: explicit attribute argument, or
    /// <c>With{Capitalized}</c> default.
    /// </summary>
    private static string ResolveBulkMethodName(CollectionParameterInfo cp, TargetMetadata value)
    {
        var methodPrefix = value.Context.MethodPrefix ?? "With";
        return cp.Parameter.GetFluentMethodName(methodPrefix);
    }

    /// <summary>
    /// Builds the value storage for the accumulator step, containing only the forwarded non-collection
    /// constructor parameters (the ones already captured by the preceding regular step).
    /// </summary>
    private static OrderedDictionary<IParameterSymbol, IFluentValueStorage> BuildAccumulatorValueStorage(
        Trie<FluentMethodParameter, TargetMetadata>.Node node,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> precedingStepValueSources,
        INamedTypeSymbol rootType)
    {
        // Copy only the entries for node.Key parameters (the trie path params forwarded to the accumulator).
        var keyParamNames = new HashSet<string>(
            node.Key.Select(k => k.SourceName),
            StringComparer.Ordinal);

        var filteredEntries = precedingStepValueSources
            .Where(kvp => keyParamNames.Contains(kvp.Key.Name))
            .Select(kvp => new KeyValuePair<IParameterSymbol, IFluentValueStorage>(kvp.Key, kvp.Value));

        return new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(filteredEntries);
    }

    /// <summary>
    /// Builds the terminal method's parameter field list in the original constructor parameter order,
    /// interleaving forwarded non-collection fields and collection parameter fields so that
    /// <c>AccumulatorStepDeclaration</c> can map each argument correctly.
    /// </summary>
    private ImmutableArray<FluentMethodParameter> BuildTerminalFieldsInParameterOrder(
        TargetMetadata value,
        ImmutableArray<FluentMethodParameter> forwardedParamFields,
        HashSet<string> preSatisfiedNames,
        INamedTypeSymbol rootType)
    {
        var collectionParamNames = new HashSet<string>(
            value.CollectionParameters.Select(cp => cp.Parameter.Name),
            StringComparer.Ordinal);

        // Build a lookup from param name → FluentMethodParameter for forwarded fields
        var forwardedByName = forwardedParamFields
            .ToDictionary(f => f.SourceName, StringComparer.Ordinal);

        // Walk the original constructor parameter list in order to emit args in the right order.
        // Pre-satisfied (threaded) parameters are excluded; receiver parameters are excluded.
        var result = new List<FluentMethodParameter>();
        foreach (var param in value.Method.Parameters)
        {
            if (preSatisfiedNames.Contains(param.Name)) continue;
            if (SymbolEqualityComparer.Default.Equals(param, value.ReceiverParameter)) continue;

            if (collectionParamNames.Contains(param.Name))
            {
                // Collection parameter: emit as a regular FluentMethodParameter (SourceType = collection type)
                // so AccumulatorStepDeclaration can look it up in collectionLookup by SourceName.
                result.Add(FluentMethodParameter.FromParameter(param, param.Name));
            }
            else if (forwardedByName.TryGetValue(param.Name, out var fwd))
            {
                result.Add(fwd);
            }
        }

        return [..result];
    }

    /// <summary>
    /// Builds a <see cref="TerminalMethod"/> for a constructor metadata entry, resolving the terminal method name
    /// and merging threaded parameter value sources.
    /// </summary>
    private TerminalMethod BuildTerminalMethod(
        INamespaceSymbol rootNamespace,
        TargetMetadata metadata,
        HashSet<string> preSatisfiedNames,
        ImmutableArray<FluentMethodParameter> allParameterFields,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueSources)
    {
        var verb = metadata.Context.TerminalVerb ?? "Create";
        var methodName = metadata.TerminalMethod == TerminalMethodKind.FixedName
            ? verb
            : $"{verb}{metadata.Method.ContainingType.ToCreateMethodSuffix()}";

        var mergedValueSources = _bindingResolver.MergeThreadedValueSources(
            metadata.Method, valueSources, preSatisfiedNames);

        return new TerminalMethod(rootNamespace, metadata, allParameterFields, mergedValueSources, methodName);
    }

    /// <summary>
    /// Creates type-first builder chains by grouping constructors by target type,
    /// building a parameter trie per group, and generating entry methods + step chains.
    /// </summary>
    private (ImmutableArray<IFluentMethod> EntryMethods, ImmutableArray<IFluentStep> Steps)
        CreateTypeFirstChains(
            INamedTypeSymbol rootType,
            ImmutableArray<FluentTargetContext> typeFirstContexts,
            int startingStepIndex)
    {
        var allEntryMethods = new List<IFluentMethod>();
        var allSteps = new List<IFluentStep>();
        var stepIndex = startingStepIndex;

        // Group by namespace + type name, merging generics within the same namespace
        // (Circle and Circle<T> in the same namespace share a group; A.Circle and B.Circle do not)
        var groups = typeFirstContexts
            .GroupBy(c =>
            {
                var type = c.Method.ContainingType;
                var ns = type.ContainingNamespace?.ToDisplayString() ?? "";
                return $"{ns}.{type.Name}";
            })
            .ToList();

        // Detect ambiguous entry method names across groups
        var entryMethodNames = groups
            .GroupBy(g => g.First().EntryMethodName)
            .Where(nameGroup => nameGroup.Count() > 1)
            .ToList();

        if (entryMethodNames.Count > 0)
        {
            foreach (var collision in entryMethodNames)
            {
                var collidingTypes = collision
                    .SelectMany(g => g.Select(c => c.Method.ContainingType.ToDisplayString()))
                    .Distinct()
                    .ToArray();

                var location = collision
                    .SelectMany(g => g.Select(c => c.AttributeData))
                    .Select(a => a.ApplicationSyntaxReference?.GetSyntax().GetLocation())
                    .FirstOrDefault(l => l is not null) ?? Location.None;

                _diagnostics.Add(Diagnostic.Create(
                    FluentDiagnostics.AmbiguousEntryMethod,
                    location,
                    collision.Key,
                    string.Join(", ", collidingTypes.Select(t => $"'{t}'"))));
            }

            // Remove colliding groups
            var collidingNames = new HashSet<string>(entryMethodNames.Select(n => n.Key));
            groups.RemoveAll(g => collidingNames.Contains(g.First().EntryMethodName));
        }

        foreach (var group in groups)
        {
            var contexts = group.ToImmutableArray();
            var targetTypeName = contexts.First().Method.ContainingType.Name;

            // Build a parameter trie for this target type group
            var trie = CreateFluentStepTrie(contexts);

            // Force Fixed terminal method on all trie end values so terminals are "Create()"
            ForceFixedCreateMethod(trie.Root);

            // Use a separate step builder to avoid collisions with parameter-first steps
            var typeFirstStepDict = new OrderedDictionary<ParameterSequence, RegularFluentStep>();
            var typeFirstStepBuilder = new FluentStepBuilder(typeFirstStepDict, _diagnostics);

            // Convert the trie using the separate step builder
            var trieRootMethods = ConvertNodeToFluentFluentMethods(rootType, trie.Root, [], typeFirstStepBuilder);

            if (trieRootMethods.IsEmpty) continue;

            // Collect all steps from the trie conversion
            var childSteps = trieRootMethods
                .Select(m => m.Return)
                .OfType<IFluentStep>();
            var descendantSteps = FluentStepBuilder.GetDescendentFluentSteps(childSteps)
                .DistinctBy(step => step.KnownTargetParameters)
                .ToList();

            // Separate terminal methods from step methods so terminal methods appear last
            var stepMethods = trieRootMethods.Where(m => m is not TerminalMethod).ToList();
            var terminalMethods = trieRootMethods.Where(m => m is TerminalMethod).ToList();

            // Create the type-first root step that wraps the trie's root methods
            var rootStep = new RegularFluentStep(
                rootType,
                contexts.SelectMany(c => new[] { c.Method }))
            {
                KnownTargetParameters = [],
                FluentMethods = new List<IFluentMethod>(stepMethods),
                IsEndStep = trie.Root.IsEnd,
                ValueStorage = new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(),
                TypeFirstTargetName = targetTypeName,
            };
            rootStep.Index = stepIndex++;

            // Handle all-optional constructors on the root step
            if (trie.Root.IsEnd)
            {
                AddOptionalMethodsToStep(rootType, trie.Root, rootStep, rootStep.ValueStorage);
            }

            // Add terminal methods after optional methods
            rootStep.FluentMethods.AddRange(terminalMethods);

            // Number and tag all descendant steps with the target type name
            foreach (var step in descendantSteps)
            {
                if (step is RegularFluentStep regularStep)
                {
                    regularStep.TypeFirstTargetName = targetTypeName;
                    regularStep.Index = stepIndex++;
                }
            }

            // Determine entry method name
            var entryMethodName = contexts.First().EntryMethodName;

            var candidateTargets = contexts
                .Select(c => c.Method)
                .ToImmutableArray();

            var entryMethod = new TypeFirstEntryMethod(
                entryMethodName,
                rootStep,
                rootType.ContainingNamespace,
                candidateTargets);

            allEntryMethods.Add(entryMethod);
            allSteps.Add(rootStep);
            allSteps.AddRange(descendantSteps);

            // Propagate extension receiver to all type-first steps in this group
            var groupReceiver = contexts
                .Select(c => c.ReceiverParameter)
                .FirstOrDefault(r => r is not null);
            if (groupReceiver is not null)
            {
                ParameterBindingResolver.PropagateReceiverToSteps(
                    [rootStep, ..descendantSteps],
                    groupReceiver);
            }
        }

        return ([..allEntryMethods], [..allSteps]);
    }

    /// <summary>
    /// Recursively sets CreateMethod to Fixed on all end values in the trie,
    /// ensuring type-first terminal methods are named "Create()" instead of "CreateTypeName()".
    /// </summary>
    private static void ForceFixedCreateMethod(Trie<FluentMethodParameter, TargetMetadata>.Node node)
    {
        foreach (var endValue in node.EndValues)
            endValue.TerminalMethod = TerminalMethodKind.FixedName;

        foreach (var child in node.Children.Values)
            ForceFixedCreateMethod(child);
    }

    private Trie<FluentMethodParameter, TargetMetadata> CreateFluentStepTrie(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var trie = new Trie<FluentMethodParameter, TargetMetadata>();
        foreach (var targetContext in fluentTargetContexts)
        {
            var methodPrefix = targetContext.MethodPrefix ?? "With";

            var preSatisfiedNames = _bindingResolver.IsSelfReferencing(targetContext.Method)
                ? []
                : _bindingResolver.GetPreSatisfiedParameterNames(targetContext.Method);

            // RESEARCH.md Pattern 1 / Pitfall 2: collection parameters must be excluded from the trie
            // so they are handled as accumulator AddX methods, not regular With-step methods.
            var collectionParamNames = new HashSet<string>(
                targetContext.CollectionParameters.Select(cp => cp.Parameter.Name),
                StringComparer.Ordinal);

            var requiredParameters = targetContext.Method.Parameters
                .Where(p => !p.HasExplicitDefaultValue)
                .Where(p => !preSatisfiedNames.Contains(p.Name))
                .Where(p => !SymbolEqualityComparer.Default.Equals(p, targetContext.ReceiverParameter))
                .Where(p => !collectionParamNames.Contains(p.Name))
                .Select(ToFluentMethodParameter);

            var metadata = new TargetMetadata(targetContext);

            // Always insert the required-only path (marks root as end when all params are optional)
            trie.Insert(requiredParameters, metadata);

            // When all params are optional with exactly one param, insert it as a trie path
            // so that step methods are generated (e.g., Animal.WithLegs(2).CreateDog())
            // Multi-param all-optional constructors are handled by CreateAllOptionalStepsAndGatewayMethods
            var effectiveRequiredCount = metadata.RequiredParameterCount - preSatisfiedNames.Count;
            if (effectiveRequiredCount <= 0 && metadata.OptionalParameters.Length == 1)
            {
                var optionalParameters = targetContext.Method.Parameters
                    .Where(p => p.HasExplicitDefaultValue)
                    .Select(ToFluentMethodParameter);

                trie.Insert(optionalParameters, metadata.Clone());
            }

            continue;

            FluentMethodParameter ToFluentMethodParameter(IParameterSymbol parameter)
            {
                var methodNames = compilation
                    .GetMultipleFluentMethodSymbols(parameter)
                    .Select(methodInfo => methodInfo.Method.Name)
                    .DefaultIfEmpty(parameter.GetFluentMethodName(methodPrefix))
                    .ToImmutableArray();

                if (parameter.Type is not INamedTypeSymbol { IsTupleType: true } tupleType)
                    return FluentMethodParameter.FromParameter(parameter, methodNames);
                
                var tupleElements = tupleType.TupleElements;
                var allNamed = tupleElements.All(e => !e.IsImplicitlyDeclared);

                if (allNamed)
                {
                    var elements = tupleElements
                        .Select(e => (e.Name, e.Type))
                        .ToImmutableArray();

                    return TupleFluentMethodParameter.FromTupleParameter(parameter, methodNames, elements);
                }

                _diagnostics.Add(Diagnostic.Create(
                    FluentDiagnostics.UnnamedTupleElements,
                    parameter.Locations.FirstOrDefault(),
                    parameter.Name));

                return FluentMethodParameter.FromParameter(parameter, methodNames);
            }
        }

        return trie;
    }

    private (ImmutableArray<IFluentMethod> GatewayMethods, ImmutableArray<IFluentStep> Steps)
        CreateAllOptionalStepsAndGatewayMethods(
            INamedTypeSymbol rootType,
            Trie<FluentMethodParameter, TargetMetadata>.Node rootNode,
            int nextStepIndex)
    {
        if (!rootNode.IsEnd) return ([], []);

        // Find all-optional constructors with multiple params (not handled by trie insertion)
        // Account for pre-satisfied (threaded) parameters when determining effective required count
        var allOptionalTargets = rootNode.EndValues
            .Where(v =>
            {
                var preSatisfiedCount = _bindingResolver.IsSelfReferencing(v.Method)
                    ? 0
                    : _bindingResolver.GetPreSatisfiedParameterNames(v.Method).Count;
                return v.RequiredParameterCount - preSatisfiedCount <= 0
                    && v.OptionalParameters.Length > 1;
            })
            .ToList();

        if (allOptionalTargets.Count == 0) return ([], []);

        // Group constructors that share the same set of optional parameters
        var groups = allOptionalTargets
            .GroupBy(v => string.Join(",", v.OptionalParameters
                .Select(p => $"{p.Type.ToGlobalDisplayString()}:{p.Name}")
                .OrderBy(s => s)))
            .ToList();

        var gatewayMethods = new List<IFluentMethod>();
        var steps = new List<IFluentStep>();

        foreach (var group in groups)
        {
            var constructors = group.ToList();
            var allOptionalParams = constructors.First().OptionalParameters;

            // Create value storage with all optional params as readonly fields
            // Readonly fields enable aggressive inlining; setter methods return new instances
            var valueStorages = new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(
                allOptionalParams.Select(p =>
                    new KeyValuePair<IParameterSymbol, IFluentValueStorage>(
                        p,
                        FieldStorage.FromParameter(p, rootType.ContainingNamespace))));

            var knownTargetParameters = new ParameterSequence(allOptionalParams);

            // Create the shared step
            var candidateCtors = constructors
                .SelectMany(c => c.CandidateTargets)
                .Distinct<IMethodSymbol>(SymbolEqualityComparer.Default);

            var step = new RegularFluentStep(rootType, candidateCtors)
            {
                KnownTargetParameters = knownTargetParameters,
                FluentMethods = [],
                IsEndStep = true,
                IsAllOptionalStep = true,
                ValueStorage = valueStorages,
                Index = nextStepIndex + steps.Count
            };

            // Add optional setter methods to the step
            var methodPrefix = constructors.First().Context.MethodPrefix ?? "With";
            foreach (var parameter in allOptionalParams)
            {
                var methodName = parameter.GetFluentMethodName(methodPrefix);
                var optionalMethod = new OptionalFluentMethod(
                    methodName,
                    parameter,
                    step,
                    rootType.ContainingNamespace,
                    [],
                    valueStorages);
                step.FluentMethods.Add(optionalMethod);
            }

            // Add terminal methods to the step
            foreach (var metadata in constructors)
            {
                if (metadata.TerminalMethod == TerminalMethodKind.None) continue;

                var preSatisfiedNames = _bindingResolver.IsSelfReferencing(metadata.Method)
                    ? new HashSet<string>()
                    : _bindingResolver.GetPreSatisfiedParameterNames(metadata.Method);
                var threadedParamFields = _bindingResolver.GetThreadedParameterFields(metadata.Method, preSatisfiedNames);
                var optionalParamFields = allOptionalParams
                    .Select(p => FluentMethodParameter.FromParameter(p, p.GetFluentMethodName(methodPrefix)));
                ImmutableArray<FluentMethodParameter> allParamFields = [..threadedParamFields, ..optionalParamFields];

                var terminalMethod = BuildTerminalMethod(
                    rootType.ContainingNamespace, metadata, preSatisfiedNames, allParamFields, valueStorages);

                _unreachableTargetAnalyzer.AddReachableMethod(terminalMethod);
                step.FluentMethods.Add(terminalMethod);
            }

            steps.Add(step);

            // Create gateway methods from root to this step (one per optional param)
            foreach (var parameter in allOptionalParams)
            {
                var methodName = parameter.GetFluentMethodName(methodPrefix);
                var gateway = new OptionalGatewayMethod(
                    methodName,
                    parameter,
                    step,
                    rootType.ContainingNamespace,
                    valueStorages);
                gatewayMethods.Add(gateway);
            }
        }

        return ([..gatewayMethods], [..steps]);
    }

    /// <summary>
    /// Populates <see cref="IFluentReturn.UnavailableTargets"/> on every step and terminal return
    /// in the graph, after method selection is complete. The unavailable set is the subset of
    /// candidate targets that the <see cref="UnreachableTargetAnalyzer"/> did not mark as reached.
    /// </summary>
    private void MarkUnavailableTargets(
        ImmutableArray<IFluentMethod> rootMethods,
        ImmutableArray<IFluentStep> steps)
    {
        foreach (var step in steps)
            step.UnavailableTargets = ComputeUnavailable(step.CandidateTargets);

        MarkReturnsFromMethods(rootMethods);
        return;

        void MarkReturnsFromMethods(IEnumerable<IFluentMethod> methods, HashSet<IFluentStep>? visitedSteps = null)
        {
            // Exclude methods that return the step they live on (optional setters and any
            // ISelfReturningAccumulatorMethod: AddX and WithXs bulk methods), which would otherwise
            // produce an infinite recursion (Phase 22 Plan 04 STATE.md decision generalized via marker).
            var forwardMethods = methods
                .Where(m => m is not OptionalFluentMethod and not OptionalPropertyFluentMethod
                            and not ISelfReturningAccumulatorMethod);

            foreach (var method in forwardMethods)
            {
                switch (method.Return)
                {
                    case TargetTypeReturn targetTypeReturn:
                        targetTypeReturn.UnavailableTargets = ComputeUnavailable(targetTypeReturn.CandidateTargets);
                        break;
                    case IFluentStep step:
                        // Guard against visiting accumulator steps twice (their AccumulatorMethod
                        // would loop back to themselves if we recurse without a visited check).
                        visitedSteps ??= new HashSet<IFluentStep>();
                        if (!visitedSteps.Add(step)) break;
                        // The step's own UnavailableTargets was set in the outer loop;
                        // here we recurse to reach nested TargetTypeReturns.
                        MarkReturnsFromMethods(step.FluentMethods, visitedSteps);
                        break;
                }
            }
        }

        ImmutableArray<IMethodSymbol> ComputeUnavailable(ImmutableArray<IMethodSymbol> candidates) =>
            [..candidates.Where(target => !_unreachableTargetAnalyzer.IsReachable(target))];
    }

    private static ImmutableArray<INamespaceSymbol> GetUsingStatements(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        return
        [
            ..fluentTargetContexts
                .SelectMany(ctx => ctx.Method.Parameters)
                .Select(parameter => parameter.Type.ContainingNamespace)
                .Concat(fluentTargetContexts.Select(ctx => ctx.Method.ContainingType.ContainingNamespace))
                .Where(namespaceSymbol => namespaceSymbol is not null)
                .Select(namespaceSymbol => (namespaceSymbol, displayString: namespaceSymbol.ToDisplayString()))
                .DistinctBy(ns => ns.displayString)
                .OrderBy(ns => ns.displayString)
                .Select(ns => ns.namespaceSymbol)
        ];
    }

}
