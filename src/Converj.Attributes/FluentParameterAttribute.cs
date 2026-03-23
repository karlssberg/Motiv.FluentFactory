using System.Diagnostics.CodeAnalysis;

namespace Converj.Attributes;

/// <summary>
/// Marks a field or property as a pre-satisfied parameter that should be automatically
/// threaded to matching target constructor parameters, removing them from the fluent step chain.
/// When no parameter name is specified, the name is inferred from the member name
/// (stripping leading underscores and matching case-insensitively).
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
public class FluentParameterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance that infers the target parameter name from the annotated member.
    /// </summary>
    public FluentParameterAttribute() { }

    /// <summary>
    /// Initializes a new instance with an explicit target constructor parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the target constructor parameter to bind to.</param>
    public FluentParameterAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }

    /// <summary>
    /// The name of the target constructor parameter to bind to.
    /// When null, the name is inferred from the annotated member.
    /// </summary>
    public string? ParameterName { get; }
}
