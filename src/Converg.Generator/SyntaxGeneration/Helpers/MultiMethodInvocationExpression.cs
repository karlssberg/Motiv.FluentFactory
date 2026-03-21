using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converg.Generator.SyntaxGeneration.Helpers;

internal static class MultiMethodInvocationExpression
{
    public static InvocationExpressionSyntax Create(
        IMethodSymbol parameterConverterMethod,
        IEnumerable<ArgumentSyntax> arguments)
    {
        SimpleNameSyntax identifierName = parameterConverterMethod.IsGenericMethod
            ? GenericName(parameterConverterMethod.Name)
                .WithTypeArgumentList(
                    TypeArgumentList(SeparatedList<TypeSyntax>(
                        parameterConverterMethod.TypeArguments
                            .Select(t => ParseTypeName(t.ToGlobalDisplayString()))))
                )
            : IdentifierName(parameterConverterMethod.Name);

        var invocationExpression = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ParseTypeName(parameterConverterMethod.ContainingType.ToGlobalDisplayString()),
                    identifierName))
            .WithArgumentList(ArgumentList(SeparatedList(arguments)));

        return invocationExpression;
    }
}
