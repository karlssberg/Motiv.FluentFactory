using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Converj.Generator.ConstructorAnalysis;
using FluentPropertyMember = Converj.Generator.ConstructorAnalysis.FluentPropertyMember;

namespace Converj.Generator;

[DebuggerDisplay("{ToDisplayString()}")]
internal class ConstructorMetadata(FluentTargetContext targetContext)
{
    public IMethodSymbol Constructor { get; set; } = targetContext.Constructor;

    public IList<IMethodSymbol> CandidateConstructors { get; } = [targetContext.Constructor];

    public TerminalMethodKind TerminalMethod { get; set; } = targetContext.TerminalMethod;

    public INamedTypeSymbol? ReturnType { get; } = targetContext.ReturnType;

    public bool HasEntryMethod { get; } = targetContext.HasEntryMethod;

    public string EntryMethodName { get; } = targetContext.EntryMethodName;

    /// <summary>
    /// Whether this target is a static method (not a constructor).
    /// When true, the terminal step calls the static method instead of new T(...).
    /// </summary>
    public bool IsStaticMethodTarget { get; } = targetContext.IsStaticMethodTarget;

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; } =
        targetContext.ValueStorage;

    public FluentTargetContext Context { get; } = targetContext;

    /// <summary>
    /// Parameters with explicit default values that become optional fluent setter methods.
    /// </summary>
    public ImmutableArray<IParameterSymbol> OptionalParameters { get; } =
        [..targetContext.Constructor.Parameters.Where(p => p.HasExplicitDefaultValue)];

    /// <summary>
    /// The count of required (non-optional) parameters in the constructor.
    /// </summary>
    public int RequiredParameterCount =>
        Constructor.Parameters.Length - OptionalParameters.Length;

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

    public ConstructorMetadata Clone()
    {
        return new ConstructorMetadata(Context);
    }

    public string ToDisplayString() => Constructor.ToDisplayString();
}
