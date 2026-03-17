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
        ObjectCreationExpressionSyntax returnObjectExpression)
    {
        var returnType = method.Return is TargetTypeReturn targetTypeReturn
            ? ParseTypeName(targetTypeReturn.ReturnTypeDisplayString())
            : returnObjectExpression.Type;

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
            .WithBody(Block(ReturnStatement(returnObjectExpression)));

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
