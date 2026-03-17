using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Motiv.FluentFactory.Generator.ConstructorAnalysis;

[DebuggerDisplay("{ToDisplayString()}}")]
internal record FluentConstructorContext
{
    public FluentConstructorContext(
        IMethodSymbol constructor,
        AttributeData attributeData,
        INamedTypeSymbol rootSymbol,
        FluentFactoryMetadata metadata,
        bool isAttributedUsedOnContainingType,
        SemanticModel semanticModel)
    {
        Constructor = constructor;
        AttributeData = attributeData;
        CreateMethod = metadata.CreateMethod ?? CreateMethodMode.Dynamic;
        CreateVerb = metadata.CreateVerb;
        MethodPrefix = metadata.MethodPrefix;
        IsAttributedUsedOnContainingType = isAttributedUsedOnContainingType;
        IsStatic = rootSymbol.IsStatic;
        IsRecord = rootSymbol.IsRecord;
        TypeKind = rootSymbol.TypeKind;
        Accessibility = rootSymbol.DeclaredAccessibility;
        ValueStorage = new ConstructorAnalyzer(semanticModel).FindParameterValueStorage(constructor);
        RootType = rootSymbol;

        // Get all declarations of the type to find modifiers
        var declarations = constructor.ContainingType.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .ToArray();

        // Find declaration with readonly modifier if it exists
        var declaration = declarations.FirstOrDefault(d =>
            d.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)));

        // If no readonly found, use first declaration with any modifiers
        declaration ??= declarations.FirstOrDefault(d => d.Modifiers.Any());

        if (declaration != null)
        {
            OriginalTypeModifiers = declaration.Modifiers;
        }
    }

    public INamedTypeSymbol RootType { get; }

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; } =
        new(FluentParameterComparer.Default);

    public CreateMethodMode CreateMethod { get; }

    public bool IsRecord { get; }

    public Accessibility Accessibility { get; }

    public bool IsStatic { get; }

    public TypeKind TypeKind { get; }

    public IMethodSymbol Constructor { get; }
    public AttributeData AttributeData { get; }

    public string? CreateVerb { get; }

    public string? MethodPrefix { get; }

    public bool IsAttributedUsedOnContainingType { get; }

    public SyntaxTokenList OriginalTypeModifiers { get; }

    public string ToDisplayString() => $"{Constructor.ToDisplayString()}";
}
