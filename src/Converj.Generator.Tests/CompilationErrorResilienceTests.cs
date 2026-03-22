using Microsoft.CodeAnalysis.Testing;
using VerifyCS =
    Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentFactoryGenerator>;

namespace Converj.Generator.Tests;

/// <summary>
/// Tests verifying that the generator handles user code with C# compilation errors gracefully.
///
/// DIAG-03: The generator must not throw unhandled exceptions when processing source code that
/// has C# compilation errors. These tests exercise resilience by feeding deliberately broken
/// input to the generator pipeline and verifying it exits cleanly.
/// </summary>
public class CompilationErrorResilienceTests
{
    private const string SourceFile = "Source.cs";

    /// <summary>
    /// DIAG-03 Test 1: A FluentConstructor with a parameter whose type does not exist
    /// (e.g., <c>NonExistentType value</c>) causes the C# compiler to emit CS0246.
    ///
    /// The generator must not throw an unhandled exception when it encounters an IErrorTypeSymbol
    /// for the parameter type. The DESIRED behavior is that the generator silently skips this
    /// constructor and emits no MFFG diagnostics -- the C# compiler error is sufficient signal.
    /// </summary>
    [Fact]
    internal async Task Given_a_constructor_with_a_parameter_of_nonexistent_type_Should_not_throw()
    {
        const string code =
            """
            using Converj.Generator;

            namespace Test;

            [FluentFactory]
            public static partial class Factory;

            public partial class Target
            {
                [FluentConstructor(typeof(Factory))]
                public Target(NonExistentType value) { }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) }
            },
            CompilerDiagnostics = CompilerDiagnostics.None
        };
        await test.RunAsync();
    }

    /// <summary>
    /// DIAG-03 Test 2: A FluentConstructor on a class where the factory type's declaration has
    /// a deliberate syntax error (missing closing brace). The C# parser may not recognize the
    /// attribute at all, or may partially parse the file.
    ///
    /// The generator must not throw when the syntax tree contains errors. The DESIRED behavior
    /// is that no generated output is produced and no MFFG diagnostics are emitted.
    /// </summary>
    [Fact]
    internal async Task Given_a_factory_type_declaration_with_a_syntax_error_Should_not_throw()
    {
        const string code =
            """
            using Converj.Generator;

            namespace Test;

            [FluentFactory]
            public static partial class Factory
            {
                // Missing closing brace is impossible to represent cleanly in a well-formed string,
                // so we simulate a syntax error via an unclosed generic constraint instead.
                // The constructor references an undefined base type to trigger a compilation error.
                public static void BrokenMethod(
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) }
            },
            CompilerDiagnostics = CompilerDiagnostics.None
        };
        await test.RunAsync();
    }

    /// <summary>
    /// DIAG-03 Test 3: A FluentConstructor on a class with a mix of valid and invalid parameter
    /// types. The first parameter (<c>string name</c>) is valid, but the second (<c>UndefinedType broken</c>)
    /// does not exist and triggers CS0246.
    ///
    /// The generator encounters an IErrorTypeSymbol midway through parameter analysis.
    /// The DESIRED behavior is that it skips this constructor entirely (since one parameter is
    /// unresolvable), emits no MFFG diagnostics, produces no generated source, and does not throw.
    /// </summary>
    [Fact]
    internal async Task Given_a_constructor_with_mixed_valid_and_invalid_parameter_types_Should_not_throw()
    {
        const string code =
            """
            using Converj.Generator;

            namespace Test;

            [FluentFactory]
            public static partial class Factory;

            public partial class Target
            {
                [FluentConstructor(typeof(Factory))]
                public Target(string name, UndefinedType broken) { }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) }
            },
            CompilerDiagnostics = CompilerDiagnostics.None
        };
        await test.RunAsync();
    }
}
