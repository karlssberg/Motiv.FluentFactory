using System.Diagnostics.CodeAnalysis;

namespace Motiv.FluentFactory.Attributes;

/// <summary>
/// Marks a constructor, class or struct to be used as a fluent factory for the specified root type.
/// </summary>
/// <param name="rootType">The type to create instances of.</param>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class FluentConstructorAttribute(Type rootType) : Attribute
{
    /// <summary>
    /// The type to create instances of.
    /// </summary>
    public Type RootType { get; } = rootType;

    /// <summary>
    /// Controls how the terminal Create method is generated.
    /// </summary>
    public CreateMethod CreateMethod { get; set; }

    /// <summary>
    /// The verb used for the Create method name. If not set, "Create" will be used.
    /// In <see cref="Attributes.CreateMethod.Dynamic"/> mode, the target type name is appended (e.g., "Create" + "User" = "CreateUser").
    /// In <see cref="Attributes.CreateMethod.Fixed"/> mode, the verb is used as-is.
    /// </summary>
    public string? CreateVerb { get; set; }

    /// <summary>
    /// The prefix used for fluent method names. If not set, inherits from the factory default or "With".
    /// For example, setting this to "Having" generates "HavingValue" instead of "WithValue".
    /// An empty string produces bare parameter names (e.g., "Value").
    /// </summary>
    public string? MethodPrefix { get; set; }

    /// <summary>
    /// The return type for the creation method. If not set, inherits from the factory default or uses the concrete target type.
    /// When set, the generated Create method returns this type instead of the concrete target type.
    /// The target type must be assignable to this type (e.g., implement the interface or extend the base class).
    /// </summary>
    public Type? ReturnType { get; set; }
}

/// <summary>
/// Generic variant of <see cref="FluentConstructorAttribute"/> for C# 11+ projects.
/// Marks a constructor, class or struct to be used as a fluent factory for the specified root type.
/// </summary>
/// <typeparam name="TFluentFactory">The factory type to generate fluent methods on.</typeparam>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class FluentConstructorAttribute<TFluentFactory> : FluentConstructorAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FluentConstructorAttribute{TFluentFactory}"/> class.
    /// </summary>
    public FluentConstructorAttribute() : base(typeof(TFluentFactory))
    {
    }
}
