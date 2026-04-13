using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

internal static class CompilationUnit
{
    public static SyntaxNode CreateCompilationUnit(
        FluentRootCompilationUnit file)
    {
        var members = GetMembers(file);

        return CompilationUnit()
            .WithMembers(List(members));
    }

    private static IEnumerable<MemberDeclarationSyntax> GetMembers(FluentRootCompilationUnit file)
    {
        var rootTypeDeclaration = RootTypeDeclaration.Create(file);
        var rootType = WrapInContainingTypes(rootTypeDeclaration, file.RootType);
        var namespacesGroups = file.FluentSteps
            .GroupBy(
                step => step.Namespace,
                SymbolEqualityComparer.Default)
            .Select(stepsInNamespace =>
            {
                var declarations = CreateTypeDeclarations(stepsInNamespace);
                return
                (
                    namespaces: stepsInNamespace.Key as INamespaceSymbol,
                    declarations: SymbolEqualityComparer.Default.Equals(file.RootType.ContainingNamespace,
                        stepsInNamespace.Key)
                        ? [rootType, ..declarations]
                        : declarations
                );
            });

        var memberDeclarations = namespacesGroups
            .SelectMany(tuple => MaybeEncapsulateInNamespace(tuple.namespaces, tuple.declarations))
            .ToArray();

        return DoFluentStepsShareTheRootNamespace(file)
            ? memberDeclarations
            : [..MaybeEncapsulateInNamespace(file.RootType.ContainingNamespace, [rootType]), ..memberDeclarations];
    }

    private static IEnumerable<TypeDeclarationSyntax> CreateTypeDeclarations(
        IEnumerable<IFluentStep> fluentSteps)
    {
        var existingTypeGroups = new Dictionary<INamedTypeSymbol, List<ExistingTypeFluentStep>>(SymbolEqualityComparer.Default);
        var outputOrder = new List<object>(); // INamedTypeSymbol (first-seen placeholder) or RegularFluentStep

        foreach (var step in fluentSteps)
        {
            switch (step)
            {
                case ExistingTypeFluentStep existingStep:
                    var containingType = existingStep.TargetContext.Method.ContainingType;
                    if (!existingTypeGroups.TryGetValue(containingType, out var group))
                    {
                        group = [];
                        existingTypeGroups[containingType] = group;
                        outputOrder.Add(containingType);
                    }
                    group.Add(existingStep);
                    break;

                case RegularFluentStep regularStep:
                    outputOrder.Add(regularStep);
                    break;

                default:
                    throw new NotSupportedException($"Step type {step.GetType()} is not supported.");
            }
        }

        foreach (var entry in outputOrder)
        {
            yield return entry switch
            {
                INamedTypeSymbol typeKey => ExistingPartialTypeStepDeclaration.CreateMerged(existingTypeGroups[typeKey]),
                RegularFluentStep regularStep => FluentStepDeclaration.Create(regularStep),
                _ => throw new NotSupportedException()
            };
        }
    }

    private static IEnumerable<MemberDeclarationSyntax> MaybeEncapsulateInNamespace(
        INamespaceSymbol? namespaces,
        IEnumerable<TypeDeclarationSyntax> declarations)
    {
        if (namespaces is null || namespaces.IsGlobalNamespace)
            return declarations;

        return
            [
                NamespaceDeclaration(ParseName(namespaces.ToDisplayString()))
                    .WithMembers(List([..declarations.OfType<MemberDeclarationSyntax>()]))
            ];
    }

    private static TypeDeclarationSyntax WrapInContainingTypes(
        TypeDeclarationSyntax declaration,
        INamedTypeSymbol typeSymbol)
    {
        var containingType = typeSymbol.ContainingType;
        while (containingType is not null)
        {
            var modifiers = TokenList(
                containingType.DeclaredAccessibility
                    .AccessibilityToSyntaxKind()
                    .Select(Token)
                    .Append(Token(SyntaxKind.PartialKeyword)));

            declaration = (containingType.TypeKind, containingType.IsRecord) switch
            {
                (TypeKind.Struct, true) =>
                    RecordDeclaration(SyntaxKind.RecordStructDeclaration, Token(SyntaxKind.StructKeyword),
                            Identifier(containingType.Name))
                        .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                        .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken))
                        .WithModifiers(TokenList(modifiers.Prepend(Token(SyntaxKind.RecordKeyword))))
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(declaration)),

                (TypeKind.Struct, false) =>
                    StructDeclaration(containingType.Name)
                        .WithModifiers(modifiers)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(declaration)),

                (_, true) =>
                    RecordDeclaration(SyntaxKind.RecordDeclaration, Token(SyntaxKind.RecordKeyword),
                            Identifier(containingType.Name))
                        .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                        .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken))
                        .WithModifiers(modifiers)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(declaration)),

                _ =>
                    ClassDeclaration(containingType.Name)
                        .WithModifiers(modifiers)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(declaration))
            };

            containingType = containingType.ContainingType;
        }

        return declaration;
    }

    private static bool DoFluentStepsShareTheRootNamespace(FluentRootCompilationUnit file)
    {
        var rootNamespace = file.RootType.ContainingNamespace;
        return
            file.FluentSteps.Any(
                step => step is not ExistingTypeFluentStep existingTypeFluentStep
                        || SymbolEqualityComparer.Default.Equals(
                            existingTypeFluentStep.Namespace,
                             rootNamespace));
    }
}
