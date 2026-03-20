using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.SyntaxGeneration;

internal static class FluentStepDeclaration
{
    public static StructDeclarationSyntax Create(
        RegularFluentStep step)
    {
        var methodDeclarationSyntaxes = step.FluentMethods
            .Select<IFluentMethod, MethodDeclarationSyntax>(method =>
                method switch
                {
                    CreationMethod createMethod => FluentFactoryMethodDeclaration.Create(createMethod, step),
                    MultiMethod multiMethod => FluentStepMethodDeclaration.Create(multiMethod, step.KnownConstructorParameters),
                    OptionalFluentMethod optionalMethod => OptionalFluentMethodDeclaration.Create(optionalMethod, step),
                    _ => FluentStepMethodDeclaration.Create(method, step.KnownConstructorParameters)
                });

        var fieldDeclarations = FieldAndPropertySyntax.CreateDeclarations(step.ValueStorage);

        var constructor = FluentStepConstructorDeclaration.Create(step);

        NameSyntax name = IdentifierName(step.DeclarationDisplayString());

        var identifier = name is GenericNameSyntax genericName
            ? genericName.Identifier
            : ((SimpleNameSyntax)name).Identifier;

        // Extract type parameter list from the generic name if present
        TypeParameterListSyntax? typeParameterList = null;
        if (name is GenericNameSyntax genericNameSyntax)
        {
            var typeArgs = genericNameSyntax.TypeArgumentList.Arguments;
            var typeParameters = typeArgs
                .OfType<IdentifierNameSyntax>()
                .Select(arg => TypeParameter(arg.Identifier.ValueText))
                .ToArray();

            if (typeParameters.Length > 0)
            {
                typeParameterList = TypeParameterList(SeparatedList(typeParameters));
            }
        }

        var hasMutableOptionalMethods = step.FluentMethods.OfType<OptionalFluentMethod>().Any()
                                       && !step.IsAllOptionalStep;

        SyntaxTokenList accessibilityToken = (step.Accessibility, hasMutableOptionalMethods) switch
        {
            (Accessibility.Public, false) => [Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ReadOnlyKeyword)],
            (Accessibility.Public, true) => [Token(SyntaxKind.PublicKeyword)],
            (Accessibility.Private, false) => [Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)],
            (Accessibility.Private, true) => [Token(SyntaxKind.PrivateKeyword)],
            (Accessibility.Protected, false) => [Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.ReadOnlyKeyword)],
            (Accessibility.Protected, true) => [Token(SyntaxKind.ProtectedKeyword)],
            (Accessibility.Internal, false) => [Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.ReadOnlyKeyword)],
            (Accessibility.Internal, true) => [Token(SyntaxKind.InternalKeyword)],
            (Accessibility.ProtectedOrInternal, false) => [Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.ReadOnlyKeyword)],
            (Accessibility.ProtectedOrInternal, true) => [Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.InternalKeyword)],
            (Accessibility.ProtectedAndInternal, false) => [Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.ReadOnlyKeyword)],
            (Accessibility.ProtectedAndInternal, true) => [Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ProtectedKeyword)],
            (_, false) => [Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ReadOnlyKeyword)],
            _ => [Token(SyntaxKind.PublicKeyword)]
        };

        // Create XML documentation for the struct
        var xmlDocTrivia = FluentMethodSummaryDocXml.Create(
            [
                ..FluentMethodSummaryDocXml.GenerateCandidateConstructorTypeSeeAlsoLinks(step.CandidateConstructors)
            ]);

        // Get type parameters for constraints - use target type parameters for non-generic root types
        var constraintTypeParameters = GetConstraintTypeParameters(step);

        var structDeclaration = StructDeclaration(identifier)
            .WithModifiers(accessibilityToken)
            .WithAttributeLists(SingletonList(Helpers.GeneratedCodeAttributeSyntax.Create()))
            .WithLeadingTrivia(xmlDocTrivia)
            .WithTypeParameterList(typeParameterList)
            .WithConstraintClauses(CreateTypeParameterConstraints(constraintTypeParameters))
            .WithMembers(List<MemberDeclarationSyntax>([
                ..fieldDeclarations,
                constructor,
                ..methodDeclarationSyntaxes,
            ]));

        return structDeclaration;
    }

    private static IReadOnlyList<ITypeParameterSymbol> GetConstraintTypeParameters(RegularFluentStep step)
    {
        var targetTypeParameters = new List<ITypeParameterSymbol>();

        // Get type parameters from all candidate constructors
        foreach (var constructor in step.CandidateConstructors)
        {
            var targetType = constructor.ContainingType;
            if (targetType.IsGenericType)
            {
                targetTypeParameters.AddRange(targetType.OriginalDefinition.TypeParameters);
            }
        }

        // Get the generic type arguments that are actually used in this step
        var usedGenericArguments = step.GenericConstructorParameters
            .SelectMany(p => p.Type.GetGenericTypeArguments())
            .DistinctBy(symbol => symbol.GetEffectiveName())
            .ToArray();

        // Only return type parameters that are actually used in this step,
        // matching by effective name to correctly handle [As] aliases
        return targetTypeParameters
            .Where(tp => usedGenericArguments.Any(arg => arg.GetEffectiveName() == tp.GetEffectiveName()))
            .DistinctBy(tp => tp.GetEffectiveName())
            .ToArray();
    }

    private static SyntaxList<TypeParameterConstraintClauseSyntax> CreateTypeParameterConstraints(IReadOnlyList<ITypeParameterSymbol> typeParameters)
    {
        if (typeParameters.Count == 0)
            return List<TypeParameterConstraintClauseSyntax>();

        ImmutableArray<ITypeParameterSymbol> immutableTypeParameters = [..typeParameters];
        var constraintClauses = TypeParameterConstraintBuilder.Create(immutableTypeParameters);

        return List(constraintClauses);
    }
}
