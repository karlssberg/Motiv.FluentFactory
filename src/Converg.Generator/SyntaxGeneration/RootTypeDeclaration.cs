using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converg.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converg.Generator.SyntaxGeneration;

internal static class RootTypeDeclaration
{
    private static readonly SymbolDisplayFormat NameOnlyFormat = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

    .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
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

        return typeDeclaration.WithMembers(
            List(rootMethodDeclarations.OfType<MemberDeclarationSyntax>()));
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
        // Build effective→local name mapping for root types with [As] aliases
        var effectiveToLocalMap = BuildEffectiveToLocalMapping(file.RootType);

        return file.FluentMethods
            .Select<IFluentMethod, MethodDeclarationSyntax>(method => method switch
            {
                { Return: TargetTypeReturn } => FluentRootFactoryMethodDeclaration.Create(method, file.RootType),
                OptionalGatewayMethod gateway => OptionalGatewayMethodDeclaration.Create(gateway),
                MultiMethod multiMethod => FluentStepMethodDeclaration.Create(multiMethod, [], file.RootType.TypeParameters),
                _ => FluentStepMethodDeclaration.Create(method, [], file.RootType.TypeParameters)
            })
            .Select(method =>
            {
                var result = method
                    .WithModifiers(
                        TokenList(GetSyntaxTokens()));

                // Remap effective type parameter names to local names for root types with [As] aliases
                if (effectiveToLocalMap.Count > 0)
                    result = (MethodDeclarationSyntax)new TypeParameterNameRewriter(effectiveToLocalMap).Visit(result);

                return result;

                IEnumerable<SyntaxToken> GetSyntaxTokens()
                {
                    yield return Token(SyntaxKind.PublicKeyword);
                    yield return Token(SyntaxKind.StaticKeyword);
                }
            });
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
            if (effectiveToLocalMap.TryGetValue(node.Identifier.ValueText, out var localName))
                return node.WithIdentifier(Identifier(localName));

            return base.VisitIdentifierName(node);
        }
    }
}
