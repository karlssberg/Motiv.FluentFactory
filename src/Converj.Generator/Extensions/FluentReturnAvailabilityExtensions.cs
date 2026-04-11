using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Extensions;

/// <summary>
/// Derives the available (reachable) target methods for an <see cref="IFluentReturn"/>
/// by subtracting <see cref="IFluentReturn.UnavailableTargets"/> from
/// <see cref="IFluentReturn.CandidateTargets"/>.
/// </summary>
internal static class FluentReturnAvailabilityExtensions
{
    /// <summary>
    /// Returns the target methods that are still reachable from this return node —
    /// i.e., candidates minus those marked unavailable during post-processing.
    /// </summary>
    public static IEnumerable<IMethodSymbol> GetAvailableTargets(this IFluentReturn node)
    {
        if (node.UnavailableTargets.IsDefaultOrEmpty)
            return node.CandidateTargets;

        return node.CandidateTargets.Except<IMethodSymbol>(node.UnavailableTargets, SymbolEqualityComparer.Default);
    }
}
