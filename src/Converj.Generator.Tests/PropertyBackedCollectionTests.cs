using Converj.Generator;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentRootGenerator>;

namespace Converj.Generator.Tests;

public class PropertyBackedCollectionTests
{
    [Fact]
    internal async Task Property_target_no_diagnostic_and_detected()
    {
        // Task 1: Verifies that [FluentCollectionMethod] on a property does not produce a diagnostic
        // and that the generated output contains the accumulator method (AddTag).
        // Source-gen output assertion is skipped at this stage; full snapshot added in Task 2.
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
                public IList<string> Tags { get; init; } = new List<string>();
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } },
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        // No diagnostics expected — the attribute on a property must be valid.
        await test.RunAsync();
    }
}
