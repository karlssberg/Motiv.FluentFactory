using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using static Converj.Generator.Diagnostics.FluentDiagnostics;
using VerifyCS = Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentRootGenerator>;

namespace Converj.Generator.Tests;

/// <summary>
/// Tests for CVJG0053 — [FluentCollectionMethod] on a property with an unsupported accessor shape.
/// Primary case: record primary-constructor positional property (via [property:] attribute forwarding).
/// Secondary case: property with no set or init accessor (read-only computed property).
/// </summary>
public class CollectionMethodPropertyAccessorDiagnosticTests
{
    // ── (a) Record positional property (via [property: ...] forwarding) emits CVJG0053 ────

    [Fact]
    internal async Task Record_positional_property_emits_CVJG0053()
    {
        // When [FluentCollectionMethod] is applied to a record primary-ctor positional property
        // via [property:] attribute forwarding, CVJG0053 must be emitted.
        // The auto-generated positional property cannot be re-assigned via object initializer.
        const string code =
            """
            using System.Collections.Generic;
            using Converj.Attributes;

            namespace Test;

            [FluentRoot]
            public static partial class Builder;

            public record Target(
                [property: FluentCollectionMethod] IList<string> Tags)
            {
                [FluentTarget(typeof(Builder))]
                public Target() : this(new List<string>()) { }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(UnsupportedCollectionPropertyAccessor.Id, DiagnosticSeverity.Error)
                        .WithSpan("/0/Test1.cs", 10, 16, 10, 38)
                        .WithArguments("Tags", "Target", "record primary-constructor positional properties cannot be re-assigned via object initializer")
                }
            },
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        }.RunAsync();
    }

    // ── (b) Non-positional record property does NOT emit CVJG0053 ────────────

    [Fact]
    internal async Task Non_positional_record_property_does_not_emit_CVJG0053()
    {
        // A record with a separately-declared init property (NOT positional) is supported.
        const string code =
            """
            using System.Collections.Generic;
            using Converj.Attributes;

            namespace Test;

            [FluentRoot]
            public static partial class Builder;

            public record Target
            {
                [FluentTarget(typeof(Builder))]
                public Target() { }

                [FluentCollectionMethod]
                public IList<string> Tags { get; init; }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } },
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        // No CVJG0053 expected.
        await test.RunAsync();
    }

    // ── (c) Class with init property does NOT emit CVJG0053 ──────────────────

    [Fact]
    internal async Task Class_with_init_property_does_not_emit_CVJG0053()
    {
        // A plain class with IList<string> Tags { get; init; } is fully supported.
        const string code =
            """
            using System.Collections.Generic;
            using Converj.Attributes;

            namespace Test;

            [FluentRoot]
            public static partial class Builder;

            public class Target
            {
                [FluentTarget(typeof(Builder))]
                public Target() { }

                [FluentCollectionMethod]
                public IList<string> Tags { get; init; }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } },
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        // No CVJG0053 expected.
        await test.RunAsync();
    }

    // ── (d) CVJG0053 on property does not block sibling constructor targets ────

    [Fact]
    internal async Task CVJG0053_on_positional_property_does_not_block_sibling_targets()
    {
        // When CVJG0053 fires on a positional property, generation continues for the constructor target —
        // the error is stored in _skippedTargetDiagnostics so sibling (non-property) parts still generate.
        // The diagnostic fires; no crash/abort of the root.
        const string code =
            """
            using System.Collections.Generic;
            using Converj.Attributes;

            namespace Test;

            [FluentRoot]
            public static partial class Builder;

            public record Target(
                [property: FluentCollectionMethod] IList<string> Tags)
            {
                [FluentTarget(typeof(Builder))]
                public Target(string name) : this(new List<string>()) { }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(UnsupportedCollectionPropertyAccessor.Id, DiagnosticSeverity.Error)
                        .WithSpan("/0/Test1.cs", 10, 16, 10, 38)
                        .WithArguments("Tags", "Target", "record primary-constructor positional properties cannot be re-assigned via object initializer")
                }
            },
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        }.RunAsync();
    }
}
