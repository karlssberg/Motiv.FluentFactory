using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.Extensions;
using Converj.Generator.Models.Parameters;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

/// <summary>
/// Emits the terminal (creation) method directly on the root type. Used when the
/// fluent chain can construct the target in a single call from the root, with no
/// step struct intermediates. The method takes its arguments via the parameter list
/// rather than reading fields, because the root has no per-call value storage.
/// </summary>
internal static class RootTerminalMethodDeclaration
{
    /// <summary>
    /// Creates a method declaration syntax for a terminal method emitted directly on the root type (the inline construction case where no step struct intermediates are needed).
    /// </summary>
    public static MethodDeclarationSyntax Create(
        IFluentMethod method,
        INamedTypeSymbol rootType)
    {
        var fieldArguments = ExpandArguments(method.AvailableParameterFields);
        var methodArguments = ExpandArguments(method.MethodParameters);

        ExpressionSyntax returnExpression = method is TerminalMethod { IsStaticMethodTarget: true } staticCreation
            ? TargetTypeObjectCreationExpression.CreateStaticMethodInvocation(staticCreation, fieldArguments, methodArguments)
            : TargetTypeObjectCreationExpression.Create(method, fieldArguments, methodArguments);

        var methodDeclaration = CreateBaseMethodDeclaration(method, returnExpression);

        return RootMethodTypeParameterResolver.AttachTypeParametersAndConstraints(methodDeclaration, method, rootType)
            .WithLeadingTrivia(
                FluentMethodSummaryDocXml.Create(
                [
                    method.DocumentationSummary,
                    ..FluentMethodSummaryDocXml.GenerateCandidateTargetTypeSeeAlsoLinks(method.Return.GetAvailableTargets())
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
