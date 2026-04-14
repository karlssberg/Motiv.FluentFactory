using Microsoft.CodeAnalysis.Testing;
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Converj.Generator.Diagnostics.FluentDiagnostics;
using VerifyCS =
    Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentRootGenerator>;

namespace Converj.Generator.Tests;

/// <summary>
/// Tests for NAME-04: CVJG0052 accumulator method name collision detection.
/// Two or more [FluentCollectionMethod] parameters on the same target that produce the same
/// derived accumulator method name trigger CVJG0052 and the target is skipped from model-building.
/// </summary>
public class AccumulatorNameCollisionTests
{
    private const string SourceFile = "Source.cs";

    /// <summary>
    /// Test 1: Two parameters whose names both singularize to the same accumulator method name.
    /// "babies" (via -ies→-y rule) → "baby" → "AddBaby"
    /// "babys"  (via trailing-s rule) → "baby" → "AddBaby"
    /// Both implicitly derive "AddBaby" — collision triggers CVJG0052.
    /// </summary>
    [Fact]
    internal async Task Two_parameters_deriving_same_name_emit_CVJG0052()
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
                    [FluentCollectionMethod] IList<string> babies,
                    [FluentCollectionMethod] IList<string> babys)
                { }
            }
            """;

        // Line 13: "        [FluentCollectionMethod] IList<string> babies,"
        // 8 spaces + "[FluentCollectionMethod] " (25) + "IList<string> " (14) = 47 → "babies" at col 48
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(AccumulatorMethodNameCollision.Id, Error)
                        .WithSpan(SourceFile, 13, 48, 13, 54)
                        .WithArguments("babies", "babys", "Test.Target.Target(System.Collections.Generic.IList<string>, System.Collections.Generic.IList<string>)", "AddBaby")
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// Test 2: Explicit override colliding with a derived name triggers CVJG0052.
    /// "tags" (implicit) → singularizes → "AddTag"
    /// "entries" with explicit override "AddTag" → "AddTag"
    /// Both produce "AddTag" — collision triggers CVJG0052.
    /// </summary>
    [Fact]
    internal async Task Explicit_override_colliding_with_derived_name_emits_CVJG0052()
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
                    [FluentCollectionMethod("AddTag")] IList<string> entries)
                { }
            }
            """;

        // Line 13: "        [FluentCollectionMethod] IList<string> tags,"
        // 8 + 25 (attr+space) + 14 (type+space) = 47 → "tags" at col 48, length 4 → end 52
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(AccumulatorMethodNameCollision.Id, Error)
                        .WithSpan(SourceFile, 13, 48, 13, 52)
                        .WithArguments("tags", "entries", "Test.Target.Target(System.Collections.Generic.IList<string>, System.Collections.Generic.IList<string>)", "AddTag")
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// Test 3: Two explicit overrides colliding on the same name trigger CVJG0052.
    /// Both parameters explicitly set to "AddItem" — clear collision.
    /// </summary>
    [Fact]
    internal async Task Two_explicit_overrides_colliding_emit_CVJG0052()
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
                    [FluentCollectionMethod("AddItem")] IList<int> first,
                    [FluentCollectionMethod("AddItem")] IList<int> second)
                { }
            }
            """;

        // Line 13: "        [FluentCollectionMethod("AddItem")] IList<int> first,"
        // 8 + "[FluentCollectionMethod(\"AddItem\")] " (36) + "IList<int> " (11) = 55 → "first" at col 56
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(AccumulatorMethodNameCollision.Id, Error)
                        .WithSpan(SourceFile, 13, 56, 13, 61)
                        .WithArguments("first", "second", "Test.Target.Target(System.Collections.Generic.IList<int>, System.Collections.Generic.IList<int>)", "AddItem")
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// Test 4: A collision on one target does not affect a sibling target on the same root.
    /// TargetA has a collision (both params produce "AddTag") — CVJG0052 emitted, TargetA skipped.
    /// TargetB is collision-free — its output is still generated normally.
    /// Mirrors the skip-target-on-error pattern of CVJG0011 (ParameterModifierTests).
    /// </summary>
    [Fact]
    internal async Task Collision_on_one_target_does_not_affect_sibling_target()
    {
        const string code = """
            using System.Collections.Generic;
            using Converj.Attributes;

            namespace Test;

            [FluentRoot]
            public static partial class Builder { }

            public class TargetA
            {
                [FluentTarget(typeof(Builder))]
                public TargetA(
                    [FluentCollectionMethod] IList<string> tags,
                    [FluentCollectionMethod("AddTag")] IList<string> entries)
                { }
            }

            public class TargetB
            {
                [FluentTarget(typeof(Builder))]
                public TargetB(int value) { }
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
                    ///     <seealso cref="Test.TargetB"/>
                    /// </summary>
                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    public static global::Test.Step_0__Test_Builder WithValue(in int value)
                    {
                        return new global::Test.Step_0__Test_Builder(value);
                    }
                }

                /// <summary>
                ///     <seealso cref="Test.TargetB"/>
                /// </summary>
                [global::System.CodeDom.Compiler.GeneratedCode("Converj", "$$VERSION$$")]
                public readonly struct Step_0__Test_Builder
                {
                    private readonly int _value__parameter;
                    internal Step_0__Test_Builder(in int value)
                    {
                        this._value__parameter = value;
                    }

                    /// <summary>
                    /// Creates a new instance using constructor Test.TargetB.TargetB(int value).
                    ///
                    ///     <seealso cref="Test.TargetB"/>
                    /// </summary>
                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    public global::Test.TargetB CreateTargetB()
                    {
                        return new global::Test.TargetB(this._value__parameter);
                    }
                }
            }
            """;

        // Line 13: "        [FluentCollectionMethod] IList<string> tags,"
        // 8 + 25 + 14 = 47 → "tags" at col 48, length 4 → end 52
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(AccumulatorMethodNameCollision.Id, Error)
                        .WithSpan(SourceFile, 13, 48, 13, 52)
                        .WithArguments("tags", "entries", "Test.TargetA.TargetA(System.Collections.Generic.IList<string>, System.Collections.Generic.IList<string>)", "AddTag")
                },
                GeneratedSources =
                {
                    (typeof(FluentRootGenerator), "Test.Builder.g.cs", expected)
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// Test 5: No collision when two parameters have distinct (non-colliding) accumulator names.
    /// "tags" explicit → "AddTag", "entries" explicit → "AddEntry" — no collision, no CVJG0052.
    /// </summary>
    [Fact]
    internal async Task No_collision_when_override_breaks_it()
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
                    [FluentCollectionMethod("AddTag")] IList<string> tags,
                    [FluentCollectionMethod("AddEntry")] IList<string> entries)
                { }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        // No diagnostics expected — distinct names mean no collision.
        // Skip generated source check — accumulator code generation is Phase 22 work.
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        await test.RunAsync();
    }

    /// <summary>
    /// Test 6: Two constructors (on different target types) each with a [FluentCollectionMethod] parameter
    /// of the same derived name are NOT a Phase 21 collision — collision scope is per-target only.
    /// Each constructor is its own target; cross-target same-name is not a collision.
    /// </summary>
    [Fact]
    internal async Task No_collision_when_same_name_on_different_targets()
    {
        const string code = """
            using System.Collections.Generic;
            using Converj.Attributes;

            namespace Test;

            [FluentRoot]
            public static partial class Builder { }

            public class TargetA
            {
                [FluentTarget(typeof(Builder))]
                public TargetA([FluentCollectionMethod] IList<string> tags) { }
            }

            public class TargetB
            {
                [FluentTarget(typeof(Builder))]
                public TargetB([FluentCollectionMethod] IList<string> tags) { }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        // No CVJG0052 — same accumulator name on different targets is not a Phase 21 collision.
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        await test.RunAsync();
    }
}
