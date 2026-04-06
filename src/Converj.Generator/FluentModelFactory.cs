using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Converj.Generator.ConstructorAnalysis;
using Converj.Generator.Diagnostics;
using Converj.Generator.ModelBuilding;

namespace Converj.Generator;

internal class FluentModelFactory(Compilation compilation)
{
    private readonly DiagnosticList _diagnostics = [];
    private readonly OrderedDictionary<ParameterSequence, RegularFluentStep> _regularFluentSteps = new();
    private readonly UnreachableConstructorAnalyzer _unreachableConstructorAnalyzer = new();

    private FluentMethodSelector _methodSelector = null!;
    private FluentStepBuilder _stepBuilder = null!;
    private ImmutableArray<FluentParameterBinding> _threadedParameters = [];
    private INamedTypeSymbol _rootType = null!;

    public FluentFactoryCompilationUnit CreateFluentFactoryCompilationUnit(
        INamedTypeSymbol rootType,
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        _regularFluentSteps.Clear();
        _diagnostics.Clear();
        _unreachableConstructorAnalyzer.Clear();

        // Filter out instance method targets and report diagnostics
        foreach (var instanceMethod in fluentTargetContexts.Where(c => c.IsInstanceMethodTarget))
        {
            var location = instanceMethod.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                           ?? Location.None;
            _diagnostics.Add(Diagnostic.Create(
                FluentDiagnostics.InstanceMethodTarget,
                location,
                instanceMethod.Constructor.Name));
        }

        _diagnostics.AddRange(TargetContextFilter.ValidateThisAttributeUsage(fluentTargetContexts));

        fluentTargetContexts = [
            ..fluentTargetContexts
                .Where(c => !c.IsInstanceMethodTarget)
        ];

        _diagnostics.AddRange(TargetContextFilter.ValidateRootForExtensionTargets(rootType, fluentTargetContexts));

        var (validContexts, unsupportedModifierDiagnostics) =
            TargetContextFilter.FilterUnsupportedParameterModifierConstructors(fluentTargetContexts);

        _diagnostics.AddRange(unsupportedModifierDiagnostics);

        var (accessibleContexts, inaccessibleConstructorDiagnostics) =
            TargetContextFilter.FilterInaccessibleConstructors(validContexts);

        _diagnostics.AddRange(inaccessibleConstructorDiagnostics);
        validContexts = accessibleContexts;

        validContexts = TargetContextFilter.FilterErrorTypeConstructors(validContexts);

        if (validContexts.IsEmpty)
            return new FluentFactoryCompilationUnit(rootType) { Diagnostics = _diagnostics };

        fluentTargetContexts = validContexts;

        _unreachableConstructorAnalyzer.AddAllFluentConstructors(fluentTargetContexts.Select(context => context.Constructor));
        _methodSelector = new FluentMethodSelector(compilation, _diagnostics, _unreachableConstructorAnalyzer);

        _stepBuilder = new FluentStepBuilder(_regularFluentSteps, _diagnostics);
        _rootType = rootType;

        var fluentParameterMembers = FluentParameterAnalyzer.Analyze(rootType, _diagnostics);
        _threadedParameters = CreateThreadedParameterBindings(rootType, fluentParameterMembers, fluentTargetContexts);

        var usings = GetUsingStatements(fluentTargetContexts);

        _diagnostics.AddRange(fluentTargetContexts.GetDiagnostics());

        // Collect property analysis diagnostics
        foreach (var context in fluentTargetContexts)
            _diagnostics.AddRange(context.PropertyDiagnostics);

        if (_diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return new FluentFactoryCompilationUnit(rootType) { Diagnostics = _diagnostics, Usings = usings };
        }

        // Split constructors: type-first ones are excluded from the parameter-first trie
        ImmutableArray<FluentTargetContext> parameterFirstContexts = [
            ..fluentTargetContexts
                .Where(c => !c.HasEntryMethod)
        ];

        var stepTrie = CreateFluentStepTrie(parameterFirstContexts);

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

        if (!_threadedParameters.IsEmpty)
        {
            PropagateThreadedParametersToSteps(fluentBuilderSteps);
        }

        // Propagate extension receiver to all steps in the chain
        var receiverParameter = parameterFirstContexts
            .Select(c => c.ReceiverParameter)
            .FirstOrDefault(r => r is not null);
        if (receiverParameter is not null)
        {
            PropagateReceiverToSteps(fluentBuilderSteps, receiverParameter);
        }

        // Handle multi-param all-optional constructors via post-processing
        var (gatewayMethods, gatewaySteps) = CreateAllOptionalStepsAndGatewayMethods(
            rootType, stepTrie.Root, fluentBuilderSteps.Length);

        if (!_threadedParameters.IsEmpty && !gatewaySteps.IsEmpty)
        {
            PropagateThreadedParametersToSteps(gatewaySteps);
        }

        if (receiverParameter is not null && !gatewaySteps.IsEmpty)
        {
            PropagateReceiverToSteps(gatewaySteps, receiverParameter);
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

            if (!_threadedParameters.IsEmpty && !typeFirstSteps.IsEmpty)
            {
                PropagateThreadedParametersToSteps(typeFirstSteps);
            }

            fluentRootMethods = [..fluentRootMethods, ..typeFirstEntryMethods];
            fluentBuilderSteps = [..fluentBuilderSteps, ..typeFirstSteps];
        }

        // Post-process: insert required property steps between end steps and creation methods
        var (propertySteps, updatedRootMethods) = PropertyStepEnricher.InsertRequiredPropertySteps(rootType, fluentRootMethods, fluentBuilderSteps);
        if (propertySteps.Length > 0)
        {
            if (!_threadedParameters.IsEmpty)
                PropagateThreadedParametersToSteps(propertySteps);

            if (receiverParameter is not null)
                PropagateReceiverToSteps(propertySteps, receiverParameter);

            fluentBuilderSteps = [..fluentBuilderSteps, ..propertySteps];
        }
        fluentRootMethods = updatedRootMethods;

        _diagnostics.AddRange(_unreachableConstructorAnalyzer.GetUnreachableConstructorsDiagnostics());
        var sampleConstructorContext = fluentTargetContexts.First();

        return new FluentFactoryCompilationUnit(rootType)
        {
            FluentMethods = fluentRootMethods,
            FluentSteps = fluentBuilderSteps,
            Usings = usings,
            IsStatic = sampleConstructorContext.IsStatic && _threadedParameters.IsEmpty,
            TypeKind = sampleConstructorContext.TypeKind,
            Accessibility = sampleConstructorContext.Accessibility,
            IsRecord = sampleConstructorContext.IsRecord,
            ThreadedParameters = _threadedParameters,
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
                (rootType, child) => _stepBuilder.ConvertNodeToFluentStep(
                    rootType, child, ConvertNodeToFluentFluentMethods, AddOptionalMethodsToStep)),
            ..ConvertNodeToCreationMethods(type, node, valueStorages)
        ];

