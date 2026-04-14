using System.Collections.Immutable;
using Converj.Generator.Models.Parameters;
using Converj.Generator.Models.Steps;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Methods;

/// <summary>
/// Represents the bridge method that replaces the terminal on the last regular trie step when the
/// constructor target has collection parameters.  Instead of creating the target type directly, this
/// method transitions to an <see cref="AccumulatorFluentStep"/> where the caller can call
/// <c>AddX</c> methods before finalising with the actual terminal.
/// </summary>
internal class AccumulatorTransitionMethod : IFluentMethod
{
    /// <summary>
    /// Initialises a new <see cref="AccumulatorTransitionMethod"/>.
    /// </summary>
    /// <param name="name">
    /// The method name used on the preceding regular step.  By default this matches the terminal verb
    /// that the regular trie would have produced (e.g., <c>Create</c> or <c>CreateTarget</c>),
    /// but Plan 22-04 controls the final name by passing it here without reopening this file.
    /// </param>
    /// <param name="returnStep">The <see cref="AccumulatorFluentStep"/> this method transitions to.</param>
    /// <param name="rootNamespace">The namespace of the fluent root type.</param>
    /// <param name="availableParameterFields">
    /// The fields on the preceding regular step, used by the caller context during generation.
    /// </param>
    /// <param name="methodParameters">
    /// The method parameters for this transition.  Usually empty because all regular parameters are
    /// already captured by preceding <c>With</c> methods.  For the "all collection, no regular
    /// parameters" case the caller may supply forwarded parameters here.
    /// </param>
    /// <param name="valueSources">Value-source map from the preceding step.</param>
    public AccumulatorTransitionMethod(
        string name,
        AccumulatorFluentStep returnStep,
        INamespaceSymbol rootNamespace,
        ImmutableArray<FluentMethodParameter> availableParameterFields,
        ImmutableArray<FluentMethodParameter> methodParameters,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueSources)
    {
        Name = name;
        Return = returnStep;
        RootNamespace = rootNamespace;
        AvailableParameterFields = availableParameterFields;
        MethodParameters = methodParameters;
        ValueSources = valueSources;
    }

    /// <summary>Gets the name of this transition method as set by the caller (Plan 22-04).</summary>
    public string Name { get; }

    /// <summary>Gets the accumulator step that this method returns.</summary>
    public IFluentReturn Return { get; }

    /// <summary>
    /// Gets <see langword="null"/> — transition methods do not correspond to a single source parameter;
    /// they represent the full set of regular parameters already captured by the preceding step.
    /// </summary>
    public IParameterSymbol? SourceParameter => null;

    /// <summary>Gets the method parameters for this transition (usually empty).</summary>
    public ImmutableArray<FluentMethodParameter> MethodParameters { get; }

    /// <summary>Gets the forwarded fields from the preceding regular step.</summary>
    public ImmutableArray<FluentMethodParameter> AvailableParameterFields { get; }

    /// <summary>Transition methods introduce no new generic type parameters.</summary>
    public ImmutableArray<FluentTypeParameter> TypeParameters => [];

    /// <summary>Gets the namespace of the fluent root type.</summary>
    public INamespaceSymbol RootNamespace { get; }

    /// <summary>Gets the value-source map from the preceding step.</summary>
    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueSources { get; }

    /// <inheritdoc/>
    public string? DocumentationSummary => null;

    /// <inheritdoc/>
    public Dictionary<string, string>? ParameterDocumentation => null;
}
