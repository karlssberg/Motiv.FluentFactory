using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converg.Generator;

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

    /// <summary>
    /// Indicates this step was created for constructors where all parameters are optional.
    /// When true, the step constructor will have default parameter values.
    /// </summary>
    public bool IsAllOptionalStep { get; set; }

    public TypeKind TypeKind { get; set; } = TypeKind.Class;

    public bool IsRecord { get; set; }  = false;

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; set; } = [];

    public INamedTypeSymbol RootType { get; } = rootType;

    public ImmutableArray<IMethodSymbol> CandidateConstructors => [..candidateConstructors];

    public string IdentifierDisplayString()
    {
        return BuildGlobalIdentifier(
            GetDistinctEffectiveTypeArguments(),
            arg => IdentifierName(arg.GetEffectiveName()));
    }

    /// <summary>
    /// Returns the display string with type parameter names remapped using effective-to-local name mapping.
    /// Used when generating methods on existing partial types where effective names must be
    /// translated to the partial type's own type parameter names.
    /// </summary>
    public string IdentifierDisplayString(IDictionary<string, string> effectiveToLocalNameMap)
    {
        return BuildGlobalIdentifier(
            GetDistinctEffectiveTypeArguments(),
            arg =>
            {
                var effectiveName = arg.GetEffectiveName();
                return effectiveToLocalNameMap.TryGetValue(effectiveName, out var localName)
                    ? IdentifierName(localName)
                    : IdentifierName(effectiveName);
            });
    }

    public string IdentifierDisplayString(IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap)
    {
        var distinctGenericParameters = this.GetGenericTypeArguments(genericTypeArgumentMap)
            .ToArray();

        return BuildGlobalIdentifier(
            distinctGenericParameters,
            arg => ParseTypeName(arg.ToGlobalDisplayString()));
    }

    /// <summary>
    /// Returns the display string for the struct declaration (no global:: qualification).
    /// </summary>
    public string DeclarationDisplayString()
    {
        var distinctGenericParameters = GenericConstructorParameters
            .SelectMany(t => t.Type.GetGenericTypeArguments())
            .DistinctBy(symbol => symbol.GetEffectiveName())
            .ToArray();

        return distinctGenericParameters.Length > 0
            ? GenericName(Identifier(Name))
                .WithTypeArgumentList(
                    TypeArgumentList(SeparatedList<TypeSyntax>(
                        distinctGenericParameters
                            .Select(arg => IdentifierName(arg.GetEffectiveName())))))
                .NormalizeWhitespace()
                .ToString()
            : Name;
    }

    public INamespaceSymbol Namespace => RootType.ContainingNamespace;

    private ITypeParameterSymbol[] GetDistinctEffectiveTypeArguments() =>
        GenericConstructorParameters
            .SelectMany(t => t.Type.GetGenericTypeArguments())
            .DistinctBy(symbol => symbol.GetEffectiveName())
            .ToArray();

    private string BuildGlobalIdentifier<T>(
        T[] typeArguments,
        Func<T, TypeSyntax> argumentSelector)
    {
        var globalPrefix = Namespace.IsGlobalNamespace
            ? "global::"
            : $"global::{Namespace.ToDisplayString()}.";

        return typeArguments.Length > 0
            ? $"{globalPrefix}{GenericName(Identifier(Name))
                .WithTypeArgumentList(
                    TypeArgumentList(SeparatedList(
                        typeArguments.Select(argumentSelector))))
                .NormalizeWhitespace()}"
            : $"{globalPrefix}{Name}";
    }

    public int Index { get; set; }

    private string GetStepName(INamedTypeSymbol rootType)
    {
        var identifier = rootType.ToIdentifier();
        var name = $"Step_{Index}__{identifier}";

        return name;
    }
}
