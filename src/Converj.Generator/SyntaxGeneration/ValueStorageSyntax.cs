using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

internal static class FieldAndPropertySyntax
{
    public static ImmutableArray<MemberDeclarationSyntax> CreateDeclarations(
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages) =>
        [..valueStorages.Values.SelectMany(CreateDeclarations)];

    private static IEnumerable<MemberDeclarationSyntax> CreateDeclarations(IFluentValueStorage valueStorage)
    {
        return valueStorage switch
        {
            TupleFieldStorage tupleStorage =>
                tupleStorage.ElementStorages
                    .Where(e => !e.DefinitionExists)
                    .Select(CreateFieldDeclaration),
            FieldStorage { DefinitionExists: false } fieldStorage =>
                [CreateFieldDeclaration(fieldStorage)],
            PropertyStorage { DefinitionExists: false } propertyStorage =>
                [CreatePropertyDeclaration(propertyStorage)],
            _ => []
        };
    }

    public static FieldDeclarationSyntax CreateFieldDeclaration(FieldStorage fieldStorage)
    {
        var modifiers = fieldStorage.IsReadOnly
            ? TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword))
            : TokenList(Token(SyntaxKind.PrivateKeyword));

        return FieldDeclaration(
                VariableDeclaration(ParseTypeName(fieldStorage.Type.ToGlobalDisplayString()))
                    .AddVariables(VariableDeclarator(
                        Identifier(fieldStorage.IdentifierName))))
            .WithModifiers(modifiers);
    }

    private static PropertyDeclarationSyntax CreatePropertyDeclaration(PropertyStorage propertyStorage)
    {
        return PropertyDeclaration(
                ParseTypeName(propertyStorage.Type.ToGlobalDisplayString()),
                Identifier(propertyStorage.IdentifierName))
            .WithModifiers(TokenList(propertyStorage.Accessibility
                .AccessibilityToSyntaxKind()
                .Select(Token)))
            .WithAccessorList(
                AccessorList(
                    SingletonList(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))));
    }


}
