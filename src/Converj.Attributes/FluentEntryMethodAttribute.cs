using System.Diagnostics.CodeAnalysis;

namespace Converj.Attributes;

/// <summary>
/// Marks a constructor, class, struct, or method as having a type-first entry method.
/// When applied alongside <see cref="FluentTargetAttribute"/>, consumers select
/// the target type up front via a parameterless entry method, then fill in only
/// that type's parameters (e.g., <c>Factory.BuildDog().WithName("Rex").Create()</c>).
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(
    AttributeTargets.Constructor | AttributeTargets.Class |
    AttributeTargets.Struct | AttributeTargets.Method)]
public class FluentEntryMethodAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FluentEntryMethodAttribute"/> class.
    /// </summary>
    /// <param name="name">The full method identifier for the entry method (e.g., "BuildDog").</param>
    public FluentEntryMethodAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The full method identifier for the entry method.
    /// </summary>
    public string Name { get; }
}
