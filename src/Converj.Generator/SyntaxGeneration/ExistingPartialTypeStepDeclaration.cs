using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

internal static class ExistingPartialTypeStepDeclaration
{
    /// <summary>
    /// Creates a single partial type declaration from one step.
    /// </summary>
    public static TypeDeclarationSyntax Create(
        ExistingTypeFluentStep step)
    {
        return CreateMerged([step]);
    }

    /// <summary>
    /// Creates a single partial type declaration by merging methods from multiple steps
    /// for the same containing type. Each method retains its original step context for rendering.
    /// </summary>
    public static TypeDeclarationSyntax CreateMerged(
        IReadOnlyList<ExistingTypeFluentStep> steps)
    {
        var representative = steps[0];

        var methodDeclarationSyntaxes = steps
            .SelectMany(step => step.FluentMethods
                .Select<IFluentMethod, MethodDeclarationSyntax>(method => method switch
                {
                    OptionalFluentMethod optionalMethod =>
                        ExistingTypeOptionalMethodDeclaration.Create(optionalMethod, step),
                    _ =>
                        ExistingPartialTypeMethodDeclaration.Create(method, step)
                }));

        var mergedStorage = MergeValueStorage(steps);
        var parameterFieldDeclaration = FieldAndPropertySyntax.CreateDeclarations(mergedStorage);

        var identifier = IdentifierName(representative.Name).Identifier;
        var typeDeclaration = CreateTypeDeclarationSyntax(representative, identifier)
            .WithMembers(List<MemberDeclarationSyntax>([
                ..parameterFieldDeclaration,
                ..methodDeclarationSyntaxes,
            ]));

        if (!HasOwnFactoryDeclaration(representative))
            typeDeclaration = typeDeclaration.WithAttributeLists(
                SingletonList(Helpers.GeneratedCodeAttributeSyntax.Create()));

        return typeDeclaration;
    }

    /// <summary>
    /// Merges value storage from multiple steps into a single dictionary.
    /// For existing types, storage entries typically have DefinitionExists=true,
    /// so no field declarations are emitted — the union is safe.
    /// </summary>
    private static OrderedDictionary<IParameterSymbol, IFluentValueStorage> MergeValueStorage(
        IReadOnlyList<ExistingTypeFluentStep> steps)
    {
        if (steps.Count == 1) return steps[0].ValueStorage;

        var merged = new OrderedDictionary<IParameterSymbol, IFluentValueStorage>();
        foreach (var step in steps)
        {
            foreach (var kvp in step.ValueStorage)
            {
                if (!merged.ContainsKey(kvp.Key))
                    merged[kvp.Key] = kvp.Value;
            }
        }

        return merged;
    }

    /// <summary>
    /// Checks whether the existing type has a [FluentFactory] attribute, indicating it has
    /// its own generated file that already emits [GeneratedCode] on its partial declaration.
    /// </summary>
    private static bool HasOwnFactoryDeclaration(ExistingTypeFluentStep step)
    {
        return step.ConstructorContext.Constructor.ContainingType
            .GetAttributes(TypeName.FluentRootAttribute)
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
