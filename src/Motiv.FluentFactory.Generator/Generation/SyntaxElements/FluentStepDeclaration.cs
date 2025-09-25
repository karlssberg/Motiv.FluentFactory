using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.Generation.SyntaxElements.Constructors;
using Motiv.FluentFactory.Generator.Generation.SyntaxElements.Methods;
using Motiv.FluentFactory.Generator.Generation.SyntaxElements.ValueStorage;
using Motiv.FluentFactory.Generator.Model.Methods;
using Motiv.FluentFactory.Generator.Model.Steps;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.Generation.SyntaxElements;

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
                    MultiMethod multiMethod => FluentStepMethodDeclaration.Create(multiMethod, step.KnownConstructorParameters, step.Namespace),
                    _ => FluentStepMethodDeclaration.Create(method, step.KnownConstructorParameters, step.Namespace)
                });

        var fieldDeclarations = FieldAndPropertySyntax.CreateDeclarations(step.ValueStorage);

        var constructor = FluentStepConstructorDeclaration.Create(step);

        NameSyntax name = IdentifierName(((IFluentReturn)step).IdentifierDisplayString(step.Namespace));

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

        SyntaxTokenList accessibilityToken = step.Accessibility switch
        {
            Accessibility.Public => [Token(SyntaxKind.PublicKeyword)],
            Accessibility.Private => [Token(SyntaxKind.PrivateKeyword)],
            Accessibility.Protected => [Token(SyntaxKind.ProtectedKeyword)],
            Accessibility.Internal => [Token(SyntaxKind.InternalKeyword)],
            Accessibility.ProtectedOrInternal => [Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.InternalKeyword)],
            Accessibility.ProtectedAndInternal => [Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ProtectedKeyword)],
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
        // For non-generic root types, extract type parameters from candidate constructors
        if (!step.RootType.IsGenericType)
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
                .DistinctBy(symbol => symbol.Name)
                .ToArray();

            // Only return type parameters that are actually used in this step
            return targetTypeParameters
                .Where(tp => usedGenericArguments.Any(arg => arg.Name == tp.Name))
                .DistinctBy(tp => tp.Name)
                .ToArray();
        }

        // For generic root types, use the root type's type parameters
        return step.RootType.TypeParameters;
    }

    private static SyntaxList<TypeParameterConstraintClauseSyntax> CreateTypeParameterConstraints(IReadOnlyList<ITypeParameterSymbol> typeParameters)
    {
        if (typeParameters.Count == 0)
            return List<TypeParameterConstraintClauseSyntax>();

        var constraintClauses = typeParameters
            .Where(tp => tp.HasConstructorConstraint || tp.HasReferenceTypeConstraint || tp.HasValueTypeConstraint || tp.ConstraintTypes.Length > 0)
            .Select(tp =>
            {
                var constraints = new List<TypeParameterConstraintSyntax>();

                // Add reference type constraint (class)
                if (tp.HasReferenceTypeConstraint)
                    constraints.Add(ClassOrStructConstraint(SyntaxKind.ClassConstraint));

                // Add value type constraint (struct)
                if (tp.HasValueTypeConstraint)
                    constraints.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));

                // Add type constraints
                foreach (var constraintType in tp.ConstraintTypes)
                {
                    var typeName = constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    constraints.Add(TypeConstraint(ParseTypeName(typeName)));
                }

                // Add constructor constraint (new())
                if (tp.HasConstructorConstraint)
                    constraints.Add(ConstructorConstraint());

                return TypeParameterConstraintClause(tp.Name)
                    .WithConstraints(SeparatedList(constraints));
            })
            .ToArray();

        return List(constraintClauses);
    }
}
