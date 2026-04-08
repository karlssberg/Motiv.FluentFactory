using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Analyzes constructors to determine how parameters are stored as fields or properties.
/// Dispatches to storage detection strategies and handles initializer chain resolution.
/// </summary>
internal class ConstructorAnalyzer(SemanticModel semanticModel)
{
    private static readonly IStorageDetectionStrategy[] Strategies =
    [
        new RecordStorageStrategy(),
        new PrimaryConstructorStorageStrategy(),
        new ExplicitConstructorStorageStrategy()
    ];

    /// <summary>
    /// Finds the value storage (field, property, or primary constructor parameter) for each constructor parameter.
    /// </summary>
    /// <param name="constructor">The constructor to analyze.</param>
    /// <returns>An ordered dictionary mapping each parameter to its storage location.</returns>
    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> FindParameterValueStorage(IMethodSymbol constructor)
    {
        var results =
            new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(constructor.Parameters
                .Select(parameterSymbol =>
                    new KeyValuePair<IParameterSymbol, IFluentValueStorage>(
                        parameterSymbol,
                        new NullStorage(semanticModel.Compilation.GetSpecialType(SpecialType.System_Void)))),
                FluentParameterComparer.Default);

        var strategy = Strategies.FirstOrDefault(s => s.CanHandle(constructor, semanticModel));
        strategy?.PopulateStorage(constructor, results, semanticModel);

        ResolveInitializerChain(constructor, results);

        return results;
    }

    private void ResolveInitializerChain(
        IMethodSymbol constructor,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> results)
    {
        var syntaxNode = constructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (syntaxNode is not ConstructorDeclarationSyntax { Initializer: { } initializer }) return;

        var initializerMethod = (IMethodSymbol?)semanticModel.GetSymbolInfo(initializer).Symbol;
        if (initializerMethod is null) return;

        var baseResults = FindParameterValueStorage(initializerMethod);

        for (var i = 0; i < initializer.ArgumentList.Arguments.Count; i++)
        {
            var argument = initializer.ArgumentList.Arguments[i];
            if (semanticModel.GetSymbolInfo(argument.Expression).Symbol is not IParameterSymbol parameterSymbol ||
                !results.ContainsKey(parameterSymbol)) continue;

            if (i >= initializerMethod.Parameters.Length) continue;

            var baseParam = initializerMethod.Parameters[i];
            if (baseResults.TryGetValue(baseParam, out var baseProperty))
            {
                results[parameterSymbol] = baseProperty;
            }
        }
    }
}
