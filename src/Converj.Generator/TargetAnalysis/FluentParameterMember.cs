using Microsoft.CodeAnalysis;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Represents a field or property on a factory type marked with [FluentParameter],
/// which provides a pre-satisfied value to be threaded to target constructor parameters.
/// </summary>
internal class FluentParameterMember(
    string targetParameterName,
    ITypeSymbol type,
    string memberIdentifierName,
    bool isProperty,
    Location location,
    bool requiresGeneratedField = false,
    string? primaryConstructorParameterName = null,
    bool isImplicit = false)
{
    /// <summary>
    /// The target constructor parameter name to bind to (from the attribute).
    /// </summary>
    public string TargetParameterName { get; } = targetParameterName;

    /// <summary>
    /// The type of the field or property.
    /// </summary>
    public ITypeSymbol Type { get; } = type;

    /// <summary>
    /// The identifier name used to access the member (e.g., "_wheels" or "Wheels").
    /// </summary>
    public string MemberIdentifierName { get; } = memberIdentifierName;

    /// <summary>
    /// True if the member is a property; false if a field.
    /// </summary>
    public bool IsProperty { get; } = isProperty;

    /// <summary>
    /// The source location for diagnostic reporting.
    /// </summary>
    public Location Location { get; } = location;

    /// <summary>
    /// When true, a backing field needs to be generated in the partial type because
    /// no explicit storage exists for this primary constructor parameter.
    /// </summary>
    public bool RequiresGeneratedField { get; } = requiresGeneratedField;

    /// <summary>
    /// The primary constructor parameter name used to initialize the generated field.
    /// Only set when <see cref="RequiresGeneratedField"/> is true.
    /// </summary>
    public string? PrimaryConstructorParameterName { get; } = primaryConstructorParameterName;

    /// <summary>
    /// True when this member was auto-detected from a record primary constructor parameter
    /// rather than explicitly attributed with [FluentParameter]. Implicit members are
    /// silently skipped when no matching target parameters exist.
    /// </summary>
    public bool IsImplicit { get; } = isImplicit;
}
