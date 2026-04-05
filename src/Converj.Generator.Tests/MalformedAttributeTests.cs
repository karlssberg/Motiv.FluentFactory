using Microsoft.CodeAnalysis.Testing;
using VerifyCS =
    Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentFactoryGenerator>;

namespace Converj.Generator.Tests;

/// <summary>
/// Edge case tests for malformed FluentConstructor attribute usage (DIAG-01).
/// Tests assert DESIRED correct output — failing tests document generator shortcomings.
/// </summary>
public class MalformedAttributeTests
{
    private const string SourceFile = "Source.cs";

    /// <summary>
    /// Exercises CVJG0010 on a primary constructor record — combining the NoCreateMethod/CreateVerb conflict
    /// with record primary-constructor syntax. This is a distinct scenario from the explicit-constructor case
    /// already covered in FluentFactoryGeneratorBugDiscoveryTests.
    /// </summary>
    [Fact]
    internal async Task Should_error_when_create_method_name_and_no_create_method_conflict_on_primary_constructor_record()
    {
        const string source =
            """
            using Converj.Generator;

            namespace Test.Namespace
            {
                [FluentRoot]
                public partial class Factory;

                [FluentTarget(typeof(Factory), TerminalMethod = TerminalMethod.None, TerminalVerb = "Build")]
                public partial record MyRecord(int Value, string Name);
            }
            """;

        // Line 8: [FluentTarget(typeof(Factory), TerminalMethod = TerminalMethod.None, TerminalVerb = "Build")]
        // Attribute starts at col 6 (1-based), ends after closing bracket.
        // The entire attribute span is expected for CVJG0010.
        await new VerifyCS.Test
        {
            TestState = { Sources = { (SourceFile, source) } },
            ExpectedDiagnostics =
            {
                DiagnosticResult.CompilerError("CVJG0010")
                    .WithSpan("Source.cs", 8, 6, 8, 97)
                    .WithMessage("TerminalVerb cannot be used with TerminalMethod.None"),
            }
        }.RunAsync();
    }

    /// <summary>
    /// Exercises multiple simultaneous validation errors on a single FluentConstructor attribute:
    /// (1) the target type lacks [FluentRoot] (CVJG0009), and
    /// (2) the CreateVerb is an invalid identifier (CVJG0007).
    /// Tests assert DESIRED behavior where both diagnostics fire independently.
    /// If only one fires, the test documents the validation short-circuit.
    /// </summary>
    [Fact]
    internal async Task Should_error_for_both_missing_fluent_factory_and_invalid_create_method_name_simultaneously()
    {
        const string source =
            """
            using Converj.Generator;

            namespace Test.Namespace
            {
                // Missing [FluentRoot] attribute on purpose
                public partial class NonFactoryType;

                [FluentTarget(typeof(NonFactoryType), TerminalVerb = "123invalid")]
                public partial record MyRecord(int Value);
            }
            """;

        // CVJG0009 fires on the typeof(NonFactoryType) argument expression
        // CVJG0007 fires on the TerminalVerb = "123invalid" named argument
        // DESIRED: both CVJG0009 and CVJG0007 fire. If only CVJG0009 fires, this test will fail,
        // documenting that validation short-circuits after the missing-FluentFactory error.
        await new VerifyCS.Test
        {
            TestState = { Sources = { (SourceFile, source) } },
            ExpectedDiagnostics =
            {
                DiagnosticResult.CompilerError("CVJG0009")
                    .WithSpan("Source.cs", 8, 19, 8, 41)
                    .WithMessage("FluentTarget references type 'Test.Namespace.NonFactoryType' which does not have the FluentRoot attribute"),
                DiagnosticResult.CompilerError("CVJG0007")
                    .WithSpan("Source.cs", 8, 43, 8, 70)
                    .WithMessage("TerminalVerb must be a valid identifier"),
            }
        }.RunAsync();
    }

    /// <summary>
    /// Exercises cascading validation errors across two constructors on the same type:
    /// both use TerminalVerb = "Build" (triggering CVJG0008 — duplicate),
    /// and one also has NoCreateMethod (triggering CVJG0010 — conflict).
    /// Tests assert DESIRED behavior where both independent errors are reported.
    /// </summary>
    [Fact]
    internal async Task Should_error_for_both_duplicate_create_method_name_and_no_create_method_conflict()
    {
        const string source =
            """
            using Converj.Generator;

            namespace Test.Namespace
            {
                [FluentRoot]
                public partial class Factory;

                public partial class MyTarget
                {
                    [FluentTarget(typeof(Factory), TerminalVerb = "Build")]
                    public MyTarget(int value) { }

                    [FluentTarget(typeof(Factory), TerminalVerb = "Build", TerminalMethod = TerminalMethod.None)]
                    public MyTarget(string name) { }
                }
            }
            """;

        // CVJG0008 fires on the CreateVerb argument of both constructors (duplicate)
        // CVJG0010 fires on the entire attribute of the second constructor (NoCreateMethod conflict)
        await new VerifyCS.Test
        {
            TestState = { Sources = { (SourceFile, source) } },
            ExpectedDiagnostics =
            {
                DiagnosticResult.CompilerError("CVJG0008")
                    .WithSpan("Source.cs", 10, 40, 10, 62)
                    .WithSpan("Source.cs", 13, 40, 13, 62)
                    .WithMessage("Terminal method name must be unique"),
                DiagnosticResult.CompilerError("CVJG0010")
                    .WithSpan("Source.cs", 13, 10, 13, 101)
                    .WithMessage("TerminalVerb cannot be used with TerminalMethod.None"),
            }
        }.RunAsync();
    }
}
