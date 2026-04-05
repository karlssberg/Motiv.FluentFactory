using System.Diagnostics.CodeAnalysis;

namespace Converj.Attributes;

/// <summary>
/// Marks a class or struct to be used as the root of a fluent builder chain.
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class FluentRootAttribute : Attribute
{
    /// <summary>
    /// Controls how the builder chain is structured for all targets in this root.
    /// Can be overridden per-target via <see cref="FluentTargetAttribute.BuilderMethod"/>.
    /// </summary>
    public BuilderMethod BuilderMethod { get; set; }

    /// <summary>
    /// The default verb used for the terminal method name across all targets in this root.
    /// Can be overridden per-target via <see cref="FluentTargetAttribute.TerminalVerb"/>.
    /// </summary>
    public string? TerminalVerb { get; set; }

    /// <summary>
    /// The default prefix used for fluent method names across all targets in this root.
    /// For example, setting this to "Having" generates "HavingValue" instead of "WithValue".
    /// An empty string produces bare parameter names (e.g., "Value").
    /// Can be overridden per-target via <see cref="FluentTargetAttribute.MethodPrefix"/>.
    /// </summary>
    public string? MethodPrefix { get; set; }

    /// <summary>
    /// The default return type for terminal methods across all targets in this root.
    /// When set, the generated terminal methods return this type instead of the concrete target type.
    /// The target type must be assignable to this type (e.g., implement the interface or extend the base class).
    /// Can be overridden per-target via <see cref="FluentTargetAttribute.ReturnType"/>.
    /// </summary>
    public Type? ReturnType { get; set; }

    /// <summary>
    /// When true, allows <see cref="FluentParameterAttribute"/> members to match only a subset
    /// of target constructors. By default, a fluent parameter must match all target constructors
    /// or a diagnostic error is reported.
    /// </summary>
    public bool AllowPartialParameterOverlap { get; set; }

    /// <summary>
    /// The verb used for initial method names when <see cref="BuilderMethod"/> is <see cref="Attributes.BuilderMethod.Eager"/>. Default is "Build".
    /// For example, "Build" + "Dog" = <c>BuildDog()</c>.
    /// Can be overridden per-target via <see cref="FluentTargetAttribute.EagerVerb"/>.
    /// </summary>
    public string? InitialVerb { get; set; }
}
