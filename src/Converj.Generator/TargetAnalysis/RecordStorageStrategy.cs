using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Detects storage for record primary constructor parameters, which automatically become properties.
/// </summary>
internal class RecordStorageStrategy : IStorageDetectionStrategy
{
    /// <inheritdoc />
    public bool CanHandle(IMethodSymbol constructor, SemanticModel semanticModel) =>
        constructor.ContainingType.IsRecord;

    /// <inheritdoc />
    public void PopulateStorage(
        IMethodSymbol constructor,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> results,
        SemanticModel semanticModel)
    {
        var containingType = constructor.ContainingType;

        foreach (var parameter in constructor.Parameters)
        {
            var property = containingType.FindRecordProperty(parameter);
            if (property is null) continue;

            results[parameter] =
                new PropertyStorage(property.Name, property.Type, constructor.ContainingNamespace)
                {
                    DefinitionExists = true
                };
        }
    }
}
