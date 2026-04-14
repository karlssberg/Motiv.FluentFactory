using Microsoft.CodeAnalysis.Testing;
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Converj.Generator.Diagnostics.FluentDiagnostics;
using VerifyCS =
    Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentRootGenerator>;

namespace Converj.Generator.Tests;

/// <summary>
/// Tests for ATTR-02 (allowed collection types) and ATTR-03 (rejected non-collection types)
/// for the [FluentCollectionMethod] attribute.
/// </summary>
public class CollectionTypeDetectionTests
{
    private const string SourceFile = "/0/Test1.cs";

    // Shared preamble for test fixtures
    private const string AttributeUsings = """
        using System;
        using System.Collections.Generic;
        using Converj.Attributes;
        """;

    /// <summary>
    /// Builds a complete test source with a constructor that has a single collection parameter
    /// annotated with [FluentCollectionMethod].
    /// </summary>
    private static string BuildSource(string paramType, string paramName = "items", string attrArgs = "")
    {
        var attrSuffix = attrArgs.Length > 0 ? $"({attrArgs})" : string.Empty;
        return $$"""
        {{AttributeUsings}}

        namespace Test;

        [FluentRoot]
        public partial class Builder;

        public class Target
        {
            [FluentTarget(typeof(Builder))]
            public Target([FluentCollectionMethod{{attrSuffix}}] {{paramType}} {{paramName}}) { }
        }
        """;
    }

    // ATTR-02: Positive tests — all six allowlisted collection types should produce no diagnostic

    [Theory]
    [InlineData("int[]", "arrays")]
    [InlineData("System.Collections.Generic.IEnumerable<int>", "enumerables")]
    [InlineData("System.Collections.Generic.ICollection<int>", "collections")]
    [InlineData("System.Collections.Generic.IList<int>", "lists")]
    [InlineData("System.Collections.Generic.IReadOnlyCollection<int>", "readOnlyCollections")]
    [InlineData("System.Collections.Generic.IReadOnlyList<int>", "readOnlyLists")]
    internal async Task AllowedCollectionType_ProducesNoDiagnostic(string collectionType, string paramName)
    {
        var code = BuildSource(collectionType, paramName);

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        // Skip generated sources check — we only care that no CVJG0050 diagnostic is emitted.
        // The fluent chain generated for collection parameters is verified by Plan 05's snapshot test.
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        await test.RunAsync();
    }

    // ATTR-03: Negative tests — non-allowlisted types should produce CVJG0050
    // All tests use BuildSource which puts [FluentCollectionMethod] at line 13, col 20-42

    [Theory]
    [InlineData("string", "items", "items", "string")]
    [InlineData("int", "counts", "counts", "int")]
    [InlineData("System.Collections.Generic.Dictionary<string, string>", "mappings", "mappings", "System.Collections.Generic.Dictionary<string, string>")]
    [InlineData("System.Collections.Generic.HashSet<int>", "hashValues", "hashValues", "System.Collections.Generic.HashSet<int>")]
    [InlineData("System.Collections.Generic.List<int>", "listValues", "listValues", "System.Collections.Generic.List<int>")]
    [InlineData("System.Collections.Generic.Stack<int>", "stacks", "stacks", "System.Collections.Generic.Stack<int>")]
    [InlineData("System.Collections.Generic.Queue<int>", "queues", "queues", "System.Collections.Generic.Queue<int>")]
    internal async Task NonAllowedCollectionType_ProducesCVJG0050(
        string paramType, string paramName, string expectedParamName, string expectedTypeName)
    {
        var code = BuildSource(paramType, paramName);

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        test.TestState.ExpectedDiagnostics.Add(
            new DiagnosticResult(NonCollectionFluentCollectionMethod.Id, Error)
                .WithSpan(SourceFile, 13, 20, 13, 42)
                .WithArguments(expectedParamName, expectedTypeName));

        await test.RunAsync();
    }

    // Pitfall 1 regression: string must NOT be silently accepted as IEnumerable<char>
    // string implements IEnumerable<char> at runtime, but we do NOT walk AllInterfaces.
    // Applying [FluentCollectionMethod] to a string parameter must produce CVJG0050.
    [Fact]
    internal async Task String_ProducesCVJG0050_NotSilentlyAcceptedAsIEnumerableChar()
    {
        // Note: this is explicitly covered by the theory above, but we add a standalone
        // test with a comment to document the Pitfall 1 regression requirement.
        var code = BuildSource("string", "items");

        var test = new VerifyCS.Test
        {
            TestState = { Sources = { code } }
        };

        test.TestState.ExpectedDiagnostics.Add(
            new DiagnosticResult(NonCollectionFluentCollectionMethod.Id, Error)
                .WithSpan(SourceFile, 13, 20, 13, 42)
                .WithArguments("items", "string"));

        await test.RunAsync();
    }
}
