using Converj.Generator.Extensions;
using Converj.Generator.Models.Methods;
using Converj.Generator.Models.Steps;
using Converj.Generator.SyntaxGeneration.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

/// <summary>
/// Emits the <c>WithXs(IEnumerable&lt;T&gt; items)</c> method on the last regular trie step
/// that provides a parameterised bulk-append entry into the <see cref="AccumulatorFluentStep"/>.
/// The generated method calls the accumulator step's copy constructor, seeding the target
/// collection field via <c>ImmutableArray&lt;T&gt;.Empty.AddRange(items)</c> and leaving all
/// other collection fields as <c>.Empty</c> (COMP-01, Phase 23 Plan 02).
/// </summary>
internal static class AccumulatorBulkTransitionMethodDeclaration
{
    private const string IEnumerableGlobal =
        "global::System.Collections.Generic.IEnumerable";

    /// <summary>
    /// Creates the method declaration for a bulk-transition method, either on a regular step
    /// or at the root level (when <paramref name="currentStep"/> is <see langword="null"/>).
    /// The generated body creates an accumulator step via the entry constructor and chains
    /// the accumulator step's own <c>WithXs</c> method: <c>new AccumulatorStep(forwarded...).WithXs(items)</c>.
    /// This avoids calling the private copy constructor from an external context.
    /// </summary>
    /// <param name="method">The bulk transition method model.</param>
    /// <param name="currentStep">
    /// The regular step this method lives on, or <see langword="null"/> when emitting
    /// from the root factory class (no preceding regular step).
    /// </param>
    /// <returns>A <see cref="MethodDeclarationSyntax"/> for the bulk-transition method.</returns>
    public static MethodDeclarationSyntax Create(
        AccumulatorBulkTransitionMethod method,
        RegularFluentStep? currentStep)
    {
        var accumulatorStep = (AccumulatorFluentStep)method.Return;
        var stepGlobalName = accumulatorStep.IdentifierDisplayString();

        var cp = method.CollectionParameter;
        var elementTypeName = cp.ElementType.ToGlobalDisplayString();

        // Build the entry constructor arguments (forwarded non-collection fields only).
        var entryCtorArgs = BuildEntryConstructorArguments(accumulatorStep);

        // Emit: return new AccumulatorStep(forwarded...).WithXs(items);
        // This chains the public entry ctor with the public WithXs method to avoid
        // calling the private copy constructor from an external scope.
        var entryCtorCall = ObjectCreationExpression(ParseTypeName(stepGlobalName))
            .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(
                entryCtorArgs.InterleaveWith(Token(SyntaxKind.CommaToken)))));

        var chainedCall = InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                entryCtorCall,
                IdentifierName(method.Name)))
            .WithArgumentList(ArgumentList(
                SingletonSeparatedList(Argument(IdentifierName("items")))));

        return MethodDeclaration(ParseTypeName(stepGlobalName), Identifier(method.Name))
            .WithAttributeLists(SingletonList(
                AttributeList(SingletonSeparatedList(AggressiveInliningAttributeSyntax.Create()))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(SingletonSeparatedList(
                Parameter(Identifier("items"))
                    .WithType(ParseTypeName($"{IEnumerableGlobal}<{elementTypeName}>")))))
            .WithBody(Block(
                ReturnStatement(chainedCall)));
    }

    /// <summary>
    /// Builds the argument list for the accumulator step's public entry constructor.
    /// These are the forwarded non-collection fields, passed as <c>this._field</c>.
    /// The entry constructor initialises all collection fields to <c>.Empty</c> automatically.
    /// </summary>
    private static IEnumerable<ArgumentSyntax> BuildEntryConstructorArguments(
        AccumulatorFluentStep accumulatorStep)
    {
        // Forwarded non-collection fields from the preceding step (or empty if root-level)
        foreach (var p in accumulatorStep.ForwardedTargetParameters)
        {
            yield return Argument(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(p.Name.ToParameterFieldName())));
        }
    }
}
