using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

/// <summary>
/// Generates method declarations for gateway methods that create all-optional steps
/// using named arguments, allowing callers to specify only the parameter they want to set.
/// </summary>
internal static class OptionalGatewayMethodDeclaration
{
    /// <summary>
    /// Creates a method declaration that constructs an all-optional step using a named argument
    /// for the specified parameter, relying on default values for the remaining parameters.
    /// </summary>
    public static MethodDeclarationSyntax Create(OptionalGatewayMethod method)
    {
        var parameterName = method.SourceParameter.Name.ToCamelCase();
        var returnTypeName = ParseTypeName(method.Return.IdentifierDisplayString());

        var namedArgument = Argument(IdentifierName(parameterName))
            .WithNameColon(NameColon(IdentifierName(parameterName)));

        var body = Block(
            ReturnStatement(
                ObjectCreationExpression(returnTypeName)
                    .WithArgumentList(
                        ArgumentList(SingletonSeparatedList(namedArgument)))));

        var xmlDocTrivia = FluentMethodSummaryDocXml.Create(
            [..FluentMethodSummaryDocXml.GenerateCandidateConstructorTypeSeeAlsoLinks(method.Return.CandidateConstructors)]);

        return MethodDeclaration(returnTypeName, Identifier(method.Name))
            .WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(AggressiveInliningAttributeSyntax.Create()))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                ParameterList(SingletonSeparatedList(
                    Parameter(Identifier(parameterName))
                        .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                        .WithType(ParseTypeName(method.SourceParameter.Type.ToGlobalDisplayString())))))
            .WithBody(body)
            .WithLeadingTrivia(xmlDocTrivia);
    }
}
