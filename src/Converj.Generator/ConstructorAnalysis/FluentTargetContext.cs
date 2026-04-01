using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.Diagnostics;

namespace Converj.Generator.ConstructorAnalysis;

[DebuggerDisplay("{ToDisplayString()}}")]
internal record FluentTargetContext
{
    public FluentTargetContext(
        IMethodSymbol constructor,
        AttributeData attributeData,
        INamedTypeSymbol rootSymbol,
        FluentFactoryMetadata metadata,
        bool isAttributedUsedOnContainingType,
        SemanticModel semanticModel)
    {
        Constructor = constructor;
        AttributeData = attributeData;
        MethodPrefix = metadata.MethodPrefix;
        ReturnType = metadata.ReturnType;
        InitialVerb = metadata.InitialVerb ?? "Build";
        IsAttributedUsedOnContainingType = isAttributedUsedOnContainingType;
        IsStatic = rootSymbol.IsStatic;
        IsRecord = rootSymbol.IsRecord;
        TypeKind = rootSymbol.TypeKind;
        Accessibility = rootSymbol.DeclaredAccessibility;
        RootType = rootSymbol;

        // Detect method targets (non-constructor IMethodSymbol)
        IsStaticMethodTarget = constructor.MethodKind == MethodKind.Ordinary && constructor.IsStatic;
        IsInstanceMethodTarget = constructor.MethodKind == MethodKind.Ordinary && !constructor.IsStatic;

        // For static methods, default terminal verb to the method name and builder to FixedName
        Builder = IsStaticMethodTarget
            ? metadata.Builder ?? BuilderMethodKind.FixedName
            : metadata.Builder ?? BuilderMethodKind.DynamicSuffix;
        TerminalVerb = metadata.TerminalVerb ?? (IsStaticMethodTarget ? constructor.Name : null);

        if (IsStaticMethodTarget || IsInstanceMethodTarget)
        {
            // Method targets don't have value storage or property analysis
            ValueStorage = new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(FluentParameterComparer.Default);
            TargetTypeProperties = [];
            PropertyDiagnostics = new DiagnosticList();
            return;
        }

        ValueStorage = new ConstructorAnalyzer(semanticModel).FindParameterValueStorage(constructor);

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

        // Analyze target type properties for the fluent chain
        var methodPrefixValue = MethodPrefix ?? "With";
        var propertyDiagnostics = new DiagnosticList();
        TargetTypeProperties = FluentPropertyAnalyzer.Analyze(
            constructor, constructor.ContainingType, methodPrefixValue, propertyDiagnostics);
        PropertyDiagnostics = propertyDiagnostics;
    }

    public INamedTypeSymbol RootType { get; }

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; } =
        new(FluentParameterComparer.Default);

    public BuilderMethodKind Builder { get; }

    public bool IsRecord { get; }

    public Accessibility Accessibility { get; }

    public bool IsStatic { get; }

    public TypeKind TypeKind { get; }

    public IMethodSymbol Constructor { get; }
    public AttributeData AttributeData { get; }

    /// <summary>
    /// Whether this target is a static method (not a constructor).
    /// </summary>
    public bool IsStaticMethodTarget { get; }

    /// <summary>
    /// Whether this target is an instance method (not a constructor or static method).
    /// </summary>
    public bool IsInstanceMethodTarget { get; }

    public string? TerminalVerb { get; }

    public string? MethodPrefix { get; }

    public INamedTypeSymbol? ReturnType { get; }

    public bool IsAttributedUsedOnContainingType { get; }

    /// <summary>
    /// The verb used for the initial method name in First mode (e.g., "Build" -> "BuildDog").
    /// </summary>
    public string InitialVerb { get; }

    public SyntaxTokenList OriginalTypeModifiers { get; }

    /// <summary>
    /// Properties on the target type that participate in the fluent chain.
    /// </summary>
    public ImmutableArray<FluentPropertyMember> TargetTypeProperties { get; }

    /// <summary>
    /// Diagnostics from property analysis.
    /// </summary>
    public DiagnosticList PropertyDiagnostics { get; }

    public string ToDisplayString() => $"{Constructor.ToDisplayString()}";
}
