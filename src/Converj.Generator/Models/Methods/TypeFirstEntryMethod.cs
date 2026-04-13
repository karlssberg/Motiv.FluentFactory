using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Methods;

/// <summary>
/// Represents a parameterless type-first entry method on the factory root type
/// (e.g., <c>BuildDog()</c>) that returns the first step in a type-scoped builder chain.
/// </summary>
internal class TypeFirstEntryMethod(
    string name,
    IFluentStep rootStep,
    INamespaceSymbol rootNamespace,
    ImmutableArray<IMethodSymbol> candidateTargets) : IFluentMethod
{
    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public IParameterSymbol? SourceParameter => null;

    /// <inheritdoc />
    public ImmutableArray<FluentMethodParameter> AvailableParameterFields { get; } = [];

    /// <inheritdoc />
    public IFluentReturn Return { get; } = rootStep;

    /// <inheritdoc />
    public ImmutableArray<FluentTypeParameter> TypeParameters { get; } = [];

    /// <inheritdoc />
    public INamespaceSymbol RootNamespace { get; } = rootNamespace;

    /// <inheritdoc />
    public ImmutableArray<FluentMethodParameter> MethodParameters { get; } = [];

    /// <inheritdoc />
    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueSources { get; } = new();

    /// <inheritdoc />
    public string? DocumentationSummary => null;

    /// <inheritdoc />
    public Dictionary<string, string>? ParameterDocumentation => null;

    /// <summary>
    /// The candidate constructors reachable from this type-first entry point.
    /// </summary>
    public ImmutableArray<IMethodSymbol> CandidateTargets { get; } = candidateTargets;
}
