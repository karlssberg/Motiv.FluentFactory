using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Converj.Generator.Diagnostics;
using Converj.Generator.Domain;
using Converj.Generator.TargetAnalysis;

namespace Converj.Generator.ModelBuilding;

/// <summary>
/// Validates and filters <see cref="FluentTargetContext"/> inputs before domain model building begins.
/// All methods are static and return diagnostics without side-effects on shared state.
/// </summary>
internal static class TargetContextFilter
{
    /// <summary>
    /// Filters out constructors that are inaccessible (private, protected, or protected-and-internal).
    /// </summary>
    public static (ImmutableArray<FluentTargetContext> Valid, IEnumerable<Diagnostic> Diagnostics)
        FilterInaccessibleConstructors(
            ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var diagnostics = new List<Diagnostic>();
        var validContexts = ImmutableArray.CreateBuilder<FluentTargetContext>(fluentTargetContexts.Length);

        foreach (var context in fluentTargetContexts)
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

    /// <summary>
    /// Filters out constructors that have unsupported parameter modifiers (ref, out, ref readonly).
    /// </summary>
    public static (ImmutableArray<FluentTargetContext> Valid, IEnumerable<Diagnostic> Diagnostics)
        FilterUnsupportedParameterModifierConstructors(
            ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var diagnostics = new List<Diagnostic>();
        var validContexts = ImmutableArray.CreateBuilder<FluentTargetContext>(fluentTargetContexts.Length);

        foreach (var context in fluentTargetContexts)
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

    /// <summary>
    /// Filters out constructors that have parameters with error types.
    /// </summary>
    public static ImmutableArray<FluentTargetContext> FilterErrorTypeConstructors(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        return
        [
            ..fluentTargetContexts
                .Where(ctx => ctx.Constructor.Parameters
                    .All(p => p.Type.TypeKind != TypeKind.Error))
        ];
    }

    /// <summary>
    /// Validates [This] attribute usage and yields diagnostics for invalid placements.
    /// </summary>
    public static IEnumerable<Diagnostic> ValidateThisAttributeUsage(ImmutableArray<FluentTargetContext> contexts)
    {
        foreach (var context in contexts)
        {
            var method = context.Constructor;

            foreach (var param in method.Parameters)
            {
                var thisAttr = param.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == TypeName.ThisAttribute);

                if (thisAttr is null)
                    continue;

                var location = thisAttr.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                               ?? param.Locations.FirstOrDefault()
                               ?? Location.None;

                // [This] on non-first parameter
                if (!SymbolEqualityComparer.Default.Equals(param, method.Parameters[0]))
                {
                    yield return Diagnostic.Create(
                        FluentDiagnostics.ThisAttributeNotOnFirstParameter,
                        location,
                        param.Name);
                    continue;
                }

                // [This] on instance method
                if (context.IsInstanceMethodTarget)
                {
                    yield return Diagnostic.Create(
                        FluentDiagnostics.ThisAttributeOnInstanceMethod,
                        location,
                        method.Name);
                    continue;
                }

                // [This] on extension method first param (tautologous)
                if (method.IsExtensionMethod)
                {
                    yield return Diagnostic.Create(
                        FluentDiagnostics.ThisAttributeTautologous,
                        location,
                        param.Name);
                }
            }
        }
    }

    /// <summary>
    /// Validates the root type is a static partial class when used with extension method targets.
    /// </summary>
    public static IEnumerable<Diagnostic> ValidateRootForExtensionTargets(
        INamedTypeSymbol rootType,
        ImmutableArray<FluentTargetContext> contexts)
    {
        if (rootType.IsStatic)
            yield break;

        if (!contexts.Any(c => c.HasReceiver))
            yield break;

        var location = rootType.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()
                       ?? Location.None;

        yield return Diagnostic.Create(
            FluentDiagnostics.RootMustBeStaticForExtensionTargets,
            location,
            rootType.Name);
    }
}
