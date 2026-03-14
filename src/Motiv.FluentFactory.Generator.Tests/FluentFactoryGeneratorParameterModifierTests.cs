using Microsoft.CodeAnalysis.Testing;
using static Motiv.FluentFactory.Generator.Diagnostics.FluentDiagnostics;
using VerifyCS =
    Motiv.FluentFactory.Generator.Tests.CSharpSourceGeneratorVerifier<Motiv.FluentFactory.Generator.FluentFactoryGenerator>;

namespace Motiv.FluentFactory.Generator.Tests;

/// <summary>
/// Tests for constructor parameter modifier edge cases.
/// Constructors with ref, out, in, or ref readonly parameters cannot be used in fluent
/// factory generation because the builder pattern stores values in struct fields and
/// the reference semantics would be lost. Such constructors should be skipped with
/// a diagnostic warning.
/// </summary>
public class FluentFactoryGeneratorParameterModifierTests
{
    private const string SourceFile = "Source.cs";

    /// <summary>
    /// A constructor with a single <c>ref</c> parameter cannot be used in a fluent factory.
    /// The generator should emit a warning and produce no generated output for that constructor.
    /// When no valid constructors remain, no source file is generated.
    /// </summary>
    [Fact]
    internal async Task Given_a_constructor_with_a_ref_parameter_Should_emit_diagnostic_and_skip_generation()
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
                public MyTarget(ref int value)
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
                    DiagnosticResult.CompilerWarning(UnsupportedParameterModifier.Id)
                        .WithSpan(SourceFile, 11, 12, 11, 20)
                        .WithArguments("Test.MyTarget.MyTarget(ref int)", "value", "ref")
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// A constructor with a single <c>out</c> parameter cannot be used in a fluent factory.
    /// The generator should emit a warning and produce no generated output for that constructor.
    /// When no valid constructors remain, no source file is generated.
    /// </summary>
    [Fact]
    internal async Task Given_a_constructor_with_an_out_parameter_Should_emit_diagnostic_and_skip_generation()
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
                public MyTarget(out int value)
                {
                    value = 0;
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
                    DiagnosticResult.CompilerWarning(UnsupportedParameterModifier.Id)
                        .WithSpan(SourceFile, 11, 12, 11, 20)
                        .WithArguments("Test.MyTarget.MyTarget(out int)", "value", "out")
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// A constructor with a <c>ref readonly</c> parameter cannot be used in a fluent factory.
    /// The generator should emit a warning and produce no generated output for that constructor.
    /// When no valid constructors remain, no source file is generated.
    /// </summary>
    [Fact]
    internal async Task Given_a_constructor_with_a_ref_readonly_parameter_Should_emit_diagnostic_and_skip_generation()
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
                public MyTarget(ref readonly int value)
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
                    DiagnosticResult.CompilerWarning(UnsupportedParameterModifier.Id)
                        .WithSpan(SourceFile, 11, 12, 11, 20)
                        .WithArguments("Test.MyTarget.MyTarget(ref readonly int)", "value", "ref readonly")
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// When a class has two constructors — one with a <c>ref</c> parameter (unsupported) and one
    /// with normal parameters (supported) — the generator should emit a diagnostic for the ref
    /// constructor and still produce fluent factory output for the normal constructor.
    /// </summary>
    [Fact]
    internal async Task Given_a_class_with_two_constructors_where_one_has_ref_param_Should_emit_diagnostic_and_generate_for_normal_constructor()
    {
        const string code =
            """
            using Motiv.FluentFactory.Generator;

            namespace Test;

            [FluentFactory]
            public static partial class Factory;

            [FluentConstructor(typeof(Factory))]
            public class MyTarget
            {
                public MyTarget(int value)
                {
                    Value = value;
                }

                public MyTarget(ref int value)
                {
                    Value = value;
                }

                public int Value { get; set; }
            }
            """;

        const string expected =
            """
            namespace Test
            {
                [global::System.CodeDom.Compiler.GeneratedCode("Motiv.FluentFactory", "1.0.0.0")]
                public static partial class Factory
                {
                    /// <summary>
                    ///     <seealso cref="Test.MyTarget"/>
                    /// </summary>
                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    public static global::Test.Step_0__Test_Factory WithValue(in int value)
                    {
                        return new global::Test.Step_0__Test_Factory(value);
                    }
                }

                [global::System.CodeDom.Compiler.GeneratedCode("Motiv.FluentFactory", "1.0.0.0")]
                /// <summary>
                ///     <seealso cref="Test.MyTarget"/>
                /// </summary>
                public struct Step_0__Test_Factory
                {
                    private readonly int _value__parameter;
                    internal Step_0__Test_Factory(in int value)
                    {
                        this._value__parameter = value;
                    }

                    /// <summary>
                    /// Creates a new instance using constructor Test.MyTarget.MyTarget(int value).
                    ///
                    ///     <seealso cref="Test.MyTarget"/>
                    /// </summary>
                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    public global::Test.MyTarget Create()
                    {
                        return new global::Test.MyTarget(this._value__parameter);
                    }
                }
            }// <auto-generated/>
            #nullable enable

            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerWarning(UnsupportedParameterModifier.Id)
                        .WithSpan(SourceFile, 16, 12, 16, 20)
                        .WithArguments("Test.MyTarget.MyTarget(ref int)", "value", "ref")
                },
                GeneratedSources =
                {
                    (typeof(FluentFactoryGenerator), "Test.Factory.g.cs", expected)
                }
            }
        }.RunAsync();
    }
}
