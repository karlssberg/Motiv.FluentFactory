using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.SyntaxGeneration;

/// <summary>
/// Generates method declarations for optional fluent setter methods that either mutate a field
/// or return a new instance of the same step type.
/// </summary>
internal static class OptionalFluentMethodDeclaration
{
    /// <summary>
    /// Creates a method declaration for an optional parameter setter.
    /// For all-optional steps, returns a new instance to preserve readonly fields and enable inlining.
    /// For regular steps, mutates the field and returns this.
    /// </summary>
    public static MethodDeclarationSyntax Create(OptionalFluentMethod method, IFluentStep step)
    {
        return step is RegularFluentStep { IsAllOptionalStep: true }
            ? CreateImmutable(method, step)
            : CreateMutable(method, step);
    }

    /// <summary>
    /// Creates a method that returns a new instance with the updated parameter value,
    /// preserving readonly fields for better aggressive inlining support.
    /// </summary>
    private static MethodDeclarationSyntax CreateImmutable(OptionalFluentMethod method, IFluentStep step)
    {
        var parameterName = method.SourceParameter.Name.ToCamelCase();
        var returnTypeName = ParseTypeName(step.IdentifierDisplayString());

        var arguments = step.KnownConstructorParameters.Select(p =>
            SymbolEqualityComparer.Default.Equals(p, method.SourceParameter)
                ? Argument(IdentifierName(parameterName))
                : Argument(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        IdentifierName(p.Name.ToParameterFieldName()))));

        var body = Block(
            ReturnStatement(
                ObjectCreationExpression(returnTypeName)
                    .WithArgumentList(ArgumentList(SeparatedList(arguments)))));

        return CreateMethodSyntax(method, step, returnTypeName, parameterName, body);
    }

    /// <summary>
    /// Creates a method that mutates the field and returns this.
    /// Used for regular steps where some fields are mutable optional parameters.
    /// </summary>
    private static MethodDeclarationSyntax CreateMutable(OptionalFluentMethod method, IFluentStep step)
    {
        var parameterName = method.SourceParameter.Name.ToCamelCase();
        var fieldName = method.SourceParameter.Name.ToParameterFieldName();
        var returnTypeName = ParseTypeName(step.IdentifierDisplayString());

        var body = Block(
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(fieldName)),
                    IdentifierName(parameterName))),
            ReturnStatement(ThisExpression()));

        return CreateMethodSyntax(method, step, returnTypeName, parameterName, body);
    }

    private static MethodDeclarationSyntax CreateMethodSyntax(
        OptionalFluentMethod method,
        IFluentStep step,
        TypeSyntax returnTypeName,
        string parameterName,
        BlockSyntax body)
    {
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
                        .WithType(ParseTypeName(method.SourceParameter.Type.ToGlobalDisplayString())))))
            .WithBody(body)
            .WithLeadingTrivia(xmlDocTrivia);
    }
}
