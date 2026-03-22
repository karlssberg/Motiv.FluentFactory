using Microsoft.CodeAnalysis;

namespace Converj.Generator;

/// <summary>
/// Extension methods for filtering, union, and set operations on type parameter collections.
/// </summary>
internal static class TypeParameterFilterExtensions
{
    /// <summary>
    /// Returns the union of two type parameter collections, ordered by name.
    /// Uses <see cref="SymbolEqualityComparer.IncludeNullability"/> for equality comparison.
    /// </summary>
    /// <param name="first">The first collection of type parameters.</param>
    /// <param name="second">The second collection of type parameters.</param>
    /// <returns>A union of both collections, ordered by name.</returns>
    public static IEnumerable<ITypeParameterSymbol> Union(
       this IEnumerable<ITypeParameterSymbol> first,
       IEnumerable<ITypeParameterSymbol> second)
    {
        return first
            .Union<ITypeParameterSymbol>(second, SymbolEqualityComparer.IncludeNullability)
            .OrderBy(symbol => symbol.Name);
    }

    /// <summary>
    /// Returns elements from the collection excluding those present in the exclusions,
    /// compared by display string.
    /// </summary>
    /// <param name="collection">The source collection of type parameters.</param>
    /// <param name="exclusions">The type parameters to exclude.</param>
    /// <returns>Elements from the collection not present in exclusions.</returns>
    public static IEnumerable<ITypeParameterSymbol> Except(
       this IEnumerable<ITypeParameterSymbol> collection,
       IEnumerable<ITypeParameterSymbol> exclusions)
    {
        var exclusionSet = new HashSet<string>(exclusions.Select(parameter => parameter.GetEffectiveName()));

        foreach (var item in collection)
        {
           if (!exclusionSet.Contains(item.GetEffectiveName()))
             yield return item;
        }
    }
}
