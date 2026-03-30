using Microsoft.CodeAnalysis;
using Converj.Generator.ConstructorAnalysis;

namespace Converj.Generator.Models.Parameters;

/// <summary>
/// Represents a binding between a factory member marked with [FluentParameter]
/// and a target constructor parameter it satisfies.
/// </summary>
internal class FluentParameterBinding(FluentParameterMember factoryMember, IParameterSymbol targetParameter)
{
    /// <summary>
    /// The factory field/property providing the value.
    /// </summary>
    public FluentParameterMember FactoryMember { get; } = factoryMember;

    /// <summary>
    /// The target constructor parameter being satisfied.
    /// </summary>
    public IParameterSymbol TargetParameter { get; } = targetParameter;
}
