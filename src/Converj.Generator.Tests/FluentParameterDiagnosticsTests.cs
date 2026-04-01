using Microsoft.CodeAnalysis.Testing;
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Converj.Generator.Diagnostics.FluentDiagnostics;
using VerifyCS =
    Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentFactoryGenerator>;

namespace Converj.Generator.Tests;

public class FluentParameterDiagnosticsTests
{
    private const string SourceFile = "/0/Test1.cs";

    [Fact]
    internal async Task Should_error_when_fluent_parameter_type_does_not_match()
    {
        const string code =
            """
            using System;
            using Converj.Generator;

            namespace Test;

            [FluentRoot]
            public partial class Factory
            {
                [FluentParameter("value")]
                private readonly string _value;

                public Factory(string value) { _value = value; }
            }

            public class Target
            {
                [FluentTarget(typeof(Factory))]
                public Target(int value) { Value = value; }

                public int Value { get; set; }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        test.TestState.ExpectedDiagnostics.Add(
            new DiagnosticResult(FluentParameterTypeMismatch.Id, Error)
                .WithSpan(SourceFile, 10, 29, 10, 35)
                .WithArguments("_value", "string", "value", "int"));

        await test.RunAsync();
    }

    [Fact]
    internal async Task Should_error_when_duplicate_fluent_parameter_mappings_exist()
    {
        const string code =
            """
            using System;
            using Converj.Generator;

            namespace Test;

            [FluentRoot]
            public partial class Factory
            {
                [FluentParameter("value")]
                private readonly int _value1;

                [FluentParameter("value")]
                private readonly int _value2;

                public Factory(int value1, int value2) { _value1 = value1; _value2 = value2; }
            }

            public class Target
            {
                [FluentTarget(typeof(Factory))]
                public Target(int value) { Value = value; }

                public int Value { get; set; }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        test.TestState.ExpectedDiagnostics.Add(
            new DiagnosticResult(DuplicateFluentParameterMapping.Id, Error)
                .WithSpan(SourceFile, 13, 26, 13, 33)
                .WithArguments("value", "_value1", "_value2"));

        await test.RunAsync();
    }

    [Fact]
    internal async Task Should_error_when_fluent_parameter_property_has_no_getter()
    {
        const string code =
            """
            using System;
            using Converj.Generator;

            namespace Test;

            [FluentRoot]
            public partial class Factory
            {
                [FluentParameter("value")]
                public int Value { set { } }
            }

            public class Target
            {
                [FluentTarget(typeof(Factory))]
                public Target(int value) { }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        test.TestState.ExpectedDiagnostics.Add(
            new DiagnosticResult(FluentParameterPropertyWithoutGetter.Id, Error)
                .WithSpan(SourceFile, 10, 16, 10, 21)
                .WithArguments("Value"));

        await test.RunAsync();
    }
}
