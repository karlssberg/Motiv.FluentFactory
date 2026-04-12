using VerifyCS =
    Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentRootGenerator>;

namespace Converj.Generator.Tests;

/// <summary>
/// Issue #19: A [FluentRoot] with no [FluentTarget] attributes anywhere.
/// The generator should gracefully produce nothing without crashing.
/// </summary>
public class EmptyFactoryTests
{
    [Fact]
    internal async Task Should_not_crash_when_factory_has_no_constructors()
    {
        const string code =
            """
            using System;
            using Converj.Generator;

            namespace Test;

            [FluentRoot]
            public static partial class Factory;
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code }
            }
        }.RunAsync();
    }
}
