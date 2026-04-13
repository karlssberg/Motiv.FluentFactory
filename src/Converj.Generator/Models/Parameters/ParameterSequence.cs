using System.Collections;
using System.Collections.Immutable;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Parameters;

internal class ParameterSequence : IEquatable<ParameterSequence>, IEnumerable<IParameterSymbol>
{
    private ImmutableArray<IParameterSymbol> ParameterSymbols { get; }
    private ImmutableArray<(string Type, string Name)> ParameterSymbolsPrecomputed { get; }
    private Dictionary<IParameterSymbol, FluentMethodParameter> FluentParameterMap { get; }

    private readonly int _hashCode;

    private ParameterSequence(
        ImmutableArray<IParameterSymbol> parameterSymbols,
        ImmutableArray<(string Type, string Name)> precomputed,
        Dictionary<IParameterSymbol, FluentMethodParameter> fluentParameterMap)
    {
        ParameterSymbols = parameterSymbols;
        ParameterSymbolsPrecomputed = precomputed;
        FluentParameterMap = fluentParameterMap;
        _hashCode = ComputeHashCode(precomputed);
    }

    public ParameterSequence()
        : this([], [], [])
    {
    }

    public ParameterSequence(IReadOnlyList<IParameterSymbol> parameterSymbols)
        : this(
            [
                ..parameterSymbols
            ],
            [
                ..parameterSymbols
                    .Select(p => (Type: p.Type.ToDisplayString(), Name: p.GetFluentMethodName()))
            ],
            [])
    {
    }

    /// <summary>
    /// Creates a parameter sequence from fluent method parameters, supporting both
    /// parameter-backed and property-backed entries.
    /// </summary>
    public ParameterSequence(IReadOnlyList<FluentMethodParameter> fluentParameters)
        : this(
            [
                ..fluentParameters
                .Where(fp => fp.ParameterSymbol is not null)
                .Select(fp => fp.ParameterSymbol!)
            ],
            [
                ..fluentParameters
                    .Select(fp => (Type: fp.SourceType.ToDisplayString(), Name: fp.Names.First()))
            ],
            BuildFluentParameterMap(fluentParameters))
    {
    }

    /// <summary>
    /// Gets the FluentMethodParameter associated with the given parameter symbol, if available.
    /// </summary>
    public FluentMethodParameter? GetFluentMethodParameter(IParameterSymbol parameter) =>
        FluentParameterMap.TryGetValue(parameter, out var fp)
            ? fp
            : null;

    private static Dictionary<IParameterSymbol, FluentMethodParameter> BuildFluentParameterMap(
        IEnumerable<FluentMethodParameter> fluentParameters)
    {
        return fluentParameters
            .Where(fp => fp.ParameterSymbol is not null)
            .ToDictionary(
                IParameterSymbol (fp) => fp.ParameterSymbol!,
                SymbolEqualityComparer.Default);
    }

    public IEnumerator<IParameterSymbol> GetEnumerator()
    {
        return ParameterSymbols.AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)ParameterSymbols).GetEnumerator();
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals(obj as ParameterSequence);
    }

    public bool Equals(ParameterSequence? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ParameterSymbolsPrecomputed.SequenceEqual(other.ParameterSymbolsPrecomputed);
    }

    public static implicit operator ImmutableArray<IParameterSymbol> (ParameterSequence knownTargetParameters)
    {
        return knownTargetParameters.ParameterSymbols;
    }

    public static bool operator ==(ParameterSequence left, ParameterSequence right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ParameterSequence left, ParameterSequence right)
    {
        return !(left == right);
    }

    private static int ComputeHashCode(ImmutableArray<(string Type, string Name)> precomputed) =>
        precomputed.Aggregate(
            101, (left, right) =>
                left * 397 ^ right.Type.GetHashCode() * 397 ^ right.Name.GetHashCode());
}
