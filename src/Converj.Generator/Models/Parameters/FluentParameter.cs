using Microsoft.CodeAnalysis;

namespace Converj.Generator;


internal class FluentMethodParameter : IEquatable<FluentMethodParameter>
{
    /// <summary>
    /// Creates a parameter-backed fluent method parameter.
    /// </summary>
    public FluentMethodParameter(
        IParameterSymbol parameterSymbol,
        IEnumerable<string> names)
    {
        ParameterSymbol = parameterSymbol;
        SourceName = parameterSymbol.Name;
        SourceType = parameterSymbol.Type;
        FluentType = new FluentType(parameterSymbol.Type);
        Names = new HashSet<string>(names);
    }

    /// <summary>
    /// Creates a parameter-backed fluent method parameter with a single name.
    /// </summary>
    public FluentMethodParameter(
        IParameterSymbol parameterSymbol,
        string name)
        : this(parameterSymbol, [name])
    {
    }

    /// <summary>
    /// Creates a property-backed fluent method parameter.
    /// </summary>
    public FluentMethodParameter(
        IPropertySymbol propertySymbol,
        IEnumerable<string> names)
    {
        SourceProperty = propertySymbol;
        SourceName = propertySymbol.Name;
        SourceType = propertySymbol.Type;
        FluentType = new FluentType(propertySymbol.Type);
        Names = new HashSet<string>(names);
    }

    /// <summary>
    /// Creates a property-backed fluent method parameter with a single name.
    /// </summary>
    public FluentMethodParameter(
        IPropertySymbol propertySymbol,
        string name)
        : this(propertySymbol, [name])
    {
    }

    /// <summary>
    /// The underlying constructor parameter symbol, or null if this is property-backed.
    /// </summary>
    public IParameterSymbol? ParameterSymbol { get; }

    /// <summary>
    /// The underlying property symbol, or null if this is parameter-backed.
    /// </summary>
    public IPropertySymbol? SourceProperty { get; }

    /// <summary>
    /// The name of the source member (parameter name or property name).
    /// </summary>
    public string SourceName { get; }

    /// <summary>
    /// The type of the source member.
    /// </summary>
    public ITypeSymbol SourceType { get; }

    /// <summary>
    /// Whether this fluent method parameter is backed by a property (set via object initializer)
    /// rather than a constructor parameter.
    /// </summary>
    public bool IsPropertyBacked => SourceProperty is not null;

    public FluentType FluentType { get; }

    public ISet<string> Names { get; }

    public bool Equals(FluentMethodParameter? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (!FluentType.Equals(other.FluentType)) return false;
        return Names.Overlaps(other.Names);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((FluentMethodParameter)obj);
    }

    public override int GetHashCode() => FluentType.GetHashCode();

    public override string ToString()
    {
        var names = Names.Select(name => $"'{name}'");
        return $"{FluentType} ({string.Join(", ", names)})";
    }
}
