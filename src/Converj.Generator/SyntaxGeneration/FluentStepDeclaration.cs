using System.Collections.Immutable;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

internal static class FluentStepDeclaration
{
    public static StructDeclarationSyntax Create(
        RegularFluentStep step)
    {
        // Compute ambient type parameters from the step struct's generic type parameters
        // so that step methods don't re-declare type parameters already on the struct
        var ambientTypeParameters = step.GenericConstructorParameters
            .SelectMany(p => p.Type.GetGenericTypeArguments())
            .DistinctBy(symbol => symbol.GetEffectiveName())
            .ToImmutableArray();

        var methodDeclarationSyntaxes = step.FluentMethods
            .Select<IFluentMethod, MethodDeclarationSyntax>(method =>
                method switch
                {
                    TerminalMethod terminalMethod => StepTerminalMethodDeclaration.Create(terminalMethod, step),
                    MultiMethod multiMethod => FluentStepMethodDeclaration.Create(multiMethod, step.KnownConstructorParameters, ambientTypeParameters, isStepContext: true),
                    OptionalFluentMethod optionalMethod => OptionalFluentMethodDeclaration.Create(optionalMethod, step),
                    OptionalPropertyFluentMethod optionalPropertyMethod => OptionalPropertyFluentMethodDeclaration.Create(optionalPropertyMethod, step),
                    _ => FluentStepMethodDeclaration.Create(method, step.KnownConstructorParameters, ambientTypeParameters, isStepContext: true)
                });

        var fieldDeclarations = FieldAndPropertySyntax.CreateDeclarations(step.ValueStorage);

        // Add property-backed field declarations
        if (!step.PropertyFieldStorage.IsEmpty)
        {
            var propertyFields = step.PropertyFieldStorage
                .Select(pf => FieldAndPropertySyntax.CreateFieldDeclaration(pf))
                .ToImmutableArray();
            fieldDeclarations = [..fieldDeclarations, ..propertyFields];
        }

        // Add optional property field declarations (non-readonly)
        if (!step.OptionalPropertyFieldStorage.IsEmpty)
        {
            var optionalPropertyFields = step.OptionalPropertyFieldStorage
                .Select(pf => FieldAndPropertySyntax.CreateFieldDeclaration(pf))
                .ToImmutableArray();
            fieldDeclarations = [..fieldDeclarations, ..optionalPropertyFields];
        }

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

        var hasMutableOptionalMethods = (step.FluentMethods.OfType<OptionalFluentMethod>().Any()
                                       || step.FluentMethods.OfType<OptionalPropertyFluentMethod>().Any())
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
                ..FluentMethodSummaryDocXml.GenerateCandidateTargetTypeSeeAlsoLinks(step.GetAvailableTargets())
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
        var targetGenericTypeParameters = new List<ITypeParameterSymbol>();

        // Get type parameters from all candidate constructors' containing types
        var genericTargetTypes = step.CandidateTargets
            .Select(constructor => constructor.ContainingType)
            .Where(targetType => targetType.IsGenericType);
        
        foreach (var genericTargetType in genericTargetTypes)
        {
            targetGenericTypeParameters.AddRange(genericTargetType.OriginalDefinition.TypeParameters);
        }

        // Get type parameters from the receiver type's original definition
        // (for extension method targets where generics come from the receiver, not the target type)
        if (step.ReceiverParameter?.Type is INamedTypeSymbol { IsGenericType: true } receiverType)
        {
            targetGenericTypeParameters.AddRange(receiverType.OriginalDefinition.TypeParameters);
        }

        // Get the generic type arguments that are actually used in this step
        var usedGenericArguments = step.GenericConstructorParameters
            .SelectMany(p => p.Type.GetGenericTypeArguments())
            .DistinctBy(symbol => symbol.GetEffectiveName())
            .ToArray();

        // Only return type parameters that are actually used in this step,
        // matching by effective name to correctly handle [As] aliases
        return targetGenericTypeParameters
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
