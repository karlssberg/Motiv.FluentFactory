using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.Generation.Shared;
using Motiv.FluentFactory.Generator.Model;
using Motiv.FluentFactory.Generator.Model.Methods;
using Motiv.FluentFactory.Generator.Model.Steps;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.Generation.SyntaxElements.Methods;

internal static class FluentStepMethodDeclaration
{
    public static MethodDeclarationSyntax Create(
        MultiMethod multiMethod,
        ParameterSequence knownConstructorParameters,
        ImmutableArray<ITypeParameterSymbol>? ambientTypeParameters = null)
    {
        var stepActivationArgs = CreateStepConstructorArguments(multiMethod, knownConstructorParameters);

        var returnObjectExpression = FluentStepCreationExpression.Create(multiMethod, stepActivationArgs);

        return CreateMethodDeclaration(multiMethod, knownConstructorParameters, returnObjectExpression, ambientTypeParameters ?? []);
    }

    public static MethodDeclarationSyntax Create(
        IFluentMethod method,
        ParameterSequence knownConstructorParameters,
        ImmutableArray<ITypeParameterSymbol>? ambientTypeParameters = null)
    {
        var stepActivationArgs = CreateStepConstructorArguments(method, knownConstructorParameters);

        var returnObjectExpression = FluentStepCreationExpression.Create(method, stepActivationArgs);

        return CreateMethodDeclaration(method, knownConstructorParameters, returnObjectExpression, ambientTypeParameters ?? []);
    }

    private static List<object?> GetDocumentationLinesWithParameters(IFluentMethod method)
    {
        var lines = new List<object?>();

        // Add the main documentation summary
        if (!string.IsNullOrWhiteSpace(method.DocumentationSummary))
        {
            lines.Add(method.DocumentationSummary?.Trim());
            lines.Add("");
        }

        // Add constructor type information
        lines.AddRange(FluentMethodSummaryDocXml.GenerateCandidateConstructorTypeSeeAlsoLinks(method.Return.CandidateConstructors).Cast<object?>());

        return lines;
    }

