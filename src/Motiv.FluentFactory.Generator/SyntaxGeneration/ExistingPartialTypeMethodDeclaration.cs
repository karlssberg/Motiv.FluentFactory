using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.SyntaxGeneration;

internal static class ExistingPartialTypeMethodDeclaration
{
    public static MethodDeclarationSyntax Create(
        IFluentMethod method,
        IFluentStep step)
    {
        var typeParamMap = BuildTypeParameterMapping(step);
        var stepActivationArgs = ReturnTypeConstructorArgumentsSyntax.Create(method);

        var returnObjectExpression = method.Return switch
        {
            TargetTypeReturn => TargetTypeObjectCreationExpression.Create(method, stepActivationArgs, []),
            _ => FluentStepCreationExpression.Create(method, stepActivationArgs)
        };

        var returnType = method.Return is TargetTypeReturn targetTypeReturn
            ? ParseTypeName(typeParamMap.Count > 0
                ? targetTypeReturn.ReturnTypeDisplayString(typeParamMap)
                : targetTypeReturn.ReturnTypeDisplayString())
            : returnObjectExpression.Type;

        // For existing partial types, remap the return expression's type references
        if (typeParamMap.Count > 0)
        {
            var remappedReturnIdentifier = method.Return switch
            {
                TargetTypeReturn tr => tr.IdentifierDisplayString(typeParamMap),
                ExistingTypeFluentStep existingReturn =>
                    existingReturn.ConstructorContext.Constructor.ContainingType
                        .ToGlobalDisplayString(typeParamMap),
                _ => method.Return.IdentifierDisplayString()
            };

            returnObjectExpression = RemapObjectCreationType(returnObjectExpression, remappedReturnIdentifier);

            // Update returnType for non-TargetTypeReturn cases since it was set before remapping
            if (method.Return is not TargetTypeReturn)
            {
                returnType = returnObjectExpression.Type;
            }
        }

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
            .WithBody(Block(ReturnStatement(returnObjectExpression)));

        if (method.MethodParameters.Length > 0)
        {
            methodDeclaration = methodDeclaration
                .WithParameterList(
                    ParameterList(SeparatedList(
                        method.MethodParameters
                            .Select(parameter =>
                                Parameter(
                                        Identifier(parameter.ParameterSymbol.Name.ToCamelCase()))
                                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                                    .WithType(
                                        ParseTypeName(parameter.ParameterSymbol.Type
                                            .ToGlobalDisplayString(typeParamMap)))))));
        }

        methodDeclaration = methodDeclaration.WithLeadingTrivia(
            FluentMethodSummaryDocXml.Create(
                [
                    method.DocumentationSummary,
                    ..FluentMethodSummaryDocXml.GenerateCandidateConstructorTypeSeeAlsoLinks(method.Return.CandidateConstructors)
                ]));

        if (!method.TypeParameters.Any())
            return methodDeclaration;

        var typeParameterSyntaxes = method.TypeParameters
            .Except(
                step.KnownConstructorParameters
                    .SelectMany(parameter => parameter.Type.GetGenericTypeParameters())
                    .Select(genericTypeParameters => new FluentTypeParameter(genericTypeParameters)))
            .Select(fluentTypeParameter => fluentTypeParameter.TypeParameterSymbol.ToTypeParameterSyntax())
            .ToImmutableArray();

        return typeParameterSyntaxes.Length == 0
            ? methodDeclaration
            : methodDeclaration.WithTypeParameterList(
                TypeParameterList(SeparatedList([..typeParameterSyntaxes])));
    }

    /// <summary>
    /// Builds a mapping from effective type parameter names to the step's own type parameter names.
    /// This ensures that when generating methods on existing partial types, type references
    /// use the type parameter names that are actually in scope.
    /// </summary>
    private static IDictionary<string, string> BuildTypeParameterMapping(IFluentStep step)
    {
        if (step is not ExistingTypeFluentStep existingStep)
            return new Dictionary<string, string>();

        var containingType = existingStep.ConstructorContext.Constructor.ContainingType;

        // Only build a mapping when at least one type parameter has an [As] alias.
        // This avoids unnecessary work in downstream methods that check Count > 0.
        var hasAnyAlias = containingType.TypeParameters.Any(tp => tp.GetEffectiveName() != tp.Name);
        if (!hasAnyAlias)
            return new Dictionary<string, string>();

        var mapping = new Dictionary<string, string>();
        foreach (var tp in containingType.TypeParameters)
        {
            mapping[tp.GetEffectiveName()] = tp.Name;
        }

        return mapping;
    }

    private static ObjectCreationExpressionSyntax RemapObjectCreationType(
        ObjectCreationExpressionSyntax expression,
        string newTypeName)
    {
        return expression.WithType(ParseTypeName(newTypeName));
    }
}
