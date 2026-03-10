using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator.Analysis;

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
            ..GetFluentFactoryMetadata(symbol)
                .Select(metadata =>
                {
                    var attributePresent = metadata.AttributePresent;
                    var rootTypeFullName = metadata.RootTypeFullName;
                    if (!attributePresent || string.IsNullOrWhiteSpace(rootTypeFullName))
                        return [];

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
    /// Extracts fluent factory metadata from a symbol's FluentConstructor attributes.
    /// </summary>
    /// <param name="symbol">The symbol to extract metadata from.</param>
    /// <returns>An enumerable of fluent factory metadata for each attribute found.</returns>
    public static IEnumerable<FluentFactoryMetadata> GetFluentFactoryMetadata(ISymbol symbol)
    {
        return symbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == TypeName.FluentConstructorAttribute)
            .Select(attribute =>
            {
                // ensure an attribute is present and has an argument
                if (attribute is null || attribute.ConstructorArguments.Length == 0)
                    return FluentFactoryMetadata.Invalid;

                var typeArg = attribute.ConstructorArguments.FirstOrDefault();
                if (typeArg.IsNull || typeArg.Value is not INamedTypeSymbol typeSymbol)
                    return FluentFactoryMetadata.Invalid;

                // Grab the options flags symbol
                var optionsArgument = attribute.NamedArguments
                    .FirstOrDefault(namedArg => namedArg.Key == "Options")
                    .Value;
                var options = ConvertToFluentFactoryGeneratorOptions(optionsArgument);

                // Grab the create method name
                var createMethodNameArgument = attribute.NamedArguments
                    .FirstOrDefault(namedArg => namedArg.Key == "CreateMethodName")
                    .Value;
                var createMethodName = createMethodNameArgument.Value as string;

                return new FluentFactoryMetadata(typeSymbol)
                {
                    Options = options,
                    RootTypeFullName = typeSymbol.ToDisplayString(),
                    CreateMethodName = createMethodName,
                    AttributeData = attribute,
                };
            });
    }

    /// <summary>
    /// Converts a typed constant from an attribute argument to FluentFactoryGeneratorOptions.
    /// </summary>
    /// <param name="namedAttributeArgument">The typed constant representing the options argument.</param>
    /// <returns>The parsed generator options flags.</returns>
    public static FluentFactoryGeneratorOptions ConvertToFluentFactoryGeneratorOptions(
        TypedConstant namedAttributeArgument)
    {
        if (namedAttributeArgument.Kind != TypedConstantKind.Enum)
            return FluentFactoryGeneratorOptions.None;

        // Get the underlying int value
        var value = (int?)namedAttributeArgument.Value ?? 0;

        // Get the type symbol for the enum
        if (namedAttributeArgument.Type is not INamedTypeSymbol enumType)
            return FluentFactoryGeneratorOptions.None;

        // Get all the declared members of the enum
        var flagMembers = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue && f.ConstantValue is int)
            .ToList();

        // Check which flags are set
        var setFlags = flagMembers
            .Where(member =>
            {
                var memberValue = (int?)member.ConstantValue ?? 0;
                return memberValue != 0 && (value & memberValue) == memberValue;
            })
            .ToList();

        if (setFlags.Count == 0)
            return FluentFactoryGeneratorOptions.None;

        return setFlags
            .Select(flag => Enum.TryParse<FluentFactoryGeneratorOptions>(flag.Name, true, out var option)
                ? option
                : FluentFactoryGeneratorOptions.None)
            .Aggregate((prev, next) => prev | next);
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
