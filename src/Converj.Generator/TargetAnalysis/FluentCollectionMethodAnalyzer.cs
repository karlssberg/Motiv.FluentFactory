using System.Collections.Immutable;
using Converj.Generator.Diagnostics;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Analyzer that detects <c>[FluentCollectionMethod]</c> usage on constructor/method parameters
/// and on target-type properties. Validates that the type is a supported collection type and
/// derives or validates the accumulator method name.
/// </summary>
/// <remarks>
/// Supported collection types: <c>T[]</c>, <c>IEnumerable&lt;T&gt;</c>,
/// <c>ICollection&lt;T&gt;</c>, <c>IList&lt;T&gt;</c>,
/// <c>IReadOnlyCollection&lt;T&gt;</c>, <c>IReadOnlyList&lt;T&gt;</c>.
///
/// Name derivation: explicit <c>[FluentCollectionMethod("Name")]</c> wins. Otherwise the
/// parameter/property name is singularized and capitalized to produce <c>Add{Singular}</c>.
/// If no rule fires (or the result is a C# keyword), <c>CVJG0051</c> is emitted.
/// </remarks>
internal static class FluentCollectionMethodAnalyzer
{
    /// <summary>
    /// Analyses every parameter of <paramref name="method"/> that carries a
    /// <c>[FluentCollectionMethod]</c> attribute.  Valid parameters produce a
    /// <see cref="CollectionParameterInfo"/> entry; invalid ones contribute a diagnostic
    /// to <paramref name="diagnostics"/>.
    /// </summary>
    /// <param name="method">The target method (constructor or static method) to analyse.</param>
    /// <param name="diagnostics">Collector for any diagnostics produced during analysis.</param>
    /// <returns>
    /// An <see cref="ImmutableArray{T}"/> of <see cref="CollectionParameterInfo"/> for each
    /// valid collection parameter.  Empty when no parameters carry the attribute.
    /// </returns>
    public static ImmutableArray<CollectionParameterInfo> Analyze(
        IMethodSymbol method,
        DiagnosticList diagnostics)
    {
        var builder = ImmutableArray.CreateBuilder<CollectionParameterInfo>();

        foreach (var parameter in method.Parameters)
        {
            var attr = parameter.GetAttributes(TypeName.FluentCollectionMethodAttribute).FirstOrDefault();
            if (attr is null)
                continue;

            var location = GetAttributeLocation(attr, parameter);

            var (isCollection, elementType) = DetectCollection(parameter.Type);
            if (!isCollection)
            {
                diagnostics.Add(Diagnostic.Create(
                    FluentDiagnostics.NonCollectionFluentCollectionMethod,
                    location,
                    parameter.Name,
                    parameter.Type.ToDisplayString()));
                continue;
            }

            var explicitName = attr.GetFirstStringArgument();
            var minItems = ReadMinItemsNamedArg(attr);

            string? methodName;
            if (explicitName is not null)
            {
                // Explicit override — use as-is; the generator will surface CS errors at emission if invalid
                methodName = explicitName;
            }
            else
            {
                methodName = TryDeriveAccumulatorName(parameter.Name);
                if (methodName is null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        FluentDiagnostics.UnsingularizableParameterName,
                        location,
                        parameter.Name));
                    continue;
                }
            }

