using Microsoft.CodeAnalysis;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Represents a property on a target type that participates in the fluent chain,
/// either because it has the <c>required</c> keyword / <c>[Required]</c> attribute,
/// or because it was opted in with <c>[FluentMethod]</c>.
/// </summary>
internal class FluentPropertyMember(
    string fluentMethodName,
    IPropertySymbol property,
    bool isRequired,
    Location location)
{
    /// <summary>
    /// The name to use for the fluent method (e.g., "WithEmail").
    /// Derived from the property name or overridden via [FluentMethod("CustomName")].
    /// </summary>
    public string FluentMethodName { get; } = fluentMethodName;

    /// <summary>
    /// The property symbol on the target type.
    /// </summary>
    public IPropertySymbol Property { get; } = property;

    /// <summary>
    /// True if the property is required (C# <c>required</c> keyword or <c>[Required]</c> attribute).
    /// </summary>
    public bool IsRequired { get; } = isRequired;

    /// <summary>
    /// The type of the property.
    /// </summary>
    public ITypeSymbol Type { get; } = property.Type;

    /// <summary>
    /// The source location for diagnostic reporting.
    /// </summary>
    public Location Location { get; } = location;
}
