using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

/// <summary>
/// Produces the <see cref="ExpressionSyntax"/> the terminal method on an accumulator step uses to
/// convert an <c>ImmutableArray&lt;T&gt;</c> field to the declared collection parameter type.
/// </summary>
/// <remarks>
/// Conversion table (RESEARCH.md Pattern 5 — six allowlisted types):
/// <list type="table">
/// <listheader><term>Declared type</term><description>Generated expression</description></listheader>
/// <item><term><c>T[]</c></term><description><c>fieldAccess.ToArray()</c></description></item>
/// <item><term><c>IEnumerable&lt;T&gt;</c></term><description><c>fieldAccess</c> (identity)</description></item>
/// <item><term><c>ICollection&lt;T&gt;</c></term><description><c>fieldAccess.ToArray()</c></description></item>
/// <item><term><c>IList&lt;T&gt;</c></term><description><c>fieldAccess.ToArray()</c></description></item>
/// <item><term><c>IReadOnlyCollection&lt;T&gt;</c></term><description><c>fieldAccess</c> (identity)</description></item>
/// <item><term><c>IReadOnlyList&lt;T&gt;</c></term><description><c>fieldAccess</c> (identity)</description></item>
/// </list>
/// <para>
/// The allowlist is enforced upstream by <c>FluentCollectionMethodAnalyzer</c> (CVJG0050).
/// Passing a type outside the allowlist is a programming error that surfaces as
/// <see cref="NotSupportedException"/>.
/// </para>
/// <para>
/// <b>No C# 12 collection expressions</b> (<c>[..]</c>) are emitted — generated code must compile
/// under the user's chosen language version (RESEARCH.md Pitfall 6).
/// </para>
/// </remarks>
internal static class AccumulatorCollectionConversionExpression
{
    /// <summary>
    /// Returns the <see cref="ExpressionSyntax"/> the terminal method uses to pass the accumulated
    /// <c>ImmutableArray&lt;T&gt;</c> field as the declared collection type.
    /// </summary>
    /// <param name="fieldAccess">
    /// The field access expression, e.g. <c>IdentifierName("this._items__parameter")</c> or a
    /// <c>MemberAccessExpression</c> on <c>this</c>.
    /// </param>
    /// <param name="declaredCollection">
    /// The parameter's declared type as written in user code (e.g. <c>IList&lt;string&gt;</c>,
    /// <c>string[]</c>, <c>IEnumerable&lt;int&gt;</c>).
    /// Must be one of the six allowlisted collection types gated by CVJG0050.
    /// </param>
    /// <param name="_elementType">
    /// The collection's element type (e.g. <c>string</c>, <c>int</c>).
    /// Currently unused; reserved for Phase 23 bulk-set composability extensions.
    /// </param>
    /// <returns>
    /// An <see cref="ExpressionSyntax"/> representing the correctly converted value.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="declaredCollection"/> is not one of the six allowlisted types.
    /// This should be unreachable in production because CVJG0050 gates the allowlist upstream.
    /// </exception>
    public static ExpressionSyntax ConvertToDeclaredType(
        ExpressionSyntax fieldAccess,
        ITypeSymbol declaredCollection,
        ITypeSymbol _elementType)
    {
        // T[] → fieldAccess.ToArray()
        if (declaredCollection is IArrayTypeSymbol { Rank: 1 })
            return InvokeToArray(fieldAccess);

        var specialType = declaredCollection.OriginalDefinition.SpecialType;
        return specialType switch
        {
            SpecialType.System_Collections_Generic_IEnumerable_T          => fieldAccess,
            SpecialType.System_Collections_Generic_IReadOnlyCollection_T  => fieldAccess,
            SpecialType.System_Collections_Generic_IReadOnlyList_T        => fieldAccess,
            SpecialType.System_Collections_Generic_ICollection_T          => InvokeToArray(fieldAccess),
            SpecialType.System_Collections_Generic_IList_T                => InvokeToArray(fieldAccess),
            _ => throw new NotSupportedException(
                $"Unsupported declared collection type for accumulator conversion: {declaredCollection.ToDisplayString()}")
        };
    }

    private static InvocationExpressionSyntax InvokeToArray(ExpressionSyntax fieldAccess) =>
        InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                fieldAccess,
                IdentifierName("ToArray")));
}
