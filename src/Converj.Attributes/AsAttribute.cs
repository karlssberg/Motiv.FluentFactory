using System.Diagnostics.CodeAnalysis;

namespace Converj.Attributes;

/// <summary>
/// Aliases a generic type parameter name for matching purposes in fluent factory generation.
/// When multiple constructors use different type parameter names for the same conceptual type parameter,
/// apply this attribute to unify them under a common name.
/// </summary>
/// <param name="name">The canonical name to use for matching this type parameter.</param>
/// <example>
/// <code>
/// // Line2D's TNum will be treated as "T" for matching with Line1D and Line3D
/// [FluentTarget&lt;Line&gt;]
/// record Line2D&lt;[As("T")] TNum&gt;(TNum X, TNum Y) where TNum : INumber&lt;TNum&gt;;
/// </code>
/// </example>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.GenericParameter)]
public sealed class AsAttribute(string name) : Attribute
{
    /// <summary>
    /// The canonical name to use for this type parameter during matching.
    /// </summary>
    public string Name { get; } = name;
}
