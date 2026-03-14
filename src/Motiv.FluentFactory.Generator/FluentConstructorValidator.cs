using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.ConstructorAnalysis;
using Motiv.FluentFactory.Generator.Diagnostics;
using static Motiv.FluentFactory.Generator.FluentFactoryGeneratorOptions;

namespace Motiv.FluentFactory.Generator;

internal static class FluentConstructorValidatorExtensions
{
    public static IEnumerable<Diagnostic> GetDiagnostics(this ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        return ValidateRootTypeAttributes(fluentConstructorContexts)
            .Concat(ValidateMissingPartialModifier(fluentConstructorContexts))
            .Concat(ValidateCreateMethodNames(fluentConstructorContexts))
            .Concat(ValidateDuplicateCreateMethodNames(fluentConstructorContexts))
            .Concat(ValidateCreateMethodNameConflicts(fluentConstructorContexts))
            .Concat(ValidateParameterTypeAccessibility(fluentConstructorContexts))
            .Concat(ValidateAccessibilityMismatch(fluentConstructorContexts));
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

    private static IEnumerable<Diagnostic> ValidateCreateMethodNames(ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        // Get contexts that have duplicates - we'll skip MFFG0007 for these since MFFG0008 will be reported
        var duplicateContexts = new HashSet<FluentConstructorContext>(GetDuplicateConstructorContexts(fluentConstructorContexts)
            .SelectMany(group => group));

        // Check for valid CreateMethodName values, but skip those that are duplicates
        var constructorContextWithInvalidCreateMethodName = fluentConstructorContexts
            .Except(duplicateContexts)
            .Where(IsMethodNameValid);

        foreach (var context in constructorContextWithInvalidCreateMethodName)
        {
            yield return Diagnostic.Create(
                FluentDiagnostics.InvalidCreateMethodName,
                FindCreateMethodNameArgumentLocation(context));
        }

        yield break;

        bool IsMethodNameValid(FluentConstructorContext context)
        {
            var isFirstCharValid = context.CreateMethodName?.Select(char.IsLetter).FirstOrDefault() ?? true;
            var areRemainingCharsValid = context.CreateMethodName?.Skip(1).All(char.IsLetterOrDigit) ?? true;
            return !(isFirstCharValid && areRemainingCharsValid);
        }
    }

    private static IEnumerable<Diagnostic> ValidateDuplicateCreateMethodNames(ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        // Check for duplicate CreateMethodName values within the same type
        var duplicateGroups = GetDuplicateConstructorContexts(fluentConstructorContexts);

        foreach (var group in duplicateGroups)
        {

            var contexts = group.ToList();
            var primaryLocation = FindCreateMethodNameArgumentLocation(contexts[0]);
            var additionalLocations = contexts
                .Skip(1)
                .Select(FindCreateMethodNameArgumentLocation);

            yield return Diagnostic.Create(
                FluentDiagnostics.DuplicateCreateMethodName,
                primaryLocation,
                additionalLocations: additionalLocations);
        }
    }

    private static IEnumerable<IGrouping<(string? CreateMethodName, string TypeName), FluentConstructorContext>> GetDuplicateConstructorContexts(
        ImmutableArray<FluentConstructorContext> fluentConstructorContexts) =>
        fluentConstructorContexts
            .Where(context => !string.IsNullOrEmpty(context.CreateMethodName))
            .GroupBy(context =>(context.CreateMethodName, TypeName: context.Constructor.ContainingType.ToDisplayString()))
            .Where(group => group.Count() > 1);

    private static IEnumerable<Diagnostic> ValidateCreateMethodNameConflicts(ImmutableArray<FluentConstructorContext> fluentConstructorContexts)
    {
        // Check for NoCreateMethod option used with CreateMethodName
        var conflictedMethodNameFluentConstructorContexts = fluentConstructorContexts.AsEnumerable().Where(context =>
            context.Options.HasFlag(NoCreateMethod)
            && !string.IsNullOrEmpty(context.CreateMethodName));

        foreach (var context in conflictedMethodNameFluentConstructorContexts)
        {
            yield return Diagnostic.Create(
                FluentDiagnostics.CreateMethodNameWithNoCreateMethod,
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

    private static Location FindCreateMethodNameArgumentLocation(FluentConstructorContext context)
    {
        Location location;
        if (context.AttributeData.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax attributeSyntax)
        {
            // Find the CreateMethodName named argument
            var createMethodNameArg = attributeSyntax.ArgumentList?.Arguments
                .OfType<AttributeArgumentSyntax>()
                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.ValueText == "CreateMethodName");

            location = createMethodNameArg != null
                ? createMethodNameArg.GetLocation()
                : attributeSyntax.GetLocation();
        }
        else
        {
            location = context.Constructor.Locations.FirstOrDefault() ?? Location.None;
        }

        return location;
    }
}
