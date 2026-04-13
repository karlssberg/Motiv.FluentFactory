using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Steps;

internal interface IFluentReturn
{
    /// <summary>
    /// Returns the display string for this return type, using global:: qualification.
    /// </summary>
    string IdentifierDisplayString();

    /// <summary>
    /// Returns the display string for this return type with generic type argument mappings applied.
    /// </summary>
    string IdentifierDisplayString(IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap);

    INamespaceSymbol Namespace { get; }

    /// <summary>
    /// The known constructor parameters up until this step.
    /// Potentially more parameters are required to satisfy a constructor signature.
    /// </summary>
    ParameterSequence KnownTargetParameters { get; }

    ImmutableArray<IParameterSymbol> GenericTargetParameters { get; }

    OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; }

    /// <summary>
    /// The full set of target methods (constructors or static factory methods) that the
    /// trie associated with this return node during merge. Construction-phase code uses this
    /// to reason about all possible paths; syntax generation should prefer the filtered view
    /// via the GetAvailableTargets extension method.
    /// </summary>
    ImmutableArray<IMethodSymbol> CandidateTargets { get; }

    /// <summary>
    /// The subset of <see cref="CandidateTargets"/> that have been determined unreachable
    /// (e.g., superseded by a competing method during selection). Populated as a post-processing
    /// step once method selection is complete, so seealso/doc generation can exclude them.
    /// </summary>
    ImmutableArray<IMethodSymbol> UnavailableTargets { get; set; }
}
