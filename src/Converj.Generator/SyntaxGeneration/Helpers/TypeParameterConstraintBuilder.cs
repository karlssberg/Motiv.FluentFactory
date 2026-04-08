using System.Collections.Immutable;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration.Helpers;

/// <summary>
/// Builds type parameter constraint clauses from type parameter symbols.
/// Centralizes constraint generation to ensure consistent ordering and
/// correct global:: qualification across all declaration types.
/// </summary>
internal static class TypeParameterConstraintBuilder
{
    /// <summary>
    /// Creates type parameter constraint clauses using effective (aliased) names.
    /// Use this overload for generated step structs where type parameters are declared with effective names.
    /// </summary>
    /// <param name="typeParameters">The type parameters to generate constraint clauses for.</param>
    /// <returns>An immutable array of constraint clause syntax nodes for type parameters that have constraints.</returns>
    public static ImmutableArray<TypeParameterConstraintClauseSyntax> Create(
        ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        return Create(typeParameters, useEffectiveNames: true);
    }

    /// <summary>
    /// Creates type parameter constraint clauses, optionally using effective (aliased) names.
    /// </summary>
    /// <param name="typeParameters">The type parameters to generate constraint clauses for.</param>
    /// <param name="useEffectiveNames">
    /// When true, uses effective names (from [As] aliases) for constraint clause names
    /// and type references (for generated step structs). When false, uses original names (for root types
    /// and existing partial types where declarations must match the original type parameter names).
    /// </param>
    /// <returns>An immutable array of constraint clause syntax nodes for type parameters that have constraints.</returns>
    public static ImmutableArray<TypeParameterConstraintClauseSyntax> Create(
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        bool useEffectiveNames)
    {
        if (typeParameters.IsDefaultOrEmpty)
            return [];

        var constraintClauses = typeParameters
            .Where(tp => tp.HasConstructorConstraint ||
                         tp.HasReferenceTypeConstraint ||
                         tp.HasValueTypeConstraint ||
                         tp.ConstraintTypes.Length > 0)
            .Select(tp => BuildConstraintClause(tp, useEffectiveNames))
            .ToImmutableArray();

        return constraintClauses;
    }

    private static TypeParameterConstraintClauseSyntax BuildConstraintClause(
        ITypeParameterSymbol typeParameter,
        bool useEffectiveNames)
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
            var displayString = useEffectiveNames
                ? constraintType.ToGlobalDisplayString()
                : constraintType.ToGlobalOriginalDisplayString();
            constraints.Add(TypeConstraint(ParseTypeName(displayString)));
        }

        // Add constructor constraint (new())
        if (typeParameter.HasConstructorConstraint)
            constraints.Add(ConstructorConstraint());

        var name = useEffectiveNames
            ? typeParameter.GetEffectiveName()
            : typeParameter.Name;

        return TypeParameterConstraintClause(name)
            .WithConstraints(SeparatedList(constraints));
    }
}
