using System.Collections.Immutable;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.ModelBuilding;

/// <summary>
/// Post-processes fluent step chains to insert required and optional property initialization steps
/// between end steps and their terminal methods.
/// </summary>
internal static class PropertyStepEnricher
{
    /// <summary>
    /// Post-processes the step chain to insert required property steps between end steps
    /// and their terminal methods. For each terminal method whose target type has required
    /// properties, removes the terminal method from its current step, creates a chain of
    /// property steps, and places the terminal method on the last property step.
    /// Also handles root-level terminal methods (when all constructor params are pre-satisfied).
    /// </summary>
    public static (ImmutableArray<IFluentStep> NewSteps, ImmutableArray<IFluentMethod> UpdatedRootMethods)
        InsertRequiredPropertySteps(
            INamedTypeSymbol rootType,
            ImmutableArray<IFluentMethod> rootMethods,
            ImmutableArray<IFluentStep> existingSteps)
    {
        var newSteps = new List<IFluentStep>();
        var nextStepIndex = existingSteps.Length;

        // Process step-level terminal methods
        foreach (var step in existingSteps)
        {
            ProcessTerminalMethodsOnStep(rootType, step, step.FluentMethods, step.ValueStorage,
                step.KnownConstructorParameters, step.Accessibility,
                step is RegularFluentStep rfs ? rfs.TypeKind : TypeKind.Class,
                step is RegularFluentStep rfs2 ? rfs2.PropertyFieldStorage : ImmutableArray<FieldStorage>.Empty,
                step.CandidateTargets,
                newSteps, ref nextStepIndex);
        }

        // Process root-level terminal methods (0-param constructors with pre-satisfied params)
        var updatedRootMethods = ProcessRootLevelTerminalMethods(
            rootType, rootMethods, newSteps, ref nextStepIndex);

        return ([..newSteps], updatedRootMethods);
    }

    /// <summary>
    /// Processes terminal methods on a step, replacing them with property step chains
    /// when the target type has required properties.
    /// </summary>
    private static void ProcessTerminalMethodsOnStep(
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
        var terminalMethods = methods.OfType<TerminalMethod>()
            .Where(cm => cm.Return is TargetTypeReturn)
            .ToList();

        foreach (var terminalMethod in terminalMethods)
        {
            var requiredProperties = GetRequiredPropertiesForTerminalMethod(terminalMethod);
            if (requiredProperties.Count > 0)
            {
                methods.Remove(terminalMethod);

                BuildPropertyStepChain(
                    rootType, terminalMethod, requiredProperties,
                    ownerStep, valueStorage, knownConstructorParameters,
                    accessibility, typeKind, propertyFieldStorage, candidateConstructors,
                    newSteps, ref nextStepIndex);
                continue;
            }

            // Handle optional [FluentMethod] properties when there are no required properties
            var optionalProperties = GetOptionalFluentMethodProperties(terminalMethod);
            if (optionalProperties.Count > 0 && ownerStep is RegularFluentStep regularStep)
            {
                AddOptionalPropertyMethodsToStep(
                    rootType, regularStep, terminalMethod, optionalProperties, "With");
            }
        }
    }

