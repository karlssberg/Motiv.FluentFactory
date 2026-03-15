using System.Diagnostics.CodeAnalysis;

namespace Motiv.FluentFactory.Attributes;

/// <summary>
/// Marks a constructor, class or struct to be used as a fluent factory for the specified root type.
/// </summary>
/// <param name="rootType">The type to create instances of.</param>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class FluentConstructorAttribute(Type rootType) : Attribute
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
}
