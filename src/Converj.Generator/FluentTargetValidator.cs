using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.Diagnostics;
using Converj.Generator.Extensions;
using Converj.Generator.TargetAnalysis;

namespace Converj.Generator;

internal static class FluentTargetValidatorExtensions
{
    private const string DefaultTerminalVerb = "Create";

    public static IEnumerable<Diagnostic> GetDiagnostics(this ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        return ValidateRootTypeAttributes(fluentTargetContexts)
            .Concat(ValidateMissingPartialModifier(fluentTargetContexts))
            .Concat(ValidateFactoryDefaults(fluentTargetContexts))
            .Concat(ValidateTerminalVerb(fluentTargetContexts))
            .Concat(ValidateMethodPrefix(fluentTargetContexts))
            .Concat(ValidateDuplicateTerminalMethods(fluentTargetContexts))
            .Concat(ValidateTerminalVerbConflicts(fluentTargetContexts))
            .Concat(ValidateParameterTypeAccessibility(fluentTargetContexts))
            .Concat(ValidateAccessibilityMismatch(fluentTargetContexts))
            .Concat(ValidateAmbiguousFluentMethodChains(fluentTargetContexts))
            .Concat(ValidateOptionalParameterAmbiguousChains(fluentTargetContexts))
            .Concat(ValidateConflictingTypeConstraints(fluentTargetContexts))
            .Concat(ValidateReturnType(fluentTargetContexts))
            .Concat(ValidateMultipleTargetsWithBuilderNone(fluentTargetContexts))
            .Concat(ValidateFluentMethodNoEffectOnParameters(fluentTargetContexts))
            .Concat(ValidatePropertyWithBuilderNone(fluentTargetContexts));
    }

    private static IEnumerable<Diagnostic> ValidateMissingPartialModifier(ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var rootTypesWithoutPartial = fluentTargetContexts
            .GroupBy(context => context.RootType, SymbolEqualityComparer.Default)
            .Where(group => !HasPartialModifier(group.Key as INamedTypeSymbol));

        foreach (var group in rootTypesWithoutPartial)
        {
            var rootType = (INamedTypeSymbol)group.Key!;
            var location = rootType.Locations.FirstOrDefault() ?? Location.None;
            yield return Diagnostic.Create(
                FluentDiagnostics.MissingPartialModifier,
                location,
                rootType.ToDisplayString());
        }
    }

    private static bool HasPartialModifier(INamedTypeSymbol? rootType)
    {
        if (rootType is null) return false;

        return rootType.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private static IEnumerable<Diagnostic> ValidateFactoryDefaults(ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var rootTypes = fluentTargetContexts
            .Select(context => context.RootType)
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var rootType in rootTypes)
        {
            var factoryAttribute = rootType.GetAttributes(TypeName.FluentRootAttribute).FirstOrDefault();
            if (factoryAttribute is null)
                continue;

            var defaults = FluentRootMetadataReader.GetFluentRootDefaults(rootType);
            var location = GetAttributeLocation(factoryAttribute);

            switch (defaults)
            {
                case { TerminalVerb: not null }
                        when !IsValidTerminalVerbForMode(defaults.TerminalVerb, defaults.TerminalMethod ?? TerminalMethodKind.DynamicSuffix):
                    yield return Diagnostic.Create(FluentDiagnostics.InvalidTerminalVerb, location);
                    break;

                case { TerminalMethod: TerminalMethodKind.None,  TerminalVerb.Length: > 0 }:
                    yield return Diagnostic.Create(FluentDiagnostics.TerminalVerbWithNone, location);
                    break;

                case { TerminalMethod: TerminalMethodKind.None, TerminalVerb: "" }:
                    yield return Diagnostic.Create(FluentDiagnostics.EmptyTerminalVerbWithNone, location);
                    break;
            }

            if (defaults.MethodPrefix is not null && !IsValidMethodPrefix(defaults.MethodPrefix))
                yield return Diagnostic.Create(FluentDiagnostics.InvalidMethodPrefix, location);

            if (defaults is { TerminalMethod: TerminalMethodKind.None, ReturnType: not null })
                yield return Diagnostic.Create(FluentDiagnostics.ReturnTypeWithNone,
                    FindNamedArgumentLocation(factoryAttribute, "ReturnType"));
        }
    }

