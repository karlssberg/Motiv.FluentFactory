using Microsoft.CodeAnalysis.Testing;
using VerifyCS =
    Motiv.FluentFactory.Generator.Tests.CSharpSourceGeneratorVerifier<Motiv.FluentFactory.Generator.FluentFactoryGenerator>;

namespace Motiv.FluentFactory.Generator.Tests;

/// <summary>
/// Edge case tests for malformed FluentConstructor attribute usage (DIAG-01).
/// Tests assert DESIRED correct output — failing tests document generator shortcomings.
/// </summary>
public class FluentFactoryGeneratorMalformedAttributeTests
{
    private const string SourceFile = "Source.cs";

    /// <summary>
    /// Exercises MFFG0010 on a primary constructor record — combining the NoCreateMethod/CreateVerb conflict
    /// with record primary-constructor syntax. This is a distinct scenario from the explicit-constructor case
    /// already covered in FluentFactoryGeneratorBugDiscoveryTests.
    /// </summary>
    [Fact]
    internal async Task Should_error_when_create_method_name_and_no_create_method_conflict_on_primary_constructor_record()
    {
        const string source =
            """
            using Motiv.FluentFactory.Generator;

            namespace Test.Namespace
            {
                [FluentFactory]
                public partial class Factory;

                [FluentConstructor(typeof(Factory), CreateMethod = CreateMethod.None, CreateVerb = "Build")]
                public partial record MyRecord(int Value, string Name);
            }
            """;

        // Line 8: [FluentConstructor(typeof(Factory), CreateMethod = CreateMethod.None, CreateVerb = "Build")]
        // Attribute starts at col 6 (1-based), ends after closing bracket.
        // The entire attribute span is expected for MFFG0010.
        await new VerifyCS.Test
        {
            TestState = { Sources = { (SourceFile, source) } },
            ExpectedDiagnostics =
            {
                DiagnosticResult.CompilerError("MFFG0010")
                    .WithSpan("Source.cs", 8, 6, 8, 96)
                    .WithMessage("CreateVerb cannot be used with CreateMethod.None"),
            }
        }.RunAsync();
    }

    /// <summary>
    /// Exercises multiple simultaneous validation errors on a single FluentConstructor attribute:
    /// (1) the target type lacks [FluentFactory] (MFFG0009), and
    /// (2) the CreateVerb is an invalid identifier (MFFG0007).
    /// Tests assert DESIRED behavior where both diagnostics fire independently.
    /// If only one fires, the test documents the validation short-circuit.
    /// </summary>
    [Fact]
    internal async Task Should_error_for_both_missing_fluent_factory_and_invalid_create_method_name_simultaneously()
    {
        const string source =
            """
            using Motiv.FluentFactory.Generator;

            namespace Test.Namespace
            {
                // Missing [FluentFactory] attribute on purpose
                public partial class NonFactoryType;

                [FluentConstructor(typeof(NonFactoryType), CreateVerb = "123invalid")]
                public partial record MyRecord(int Value);
            }
            """;

        // MFFG0009 fires on the typeof(NonFactoryType) argument expression
        // MFFG0007 fires on the CreateVerb = "123invalid" named argument
        // DESIRED: both MFFG0009 and MFFG0007 fire. If only MFFG0009 fires, this test will fail,
        // documenting that validation short-circuits after the missing-FluentFactory error.
        await new VerifyCS.Test
        {
            TestState = { Sources = { (SourceFile, source) } },
            ExpectedDiagnostics =
            {
                DiagnosticResult.CompilerError("MFFG0009")
                    .WithSpan("Source.cs", 8, 24, 8, 46)
                    .WithMessage("FluentConstructor references type 'Test.Namespace.NonFactoryType' which does not have the FluentFactory attribute"),
                DiagnosticResult.CompilerError("MFFG0007")
                    .WithSpan("Source.cs", 8, 48, 8, 73)
                    .WithMessage("CreateVerb must be a valid identifier"),
            }
        }.RunAsync();
    }

    /// <summary>
    /// Exercises cascading validation errors across two constructors on the same type:
    /// both use CreateVerb = "Build" (triggering MFFG0008 — duplicate),
    /// and one also has NoCreateMethod (triggering MFFG0010 — conflict).
    /// Tests assert DESIRED behavior where both independent errors are reported.
    /// </summary>
    [Fact]
    internal async Task Should_error_for_both_duplicate_create_method_name_and_no_create_method_conflict()
    {
        const string source =
            """
            using Motiv.FluentFactory.Generator;

            namespace Test.Namespace
            {
                [FluentFactory]
                public partial class Factory;

                public partial class MyTarget
                {
                    [FluentConstructor(typeof(Factory), CreateVerb = "Build")]
                    public MyTarget(int value) { }

                    [FluentConstructor(typeof(Factory), CreateVerb = "Build", CreateMethod = CreateMethod.None)]
                    public MyTarget(string name) { }
                }
            }
            """;

        // MFFG0008 fires on the CreateVerb argument of both constructors (duplicate)
        // MFFG0010 fires on the entire attribute of the second constructor (NoCreateMethod conflict)
        await new VerifyCS.Test
        {
            TestState = { Sources = { (SourceFile, source) } },
            ExpectedDiagnostics =
            {
                DiagnosticResult.CompilerError("MFFG0008")
                    .WithSpan("Source.cs", 10, 45, 10, 65)
                    .WithSpan("Source.cs", 13, 45, 13, 65)
                    .WithMessage("Create method name must be unique"),
                DiagnosticResult.CompilerError("MFFG0010")
                    .WithSpan("Source.cs", 13, 10, 13, 100)
                    .WithMessage("CreateVerb cannot be used with CreateMethod.None"),
            }
        }.RunAsync();
    }
}
