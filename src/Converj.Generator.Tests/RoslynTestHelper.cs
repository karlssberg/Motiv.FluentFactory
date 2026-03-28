using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Converj.Generator.Tests;

/// <summary>
/// Provides helpers for running the source generator and inspecting output
/// without requiring exact snapshot matching.
/// </summary>
internal static class RoslynTestHelper
{
    private static readonly string GlobalAliases =
        """
        global using Converj.Attributes;

        namespace System.Runtime.CompilerServices
        {
            internal static class IsExternalInit {}
        }
        """;

    /// <summary>
    /// Runs the FluentFactoryGenerator on the given source and returns the resulting compilation.
    /// </summary>
    public static Task<Compilation> GetGeneratedCompilationAsync(string source)
    {
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(source),
            CSharpSyntaxTree.ParseText(GlobalAliases)
        };

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(FluentFactoryGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attributes.FluentConstructorAttribute).Assembly.Location)
            });

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new FluentFactoryGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out _);

        return Task.FromResult(outputCompilation);
    }

    /// <summary>
    /// Extracts the first generated source text from a compilation that was run through the generator.
    /// </summary>
    public static string GetGeneratedSource(Compilation compilation)
    {
        return compilation.SyntaxTrees
            .Where(t => t.FilePath.Contains(".g.cs"))
            .Select(t => t.GetText().ToString())
            .FirstOrDefault() ?? string.Empty;
    }
}
