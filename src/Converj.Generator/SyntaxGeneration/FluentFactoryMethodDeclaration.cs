using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

internal static class FluentFactoryMethodDeclaration
{
    public static MethodDeclarationSyntax Create(
        IFluentMethod method,
        IFluentStep step)
    {
        var fieldArguments = GetFieldArguments(method);

        var methodArguments = GetMethodArguments(method);

        ExpressionSyntax returnExpression = method is CreationMethod { IsStaticMethodTarget: true } staticCreation
            ? TargetTypeObjectCreationExpression.CreateStaticMethodInvocation(staticCreation, fieldArguments, methodArguments)
            : TargetTypeObjectCreationExpression.Create(method, fieldArguments, methodArguments);

        var methodDeclaration = CreateMethodDeclarationSyntax(method, returnExpression);

        if (!method.TypeParameters.Any())
            return methodDeclaration;

        var typeParameterSyntaxes = GetTypeParameterSyntaxes(method, step);

        if (typeParameterSyntaxes.Length == 0)
            return methodDeclaration;

        return methodDeclaration
            .WithTypeParameterList(
                TypeParameterList(SeparatedList([..typeParameterSyntaxes])));
    }

    private static MethodDeclarationSyntax CreateMethodDeclarationSyntax(
        IFluentMethod method,
        ExpressionSyntax returnExpression)
    {
        var returnType = method.Return is TargetTypeReturn targetTypeReturn
            ? ParseTypeName(targetTypeReturn.ReturnTypeDisplayString())
            : returnExpression is ObjectCreationExpressionSyntax objCreation ? objCreation.Type
            : ParseTypeName(method.Return.IdentifierDisplayString());

        var methodDeclaration = MethodDeclaration(
                returnType,
                Identifier(method.Name))
            .WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(AggressiveInliningAttributeSyntax.Create()))))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword)))
            .WithBody(Block(ReturnStatement(returnExpression)));

        if (method.SourceParameter is not null)
        {
            methodDeclaration = methodDeclaration.WithParameterList(
                ParameterList(SingletonSeparatedList(
                    Parameter(
                            Identifier(method.SourceParameter.Name.ToCamelCase()))
                        .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                        .WithType(
                            ParseTypeName(method.SourceParameter.Type.ToGlobalDisplayString())))));
        }

        return methodDeclaration.WithLeadingTrivia(
            FluentMethodSummaryDocXml.Create(
            [
                method.DocumentationSummary,
                ..FluentMethodSummaryDocXml.GenerateCandidateConstructorTypeSeeAlsoLinks(method.Return.CandidateConstructors)
            ]));
    }

    private static ImmutableArray<TypeParameterSyntax> GetTypeParameterSyntaxes(IFluentMethod method, IFluentStep step)
    {
        return method.TypeParameters
            .Except(step.KnownConstructorParameters
                .SelectMany(parameter => parameter.Type.GetGenericTypeParameters())
                .Select(genericTypeParameters => new FluentTypeParameter(genericTypeParameters)))
            .Select(fluentTypeParameter => fluentTypeParameter.TypeParameterSymbol.ToTypeParameterSyntax())
            .ToImmutableArray();
    }
    private static IEnumerable<ArgumentSyntax> GetMethodArguments(IFluentMethod method)
    {
        return method.MethodParameters
            .Select(ExpressionSyntax (p) =>
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(p.SourceName.ToParameterFieldName())))
            .Select(Argument);
    }

    private static IEnumerable<ArgumentSyntax> GetFieldArguments(IFluentMethod method)
    {
        return method.AvailableParameterFields
            .Select(ExpressionSyntax (p) =>
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(p.SourceName.ToParameterFieldName())))
            .Select(Argument);
    }
}
