using System.Diagnostics.CodeAnalysis;

namespace Converg.Attributes;

/// <summary>
/// Marks a class or struct to be used as a fluent factory.
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class FluentFactoryAttribute : Attribute
{
    /// <summary>
    /// Controls how the terminal Create method is generated for all constructors in this factory.
    /// Can be overridden per-constructor via <see cref="FluentConstructorAttribute.CreateMethod"/>.
    /// </summary>
    public CreateMethod CreateMethod { get; set; }

    /// <summary>
    /// The default verb used for the Create method name across all constructors in this factory.
    /// Can be overridden per-constructor via <see cref="FluentConstructorAttribute.CreateVerb"/>.
    /// </summary>
    public string? CreateVerb { get; set; }

    /// <summary>
    /// The default prefix used for fluent method names across all constructors in this factory.
    /// For example, setting this to "Having" generates "HavingValue" instead of "WithValue".
    /// An empty string produces bare parameter names (e.g., "Value").
    /// Can be overridden per-constructor via <see cref="FluentConstructorAttribute.MethodPrefix"/>.
    /// </summary>
    public string? MethodPrefix { get; set; }

    /// <summary>
    /// The default return type for creation methods across all constructors in this factory.
    /// When set, the generated Create methods return this type instead of the concrete target type.
    /// The target type must be assignable to this type (e.g., implement the interface or extend the base class).
    /// Can be overridden per-constructor via <see cref="FluentConstructorAttribute.ReturnType"/>.
    /// </summary>
    public Type? ReturnType { get; set; }
}
