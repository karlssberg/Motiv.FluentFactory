using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Factory methods for creating and de-duplicating fluent target contexts from syntax nodes.
/// </summary>
internal static class FluentTargetContextFactory
{
    /// <summary>
    /// Creates target contexts from a syntax node that has a FluentTarget attribute.
    /// </summary>
    /// <param name="compilation">The current compilation.</param>
    /// <param name="syntaxTree">The syntax node to extract contexts from.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An array of target context enumerables grouped by attribute usage.</returns>
    public static ImmutableArray<IEnumerable<FluentTargetContext>> CreateTargetContexts(
        Compilation compilation,
        SyntaxNode syntaxTree,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var semanticModel = compilation.GetSemanticModel(syntaxTree.SyntaxTree);

        var symbol = semanticModel.GetDeclaredSymbol(syntaxTree);
        if (symbol is null)
            return [];

        return
        [
            ..FluentRootMetadataReader.GetFluentFactoryMetadata(symbol)
                .Select(metadata =>
                {
                    var attributePresent = metadata.AttributePresent;
                    var rootTypeFullName = metadata.RootTypeFullName;
                    if (!attributePresent || string.IsNullOrWhiteSpace(rootTypeFullName))
                        return [];

                    var defaults = FluentRootMetadataReader.GetFluentFactoryDefaults(metadata.RootTypeSymbol);
                    metadata.TerminalMethod ??= defaults.TerminalMethod;
                    metadata.TerminalVerb ??= defaults.TerminalVerb;
                    metadata.MethodPrefix ??= defaults.MethodPrefix;
                    metadata.ReturnType ??= defaults.ReturnType;

                    // Check for [FluentEntryMethod] on the symbol
                    var (hasEntryMethod, entryMethodName) =
                        FluentRootMetadataReader.ReadFluentEntryMethodAttribute(symbol);
                    metadata.HasEntryMethod = hasEntryMethod;
                    metadata.EntryMethodName = entryMethodName;

                    return symbol switch
                    {
                        IMethodSymbol constructor =>
                        [
                            new FluentTargetContext(
                                constructor,
                                metadata.AttributeData!,
                                metadata.RootTypeSymbol,
                                metadata,
                                false,
                                semanticModel)
                        ],
                        INamedTypeSymbol type => CreateContainingTypeFluentTargetContexts(
                            type,
                            metadata.RootTypeSymbol,
                            metadata),
                        _ => []
                    };
                })
        ];

        ImmutableArray<FluentTargetContext> CreateContainingTypeFluentTargetContexts(
            INamedTypeSymbol type,
            INamedTypeSymbol alreadyDeclaredRootType,
            FluentRootMetadata metadata)
        {
            return
            [
                ..type.Constructors
                    .Where(ctor => !ctor.IsImplicitlyDeclared)
                    .Select(ctor =>
                        new FluentTargetContext(
                            ctor,
                            metadata.AttributeData!,
                            alreadyDeclaredRootType,
                            metadata,
                            true,
                            semanticModel))
            ];
        }
    }

    /// <summary>
    /// De-duplicates fluent targets by choosing overriding targets when the same constructor
    /// is attributed both on the type and on the constructor directly.
    /// </summary>
    /// <param name="fluentApiTargets">The target contexts to de-duplicate.</param>
    /// <returns>De-duplicated target contexts.</returns>
    public static IEnumerable<FluentTargetContext> DeDuplicateFluentTargets(
        IEnumerable<FluentTargetContext> fluentApiTargets) =>
        fluentApiTargets
            .GroupBy(targetContext => targetContext.Constructor,
                SymbolEqualityComparer.Default)
            .SelectMany(ChooseOverridingTargets);

    /// <summary>
    /// Chooses which target contexts to keep when duplicates exist, preferring
    /// constructor-level attributes over type-level attributes.
    /// </summary>
    /// <param name="duplicateTargets">The duplicate target contexts to choose from.</param>
    /// <returns>The chosen target contexts.</returns>
    public static ImmutableList<FluentTargetContext> ChooseOverridingTargets(IEnumerable<FluentTargetContext> duplicateTargets)
    {
        var emptyList = ImmutableList<FluentTargetContext>.Empty;
        var (usedOnType, usedOnConstructor) = duplicateTargets
            .Aggregate(
                (OnType: emptyList, OnConstructor: emptyList),
                (whenAttributes, ctor) => ctor.IsAttributedUsedOnContainingType switch
                {
                    true => (whenAttributes.OnType.Add(ctor),
                        whenAttributes.OnConstructor),
                    false => (whenAttributes.OnType,
                        whenAttributes.OnConstructor.Add(ctor)),
                });

        return usedOnConstructor.Any()
            ? usedOnConstructor
            : usedOnType;
    }
}
