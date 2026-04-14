using System.Diagnostics.CodeAnalysis;

namespace Converj.Attributes;

/// <summary>
/// Marks a constructor parameter as a collection accumulator target, enabling the generator to
/// produce per-item accumulator methods (e.g., <c>AddTag</c>) in addition to the standard bulk
/// fluent method. The parameter type must be a supported collection type: <c>T[]</c>,
/// <c>IEnumerable&lt;T&gt;</c>, <c>ICollection&lt;T&gt;</c>, <c>IList&lt;T&gt;</c>,
/// <c>IReadOnlyCollection&lt;T&gt;</c>, or <c>IReadOnlyList&lt;T&gt;</c>.
/// </summary>
/// <remarks>
/// Only valid on constructor parameters (see <see cref="AttributeTargets.Parameter"/>).
/// The <see cref="MinItems"/> property is parsed and retained for future enforcement in Phase 24;
/// it is not enforced by the Phase 21 generator.
/// </remarks>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter)]
public class FluentCollectionMethodAttribute : Attribute
{
    /// <summary>
    /// Marks a collection parameter for accumulator method generation, deriving the method name
    /// from the parameter name via singularization (e.g., <c>tags</c> → <c>AddTag</c>).
    /// </summary>
    public FluentCollectionMethodAttribute()
    {
    }

    /// <summary>
    /// Marks a collection parameter for accumulator method generation with an explicit method name.
    /// </summary>
    /// <param name="methodName">
    /// The name of the accumulator method to generate (e.g., <c>"AddEntry"</c>).
    /// Use this overload when singularization would produce an incorrect or ambiguous name.
    /// </param>
    public FluentCollectionMethodAttribute(string methodName)
    {
        MethodName = methodName;
    }

    /// <summary>
    /// The explicit accumulator method name, or <see langword="null"/> if the name should be
    /// derived from the parameter name via singularization.
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    /// The minimum number of items that must be provided before the terminal method can be called.
    /// Defaults to <c>0</c> (no minimum enforced). Enforcement is deferred to Phase 24 — this
    /// property is defined here for API stability across v2.2.
    /// </summary>
    public int MinItems { get; set; } = 0;
}
