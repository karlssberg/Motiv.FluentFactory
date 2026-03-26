using System.Diagnostics.CodeAnalysis;

namespace Converj.Attributes;

/// <summary>
/// Marks a field or property on a target type as explicit storage for a constructor parameter.
/// Used when a type has multiple <see cref="FluentConstructorAttribute"/> entries with
/// <c>CreateMethod.None</c> and the generator cannot auto-discover storage for parameters
/// beyond the shortest constructor's parameter list.
/// When no parameter name is specified, the name is inferred from the member name
/// (stripping leading underscores and matching case-insensitively).
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class FluentStorageAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance that infers the target parameter name from the annotated member.
    /// </summary>
    public FluentStorageAttribute() { }

    /// <summary>
    /// Initializes a new instance with an explicit target constructor parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the constructor parameter to provide storage for.</param>
    public FluentStorageAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }

    /// <summary>
    /// The name of the constructor parameter to provide storage for.
    /// When null, the name is inferred from the annotated member.
    /// </summary>
    public string? ParameterName { get; }
}
