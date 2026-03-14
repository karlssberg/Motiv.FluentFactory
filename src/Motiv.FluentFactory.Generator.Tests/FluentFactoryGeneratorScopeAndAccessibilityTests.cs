using Microsoft.CodeAnalysis.Testing;
using static Motiv.FluentFactory.Generator.Diagnostics.FluentDiagnostics;
using VerifyCS =
    Motiv.FluentFactory.Generator.Tests.CSharpSourceGeneratorVerifier<Motiv.FluentFactory.Generator.FluentFactoryGenerator>;

namespace Motiv.FluentFactory.Generator.Tests;

/// <summary>
/// Tests for constructor scope and accessibility edge cases.
/// Private or protected constructors with [FluentConstructor] cannot be called from generated code.
/// Factory root types missing the partial modifier cannot receive generated methods.
/// </summary>
public class FluentFactoryGeneratorScopeAndAccessibilityTests
{
    private const string SourceFile = "Source.cs";

    /// <summary>
    /// A private constructor with [FluentConstructor] cannot be used in a fluent factory
    /// because the generated code cannot call it. The generator should emit MFFG0012
    /// warning and skip generation for that constructor.
    /// </summary>
    [Fact]
    internal async Task Should_emit_diagnostic_when_FluentConstructor_applied_to_private_constructor()
    {
        const string code =
            """
            using Motiv.FluentFactory.Generator;

            namespace Test;

            [FluentFactory]
            public static partial class Factory;

            public class MyTarget
            {
                [FluentConstructor(typeof(Factory))]
                private MyTarget(int value)
                {
                    Value = value;
                }

                public int Value { get; set; }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerWarning(InaccessibleConstructor.Id)
                        .WithSpan(SourceFile, 11, 13, 11, 21)
                        .WithArguments("Test.MyTarget.MyTarget(int)", "Private")
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// A protected constructor with [FluentConstructor] cannot be used in a fluent factory
    /// because the generated code cannot call it. The generator should emit MFFG0012
    /// warning and skip generation for that constructor.
    /// </summary>
    [Fact]
    internal async Task Should_emit_diagnostic_when_FluentConstructor_applied_to_protected_constructor()
    {
        const string code =
            """
            using Motiv.FluentFactory.Generator;

            namespace Test;

            [FluentFactory]
            public static partial class Factory;

            public class MyTarget
            {
                [FluentConstructor(typeof(Factory))]
                protected MyTarget(int value)
                {
                    Value = value;
                }

                public int Value { get; set; }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerWarning(InaccessibleConstructor.Id)
                        .WithSpan(SourceFile, 11, 15, 11, 23)
                        .WithArguments("Test.MyTarget.MyTarget(int)", "Protected")
                }
            }
        }.RunAsync();
    }
}
