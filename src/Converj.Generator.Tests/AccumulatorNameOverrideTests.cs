using Microsoft.CodeAnalysis.Testing;
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Converj.Generator.Diagnostics.FluentDiagnostics;
using VerifyCS =
    Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentRootGenerator>;

namespace Converj.Generator.Tests;

/// <summary>
/// Tests for NAME-02 (explicit accumulator name override) and NAME-03 (singularization failures)
/// for the [FluentCollectionMethod] attribute.
/// </summary>
public class AccumulatorNameOverrideTests
{
    private const string AttributeUsings = """
        using System;
        using System.Collections.Generic;
        using Converj.Attributes;
        """;

    private static string BuildSource(string paramType, string paramName, string attrArgs = "")
    {
        var attrSuffix = attrArgs.Length > 0 ? $"({attrArgs})" : string.Empty;
        return $$"""
        {{AttributeUsings}}

        namespace Test;

        [FluentRoot]
        public partial class Builder;

        public class Target
        {
            [FluentTarget(typeof(Builder))]
            public Target([FluentCollectionMethod{{attrSuffix}}] {{paramType}} {{paramName}}) { }
        }
        """;
    }

    // NAME-02: Explicit override "AddEntry" on a valid collection — no diagnostic
    [Fact]
    internal async Task ExplicitName_OnValidCollection_ProducesNoDiagnostic()
    {
        var code = BuildSource("IList<string>", "items", "\"AddEntry\"");

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        // Skip generated sources check — we only verify no CVJG0050/0051 is emitted.
        // The accumulator method name is verified by Plan 05's snapshot test.
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        // No diagnostics expected — explicit name bypasses singularization completely
        await test.RunAsync();
    }

    // NAME-02: Explicit override on an unsingularizable parameter name — still no diagnostic
    // "data" has no rule in Singularize() (returns null), but explicit override bypasses derivation
    [Fact]
    internal async Task ExplicitName_OnUnsingularizableParam_ProducesNoDiagnostic()
    {
        var code = BuildSource("IList<byte>", "data", "\"AddDatum\"");

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        // Skip generated sources check — we only verify no CVJG0051 is emitted.
        // Explicit name bypasses NAME-03 fallback.
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        await test.RunAsync();
    }

    // NAME-03: Without override, "data: IList<byte>" → CVJG0051 (no rule fires for "data")
    // BuildSource puts [FluentCollectionMethod] at line 13, col 20-42.
    [Fact]
    internal async Task NoOverride_UnsingularizableParam_ProducesCVJG0051()
    {
        var code = BuildSource("IList<byte>", "data");

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        test.TestState.ExpectedDiagnostics.Add(
            new DiagnosticResult(UnsingularizableParameterName.Id, Error)
                .WithSpan("/0/Test1.cs", 13, 20, 13, 42)
                .WithArguments("data"));

        await test.RunAsync();
    }

    // NAME-03: Without override, "events: IList<Event>" → CVJG0051
    // "events".Singularize() → "event", but "event" is a C# keyword (SyntaxFacts.GetKeywordKind
    // returns SyntaxKind.EventKeyword), so TryDeriveAccumulatorName returns null → CVJG0051.
    // The [FluentCollectionMethod] attribute is at line 15, col 20-42 in this source.
    [Fact]
    internal async Task NoOverride_KeywordSingularization_ProducesCVJG0051()
    {
        const string code = """
            using System;
            using System.Collections.Generic;
            using Converj.Attributes;

            namespace Test;

            [FluentRoot]
            public partial class Builder;

            public class Event { }

            public class Target
            {
                [FluentTarget(typeof(Builder))]
                public Target([FluentCollectionMethod] IList<Event> events) { }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        test.TestState.ExpectedDiagnostics.Add(
            new DiagnosticResult(UnsingularizableParameterName.Id, Error)
                .WithSpan("/0/Test1.cs", 15, 20, 15, 42)
                .WithArguments("events"));

        await test.RunAsync();
    }
}
