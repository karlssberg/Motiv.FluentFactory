using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator;

/// <summary>
/// Extension methods for string manipulation and symbol-to-string conversion utilities.
/// </summary>
internal static class StringExtensions
{
    private static readonly SymbolDisplayFormat FullyQualifiedFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );

    /// <summary>
    /// Capitalizes the first character of a string.
    /// </summary>
    /// <param name="input">The string to capitalize.</param>
    /// <returns>The input string with the first character converted to uppercase.</returns>
    public static string Capitalize(this string input) =>
        string.IsNullOrEmpty(input)
            ? input
            : $"{char.ToUpper(input[0])}{input.Substring(1)}";

    /// <summary>
    /// Converts the first character of a string to lowercase (camelCase convention).
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The input string with the first character converted to lowercase.</returns>
    public static string ToCamelCase(this string input) =>
        string.IsNullOrEmpty(input)
            ? input
            : $"{char.ToLower(input[0])}{input.Substring(1)}";

    /// <summary>
    /// Converts a name to a parameter field name by prepending an underscore
    /// and appending "__parameter" to the camelCased name.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>A parameter field name in the format _camelCaseName__parameter.</returns>
    public static string ToParameterFieldName(this string name)
    {
        return $"_{name.ToCamelCase()}__parameter";
    }

    /// <summary>
    /// Converts a type symbol to a safe identifier string by replacing namespace
    /// separators and generic type markers with underscores.
    /// </summary>
    /// <param name="name">The type symbol to convert.</param>
    /// <returns>A safe identifier string.</returns>
    public static string ToIdentifier(this ITypeSymbol name)
    {
        return name
            .ToDisplayString(FullyQualifiedFormat)
            .Replace(".", "_")
            .Replace(",", "_")
            .Replace("<", "__")
            .Replace(">", "__")
            .Replace(" ", "");
    }

    /// <summary>
    /// Converts a named type symbol to a safe file name by replacing generic
    /// type markers with underscores.
    /// </summary>
    /// <param name="namedTypeSymbol">The named type symbol to convert.</param>
    /// <returns>A safe file name string.</returns>
    public static string ToFileName(this INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol
            .ToDisplayString()
            .Replace("<", "__")
            .Replace(">", "__")
            .Replace(',', '_');
    }

    /// <summary>
    /// Derives a Create method suffix from a named type symbol by walking the containing type
    /// chain for nested types and joining with underscores (e.g., <c>Outer_Inner_Target</c>).
    /// </summary>
    /// <param name="symbol">The named type symbol to derive a suffix from.</param>
    /// <returns>A suffix string suitable for appending to a Create method verb.</returns>
    public static string ToCreateMethodSuffix(this INamedTypeSymbol symbol)
    {
        var names = new List<string>();
        var current = symbol;
        while (current is not null)
        {
            names.Add(current.Name);
            current = current.ContainingType;
        }

        names.Reverse();
        return string.Join("_", names);
    }
}
