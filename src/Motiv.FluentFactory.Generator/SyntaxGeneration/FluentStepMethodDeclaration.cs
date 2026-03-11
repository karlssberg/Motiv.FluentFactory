using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.SyntaxGeneration;

internal static class FluentStepMethodDeclaration
{
    public static MethodDeclarationSyntax Create(
        MultiMethod multiMethod,
        ParameterSequence knownConstructorParameters,
        ImmutableArray<ITypeParameterSymbol>? ambientTypeParameters = null)
    {
        var stepActivationArgs = CreateStepConstructorArguments(multiMethod, knownConstructorParameters);

        var returnObjectExpression = FluentStepCreationExpression.Create(multiMethod, stepActivationArgs);

        return CreateMethodDeclaration(multiMethod, knownConstructorParameters, returnObjectExpression, ambientTypeParameters ?? []);
    }

    public static MethodDeclarationSyntax Create(
        IFluentMethod method,
        ParameterSequence knownConstructorParameters,
        ImmutableArray<ITypeParameterSymbol>? ambientTypeParameters = null)
    {
        var stepActivationArgs = CreateStepConstructorArguments(method, knownConstructorParameters);

        var returnObjectExpression = FluentStepCreationExpression.Create(method, stepActivationArgs);

        return CreateMethodDeclaration(method, knownConstructorParameters, returnObjectExpression, ambientTypeParameters ?? []);
    }

    private static List<object?> GetDocumentationLinesWithParameters(IFluentMethod method)
    {
        var lines = new List<object?>();

        // Add the main documentation summary
        if (!string.IsNullOrWhiteSpace(method.DocumentationSummary))
        {
            lines.Add(method.DocumentationSummary?.Trim());
            lines.Add("");
        }

        // Add constructor type information
        lines.AddRange(FluentMethodSummaryDocXml.GenerateCandidateConstructorTypeSeeAlsoLinks(method.Return.CandidateConstructors).Cast<object?>());

        return lines;
    }

    private static MethodDeclarationSyntax CreateMethodDeclaration(
        IFluentMethod method,
        ParameterSequence knownConstructorParameters,
        ObjectCreationExpressionSyntax returnObjectExpression,
        ImmutableArray<ITypeParameterSymbol> ambientTypeParameters)
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
            .WithLeadingTrivia(GetDocumentationTrivia(method));

        methodDeclaration = AttachParameterList(method, methodDeclaration);

        return StepMethodTypeParameterResolver.AttachTypeParameters(method, knownConstructorParameters, ambientTypeParameters, methodDeclaration);
    }

    /// <summary>
    /// Gets the XML documentation trivia for the method declaration.
    /// </summary>
    private static SyntaxTriviaList GetDocumentationTrivia(IFluentMethod method)
    {
        return method switch
        {
            { ParameterDocumentation: not null, MethodParameters.Length: > 0 } =>
                FluentMethodSummaryDocXml.CreateWithParameters(
                    GetDocumentationLinesWithParameters(method),
                    method.ParameterDocumentation,
                    method.MethodParameters.Select(p => p.ParameterSymbol.Name.ToCamelCase())),
            _ =>
                FluentMethodSummaryDocXml.Create(GetDocumentationLinesWithParameters(method))
        };
    }

    /// <summary>
    /// Attaches the parameter list to the method declaration if the method has parameters.
    /// </summary>
    private static MethodDeclarationSyntax AttachParameterList(
        IFluentMethod method,
        MethodDeclarationSyntax methodDeclaration)
    {
        if (method.MethodParameters.Length == 0)
            return methodDeclaration;

        return methodDeclaration
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

    private static IEnumerable<ArgumentSyntax> CreateStepConstructorArguments(
        IFluentMethod method,
        ParameterSequence knownConstructorParameters)
    {
        return knownConstructorParameters
            .Select(parameter =>
                Argument(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        IdentifierName(parameter.Name.ToParameterFieldName()))))
            .Concat(
                method.MethodParameters.Select(p => p.ParameterSymbol.Name.ToCamelCase())
                    .Select(IdentifierName)
                    .Select(Argument));
    }
}
