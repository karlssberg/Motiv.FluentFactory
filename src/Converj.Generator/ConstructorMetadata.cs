using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Converj.Generator.ConstructorAnalysis;
using FluentPropertyMember = Converj.Generator.ConstructorAnalysis.FluentPropertyMember;

namespace Converj.Generator;

[DebuggerDisplay("{ToDisplayString()}")]
internal class ConstructorMetadata(FluentConstructorContext constructorContext)
{
    public IMethodSymbol Constructor { get; set; } = constructorContext.Constructor;

    public IList<IMethodSymbol> CandidateConstructors { get; } = [constructorContext.Constructor];

    public CreateMethodMode CreateMethod { get; set; } = constructorContext.CreateMethod;

    public INamedTypeSymbol? ReturnType { get; } = constructorContext.ReturnType;

    public BuilderMode BuilderMode { get; } = constructorContext.BuilderMode;

    public string TypeFirstVerb { get; } = constructorContext.TypeFirstVerb;

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; } =
        constructorContext.ValueStorage;

    public FluentConstructorContext Context { get; } = constructorContext;

    /// <summary>
    /// Parameters with explicit default values that become optional fluent setter methods.
    /// </summary>
    public ImmutableArray<IParameterSymbol> OptionalParameters { get; } =
        [..constructorContext.Constructor.Parameters.Where(p => p.HasExplicitDefaultValue)];

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
        [..constructorContext.TargetTypeProperties.Where(p => p.IsRequired)];

    /// <summary>
    /// Optional properties opted in via [FluentMethod] that become setter methods.
    /// </summary>
    public ImmutableArray<FluentPropertyMember> OptionalProperties { get; } =
        [..constructorContext.TargetTypeProperties.Where(p => !p.IsRequired)];

    public ConstructorMetadata Clone()
    {
        return new ConstructorMetadata(Context);
    }

    public string ToDisplayString() => Constructor.ToDisplayString();
}
