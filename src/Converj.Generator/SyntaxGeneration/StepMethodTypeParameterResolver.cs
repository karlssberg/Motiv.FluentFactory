using System.Collections.Immutable;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

/// <summary>
/// Resolves and attaches type parameters and constraint clauses
/// for fluent step method declarations.
/// </summary>
internal static class StepMethodTypeParameterResolver
{
    /// <summary>
    /// Filters the method's type parameters by excluding known constructor parameters
    /// and ambient type parameters, returning the type parameter syntaxes to add to the method.
    /// </summary>
    internal static ImmutableArray<TypeParameterSyntax> GetMethodTypeParameterSyntaxes(
        IFluentMethod method,
        ParameterSequence knownTargetParameters,
        ImmutableArray<ITypeParameterSymbol> ambientTypeParameters)
    {
        var rootTypeParametersSet = new HashSet<FluentTypeParameter>(
            ambientTypeParameters.Select(tp => new FluentTypeParameter(tp)));

        return method.TypeParameters
            .Except(knownTargetParameters
                .SelectMany(parameter => parameter.Type.GetGenericTypeParameters())
                .Select(genericTypeParameters => new FluentTypeParameter(genericTypeParameters)))
            .Except(rootTypeParametersSet)
            .Select(fluentTypeParameter => fluentTypeParameter.TypeParameterSymbol.ToTypeParameterSyntax())
            .ToImmutableArray();
    }

    /// <summary>
    /// Attaches type parameter list and constraint clauses to the method declaration
    /// if the method has type parameters that are not already covered by constructor
    /// parameters or ambient type parameters.
    /// </summary>
    internal static MethodDeclarationSyntax AttachTypeParameters(
        IFluentMethod method,
        ParameterSequence knownTargetParameters,
        ImmutableArray<ITypeParameterSymbol> ambientTypeParameters,
        MethodDeclarationSyntax methodDeclaration)
    {
        if (!method.TypeParameters.Any())
            return methodDeclaration;

        var typeParameterSyntaxes = GetMethodTypeParameterSyntaxes(method, knownTargetParameters, ambientTypeParameters);

        if (typeParameterSyntaxes.Length == 0)
            return methodDeclaration;

        methodDeclaration = methodDeclaration.WithTypeParameterList(
            TypeParameterList(SeparatedList([..typeParameterSyntaxes])));

        var combinedTypeParameters = GetCombinedTypeParameters(method, ambientTypeParameters);
        var constraintClauses = TypeParameterConstraintBuilder.Create(combinedTypeParameters);
        if (constraintClauses.Length > 0)
        {
            methodDeclaration = methodDeclaration
                .WithConstraintClauses(List(constraintClauses));
        }

        return methodDeclaration;
    }

    /// <summary>
    /// Collects type parameters from both the target type (for non-generic root types)
    /// and the method's own type parameters into a single array for constraint building.
    /// </summary>
    internal static ImmutableArray<ITypeParameterSymbol> GetCombinedTypeParameters(
        IFluentMethod method,
        ImmutableArray<ITypeParameterSymbol> ambientTypeParameters)
    {
        var typeParameters = new List<ITypeParameterSymbol>();

        // Include target type parameters for non-generic root types
        if (ambientTypeParameters.IsEmpty && method.Return is TargetTypeReturn targetTypeReturn &&
            targetTypeReturn.Method.ContainingType.IsGenericType)
        {
            typeParameters.AddRange(targetTypeReturn.Method.ContainingType.OriginalDefinition.TypeParameters);
        }

        // Include method type parameters
        typeParameters.AddRange(method.TypeParameters.Select(tp => tp.TypeParameterSymbol));

        return [..typeParameters];
    }
}
