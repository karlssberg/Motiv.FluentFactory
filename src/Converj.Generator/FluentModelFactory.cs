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
        ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        _regularFluentSteps.Clear();
        _diagnostics.Clear();
        _unreachableConstructorAnalyzer.Clear();

        var (validContexts, unsupportedModifierDiagnostics) =
            FilterUnsupportedParameterModifierConstructors(fluentConstructorContexts);

        _diagnostics.AddRange(unsupportedModifierDiagnostics);

        var (accessibleContexts, inaccessibleConstructorDiagnostics) =
            FilterInaccessibleConstructors(validContexts);

        _diagnostics.AddRange(inaccessibleConstructorDiagnostics);
        validContexts = accessibleContexts;

        validContexts = FilterErrorTypeConstructors(validContexts);

        if (validContexts.IsEmpty)
            return new FluentFactoryCompilationUnit(rootType) { Diagnostics = _diagnostics };

        fluentConstructorContexts = validContexts;

        _unreachableConstructorAnalyzer.AddAllFluentConstructors(fluentConstructorContexts.Select(context => context.Constructor));
        _methodSelector = new FluentMethodSelector(compilation, _diagnostics, _unreachableConstructorAnalyzer);

        _stepBuilder = new FluentStepBuilder(_regularFluentSteps, _diagnostics);
        _rootType = rootType;

        var fluentParameterMembers = FluentParameterAnalyzer.Analyze(rootType, _diagnostics);
        _threadedParameters = CreateThreadedParameterBindings(rootType, fluentParameterMembers, fluentConstructorContexts);

        var usings = GetUsingStatements(fluentConstructorContexts);

        _diagnostics.AddRange(fluentConstructorContexts.GetDiagnostics());

        // Collect property analysis diagnostics
        foreach (var context in fluentConstructorContexts)
            _diagnostics.AddRange(context.PropertyDiagnostics);

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

        if (!_threadedParameters.IsEmpty)
        {
            PropagateThreadedParametersToSteps(fluentBuilderSteps);
        }

        // Handle multi-param all-optional constructors via post-processing
        var (gatewayMethods, gatewaySteps) = CreateAllOptionalStepsAndGatewayMethods(
            rootType, stepTrie.Root, fluentBuilderSteps.Length);

        fluentRootMethods = [..fluentRootMethods, ..gatewayMethods];
        fluentBuilderSteps = [..fluentBuilderSteps, ..gatewaySteps];

        // Post-process: insert required property steps between end steps and creation methods
        var (propertySteps, updatedRootMethods) = InsertRequiredPropertySteps(rootType, fluentRootMethods, fluentBuilderSteps);
        if (propertySteps.Length > 0)
        {
            if (!_threadedParameters.IsEmpty)
                PropagateThreadedParametersToSteps(propertySteps);

            fluentBuilderSteps = [..fluentBuilderSteps, ..propertySteps];
        }
        fluentRootMethods = updatedRootMethods;

        _diagnostics.AddRange(_unreachableConstructorAnalyzer.GetUnreachableConstructorsDiagnostics());
        var sampleConstructorContext = fluentConstructorContexts.First();

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

    /// <summary>
    /// Post-processes the step chain to insert required property steps between end steps
    /// and their creation methods. For each creation method whose target type has required
    /// properties, removes the creation method from its current step, creates a chain of
    /// property steps, and places the creation method on the last property step.
    /// Also handles root-level creation methods (when all constructor params are pre-satisfied).
    /// </summary>
    private (ImmutableArray<IFluentStep> NewSteps, ImmutableArray<IFluentMethod> UpdatedRootMethods)
        InsertRequiredPropertySteps(
            INamedTypeSymbol rootType,
            ImmutableArray<IFluentMethod> rootMethods,
            ImmutableArray<IFluentStep> existingSteps)
    {
        var newSteps = new List<IFluentStep>();
        var nextStepIndex = existingSteps.Length;

        // Process step-level creation methods
        foreach (var step in existingSteps)
        {
            ProcessCreationMethodsOnStep(rootType, step, step.FluentMethods, step.ValueStorage,
                step.KnownConstructorParameters, step.Accessibility,
                step is RegularFluentStep rfs ? rfs.TypeKind : TypeKind.Class,
                step is RegularFluentStep rfs2 ? rfs2.PropertyFieldStorage : ImmutableArray<FieldStorage>.Empty,
                step.CandidateConstructors,
                newSteps, ref nextStepIndex);
        }

        // Process root-level creation methods (0-param constructors with pre-satisfied params)
        var updatedRootMethods = ProcessRootLevelCreationMethods(
            rootType, rootMethods, newSteps, ref nextStepIndex);

        return ([..newSteps], updatedRootMethods);
    }

    /// <summary>
    /// Processes creation methods on a step, replacing them with property step chains
    /// when the target type has required properties.
    /// </summary>
    private static void ProcessCreationMethodsOnStep(
        INamedTypeSymbol rootType,
        IFluentStep ownerStep,
        IList<IFluentMethod> methods,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorage,
        ParameterSequence knownConstructorParameters,
        Accessibility accessibility,
        TypeKind typeKind,
        ImmutableArray<FieldStorage> propertyFieldStorage,
        ImmutableArray<IMethodSymbol> candidateConstructors,
        List<IFluentStep> newSteps,
        ref int nextStepIndex)
    {
        var creationMethods = methods.OfType<CreationMethod>()
            .Where(cm => cm.Return is TargetTypeReturn)
            .ToList();

        foreach (var creationMethod in creationMethods)
        {
            var requiredProperties = GetRequiredPropertiesForCreationMethod(creationMethod);
            if (requiredProperties.Count > 0)
            {
                methods.Remove(creationMethod);

                BuildPropertyStepChain(
                    rootType, creationMethod, requiredProperties,
                    ownerStep, valueStorage, knownConstructorParameters,
                    accessibility, typeKind, propertyFieldStorage, candidateConstructors,
                    newSteps, ref nextStepIndex);
                continue;
            }

            // Handle optional [FluentMethod] properties when there are no required properties
            var optionalProperties = GetOptionalFluentMethodProperties(creationMethod);
            if (optionalProperties.Count > 0 && ownerStep is RegularFluentStep regularStep)
            {
                AddOptionalPropertyMethodsToStep(
                    rootType, regularStep, creationMethod, optionalProperties, "With");
            }
        }
    }

    /// <summary>
    /// Processes root-level creation methods, replacing them with property methods
    /// that lead to property step chains.
    /// </summary>
    private ImmutableArray<IFluentMethod> ProcessRootLevelCreationMethods(
        INamedTypeSymbol rootType,
        ImmutableArray<IFluentMethod> rootMethods,
        List<IFluentStep> newSteps,
        ref int nextStepIndex)
    {
        var rootCreationMethods = rootMethods.OfType<CreationMethod>()
            .Where(cm => cm.Return is TargetTypeReturn)
            .ToList();

        var methodsToRemove = new List<CreationMethod>();
        var methodsToAdd = new List<IFluentMethod>();

        foreach (var creationMethod in rootCreationMethods)
        {
            var requiredProperties = GetRequiredPropertiesForCreationMethod(creationMethod);
            if (requiredProperties.Count == 0) continue;

            methodsToRemove.Add(creationMethod);

            var emptyValueStorage = new OrderedDictionary<IParameterSymbol, IFluentValueStorage>();
            foreach (var kvp in creationMethod.ValueSources)
                emptyValueStorage.Add(kvp.Key, kvp.Value);

            var candidateConstructors = creationMethod.Return.CandidateConstructors;

            // Build the property step chain starting from root
            var firstProperty = requiredProperties[0];
            var methodPrefix = "With";
            var propMethodName = GetPropertyMethodName(firstProperty, methodPrefix);

            // Create the first property step
            var firstStep = CreatePropertyStep(
                rootType, emptyValueStorage, [],
                ImmutableArray<FieldStorage>.Empty, firstProperty,
                candidateConstructors, rootType.DeclaredAccessibility, TypeKind.Class,
                ref nextStepIndex);

            // Wire creation method method → first step
            var firstMethod = new RegularMethod(
                propMethodName,
                firstProperty,
                firstStep,
                rootType.ContainingNamespace,
                [],
                emptyValueStorage);

            methodsToAdd.Add(firstMethod);

            // Build remaining property steps
            var currentStep = firstStep;
            for (var i = 1; i < requiredProperties.Count; i++)
            {
                var prop = requiredProperties[i];
                var nextPropMethodName = GetPropertyMethodName(prop, methodPrefix);
                var prevPropertyFields = currentStep.PropertyFieldStorage;

                var nextStep = CreatePropertyStep(
                    rootType, currentStep.ValueStorage, currentStep.KnownConstructorParameters,
                    prevPropertyFields, prop,
                    candidateConstructors, rootType.DeclaredAccessibility, TypeKind.Class,
                    ref nextStepIndex);

                var nextMethod = new RegularMethod(
                    nextPropMethodName,
                    prop,
                    nextStep,
                    rootType.ContainingNamespace,
                    [],
                    currentStep.ValueStorage);

                currentStep.FluentMethods.Add(nextMethod);
                newSteps.Add(nextStep);
                currentStep = nextStep;
            }

            // Put the creation method on the last property step
            var isFirstAndOnly = requiredProperties.Count == 1;
            firstStep.IsEndStep = isFirstAndOnly;
            currentStep.IsEndStep = true;

            creationMethod.PropertyInitializers =
            [
                ..requiredProperties.Select(rp =>
                    (PropertyName: rp.Name, FieldName: rp.Name.ToParameterFieldName()))
            ];
            currentStep.FluentMethods = [creationMethod];

            // Add optional [FluentMethod] property methods to the last step
            var optionalProperties = GetOptionalFluentMethodProperties(creationMethod);
            if (optionalProperties.Count > 0)
            {
                AddOptionalPropertyMethodsToStep(
                    rootType, currentStep, creationMethod, optionalProperties, "With");
            }

            newSteps.Insert(newSteps.Count - (requiredProperties.Count - 1), firstStep);
        }

        if (methodsToRemove.Count == 0)
            return rootMethods;

        return
        [
            ..rootMethods
                .Where(m => !methodsToRemove.Contains(m))
                .Concat(methodsToAdd)
        ];
    }

    /// <summary>
    /// Gets the required properties for a creation method's target type.
    /// </summary>
    private static List<IPropertySymbol> GetRequiredPropertiesForCreationMethod(CreationMethod creationMethod)
    {
        var constructor = creationMethod.Return.CandidateConstructors.FirstOrDefault();
        if (constructor is null) return [];

        var targetType = constructor.ContainingType;

        return targetType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && !p.IsIndexer)
            .Where(p => p.IsRequired || p.HasAttribute(TypeName.RequiredAttribute))
            .Where(p => p.SetMethod is not null)
            .Where(p => !IsPropertyInitializedByConstructor(constructor, targetType, p))
            .ToList();
    }

    /// <summary>
    /// Gets optional [FluentMethod] properties for a creation method's target type.
    /// These are non-required properties opted in via [FluentMethod] attribute.
    /// </summary>
    private static List<IPropertySymbol> GetOptionalFluentMethodProperties(CreationMethod creationMethod)
    {
        var constructor = creationMethod.Return.CandidateConstructors.FirstOrDefault();
        if (constructor is null) return [];

        var targetType = constructor.ContainingType;

        return targetType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && !p.IsIndexer)
            .Where(p => !p.IsRequired && !p.HasAttribute(TypeName.RequiredAttribute))
            .Where(p => p.HasAttribute(TypeName.FluentMethodAttribute))
            .Where(p => p.SetMethod is not null)
            .Where(p => !IsPropertyInitializedByConstructor(constructor, targetType, p))
            .ToList();
    }

    /// <summary>
    /// Adds optional [FluentMethod] property methods to a step that contains a creation method.
    /// </summary>
    private static void AddOptionalPropertyMethodsToStep(
        INamedTypeSymbol rootType,
        RegularFluentStep step,
        CreationMethod creationMethod,
        List<IPropertySymbol> optionalProperties,
        string methodPrefix)
    {
        var optionalFieldStorages = new List<FieldStorage>();

        foreach (var prop in optionalProperties)
        {
            var fieldName = prop.Name.ToParameterFieldName();
            var fieldStorage = new FieldStorage(fieldName, prop.Type, rootType.ContainingNamespace)
                { IsReadOnly = false };
            optionalFieldStorages.Add(fieldStorage);

            var propMethodName = GetPropertyMethodName(prop, methodPrefix);
            var optionalMethod = new OptionalPropertyFluentMethod(
                propMethodName,
                prop,
                fieldStorage,
                step,
                rootType.ContainingNamespace);

            step.FluentMethods.Add(optionalMethod);
        }

        step.OptionalPropertyFieldStorage = [..optionalFieldStorages];

        // Add optional properties to the creation method's property initializers
        creationMethod.PropertyInitializers =
        [
            ..creationMethod.PropertyInitializers,
            ..optionalProperties.Select(p =>
                (PropertyName: p.Name, FieldName: p.Name.ToParameterFieldName()))
        ];
    }

    /// <summary>
    /// Gets the fluent method name for a property, checking for [FluentMethod] rename.
    /// </summary>
    private static string GetPropertyMethodName(IPropertySymbol property, string methodPrefix)
    {
        var fluentMethodAttr = property.GetAttributes(TypeName.FluentMethodAttribute).FirstOrDefault();
        if (fluentMethodAttr is not null)
        {
            var explicitName = fluentMethodAttr.GetFirstStringArgument();
            if (explicitName is not null)
                return explicitName;
        }

        return $"{methodPrefix}{property.Name.Capitalize()}";
    }

    /// <summary>
    /// Creates a property step with the given configuration.
    /// </summary>
    private static RegularFluentStep CreatePropertyStep(
        INamedTypeSymbol rootType,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> sourceValueStorage,
        ParameterSequence knownConstructorParameters,
        ImmutableArray<FieldStorage> prevPropertyFields,
        IPropertySymbol property,
        ImmutableArray<IMethodSymbol> candidateConstructors,
        Accessibility accessibility,
        TypeKind typeKind,
        ref int nextStepIndex)
    {
        var propFieldName = property.Name.ToParameterFieldName();
        var thisPropField = new FieldStorage(
            propFieldName, property.Type, rootType.ContainingNamespace);
        var allPropertyFields = prevPropertyFields.Add(thisPropField);

        var propertyStep = new RegularFluentStep(rootType, candidateConstructors)
        {
            Index = nextStepIndex++,
            IsEndStep = false,
            Accessibility = accessibility,
            TypeKind = typeKind,
        };

        var newStorage = new OrderedDictionary<IParameterSymbol, IFluentValueStorage>();
        foreach (var kvp in sourceValueStorage)
            newStorage.Add(kvp.Key, kvp.Value);

        propertyStep.KnownConstructorParameters = knownConstructorParameters;
        propertyStep.ValueStorage = newStorage;
        propertyStep.PropertyFieldStorage = allPropertyFields;

        return propertyStep;
    }

    /// <summary>
    /// Builds a chain of property steps for required properties, attaching them
    /// to the owner step and adding the creation method to the last step.
    /// </summary>
    private static void BuildPropertyStepChain(
        INamedTypeSymbol rootType,
        CreationMethod creationMethod,
        List<IPropertySymbol> requiredProperties,
        IFluentStep ownerStep,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorage,
        ParameterSequence knownConstructorParameters,
        Accessibility accessibility,
        TypeKind typeKind,
        ImmutableArray<FieldStorage> propertyFieldStorage,
        ImmutableArray<IMethodSymbol> candidateConstructors,
        List<IFluentStep> newSteps,
        ref int nextStepIndex)
    {
        var methodPrefix = "With";
        var currentStep = ownerStep;
        var currentValueStorage = valueStorage;

        for (var i = 0; i < requiredProperties.Count; i++)
        {
            var prop = requiredProperties[i];
            var isLast = i == requiredProperties.Count - 1;
            var propMethodName = GetPropertyMethodName(prop, methodPrefix);

            var prevPropertyFields = currentStep is RegularFluentStep rfs2
                ? rfs2.PropertyFieldStorage
                : propertyFieldStorage;

            var propertyStep = CreatePropertyStep(
                rootType, currentValueStorage, knownConstructorParameters,
                prevPropertyFields, prop,
                candidateConstructors, accessibility, typeKind,
                ref nextStepIndex);

            propertyStep.IsEndStep = isLast;

            var method = new RegularMethod(
                propMethodName,
                prop,
                propertyStep,
                rootType.ContainingNamespace,
                [],
                currentValueStorage);

            currentStep.FluentMethods.Add(method);

            if (isLast)
            {
                creationMethod.PropertyInitializers =
                [
                    ..requiredProperties.Select(rp =>
                        (PropertyName: rp.Name, FieldName: rp.Name.ToParameterFieldName()))
                ];
                propertyStep.FluentMethods = [creationMethod];

                // Add optional [FluentMethod] property methods to the last step
                var optionalProperties = GetOptionalFluentMethodProperties(creationMethod);
                if (optionalProperties.Count > 0)
                {
                    AddOptionalPropertyMethodsToStep(
                        rootType, propertyStep, creationMethod, optionalProperties, methodPrefix);
                }
            }

            newSteps.Add(propertyStep);
            currentStep = propertyStep;
            currentValueStorage = propertyStep.ValueStorage;
        }
    }

    /// <summary>
    /// Checks whether a property is initialized by the given constructor.
    /// </summary>
    private static bool IsPropertyInitializedByConstructor(
        IMethodSymbol constructor,
        INamedTypeSymbol targetType,
        IPropertySymbol property)
    {
        // Record types: constructor params create properties
        if (targetType.IsRecord)
        {
            foreach (var param in constructor.Parameters)
            {
                if (string.Equals(param.Name, property.Name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        // Check explicit assignments in constructor body
        var syntaxRef = constructor.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef?.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax ctorSyntax
            && ctorSyntax.Body is not null)
        {
            var paramNames = new HashSet<string>(constructor.Parameters.Select(p => p.Name));
            foreach (var statement in ctorSyntax.Body.Statements)
            {
                if (statement is not Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax
                    {
                        Expression: Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax assignment
                    }) continue;

                var assignedName = assignment.Left switch
                {
                    Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax
                        { Expression: Microsoft.CodeAnalysis.CSharp.Syntax.ThisExpressionSyntax, Name: var name } =>
                        name.Identifier.ValueText,
                    Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax id => id.Identifier.ValueText,
                    _ => null
                };

                if (string.Equals(assignedName, property.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var rightName = assignment.Right switch
                    {
                        Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax id => id.Identifier.ValueText,
                        _ => null
                    };
                    if (rightName is not null && paramNames.Contains(rightName))
                        return true;
                }
            }
        }

        return false;
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
            where node.Key.Length >= value.RequiredParameterCount - preSatisfiedNames.Count
            where node.Key.Length <= value.Constructor.Parameters.Length - preSatisfiedNames.Count
            where value.CreateMethod != CreateMethodMode.None
            let verb = value.Context.CreateVerb ?? "Create"
            let methodName = value.CreateMethod == CreateMethodMode.Fixed
                ? verb
                : $"{verb}{value.Constructor.ContainingType.ToCreateMethodSuffix()}"
            let keyParamFieldNames = new HashSet<string>(
                node.Key.Select(k => k.SourceName.ToParameterFieldName()))
            let optionalParamFields = value.OptionalParameters
                .Where(p => !keyParamFieldNames.Contains(p.Name.ToParameterFieldName()))
                .Select(p => new FluentMethodParameter(p, p.GetFluentMethodName(methodPrefix)))
            let hasStep = node.Key.Length > 0
            let threadedParamFields = GetThreadedParameterFields(value.Constructor, preSatisfiedNames)
            let allParameterFields = hasStep
                ? threadedParamFields.AddRange(node.Key).AddRange(optionalParamFields)
                : threadedParamFields.AddRange(node.Key)
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
                .Select(p => new FluentMethodParameter(p, p.Name))
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
    /// Creates bindings between [FluentParameter] members and target constructor parameters.
    /// </summary>
    private ImmutableArray<FluentParameterBinding> CreateThreadedParameterBindings(
        INamedTypeSymbol rootType,
        ImmutableArray<FluentParameterMember> fluentParameterMembers,
        ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        if (fluentParameterMembers.IsEmpty)
            return [];

        var bindings = ImmutableArray.CreateBuilder<FluentParameterBinding>();

        var allTargetParams = fluentConstructorContexts
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


    private Trie<FluentMethodParameter, ConstructorMetadata> CreateFluentStepTrie(
        ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        var trie = new Trie<FluentMethodParameter, ConstructorMetadata>();
        foreach (var constructorContext in fluentConstructorContexts)
        {
            var methodPrefix = constructorContext.MethodPrefix ?? "With";

            FluentMethodParameter ToFluentMethodParameter(IParameterSymbol parameter)
            {
                var methodNames = compilation
                    .GetMultipleFluentMethodSymbols(parameter)
                    .Select(methodInfo => methodInfo.Method.Name)
                    .DefaultIfEmpty(parameter.GetFluentMethodName(methodPrefix));

                return new FluentMethodParameter(parameter, methodNames);
            }

            var preSatisfiedNames = IsSelfReferencing(constructorContext.Constructor, _rootType)
                ? new HashSet<string>()
                : GetPreSatisfiedParameterNames(constructorContext.Constructor);

            var requiredParameters = constructorContext.Constructor.Parameters
                .Where(p => !p.HasExplicitDefaultValue)
                .Where(p => !preSatisfiedNames.Contains(p.Name))
                .Select(ToFluentMethodParameter);

            var metadata = new ConstructorMetadata(constructorContext);

            // Always insert the required-only path (marks root as end when all params are optional)
            trie.Insert(requiredParameters, metadata);

            // When all params are optional with exactly one param, insert it as a trie path
            // so that step methods are generated (e.g., Animal.WithLegs(2).CreateDog())
            // Multi-param all-optional constructors are handled by CreateAllOptionalStepsAndGatewayMethods
            if (metadata.RequiredParameterCount == 0 && metadata.OptionalParameters.Length == 1)
            {
                var optionalParameters = constructorContext.Constructor.Parameters
                    .Where(p => p.HasExplicitDefaultValue)
                    .Select(ToFluentMethodParameter);

                trie.Insert(optionalParameters, metadata.Clone());
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
        var allOptionalConstructors = rootNode.EndValues
            .Where(v => v.RequiredParameterCount == 0 && v.OptionalParameters.Length > 1)
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
                if (metadata.CreateMethod == CreateMethodMode.None) continue;

                var verb = metadata.Context.CreateVerb ?? "Create";
                var createMethodName = metadata.CreateMethod == CreateMethodMode.Fixed
                    ? verb
                    : $"{verb}{metadata.Constructor.ContainingType.ToCreateMethodSuffix()}";

                var allParamFields = allOptionalParams
                    .Select(p => new FluentMethodParameter(p, p.GetFluentMethodName(methodPrefix)))
                    .ToImmutableArray();

                var creationMethod = new CreationMethod(
                    rootType.ContainingNamespace,
                    metadata,
                    allParamFields,
                    valueStorages,
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

        return ([..gatewayMethods], [..steps.OfType<IFluentStep>()]);
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
                .Where(namespaceSymbol => namespaceSymbol is not null)
                .Select(namespaceSymbol => (namespaceSymbol, displayString: namespaceSymbol.ToDisplayString()))
                .DistinctBy(ns => ns.displayString)
                .OrderBy(ns => ns.displayString)
                .Select(ns => ns.namespaceSymbol)
        ];
    }

    private static (ImmutableArray<FluentConstructorContext> Valid, IEnumerable<Diagnostic> Diagnostics)
        FilterInaccessibleConstructors(
            ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        var diagnostics = new List<Diagnostic>();
        var validContexts = ImmutableArray.CreateBuilder<FluentConstructorContext>(fluentConstructorContexts.Length);

        foreach (var context in fluentConstructorContexts)
        {
            var accessibility = context.Constructor.DeclaredAccessibility;
            var isInaccessible = accessibility is
                Accessibility.Private or
                Accessibility.Protected or
                Accessibility.ProtectedAndInternal;

            if (!isInaccessible)
            {
                validContexts.Add(context);
                continue;
            }

            var location = context.Constructor.Locations.FirstOrDefault() ?? Location.None;
            diagnostics.Add(Diagnostic.Create(
                FluentDiagnostics.InaccessibleConstructor,
                location,
                context.Constructor.ToDisplayString(),
                accessibility.ToString()));
        }

        return (validContexts.ToImmutable(), diagnostics);
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

    private static ImmutableArray<FluentConstructorContext> FilterErrorTypeConstructors(
        ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        return
        [
            ..fluentConstructorContexts
                .Where(ctx => ctx.Constructor.Parameters
                    .All(p => p.Type.TypeKind != TypeKind.Error))
        ];
    }
}
