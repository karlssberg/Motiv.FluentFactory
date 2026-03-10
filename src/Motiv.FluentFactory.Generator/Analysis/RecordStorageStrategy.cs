using Microsoft.CodeAnalysis;
using Motiv.FluentFactory.Generator.Model;
using Motiv.FluentFactory.Generator.Model.Storage;

namespace Motiv.FluentFactory.Generator.Analysis;

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
            var property = containingType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));

            if (property is null) continue;

            results[parameter] =
                new PropertyStorage(property.Name, property.Type, constructor.ContainingNamespace)
                {
                    DefinitionExists = true
                };
        }
    }
}
