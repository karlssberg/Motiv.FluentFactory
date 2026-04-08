using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

/// <summary>
/// Generates method declarations for optional [FluentMethod] property setter methods
/// that mutate a field and return this.
/// </summary>
internal static class OptionalPropertyFluentMethodDeclaration
{
    /// <summary>
    /// Creates a mutable setter method for an optional property.
    /// Assigns the parameter value to the backing field and returns this.
    /// </summary>
    public static MethodDeclarationSyntax Create(OptionalPropertyFluentMethod method, IFluentStep step)
    {
        var parameterName = method.SourceProperty.Name.ToCamelCase();
        var fieldName = method.FieldStorage.IdentifierName;
        var returnTypeName = ParseTypeName(step.IdentifierDisplayString());

        var body = Block(
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(fieldName)),
                    IdentifierName(parameterName))),
            ReturnStatement(ThisExpression()));

        var xmlDocTrivia = FluentMethodSummaryDocXml.Create(
            [..FluentMethodSummaryDocXml.GenerateCandidateConstructorTypeSeeAlsoLinks(step.CandidateConstructors)]);

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
                        .WithType(ParseTypeName(method.SourceProperty.Type.ToGlobalDisplayString())))))
            .WithBody(body)
            .WithLeadingTrivia(xmlDocTrivia);
    }
}
