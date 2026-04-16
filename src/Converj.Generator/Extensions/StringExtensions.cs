using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Extensions;

/// <summary>
/// Extension methods for string manipulation and symbol-to-string conversion utilities.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Irregular plural-to-singular mappings that override all suffix rules.
    /// Lookup is performed BEFORE any suffix rule is applied (Pattern 4 — irregulars first).
    /// Covers NAME-03 irregular requirement.
    /// </summary>
    private static readonly Dictionary<string, string> Irregulars =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "children", "child" },
            { "people",   "person" },
            { "men",      "man" },
            { "women",    "woman" },
            { "indices",  "index" },
            { "matrices", "matrix" },
            { "analyses", "analysis" },
            { "theses",   "thesis" },
            { "criteria", "criterion" },
            { "feet",     "foot" },
            { "mice",     "mouse" },
            { "geese",    "goose" },
            { "teeth",    "tooth" },
        };

    /// <summary>
    /// Curated set of "-ves" words that should singularize to "-f" or "-fe".
    /// Words NOT in this list fall through to the trailing-s rule.
    /// Covers the safe subset of NAME-03 -ves handling.
    /// </summary>
    private static readonly Dictionary<string, string> VesExceptions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "knives",  "knife" },
            { "wolves",  "wolf" },
            { "leaves",  "leaf" },
            { "lives",   "life" },
            { "calves",  "calf" },
            { "halves",  "half" },
            { "selves",  "self" },
            { "shelves", "shelf" },
        };

    /// <summary>
    /// Attempts to singularize an English plural identifier following the rule chain:
    /// (1) irregulars dict, (2) -ies→-y, (3) -sses/-shes/-ches/-xes/-zes/-ses→trim es,
    /// (4) -ves→-f/-fe via curated exceptions, (5) trailing -s (not -ss)→trim.
    /// Returns <see langword="null"/> when no rule fires (the caller emits CVJG0051).
    /// Covers requirements NAME-01 (regular suffixes) and NAME-03 (irregulars + fallback).
    /// </summary>
    /// <param name="input">The plural word to singularize. May be null.</param>
    /// <returns>
    /// The singularized form preserving the case of the first character,
    /// or <see langword="null"/> if no rule applies.
    /// </returns>
    public static string? Singularize(this string? input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        // Rule 1: Irregulars dictionary (case-insensitive lookup, case-preserved result)
        if (Irregulars.TryGetValue(input!, out var irregular))
            return PreserveCase(input!, irregular);

        // Rule 2: -ies → -y  (e.g., categories → category, Categories → Category)
        if (input!.Length > 3 && input.EndsWith("ies", StringComparison.Ordinal))
        {
            var stem = input.Substring(0, input.Length - 3) + "y";
            return PreserveCase(input, stem);
        }

        // Rule 3: -sses / -shes / -ches / -xes / -zes → trim "es"
        // Also handles -ses (e.g., buses → bus) when NOT already caught by -sses
        if (input.EndsWith("sses", StringComparison.Ordinal) ||
            input.EndsWith("shes", StringComparison.Ordinal) ||
            input.EndsWith("ches", StringComparison.Ordinal) ||
            input.EndsWith("xes",  StringComparison.Ordinal) ||
            input.EndsWith("zes",  StringComparison.Ordinal) ||
            (input.EndsWith("ses", StringComparison.Ordinal) && input.Length > 4))
        {
            return input.Substring(0, input.Length - 2);
        }

        // Rule 4: -ves → curated exception list
        if (input.EndsWith("ves", StringComparison.Ordinal) &&
            VesExceptions.TryGetValue(input, out var vesSingular))
        {
            return PreserveCase(input, vesSingular);
        }

        // Rule 5: trailing -s (but not -ss)
        if (input.Length > 1 &&
            input.EndsWith("s", StringComparison.Ordinal) &&
            !input.EndsWith("ss", StringComparison.Ordinal))
        {
            return input.Substring(0, input.Length - 1);
        }

        // No rule fired — return null so the caller can emit CVJG0051
        return null;
    }

    /// <summary>
    /// Preserves the case of the first character of <paramref name="original"/>
    /// in <paramref name="singular"/>.
    /// </summary>
    private static string PreserveCase(string original, string singular)
    {
        if (string.IsNullOrEmpty(singular))
            return singular;

        return char.IsUpper(original[0])
            ? char.ToUpperInvariant(singular[0]) + singular.Substring(1)
            : singular;
    }

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
    /// Converts a property name to its accumulator backing-field name for property-backed collection fields.
    /// For example, "Tags" becomes "_tags__property".
    /// </summary>
    /// <param name="name">The property name to convert.</param>
    /// <returns>A property field name in the format _camelCaseName__property.</returns>
    public static string ToPropertyFieldName(this string name)
    {
        return $"_{name.ToCamelCase()}__property";
    }

    /// <summary>
    /// Reverses <see cref="ToParameterFieldName"/> to recover the original camelCase parameter name.
    /// For example, "_wheels__parameter" becomes "wheels".
    /// </summary>
    /// <param name="fieldName">The parameter field name to convert back.</param>
    /// <returns>The original camelCase parameter name.</returns>
    public static string FromParameterFieldName(this string fieldName) =>
        fieldName.Replace("__parameter", "").TrimStart('_').ToCamelCase();

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
    /// Normalizes a member name to a canonical parameter name by stripping leading underscores.
    /// For example, "_wheels" becomes "wheels" and "Scale" stays "Scale".
    /// </summary>
    /// <param name="name">The member name to normalize.</param>
    /// <returns>The name with leading underscores removed.</returns>
    public static string StripLeadingUnderscores(this string name) =>
        name.TrimStart('_');

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
