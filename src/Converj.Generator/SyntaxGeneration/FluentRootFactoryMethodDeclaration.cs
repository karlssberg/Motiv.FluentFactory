using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.Models.Parameters;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

internal static class FluentRootFactoryMethodDeclaration
{
    /// <summary>
    /// Creates a method declaration syntax for a root factory method.
    /// </summary>
    public static MethodDeclarationSyntax Create(
        IFluentMethod method,
        INamedTypeSymbol rootType)
    {
        var fieldArguments = ExpandArguments(method.AvailableParameterFields);
        var methodArguments = ExpandArguments(method.MethodParameters);

        ExpressionSyntax returnExpression = method is CreationMethod { IsStaticMethodTarget: true } staticCreation
            ? TargetTypeObjectCreationExpression.CreateStaticMethodInvocation(staticCreation, fieldArguments, methodArguments)
            : TargetTypeObjectCreationExpression.Create(method, fieldArguments, methodArguments);

        var methodDeclaration = CreateBaseMethodDeclaration(method, returnExpression);

        return RootMethodTypeParameterResolver.AttachTypeParametersAndConstraints(methodDeclaration, method, rootType)
            .WithLeadingTrivia(
                FluentMethodSummaryDocXml.Create(
                [
                    method.DocumentationSummary,
                    ..FluentMethodSummaryDocXml.GenerateCandidateConstructorTypeSeeAlsoLinks(method.Return.CandidateConstructors)
                ]));
    }

    private static MethodDeclarationSyntax CreateBaseMethodDeclaration(
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

        if (method.MethodParameters.Length > 0)
        {
            methodDeclaration = methodDeclaration
                .WithParameterList(
                    ParameterList(SeparatedList(
                        method.MethodParameters
                            .SelectMany(FluentStepMethodDeclaration.ExpandMethodParameter))));
        }

        return methodDeclaration;
    }

    private static IEnumerable<ArgumentSyntax> ExpandArguments(
        IEnumerable<FluentMethodParameter> parameters) =>
        parameters.SelectMany(FluentStepMethodDeclaration.ExpandMethodParameterArguments);
}
