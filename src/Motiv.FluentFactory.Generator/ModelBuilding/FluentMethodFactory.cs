using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Motiv.FluentFactory.Generator.Diagnostics;
using Motiv.FluentFactory.Generator.SyntaxGeneration.Helpers;

namespace Motiv.FluentFactory.Generator.ModelBuilding;

/// <summary>
/// Creates fluent method instances (RegularMethod and MultiMethod) from
/// trie node data, handling multi-method template resolution and validation.
/// </summary>
internal class FluentMethodFactory(
    Compilation compilation,
    DiagnosticList diagnostics)
{
    /// <summary>
    /// Creates fluent methods for a set of parameter instances at a trie node.
    /// </summary>
    /// <param name="rootType">The root type being built by the fluent factory.</param>
    /// <param name="node">The current trie node.</param>
    /// <param name="fluentParameterInstances">The parameter instances at this node.</param>
    /// <param name="nextStep">The next fluent step, or null if this is the final step.</param>
    /// <param name="constructorMetadataList">The constructor metadata for this node.</param>
    /// <param name="valueStorages">The accumulated value storages.</param>
    /// <returns>An enumerable of fluent methods.</returns>
    public IEnumerable<IFluentMethod> CreateFluentMethods(
        INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node,
        ICollection<FluentMethodParameter> fluentParameterInstances,
        IFluentStep? nextStep,
        IList<ConstructorMetadata> constructorMetadataList,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages)
    {
        var constructorMetadata = MergeConstructorMetadata(node, constructorMetadataList);
        IFluentReturn methodReturn = nextStep switch
        {
            null => new TargetTypeReturn(
                constructorMetadata.Constructor,
                [..constructorMetadata.CandidateConstructors],
                new ParameterSequence(node.Key.Select(p => p.ParameterSymbol))),
            _ => nextStep
        };

        foreach (var parameter in fluentParameterInstances)
        {
            var multipleFluentMethodInfo = compilation
                .GetMultipleFluentMethodSymbols(parameter.ParameterSymbol)
                .ToList();

            ValidateMultipleFluentMethodCompatibility(parameter, multipleFluentMethodInfo);

            var normalizedFluentMethodSymbols = multipleFluentMethodInfo
                .Where(methodInfo => methodInfo.Diagnostics.Count == 0)
                .Select(methodInfo => NormalizedConverterMethod(methodInfo.Method, parameter.ParameterSymbol.Type))
                .ToImmutableArray();

            foreach (var normalizedFluentMethodSymbol in normalizedFluentMethodSymbols)
                yield return new MultiMethod(
                    parameter.ParameterSymbol,
                    methodReturn,
                    rootType.ContainingNamespace,
                    normalizedFluentMethodSymbol,
                    node.Key,
                    valueStorages,
                    normalizedFluentMethodSymbols);

            var hasMultipleFluentMethodsAttribute = parameter.ParameterSymbol
                .GetAttribute(TypeName.MultipleFluentMethodsAttribute) is not null;

            var hasFluentMethodAttribute = parameter.ParameterSymbol
                .GetAttribute(TypeName.FluentMethodAttribute) is not null;

            if (!hasFluentMethodAttribute && hasMultipleFluentMethodsAttribute) continue;

            var fluentParameter = fluentParameterInstances.First();
            foreach (var name in fluentParameter.Names)
                yield return new RegularMethod(
                    name,
                    fluentParameter.ParameterSymbol,
                    methodReturn,
                    rootType.ContainingNamespace,
                    node.Key,
                    valueStorages);
        }
    }

    private void ValidateMultipleFluentMethodCompatibility(FluentMethodParameter parameter,
        List<(IMethodSymbol Method, ICollection<Diagnostic> Diagnostics)> multipleFluentMethodInfo)
    {
        if (multipleFluentMethodInfo.Any()
            && multipleFluentMethodInfo.All(info => info.Diagnostics.Count > 0))
            diagnostics.AddRange(
            [
                Diagnostic.Create(
                    FluentDiagnostics.AllFluentMethodTemplatesIncompatible,
                    parameter.ParameterSymbol
                        .GetAttribute(TypeName.MultipleFluentMethodsAttribute)?
                        .GetLocationAtIndex(0),
                    parameter.ParameterSymbol.ToFullDisplayString()),
            ]);
        else
            diagnostics.AddRange(multipleFluentMethodInfo
                .SelectMany(info => info.Diagnostics));
    }

    private static ConstructorMetadata MergeConstructorMetadata(
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node, IList<ConstructorMetadata> constructorMetadataList)
    {
        return constructorMetadataList.Skip(1).Aggregate(constructorMetadataList.First().Clone(), (merged, metadata) =>
        {
            var mergeableConstructors = metadata.CandidateConstructors
                .Except<IMethodSymbol>(merged.CandidateConstructors, SymbolEqualityComparer.Default);

            merged.CandidateConstructors.AddRange(mergeableConstructors);
            merged.Options |= metadata.Options;
            if (metadata.Constructor.Parameters.Length - 1 != node.Key.Length)
                return merged;

            merged.Constructor = metadata.Constructor;

            return merged;
        });
    }

    private static IMethodSymbol NormalizedConverterMethod(IMethodSymbol converter, ITypeSymbol targetType)
    {
        var mapping = TypeMapper.MapGenericArguments(converter.ReturnType, targetType);

        return converter.NormalizeMethodTypeParameters(mapping);
    }
}
