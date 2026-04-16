using Microsoft.CodeAnalysis.Testing;
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Converj.Generator.Diagnostics.FluentDiagnostics;
using VerifyCS = Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentRootGenerator>;

namespace Converj.Generator.Tests;

/// <summary>
/// Tests for the broad overload rule introduced in Phase 23 Plan 04:
/// two [FluentCollectionMethod] targets on the same root that produce methods with the same name
/// but signature-distinct parameter lists emit as C# overloads — no CVJG0052 diagnostic.
/// CVJG0052 is narrowed to fire only when both name AND parameter-type sequence are identical.
/// </summary>
public class CollectionMethodOverloadingTests
{
    private const string SourceFile = "Source.cs";

    /// <summary>
    /// Scenario 1: Two [FluentCollectionMethod] parameters with same derived AddX name but
    /// signature-distinct element types emit as overloads on the accumulator step — no CVJG0052.
    /// "tags" (IList&lt;string&gt;) → AddTag(string) and a parameter with explicit "AddTag" name
    /// but IList&lt;int&gt; element type → AddTag(int). Different element types = different signatures.
    /// </summary>
    [Fact]
    internal async Task Same_AddX_name_different_element_types_emit_as_overloads_no_diagnostic()
    {
        const string code = """
            using System.Collections.Generic;
            using Converj.Attributes;

            namespace Test;

            [FluentRoot]
            public static partial class Builder { }

            public class Target
            {
                [FluentTarget(typeof(Builder))]
                public Target(
                    [FluentCollectionMethod] IList<string> tags,
                    [FluentCollectionMethod("AddTag")] IList<int> numericTags)
                { }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { (SourceFile, code) } }
        };

        // No CVJG0052: same name "AddTag" but different element types (string vs int) = signature-distinct overloads.
        // Skip generated source check — snapshot is captured separately; this test pins the no-diagnostic invariant.
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        await test.RunAsync();
    }

    /// <summary>
    /// Scenario 2: Same-signature same-name collision — CVJG0052 still fires.
    /// Sanity check: narrowing did not eliminate the diagnostic entirely.
    /// Both parameters produce "AddTag" with element type "string" — identical signatures → CVJG0052.
    /// </summary>
    [Fact]
    internal async Task Same_AddX_name_same_element_type_still_emits_CVJG0052()
    {
        const string code = """
            using System.Collections.Generic;
            using Converj.Attributes;

            namespace Test;

            [FluentRoot]
            public static partial class Builder { }

            public class Target
            {
                [FluentTarget(typeof(Builder))]
                public Target(
                    [FluentCollectionMethod] IList<string> tags,
                    [FluentCollectionMethod("AddTag")] IList<string> moreTags)
                { }
            }
            """;

        // "tags" → AddTag(string), "moreTags"(explicit "AddTag") → AddTag(string)
        // Same name AND same element type → identical signatures → CVJG0052 still fires.
        // Line 13: "        [FluentCollectionMethod] IList<string> tags,"
        // 8 + "[FluentCollectionMethod] " (25) + "IList<string> " (14) = 47 → "tags" col 48
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(AccumulatorMethodNameCollision.Id, Error)
                        .WithSpan(SourceFile, 13, 48, 13, 52)
                        .WithArguments("tags", "moreTags", "Test.Target.Target(System.Collections.Generic.IList<string>, System.Collections.Generic.IList<string>)", "AddTag")
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// Scenario 3: Verify the generated output for overloaded accumulator methods.
    /// Two [FluentCollectionMethod] parameters with same name "AddItem" but different element types
    /// (string vs int) both appear in the generated accumulator step — one per overload.
    /// </summary>
    [Fact]
    internal async Task Overloaded_accumulator_methods_both_appear_in_generated_output()
    {
        const string code = """
            using System.Collections.Generic;
            using Converj.Attributes;

            namespace Test;

            [FluentRoot]
            public static partial class Builder { }

            public class Target
            {
                [FluentTarget(typeof(Builder))]
                public Target(
                    [FluentCollectionMethod("AddItem")] IList<string> textItems,
                    [FluentCollectionMethod("AddItem")] IList<int> numericItems)
                { }
            }
            """;

        const string expected = """
            // <auto-generated/>
            #nullable enable
            namespace Test
            {
                [global::System.CodeDom.Compiler.GeneratedCode("Converj", "$$VERSION$$")]
                public static partial class Builder
                {
                    /// <summary>
                    ///     <seealso cref="Test.Target"/>
                    /// </summary>
                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    public static global::Test.Accumulator_0__Test_Builder BuildTarget()
                    {
                        return new global::Test.Accumulator_0__Test_Builder();
                    }
                }

                [global::System.CodeDom.Compiler.GeneratedCode("Converj", "$$VERSION$$")]
                public readonly struct Accumulator_0__Test_Builder
                {
                    private readonly global::System.Collections.Immutable.ImmutableArray<string> _textItems__parameter;
                    private readonly global::System.Collections.Immutable.ImmutableArray<int> _numericItems__parameter;
                    public Accumulator_0__Test_Builder()
                    {
                        this._textItems__parameter = global::System.Collections.Immutable.ImmutableArray<string>.Empty;
                        this._numericItems__parameter = global::System.Collections.Immutable.ImmutableArray<int>.Empty;
                    }

                    private Accumulator_0__Test_Builder(in global::System.Collections.Immutable.ImmutableArray<string> textItems, in global::System.Collections.Immutable.ImmutableArray<int> numericItems)
                    {
                        this._textItems__parameter = textItems;
                        this._numericItems__parameter = numericItems;
                    }

                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    public global::Test.Accumulator_0__Test_Builder AddItem(in string item)
                    {
                        return new global::Test.Accumulator_0__Test_Builder(this._textItems__parameter.Add(item), this._numericItems__parameter);
                    }

                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    public global::Test.Accumulator_0__Test_Builder AddItem(in int item)
                    {
                        return new global::Test.Accumulator_0__Test_Builder(this._textItems__parameter, this._numericItems__parameter.Add(item));
                    }

                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    public global::Test.Target CreateTarget()
                    {
                        return new global::Test.Target(this._textItems__parameter.ToArray(), this._numericItems__parameter.ToArray());
                    }
                }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                GeneratedSources =
                {
                    (typeof(FluentRootGenerator), "Test.Builder.g.cs", expected)
                }
            }
        };

        await test.RunAsync();
    }
}
