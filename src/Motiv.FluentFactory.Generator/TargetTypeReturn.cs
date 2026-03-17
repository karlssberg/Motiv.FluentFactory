using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator;

internal class TargetTypeReturn(
    IMethodSymbol targetTypeConstructor,
    ImmutableArray<IMethodSymbol> candidateConstructors,
    ParameterSequence knownConstructorParameters,
    INamedTypeSymbol? returnTypeOverride = null) : IFluentReturn
{
    public ImmutableArray<IParameterSymbol> GenericConstructorParameters { get; } =
    [
        ..knownConstructorParameters
            .Where(parameter => parameter.Type.IsOpenGenericType())
    ];

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; set; } = new();

    public ImmutableArray<IMethodSymbol> CandidateConstructors => candidateConstructors;

    public IMethodSymbol Constructor => targetTypeConstructor;

    public string IdentifierDisplayString() => 
        IdentifierDisplayString(new Dictionary<FluentType, ITypeSymbol>());

    public string IdentifierDisplayString(
        IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap) =>
        ConstructAndDisplay(targetTypeConstructor.ContainingType, genericTypeArgumentMap);

    /// <summary>
    /// Returns the display string for the method return type, using the override if set.
    /// </summary>
    public string ReturnTypeDisplayString() =>
        returnTypeOverride?.ToGlobalDisplayString() ?? IdentifierDisplayString();

    /// <summary>
    /// Returns the display string for the method return type, applying generic type argument mappings.
    /// </summary>
    public string ReturnTypeDisplayString(IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap) => 
        ConstructAndDisplay(returnTypeOverride ?? targetTypeConstructor.ContainingType, genericTypeArgumentMap);

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

    public INamespaceSymbol Namespace => targetTypeConstructor.ContainingNamespace;
    public ParameterSequence KnownConstructorParameters { get; } = knownConstructorParameters;
}
