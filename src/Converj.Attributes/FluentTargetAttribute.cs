using System.Diagnostics.CodeAnalysis;

namespace Converj.Attributes;

/// <summary>
/// Marks a constructor, class, struct, or static method as a target for fluent builder generation
/// on the specified root type.
/// </summary>
/// <param name="rootType">The root type to generate fluent methods on.</param>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true)]
public class FluentTargetAttribute(Type rootType) : Attribute
{
    /// <summary>
    /// The root type to generate fluent methods on.
    /// </summary>
    public Type RootType { get; } = rootType;

    /// <summary>
    /// Controls how the builder chain is structured for this target.
    /// </summary>
    public TerminalMethod TerminalMethod { get; set; }

    /// <summary>
    /// The verb used for the terminal method name. If not set, "Create" will be used.
    /// In <see cref="Attributes.TerminalMethod.DynamicSuffix"/> mode, the target type name is appended (e.g., "Create" + "User" = "CreateUser").
    /// In <see cref="Attributes.TerminalMethod.FixedName"/> mode, the verb is used as-is.
    /// </summary>
    public string? TerminalVerb { get; set; }

    /// <summary>
    /// The prefix used for fluent method names. If not set, inherits from the root default or "With".
    /// For example, setting this to "Having" generates "HavingValue" instead of "WithValue".
    /// An empty string produces bare parameter names (e.g., "Value").
    /// </summary>
    public string? MethodPrefix { get; set; }

    /// <summary>
    /// The return type for the terminal method. If not set, inherits from the root default or uses the concrete target type.
    /// When set, the generated terminal method returns this type instead of the concrete target type.
    /// The target type must be assignable to this type (e.g., implement the interface or extend the base class).
    /// </summary>
    public Type? ReturnType { get; set; }
}

/// <summary>
/// Generic variant of <see cref="FluentTargetAttribute"/> for C# 11+ projects.
/// Marks a constructor, class, struct, or static method as a target for fluent builder generation.
/// </summary>
/// <typeparam name="TFluentRoot">The root type to generate fluent methods on.</typeparam>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true)]
public sealed class FluentTargetAttribute<TFluentRoot> : FluentTargetAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FluentTargetAttribute{TFluentRoot}"/> class.
    /// </summary>
    public FluentTargetAttribute() : base(typeof(TFluentRoot))
    {
    }
}
