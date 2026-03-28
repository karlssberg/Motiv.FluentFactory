using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.ConstructorAnalysis;

/// <summary>
/// Factory methods for creating and de-duplicating fluent constructor contexts from syntax nodes.
/// </summary>
internal static class FluentConstructorContextFactory
{
    /// <summary>
    /// Creates constructor contexts from a syntax node that has a FluentConstructor attribute.
    /// </summary>
    /// <param name="compilation">The current compilation.</param>
    /// <param name="syntaxTree">The syntax node to extract contexts from.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An array of constructor context enumerables grouped by attribute usage.</returns>
    public static ImmutableArray<IEnumerable<FluentConstructorContext>> CreateConstructorContexts(
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
            ..FluentFactoryMetadataReader.GetFluentFactoryMetadata(symbol)
                .Select(metadata =>
                {
                    var attributePresent = metadata.AttributePresent;
                    var rootTypeFullName = metadata.RootTypeFullName;
                    if (!attributePresent || string.IsNullOrWhiteSpace(rootTypeFullName))
                        return [];

                    var defaults = FluentFactoryMetadataReader.GetFluentFactoryDefaults(metadata.RootTypeSymbol);
                    metadata.CreateMethod ??= defaults.CreateMethod ?? CreateMethodMode.Dynamic;
                    metadata.CreateVerb ??= defaults.CreateVerb;
                    metadata.MethodPrefix ??= defaults.MethodPrefix;
                    metadata.ReturnType ??= defaults.ReturnType;
                    metadata.BuilderMode ??= defaults.BuilderMode;
                    metadata.TypeFirstVerb ??= defaults.TypeFirstVerb;

                    return symbol switch
                    {
                        IMethodSymbol constructor =>
                        [
                            new FluentConstructorContext(
                                constructor,
                                metadata.AttributeData!,
                                metadata.RootTypeSymbol,
                                metadata,
                                false,
                                semanticModel)
                        ],
                        INamedTypeSymbol type => CreateContainingTypeFluentConstructorContexts(
                            type,
                            metadata.RootTypeSymbol,
                            metadata),
                        _ => []
                    };
                })
        ];

        ImmutableArray<FluentConstructorContext> CreateContainingTypeFluentConstructorContexts(
            INamedTypeSymbol type,
            INamedTypeSymbol alreadyDeclaredRootType,
            FluentFactoryMetadata metadata)
        {
            return
            [
                ..type.Constructors
                    .Where(ctor => !ctor.IsImplicitlyDeclared)
                    .Select(ctor =>
                        new FluentConstructorContext(
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
    /// De-duplicates fluent constructors by choosing overriding constructors when the same constructor
    /// is attributed both on the type and on the constructor directly.
    /// </summary>
    /// <param name="fluentApiConstructors">The constructor contexts to de-duplicate.</param>
    /// <returns>De-duplicated constructor contexts.</returns>
    public static IEnumerable<FluentConstructorContext> DeDuplicateFluentConstructors(
        IEnumerable<FluentConstructorContext> fluentApiConstructors) =>
        fluentApiConstructors
            .GroupBy(constructorContext => constructorContext.Constructor,
                SymbolEqualityComparer.Default)
            .SelectMany(ChooseOverridingConstructors);

    /// <summary>
    /// Chooses which constructor contexts to keep when duplicates exist, preferring
    /// constructor-level attributes over type-level attributes.
    /// </summary>
    /// <param name="duplicateConstructors">The duplicate constructor contexts to choose from.</param>
    /// <returns>The chosen constructor contexts.</returns>
    public static ImmutableList<FluentConstructorContext> ChooseOverridingConstructors(IEnumerable<FluentConstructorContext> duplicateConstructors)
    {
        var emptyList = ImmutableList<FluentConstructorContext>.Empty;
        var (usedOnType, usedOnConstructor) = duplicateConstructors
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
