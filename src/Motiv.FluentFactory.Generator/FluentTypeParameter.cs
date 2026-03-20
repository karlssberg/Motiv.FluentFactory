using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator;

internal class FluentTypeParameter(ITypeParameterSymbol typeParameterSymbol) : IEquatable<FluentTypeParameter>
{
    private readonly string _key = typeParameterSymbol.GetEffectiveName();

    public ITypeParameterSymbol TypeParameterSymbol { get; } = typeParameterSymbol;

    /// <summary>
    /// The effective name of this type parameter, using the [As] alias if present.
    /// </summary>
    public string Name => _key;

    public bool Equals(FluentTypeParameter? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _key.Equals(other._key);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((FluentTypeParameter)obj);
    }

    public override int GetHashCode() => _key.GetHashCode();

    public override string ToString() => _key;
}