    private static Location GetAttributeLocation(AttributeData attributeData) =>
        attributeData.ApplicationSyntaxReference?.GetSyntax() switch
        {
            AttributeSyntax attributeSyntax => attributeSyntax.GetLocation(),
            _ => Location.None,
        };

    private static IEnumerable<Diagnostic> ValidateRootTypeAttributes(ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        // Check if the target type has the FluentRoot attribute
        var constructorContexts = fluentTargetContexts
            .Where(context => !context.RootType.HasAttribute(TypeName.FluentRootAttribute));

        foreach (var context in constructorContexts)
            yield return Diagnostic.Create(
                FluentDiagnostics.FluentTargetTypeMissingFluentRoot,
                FindRootTypeLocation(context.AttributeData, context),
                context.RootType.ToDisplayString());
    }

    private static IEnumerable<Diagnostic> ValidateTerminalVerb(ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        // Get contexts that have duplicates - we'll skip CVJG0007 for these since CVJG0008 will be reported
        var duplicateContexts = new HashSet<FluentTargetContext>(GetDuplicateTargetContexts(fluentTargetContexts)
            .SelectMany(group => group));

        // Only validate constructors that explicitly set TerminalVerb (not inherited from factory)
        var constructorContextWithInvalidTerminalVerb = fluentTargetContexts
            .Except(duplicateContexts)
            .Where(HasExplicitTerminalVerb)
            .Where(context => !IsValidTerminalVerbForMode(context.TerminalVerb, context.TerminalMethod));

        foreach (var context in constructorContextWithInvalidTerminalVerb)
        {
            yield return Diagnostic.Create(
                FluentDiagnostics.InvalidTerminalVerb,
                FindTerminalVerbArgumentLocation(context));
        }
    }

    private static IEnumerable<Diagnostic> ValidateMethodPrefix(ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var constructorsWithInvalidPrefix = fluentTargetContexts
            .Where(HasExplicitMethodPrefix)
            .Where(context => !IsValidMethodPrefix(context.MethodPrefix));

        foreach (var context in constructorsWithInvalidPrefix)
        {
            yield return Diagnostic.Create(
                FluentDiagnostics.InvalidMethodPrefix,
                FindMethodPrefixArgumentLocation(context));
        }
    }

    private static bool IsValidMethodPrefix(string? prefix)
    {
        if (prefix is null or { Length: 0 })
            return true;

        return char.IsLetter(prefix[0]) && prefix.Skip(1).All(char.IsLetterOrDigit);
    }

    private static bool IsValidTerminalVerbForMode(string? verb, TerminalMethodKind mode)
    {
        if (verb is { Length: 0 } && mode is TerminalMethodKind.DynamicSuffix or TerminalMethodKind.None)
            return true;

        return IsValidTerminalVerb(verb);
    }

    private static bool IsValidTerminalVerb(string? verb)
    {
        if (verb is null)
            return true;

        if (verb.Length == 0)
            return false;

        return char.IsLetter(verb[0]) && verb.Skip(1).All(char.IsLetterOrDigit);
    }

    private static IEnumerable<Diagnostic> ValidateDuplicateTerminalMethods(ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        // Check for duplicate resolved create method names within the same type
        var duplicateGroups = GetDuplicateTargetContexts(fluentTargetContexts);

        foreach (var group in duplicateGroups)
        {

            var contexts = group.ToList();
            var primaryLocation = FindTerminalVerbArgumentLocation(contexts[0]);
            var additionalLocations = contexts
                .Skip(1)
                .Select(FindTerminalVerbArgumentLocation);

            yield return Diagnostic.Create(
                FluentDiagnostics.DuplicateTerminalMethodName,
                primaryLocation,
                additionalLocations: additionalLocations);
        }
    }

