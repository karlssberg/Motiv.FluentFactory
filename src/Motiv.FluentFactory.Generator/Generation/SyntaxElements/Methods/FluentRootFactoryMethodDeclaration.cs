using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.Generation.Shared;
using Motiv.FluentFactory.Generator.Model.Methods;
using Motiv.FluentFactory.Generator.Model.Steps;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.Generation.SyntaxElements.Methods;

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

        var constraintClauses = GetConstraintClauses(method, rootType);
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

    private static ImmutableArray<TypeParameterConstraintClauseSyntax> GetConstraintClauses(
        IFluentMethod method,
        INamedTypeSymbol? rootType)
    {
        var shouldIncludeTargetTypeConstraints = rootType?.IsGenericType != true;

        var constraintClauses = new List<TypeParameterConstraintClauseSyntax>();

        if (shouldIncludeTargetTypeConstraints && method.Return is TargetTypeReturn targetTypeReturn)
        {
            foreach (var typeParam in targetTypeReturn.Constructor.ContainingType.OriginalDefinition.TypeParameters)
            {
                var clause = BuildConstraintClause(typeParam);
                if (clause is not null)
                    constraintClauses.Add(clause);
            }
        }

        foreach (var typeParam in method.TypeParameters)
        {
            var clause = BuildConstraintClause(typeParam.TypeParameterSymbol);
            if (clause is not null)
                constraintClauses.Add(clause);
        }

        return [..constraintClauses];
    }

    private static TypeParameterConstraintClauseSyntax? BuildConstraintClause(ITypeParameterSymbol typeParam)
    {
        var constraints = new List<TypeParameterConstraintSyntax>();

        if (typeParam.HasValueTypeConstraint)
            constraints.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));

        if (typeParam.HasReferenceTypeConstraint)
            constraints.Add(ClassOrStructConstraint(SyntaxKind.ClassConstraint));

        if (typeParam.HasConstructorConstraint)
            constraints.Add(ConstructorConstraint());

        foreach (var constraintType in typeParam.ConstraintTypes)
            constraints.Add(TypeConstraint(ParseTypeName(constraintType.ToGlobalDisplayString())));

        if (constraints.Count == 0)
            return null;

        return TypeParameterConstraintClause(IdentifierName(typeParam.Name))
            .WithConstraints(SeparatedList(constraints));
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
