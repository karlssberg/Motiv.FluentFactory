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

    /// <summary>
    /// A public factory with a constructor whose parameter type is internal is an accessibility mismatch.
    /// The generated fluent method would expose the internal type, which is inaccessible to consumers.
    /// The generator should emit MFFG0014 warning.
    /// </summary>
    [Fact]
    internal async Task Should_emit_diagnostic_when_parameter_type_less_accessible_than_factory()
    {
        const string code =
            """
            using Motiv.FluentFactory.Generator;

            namespace Test;

            [FluentFactory]
            public static partial class Factory;

            internal class InternalParam
            {
                public int Value { get; set; }
            }

            public class MyTarget
            {
                [FluentConstructor(typeof(Factory))]
                public MyTarget(InternalParam param)
                {
                    Param = param;
                }

                public InternalParam Param { get; set; }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerWarning(InaccessibleParameterType.Id)
                        .WithSpan(SourceFile, 1, 1, 1, 1)
                        .WithArguments("param", "Test.InternalParam", "Test.MyTarget.MyTarget(Test.InternalParam)", "Test.Factory")
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// A public factory wrapping an internal target type is an accessibility mismatch.
    /// The generated factory would expose creation of an inaccessible type to public consumers.
    /// The generator should emit MFFG0015 warning.
    /// </summary>
    [Fact]
    internal async Task Should_emit_diagnostic_when_public_factory_wraps_internal_target_type()
    {
        const string code =
            """
            using Motiv.FluentFactory.Generator;

            namespace Test;

            [FluentFactory]
            public static partial class Factory;

            internal class MyTarget
            {
                [FluentConstructor(typeof(Factory))]
                public MyTarget(int value)
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
                    DiagnosticResult.CompilerWarning(AccessibilityMismatch.Id)
                        .WithSpan(SourceFile, 1, 1, 1, 1)
                        .WithArguments("Test.Factory", "Public", "Test.MyTarget", "Internal")
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// An internal factory wrapping an internal target type is NOT an accessibility mismatch.
    /// Both are internal so no diagnostic should be emitted.
    /// </summary>
    [Fact]
    internal async Task Should_not_emit_accessibility_mismatch_when_both_internal()
    {
        const string code =
            """
            using Motiv.FluentFactory.Generator;

            namespace Test;

            [FluentFactory]
            internal static partial class Factory;

            internal class MyTarget
            {
                [FluentConstructor(typeof(Factory))]
                public MyTarget(int value)
                {
                    Value = value;
                }

                public int Value { get; set; }
            }
            """;

        const string expected =
            """
            // <auto-generated/>
            #pragma warning disable
            #nullable enable
            namespace Test
            {
                partial class Factory
                {
                    /// <summary>
                    /// Creates a new instance of <see cref="global::Test.MyTarget" />.
                    /// </summary>
                    /// <param name="value">The value parameter.</param>
                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    internal static global::Test.MyTarget Create(int value) =>
                        new global::Test.MyTarget(value);
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                GeneratedSources = { (typeof(FluentFactoryGenerator), "Test.Factory.g.cs", expected) }
            }
        }.RunAsync();
    }

    /// <summary>
    /// A factory root type without the <c>partial</c> modifier cannot receive generated fluent methods.
    /// The generator should emit MFFG0013 error and produce no generated source output.
    /// </summary>
    [Fact]
    internal async Task Should_emit_diagnostic_when_factory_root_type_missing_partial_modifier()
    {
        const string code =
            """
            using Motiv.FluentFactory.Generator;

            namespace Test;

            [FluentFactory]
            public static class Factory;

            public class MyTarget
            {
                [FluentConstructor(typeof(Factory))]
                public MyTarget(int value)
                {
                    Value = value;
                }

                public int Value { get; set; }
            }
            """;

        await new VerifyCS.Test
        {
            CompilerDiagnostics = Microsoft.CodeAnalysis.Testing.CompilerDiagnostics.None,
            TestState =
            {
                Sources = { (SourceFile, code) },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError(MissingPartialModifier.Id)
                        .WithSpan(SourceFile, 6, 21, 6, 28)
                        .WithArguments("Test.Factory")
                }
            }
        }.RunAsync();
    }
}