    private static IEnumerable<IGrouping<(string ResolvedName, string TypeName), FluentTargetContext>> GetDuplicateTargetContexts(
        ImmutableArray<FluentTargetContext> fluentTargetContexts) =>
        fluentTargetContexts
            .Where(context => !string.IsNullOrEmpty(context.TerminalVerb))
            .Select(context => (Context: context, ResolvedName: ResolveTerminalMethodName(context)))
            .GroupBy(x => (x.ResolvedName, TypeName: x.Context.Constructor.ContainingType.ToDisplayString()), x => x.Context)
            .Where(group => group.Count() > 1);

    private static string ResolveTerminalMethodName(FluentTargetContext context)
    {
        var verb = context.TerminalVerb ?? DefaultTerminalVerb;
        return context.TerminalMethod switch
        {
            TerminalMethodKind.FixedName => verb,
            _ => $"{verb}{context.Constructor.ContainingType.ToCreateMethodSuffix()}"
        };
    }

    private static IEnumerable<Diagnostic> ValidateTerminalVerbConflicts(ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        // Check for TerminalMethod.None used with non-empty TerminalVerb, only when the constructor explicitly sets at least one
        var conflictedContexts = fluentTargetContexts.AsEnumerable().Where(context =>
            context.TerminalMethod == TerminalMethodKind.None
            && !string.IsNullOrEmpty(context.TerminalVerb)
            && (HasExplicitTerminalVerb(context) || HasExplicitBuilder(context)));

        foreach (var context in conflictedContexts)
        {
            yield return Diagnostic.Create(
                FluentDiagnostics.TerminalVerbWithNone,
                GetAttributeLocation(context.AttributeData));
        }

        // Empty TerminalVerb + None -> warning CVJG0017
        var emptyVerbWithNone = fluentTargetContexts.AsEnumerable().Where(context =>
            context.TerminalMethod == TerminalMethodKind.None
            && context.TerminalVerb is ""
            && HasExplicitTerminalVerb(context));

        foreach (var context in emptyVerbWithNone)
        {
            yield return Diagnostic.Create(
                FluentDiagnostics.EmptyTerminalVerbWithNone,
                FindTerminalVerbArgumentLocation(context));
        }
    }

    private static IEnumerable<Diagnostic> ValidateParameterTypeAccessibility(ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        foreach (var context in fluentTargetContexts)
        {
            var factoryAccessibility = context.RootType.DeclaredAccessibility;

            foreach (var parameter in context.Constructor.Parameters)
            {
                var paramType = parameter.Type;

                // Skip built-in types (string, int, etc.) -- SpecialType.None means it's a user-defined type
                if (paramType.SpecialType != SpecialType.None)
                    continue;

                // Skip type parameters -- their DeclaredAccessibility is NotApplicable
                if (paramType.DeclaredAccessibility == Accessibility.NotApplicable)
                    continue;

                if ((int)paramType.DeclaredAccessibility < (int)factoryAccessibility)
                {
                    var location = parameter.Locations.FirstOrDefault() ?? Location.None;
                    yield return Diagnostic.Create(
                        FluentDiagnostics.InaccessibleParameterType,
                        location,
                        parameter.Name,
                        paramType.ToDisplayString(),
                        context.Constructor.ToDisplayString(),
                        context.RootType.ToDisplayString());
                }
            }
        }
    }

