using Microsoft.CodeAnalysis;

namespace Converg.Generator;

/// <summary>
/// Extension methods for checking and retrieving attributes from Roslyn symbols.
/// </summary>
internal static class SymbolAttributeExtensions
{
    /// <summary>
    /// Determines whether a symbol has an attribute matching the specified type name.
    /// </summary>
    /// <param name="type">The symbol to check.</param>
    /// <param name="attribute">The attribute type name to look for.</param>
    /// <returns><c>true</c> if the symbol has the specified attribute; otherwise, <c>false</c>.</returns>
    public static bool HasAttribute(this ISymbol type, TypeName attribute) =>
        GetAttributes(type, attribute).Any();

    /// <summary>
    /// Determines whether a symbol has an attribute of the specified generic type.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to look for.</typeparam>
    /// <param name="type">The symbol to check.</param>
    /// <returns><c>true</c> if the symbol has the specified attribute; otherwise, <c>false</c>.</returns>
    public static bool HasAttribute<TAttribute>(this ISymbol type) where TAttribute : Attribute =>
        GetAttributes<TAttribute>(type).Any();

    /// <summary>
    /// Gets all attribute data instances matching the specified type name from a symbol.
    /// </summary>
    /// <param name="type">The symbol to inspect.</param>
    /// <param name="attribute">The attribute type name to match.</param>
    /// <returns>An enumerable of matching <see cref="AttributeData"/> instances.</returns>
    public static IEnumerable<AttributeData> GetAttributes(this ISymbol type, TypeName attribute) =>
        type.GetAttributes()
            .Where(attr => attr.AttributeClass?.ToDisplayString() == attribute);

    /// <summary>
    /// Gets all attribute data instances of the specified generic type from a symbol.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to match.</typeparam>
    /// <param name="type">The symbol to inspect.</param>
    /// <returns>An enumerable of matching <see cref="AttributeData"/> instances.</returns>
    public static IEnumerable<AttributeData> GetAttributes<TAttribute>(this ISymbol type) where TAttribute : Attribute =>
        type.GetAttributes()
            .Where(attr => attr.AttributeClass?.ToDisplayString() == typeof(TAttribute).FullName);
}
