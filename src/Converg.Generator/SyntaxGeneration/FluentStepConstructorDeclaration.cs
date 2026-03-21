using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converg.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converg.Generator.SyntaxGeneration;

internal static class FluentStepConstructorDeclaration
{
    public static ConstructorDeclarationSyntax Create(IFluentStep step)
    {
        var constructorParameters = CreateFluentStepConstructorParameters(step);

        var requiredParamAssignments = step.KnownConstructorParameters
            .Select(p =>
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(p.Name.ToParameterFieldName())),
                        IdentifierName(p.Name.ToCamelCase()))));

        var optionalParamInitializations = GetOptionalParameterInitializations(step);

        var constructor = ConstructorDeclaration(Identifier(step.Name))
            .WithModifiers(TokenList(
                Token(SyntaxKind.InternalKeyword)))
            .WithParameterList(
                ParameterList(SeparatedList<ParameterSyntax>(constructorParameters)))
            .WithBody(Block(
                requiredParamAssignments.Concat(optionalParamInitializations).ToArray()));
        return constructor;
    }

    private static IEnumerable<SyntaxNodeOrToken> CreateFluentStepConstructorParameters(IFluentStep step)
    {
        var isAllOptionalStep = step is RegularFluentStep { IsAllOptionalStep: true };

        return step.KnownConstructorParameters
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
            })
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