    private static MethodDeclarationSyntax CreateMethodDeclaration(
        IFluentMethod method,
        ParameterSequence knownConstructorParameters,
        ObjectCreationExpressionSyntax returnObjectExpression,
        ImmutableArray<ITypeParameterSymbol> ambientTypeParameters)
    {
        var methodDeclaration = MethodDeclaration(
                returnObjectExpression.Type,
                Identifier(method.Name))
            .WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(AggressiveInliningAttributeSyntax.Create()))))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword)))
            .WithBody(Block(ReturnStatement(returnObjectExpression)))
            .WithLeadingTrivia(GetDocumentationTrivia(method));

        methodDeclaration = AttachParameterList(method, methodDeclaration);

        return AttachTypeParameters(method, knownConstructorParameters, ambientTypeParameters, methodDeclaration);
    }

    /// <summary>
    /// Gets the XML documentation trivia for the method declaration.
    /// </summary>
    private static SyntaxTriviaList GetDocumentationTrivia(IFluentMethod method)
    {
        return method switch
        {
            { ParameterDocumentation: not null, MethodParameters.Length: > 0 } =>
                FluentMethodSummaryDocXml.CreateWithParameters(
                    GetDocumentationLinesWithParameters(method),
                    method.ParameterDocumentation,
                    method.MethodParameters.Select(p => p.ParameterSymbol.Name.ToCamelCase())),
            _ =>
                FluentMethodSummaryDocXml.Create(GetDocumentationLinesWithParameters(method))
        };
    }

    /// <summary>
    /// Attaches the parameter list to the method declaration if the method has parameters.
    /// </summary>
    private static MethodDeclarationSyntax AttachParameterList(
        IFluentMethod method,
        MethodDeclarationSyntax methodDeclaration)
    {
        if (method.MethodParameters.Length == 0)
            return methodDeclaration;

        return methodDeclaration
            .WithParameterList(
                ParameterList(SeparatedList(
                    method.MethodParameters
                        .Select(parameter =>
                            Parameter(
                                    Identifier(parameter.ParameterSymbol.Name.ToCamelCase()))
                                .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                                .WithType(
                                    ParseTypeName(parameter.ParameterSymbol.Type.ToGlobalDisplayString()))))));
    }

    /// <summary>
    /// Filters the method's type parameters by excluding known constructor parameters
    /// and ambient type parameters, returning the type parameter syntaxes to add to the method.
    /// </summary>
    private static ImmutableArray<TypeParameterSyntax> GetMethodTypeParameterSyntaxes(
        IFluentMethod method,
        ParameterSequence knownConstructorParameters,
        ImmutableArray<ITypeParameterSymbol> ambientTypeParameters)
    {
        var rootTypeParametersSet = new HashSet<FluentTypeParameter>(
            ambientTypeParameters.Select(tp => new FluentTypeParameter(tp)));

        return method.TypeParameters
            .Except(knownConstructorParameters
                .SelectMany(parameter => parameter.Type.GetGenericTypeParameters())
                .Select(genericTypeParameters => new FluentTypeParameter(genericTypeParameters)))
            .Except(rootTypeParametersSet)
            .Select(fluentTypeParameter => fluentTypeParameter.TypeParameterSymbol.ToTypeParameterSyntax())
            .ToImmutableArray();
    }

    /// <summary>
    /// Attaches type parameter list and constraint clauses to the method declaration
    /// if the method has type parameters that are not already covered by constructor
    /// parameters or ambient type parameters.
    /// </summary>
    private static MethodDeclarationSyntax AttachTypeParameters(
        IFluentMethod method,
        ParameterSequence knownConstructorParameters,
        ImmutableArray<ITypeParameterSymbol> ambientTypeParameters,
        MethodDeclarationSyntax methodDeclaration)
    {
        if (!method.TypeParameters.Any())
            return methodDeclaration;

        var typeParameterSyntaxes = GetMethodTypeParameterSyntaxes(method, knownConstructorParameters, ambientTypeParameters);

        if (typeParameterSyntaxes.Length == 0)
            return methodDeclaration;

        methodDeclaration = methodDeclaration.WithTypeParameterList(
            TypeParameterList(SeparatedList([..typeParameterSyntaxes])));

        var combinedTypeParameters = GetCombinedTypeParameters(method, ambientTypeParameters);
        var constraintClauses = TypeParameterConstraintBuilder.Create(combinedTypeParameters);
        if (constraintClauses.Length > 0)
        {
            methodDeclaration = methodDeclaration
                .WithConstraintClauses(List(constraintClauses));
        }

        return methodDeclaration;
    }

    /// <summary>
    /// Collects type parameters from both the target type (for non-generic root types)
    /// and the method's own type parameters into a single array for constraint building.
    /// </summary>
    private static ImmutableArray<ITypeParameterSymbol> GetCombinedTypeParameters(
        IFluentMethod method,
        ImmutableArray<ITypeParameterSymbol> ambientTypeParameters)
    {
        var typeParameters = new List<ITypeParameterSymbol>();

        // Include target type parameters for non-generic root types
        if (ambientTypeParameters.IsEmpty && method.Return is TargetTypeReturn targetTypeReturn &&
            targetTypeReturn.Constructor.ContainingType.IsGenericType)
        {
            typeParameters.AddRange(targetTypeReturn.Constructor.ContainingType.OriginalDefinition.TypeParameters);
        }

        // Include method type parameters
        typeParameters.AddRange(method.TypeParameters.Select(tp => tp.TypeParameterSymbol));

        return [..typeParameters];
    }

    private static IEnumerable<ArgumentSyntax> CreateStepConstructorArguments(
        IFluentMethod method,
        ParameterSequence knownConstructorParameters)
    {
        return knownConstructorParameters
            .Select(parameter =>
                Argument(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        IdentifierName(parameter.Name.ToParameterFieldName()))))
            .Concat(
                method.MethodParameters.Select(p => p.ParameterSymbol.Name.ToCamelCase())
                    .Select(IdentifierName)
                    .Select(Argument));
    }
}
