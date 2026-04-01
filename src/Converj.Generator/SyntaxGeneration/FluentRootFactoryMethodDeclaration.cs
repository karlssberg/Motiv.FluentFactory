using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        var fieldSourcedArguments = GetFieldSourcedArguments(method);
        var methodSourcedArguments = GetMethodSourcedArguments(method);

        ExpressionSyntax returnExpression = method is CreationMethod { IsStaticMethodTarget: true } staticCreation
            ? TargetTypeObjectCreationExpression.CreateStaticMethodInvocation(staticCreation, fieldSourcedArguments, methodSourcedArguments)
            : TargetTypeObjectCreationExpression.Create(method, fieldSourcedArguments, methodSourcedArguments);

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
                            .Select(parameter =>
                                Parameter(
                                        Identifier(parameter.SourceName.ToCamelCase()))
                                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                                    .WithType(
                                        ParseTypeName(parameter.SourceType.ToGlobalDisplayString()))))));
        }

        return methodDeclaration;
    }

    private static IEnumerable<ArgumentSyntax> GetMethodSourcedArguments(IFluentMethod method)
    {
        return method.MethodParameters
            .Select(parameter => IdentifierName(parameter.SourceName.ToCamelCase()))
            .Select(Argument);
    }

    private static IEnumerable<ArgumentSyntax> GetFieldSourcedArguments(IFluentMethod method)
    {
        return method.AvailableParameterFields
            .Select(parameter => IdentifierName(parameter.SourceName.ToCamelCase()))
            .Select(Argument);
    }
}