    /// <summary>
    /// Processes root-level terminal methods, replacing them with property methods
    /// that lead to property step chains.
    /// </summary>
    private static ImmutableArray<IFluentMethod> ProcessRootLevelTerminalMethods(
        INamedTypeSymbol rootType,
        ImmutableArray<IFluentMethod> rootMethods,
        List<IFluentStep> newSteps,
        ref int nextStepIndex)
    {
        var rootTerminalMethods = rootMethods.OfType<TerminalMethod>()
            .Where(cm => cm.Return is TargetTypeReturn)
            .ToList();

        var methodsToRemove = new List<TerminalMethod>();
        var methodsToAdd = new List<IFluentMethod>();

        foreach (var terminalMethod in rootTerminalMethods)
        {
            var requiredProperties = GetRequiredPropertiesForTerminalMethod(terminalMethod);
            if (requiredProperties.Count == 0) continue;

            methodsToRemove.Add(terminalMethod);

            var emptyValueStorage = new OrderedDictionary<IParameterSymbol, IFluentValueStorage>();
            foreach (var kvp in terminalMethod.ValueSources)
                emptyValueStorage.Add(kvp.Key, kvp.Value);

            var candidateConstructors = terminalMethod.Return.CandidateTargets;

            // Create the first property step and entry method
            var firstProperty = requiredProperties[0];
            var methodPrefix = "With";
            var propMethodName = GetPropertyMethodName(firstProperty, methodPrefix);

            var firstStep = CreatePropertyStep(
                rootType, emptyValueStorage, [],
                ImmutableArray<FieldStorage>.Empty, firstProperty,
                candidateConstructors, rootType.DeclaredAccessibility, TypeKind.Class,
                ref nextStepIndex);

            var firstMethod = new RegularMethod(
                propMethodName,
                firstProperty,
                firstStep,
                rootType.ContainingNamespace,
                [],
                emptyValueStorage);

            methodsToAdd.Add(firstMethod);
            newSteps.Add(firstStep);

            if (requiredProperties.Count == 1)
            {
                // Single property: finalize directly
                firstStep.IsEndStep = true;
                terminalMethod.PropertyInitializers =
                [
                    ..requiredProperties.Select(rp =>
                        (PropertyName: rp.Name, FieldName: rp.Name.ToParameterFieldName()))
                ];
                firstStep.FluentMethods = [terminalMethod];

                var optionalProperties = GetOptionalFluentMethodProperties(terminalMethod);
                if (optionalProperties.Count > 0)
                {
                    AddOptionalPropertyMethodsToStep(
                        rootType, firstStep, terminalMethod, optionalProperties, "With");
                }
            }
            else
            {
                // Multiple properties: delegate remaining chain to shared builder
                BuildPropertyStepChain(
                    rootType, terminalMethod, requiredProperties,
                    firstStep, firstStep.ValueStorage, [],
                    rootType.DeclaredAccessibility, TypeKind.Class,
                    firstStep.PropertyFieldStorage, candidateConstructors,
                    newSteps, ref nextStepIndex,
                    startIndex: 1);
            }
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
    /// Gets the required properties for a terminal method's target type.
    /// </summary>
    private static List<IPropertySymbol> GetRequiredPropertiesForTerminalMethod(TerminalMethod terminalMethod)
    {
        var constructor = terminalMethod.Return.CandidateTargets.FirstOrDefault();
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
    /// Gets optional [FluentMethod] properties for a terminal method's target type.
    /// These are non-required properties opted in via [FluentMethod] attribute.
    /// </summary>
    private static List<IPropertySymbol> GetOptionalFluentMethodProperties(TerminalMethod terminalMethod)
    {
        var constructor = terminalMethod.Return.CandidateTargets.FirstOrDefault();
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
    /// Adds optional [FluentMethod] property methods to a step that contains a terminal method.
    /// </summary>
    private static void AddOptionalPropertyMethodsToStep(
        INamedTypeSymbol rootType,
        RegularFluentStep step,
        TerminalMethod terminalMethod,
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

        // Add optional properties to the terminal method's property initializers
        terminalMethod.PropertyInitializers =
        [
            ..terminalMethod.PropertyInitializers,
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
    /// to the owner step and adding the terminal method to the last step.
    /// </summary>
    private static void BuildPropertyStepChain(
        INamedTypeSymbol rootType,
        TerminalMethod terminalMethod,
        List<IPropertySymbol> requiredProperties,
        IFluentStep ownerStep,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorage,
        ParameterSequence knownConstructorParameters,
        Accessibility accessibility,
        TypeKind typeKind,
        ImmutableArray<FieldStorage> propertyFieldStorage,
        ImmutableArray<IMethodSymbol> candidateConstructors,
        List<IFluentStep> newSteps,
        ref int nextStepIndex,
        int startIndex = 0)
    {
        var methodPrefix = "With";
        var currentStep = ownerStep;
        var currentValueStorage = valueStorage;

        for (var i = startIndex; i < requiredProperties.Count; i++)
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
                terminalMethod.PropertyInitializers =
                [
                    ..requiredProperties.Select(rp =>
                        (PropertyName: rp.Name, FieldName: rp.Name.ToParameterFieldName()))
                ];
                propertyStep.FluentMethods = [terminalMethod];

                // Add optional [FluentMethod] property methods to the last step
                var optionalProperties = GetOptionalFluentMethodProperties(terminalMethod);
                if (optionalProperties.Count > 0)
                {
                    AddOptionalPropertyMethodsToStep(
                        rootType, propertyStep, terminalMethod, optionalProperties, methodPrefix);
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
}
