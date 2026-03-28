using System.Diagnostics.CodeAnalysis;

namespace Converj.Attributes;

/// <summary>
/// Marks a constructor parameter or property to customize its fluent method name.
/// On constructor parameters, overrides the generated method name.
/// On properties, opts the property into the fluent chain (required for non-required properties)
/// and optionally overrides the method name.
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class FluentMethodAttribute : Attribute
{
    /// <summary>
    /// Opts a property into the fluent chain using the default method name derived from the property name.
    /// On constructor parameters, this has no effect (emits an informational diagnostic).
    /// </summary>
    public FluentMethodAttribute()
    {
    }

    /// <summary>
    /// Overrides the fluent method name for a constructor parameter or property.
    /// On properties, also opts the property into the fluent chain.
    /// </summary>
    /// <param name="methodName">The name of the method to generate.</param>
    public FluentMethodAttribute(string methodName)
    {
        MethodName = methodName;
    }

    /// <summary>
    /// The explicit method name, or null if the default name should be used.
    /// </summary>
    public string? MethodName { get; }
}
