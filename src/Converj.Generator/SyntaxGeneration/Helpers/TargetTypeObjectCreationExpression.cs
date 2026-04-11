using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration.Helpers;

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

        var objectCreation = ObjectCreationExpression(name)
            .WithNewKeyword(Token(SyntaxKind.NewKeyword))
            .WithArgumentList(ArgumentList(SeparatedList([..fieldArguments, ..methodArguments])));

        // Add object initializer for property-backed parameters
        if (method is CreationMethod { PropertyInitializers.IsEmpty: false } creationMethod)
        {
            var initializerExpressions = creationMethod.PropertyInitializers
                .Select(pi =>
                    (ExpressionSyntax)AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(pi.PropertyName),
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(pi.FieldName))));

            objectCreation = objectCreation
                .WithInitializer(
                    InitializerExpression(
                        SyntaxKind.ObjectInitializerExpression,
                        SeparatedList(initializerExpressions)));
        }

        return objectCreation;
    }

    /// <summary>
    /// Creates an invocation expression for a static method target (e.g., Class.Method(args)).
    /// </summary>
    public static InvocationExpressionSyntax CreateStaticMethodInvocation(
        CreationMethod method,
        IEnumerable<ArgumentSyntax> fieldArguments,
        IEnumerable<ArgumentSyntax> methodArguments)
    {
        var containingType = method.Return.CandidateTargets[0].ContainingType.ToGlobalDisplayString();
        var methodName = method.Return.CandidateTargets[0].Name;

        var memberAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ParseExpression(containingType),
            IdentifierName(methodName));

        return InvocationExpression(memberAccess)
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
