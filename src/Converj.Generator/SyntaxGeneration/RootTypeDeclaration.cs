using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

internal static class RootTypeDeclaration
{
    public static TypeDeclarationSyntax Create(FluentFactoryCompilationUnit file)
    {
        var rootMethodDeclarations = GetRootMethodDeclarations(file);

        var identifier = Identifier(file.RootType.Name);
        TypeDeclarationSyntax typeDeclaration = file.TypeKind switch
        {
            TypeKind.Struct when file.IsRecord  =>
                RecordDeclaration(SyntaxKind.RecordStructDeclaration, Token(SyntaxKind.StructKeyword), identifier)
                    .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                    .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken))
                    .WithModifiers(
                        TokenList(GetRootTypeModifiers(file).Append(Token(SyntaxKind.RecordKeyword))))
                    .WithTypeParameterList(CreateTypeParameterList(file.RootType))
                    .WithConstraintClauses(CreateTypeParameterConstraints(file.RootType)),

            TypeKind.Struct =>
                StructDeclaration(identifier)
                    .WithModifiers(
                        TokenList(GetRootTypeModifiers(file)))
                    .WithTypeParameterList(CreateTypeParameterList(file.RootType))
                    .WithConstraintClauses(CreateTypeParameterConstraints(file.RootType)),

