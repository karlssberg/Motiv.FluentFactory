using System.Collections.Immutable;
using Converj.Generator.Diagnostics;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Per-parameter analyzer that detects <c>[FluentCollectionMethod]</c> usage,
/// validates that the parameter type is a supported collection type, and derives
/// (or validates) the accumulator method name.
/// </summary>
/// <remarks>
/// Supported collection types: <c>T[]</c>, <c>IEnumerable&lt;T&gt;</c>,
/// <c>ICollection&lt;T&gt;</c>, <c>IList&lt;T&gt;</c>,
/// <c>IReadOnlyCollection&lt;T&gt;</c>, <c>IReadOnlyList&lt;T&gt;</c>.
///
/// Name derivation: explicit <c>[FluentCollectionMethod("Name")]</c> wins. Otherwise the
/// parameter name is singularized and capitalized to produce <c>Add{Singular}</c>.
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

        return builder.ToImmutable();
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
