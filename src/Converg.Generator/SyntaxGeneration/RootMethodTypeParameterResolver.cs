using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converg.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converg.Generator.SyntaxGeneration;

/// <summary>
/// Resolves and attaches type parameters and constraint clauses
/// for fluent root factory method declarations.
/// </summary>
internal static class RootMethodTypeParameterResolver
{
    /// <summary>
    /// Attaches type parameter list and constraint clauses to a root factory method declaration.
    /// </summary>
    internal static MethodDeclarationSyntax AttachTypeParametersAndConstraints(
        MethodDeclarationSyntax methodDeclaration,
        IFluentMethod method,
        INamedTypeSymbol rootType)
    {
        if (!HasTypeParametersToAdd(method, rootType))
            return methodDeclaration;

        var typeParameterSyntaxes = GetTypeParameterSyntaxes(method, rootType);

        if (typeParameterSyntaxes.Length == 0)
            return methodDeclaration;

        var methodWithTypeParameters = methodDeclaration
            .WithTypeParameterList(
                TypeParameterList(SeparatedList([..typeParameterSyntaxes])));

        var combinedTypeParameters = GetCombinedTypeParameters(method, rootType);
        var constraintClauses = TypeParameterConstraintBuilder.Create(combinedTypeParameters);
        if (constraintClauses.Length > 0)
        {
            methodWithTypeParameters = methodWithTypeParameters
                .WithConstraintClauses(List(constraintClauses));
        }

        return methodWithTypeParameters;
    }

    /// <summary>
    /// Determines whether the method has type parameters that need to be added to the declaration.
    /// </summary>
    internal static bool HasTypeParametersToAdd(IFluentMethod method, INamedTypeSymbol rootType)
    {
        var hasMethodTypeParameters = method.TypeParameters.Any();
        var shouldIncludeTargetTypeParameters = rootType?.IsGenericType != true &&
                                               method.Return is TargetTypeReturn targetTypeReturn &&
                                               targetTypeReturn.Constructor.ContainingType.IsGenericType;

        return hasMethodTypeParameters || shouldIncludeTargetTypeParameters;
    }

    /// <summary>
    /// Gets the combined type parameter syntaxes from both target type and method type parameters.
    /// </summary>
    internal static ImmutableArray<TypeParameterSyntax> GetTypeParameterSyntaxes(
        IFluentMethod method,
        INamedTypeSymbol? rootType)
    {
        var shouldIncludeTargetTypeParameters = rootType?.IsGenericType != true;

        var targetTypeParameterSyntaxes = shouldIncludeTargetTypeParameters
            ? method.Return switch
            {
                TargetTypeReturn targetTypeReturn => targetTypeReturn.Constructor.ContainingType.OriginalDefinition.TypeParameters
                    .Select(typeParameterSymbol => typeParameterSymbol.ToTypeParameterSyntax()),
                _ => []
            }
            : [];

        var accumulatedTypeParameterSyntaxes = method.TypeParameters
            .Select(typeParameter => typeParameter.TypeParameterSymbol.ToTypeParameterSyntax());

        var allTypeParameters = accumulatedTypeParameterSyntaxes
            .Concat(targetTypeParameterSyntaxes)
            .DistinctBy(typeParameter => typeParameter.Identifier.Text);

        return [..allTypeParameters];
    }

    /// <summary>
    /// Collects type parameters from both the target type (for non-generic root types)
    /// and the method's own type parameters into a single array for constraint building.
    /// </summary>
    internal static ImmutableArray<ITypeParameterSymbol> GetCombinedTypeParameters(
        IFluentMethod method,
        INamedTypeSymbol? rootType)
    {
        var typeParameters = new List<ITypeParameterSymbol>();

        // Include target type parameters for non-generic root types
        if (rootType?.IsGenericType != true && method.Return is TargetTypeReturn targetTypeReturn &&
            targetTypeReturn.Constructor.ContainingType.IsGenericType)
        {
            typeParameters.AddRange(targetTypeReturn.Constructor.ContainingType.OriginalDefinition.TypeParameters);
        }

        // Include method type parameters
        typeParameters.AddRange(method.TypeParameters.Select(tp => tp.TypeParameterSymbol));

        return [..typeParameters.DistinctBy(tp => tp.Name)];
    }
}
