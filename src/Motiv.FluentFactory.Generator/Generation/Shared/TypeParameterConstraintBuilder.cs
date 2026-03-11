using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.Generation.Shared;

/// <summary>
/// Builds type parameter constraint clauses from type parameter symbols.
/// Centralizes constraint generation to ensure consistent ordering and
/// correct global:: qualification across all declaration types.
/// </summary>
internal static class TypeParameterConstraintBuilder
{
    /// <summary>
    /// Creates type parameter constraint clauses for the given type parameters.
    /// Constraints are ordered: reference type (class), value type (struct),
    /// type constraints, constructor constraint (new()).
    /// All type constraints use globally-qualified names via <see cref="SymbolExtensions.ToGlobalDisplayString"/>.
    /// </summary>
    /// <param name="typeParameters">The type parameters to generate constraint clauses for.</param>
    /// <returns>An immutable array of constraint clause syntax nodes for type parameters that have constraints.</returns>
    public static ImmutableArray<TypeParameterConstraintClauseSyntax> Create(
        ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        if (typeParameters.IsDefaultOrEmpty)
            return [];

        var constraintClauses = typeParameters
            .Where(tp => tp.HasConstructorConstraint ||
                         tp.HasReferenceTypeConstraint ||
                         tp.HasValueTypeConstraint ||
                         tp.ConstraintTypes.Length > 0)
            .Select(BuildConstraintClause)
            .ToImmutableArray();

        return constraintClauses;
    }

    private static TypeParameterConstraintClauseSyntax BuildConstraintClause(ITypeParameterSymbol typeParameter)
    {
        var constraints = new List<TypeParameterConstraintSyntax>();

        // Add reference type constraint (class)
        if (typeParameter.HasReferenceTypeConstraint)
            constraints.Add(ClassOrStructConstraint(SyntaxKind.ClassConstraint));

        // Add value type constraint (struct)
        if (typeParameter.HasValueTypeConstraint)
            constraints.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));

        // Add type constraints (always using global:: qualification)
        foreach (var constraintType in typeParameter.ConstraintTypes)
        {
            constraints.Add(TypeConstraint(ParseTypeName(constraintType.ToGlobalDisplayString())));
        }

        // Add constructor constraint (new())
        if (typeParameter.HasConstructorConstraint)
            constraints.Add(ConstructorConstraint());

        return TypeParameterConstraintClause(typeParameter.Name)
            .WithConstraints(SeparatedList(constraints));
    }
}
