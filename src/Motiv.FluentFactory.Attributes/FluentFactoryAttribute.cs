using System.Diagnostics.CodeAnalysis;

namespace Motiv.FluentFactory.Attributes;

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
}
