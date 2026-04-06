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
            .Select(method => BuildRootMethodSyntax(method, file))
            .Select(pair => ApplyModifiersAndTypeRemapping(pair, effectiveToLocalMap));
    }

    private static (MethodDeclarationSyntax Syntax, bool IsInstance) BuildRootMethodSyntax(
        IFluentMethod method,
        FluentFactoryCompilationUnit file)
    {
        var isInstance = IsInstanceMethod(method, file);

        var syntax = method switch
        {
            { Return: TargetTypeReturn } =>
                FluentRootFactoryMethodDeclaration.Create(method, file.RootType),
            
            OptionalGatewayMethod gateway =>
                OptionalGatewayMethodDeclaration.Create(gateway),
            
            MultiMethod multiMethod =>
                FluentStepMethodDeclaration.Create(multiMethod, [], file.RootType.TypeParameters),
            
            _ =>
                FluentStepMethodDeclaration.Create(method, [], file.RootType.TypeParameters)
        };

        syntax = ApplyInstanceRewrite(syntax, method, isInstance, file);
        syntax = ApplyExtensionReceiverRewrite(syntax, method);

        return (syntax, isInstance);
    }

    private static MethodDeclarationSyntax ApplyInstanceRewrite(
        MethodDeclarationSyntax syntax,
        IFluentMethod method,
        bool isInstance,
        FluentFactoryCompilationUnit file)
    {
        if (!isInstance) return syntax;

        return method.Return switch
        {
            IFluentStep { ThreadedParameters.IsEmpty: false } nextStep =>
                RewriteRootMethodForThreadedParameters(syntax, nextStep),
            TargetTypeReturn =>
                RewriteRootMethodForDirectCreation(syntax, file.ThreadedParameters),
            _ => syntax
        };
    }

    private static MethodDeclarationSyntax ApplyExtensionReceiverRewrite(
        MethodDeclarationSyntax syntax,
        IFluentMethod method)
    {
        var receiverParameter = method switch
        {
            { Return: IFluentStep { ReceiverParameter: not null } step } => step.ReceiverParameter,
            CreationMethod { ReceiverParameter: not null } creation => creation.ReceiverParameter,
            _ => null
        };

        return receiverParameter is not null
            ? RewriteRootMethodForExtensionReceiver(syntax, receiverParameter)
            : syntax;
    }

    private static MethodDeclarationSyntax ApplyModifiersAndTypeRemapping(
        (MethodDeclarationSyntax Syntax, bool IsInstance) pair,
        Dictionary<string, string> effectiveToLocalMap)
    {
        var result = pair.Syntax
            .WithModifiers(TokenList(GetSyntaxTokens(pair.IsInstance)));

        if (effectiveToLocalMap.Count > 0)
            result = (MethodDeclarationSyntax)new TypeParameterNameRewriter(effectiveToLocalMap).Visit(result);

        return result;
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
    /// Rewrites a root method to be an extension method: prepends a 'this' parameter,
    /// adds the receiver as the first argument to the step constructor, and merges
    /// any generic type parameters from the receiver type into the method declaration.
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

        // Merge receiver type parameters into the method declaration
        method = MergeReceiverTypeParameters(method, receiverParameter);

        // Prepend receiver identifier to the step constructor arguments
        return RewriteReturnCreationArguments(method, creation =>
        {
            var receiverArg = Argument(IdentifierName(receiverParameter.Name.ToCamelCase()));
            var existingArgs = creation.ArgumentList?.Arguments ?? SeparatedList<ArgumentSyntax>();
            return new[] { receiverArg }.Concat(existingArgs);
        });
    }

    /// <summary>
    /// Merges generic type parameters from the receiver type into the method's type parameter list
    /// and constraint clauses, skipping any that are already declared on the method.
    /// </summary>
    private static MethodDeclarationSyntax MergeReceiverTypeParameters(
        MethodDeclarationSyntax method,
        IParameterSymbol receiverParameter)
    {
        var receiverTypeParams = receiverParameter.Type.GetGenericTypeParameters().ToImmutableArray();
        if (receiverTypeParams.IsEmpty)
            return method;

        var existingNames = new HashSet<string>(
            method.TypeParameterList?.Parameters.Select(p => p.Identifier.Text) ?? []);

        var newTypeParams = receiverTypeParams
            .Where(tp => !existingNames.Contains(tp.GetEffectiveName()))
            .ToImmutableArray();

        if (newTypeParams.IsEmpty)
            return method;

        var existingSyntaxes = method.TypeParameterList?.Parameters ?? SeparatedList<TypeParameterSyntax>();
        var mergedSyntaxes = existingSyntaxes
            .Concat(newTypeParams.Select(tp => tp.ToTypeParameterSyntax()));

        method = method.WithTypeParameterList(
            TypeParameterList(SeparatedList(mergedSyntaxes)));

        // Build constraints for all type parameters (existing + new)
        var allTypeParamSymbols = receiverTypeParams;
        var constraintClauses = TypeParameterConstraintBuilder.Create(allTypeParamSymbols);

        if (constraintClauses.Length > 0)
        {
            var existingConstraints = method.ConstraintClauses;
            var existingConstraintNames = new HashSet<string>(
                existingConstraints.Select(c => c.Name.Identifier.Text));

            var newConstraints = constraintClauses
                .Where(c => !existingConstraintNames.Contains(c.Name.Identifier.Text));

            method = method.WithConstraintClauses(List(existingConstraints.Concat(newConstraints)));
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