        return fluentMethods;
    }

    private void AddOptionalMethodsToStep(
        INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node,
        IFluentStep step,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages)
    {
        if (!node.IsEnd) return;

        // Exclude optional params already in the trie path (handled as regular step methods)
        var knownParamFieldNames = new HashSet<string>(
            step.KnownConstructorParameters.Select(p => p.Name.ToParameterFieldName()));

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

    private IEnumerable<IFluentMethod> ConvertNodeToCreationMethods(INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueSources)
    {
        if (!node.IsEnd) yield break;

        var methodPrefix = node.EndValues
            .Select(v => v.Context.MethodPrefix)
            .FirstOrDefault() ?? "With";

        var creationMethods =
            from value in node.EndValues
            let preSatisfiedNames = IsSelfReferencing(value.Constructor, _rootType)
                ? new HashSet<string>()
                : GetPreSatisfiedParameterNames(value.Constructor)
            let receiverCount = value.ReceiverParameter is not null ? 1 : 0
            where node.Key.Length >= value.RequiredParameterCount - preSatisfiedNames.Count - receiverCount
            where node.Key.Length <= value.Constructor.Parameters.Length - preSatisfiedNames.Count - receiverCount
            where value.TerminalMethod != TerminalMethodKind.None
            let verb = value.Context.TerminalVerb ?? "Create"
            let methodName = value.TerminalMethod == TerminalMethodKind.FixedName
                ? verb
                : $"{verb}{value.Constructor.ContainingType.ToCreateMethodSuffix()}"
            let keyParamFieldNames = new HashSet<string>(
                node.Key.Select(k => k.SourceName.ToParameterFieldName()))
            let optionalParamFields = value.OptionalParameters
                .Where(p => !keyParamFieldNames.Contains(p.Name.ToParameterFieldName()))
                .Select(p => FluentMethodParameter.FromParameter(p, p.GetFluentMethodName(methodPrefix)))
            let hasStep = node.Key.Length > 0
            let threadedParamFields = GetThreadedParameterFields(value.Constructor, preSatisfiedNames)
            let receiverField = value.ReceiverParameter is { } receiver
                ? ImmutableArray.Create(FluentMethodParameter.FromParameter(receiver, receiver.Name))
                : ImmutableArray<FluentMethodParameter>.Empty
            let allParameterFields = hasStep
                ? receiverField.AddRange(threadedParamFields).AddRange(node.Key).AddRange(optionalParamFields)
                : receiverField.AddRange(threadedParamFields).AddRange(node.Key)
            let mergedValueSources = MergeThreadedValueSources(value.Constructor, valueSources, preSatisfiedNames)
            select new CreationMethod(
                rootType.ContainingNamespace,
                value,
                allParameterFields,
                mergedValueSources,
                methodName);

        foreach (var createMethod in creationMethods)
        {
            _unreachableConstructorAnalyzer.AddReachableMethod(createMethod);
            yield return createMethod;
        }
    }

    /// <summary>
    /// Gets FluentMethodParameter entries for threaded parameters of a constructor,
    /// ordered by their position in the target constructor.
    /// </summary>
    private ImmutableArray<FluentMethodParameter> GetThreadedParameterFields(
        IMethodSymbol constructor,
        HashSet<string> preSatisfiedNames)
    {
        if (preSatisfiedNames.Count == 0)
            return [];

        return
        [
            ..constructor.Parameters
                .Where(p => preSatisfiedNames.Contains(p.Name))
                .Select(p => FluentMethodParameter.FromParameter(p, p.Name))
        ];
    }

    /// <summary>
    /// Merges threaded parameter storage into value sources, ordered by the target constructor's parameter positions.
    /// This ensures the final constructor call has all arguments in the correct order.
    /// </summary>
    private OrderedDictionary<IParameterSymbol, IFluentValueStorage> MergeThreadedValueSources(
        IMethodSymbol constructor,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> stepValueSources,
        HashSet<string> preSatisfiedNames)
    {
        if (preSatisfiedNames.Count == 0)
            return stepValueSources;

        // Build a new OrderedDictionary in the target constructor's parameter order
        var merged = new OrderedDictionary<IParameterSymbol, IFluentValueStorage>();

        foreach (var ctorParam in constructor.Parameters)
        {
            if (preSatisfiedNames.Contains(ctorParam.Name))
            {
                merged.Add(ctorParam, FieldStorage.FromParameter(ctorParam, _rootType.ContainingNamespace));
            }
            else
            {
                // Step-acquired param — find it in the existing value sources
                var existingEntry = stepValueSources
                    .FirstOrDefault(kvp => kvp.Key.Name == ctorParam.Name);
                if (existingEntry.Value is not null)
                {
                    merged.Add(existingEntry.Key, existingEntry.Value);
                }
            }
        }

        return merged;
    }

    private static bool IsSelfReferencing(IMethodSymbol constructor, INamedTypeSymbol rootType) =>
        SymbolEqualityComparer.Default.Equals(
            constructor.ContainingType.OriginalDefinition,
            rootType.OriginalDefinition);

    /// <summary>
    /// Checks type compatibility between a factory member type and a target constructor parameter type.
    /// Handles type parameters from different generic types by comparing ordinal positions,
    /// since they will be unified at instantiation time via the open generic type reference.
    /// </summary>
    private static bool AreTypesCompatible(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        if (compilation.HasImplicitConversion(sourceType, targetType))
            return true;

        // Type parameters from different generic types (e.g., T from Container<T> vs T from Widget<T>)
        // are different symbols but will be the same type at instantiation since they share
        // the same ordinal position via the open generic type reference.
        if (sourceType is ITypeParameterSymbol sourceTypeParam &&
            targetType is ITypeParameterSymbol targetTypeParam)
        {
            return sourceTypeParam.Ordinal == targetTypeParam.Ordinal;
        }

        return false;
    }

    /// <summary>
    /// Sets ThreadedParameters on all steps and adds threaded param field storage to each step's ValueStorage.
    /// </summary>
    private void PropagateThreadedParametersToSteps(ImmutableArray<IFluentStep> steps)
    {
        foreach (var step in steps)
        {
            step.ThreadedParameters = _threadedParameters;

            foreach (var binding in _threadedParameters)
            {
                if (!step.ValueStorage.ContainsKey(binding.TargetParameter))
                {
                    step.ValueStorage.Add(binding.TargetParameter,
                        FieldStorage.FromParameter(binding.TargetParameter, _rootType.ContainingNamespace));
                }
            }
        }
    }


    /// <summary>
    /// Propagates the extension receiver parameter to all steps in the chain.
    /// Prepends it to KnownConstructorParameters and adds field storage.
    /// </summary>
    private static void PropagateReceiverToSteps(ImmutableArray<IFluentStep> steps, IParameterSymbol receiverParameter)
    {
        foreach (var step in steps)
        {
            step.ReceiverParameter = receiverParameter;

            // Prepend receiver to known parameters so it gets a field and constructor parameter
            if (step is RegularFluentStep regularStep)
            {
                regularStep.KnownConstructorParameters = new ParameterSequence(
                    [receiverParameter, ..regularStep.KnownConstructorParameters]);
            }

            if (!step.ValueStorage.ContainsKey(receiverParameter))
            {
                step.ValueStorage.Insert(0, receiverParameter,
                    FieldStorage.FromParameter(receiverParameter, step.Namespace));
            }
        }
    }

    /// <summary>
    /// Creates bindings between [FluentParameter] members and target constructor parameters.
    /// </summary>
    private ImmutableArray<FluentParameterBinding> CreateThreadedParameterBindings(
        INamedTypeSymbol rootType,
        ImmutableArray<FluentParameterMember> fluentParameterMembers,
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        if (fluentParameterMembers.IsEmpty)
            return [];

        var bindings = ImmutableArray.CreateBuilder<FluentParameterBinding>();

        var allTargetParams = fluentTargetContexts
            .Where(ctx => !IsSelfReferencing(ctx.Constructor, rootType))
            .SelectMany(ctx => ctx.Constructor.Parameters)
            .ToList();

        foreach (var member in fluentParameterMembers)
        {
            var matchingParams = allTargetParams
                .Where(p => string.Equals(p.Name, member.TargetParameterName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingParams.Count == 0)
            {
                if (!member.IsImplicit)
                {
                    _diagnostics.Add(Diagnostic.Create(
                        FluentDiagnostics.FluentParameterNoMatch,
                        member.Location,
                        member.MemberIdentifierName,
                        member.TargetParameterName));
                }

                continue;
            }

            // Check type assignability for all matches
            var allAssignable = true;
            foreach (var targetParam in matchingParams)
            {
                if (!AreTypesCompatible(compilation, member.Type, targetParam.Type))
                {
                    _diagnostics.Add(Diagnostic.Create(
                        FluentDiagnostics.FluentParameterTypeMismatch,
                        member.Location,
                        member.MemberIdentifierName,
                        member.Type.ToDisplayString(),
                        targetParam.Name,
                        targetParam.Type.ToDisplayString()));
                    allAssignable = false;
                }
            }

            if (!allAssignable) continue;

            // Create bindings for each matching parameter (deduped by constructor)
            var addedConstructors = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            foreach (var targetParam in matchingParams)
            {
                var constructor = (IMethodSymbol)targetParam.ContainingSymbol;
                if (addedConstructors.Add(constructor))
                {
                    bindings.Add(new FluentParameterBinding(member, targetParam));
                }
            }
        }

        return bindings.ToImmutable();
    }

    /// <summary>
    /// Gets the set of pre-satisfied parameter names for a given constructor based on threaded bindings.
    /// Matches by parameter name against all bindings (constructor identity is validated during binding creation).
    /// </summary>
    private HashSet<string> GetPreSatisfiedParameterNames(IMethodSymbol constructor)
    {
        var constructorParamNames = new HashSet<string>(constructor.Parameters.Select(p => p.Name));
        return new HashSet<string>(
            _threadedParameters
                .Where(b => constructorParamNames.Contains(b.TargetParameter.Name))
                .Select(b => b.TargetParameter.Name));
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
                var type = c.Constructor.ContainingType;
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
                    .SelectMany(g => g.Select(c => c.Constructor.ContainingType.ToDisplayString()))
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
            var targetTypeName = contexts.First().Constructor.ContainingType.Name;

            // Build a parameter trie for this target type group
            var trie = CreateFluentStepTrie(contexts);

            // Force Fixed creation method on all trie end values so terminals are "Create()"
            ForceFixedCreateMethod(trie.Root);

            // Use a separate step builder to avoid collisions with parameter-first steps
            var typeFirstStepDict = new OrderedDictionary<ParameterSequence, RegularFluentStep>();
            var typeFirstStepBuilder = new FluentStepBuilder(typeFirstStepDict, _diagnostics);

            // Convert the trie using the separate step builder
            var originalStepBuilder = _stepBuilder;
            _stepBuilder = typeFirstStepBuilder;
            var trieRootMethods = ConvertNodeToFluentFluentMethods(rootType, trie.Root, []);
            _stepBuilder = originalStepBuilder;

            if (trieRootMethods.IsEmpty) continue;

            // Collect all steps from the trie conversion
            var childSteps = trieRootMethods
                .Select(m => m.Return)
                .OfType<IFluentStep>();
            var descendantSteps = FluentStepBuilder.GetDescendentFluentSteps(childSteps)
                .DistinctBy(step => step.KnownConstructorParameters)
                .ToList();

            // Separate creation methods from step methods so creation methods appear last
            var stepMethods = trieRootMethods.Where(m => m is not CreationMethod).ToList();
            var creationMethods = trieRootMethods.Where(m => m is CreationMethod).ToList();

            // Create the type-first root step that wraps the trie's root methods
            var rootStep = new RegularFluentStep(
                rootType,
                contexts.SelectMany(c => new[] { c.Constructor }))
            {
                KnownConstructorParameters = [],
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

            // Add creation methods after optional methods
            rootStep.FluentMethods.AddRange(creationMethods);

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

            var candidateConstructors = contexts
                .Select(c => c.Constructor)
                .ToImmutableArray();

            var entryMethod = new TypeFirstEntryMethod(
                entryMethodName,
                rootStep,
                rootType.ContainingNamespace,
                candidateConstructors);

            allEntryMethods.Add(entryMethod);
            allSteps.Add(rootStep);
            allSteps.AddRange(descendantSteps);

            // Propagate extension receiver to all type-first steps in this group
            var groupReceiver = contexts
                .Select(c => c.ReceiverParameter)
                .FirstOrDefault(r => r is not null);
            if (groupReceiver is not null)
            {
                PropagateReceiverToSteps(
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
    private static void ForceFixedCreateMethod(Trie<FluentMethodParameter, ConstructorMetadata>.Node node)
    {
        foreach (var endValue in node.EndValues)
            endValue.TerminalMethod = TerminalMethodKind.FixedName;

        foreach (var child in node.Children.Values)
            ForceFixedCreateMethod(child);
    }

    private Trie<FluentMethodParameter, ConstructorMetadata> CreateFluentStepTrie(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var trie = new Trie<FluentMethodParameter, ConstructorMetadata>();
        foreach (var targetContext in fluentTargetContexts)
        {
            var methodPrefix = targetContext.MethodPrefix ?? "With";

            var preSatisfiedNames = IsSelfReferencing(targetContext.Constructor, _rootType)
                ? []
                : GetPreSatisfiedParameterNames(targetContext.Constructor);

            var requiredParameters = targetContext.Constructor.Parameters
                .Where(p => !p.HasExplicitDefaultValue)
                .Where(p => !preSatisfiedNames.Contains(p.Name))
                .Where(p => !SymbolEqualityComparer.Default.Equals(p, targetContext.ReceiverParameter))
                .Select(ToFluentMethodParameter);

            var metadata = new ConstructorMetadata(targetContext);

            // Always insert the required-only path (marks root as end when all params are optional)
            trie.Insert(requiredParameters, metadata);

            // When all params are optional with exactly one param, insert it as a trie path
            // so that step methods are generated (e.g., Animal.WithLegs(2).CreateDog())
            // Multi-param all-optional constructors are handled by CreateAllOptionalStepsAndGatewayMethods
            var effectiveRequiredCount = metadata.RequiredParameterCount - preSatisfiedNames.Count;
            if (effectiveRequiredCount <= 0 && metadata.OptionalParameters.Length == 1)
            {
                var optionalParameters = targetContext.Constructor.Parameters
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
            Trie<FluentMethodParameter, ConstructorMetadata>.Node rootNode,
            int nextStepIndex)
    {
        if (!rootNode.IsEnd) return ([], []);

        // Find all-optional constructors with multiple params (not handled by trie insertion)
        // Account for pre-satisfied (threaded) parameters when determining effective required count
        var allOptionalConstructors = rootNode.EndValues
            .Where(v =>
            {
                var preSatisfiedCount = IsSelfReferencing(v.Constructor, _rootType)
                    ? 0
                    : GetPreSatisfiedParameterNames(v.Constructor).Count;
                return v.RequiredParameterCount - preSatisfiedCount <= 0
                    && v.OptionalParameters.Length > 1;
            })
            .ToList();

        if (allOptionalConstructors.Count == 0) return ([], []);

        // Group constructors that share the same set of optional parameters
        var groups = allOptionalConstructors
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

            var knownConstructorParameters = new ParameterSequence(allOptionalParams);

            // Create the shared step
            var candidateCtors = constructors
                .SelectMany(c => c.CandidateConstructors)
                .Distinct<IMethodSymbol>(SymbolEqualityComparer.Default);

            var step = new RegularFluentStep(rootType, candidateCtors)
            {
                KnownConstructorParameters = knownConstructorParameters,
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

            // Add creation methods to the step
            foreach (var metadata in constructors)
            {
                if (metadata.TerminalMethod == TerminalMethodKind.None) continue;

                var verb = metadata.Context.TerminalVerb ?? "Create";
                var createMethodName = metadata.TerminalMethod == TerminalMethodKind.FixedName
                    ? verb
                    : $"{verb}{metadata.Constructor.ContainingType.ToCreateMethodSuffix()}";

                var preSatisfiedNames = IsSelfReferencing(metadata.Constructor, _rootType)
                    ? new HashSet<string>()
                    : GetPreSatisfiedParameterNames(metadata.Constructor);
                var threadedParamFields = GetThreadedParameterFields(metadata.Constructor, preSatisfiedNames);
                var optionalParamFields = allOptionalParams
                    .Select(p => FluentMethodParameter.FromParameter(p, p.GetFluentMethodName(methodPrefix)));
                var allParamFields = threadedParamFields.AddRange(optionalParamFields);
                var mergedValueSources = MergeThreadedValueSources(
                    metadata.Constructor, valueStorages, preSatisfiedNames);

                var creationMethod = new CreationMethod(
                    rootType.ContainingNamespace,
                    metadata,
                    allParamFields,
                    mergedValueSources,
                    createMethodName);

                _unreachableConstructorAnalyzer.AddReachableMethod(creationMethod);
                step.FluentMethods.Add(creationMethod);
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

    private static ImmutableArray<INamespaceSymbol> GetUsingStatements(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        return
        [
            ..fluentTargetContexts
                .SelectMany(ctx => ctx.Constructor.Parameters)
                .Select(parameter => parameter.Type.ContainingNamespace)
                .Concat(fluentTargetContexts.Select(ctx => ctx.Constructor.ContainingType.ContainingNamespace))
                .Where(namespaceSymbol => namespaceSymbol is not null)
                .Select(namespaceSymbol => (namespaceSymbol, displayString: namespaceSymbol.ToDisplayString()))
                .DistinctBy(ns => ns.displayString)
                .OrderBy(ns => ns.displayString)
                .Select(ns => ns.namespaceSymbol)
        ];
    }

}