    private static IEnumerable<Diagnostic> ValidateAccessibilityMismatch(ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        foreach (var context in fluentTargetContexts)
        {
            var targetType = context.Constructor.ContainingType;
            var factoryAccessibility = context.RootType.DeclaredAccessibility;
            var targetAccessibility = targetType.DeclaredAccessibility;

            if ((int)targetAccessibility < (int)factoryAccessibility)
            {
                var location = context.Constructor.Locations.FirstOrDefault() ?? Location.None;
                yield return Diagnostic.Create(
                    FluentDiagnostics.AccessibilityMismatch,
                    location,
                    context.RootType.ToDisplayString(),
                    factoryAccessibility.ToString(),
                    targetType.ToDisplayString(),
                    targetAccessibility.ToString());
            }
        }
    }

    private static bool HasExplicitTerminalVerb(FluentTargetContext context) =>
        context.AttributeData.NamedArguments.Any(namedArg => namedArg.Key == "TerminalVerb");

    private static bool HasExplicitBuilder(FluentTargetContext context) =>
        context.AttributeData.NamedArguments.Any(namedArg => namedArg.Key == "TerminalMethod");

    private static bool HasExplicitMethodPrefix(FluentTargetContext context) =>
        context.AttributeData.NamedArguments.Any(namedArg => namedArg.Key == "MethodPrefix");

    private static Location FindRootTypeLocation(AttributeData? fluentConstructorAttribute, FluentTargetContext context)
    {
        Location location;
        if (fluentConstructorAttribute?.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax attributeSyntax)
        {
            // Find the typeof argument that references the root type
            var typeofArg = attributeSyntax.ArgumentList?.Arguments
                .OfType<AttributeArgumentSyntax>()
                .FirstOrDefault(arg => arg.Expression is TypeOfExpressionSyntax);

            location = typeofArg != null
                ? typeofArg.Expression.GetLocation()
                : attributeSyntax.GetLocation();
        }
        else
        {
            location = context.Constructor.Locations.FirstOrDefault() ?? Location.None;
        }

        return location;
    }

    private static Location FindTerminalVerbArgumentLocation(FluentTargetContext context) =>
        FindNamedArgumentLocation(context, "TerminalVerb");

    private static Location FindMethodPrefixArgumentLocation(FluentTargetContext context) =>
        FindNamedArgumentLocation(context, "MethodPrefix");

    private static Location FindNamedArgumentLocation(FluentTargetContext context, string argumentName) =>
        FindNamedArgumentLocation(context.AttributeData, argumentName,
            context.Constructor.Locations.FirstOrDefault() ?? Location.None);

    private static Location FindNamedArgumentLocation(AttributeData attributeData, string argumentName,
        Location? fallback = null)
    {
        if (attributeData.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax attributeSyntax)
        {
            var namedArg = attributeSyntax.ArgumentList?.Arguments
                .OfType<AttributeArgumentSyntax>()
                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.ValueText == argumentName);

            return namedArg != null
                ? namedArg.GetLocation()
                : attributeSyntax.GetLocation();
        }

