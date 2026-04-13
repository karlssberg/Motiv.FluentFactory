using System.Collections.Immutable;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

/// <summary>
/// Emits the terminal (creation) method on a step struct. The method reads its
/// constructor argument values from <c>this.{field}</c> accessors on the surrounding
/// step struct and returns either a <c>new T(...)</c> object-creation expression or
/// a static-method invocation, depending on the target kind.
/// </summary>
internal static class StepTerminalMethodDeclaration
{
    public static MethodDeclarationSyntax Create(
        IFluentMethod method,
        IFluentStep step)
    {
        var fieldArguments = GetFieldArguments(method);

        var methodArguments = GetMethodArguments(method);

        ExpressionSyntax returnExpression = method is TerminalMethod { IsStaticMethodTarget: true } staticCreation
            ? TargetTypeObjectCreationExpression.CreateStaticMethodInvocation(staticCreation, fieldArguments, methodArguments)
            : TargetTypeObjectCreationExpression.Create(method, fieldArguments, methodArguments);

        var methodDeclaration = CreateMethodDeclarationSyntax(method, returnExpression);

        if (!method.TypeParameters.Any())
            return methodDeclaration;

        var typeParameterSyntaxes = GetTypeParameterSyntaxes(method, step);

        if (typeParameterSyntaxes.Length == 0)
            return methodDeclaration;

        return methodDeclaration
            .WithTypeParameterList(
                TypeParameterList(SeparatedList([..typeParameterSyntaxes])));
    }

    private static MethodDeclarationSyntax CreateMethodDeclarationSyntax(
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

        if (method.SourceParameter is not null)
        {
            methodDeclaration = methodDeclaration.WithParameterList(
                ParameterList(SingletonSeparatedList(
                    Parameter(
                            Identifier(method.SourceParameter.Name.ToCamelCase()))
                        .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                        .WithType(
                            ParseTypeName(method.SourceParameter.Type.ToGlobalDisplayString())))));
        }

        return methodDeclaration.WithLeadingTrivia(
            FluentMethodSummaryDocXml.Create(
            [
                method.DocumentationSummary,
                ..FluentMethodSummaryDocXml.GenerateCandidateTargetTypeSeeAlsoLinks(method.Return.GetAvailableTargets())
            ]));
    }

    private static ImmutableArray<TypeParameterSyntax> GetTypeParameterSyntaxes(IFluentMethod method, IFluentStep step)
    {
        return method.TypeParameters
            .Except(step.KnownConstructorParameters
                .SelectMany(parameter => parameter.Type.GetGenericTypeParameters())
                .Select(genericTypeParameters => new FluentTypeParameter(genericTypeParameters)))
            .Select(fluentTypeParameter => fluentTypeParameter.TypeParameterSymbol.ToTypeParameterSyntax())
            .ToImmutableArray();
    }
    private static IEnumerable<ArgumentSyntax> GetMethodArguments(IFluentMethod method) =>
        ExpandFieldAccessArguments(method.MethodParameters);

    private static IEnumerable<ArgumentSyntax> GetFieldArguments(IFluentMethod method) =>
        ExpandFieldAccessArguments(method.AvailableParameterFields);

    private static IEnumerable<ArgumentSyntax> ExpandFieldAccessArguments(
        IEnumerable<FluentMethodParameter> parameters) =>
        parameters
            .Select(p => RepackTupleFieldArgument(p, p.SourceName.ToParameterFieldName()))
            .Select(Argument);

    /// <summary>
    /// For tuple parameters, creates a tuple literal expression from the individual element fields.
    /// For regular parameters, creates a simple field access.
    /// </summary>
    private static ExpressionSyntax RepackTupleFieldArgument(FluentMethodParameter parameter, string fieldName)
    {
        if (parameter is TupleFluentMethodParameter tuple)
        {
            var elementAccesses = tuple.Elements.Select(element =>
                Argument(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        IdentifierName(element.Name.ToParameterFieldName()))));

            return TupleExpression(SeparatedList(elementAccesses));
        }

        return MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ThisExpression(),
            IdentifierName(fieldName));
    }
}
