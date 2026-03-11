using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.SyntaxGeneration.Helpers;

internal static class FluentStepCreationExpression
{
    public  static ObjectCreationExpressionSyntax Create(
        IFluentMethod method,
        IEnumerable<ArgumentSyntax> arguments)
    {
        return method switch
        {
            MultiMethod multiMethod => CreateMultiMethod(multiMethod, arguments),
            _ => CreateDefaultMethod(method, arguments)
        };
    }

    private static ObjectCreationExpressionSyntax CreateMultiMethod(
        MultiMethod method,
        IEnumerable<ArgumentSyntax> arguments)
    {
        var typeArgMappings = GenericAnalysis
            .GetGenericParameterMappings(method.SourceParameter.Type, method.ParameterConverter.ReturnType)
            .ToDictionary(pair => new FluentType(pair.Key), pair => pair.Value);

        var name = ParseName(
            method.Return.IdentifierDisplayString(typeArgMappings));

        return CreateMethodOverloadExpression(method, arguments, name);
    }

    private static ObjectCreationExpressionSyntax CreateDefaultMethod(
        IFluentMethod method,
        IEnumerable<ArgumentSyntax> arguments)
    {
        NameSyntax name = ParseName(method.Return.IdentifierDisplayString());
        return CreateObjectCreationExpression(arguments, name);
    }

    private static ObjectCreationExpressionSyntax CreateObjectCreationExpression(IEnumerable<ArgumentSyntax> arguments, NameSyntax name)
    {
        return ObjectCreationExpression(name)
            .WithNewKeyword(
                Token(SyntaxKind.NewKeyword))
            .WithArgumentList(ArgumentList(SeparatedList(arguments)));
    }

    private static ObjectCreationExpressionSyntax CreateMethodOverloadExpression(
        MultiMethod method,
        IEnumerable<ArgumentSyntax> arguments,
        TypeSyntax name)
    {
        var argumentList = arguments.ToList();
        var parameterConverterMethod = method.ParameterConverter;

        var fieldArgumentsIndex = argumentList.Count - method.MethodParameters.Length;
        var fieldSourcedArguments = argumentList.Take(fieldArgumentsIndex);
        var methodParameterSourcedArguments = argumentList.Skip(fieldArgumentsIndex);

        IEnumerable<ArgumentSyntax> argNodes =
        [
            ..fieldSourcedArguments,
            Argument(MultiMethodInvocationExpression.Create(parameterConverterMethod, methodParameterSourcedArguments))
        ];

        return ObjectCreationExpression(name)
            .WithNewKeyword(
                Token(SyntaxKind.NewKeyword))
            .WithArgumentList(ArgumentList(SeparatedList(argNodes)));
    }
}
