using Microsoft.CodeAnalysis.Testing;
using VerifyCS =
    Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentRootGenerator>;

namespace Converj.Generator.Tests;

public class FluentCollectionMethodAttributeTests
{
    [Fact]
    internal async Task Applying_parameterless_attribute_compiles()
    {
        const string code = """
            using System.Collections.Generic;

            [FluentRoot]
            public static partial class CatFactory { }

            public class Cat
            {
                public Cat([FluentCollectionMethod] IEnumerable<string> tags) { }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };
        await test.RunAsync();
    }

    [Fact]
    internal async Task Applying_single_string_attribute_compiles()
    {
        const string code = """
            using System.Collections.Generic;

            [FluentRoot]
            public static partial class CatFactory { }

            public class Cat
            {
                public Cat([FluentCollectionMethod("AddEntry")] IEnumerable<string> tags) { }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };
        await test.RunAsync();
    }

    [Fact]
    internal async Task MinItems_named_argument_compiles()
    {
        const string code = """
            using System.Collections.Generic;

            [FluentRoot]
            public static partial class CatFactory { }

            public class Cat
            {
                public Cat([FluentCollectionMethod(MinItems = 3)] IEnumerable<string> tags) { }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };
        await test.RunAsync();
    }

    [Fact]
    internal async Task Applying_to_type_is_compile_error()
    {
        const string code = """
            [FluentCollectionMethod]
            public class Foo { }
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("CS0592").WithSpan("/0/Test1.cs", 1, 2, 1, 24)
                        .WithArguments("FluentCollectionMethod", "property, indexer, parameter")
                }
            }
        };
        await test.RunAsync();
    }
}
