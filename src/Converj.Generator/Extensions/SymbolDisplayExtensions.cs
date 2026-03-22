using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Converj.Generator;

/// <summary>
/// Extension methods for symbol display formatting and accessibility conversion.
/// </summary>
internal static class SymbolDisplayExtensions
{
    private static readonly SymbolDisplayFormat TypeNameOnlyFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
    );

    private static readonly SymbolDisplayFormat GlobalQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat FullFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                         SymbolDisplayGenericsOptions.IncludeTypeConstraints,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters |
                       SymbolDisplayMemberOptions.IncludeContainingType |
                       SymbolDisplayMemberOptions.IncludeType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                          SymbolDisplayParameterOptions.IncludeName |
                          SymbolDisplayParameterOptions.IncludeDefaultValue,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                              SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>
    /// Returns the global::-qualified display string for a type symbol.
    /// Type parameters (T, TResult) are returned as their name only.
    /// C# keyword aliases (int, string, bool) are preserved via UseSpecialTypes.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to format.</param>
    /// <returns>The global::-qualified display string.</returns>
    public static string ToGlobalDisplayString(this ITypeSymbol typeSymbol)
    {
        return typeSymbol switch
        {
            ITypeParameterSymbol { NullableAnnotation: NullableAnnotation.Annotated } tp =>
                $"{tp.GetEffectiveName()}?",

            ITypeParameterSymbol tp =>
                tp.GetEffectiveName(),

            INamedTypeSymbol namedType when HasAliasedTypeParameters(namedType) =>
                RebuildGlobalGenericDisplayString(namedType, arg => arg.ToGlobalDisplayString()),

            _ => typeSymbol.ToDisplayString(GlobalQualifiedFormat)
        };
    }

    /// <summary>
    /// Returns the global::-qualified display string for a type symbol,
    /// remapping type parameter names using the provided mapping from effective names to local names.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to format.</param>
    /// <param name="effectiveToLocalNameMap">A mapping from effective type parameter names to local scope names.</param>
    /// <returns>The global::-qualified display string with remapped type parameter names.</returns>
    public static string ToGlobalDisplayString(
        this ITypeSymbol typeSymbol,
        IDictionary<string, string> effectiveToLocalNameMap)
    {
        if (effectiveToLocalNameMap.Count == 0)
            return typeSymbol.ToGlobalDisplayString();

        return typeSymbol switch
        {
            ITypeParameterSymbol { NullableAnnotation: NullableAnnotation.Annotated } tp =>
                $"{ResolveTypeParameterName(tp, effectiveToLocalNameMap)}?",

            ITypeParameterSymbol tp =>
                ResolveTypeParameterName(tp, effectiveToLocalNameMap),

            INamedTypeSymbol { IsGenericType: true } namedType =>
                RebuildGlobalGenericDisplayString(namedType, arg => arg.ToGlobalDisplayString(effectiveToLocalNameMap)),

            _ => typeSymbol.ToDisplayString(GlobalQualifiedFormat)
        };
    }

    private static bool HasAliasedTypeParameters(INamedTypeSymbol namedType)
    {
        if (!namedType.IsGenericType) return false;
        return namedType.TypeArguments.Any(arg => arg switch
        {
            ITypeParameterSymbol tp => tp.GetEffectiveName() != tp.Name,
            INamedTypeSymbol nested => HasAliasedTypeParameters(nested),
            _ => false
        });
    }

    private static string ResolveTypeParameterName(
        ITypeParameterSymbol tp,
        IDictionary<string, string> effectiveToLocalNameMap)
    {
        var effectiveName = tp.GetEffectiveName();
        return effectiveToLocalNameMap.TryGetValue(effectiveName, out var localName)
            ? localName
            : effectiveName;
    }

    /// <summary>
    /// Strips the generic type arguments from a global::-qualified display string
    /// and rebuilds them using the provided resolver for each type argument.
    /// </summary>
    private static string RebuildGlobalGenericDisplayString(
        INamedTypeSymbol namedType,
        Func<ITypeSymbol, string> resolveArgument)
    {
        var baseDisplay = namedType.OriginalDefinition.ToDisplayString(GlobalQualifiedFormat);

        var angleBracketIndex = baseDisplay.IndexOf('<');
        if (angleBracketIndex >= 0)
            baseDisplay = baseDisplay.Substring(0, angleBracketIndex);

        var resolvedArgs = namedType.TypeArguments.Select(resolveArgument);

        return $"{baseDisplay}<{string.Join(", ", resolvedArgs)}>";
    }

    /// <summary>
    /// Returns the full display string for a symbol including namespace,
    /// containing types, type parameters, constraints, and member details.
    /// </summary>
    /// <param name="symbol">The symbol to format.</param>
    /// <returns>The full display string.</returns>
    public static string ToFullDisplayString(this ISymbol symbol)
    {
        return symbol.ToDisplayString(FullFormat);
    }

    /// <summary>
    /// Returns the global::-qualified display string using original type parameter names,
    /// without applying [As] aliases.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to format.</param>
    /// <returns>The global::-qualified display string with original type parameter names.</returns>
    public static string ToGlobalOriginalDisplayString(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(GlobalQualifiedFormat);

    /// <summary>
    /// Returns the unqualified (name-only) display string for a type symbol.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to format.</param>
    /// <returns>The unqualified display string.</returns>
    public static string ToUnqualifiedDisplayString(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(TypeNameOnlyFormat);

    /// <summary>
    /// Converts a <see cref="Accessibility"/> value to the corresponding syntax kind keywords.
    /// </summary>
    /// <param name="accessibility">The accessibility to convert.</param>
    /// <returns>An enumerable of <see cref="SyntaxKind"/> values representing the access modifier keywords.</returns>
    public static IEnumerable<SyntaxKind> AccessibilityToSyntaxKind(this Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public => [SyntaxKind.PublicKeyword],
            Accessibility.Private => [SyntaxKind.PrivateKeyword],
            Accessibility.Protected => [SyntaxKind.ProtectedKeyword],
            Accessibility.Internal => [SyntaxKind.InternalKeyword],
            Accessibility.ProtectedOrInternal => [SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword],
            Accessibility.ProtectedAndInternal => [SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword],
            _ => [SyntaxKind.None]
        };
}
