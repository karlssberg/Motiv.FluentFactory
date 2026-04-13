using System.Collections.Immutable;
using System.Diagnostics;
using Converj.Generator.TargetAnalysis;
using Microsoft.CodeAnalysis;
using FluentPropertyMember = Converj.Generator.TargetAnalysis.FluentPropertyMember;

namespace Converj.Generator;

[DebuggerDisplay("{ToDisplayString()}")]
internal class TargetMetadata(FluentTargetContext targetContext)
{
    public IMethodSymbol Method { get; set; } = targetContext.Method;

    public IList<IMethodSymbol> CandidateTargets { get; } = [targetContext.Method];

    public TerminalMethodKind TerminalMethod { get; set; } = targetContext.TerminalMethod;

    public INamedTypeSymbol? ReturnType { get; } = targetContext.ReturnType;

    public bool HasEntryMethod { get; } = targetContext.HasEntryMethod;

    public string EntryMethodName { get; } = targetContext.EntryMethodName;

    /// <summary>
    /// Whether this target is a static method (not a constructor).
    /// When true, the terminal step calls the static method instead of new T(...).
    /// </summary>
    public bool IsStaticMethodTarget { get; } = targetContext.IsStaticMethodTarget;

    /// <summary>
    /// The extension receiver parameter, if this target uses extension method syntax.
    /// </summary>
    public IParameterSymbol? ReceiverParameter { get; } = targetContext.ReceiverParameter;

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; } =
        targetContext.ValueStorage;

    public FluentTargetContext Context { get; } = targetContext;

    /// <summary>
    /// Parameters with explicit default values that become optional fluent setter methods.
    /// </summary>
    public ImmutableArray<IParameterSymbol> OptionalParameters { get; } =
        [..targetContext.Method.Parameters.Where(p => p.HasExplicitDefaultValue)];

    /// <summary>
    /// The count of required (non-optional) parameters in the target method.
    /// </summary>
    public int RequiredParameterCount =>
        Method.Parameters.Length - OptionalParameters.Length;

    /// <summary>
    /// Required properties (C# required keyword or [Required] attribute) on the target type
    /// that need to be set via object initializer.
    /// </summary>
    public ImmutableArray<FluentPropertyMember> RequiredProperties { get; } =
        [..targetContext.TargetTypeProperties.Where(p => p.IsRequired)];

    /// <summary>
    /// Optional properties opted in via [FluentMethod] that become setter methods.
    /// </summary>
    public ImmutableArray<FluentPropertyMember> OptionalProperties { get; } =
        [..targetContext.TargetTypeProperties.Where(p => !p.IsRequired)];

    public TargetMetadata Clone()
    {
        return new TargetMetadata(Context);
    }

    public string ToDisplayString() => Method.ToDisplayString();
}
