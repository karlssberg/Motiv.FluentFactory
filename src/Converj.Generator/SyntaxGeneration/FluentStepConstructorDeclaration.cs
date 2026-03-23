using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

internal static class FluentStepConstructorDeclaration
{
    public static ConstructorDeclarationSyntax Create(IFluentStep step)
    {
        var constructorParameters = CreateFluentStepConstructorParameters(step);

        var threadedParamAssignments = step.ThreadedParameters
            .Select(b =>
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(b.TargetParameter.Name.ToParameterFieldName())),
                        IdentifierName(b.TargetParameter.Name.ToCamelCase()))));

        var requiredParamAssignments = step.KnownConstructorParameters
            .Select(p =>
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(p.Name.ToParameterFieldName())),
                        IdentifierName(p.Name.ToCamelCase()))));

        var optionalParamInitializations = GetOptionalParameterInitializations(step);

        var allAssignments = threadedParamAssignments
            .Concat(requiredParamAssignments)
            .Concat(optionalParamInitializations)
            .ToArray();

        var constructor = ConstructorDeclaration(Identifier(step.Name))
            .WithModifiers(TokenList(
                Token(SyntaxKind.InternalKeyword)))
            .WithParameterList(
                ParameterList(SeparatedList<ParameterSyntax>(constructorParameters)))
            .WithBody(Block(allAssignments));
        return constructor;
    }

    private static IEnumerable<SyntaxNodeOrToken> CreateFluentStepConstructorParameters(IFluentStep step)
    {
        var isAllOptionalStep = step is RegularFluentStep { IsAllOptionalStep: true };

        var threadedParams = step.ThreadedParameters
            .Select(b =>
                Parameter(Identifier(b.TargetParameter.Name.ToCamelCase()))
                    .WithType(ParseTypeName(b.TargetParameter.Type.ToGlobalDisplayString()))
                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword))));

        var regularParams = step.KnownConstructorParameters
            .Select(parameter =>
            {
                var param = Parameter(Identifier(parameter.Name.ToCamelCase()))
                    .WithType(ParseTypeName(parameter.Type.ToGlobalDisplayString()))
                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)));

                if (isAllOptionalStep && parameter.HasExplicitDefaultValue)
                {
                    param = param.WithDefault(
                        EqualsValueClause(DefaultValueExpressionSyntax.Create(parameter)));
                }

                return param;
            });

        return threadedParams.Concat(regularParams)
            .InterleaveWith(Token(SyntaxKind.CommaToken));
    }

    private static IEnumerable<StatementSyntax> GetOptionalParameterInitializations(IFluentStep step)
    {
        var knownParamFieldNames = new HashSet<string>(
            step.KnownConstructorParameters.Select(p => p.Name.ToParameterFieldName()));

        return step.ValueStorage
            .Where(kvp => kvp.Value is FieldStorage { IsReadOnly: false })
            .Where(kvp => !knownParamFieldNames.Contains(kvp.Key.Name.ToParameterFieldName()))
            .Select(kvp =>
            {
                var parameter = kvp.Key;
                var fieldName = parameter.Name.ToParameterFieldName();
                var defaultExpression = DefaultValueExpressionSyntax.Create(parameter);

                return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(fieldName)),
                        defaultExpression));
            });
    }
}
