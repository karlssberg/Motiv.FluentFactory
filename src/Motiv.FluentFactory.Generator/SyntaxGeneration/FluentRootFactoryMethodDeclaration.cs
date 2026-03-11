using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.SyntaxGeneration;

internal static class FluentRootFactoryMethodDeclaration
{
    /// <summary>
    /// Creates a method declaration syntax for a root factory method.
    /// </summary>
    public static MethodDeclarationSyntax Create(
        IFluentMethod method,
        INamedTypeSymbol rootType)
    {
        var fieldSourcedArguments = GetFieldSourcedArguments(method);
        var methodSourcedArguments = GetMethodSourcedArguments(method);

        var returnObjectExpression = TargetTypeObjectCreationExpression.Create(
            method,
            fieldSourcedArguments,
            methodSourcedArguments);

        var methodDeclaration = CreateBaseMethodDeclaration(method, returnObjectExpression);

        return AttachTypeParametersAndConstraints(methodDeclaration, method, rootType);
    }

    private static MethodDeclarationSyntax AttachTypeParametersAndConstraints(
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

    private static bool HasTypeParametersToAdd(IFluentMethod method, INamedTypeSymbol rootType)
    {
        var hasMethodTypeParameters = method.TypeParameters.Any();
        var shouldIncludeTargetTypeParameters = rootType?.IsGenericType != true &&
                                               method.Return is TargetTypeReturn targetTypeReturn &&
                                               targetTypeReturn.Constructor.ContainingType.IsGenericType;

        return hasMethodTypeParameters || shouldIncludeTargetTypeParameters;
    }

    private static ImmutableArray<TypeParameterSyntax> GetTypeParameterSyntaxes(
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
    private static ImmutableArray<ITypeParameterSymbol> GetCombinedTypeParameters(
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

        return [..typeParameters];
    }

    private static MethodDeclarationSyntax CreateBaseMethodDeclaration(
        IFluentMethod method,
        ObjectCreationExpressionSyntax returnObjectExpression)
    {
        var methodDeclaration = MethodDeclaration(
                returnObjectExpression.Type,
                Identifier(method.Name))
            .WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(AggressiveInliningAttributeSyntax.Create()))))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword)))
            .WithBody(Block(ReturnStatement(returnObjectExpression)))
            .WithLeadingTrivia(
                FluentMethodSummaryDocXml.Create(
                [
                    method.DocumentationSummary,
                    ..FluentMethodSummaryDocXml.GenerateCandidateConstructorTypeSeeAlsoLinks(method.Return.CandidateConstructors)
                ]));

        if (method.MethodParameters.Length > 0)
        {
            methodDeclaration = methodDeclaration
                .WithParameterList(
                    ParameterList(SeparatedList(
                        method.MethodParameters
                            .Select(parameter =>
                                Parameter(
                                        Identifier(parameter.ParameterSymbol.Name.ToCamelCase()))
                                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                                    .WithType(
                                        ParseTypeName(parameter.ParameterSymbol.Type.ToGlobalDisplayString()))))));
        }

        return methodDeclaration;
    }

    private static IEnumerable<ArgumentSyntax> GetMethodSourcedArguments(IFluentMethod method)
    {
        return method.MethodParameters
            .Select(parameter => IdentifierName(parameter.ParameterSymbol.Name.ToCamelCase()))
            .Select(Argument);
    }

    private static IEnumerable<ArgumentSyntax> GetFieldSourcedArguments(IFluentMethod method)
    {
        return method.AvailableParameterFields
            .Select(parameter => IdentifierName(parameter.ParameterSymbol.Name.ToCamelCase()))
            .Select(Argument);
    }
}
