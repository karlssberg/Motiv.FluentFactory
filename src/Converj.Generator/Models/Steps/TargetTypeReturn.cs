using System.Collections.Immutable;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Steps;

internal class TargetTypeReturn(
    IMethodSymbol targetTypeConstructor,
    ImmutableArray<IMethodSymbol> candidateConstructors,
    ParameterSequence knownConstructorParameters,
    INamedTypeSymbol? returnTypeOverride = null,
    INamedTypeSymbol? staticMethodReturnType = null) : IFluentReturn
{
    public ImmutableArray<IParameterSymbol> GenericConstructorParameters { get; } =
    [
        ..knownConstructorParameters
            .Where(parameter => parameter.Type.IsOpenGenericType())
    ];

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; set; } = new();

    public ImmutableArray<IMethodSymbol> CandidateTargets => candidateConstructors;

    public ImmutableArray<IMethodSymbol> UnavailableTargets { get; set; } = [];

    public IMethodSymbol Constructor { get; set; } = targetTypeConstructor;

    /// <summary>
    /// Whether this return targets a static method invocation instead of object creation.
    /// </summary>
    public bool IsStaticMethodTarget => staticMethodReturnType is not null;

    public string IdentifierDisplayString() =>
        IdentifierDisplayString(new Dictionary<FluentType, ITypeSymbol>());

    public string IdentifierDisplayString(
        IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap) =>
        ConstructAndDisplay(staticMethodReturnType ?? Constructor.ContainingType, genericTypeArgumentMap);

    /// <summary>
    /// Returns the identifier display string with type parameter names remapped
    /// to match the local scope of an existing partial type.
    /// </summary>
    public string IdentifierDisplayString(IDictionary<string, string> effectiveToLocalNameMap) =>
        ConstructAndDisplay(Constructor.ContainingType, effectiveToLocalNameMap);

    /// <summary>
    /// Returns the display string for the method return type, using the override if set.
    /// </summary>
    public string ReturnTypeDisplayString() =>
        returnTypeOverride?.ToGlobalDisplayString()
        ?? staticMethodReturnType?.ToGlobalDisplayString()
        ?? IdentifierDisplayString();

    /// <summary>
    /// Returns the display string for the method return type, applying generic type argument mappings.
    /// </summary>
    public string ReturnTypeDisplayString(IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap) =>
        ConstructAndDisplay(returnTypeOverride ?? Constructor.ContainingType, genericTypeArgumentMap);

    /// <summary>
    /// Returns the display string for the method return type, remapping type parameter names
    /// to match the local scope of an existing partial type.
    /// </summary>
    public string ReturnTypeDisplayString(IDictionary<string, string> effectiveToLocalNameMap)
    {
        var type = returnTypeOverride ?? Constructor.ContainingType;
        return ConstructAndDisplay(type, effectiveToLocalNameMap);
    }

    private static string ConstructAndDisplay(
        INamedTypeSymbol type,
        IDictionary<string, string> effectiveToLocalNameMap)
    {
        var allArgs = type.TypeParameters
            .Select(typeParameter => (ITypeSymbol)typeParameter)
            .ToArray();

        var constructedType = allArgs.Length > 0
            ? type.Construct(allArgs)
            : type;

        return constructedType.ToGlobalDisplayString(effectiveToLocalNameMap);
    }

    private static string ConstructAndDisplay(
        INamedTypeSymbol type,
        IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap)
    {
        var allArgs = type.TypeParameters
            .Select(typeParameter => genericTypeArgumentMap.TryGetValue(new FluentType(typeParameter), out var mapped)
                ? mapped
                : typeParameter)
            .ToArray();

        var constructedType = allArgs.Length > 0
            ? type.Construct(allArgs)
            : type;

        return constructedType.ToGlobalDisplayString();
    }

    public INamespaceSymbol Namespace => Constructor.ContainingNamespace;
    public ParameterSequence KnownConstructorParameters { get; } = knownConstructorParameters;
}
