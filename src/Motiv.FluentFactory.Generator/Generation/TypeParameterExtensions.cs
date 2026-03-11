using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Motiv.FluentFactory.Generator;

/// <summary>
/// Extension methods for extracting, filtering, and converting type parameters
/// from Roslyn symbol types.
/// </summary>
internal static class TypeParameterExtensions
{
    /// <summary>
    /// Gets the generic type parameter syntax list for a collection of type symbols.
    /// </summary>
    /// <param name="types">The type symbols to extract generic type parameters from.</param>
    /// <returns>An enumerable of <see cref="TypeParameterSyntax"/> nodes.</returns>
    public static IEnumerable<TypeParameterSyntax> GetGenericTypeParameterSyntaxList(this IEnumerable<ITypeSymbol> types)
    {
        return types.GetGenericTypeParameters()
            .Select(ToTypeParameterSyntax);
    }

    /// <summary>
    /// Gets the generic type parameter syntax list for a single type symbol.
    /// </summary>
    /// <param name="type">The type symbol to extract generic type parameters from.</param>
    /// <returns>An enumerable of <see cref="TypeParameterSyntax"/> nodes.</returns>
    public static IEnumerable<TypeParameterSyntax> GetGenericTypeParameterSyntaxList(this ITypeSymbol type)
    {
        return new[] { type }.GetGenericTypeParameterSyntaxList();
    }

    /// <summary>
    /// Gets the distinct generic type parameters from a collection of type symbols.
    /// </summary>
    /// <param name="type">The type symbols to extract generic type parameters from.</param>
    /// <returns>An enumerable of distinct <see cref="ITypeParameterSymbol"/> instances.</returns>
    public static IEnumerable<ITypeParameterSymbol> GetGenericTypeParameters(this IEnumerable<ITypeSymbol> type)
    {
        return type
            .SelectMany(symbol => symbol.GetGenericTypeParameters())
            .DistinctBy(symbol => symbol.ToDisplayString());
    }

    /// <summary>
    /// Gets the generic type parameters from a single type symbol, recursively
    /// extracting from named type arguments.
    /// </summary>
    /// <param name="type">The type symbol to extract generic type parameters from.</param>
    /// <returns>An enumerable of <see cref="ITypeParameterSymbol"/> instances.</returns>
    public static IEnumerable<ITypeParameterSymbol> GetGenericTypeParameters(this ITypeSymbol type)
    {
        return type switch
        {
            ITypeParameterSymbol typeParameter => [typeParameter],
            INamedTypeSymbol namedType => namedType.TypeArguments
                .SelectMany(typeArg => typeArg.GetGenericTypeParameters())
                .Distinct<ITypeParameterSymbol>(SymbolEqualityComparer.Default),
            _ => []
        };
    }

    /// <summary>
    /// Converts a type parameter symbol to its corresponding syntax representation.
    /// </summary>
    /// <param name="typeParameter">The type parameter symbol to convert.</param>
    /// <returns>A <see cref="TypeParameterSyntax"/> node.</returns>
    public static TypeParameterSyntax ToTypeParameterSyntax(this ITypeParameterSymbol typeParameter)
    {
        var typeParameterSyntax = SyntaxFactory.TypeParameter(SyntaxFactory.Identifier(typeParameter.Name));

        // Add constraints if they exist
        var constraints = new List<TypeParameterConstraintSyntax>();

        // Add value type constraint
        if (typeParameter.HasValueTypeConstraint)
        {
            constraints.Add(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.StructConstraint));
        }

        // Add reference type constraint
        if (typeParameter.HasReferenceTypeConstraint)
        {
            constraints.Add(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint));
        }

        // Add constructor constraint
        if (typeParameter.HasConstructorConstraint)
        {
            constraints.Add(SyntaxFactory.ConstructorConstraint());
        }

        // Add type constraints
        foreach (var constraintType in typeParameter.ConstraintTypes)
        {
            constraints.Add(SyntaxFactory.TypeConstraint(
                SyntaxFactory.ParseTypeName(constraintType.ToGlobalDisplayString())));
        }

        return typeParameterSyntax;
    }

    /// <summary>
    /// Gets the generic type arguments from a type symbol, recursively extracting
    /// type parameter symbols from named type arguments.
    /// </summary>
    /// <param name="type">The type symbol to extract type arguments from.</param>
    /// <returns>An enumerable of distinct <see cref="ITypeParameterSymbol"/> instances.</returns>
    public static IEnumerable<ITypeParameterSymbol> GetGenericTypeArguments(this ITypeSymbol type)
    {
        return GenericTypeArgumentsInternal(type).DistinctBy(t => t.Name);
    }

    private static IEnumerable<ITypeParameterSymbol> GenericTypeArgumentsInternal(ITypeSymbol type)
    {
        return type switch
        {
            ITypeParameterSymbol typeParameter => [typeParameter],
            INamedTypeSymbol namedType => namedType.TypeArguments
                .SelectMany(t => t.GetGenericTypeArguments()),
            _ => []
        };
    }

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
        var exclusionSet = new HashSet<string>(exclusions.Select(parameter => parameter.ToDisplayString()));

        foreach (var item in collection)
        {
           if (!exclusionSet.Contains(item.ToDisplayString()))
             yield return item;
        }
    }
}
