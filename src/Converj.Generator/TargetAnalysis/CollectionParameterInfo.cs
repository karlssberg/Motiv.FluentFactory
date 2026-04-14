using Microsoft.CodeAnalysis;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Per-parameter result of the collection method analyzer (wired in Plan 04).
/// Produced at Step 2 (target analysis) and carried forward on
/// <see cref="FluentTargetContext"/>/<see cref="TargetMetadata"/> so Phase 22's
/// code generation can emit the accumulator step struct without re-running analysis.
/// </summary>
internal sealed class CollectionParameterInfo(
    IParameterSymbol parameter,
    ITypeSymbol elementType,
    ITypeSymbol declaredCollectionType,
    string methodName,
    int minItems)
{
    /// <summary>Gets the constructor or target-method parameter symbol the attribute was applied to.</summary>
    public IParameterSymbol Parameter { get; } = parameter;

    /// <summary>Gets the collection's element type (e.g., <c>string</c> for <c>IList&lt;string&gt;</c>).</summary>
    public ITypeSymbol ElementType { get; } = elementType;

    /// <summary>Gets the parameter's declared type as written in user code, used at terminal-conversion time.</summary>
    public ITypeSymbol DeclaredCollectionType { get; } = declaredCollectionType;

    /// <summary>Gets the resolved accumulator method name — either the explicit attribute argument or <c>Add{Singularized}</c>.</summary>
    public string MethodName { get; } = methodName;

    /// <summary>Gets the minimum-item count parsed from the attribute's named argument; carried for Phase 24 enforcement.</summary>
    public int MinItems { get; } = minItems;
}