        return fallback ?? Location.None;
    }

    private static IEnumerable<Diagnostic> ValidateAmbiguousFluentMethodChains(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        // Skip constructors with [MultipleFluentMethods] parameters since their
        // fluent method names are resolved from template methods, not GetFluentMethodName()
        var eligibleContexts = fluentTargetContexts
            .Where(ctx => !ctx.Constructor.Parameters.Any(
                p => p.GetAttribute(TypeName.MultipleFluentMethodsAttribute) is not null));

        var ambiguousGroups = eligibleContexts
            .GroupBy(GetFluentParameterChainKey)
            .Where(HasAmbiguousCreateMethods);

        foreach (var group in ambiguousGroups)
        {
            var participatingTypes = group
                .Select(ctx => ctx.Constructor.ContainingType.ToDisplayString())
                .Distinct()
                .OrderBy(t => t);

            var typesString = string.Join(", ", participatingTypes);

            foreach (var context in group)
            {
                foreach (var parameter in context.Constructor.Parameters)
                {
                    var methodName = parameter.GetFluentMethodName(context.MethodPrefix ?? "With");
                    var location = FindFluentMethodOrParameterLocation(parameter);

                    yield return Diagnostic.Create(
                        FluentDiagnostics.AmbiguousFluentMethodChain,
                        location,
                        parameter.Name,
                        context.Constructor.ToDisplayString(),
                        methodName,
                        typesString);
                }
            }
        }
    }

    private static bool HasAmbiguousCreateMethods(IGrouping<string, FluentTargetContext> group)
    {
        // Must have constructors from at least 2 different types
        var distinctTypes = group
            .Select(ctx => ctx.Constructor.ContainingType)
            .Distinct(SymbolEqualityComparer.Default)
            .Count();

        if (distinctTypes <= 1)
            return false;

        // Separate constructors with None from those that produce Create methods
        var constructorsWithCreate = group
            .Where(ctx => ctx.TerminalMethod != TerminalMethodKind.None)
            .ToList();

        var constructorsWithNoCreate = group
            .Where(ctx => ctx.TerminalMethod == TerminalMethodKind.None)
            .ToList();

        // NoCreateMethod constructors use the containing type as the step, so at most one
        // distinct type can use NoCreateMethod at the same chain position
        var distinctNoCreateTypes = constructorsWithNoCreate
            .Select(ctx => ctx.Constructor.ContainingType)
            .Distinct(SymbolEqualityComparer.Default)
            .Count();

        if (distinctNoCreateTypes <= 1
            && constructorsWithNoCreate.Count > 0
            && AreCreateMethodsDisambiguated(constructorsWithCreate))
            return false;

        // Allow groups where all constructors have distinct resolved create method names
        if (AreCreateMethodsDisambiguated(group.ToList()))
            return false;

        return true;
    }

    private static bool AreCreateMethodsDisambiguated(List<FluentTargetContext> constructors)
    {
        // Zero or one constructor with Create methods cannot be ambiguous
        if (constructors.Count <= 1)
            return true;

        // Constructors with None don't produce create methods and can't be disambiguated by name
        var constructorsWithCreate = constructors
            .Where(ctx => ctx.TerminalMethod != TerminalMethodKind.None)
            .ToList();

        if (constructorsWithCreate.Count != constructors.Count)
            return false;

        // All constructors produce create methods -- check resolved names are distinct
        var resolvedNames = constructorsWithCreate
            .Select(ResolveTerminalMethodName)
            .ToList();

        var distinctNames = resolvedNames.Distinct().Count();

        return distinctNames == resolvedNames.Count;
    }

    private static string GetFluentParameterChainKey(FluentTargetContext context) =>
        string.Join("|", context.Constructor.Parameters
            .Select(p => $"{p.GetFluentMethodName(context.MethodPrefix ?? "With")}:{p.Type.ToDisplayString()}"));

    private static string GetFirstParameterKey(
        FluentTargetContext context, Func<ITypeSymbol, string> typeStringResolver)
    {
        if (context.Constructor.Parameters.Length == 0)
            return "";

        var p = context.Constructor.Parameters[0];
        return $"{p.GetFluentMethodName(context.MethodPrefix ?? "With")}:{typeStringResolver(p.Type)}";
    }

    private static IEnumerable<Diagnostic> ValidateConflictingTypeConstraints(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var eligibleContexts = fluentTargetContexts
            .Where(ctx => ctx.Constructor.Parameters.Length > 0)
            .Where(ctx => !ctx.Constructor.Parameters.Any(
                p => p.GetAttribute(TypeName.MultipleFluentMethodsAttribute) is not null));

        // Group by first parameter's C# method signature (ignoring generic constraints)
        var groups = eligibleContexts
            .GroupBy(ctx => GetFirstParameterKey(ctx, t => t.GetEffectiveSignatureString()));

        foreach (var group in groups)
        {
            var contexts = group.ToList();
            if (contexts.Count <= 1)
                continue;

            // Must have constructors from at least 2 different types
            var distinctTypes = contexts
                .Select(ctx => ctx.Constructor.ContainingType)
                .Distinct(SymbolEqualityComparer.Default)
                .Count();

            if (distinctTypes <= 1)
                continue;

            // Check if any constructors differ only by constraints on the first parameter
            var distinctConstraintKeys = contexts
                .Select(ctx => GetFirstParameterKey(ctx, t => t.GetEffectiveDisplayString()))
                .Distinct()
                .Count();

            if (distinctConstraintKeys <= 1)
                continue;

            var participatingTypes = contexts
                .Select(ctx => ctx.Constructor.ContainingType.ToDisplayString())
                .Distinct()
                .OrderBy(t => t);

            var typesString = string.Join(", ", participatingTypes);

            foreach (var context in contexts)
            {
                var parameter = context.Constructor.Parameters[0];
                var methodName = parameter.GetFluentMethodName(context.MethodPrefix ?? "With");
                var location = FindFluentMethodOrParameterLocation(parameter);

                yield return Diagnostic.Create(
                    FluentDiagnostics.ConflictingTypeConstraints,
                    location,
                    parameter.Name,
                    context.Constructor.ToDisplayString(),
                    methodName,
                    typesString);
            }
        }
    }

    private static Location FindFluentMethodOrParameterLocation(IParameterSymbol parameter)
    {
        var fluentMethodAttribute = parameter.GetAttribute(TypeName.FluentMethodAttribute);

        if (fluentMethodAttribute?.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax attributeSyntax)
            return attributeSyntax.GetLocation();

        return parameter.Locations.FirstOrDefault() ?? Location.None;
    }

    private static string GetRequiredFluentParameterChainKey(FluentTargetContext context) =>
        string.Join("|", context.Constructor.Parameters
            .Where(p => !p.HasExplicitDefaultValue)
            .Select(p => $"{p.GetFluentMethodName(context.MethodPrefix ?? "With")}:{p.Type.ToDisplayString()}"));

    private static IEnumerable<Diagnostic> ValidateOptionalParameterAmbiguousChains(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        // Skip constructors with [MultipleFluentMethods] parameters
        var eligibleContexts = fluentTargetContexts
            .Where(ctx => !ctx.Constructor.Parameters.Any(
                p => p.GetAttribute(TypeName.MultipleFluentMethodsAttribute) is not null));

        var groups = eligibleContexts
            .GroupBy(GetRequiredFluentParameterChainKey);

        foreach (var group in groups)
        {
            var contexts = group.ToList();

            // Must have constructors from at least 2 different types
            var distinctTypes = contexts
                .Select(ctx => ctx.Constructor.ContainingType)
                .Distinct(SymbolEqualityComparer.Default)
                .Count();

            if (distinctTypes <= 1)
                continue;

            // Skip groups where no constructor has optional params (CVJG0016 covers these)
            if (!contexts.Any(ctx => ctx.Constructor.Parameters.Any(p => p.HasExplicitDefaultValue)))
                continue;

            // Skip groups where all full-chain keys are identical (CVJG0016 covers these)
            var fullChainKeys = contexts.Select(GetFluentParameterChainKey).Distinct().ToList();
            if (fullChainKeys.Count == 1)
                continue;

            // Skip groups where Create methods are disambiguated
            if (AreCreateMethodsDisambiguated(contexts))
                continue;

            var participatingTypes = contexts
                .Select(ctx => ctx.Constructor.ContainingType.ToDisplayString())
                .Distinct()
                .OrderBy(t => t);

            var typesString = string.Join(", ", participatingTypes);

            foreach (var context in contexts)
            {
                var optionalParams = context.Constructor.Parameters
                    .Where(p => p.HasExplicitDefaultValue);

                // Find a colliding constructor from a different type
                var collidingContext = contexts.First(ctx =>
                    !SymbolEqualityComparer.Default.Equals(
                        ctx.Constructor.ContainingType,
                        context.Constructor.ContainingType));

                foreach (var parameter in optionalParams)
                {
                    var location = parameter.Locations.FirstOrDefault() ?? Location.None;

                    yield return Diagnostic.Create(
                        FluentDiagnostics.OptionalParameterAmbiguousFluentMethodChain,
                        location,
                        parameter.Name,
                        context.Constructor.ToDisplayString(),
                        collidingContext.Constructor.ToDisplayString(),
                        typesString);
                }
            }
        }
    }

    private static IEnumerable<Diagnostic> ValidateReturnType(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        // Validate factory-level ReturnType defaults
        var rootTypes = fluentTargetContexts
            .Select(context => context.RootType)
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var rootType in rootTypes)
        {
            var defaults = FluentRootMetadataReader.GetFluentRootDefaults(rootType);
            if (defaults.ReturnType is null || defaults.TerminalMethod == TerminalMethodKind.None)
                continue;

            var factoryAttribute = rootType.GetAttributes(TypeName.FluentRootAttribute).FirstOrDefault();
            var location = factoryAttribute is not null ? GetAttributeLocation(factoryAttribute) : Location.None;

            var factoryContexts = fluentTargetContexts
                .Where(ctx => SymbolEqualityComparer.Default.Equals(ctx.RootType, rootType))
                .Where(ctx => !HasExplicitReturnType(ctx));

            foreach (var context in factoryContexts)
            {
                var diagnostic = ValidateReturnTypeAssignment(
                    context.Constructor.ContainingType,
                    defaults.ReturnType,
                    location,
                    context.Constructor.Locations.FirstOrDefault() ?? Location.None);

                if (diagnostic is not null)
                    yield return diagnostic;
            }
        }

        // Validate constructor-level ReturnType overrides
        foreach (var context in fluentTargetContexts.Where(HasExplicitReturnType))
        {
            if (context.ReturnType is null)
                continue;

            var location = GetAttributeLocation(context.AttributeData);

            if (context.TerminalMethod == TerminalMethodKind.None)
            {
                yield return Diagnostic.Create(FluentDiagnostics.ReturnTypeWithNone,
                    FindNamedArgumentLocation(context, "ReturnType"));
                continue;
            }

            var diagnostic = ValidateReturnTypeAssignment(
                context.Constructor.ContainingType,
                context.ReturnType,
                location,
                location);

            if (diagnostic is not null)
                yield return diagnostic;
        }
    }

    private static Diagnostic? ValidateReturnTypeAssignment(
        INamedTypeSymbol targetType,
        INamedTypeSymbol returnType,
        Location pointlessLocation,
        Location notAssignableLocation)
    {
        if (SymbolEqualityComparer.Default.Equals(targetType, returnType))
            return Diagnostic.Create(
                FluentDiagnostics.PointlessReturnType,
                pointlessLocation,
                targetType.ToDisplayString());

        if (!IsAssignableTo(targetType, returnType))
            return Diagnostic.Create(
                FluentDiagnostics.ReturnTypeNotAssignable,
                notAssignableLocation,
                targetType.ToDisplayString(),
                returnType.ToDisplayString());

        return null;
    }

    private static bool HasExplicitReturnType(FluentTargetContext context) =>
        context.AttributeData.NamedArguments.Any(namedArg => namedArg.Key == "ReturnType");

    private static bool IsAssignableTo(INamedTypeSymbol sourceType, INamedTypeSymbol targetType)
    {
        if (SymbolEqualityComparer.Default.Equals(sourceType, targetType))
            return true;

        // Check base types
        var baseType = sourceType.BaseType;
        while (baseType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, targetType))
                return true;

            if (targetType.IsUnboundGenericType &&
                baseType.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(baseType.ConstructUnboundGenericType(), targetType))
                return true;

            baseType = baseType.BaseType;
        }

        // Check interfaces
        foreach (var iface in sourceType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, targetType))
                return true;

            if (targetType.IsUnboundGenericType &&
                iface.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(iface.ConstructUnboundGenericType(), targetType))
                return true;
        }

        return false;
    }

    private static IEnumerable<Diagnostic> ValidateMultipleTargetsWithBuilderNone(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var groups = fluentTargetContexts
            .Where(ctx => ctx.TerminalMethod == TerminalMethodKind.None)
            .GroupBy(ctx => (ctx.Constructor.ContainingType, ctx.RootType),
                ContainingAndRootTypeComparer.Default);

        foreach (var group in groups)
        {
            foreach (var context in group.Skip(1))
            {
                yield return Diagnostic.Create(
                    FluentDiagnostics.MultipleTargetsWithBuilderNone,
                    GetAttributeLocation(context.AttributeData),
                    context.Constructor.ContainingType.ToDisplayString(),
                    context.RootType.ToDisplayString());
            }
        }
    }

    private sealed class ContainingAndRootTypeComparer
        : IEqualityComparer<(INamedTypeSymbol ContainingType, INamedTypeSymbol RootType)>
    {
        public static readonly ContainingAndRootTypeComparer Default = new();

        public bool Equals(
            (INamedTypeSymbol ContainingType, INamedTypeSymbol RootType) x,
            (INamedTypeSymbol ContainingType, INamedTypeSymbol RootType) y) =>
            SymbolEqualityComparer.Default.Equals(x.ContainingType, y.ContainingType)
            && SymbolEqualityComparer.Default.Equals(x.RootType, y.RootType);

        public int GetHashCode((INamedTypeSymbol ContainingType, INamedTypeSymbol RootType) obj) =>
            unchecked(
                SymbolEqualityComparer.Default.GetHashCode(obj.ContainingType) * 397
                ^ SymbolEqualityComparer.Default.GetHashCode(obj.RootType));
    }

    /// <summary>
    /// Reports an info diagnostic when [FluentMethod] is used without a method name on a constructor parameter,
    /// which has no effect since the parameter already generates a fluent method with the default name.
    /// </summary>
    private static IEnumerable<Diagnostic> ValidateFluentMethodNoEffectOnParameters(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var seen = new HashSet<IParameterSymbol>(SymbolEqualityComparer.Default);

        foreach (var context in fluentTargetContexts)
        {
            foreach (var parameter in context.Constructor.Parameters)
            {
                if (!seen.Add(parameter)) continue;

                var attribute = parameter.GetAttribute(TypeName.FluentMethodAttribute);
                if (attribute is null) continue;

                // Parameterless constructor: ConstructorArguments is empty
                if (attribute.ConstructorArguments.Length == 0)
                {
                    var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                                   ?? parameter.Locations.FirstOrDefault()
                                   ?? Location.None;

                    yield return Diagnostic.Create(
                        FluentDiagnostics.FluentMethodNoEffectOnParameter,
                        location,
                        parameter.Name);
                }
            }
        }
    }

    /// <summary>
    /// Reports a warning diagnostic when a target type has required properties but the constructor
    /// uses TerminalMethod.None, which doesn't generate a creation method for object initializer syntax.
    /// </summary>
    private static IEnumerable<Diagnostic> ValidatePropertyWithBuilderNone(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        foreach (var context in fluentTargetContexts)
        {
            if (context.TerminalMethod != TerminalMethodKind.None) continue;

            foreach (var prop in context.TargetTypeProperties)
            {
                if (!prop.IsRequired) continue;

                yield return Diagnostic.Create(
                    FluentDiagnostics.FluentMethodPropertyWithBuilderNone,
                    prop.Location,
                    prop.Property.Name,
                    context.Constructor.ContainingType.ToDisplayString());
            }
        }
    }
}
