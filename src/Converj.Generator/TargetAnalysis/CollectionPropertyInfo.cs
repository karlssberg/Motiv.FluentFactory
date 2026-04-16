using Microsoft.CodeAnalysis;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Per-property result of the collection method analyzer — the property-side counterpart to
/// <see cref="CollectionParameterInfo"/>. Produced at Step 2 (target analysis) and carried
/// forward on <see cref="FluentTargetContext.CollectionProperties"/> so code generation can
/// emit the accumulator step struct without re-running analysis.
/// </summary>
/// <remarks>
/// Mirrors <see cref="CollectionParameterInfo"/> exactly, replacing
/// <see cref="CollectionParameterInfo.Parameter"/> (<see cref="IParameterSymbol"/>) with
/// <see cref="Property"/> (<see cref="IPropertySymbol"/>). All other members have identical
/// semantics and are consumed by the same code-generation paths.
/// </remarks>
internal sealed class CollectionPropertyInfo(
    IPropertySymbol property,
    ITypeSymbol elementType,
    ITypeSymbol declaredCollectionType,
    string methodName,
    int minItems)
{
    /// <summary>Gets the target-type property symbol the attribute was applied to.</summary>
    public IPropertySymbol Property { get; } = property;

    /// <summary>Gets the collection's element type (e.g., <c>string</c> for <c>IList&lt;string&gt;</c>).</summary>
    public ITypeSymbol ElementType { get; } = elementType;

    /// <summary>Gets the property's declared type as written in user code, used at terminal-conversion time.</summary>
    public ITypeSymbol DeclaredCollectionType { get; } = declaredCollectionType;

    /// <summary>Gets the resolved accumulator method name — either the explicit attribute argument or <c>Add{Singularized}</c>.</summary>
    public string MethodName { get; } = methodName;

    /// <summary>Gets the minimum-item count parsed from the attribute's named argument; carried for Phase 24 enforcement.</summary>
    public int MinItems { get; } = minItems;
}
