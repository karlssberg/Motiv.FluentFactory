using Microsoft.CodeAnalysis;

namespace Converj.Generator.Diagnostics;

internal class UnreachableConstructorAnalyzer
{
    private readonly List<IMethodSymbol> _allTargetConstructors = [];
    private readonly HashSet<IMethodSymbol> _reachedTargetConstructors =
        new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Directly marks a constructor as reachable, used when reconciliation
    /// updates a TargetTypeReturn's Constructor after initial processing.
    /// </summary>
    public void AddReachableConstructor(IMethodSymbol constructor)
    {
        _reachedTargetConstructors.Add(constructor);
    }

    /// <summary>
    /// Removes a constructor from the reachable set, used when reconciliation
    /// replaces an incorrectly-reached constructor with the correct one.
    /// </summary>
    public void RemoveReachableConstructor(IMethodSymbol constructor)
    {
        _reachedTargetConstructors.Remove(constructor);
    }

    public void AddReachableMethod(IFluentMethod method)
    {
        switch (method.Return)
        {
            case TargetTypeReturn targetTypeReturn:
                _reachedTargetConstructors.Add(targetTypeReturn.Constructor);
                break;
            case ExistingTypeFluentStep existingTypeFluentStep:
                _reachedTargetConstructors.Add(existingTypeFluentStep.ConstructorContext.Constructor);
                break;
        }
    }

    public void AddAllTargetConstructors(IEnumerable<IMethodSymbol> targetConstructors)
    {
        _allTargetConstructors.AddRange(targetConstructors);
    }

    /// <summary>
    /// Returns true if the given target method has been marked reachable during method selection.
    /// Only meaningful after all method selection is complete.
    /// </summary>
    public bool IsReachable(IMethodSymbol target)
        => _reachedTargetConstructors.Contains(target);

    public void Clear()
    {
        _reachedTargetConstructors.Clear();
        _allTargetConstructors.Clear();
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
        return _allTargetConstructors
            .Where(constructor => !_reachedTargetConstructors.Contains(constructor));
    }
}
