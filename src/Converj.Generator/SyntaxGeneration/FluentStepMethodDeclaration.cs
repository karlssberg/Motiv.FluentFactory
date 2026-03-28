using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

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
            .WithBody(Block(ReturnStatement(returnObjectExpression)));

        methodDeclaration = AttachParameterList(method, methodDeclaration);

        return StepMethodTypeParameterResolver.AttachTypeParameters(method, knownConstructorParameters, ambientTypeParameters, methodDeclaration)
            .WithLeadingTrivia(GetDocumentationTrivia(method));
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
                    method.MethodParameters.Select(p => p.SourceName.ToCamelCase())),
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
                                    Identifier(parameter.SourceName.ToCamelCase()))
                                .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                                .WithType(
                                    ParseTypeName(parameter.SourceType.ToGlobalDisplayString()))))));
    }

    private static IEnumerable<ArgumentSyntax> CreateStepConstructorArguments(
        IFluentMethod method,
        ParameterSequence knownConstructorParameters)
    {
        // Root methods handle threading via RewriteRootMethodForThreadedParameters in RootTypeDeclaration.
        var threadedArgs = Enumerable.Empty<ArgumentSyntax>();
        if (knownConstructorParameters.Any()
            && method.Return is IFluentStep { ThreadedParameters.IsEmpty: false } nextStep)
        {
            threadedArgs = nextStep.ThreadedParameters
                .Select(b =>
                    Argument(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(b.TargetParameter.Name.ToParameterFieldName()))));
        }

        // Forward property field values from the current step to the next step
        var propertyFieldArgs = Enumerable.Empty<ArgumentSyntax>();
        if (method.Return is RegularFluentStep { PropertyFieldStorage.IsEmpty: false } nextPropStep)
        {
            // Get current step's property fields (the ones we already have)
            // These are fields the current step holds that need to be forwarded
            var currentPropFieldNames = nextPropStep.PropertyFieldStorage
                .Take(nextPropStep.PropertyFieldStorage.Length - (method.MethodParameters.Length > 0 ? 1 : 0))
                .Select(pf =>
                    Argument(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(pf.IdentifierName))));
            propertyFieldArgs = currentPropFieldNames;
        }

        return threadedArgs
            .Concat(knownConstructorParameters
                .Select(parameter =>
                    Argument(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(parameter.Name.ToParameterFieldName())))))
            .Concat(propertyFieldArgs)
            .Concat(
                method.MethodParameters.Select(p => p.SourceName.ToCamelCase())
                    .Select(IdentifierName)
                    .Select(Argument));
    }
}