            builder.Add(new CollectionParameterInfo(parameter, elementType!, parameter.Type, methodName, minItems));
        }

        var result = builder.ToImmutable();
        DetectDisjointUnresolvedGenerics(method, result, diagnostics);
        return result;
    }

    /// <summary>
    /// Emits <see cref="FluentDiagnostics.DisjointUnresolvedCollectionGenerics"/> when the target has
    /// two or more collection parameters whose element types carry disjoint type parameters that are
    /// not resolved by the non-collection parameters of the target.
    /// </summary>
    /// <remarks>
    /// The split-accumulator design resolves a single unresolved element-type parameter via generic
    /// inference on the first <c>AddX</c> call. Supporting multiple disjoint unresolved parameters
    /// would require a combinatorial lattice of intermediate struct shapes; out of scope.
    /// </remarks>
    private static void DetectDisjointUnresolvedGenerics(
        IMethodSymbol method,
        ImmutableArray<CollectionParameterInfo> collectionParameters,
        DiagnosticList diagnostics)
    {
        if (collectionParameters.Length < 2)
            return;

        var collectionParamSymbols = collectionParameters
            .Select(cp => cp.Parameter)
            .ToImmutableHashSet<IParameterSymbol>(SymbolEqualityComparer.Default);

        var resolvedNames = method.Parameters
            .Where(p => !collectionParamSymbols.Contains(p))
            .SelectMany(p => CollectTypeParameterNames(p.Type))
            .ToImmutableHashSet(StringComparer.Ordinal);

        var unresolvedPerCollection = collectionParameters
            .Select(cp => CollectTypeParameterNames(cp.ElementType)
                .Where(n => !resolvedNames.Contains(n))
                .Distinct(StringComparer.Ordinal)
                .ToImmutableArray())
            .ToImmutableArray();

        var collectionsWithUnresolved = unresolvedPerCollection
            .Where(set => !set.IsEmpty)
            .ToImmutableArray();

        if (collectionsWithUnresolved.Length < 2)
            return;

        var unionSize = collectionsWithUnresolved
            .SelectMany(set => set)
            .Distinct(StringComparer.Ordinal)
            .Count();

        if (unionSize <= 1)
            return;

        var location = method.Locations.FirstOrDefault() ?? Location.None;
        var allUnresolved = string.Join(", ", collectionsWithUnresolved
            .SelectMany(set => set)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal));

        diagnostics.Add(Diagnostic.Create(
            FluentDiagnostics.DisjointUnresolvedCollectionGenerics,
            location,
            method.ContainingType.ToDisplayString(),
            allUnresolved));
    }

    private static IEnumerable<string> CollectTypeParameterNames(ITypeSymbol type)
    {
        switch (type)
        {
            case ITypeParameterSymbol tp:
                yield return tp.Name;
                yield break;
            case INamedTypeSymbol named:
                foreach (var arg in named.TypeArguments)
                foreach (var name in CollectTypeParameterNames(arg))
                    yield return name;
                yield break;
            case IArrayTypeSymbol array:
                foreach (var name in CollectTypeParameterNames(array.ElementType))
                    yield return name;
                yield break;
        }
    }

    /// <summary>
    /// Analyses every property of <paramref name="targetType"/> that carries a
    /// <c>[FluentCollectionMethod]</c> attribute.  Valid properties produce a
    /// <see cref="CollectionPropertyInfo"/> entry; invalid or unsupported ones contribute a
    /// diagnostic to <paramref name="diagnostics"/>.
    /// </summary>
    /// <param name="targetType">The target type whose properties to analyse.</param>
    /// <param name="diagnostics">Collector for any diagnostics produced during analysis.</param>
    /// <returns>
    /// An <see cref="ImmutableArray{T}"/> of <see cref="CollectionPropertyInfo"/> for each
    /// valid collection property.  Empty when no properties carry the attribute.
    /// </returns>
    public static ImmutableArray<CollectionPropertyInfo> AnalyzeProperties(
        INamedTypeSymbol targetType,
        DiagnosticList diagnostics)
    {
        var builder = ImmutableArray.CreateBuilder<CollectionPropertyInfo>();

        foreach (var property in targetType.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.IsStatic || property.IsIndexer) continue;

            var attr = property.GetAttributes(TypeName.FluentCollectionMethodAttribute).FirstOrDefault();
            if (attr is null)
                continue;

            var location = GetPropertyAttributeLocation(attr, property);

            // CVJG0053: record primary-constructor positional properties cannot be re-assigned
            if (IsRecordPrimaryConstructorPositionalProperty(targetType, property))
            {
                diagnostics.Add(Diagnostic.Create(
                    FluentDiagnostics.UnsupportedCollectionPropertyAccessor,
                    location,
                    property.Name,
                    targetType.Name,
                    "record primary-constructor positional properties cannot be re-assigned via object initializer"));
                continue;
            }

            // CVJG0053: property without a set or init accessor cannot be assigned at terminal time
            if (property.SetMethod is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    FluentDiagnostics.UnsupportedCollectionPropertyAccessor,
                    location,
                    property.Name,
                    targetType.Name,
                    "property has no set or init accessor and cannot be assigned"));
                continue;
            }

            var (isCollection, elementType) = DetectCollection(property.Type);
            if (!isCollection)
            {
                // Reuse CVJG0050 (same descriptor as non-collection parameter misuse, per plan spec)
                diagnostics.Add(Diagnostic.Create(
                    FluentDiagnostics.NonCollectionFluentCollectionMethod,
                    location,
                    property.Name,
                    property.Type.ToDisplayString()));
                continue;
            }

            var explicitName = attr.GetFirstStringArgument();
            var minItems = ReadMinItemsNamedArg(attr);

            string? methodName;
            if (explicitName is not null)
            {
                methodName = explicitName;
            }
            else
            {
                methodName = TryDeriveAccumulatorName(property.Name);
                if (methodName is null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        FluentDiagnostics.UnsingularizableParameterName,
                        location,
                        property.Name));
                    continue;
                }
            }

            builder.Add(new CollectionPropertyInfo(property, elementType!, property.Type, methodName, minItems));
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Determines whether <paramref name="property"/> is a record primary-constructor positional
    /// property — i.e., it was auto-generated from a primary constructor parameter on a record type.
    /// Such properties cannot be re-assigned via an object initializer.
    /// </summary>
    private static bool IsRecordPrimaryConstructorPositionalProperty(
        INamedTypeSymbol containingType,
        IPropertySymbol property)
    {
        if (!containingType.IsRecord) return false;

        // A record primary-ctor positional property has the same name (case-insensitive) as a
        // primary constructor parameter. Use FindPrimaryConstructor + parameter name comparison.
        var primaryCtor = containingType.FindPrimaryConstructor();
        if (primaryCtor is null) return false;

        return primaryCtor.Parameters.Any(p =>
            p.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns the location of the <c>[FluentCollectionMethod]</c> attribute application on a property,
    /// falling back to the property's own location, then <see cref="Location.None"/>.
    /// </summary>
    private static Location GetPropertyAttributeLocation(AttributeData attr, IPropertySymbol property)
    {
        if (attr.ApplicationSyntaxReference?.GetSyntax() is { } attrSyntax)
            return attrSyntax.GetLocation();

        return property.Locations.FirstOrDefault() ?? Location.None;
    }

    /// <summary>
    /// Determines whether <paramref name="type"/> is one of the six supported collection types
    /// and extracts the element type.
    /// </summary>
    /// <param name="type">The type symbol to inspect.</param>
    /// <returns>
    /// <c>(true, elementType)</c> for arrays and the five supported generic interfaces;
    /// <c>(false, null)</c> for everything else.
    /// </returns>
    /// <remarks>
    /// Detection uses <see cref="IArrayTypeSymbol"/> and <see cref="SpecialType"/> checks only —
    /// we do NOT walk <c>AllInterfaces</c>.  This prevents <c>string</c> from being accepted
    /// as <c>IEnumerable&lt;char&gt;</c> (Pitfall 1) and keeps analysis O(1) per parameter.
    /// </remarks>
    private static (bool IsCollection, ITypeSymbol? ElementType) DetectCollection(ITypeSymbol type)
    {
        // Array: T[] → element type is ElementType
        if (type is IArrayTypeSymbol array)
            return (true, array.ElementType);

        // Generic interface: check OriginalDefinition.SpecialType against the five allowlisted types
        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var specialType = named.OriginalDefinition.SpecialType;
            if (specialType is
                SpecialType.System_Collections_Generic_IEnumerable_T or
                SpecialType.System_Collections_Generic_ICollection_T or
                SpecialType.System_Collections_Generic_IList_T or
                SpecialType.System_Collections_Generic_IReadOnlyCollection_T or
                SpecialType.System_Collections_Generic_IReadOnlyList_T)
            {
                return (true, named.TypeArguments[0]);
            }
        }

        return (false, null);
    }

    /// <summary>
    /// Reads the <c>MinItems</c> named argument from a <c>[FluentCollectionMethod]</c> attribute.
    /// </summary>
    /// <param name="attribute">The attribute data to inspect.</param>
    /// <returns>The value of <c>MinItems</c>, or <c>0</c> when not specified.</returns>
    private static int ReadMinItemsNamedArg(AttributeData attribute)
    {
        foreach (var kv in attribute.NamedArguments)
        {
            if (kv.Key == "MinItems" && kv.Value.Value is int value)
                return value;
        }

        return 0;
    }

    /// <summary>
    /// Tries to derive an accumulator method name from a plural parameter name by singularizing it
    /// and prefixing with <c>Add</c>.  Returns <see langword="null"/> when:
    /// <list type="bullet">
    ///   <item><description>Singularization yields no result (no rule fires)</description></item>
    ///   <item><description>The singular form equals the original (already singular)</description></item>
    ///   <item><description>The singular form is a C# keyword</description></item>
    /// </list>
    /// </summary>
    /// <param name="paramName">The parameter name to singularize.</param>
    /// <returns>The derived <c>Add{Singular}</c> name, or <see langword="null"/>.</returns>
    private static string? TryDeriveAccumulatorName(string paramName)
    {
        var singular = paramName.Singularize();

        if (string.IsNullOrWhiteSpace(singular))
            return null;

        // If singularization produced no change, the name is already singular → cannot derive
        if (singular == paramName)
            return null;

        // Reject if the singular form is a reserved C# keyword
        if (SyntaxFacts.GetKeywordKind(singular!) != SyntaxKind.None)
            return null;

        return $"Add{singular!.Capitalize()}";
    }

    /// <summary>
    /// Returns the location of the <c>[FluentCollectionMethod]</c> attribute application,
    /// falling back to the parameter's own location, then <see cref="Location.None"/>.
    /// </summary>
    private static Location GetAttributeLocation(AttributeData attr, IParameterSymbol parameter)
    {
        if (attr.ApplicationSyntaxReference?.GetSyntax() is { } attrSyntax)
            return attrSyntax.GetLocation();

        return parameter.Locations.FirstOrDefault() ?? Location.None;
    }
}