            TypeKind.Class when file.IsRecord =>
                RecordDeclaration(SyntaxKind.RecordDeclaration, Token(SyntaxKind.RecordKeyword), identifier)
                    .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                    .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken))
                    .WithModifiers(
                        TokenList(GetRootTypeModifiers(file)))
                    .WithTypeParameterList(CreateTypeParameterList(file.RootType))
                    .WithConstraintClauses(CreateTypeParameterConstraints(file.RootType)),

            _ =>
                ClassDeclaration(identifier)
                    .WithModifiers(
                        TokenList(GetRootTypeModifiers(file)))
                    .WithTypeParameterList(CreateTypeParameterList(file.RootType))
                    .WithConstraintClauses(CreateTypeParameterConstraints(file.RootType))
        };

        typeDeclaration = typeDeclaration
            .WithAttributeLists(SingletonList(Helpers.GeneratedCodeAttributeSyntax.Create()));

        var generatedFields = GetGeneratedFluentParameterFields(file);

        return typeDeclaration.WithMembers(
            List(generatedFields
                .Concat(rootMethodDeclarations.OfType<MemberDeclarationSyntax>())));
    }

    /// <summary>
    /// Generates backing field declarations for [FluentParameter] on primary constructor
    /// parameters that have no explicit storage.
    /// </summary>
    private static IEnumerable<MemberDeclarationSyntax> GetGeneratedFluentParameterFields(
        FluentFactoryCompilationUnit file)
    {
        return file.ThreadedParameters
            .Where(b => b.FactoryMember.RequiresGeneratedField)
            .Select(b =>
                FieldDeclaration(
                    VariableDeclaration(
                        ParseTypeName(b.FactoryMember.Type.ToGlobalDisplayString()))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier(b.FactoryMember.MemberIdentifierName))
                            .WithInitializer(
                                EqualsValueClause(
                                    IdentifierName(b.FactoryMember.PrimaryConstructorParameterName!))))))
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PrivateKeyword),
                    Token(SyntaxKind.ReadOnlyKeyword))));
    }

    private static TypeParameterListSyntax? CreateTypeParameterList(INamedTypeSymbol rootType)
    {
        if (!rootType.IsGenericType || rootType.TypeParameters.Length == 0)
            return null;

        var typeParameters = rootType.TypeParameters
            .Select(tp => TypeParameter(tp.Name))
            .ToArray();

        return TypeParameterList(SeparatedList(typeParameters));
    }

    private static SyntaxList<TypeParameterConstraintClauseSyntax> CreateTypeParameterConstraints(INamedTypeSymbol rootType)
    {
        if (!rootType.IsGenericType || rootType.TypeParameters.Length == 0)
            return List<TypeParameterConstraintClauseSyntax>();

        var constraintClauses = TypeParameterConstraintBuilder.Create(rootType.TypeParameters, useEffectiveNames: false);

        return List(constraintClauses);
    }

    private static IEnumerable<SyntaxToken> GetRootTypeModifiers(FluentFactoryCompilationUnit file)
    {
        foreach (var syntaxKind in file.Accessibility.AccessibilityToSyntaxKind())
        {
            yield return Token(syntaxKind);
        }
        if (file.IsStatic)
        {
            yield return Token(SyntaxKind.StaticKeyword);
        }
        yield return Token(SyntaxKind.PartialKeyword);
    }

    private static IEnumerable<MethodDeclarationSyntax> GetRootMethodDeclarations(FluentFactoryCompilationUnit file)
    {
        var effectiveToLocalMap = BuildEffectiveToLocalMapping(file.RootType);

        return file.FluentMethods
            .Select<IFluentMethod, (MethodDeclarationSyntax Syntax, bool IsInstance)>(method =>
            {
                var isInstance = IsInstanceMethod(method, file);
                var syntax = (method, isInstance) switch
                {
                    ({ Return: TargetTypeReturn }, _) => FluentRootFactoryMethodDeclaration.Create(method, file.RootType),
                    (OptionalGatewayMethod gateway, _) => OptionalGatewayMethodDeclaration.Create(gateway),
                    (MultiMethod multiMethod, _) => FluentStepMethodDeclaration.Create(multiMethod, [], file.RootType.TypeParameters),
                    _ => FluentStepMethodDeclaration.Create(method, [], file.RootType.TypeParameters)
                };

                if (isInstance)
                {
                    syntax = method.Return switch
                    {
                        IFluentStep { ThreadedParameters.IsEmpty: false } nextStep =>
                            RewriteRootMethodForThreadedParameters(syntax, nextStep),
                        TargetTypeReturn =>
                            RewriteRootMethodForDirectCreation(syntax, file.ThreadedParameters),
                        _ => syntax
                    };
                }

                // Rewrite extension method entry: add 'this' parameter and prepend receiver argument
                if (method.Return is IFluentStep { ReceiverParameter: not null } receiverStep)
                {
                    syntax = RewriteRootMethodForExtensionReceiver(syntax, receiverStep.ReceiverParameter);
                }

                return (syntax, isInstance);
            })
            .Select(pair =>
            {
                var result = pair.Syntax
                    .WithModifiers(
                        TokenList(GetSyntaxTokens(pair.IsInstance)));

                // Remap effective type parameter names to local names for root types with [As] aliases
                if (effectiveToLocalMap.Count > 0)
                    result = (MethodDeclarationSyntax)new TypeParameterNameRewriter(effectiveToLocalMap).Visit(result);

                return result;
            });
    }

    /// <summary>
    /// Determines whether a root method should be an instance method (uses threaded parameters)
    /// or a static method.
    /// </summary>
    private static bool IsInstanceMethod(IFluentMethod method, FluentFactoryCompilationUnit file)
    {
        if (file.ThreadedParameters.IsEmpty) return false;

        if (method.Return is IFluentStep { ThreadedParameters.IsEmpty: false })
            return true;

        return method.AvailableParameterFields
            .Any(f => file.ThreadedParameters.Any(b => b.TargetParameter.Name == f.SourceName));
    }

    /// <summary>
    /// Rewrites the return statement's ObjectCreationExpression arguments using the provided transform.
    /// Returns the method unchanged if the body does not contain the expected syntax shape.
    /// </summary>
    private static MethodDeclarationSyntax RewriteReturnCreationArguments(
        MethodDeclarationSyntax method,
        Func<ObjectCreationExpressionSyntax, IEnumerable<ArgumentSyntax>> transformArguments)
    {
        var returnStatement = method.Body?.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();
        if (returnStatement?.Expression is not ObjectCreationExpressionSyntax creation)
            return method;

        var newCreation = creation.WithArgumentList(ArgumentList(SeparatedList(transformArguments(creation))));
        var newReturn = returnStatement.WithExpression(newCreation);
        var newBody = method.Body!.WithStatements(SingletonList<StatementSyntax>(newReturn));

        return method.WithBody(newBody);
    }

    /// <summary>
    /// Prepends factory field reads for threaded parameters to the step constructor arguments.
    /// </summary>
    private static MethodDeclarationSyntax RewriteRootMethodForThreadedParameters(
        MethodDeclarationSyntax method,
        IFluentStep nextStep)
    {
        return RewriteReturnCreationArguments(method, creation =>
        {
            var threadedArgs = nextStep.ThreadedParameters
                .Select(b =>
                    Argument(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(b.FactoryMember.MemberIdentifierName))));

            var existingArgs = creation.ArgumentList?.Arguments ?? SeparatedList<ArgumentSyntax>();
            return threadedArgs.Concat(existingArgs);
        });
    }

    /// <summary>
    /// Replaces bare identifier arguments matching threaded parameters with factory field reads.
    /// </summary>
    private static MethodDeclarationSyntax RewriteRootMethodForDirectCreation(
        MethodDeclarationSyntax method,
        ImmutableArray<FluentParameterBinding> threadedParameters)
    {
        var threadedParamMap = threadedParameters.ToDictionary(
            b => b.TargetParameter.Name,
            b => b.FactoryMember.MemberIdentifierName);

        return RewriteReturnCreationArguments(method, creation =>
            (creation.ArgumentList?.Arguments ?? SeparatedList<ArgumentSyntax>())
                .Select(arg =>
                    arg.Expression is IdentifierNameSyntax id
                        && threadedParamMap.TryGetValue(id.Identifier.ValueText, out var factoryMemberName)
                        ? arg.WithExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                ThisExpression(),
                                IdentifierName(factoryMemberName)))
                        : arg));
    }

    /// <summary>
    /// Rewrites a root method to be an extension method: prepends a 'this' parameter
    /// and adds the receiver as the first argument to the step constructor.
    /// </summary>
    private static MethodDeclarationSyntax RewriteRootMethodForExtensionReceiver(
        MethodDeclarationSyntax method,
        IParameterSymbol receiverParameter)
    {
        // Add 'this' receiver as the first parameter (no 'in' modifier for extension receiver)
        var receiverParam = Parameter(Identifier(receiverParameter.Name.ToCamelCase()))
            .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
            .WithType(ParseTypeName(receiverParameter.Type.ToGlobalDisplayString()));

        var existingParams = method.ParameterList?.Parameters ?? SeparatedList<ParameterSyntax>();
        method = method.WithParameterList(
            ParameterList(SeparatedList(
                new[] { receiverParam }.Concat(existingParams))));

        // Prepend receiver identifier to the step constructor arguments
        var returnStatement = method.Body?.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();
        if (returnStatement?.Expression is ObjectCreationExpressionSyntax creation)
        {
            var receiverArg = Argument(IdentifierName(receiverParameter.Name.ToCamelCase()));
            var existingArgs = creation.ArgumentList?.Arguments ?? SeparatedList<ArgumentSyntax>();
            var newCreation = creation.WithArgumentList(
                ArgumentList(SeparatedList(
                    new[] { receiverArg }.Concat(existingArgs))));
            var newReturn = returnStatement.WithExpression(newCreation);
            var newBody = method.Body!.WithStatements(SingletonList<StatementSyntax>(newReturn));
            method = method.WithBody(newBody);
        }

        return method;
    }

    private static IEnumerable<SyntaxToken> GetSyntaxTokens(bool isInstance)
    {
        yield return Token(SyntaxKind.PublicKeyword);
        if (!isInstance)
            yield return Token(SyntaxKind.StaticKeyword);
    }

    private static Dictionary<string, string> BuildEffectiveToLocalMapping(INamedTypeSymbol rootType)
    {
        if (!rootType.IsGenericType)
            return new Dictionary<string, string>();

        var mapping = new Dictionary<string, string>();
        foreach (var tp in rootType.TypeParameters)
        {
            var effectiveName = tp.GetEffectiveName();
            if (effectiveName != tp.Name)
                mapping[effectiveName] = tp.Name;
        }

        return mapping;
    }

    /// <summary>
    /// Rewrites identifier names in syntax trees to replace effective type parameter names
    /// with their local names, for use in scopes where the original type parameter names are in effect.
    /// </summary>
    private sealed class TypeParameterNameRewriter(Dictionary<string, string> effectiveToLocalMap) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            return effectiveToLocalMap.TryGetValue(node.Identifier.ValueText, out var localName) 
                ? node.WithIdentifier(Identifier(localName)) 
                : base.VisitIdentifierName(node);
        }
    }
}
