using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Converj.Generator.Tests;

internal static class CSharpSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : IIncrementalGenerator, new()
{

    internal class Test : CSharpSourceGeneratorTest<TSourceGenerator, DefaultVerifier>
    {
        /// <summary>
        /// Use this placeholder in expected generated source strings instead of a hardcoded version number.
        /// It will be replaced with the actual generator assembly version before verification.
        /// </summary>
        private const string VersionPlaceholder = "$$VERSION$$";
        
        private readonly string _actualVersion =
            typeof(FluentRootGenerator).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        
        internal Test()
        {
            // Reference the generator assembly (for any shared types if needed)
            TestState.AdditionalReferences.Add(typeof(FluentRootGenerator).Assembly);
            // Reference the attributes assembly so test code can resolve attribute types
            TestState.AdditionalReferences.Add(typeof(Attributes.FluentTargetAttribute).Assembly);

            // Add the source for required types and global aliases mapping old attribute names
            TestState.Sources.Add(
                """
                // Global aliases so tests written against the old namespace still compile
                global using Converj.Attributes;
                // Make System.Linq.Enumerable.ToArray() accessible in test compilations that
                // use ImmutableArray<T> fields in generated accumulator steps (GEN-02, GEN-04).
                global using System.Linq;

                // Provide IsExternalInit for record-like features in older targets
                namespace System.Runtime.CompilerServices
                {
                    internal static class IsExternalInit {}
                }
                """);
        }

        protected override CompilationOptions CreateCompilationOptions()
        {
            var compilationOptions = base.CreateCompilationOptions();
            return compilationOptions.WithSpecificDiagnosticOptions(
                compilationOptions.SpecificDiagnosticOptions.SetItems(GetNullableWarningsFromCompiler()));
        }

        private LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

        private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
        {
            string[] args = ["/warnaserror:nullable"];
            var commandLineArguments = CSharpCommandLineParser.Default.Parse(args,
                baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
            var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

            return nullableWarnings;
        }

        protected override ParseOptions CreateParseOptions()
        {
            return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
        }

        protected override async Task RunImplAsync(CancellationToken cancellationToken)
        {
            ResolveVersionPlaceholders();
            await base.RunImplAsync(cancellationToken);
        }

        private void ResolveVersionPlaceholders()
        {
            var sources = TestState.GeneratedSources.ToList();
            TestState.GeneratedSources.Clear();
            foreach (var (filename, content) in sources)
            {
                var resolved = content.ToString().Replace(VersionPlaceholder, _actualVersion);
                TestState.GeneratedSources.Add((filename,
                    SourceText.From(resolved, Encoding.UTF8)));
            }
        }
    }
}
