using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Motiv.FluentFactory.Generator;

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
                $"{tp.Name}?",
            
            ITypeParameterSymbol tp =>
                tp.Name,
            
            _ => typeSymbol.ToDisplayString(GlobalQualifiedFormat)
        };
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
