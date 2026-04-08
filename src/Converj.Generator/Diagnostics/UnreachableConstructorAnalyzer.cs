using Microsoft.CodeAnalysis;

namespace Converj.Generator.Diagnostics;

internal class UnreachableConstructorAnalyzer
{
    private readonly List<IMethodSymbol> _allFluentConstructors = [];
    private readonly HashSet<IMethodSymbol> _reachedFluentConstructors =
        new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Directly marks a constructor as reachable, used when reconciliation
    /// updates a TargetTypeReturn's Constructor after initial processing.
    /// </summary>
    public void AddReachableConstructor(IMethodSymbol constructor)
    {
        _reachedFluentConstructors.Add(constructor);
    }

    /// <summary>
    /// Removes a constructor from the reachable set, used when reconciliation
    /// replaces an incorrectly-reached constructor with the correct one.
    /// </summary>
    public void RemoveReachableConstructor(IMethodSymbol constructor)
    {
        _reachedFluentConstructors.Remove(constructor);
    }

    public void AddReachableMethod(IFluentMethod method)
    {
        switch (method.Return)
        {
            case TargetTypeReturn targetTypeReturn:
                _reachedFluentConstructors.Add(targetTypeReturn.Constructor);
                break;
            case ExistingTypeFluentStep existingTypeFluentStep:
                _reachedFluentConstructors.Add(existingTypeFluentStep.ConstructorContext.Constructor);
                break;
        }
    }

    public void AddAllFluentConstructors(IEnumerable<IMethodSymbol> fluentConstructors)
    {
        _allFluentConstructors.AddRange(fluentConstructors);
    }

    public void Clear()
    {
        _reachedFluentConstructors.Clear();
        _allFluentConstructors.Clear();
    }

    public IEnumerable<Diagnostic> GetUnreachableConstructorsDiagnostics()
    {
        return GetUnreachableConstructors()
            .Select(constructor =>
                Diagnostic.Create(
                    FluentDiagnostics.UnreachableConstructor,
                    constructor.Locations.FirstOrDefault(),
                    constructor.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private IEnumerable<IMethodSymbol> GetUnreachableConstructors()
    {
        return _allFluentConstructors
            .Where(constructor => !_reachedFluentConstructors.Contains(constructor));
    }
}
