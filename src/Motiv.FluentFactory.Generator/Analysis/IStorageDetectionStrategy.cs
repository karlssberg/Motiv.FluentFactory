using Microsoft.CodeAnalysis;
using Motiv.FluentFactory.Generator.Model;
using Motiv.FluentFactory.Generator.Model.Storage;

namespace Motiv.FluentFactory.Generator.Analysis;

/// <summary>
/// Defines a strategy for detecting how constructor parameters are stored as fields or properties.
/// </summary>
internal interface IStorageDetectionStrategy
{
    /// <summary>
    /// Determines whether this strategy can handle the given constructor's storage detection.
    /// </summary>
    /// <param name="constructor">The constructor to evaluate.</param>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    /// <returns><c>true</c> if this strategy handles the constructor; otherwise, <c>false</c>.</returns>
    bool CanHandle(IMethodSymbol constructor, SemanticModel semanticModel);

    /// <summary>
    /// Populates the storage mappings for constructor parameters.
    /// </summary>
    /// <param name="constructor">The constructor whose parameters to analyze.</param>
    /// <param name="results">The dictionary to populate with parameter-to-storage mappings.</param>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    void PopulateStorage(
        IMethodSymbol constructor,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> results,
        SemanticModel semanticModel);
}
