using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converj.Generator.ConstructorAnalysis;
using Converj.Generator.SyntaxGeneration;

namespace Converj.Generator;

/// <summary>
/// Source generator for creating fluent factories based on constructors marked with the FluentConstructor attribute.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class FluentFactoryGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the source generator by setting up the incremental generation pipeline.
    /// </summary>
    /// <param name="context">The initialization context for the incremental generator.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationProvider = context.CompilationProvider;

        // Step 1: Match FluentConstructorAttribute usages (non-generic and generic)
        var nonGenericDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(TypeName.FluentConstructorAttribute,
                (node, _) => node switch
                {
                    // Capture type declarations with FluentConstructor attributes (for applying to all constructors)
                    TypeDeclarationSyntax { AttributeLists.Count: > 0 } => true,
                    // Capture explicit constructors
                    ConstructorDeclarationSyntax { AttributeLists.Count: > 0 } => true,
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
            .ForAttributeWithMetadataName(TypeName.GenericFluentConstructorAttribute,
                (node, _) => node switch
                {
                    TypeDeclarationSyntax { AttributeLists.Count: > 0 } => true,
                    ConstructorDeclarationSyntax { AttributeLists.Count: > 0 } => true,
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
            .SelectMany((pair, _) => pair.Left.AddRange(pair.Right));

        // Step 2: Gather all discovered candidate constructors and capture metadata
        var constructorModels = typeOrConstructorDeclarations
            .Combine(compilationProvider)
            .SelectMany((data, ct) =>
            {
                var compilation = data.Right;
                var syntax = data.Left.syntax;
                return FluentConstructorContextFactory.CreateConstructorContexts(compilation, syntax, ct);
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
                        new FluentModelFactory(compilation)
                            .CreateFluentFactoryCompilationUnit(
                                (INamedTypeSymbol)fluentApiConstructors.Key!,
                                [..FluentConstructorContextFactory.DeDuplicateFluentConstructors(fluentApiConstructors)]));
            })
            .WithTrackingName("ConstructorModelsToFluentBuilderFiles");

        // Step 4: Write the generated files.
        context.RegisterSourceOutput(consolidated, Execute);
    }

    /// <summary>
    /// Executes the source generation for a single fluent factory compilation unit.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="builder">The compilation unit containing the fluent factory to generate.</param>
    private static void Execute(
        SourceProductionContext context,
        FluentFactoryCompilationUnit builder)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        foreach (var diagnostic in builder.Diagnostics)
            context.ReportDiagnostic(diagnostic);

        if (builder.IsEmpty || builder.Diagnostics.Any(d => d.Id == Diagnostics.FluentDiagnostics.UnresolvableCustomStepStorage.Id))
            return;

        var source = $"// <auto-generated/>\n#nullable enable\n{CompilationUnit.CreateCompilationUnit(builder).NormalizeWhitespace(eol: "\n")}";
        context.AddSource($"{builder.RootType.ToFileName()}.g.cs", source);
    }
}
