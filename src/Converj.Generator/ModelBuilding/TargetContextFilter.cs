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
    /// Filters out targets that are inaccessible (private, protected, or protected-and-internal).
    /// </summary>
    public static (ImmutableArray<FluentTargetContext> Valid, IEnumerable<Diagnostic> Diagnostics)
        FilterInaccessibleTargets(
            ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var diagnostics = new List<Diagnostic>();
        var validContexts = ImmutableArray.CreateBuilder<FluentTargetContext>(fluentTargetContexts.Length);

        foreach (var context in fluentTargetContexts)
        {
            var accessibility = context.Method.DeclaredAccessibility;
            var isInaccessible = accessibility is
                Accessibility.Private or
                Accessibility.Protected or
                Accessibility.ProtectedAndInternal;

            if (!isInaccessible)
            {
                validContexts.Add(context);
                continue;
            }

            var location = context.Method.Locations.FirstOrDefault() ?? Location.None;
            diagnostics.Add(Diagnostic.Create(
                FluentDiagnostics.InaccessibleTarget,
                location,
                context.Method.ToDisplayString(),
                accessibility.ToString()));
        }

        return (validContexts.ToImmutable(), diagnostics);
    }

    /// <summary>
    /// Filters out targets that have unsupported parameter modifiers (ref, out, ref readonly).
    /// </summary>
    public static (ImmutableArray<FluentTargetContext> Valid, IEnumerable<Diagnostic> Diagnostics)
        FilterUnsupportedParameterModifierTargets(
            ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var diagnostics = new List<Diagnostic>();
        var validContexts = ImmutableArray.CreateBuilder<FluentTargetContext>(fluentTargetContexts.Length);

        foreach (var context in fluentTargetContexts)
        {
            var unsupportedParameter = context.Method.Parameters
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

            var location = context.Method.Locations.FirstOrDefault() ?? Location.None;
            diagnostics.Add(Diagnostic.Create(
                FluentDiagnostics.UnsupportedParameterModifier,
                location,
                context.Method.ToDisplayString(),
                unsupportedParameter.Name,
                modifierText));
        }

        return (validContexts.ToImmutable(), diagnostics);
    }

    /// <summary>
    /// Filters out targets that have parameters with error types.
    /// </summary>
    public static ImmutableArray<FluentTargetContext> FilterErrorTypeTargets(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        return
        [
            ..fluentTargetContexts
                .Where(ctx => ctx.Method.Parameters
                    .All(p => p.Type.TypeKind != TypeKind.Error))
        ];
    }

    /// <summary>
    /// Filters out targets where two or more [FluentCollectionMethod] parameters produce the same
    /// derived accumulator method name. Mirrors the skip-target-on-error behaviour of
    /// <see cref="FilterUnsupportedParameterModifierTargets"/>.
    /// </summary>
    public static (ImmutableArray<FluentTargetContext> Valid, IEnumerable<Diagnostic> Diagnostics)
        FilterCollectionAccumulatorCollisions(
            ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        var diagnostics = new List<Diagnostic>();
        var validContexts = ImmutableArray.CreateBuilder<FluentTargetContext>(fluentTargetContexts.Length);

        foreach (var context in fluentTargetContexts)
        {
            var collision = FindCollision(context.CollectionParameters);
            if (collision is null)
            {
                validContexts.Add(context);
                continue;
            }

            var (a, b) = collision.Value;
            var location = a.Parameter.Locations.FirstOrDefault() ?? Location.None;
            diagnostics.Add(Diagnostic.Create(
                FluentDiagnostics.AccumulatorMethodNameCollision,
                location,
                a.Parameter.Name,
                b.Parameter.Name,
                context.Method.ToDisplayString(),
                a.MethodName));
            // Skip target entirely — consistent with CVJG0011 pattern.
        }

        return (validContexts.ToImmutable(), diagnostics);
    }

    /// <summary>
    /// Finds the first pair of collection parameters whose derived accumulator method name AND
    /// element-type signature collide. Two parameters with the same derived name but different
    /// element types are signature-distinct and are permitted to coexist as C# overloads.
    /// Returns null when no collision exists.
    /// </summary>
    private static (CollectionParameterInfo First, CollectionParameterInfo Second)? FindCollision(
        ImmutableArray<CollectionParameterInfo> parameters)
    {
        if (parameters.Length < 2) return null;
        for (var i = 0; i < parameters.Length; i++)
        for (var j = i + 1; j < parameters.Length; j++)
            if (HaveIdenticalAccumulatorSignature(parameters[i], parameters[j]))
                return (parameters[i], parameters[j]);
        return null;
    }

    /// <summary>
    /// Returns true when two collection parameters would produce accumulator methods with
    /// both the same derived name AND the same element-type parameter signature.
    /// Signature-distinct methods (same name, different element types) are not collisions.
    /// </summary>
    private static bool HaveIdenticalAccumulatorSignature(
        CollectionParameterInfo first,
        CollectionParameterInfo second)
    {
        if (!string.Equals(first.MethodName, second.MethodName, StringComparison.Ordinal))
            return false;

        return SymbolEqualityComparer.Default.Equals(first.ElementType, second.ElementType);
    }

    /// <summary>
    /// Validates [This] attribute usage and yields diagnostics for invalid placements.
    /// </summary>
    public static IEnumerable<Diagnostic> ValidateThisAttributeUsage(ImmutableArray<FluentTargetContext> contexts)
    {
        foreach (var context in contexts)
        {
            var method = context.Method;

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
