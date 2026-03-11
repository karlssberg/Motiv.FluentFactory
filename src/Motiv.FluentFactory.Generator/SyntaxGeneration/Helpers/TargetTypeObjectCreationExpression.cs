using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.SyntaxGeneration.Helpers;

internal static class TargetTypeObjectCreationExpression
{
    public static ObjectCreationExpressionSyntax Create(
        IFluentMethod method,
        IEnumerable<ArgumentSyntax> fieldArguments,
        IEnumerable<ArgumentSyntax> methodArguments)
    {
        var name = ParseName(method.Return.IdentifierDisplayString());

        if (method is MultiMethod multiMethod)
        {
            return CreateMethodOverloadExpression(multiMethod, fieldArguments, methodArguments, name);
        }

        return ObjectCreationExpression(name)
            .WithNewKeyword(Token(SyntaxKind.NewKeyword))
            .WithArgumentList(ArgumentList(SeparatedList([..fieldArguments, ..methodArguments])));
    }

    private static ObjectCreationExpressionSyntax CreateMethodOverloadExpression(
        MultiMethod method,
        IEnumerable<ArgumentSyntax> fieldArguments,
        IEnumerable<ArgumentSyntax> methodArguments,
        TypeSyntax name)
    {
        IEnumerable<ArgumentSyntax> argNodes =
        [
            ..fieldArguments,
            Argument(MultiMethodInvocationExpression.Create(method.ParameterConverter, methodArguments))
        ];

        return ObjectCreationExpression(name)
            .WithNewKeyword(Token(SyntaxKind.NewKeyword))
            .WithArgumentList(ArgumentList(SeparatedList(argNodes)));
    }
}
