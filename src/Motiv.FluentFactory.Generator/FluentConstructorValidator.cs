using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.ConstructorAnalysis;
using Motiv.FluentFactory.Generator.Diagnostics;

namespace Motiv.FluentFactory.Generator;

internal static class FluentConstructorValidatorExtensions
{
    public static IEnumerable<Diagnostic> GetDiagnostics(this ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        return ValidateRootTypeAttributes(fluentConstructorContexts)
            .Concat(ValidateMissingPartialModifier(fluentConstructorContexts))
            .Concat(ValidateCreateVerb(fluentConstructorContexts))
            .Concat(ValidateDuplicateCreateMethods(fluentConstructorContexts))
            .Concat(ValidateCreateVerbConflicts(fluentConstructorContexts))
            .Concat(ValidateParameterTypeAccessibility(fluentConstructorContexts))
            .Concat(ValidateAccessibilityMismatch(fluentConstructorContexts))
            .Concat(ValidateAmbiguousFluentMethodChains(fluentConstructorContexts));
    }

    private static IEnumerable<Diagnostic> ValidateMissingPartialModifier(ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        var rootTypesWithoutPartial = fluentConstructorContexts
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

    private static IEnumerable<Diagnostic> ValidateRootTypeAttributes(ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        // Check if the target type has the FluentFactory attribute
        var constructorContexts = fluentConstructorContexts
            .Where(context => !context.RootType.HasAttribute(TypeName.FluentFactoryAttribute));

        foreach (var context in constructorContexts)
            yield return Diagnostic.Create(
                FluentDiagnostics.FluentConstructorTargetTypeMissingFluentFactory,
                FindRootTypeLocation(context.AttributeData, context),
                context.RootType.ToDisplayString());
    }

    private static IEnumerable<Diagnostic> ValidateCreateVerb(ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        // Get contexts that have duplicates - we'll skip MFFG0007 for these since MFFG0008 will be reported
        var duplicateContexts = new HashSet<FluentConstructorContext>(GetDuplicateConstructorContexts(fluentConstructorContexts)
            .SelectMany(group => group));

        // Check for valid CreateVerb values, but skip those that are duplicates
        var constructorContextWithInvalidCreateVerb = fluentConstructorContexts
            .Except(duplicateContexts)
            .Where(IsVerbInvalid);

        foreach (var context in constructorContextWithInvalidCreateVerb)
        {
            yield return Diagnostic.Create(
                FluentDiagnostics.InvalidCreateVerb,
                FindCreateVerbArgumentLocation(context));
        }

        yield break;

        bool IsVerbInvalid(FluentConstructorContext context)
        {
            var isFirstCharValid = context.CreateVerb?.Select(char.IsLetter).FirstOrDefault() ?? true;
            var areRemainingCharsValid = context.CreateVerb?.Skip(1).All(char.IsLetterOrDigit) ?? true;
            return !(isFirstCharValid && areRemainingCharsValid);
        }
    }

    private static IEnumerable<Diagnostic> ValidateDuplicateCreateMethods(ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        // Check for duplicate resolved create method names within the same type
        var duplicateGroups = GetDuplicateConstructorContexts(fluentConstructorContexts);

        foreach (var group in duplicateGroups)
        {

            var contexts = group.ToList();
            var primaryLocation = FindCreateVerbArgumentLocation(contexts[0]);
            var additionalLocations = contexts
                .Skip(1)
                .Select(FindCreateVerbArgumentLocation);

            yield return Diagnostic.Create(
                FluentDiagnostics.DuplicateCreateMethodName,
                primaryLocation,
                additionalLocations: additionalLocations);
        }
    }

    private static IEnumerable<IGrouping<(string ResolvedName, string TypeName), FluentConstructorContext>> GetDuplicateConstructorContexts(
        ImmutableArray<FluentConstructorContext> fluentConstructorContexts) =>
        fluentConstructorContexts
            .Where(context => !string.IsNullOrEmpty(context.CreateVerb))
            .Select(context => (Context: context, ResolvedName: ResolveCreateMethodName(context)))
            .GroupBy(x => (x.ResolvedName, TypeName: x.Context.Constructor.ContainingType.ToDisplayString()), x => x.Context)
            .Where(group => group.Count() > 1);

    private static string ResolveCreateMethodName(FluentConstructorContext context)
    {
        var verb = context.CreateVerb ?? "Create";
        return context.CreateMethod switch
        {
            CreateMethodMode.Fixed => verb,
            _ => $"{verb}{context.Constructor.ContainingType.ToCreateMethodSuffix()}"
        };
    }

    private static IEnumerable<Diagnostic> ValidateCreateVerbConflicts(ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        // Check for CreateMethod.None used with CreateVerb
        var conflictedContexts = fluentConstructorContexts.AsEnumerable().Where(context =>
            context.CreateMethod == CreateMethodMode.None
            && !string.IsNullOrEmpty(context.CreateVerb));

        foreach (var context in conflictedContexts)
        {
            yield return Diagnostic.Create(
                FluentDiagnostics.CreateVerbWithNone,
                context.AttributeData.ApplicationSyntaxReference?.GetSyntax() switch
                {
                    AttributeSyntax attributeSyntax => attributeSyntax.GetLocation(),
                    _ => Location.None,
                });
        }
    }

    private static IEnumerable<Diagnostic> ValidateParameterTypeAccessibility(ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        foreach (var context in fluentConstructorContexts)
        {
            var factoryAccessibility = context.RootType.DeclaredAccessibility;

            foreach (var parameter in context.Constructor.Parameters)
            {
                var paramType = parameter.Type;

                // Skip built-in types (string, int, etc.) — SpecialType.None means it's a user-defined type
                if (paramType.SpecialType != SpecialType.None)
                    continue;

                // Skip type parameters — their DeclaredAccessibility is NotApplicable
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

    private static IEnumerable<Diagnostic> ValidateAccessibilityMismatch(ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        foreach (var context in fluentConstructorContexts)
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

    private static Location FindRootTypeLocation(AttributeData? fluentConstructorAttribute, FluentConstructorContext context)
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

    private static Location FindCreateVerbArgumentLocation(FluentConstructorContext context)
    {
        Location location;
        if (context.AttributeData.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax attributeSyntax)
        {
            // Find the CreateVerb named argument
            var createVerbArg = attributeSyntax.ArgumentList?.Arguments
                .OfType<AttributeArgumentSyntax>()
                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.ValueText == "CreateVerb");

            location = createVerbArg != null
                ? createVerbArg.GetLocation()
                : attributeSyntax.GetLocation();
        }
        else
        {
            location = context.Constructor.Locations.FirstOrDefault() ?? Location.None;
        }

        return location;
    }

    private static IEnumerable<Diagnostic> ValidateAmbiguousFluentMethodChains(
        ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        // Skip constructors with [MultipleFluentMethods] parameters since their
        // fluent method names are resolved from template methods, not GetFluentMethodName()
        var eligibleContexts = fluentConstructorContexts
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
                    var methodName = parameter.GetFluentMethodName();
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

    private static bool HasAmbiguousCreateMethods(IGrouping<string, FluentConstructorContext> group)
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
            .Where(ctx => ctx.CreateMethod != CreateMethodMode.None)
            .ToList();

        var constructorsWithNoCreate = group
            .Where(ctx => ctx.CreateMethod == CreateMethodMode.None)
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

    private static bool AreCreateMethodsDisambiguated(List<FluentConstructorContext> constructors)
    {
        // Zero or one constructor with Create methods cannot be ambiguous
        if (constructors.Count <= 1)
            return true;

        // Constructors with None don't produce create methods and can't be disambiguated by name
        var constructorsWithCreate = constructors
            .Where(ctx => ctx.CreateMethod != CreateMethodMode.None)
            .ToList();

        if (constructorsWithCreate.Count != constructors.Count)
            return false;

        // All constructors produce create methods — check resolved names are distinct
        var resolvedNames = constructorsWithCreate
            .Select(ResolveCreateMethodName)
            .ToList();

        var distinctNames = resolvedNames.Distinct().Count();

        return distinctNames == resolvedNames.Count;
    }

    private static string GetFluentParameterChainKey(FluentConstructorContext context) =>
        string.Join("|", context.Constructor.Parameters
            .Select(p => $"{p.GetFluentMethodName()}:{p.Type.ToDisplayString()}"));

    private static Location FindFluentMethodOrParameterLocation(IParameterSymbol parameter)
    {
        var fluentMethodAttribute = parameter.GetAttribute(TypeName.FluentMethodAttribute);

        if (fluentMethodAttribute?.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax attributeSyntax)
            return attributeSyntax.GetLocation();

        return parameter.Locations.FirstOrDefault() ?? Location.None;
    }
}
