using Converj.Generator.Extensions;
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
            .SelectMany(p => CreateParameterAssignments(p, step.ValueStorage));

        var propertyParamAssignments = step is RegularFluentStep { PropertyFieldStorage.IsEmpty: false } propStep
            ? propStep.PropertyFieldStorage
                .Select(pf =>
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(pf.IdentifierName)),
                            IdentifierName(pf.IdentifierName.FromParameterFieldName()))))
            : Enumerable.Empty<StatementSyntax>();

        var optionalParamInitializations = GetOptionalParameterInitializations(step);
        var optionalPropertyInitializations = GetOptionalPropertyInitializations(step);

        var allAssignments = threadedParamAssignments
            .Concat(requiredParamAssignments)
            .Concat(propertyParamAssignments)
            .Concat(optionalParamInitializations)
            .Concat(optionalPropertyInitializations)
            .ToArray();

        // Parameterless struct constructors must be public per C# spec
        var hasParameters = constructorParameters.Any();
        var accessModifier = hasParameters
            ? Token(SyntaxKind.InternalKeyword)
            : Token(SyntaxKind.PublicKeyword);

        var constructor = ConstructorDeclaration(Identifier(step.Name))
            .WithModifiers(TokenList(accessModifier))
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
            .SelectMany(parameter => CreateConstructorParameters(parameter, step.ValueStorage, isAllOptionalStep));

        var propertyParams = step is RegularFluentStep { PropertyFieldStorage.IsEmpty: false } ps
            ? ps.PropertyFieldStorage
                .Select(pf =>
                    Parameter(Identifier(pf.IdentifierName.FromParameterFieldName()))
                        .WithType(ParseTypeName(pf.Type.ToGlobalDisplayString()))
                        .WithModifiers(TokenList(Token(SyntaxKind.InKeyword))))
            : Enumerable.Empty<ParameterSyntax>();

        return threadedParams.Concat(regularParams).Concat(propertyParams)
            .InterleaveWith(Token(SyntaxKind.CommaToken));
    }

    private static IEnumerable<StatementSyntax> GetOptionalPropertyInitializations(IFluentStep step)
    {
        if (step is not RegularFluentStep { OptionalPropertyFieldStorage.IsEmpty: false } propStep)
            yield break;

        foreach (var fieldStorage in propStep.OptionalPropertyFieldStorage)
        {
            var defaultExpression = fieldStorage.Type.IsReferenceType
                    || fieldStorage.Type.NullableAnnotation == NullableAnnotation.Annotated
                ? (ExpressionSyntax)LiteralExpression(SyntaxKind.NullLiteralExpression)
                : DefaultExpression(ParseTypeName(fieldStorage.Type.ToGlobalDisplayString()));

            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(fieldStorage.IdentifierName)),
                    defaultExpression));
        }
    }

    private static IEnumerable<ParameterSyntax> CreateConstructorParameters(
        IParameterSymbol parameter,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorage,
        bool isAllOptionalStep)
    {
        if (valueStorage.TryGetValue(parameter, out var storage) && storage is TupleFieldStorage tupleStorage)
        {
            return tupleStorage.ElementStorages.Select(element =>
                Parameter(Identifier(element.IdentifierName.FromParameterFieldName()))
                    .WithType(ParseTypeName(element.Type.ToGlobalDisplayString()))
                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword))));
        }

        var param = Parameter(Identifier(parameter.Name.ToCamelCase()))
            .WithType(ParseTypeName(parameter.Type.ToGlobalDisplayString()))
            .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)));

        if (isAllOptionalStep && parameter.HasExplicitDefaultValue)
        {
            param = param.WithDefault(
                EqualsValueClause(DefaultValueExpressionSyntax.Create(parameter)));
        }

        return [param];
    }

    private static IEnumerable<StatementSyntax> CreateParameterAssignments(
        IParameterSymbol parameter,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorage)
    {
        if (valueStorage.TryGetValue(parameter, out var storage) && storage is TupleFieldStorage tupleStorage)
        {
            return tupleStorage.ElementStorages.Select(element =>
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(element.IdentifierName)),
                        IdentifierName(element.IdentifierName.FromParameterFieldName()))));
        }

        return
        [
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(parameter.Name.ToParameterFieldName())),
                    IdentifierName(parameter.Name.ToCamelCase())))
        ];
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
