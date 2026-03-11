using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator;

[DebuggerDisplay("{ToString()}")]
internal class RegularFluentStep(INamedTypeSymbol rootType, IEnumerable<IMethodSymbol> candidateConstructors) : IFluentStep
{
#if DEBUG
    public int InstanceId => RuntimeHelpers.GetHashCode(this);
#endif
    public string Name => GetStepName(RootType);

    public string FullName => $"{Namespace.ToDisplayString()}.{Name}";

    /// <summary>
    /// The known constructor parameters up until this step.
    /// Potentially more parameters are required to satisfy a constructor signature.
    /// </summary>
    public ParameterSequence KnownConstructorParameters { get; set; } = [];

    public IList<IFluentMethod> FluentMethods { get; set; } = [];

    public ImmutableArray<IParameterSymbol> GenericConstructorParameters => [
        ..KnownConstructorParameters
            .Where(parameter => parameter.Type.IsOpenGenericType())
    ];

    public Accessibility Accessibility { get; set; } = rootType.DeclaredAccessibility;

    public override string ToString()
    {
        return string.Join(", ", KnownConstructorParameters.Select(p => p.ToDisplayString()));
    }

    public bool IsEndStep { get; set; }

    public TypeKind TypeKind { get; set; } = TypeKind.Class;

    public bool IsRecord { get; set; }  = false;

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; set; } = [];

    public INamedTypeSymbol RootType { get; } = rootType;

    public ImmutableArray<IMethodSymbol> CandidateConstructors => [..candidateConstructors];

    public string IdentifierDisplayString()
    {
        var globalPrefix = Namespace.IsGlobalNamespace
            ? "global::"
            : $"global::{Namespace.ToDisplayString()}.";
        var distinctGenericParameters = GenericConstructorParameters
            .SelectMany(t => t.Type.GetGenericTypeArguments())
            .DistinctBy(symbol => symbol.Name)
            .ToArray();

        return distinctGenericParameters.Length > 0
            ? $"{globalPrefix}{GenericName(Identifier(Name))
                .WithTypeArgumentList(
                    TypeArgumentList(SeparatedList<TypeSyntax>(
                        distinctGenericParameters
                            .Select(arg => IdentifierName(arg.Name)))))
                .NormalizeWhitespace()}"
            : $"{globalPrefix}{Name}";
    }

    public string IdentifierDisplayString(IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap)
    {
        var globalPrefix = Namespace.IsGlobalNamespace
            ? "global::"
            : $"global::{Namespace.ToDisplayString()}.";
        var distinctGenericParameters = this.GetGenericTypeArguments(genericTypeArgumentMap)
            .ToArray();

        return distinctGenericParameters.Length > 0
            ? $"{globalPrefix}{GenericName(Identifier(Name))
                .WithTypeArgumentList(
                    TypeArgumentList(SeparatedList<TypeSyntax>(
                        distinctGenericParameters
                            .Select(arg => ParseTypeName(arg.ToGlobalDisplayString())))))
                .NormalizeWhitespace()}"
            : $"{globalPrefix}{Name}";
    }

    /// <summary>
    /// Returns the display string for the struct declaration (no global:: qualification).
    /// </summary>
    public string DeclarationDisplayString()
    {
        var distinctGenericParameters = GenericConstructorParameters
            .SelectMany(t => t.Type.GetGenericTypeArguments())
            .DistinctBy(symbol => symbol.Name)
            .ToArray();

        return distinctGenericParameters.Length > 0
            ? GenericName(Identifier(Name))
                .WithTypeArgumentList(
                    TypeArgumentList(SeparatedList<TypeSyntax>(
                        distinctGenericParameters
                            .Select(arg => IdentifierName(arg.Name)))))
                .NormalizeWhitespace()
                .ToString()
            : Name;
    }

    public INamespaceSymbol Namespace => RootType.ContainingNamespace;

    public int Index { get; set; }

    private string GetStepName(INamedTypeSymbol rootType)
    {
        var identifier = rootType.ToIdentifier();
        var name = $"Step_{Index}__{identifier}";

        return name;
    }
}
