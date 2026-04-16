using System.Collections.Immutable;
using System.Diagnostics;
using Converj.Generator.Diagnostics;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converj.Generator.TargetAnalysis;

[DebuggerDisplay("{ToDisplayString()}}")]
internal class FluentTargetContext
{
    public FluentTargetContext(
        IMethodSymbol method,
        AttributeData attributeData,
        INamedTypeSymbol rootSymbol,
        FluentRootMetadata metadata,
        bool isAttributedUsedOnContainingType,
        SemanticModel semanticModel)
    {
        Method = method;
        AttributeData = attributeData;
        MethodPrefix = metadata.MethodPrefix;
        ReturnType = metadata.ReturnType;
        HasEntryMethod = metadata.HasEntryMethod;
        EntryMethodName = metadata.HasEntryMethod
            ? metadata.EntryMethodName ?? string.Empty
            : string.Empty;
        IsAttributedUsedOnContainingType = isAttributedUsedOnContainingType;
        IsStatic = rootSymbol.IsStatic;
        IsRecord = rootSymbol.IsRecord;
        TypeKind = rootSymbol.TypeKind;
        Accessibility = rootSymbol.DeclaredAccessibility;
        RootType = rootSymbol;

        IsStaticMethodTarget = method.MethodKind == MethodKind.Ordinary && method.IsStatic;
        IsInstanceMethodTarget = method.MethodKind == MethodKind.Ordinary && !method.IsStatic;

        ReceiverParameter = DetectReceiverParameter(method);

        // For static methods, default terminal verb to the method name and builder to FixedName
        TerminalMethod = IsStaticMethodTarget
            ? metadata.TerminalMethod ?? TerminalMethodKind.FixedName
            : metadata.TerminalMethod ?? TerminalMethodKind.DynamicSuffix;
        TerminalVerb = metadata.TerminalVerb ?? (IsStaticMethodTarget ? method.Name : null);

        // Analyze [FluentCollectionMethod] parameters unconditionally — applies to constructors,
        // static methods, and extension methods alike. Instance methods are rejected upstream.
        var collectionDiagnostics = new DiagnosticList();
        CollectionParameters = FluentCollectionMethodAnalyzer.Analyze(method, collectionDiagnostics);
        CollectionDiagnostics = collectionDiagnostics;

        // Property-backed collection analysis is only meaningful for constructor targets
        // (static/instance method targets don't have a target type with settable properties).
        if (!IsStaticMethodTarget && !IsInstanceMethodTarget)
        {
            CollectionProperties = FluentCollectionMethodAnalyzer.AnalyzeProperties(
                method.ContainingType, collectionDiagnostics);
        }
        else
        {
            CollectionProperties = [];
        }

        if (IsStaticMethodTarget || IsInstanceMethodTarget)
        {
            // Method targets don't have value storage or property analysis
            ValueStorage = new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(FluentParameterComparer.Default);
            TargetTypeProperties = [];
            PropertyDiagnostics = new DiagnosticList();
            return;
        }

        ValueStorage = new ConstructorAnalyzer(semanticModel).FindParameterValueStorage(method);

        // Get all declarations of the type to find modifiers
        var declarations = method.ContainingType.DeclaringSyntaxReferences
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
            method, method.ContainingType, methodPrefixValue, propertyDiagnostics);
        PropertyDiagnostics = propertyDiagnostics;
    }

    public INamedTypeSymbol RootType { get; }

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; } =
        new(FluentParameterComparer.Default);

    public TerminalMethodKind TerminalMethod { get; }

    public bool IsRecord { get; }

    public Accessibility Accessibility { get; }

    public bool IsStatic { get; }

    public TypeKind TypeKind { get; }

    public IMethodSymbol Method { get; }
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
    /// Whether this target has a <c>[FluentEntryMethod]</c> attribute for type-first chain entry.
    /// </summary>
    public bool HasEntryMethod { get; }

    /// <summary>
    /// The full entry method name (e.g., "BuildDog"). Only meaningful when <see cref="HasEntryMethod"/> is true.
    /// </summary>
    public string EntryMethodName { get; }

    public SyntaxTokenList OriginalTypeModifiers { get; }

    /// <summary>
    /// Properties on the target type that participate in the fluent chain.
    /// </summary>
    public ImmutableArray<FluentPropertyMember> TargetTypeProperties { get; }

    /// <summary>
    /// Diagnostics from property analysis.
    /// </summary>
    public DiagnosticList PropertyDiagnostics { get; }

    /// <summary>
    /// Collection parameters annotated with <c>[FluentCollectionMethod]</c> on this target,
    /// including derived or explicit accumulator method names.
    /// Empty when no parameters carry the attribute.
    /// </summary>
    public ImmutableArray<CollectionParameterInfo> CollectionParameters { get; }

    /// <summary>
    /// Diagnostics from <c>[FluentCollectionMethod]</c> parameter analysis (CVJG0050, CVJG0051).
    /// </summary>
    public DiagnosticList CollectionDiagnostics { get; }

    /// <summary>
    /// Properties on the target type annotated with <c>[FluentCollectionMethod]</c>, including
    /// derived or explicit accumulator method names.
    /// Empty for static/instance method targets (which have no settable target-type properties).
    /// Parallel to <see cref="CollectionParameters"/> — separate collection per plan architecture.
    /// </summary>
    public ImmutableArray<CollectionPropertyInfo> CollectionProperties { get; }

    /// <summary>
    /// The first parameter designated as the extension receiver, either via
    /// the C# <c>this</c> modifier or the <c>[This]</c> attribute.
    /// </summary>
    public IParameterSymbol? ReceiverParameter { get; }

    /// <summary>
    /// Whether this target has an extension receiver parameter.
    /// </summary>
    public bool HasReceiver => ReceiverParameter is not null;

    public string ToDisplayString() => $"{Method.ToDisplayString()}";

    private static IParameterSymbol? DetectReceiverParameter(IMethodSymbol method)
    {
        return method switch
        {
            { Parameters.Length: > 0, IsExtensionMethod: true } => 
                method.Parameters[0],
            
            { Parameters.Length: > 0 } when method.Parameters[0].HasAttribute(TypeName.ThisAttribute) =>
                method.Parameters[0],
            
            _ => 
                null
        };
    }
}
