using Microsoft.CodeAnalysis;

namespace Converj.Generator.Diagnostics;

internal class UnreachableTargetAnalyzer
{
    private readonly List<IMethodSymbol> _allTargets = [];
    private readonly HashSet<IMethodSymbol> _reachedTargets =
        new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Directly marks a target as reachable, used when reconciliation
    /// updates a TargetTypeReturn's Method after initial processing.
    /// </summary>
    public void AddReachableTarget(IMethodSymbol target)
    {
        _reachedTargets.Add(target);
    }

    /// <summary>
    /// Removes a target from the reachable set, used when reconciliation
    /// replaces an incorrectly-reached target with the correct one.
    /// </summary>
    public void RemoveReachableTarget(IMethodSymbol target)
    {
        _reachedTargets.Remove(target);
    }

    public void AddReachableMethod(IFluentMethod method)
    {
        switch (method.Return)
        {
            case TargetTypeReturn targetTypeReturn:
                _reachedTargets.Add(targetTypeReturn.Method);
                break;
            case ExistingTypeFluentStep existingTypeFluentStep:
                _reachedTargets.Add(existingTypeFluentStep.TargetContext.Method);
                break;
        }
    }

    public void AddAllTargets(IEnumerable<IMethodSymbol> targets)
    {
        _allTargets.AddRange(targets);
    }

    /// <summary>
    /// Returns true if the given target method has been marked reachable during method selection.
    /// Only meaningful after all method selection is complete.
    /// </summary>
    public bool IsReachable(IMethodSymbol target)
        => _reachedTargets.Contains(target);

    public void Clear()
    {
        _reachedTargets.Clear();
        _allTargets.Clear();
    }

    public IEnumerable<Diagnostic> GetUnreachableTargetsDiagnostics()
    {
        return GetUnreachableTargets()
            .Select(target =>
                Diagnostic.Create(
                    FluentDiagnostics.UnreachableTarget,
                    target.Locations.FirstOrDefault(),
                    target.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private IEnumerable<IMethodSymbol> GetUnreachableTargets()
    {
        return _allTargets
            .Where(target => !_reachedTargets.Contains(target));
    }
}
