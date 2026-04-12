using System.Collections.Immutable;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.SyntaxGeneration;
using Converj.Generator.TargetAnalysis;

namespace Converj.Generator;

/// <summary>
/// Source generator that emits fluent root builder APIs from constructors, methods, and types annotated with [FluentTarget].
/// </summary>
[Generator(LanguageNames.CSharp)]
public class FluentRootGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the source generator by setting up the incremental generation pipeline.
    /// </summary>
    /// <param name="context">The initialization context for the incremental generator.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationProvider = context.CompilationProvider;

        // Step 1: Match FluentTargetAttribute usages (non-generic and generic)
        var nonGenericDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(TypeName.FluentTargetAttribute,
                (node, _) => node switch
                {
                    // Capture type declarations with FluentTarget attributes (for applying to all constructors)
                    TypeDeclarationSyntax { AttributeLists.Count: > 0 } => true,
                    // Capture explicit constructors
                    ConstructorDeclarationSyntax { AttributeLists.Count: > 0 } => true,
                    // Capture static methods
                    MethodDeclarationSyntax { AttributeLists.Count: > 0 } => true,
                    _ => false
                },
                (ctx, _) =>
                {
                    var syntax = ctx.TargetNode;
                    var filePath = syntax.SyntaxTree.FilePath;
                    return (syntax, filePath);
                }
            );

        var genericDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(TypeName.GenericFluentTargetAttribute,
                (node, _) => node switch
                {
                    TypeDeclarationSyntax { AttributeLists.Count: > 0 } => true,
                    ConstructorDeclarationSyntax { AttributeLists.Count: > 0 } => true,
                    MethodDeclarationSyntax { AttributeLists.Count: > 0 } => true,
                    _ => false
                },
                (ctx, _) =>
                {
                    var syntax = ctx.TargetNode;
                    var filePath = syntax.SyntaxTree.FilePath;
                    return (syntax, filePath);
                }
            );

        var typeOrConstructorDeclarations = nonGenericDeclarations
            .Collect()
            .Combine(genericDeclarations.Collect())
            .SelectMany((pair, _) => DeduplicateBySyntaxNode(pair.Left, pair.Right));

        // Step 2: Gather all discovered candidate constructors and capture metadata
        var constructorModels = typeOrConstructorDeclarations
            .Combine(compilationProvider)
            .SelectMany((data, ct) =>
            {
                var compilation = data.Right;
                var syntax = data.Left.syntax;
                return FluentTargetContextFactory.CreateTargetContexts(compilation, syntax, ct);
            })
            .WithTrackingName("ConstructorModelCreation");

        // Step 3: Transform constructor contexts into compiled file contents
        var consolidated = constructorModels
            .Collect()
            .Combine(compilationProvider)
            .WithTrackingName("ConstructorModelsConsolidation")
            .SelectMany((tuple, _) =>
            {
                var (builderContextsCollection, compilation) = tuple;
                return builderContextsCollection
                    .SelectMany(builderContexts => builderContexts)
                    .GroupBy(builderContext => builderContext.RootType, SymbolEqualityComparer.Default)
                    .Select(fluentApiConstructors =>
                        new FluentModelBuilder(compilation)
                            .CreateFluentRootCompilationUnit(
                                (INamedTypeSymbol)fluentApiConstructors.Key!,
                                [..FluentTargetContextFactory.DeDuplicateFluentTargets(fluentApiConstructors)]));
            })
            .WithTrackingName("ConstructorModelsToFluentBuilderFiles");

        // Step 4: Write the generated files.
        context.RegisterSourceOutput(consolidated, Execute);
    }

    /// <summary>
    /// Combines non-generic and generic pipeline results, removing duplicates where the same
    /// syntax node was discovered by both pipelines (e.g., a type with both generic and non-generic
    /// [FluentTarget] attributes).
    /// </summary>
    private static ImmutableArray<(SyntaxNode syntax, string filePath)> DeduplicateBySyntaxNode(
        ImmutableArray<(SyntaxNode syntax, string filePath)> left,
        ImmutableArray<(SyntaxNode syntax, string filePath)> right)
    {
        if (right.IsEmpty)
            return left;

        if (left.IsEmpty)
            return right;

        var seen = new HashSet<SyntaxNode>(left.Select(entry => entry.syntax));
        var builder = ImmutableArray.CreateBuilder<(SyntaxNode syntax, string filePath)>(left.Length + right.Length);
        builder.AddRange(left);

        foreach (var entry in right)
        {
            if (seen.Add(entry.syntax))
                builder.Add(entry);
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Executes the source generation for a single fluent root compilation unit.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="builder">The compilation unit containing the fluent root to generate.</param>
    private static void Execute(
        SourceProductionContext context,
        FluentRootCompilationUnit builder)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        foreach (var diagnostic in builder.Diagnostics)
            context.ReportDiagnostic(diagnostic);

        if (builder.IsEmpty || builder.Diagnostics.Any(d =>
                d.Id == Diagnostics.FluentDiagnostics.UnresolvableCustomStepStorage.Id ||
                d.Id == Diagnostics.FluentDiagnostics.FluentStoragePropertyWithoutGetter.Id ||
                d.Id == Diagnostics.FluentDiagnostics.DuplicateFluentStorageMapping.Id))
            return;

        var source = $"// <auto-generated/>\n#nullable enable\n{CompilationUnit.CreateCompilationUnit(builder).NormalizeWhitespace(eol: "\n")}";
        context.AddSource($"{builder.RootType.ToFileName()}.g.cs", source);
    }
}
