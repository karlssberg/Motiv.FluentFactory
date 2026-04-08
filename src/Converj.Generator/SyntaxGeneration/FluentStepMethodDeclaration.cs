using System.Collections.Immutable;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

internal static class FluentStepMethodDeclaration
{
    public static MethodDeclarationSyntax Create(
        MultiMethod multiMethod,
        ParameterSequence knownConstructorParameters,
        ImmutableArray<ITypeParameterSymbol>? ambientTypeParameters = null,
        bool isStepContext = false)
    {
        var stepActivationArgs = CreateStepConstructorArguments(multiMethod, knownConstructorParameters, isStepContext);

        var returnObjectExpression = FluentStepCreationExpression.Create(multiMethod, stepActivationArgs);

        return CreateMethodDeclaration(multiMethod, knownConstructorParameters, returnObjectExpression, ambientTypeParameters ?? []);
    }

    public static MethodDeclarationSyntax Create(
        IFluentMethod method,
        ParameterSequence knownConstructorParameters,
        ImmutableArray<ITypeParameterSymbol>? ambientTypeParameters = null,
        bool isStepContext = false)
    {
        var stepActivationArgs = CreateStepConstructorArguments(method, knownConstructorParameters, isStepContext);

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
            .WithBody(Block(ReturnStatement(returnObjectExpression)));

        methodDeclaration = AttachParameterList(method, methodDeclaration);

        return StepMethodTypeParameterResolver.AttachTypeParameters(method, knownConstructorParameters, ambientTypeParameters, methodDeclaration)
            .WithLeadingTrivia(GetDocumentationTrivia(method));
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
                    method.MethodParameters.Select(p => p.SourceName.ToCamelCase())),
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
                        .SelectMany(ExpandMethodParameter))));
    }

    /// <summary>
    /// Expands a fluent method parameter into one or more syntax parameters.
    /// For tuple parameters, each element becomes a separate method parameter.
    /// </summary>
    internal static IEnumerable<ParameterSyntax> ExpandMethodParameter(FluentMethodParameter parameter)
    {
        if (parameter is TupleFluentMethodParameter tuple)
        {
            return tuple.Elements.Select(element =>
                Parameter(Identifier(element.Name.ToCamelCase()))
                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                    .WithType(ParseTypeName(element.Type.ToGlobalDisplayString())));
        }

        return
        [
            Parameter(Identifier(parameter.SourceName.ToCamelCase()))
                .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                .WithType(ParseTypeName(parameter.SourceType.ToGlobalDisplayString()))
        ];
    }

    private static IEnumerable<ArgumentSyntax> CreateStepConstructorArguments(
        IFluentMethod method,
        ParameterSequence knownConstructorParameters,
        bool isStepContext)
    {
        // Root methods handle threading via RewriteRootMethodForThreadedParameters in RootTypeDeclaration.
        // Step methods must always forward threaded parameters, even when knownConstructorParameters is empty
        // (e.g., property steps from root-level creation methods where all constructor params are pre-satisfied).
        var threadedArgs = Enumerable.Empty<ArgumentSyntax>();
        if ((knownConstructorParameters.Any() || isStepContext)
            && method.Return is IFluentStep { ThreadedParameters.IsEmpty: false } nextStep)
        {
            threadedArgs = nextStep.ThreadedParameters
                .Select(b =>
                    Argument(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(b.TargetParameter.Name.ToParameterFieldName()))));
        }

        // Forward property field values from the current step to the next step
        var propertyFieldArgs = Enumerable.Empty<ArgumentSyntax>();
        if (method.Return is RegularFluentStep { PropertyFieldStorage.IsEmpty: false } nextPropStep)
        {
            // Get current step's property fields (the ones we already have)
            // These are fields the current step holds that need to be forwarded
            var currentPropFieldNames = nextPropStep.PropertyFieldStorage
                .Take(nextPropStep.PropertyFieldStorage.Length - (method.MethodParameters.Length > 0 ? 1 : 0))
                .Select(pf =>
                    Argument(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(pf.IdentifierName))));
            propertyFieldArgs = currentPropFieldNames;
        }

        var isTerminalCreation = method.Return is TargetTypeReturn;

        return threadedArgs
            .Concat(knownConstructorParameters
                .SelectMany(parameter => ExpandFieldArguments(parameter, method)))
            .Concat(propertyFieldArgs)
            .Concat(isTerminalCreation
                ? method.MethodParameters.Select(RepackMethodParameterArgument)
                : method.MethodParameters.SelectMany(ExpandMethodParameterArguments));
    }

    /// <summary>
    /// Expands field access arguments for forwarding known parameters to the next step or target constructor.
    /// For tuple parameters, expands to individual element field accesses (or re-packs into a tuple literal
    /// when forwarding to the target constructor).
    /// </summary>
    private static IEnumerable<ArgumentSyntax> ExpandFieldArguments(
        IParameterSymbol parameter,
        IFluentMethod method)
    {
        // Check if next step has tuple storage for this parameter
        if (method.Return is IFluentStep nextStep
            && nextStep.ValueStorage.TryGetValue(parameter, out var storage)
            && storage is TupleFieldStorage tupleStorage)
        {
            return tupleStorage.ElementStorages.Select(ElementFieldArgument);
        }

        // When returning to the target constructor, re-pack tuple elements into a tuple literal
        if (method.Return is TargetTypeReturn
            && method.ValueSources.TryGetValue(parameter, out var valueStorage)
            && valueStorage is TupleFieldStorage terminalTupleStorage)
        {
            return [Argument(TupleExpression(SeparatedList(
                terminalTupleStorage.ElementStorages.Select(ElementFieldArgument))))];
        }

        return
        [
            Argument(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(parameter.Name.ToParameterFieldName())))
        ];
    }

    private static ArgumentSyntax ElementFieldArgument(FieldStorage element) =>
        Argument(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                IdentifierName(element.IdentifierName)));

    /// <summary>
    /// Expands method parameter arguments (inline from caller) for forwarding to the next step constructor.
    /// For tuple parameters, expands to individual element identifier names.
    /// </summary>
    internal static IEnumerable<ArgumentSyntax> ExpandMethodParameterArguments(FluentMethodParameter parameter)
    {
        if (parameter is TupleFluentMethodParameter tuple)
        {
            return tuple.Elements.Select(element =>
                Argument(IdentifierName(element.Name.ToCamelCase())));
        }

        return [Argument(IdentifierName(parameter.SourceName.ToCamelCase()))];
    }

    /// <summary>
    /// Re-packs a method parameter argument for the target constructor call.
    /// For tuple parameters, wraps element identifiers in a tuple literal.
    /// </summary>
    private static ArgumentSyntax RepackMethodParameterArgument(FluentMethodParameter parameter)
    {
        if (parameter is TupleFluentMethodParameter tuple)
        {
            var elementArgs = tuple.Elements.Select(element =>
                Argument(IdentifierName(element.Name.ToCamelCase())));

            return Argument(TupleExpression(SeparatedList(elementArgs)));
        }

        return Argument(IdentifierName(parameter.SourceName.ToCamelCase()));
    }
}
