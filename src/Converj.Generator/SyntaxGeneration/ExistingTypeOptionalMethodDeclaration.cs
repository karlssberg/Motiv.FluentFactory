using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

/// <summary>
/// Generates optional fluent setter methods on ExistingTypeFluentSteps.
/// Produces a method that creates a new instance with the optional parameter replaced,
/// using the step's resolved value storage for all other parameters.
/// </summary>
internal static class ExistingTypeOptionalMethodDeclaration
{
    /// <summary>
    /// Creates a method declaration for an optional parameter setter on an existing partial type.
    /// The method creates a new instance, substituting the method parameter for the matching
    /// value storage entry while preserving all other stored values.
    /// </summary>
    public static MethodDeclarationSyntax Create(OptionalFluentMethod method, IFluentStep step)
    {
        var parameterName = method.SourceParameter.Name.ToCamelCase();
        var returnTypeName = ParseTypeName(step.IdentifierDisplayString());

        var arguments = step.ValueStorage
            .Select(kvp =>
                FluentParameterComparer.Default.Equals(kvp.Key, method.SourceParameter)
                    ? Argument(IdentifierName(parameterName))
                    : CreateStorageArgument(kvp.Value));

        var body = Block(
            ReturnStatement(
                ObjectCreationExpression(returnTypeName)
                    .WithArgumentList(ArgumentList(SeparatedList(arguments)))));

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

    private static ArgumentSyntax CreateStorageArgument(IFluentValueStorage storage) =>
        Argument(storage switch
        {
            PrimaryConstructorParameterStorage =>
                IdentifierName(storage.IdentifierName),
            FieldStorage or PropertyStorage =>
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(storage.IdentifierName)),
            _ =>
                DefaultExpression(ParseTypeName(storage.Type.ToGlobalDisplayString()))
        });
}
