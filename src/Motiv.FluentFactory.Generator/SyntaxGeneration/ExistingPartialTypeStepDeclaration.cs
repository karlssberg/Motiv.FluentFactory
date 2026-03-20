using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.SyntaxGeneration;

internal static class ExistingPartialTypeStepDeclaration
{
    public static TypeDeclarationSyntax Create(
        ExistingTypeFluentStep step)
    {
        var methodDeclarationSyntaxes = step.FluentMethods
            .Select<IFluentMethod, MethodDeclarationSyntax>(method => ExistingPartialTypeMethodDeclaration.Create(method, step));

        var parameterFieldDeclaration = FieldAndPropertySyntax.CreateDeclarations(step.ValueStorage);

        var identifier = IdentifierName(step.Name).Identifier;
        var typeDeclaration = CreateTypeDeclarationSyntax(step, identifier)
            .WithMembers(List<MemberDeclarationSyntax>([
                ..parameterFieldDeclaration,
                ..methodDeclarationSyntaxes,
            ]));

        // Only add [GeneratedCode] if the type doesn't already have its own factory file
        // (which would emit [GeneratedCode] on the root type declaration).
        if (!HasOwnFactoryDeclaration(step))
            typeDeclaration = typeDeclaration.WithAttributeLists(
                SingletonList(Helpers.GeneratedCodeAttributeSyntax.Create()));

        return typeDeclaration;
    }

    /// <summary>
    /// Checks whether the existing type has a [FluentFactory] attribute, indicating it has
    /// its own generated file that already emits [GeneratedCode] on its partial declaration.
    /// </summary>
    private static bool HasOwnFactoryDeclaration(ExistingTypeFluentStep step)
    {
        return step.ConstructorContext.Constructor.ContainingType
            .GetAttributes(TypeName.FluentFactoryAttribute)
            .Any();
    }

    private static TypeDeclarationSyntax CreateTypeDeclarationSyntax(IFluentStep step, SyntaxToken identifier)
    {
        return step.TypeKind switch
        {
            TypeKind.Class when step.IsRecord =>
                RecordDeclaration(
                        SyntaxKind.RecordDeclaration,
                        Token(SyntaxKind.RecordKeyword),
                        identifier)
                    .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                    .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken))
                    .WithModifiers(
                        TokenList(GetModifiers(step))),

            TypeKind.Class =>
                ClassDeclaration(identifier)
                    .WithModifiers(
                        TokenList(GetModifiers(step))),

            TypeKind.Struct when step.IsRecord =>
                StructDeclaration(identifier)
                    .WithModifiers(
                        TokenList(GetModifiers(step).Append(Token(SyntaxKind.RecordKeyword)))),

            _ =>
                StructDeclaration(identifier)
                    .WithModifiers(
                        TokenList(GetModifiers(step))),
        };
    }

    private static IEnumerable<SyntaxToken> GetModifiers(IFluentStep step)
    {
        if (step is ExistingTypeFluentStep existingStep)
        {
            var originalModifiers = existingStep.ConstructorContext.OriginalTypeModifiers;

            // Filter out 'partial' from original modifiers since we'll add it back
            var modifiersToKeep = originalModifiers.Where(m => !m.IsKind(SyntaxKind.PartialKeyword));

            return modifiersToKeep.Append(Token(SyntaxKind.PartialKeyword));
        }

        // Fallback to just accessibility + partial for non-existing types
        return step.Accessibility
            .AccessibilityToSyntaxKind()
            .Select(Token)
            .Append(Token(SyntaxKind.PartialKeyword));
    }
}
