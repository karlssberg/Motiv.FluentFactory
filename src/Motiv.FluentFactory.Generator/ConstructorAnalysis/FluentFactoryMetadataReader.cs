using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator.ConstructorAnalysis;

/// <summary>
/// Extracts fluent factory metadata from symbol attributes and converts
/// attribute arguments to generator option flags.
/// </summary>
internal static class FluentFactoryMetadataReader
{
    private const string CreateMethodKey = "CreateMethod";
    private const string CreateVerbKey = "CreateVerb";
    private const string MethodPrefixKey = "MethodPrefix";

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

                var (createMethod, createVerb, methodPrefix) = ReadNamedArguments(attribute.NamedArguments);

                return new FluentFactoryMetadata(typeSymbol)
                {
                    CreateMethod = createMethod,
                    RootTypeFullName = typeSymbol.ToDisplayString(),
                    CreateVerb = createVerb,
                    MethodPrefix = methodPrefix,
                    AttributeData = attribute,
                };
            });
    }

    /// <summary>
    /// Reads factory-level defaults from the [FluentFactory] attribute on the root type.
    /// </summary>
    /// <param name="rootType">The root type symbol that has the [FluentFactory] attribute.</param>
    /// <returns>Factory defaults with nullable values (null = not explicitly set).</returns>
    public static FluentFactoryDefaults GetFluentFactoryDefaults(INamedTypeSymbol rootType)
    {
        var attribute = rootType.GetAttributes(TypeName.FluentFactoryAttribute).FirstOrDefault();

        if (attribute is null)
            return new FluentFactoryDefaults(null, null, null);

        var (createMethod, createVerb, methodPrefix) = ReadNamedArguments(attribute.NamedArguments);

        return new FluentFactoryDefaults(createMethod, createVerb, methodPrefix);
    }

    /// <summary>
    /// Reads CreateMethod, CreateVerb, and MethodPrefix from an attribute's named arguments.
    /// Returns null for each value not explicitly present.
    /// </summary>
    /// <param name="namedArguments">The named arguments from an attribute.</param>
    /// <returns>A tuple of nullable CreateMethod, CreateVerb, and MethodPrefix values.</returns>
    private static (CreateMethodMode? CreateMethod, string? CreateVerb, string? MethodPrefix) ReadNamedArguments(
        ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
    {
        CreateMethodMode? createMethod = null;
        string? createVerb = null;
        string? methodPrefix = null;

        foreach (var arg in namedArguments)
        {
            switch (arg.Key)
            {
                case CreateMethodKey:
                    createMethod = ConvertToCreateMethodMode(arg.Value);
                    break;
                case CreateVerbKey:
                    createVerb = arg.Value.Value as string;
                    break;
                case MethodPrefixKey:
                    methodPrefix = arg.Value.Value as string;
                    break;
            }
        }

        return (createMethod, createVerb, methodPrefix);
    }

    /// <summary>
    /// Converts a typed constant from an attribute argument to CreateMethodMode.
    /// </summary>
    /// <param name="namedAttributeArgument">The typed constant representing the CreateMethod argument.</param>
    /// <returns>The parsed CreateMethodMode value.</returns>
    public static CreateMethodMode ConvertToCreateMethodMode(
        TypedConstant namedAttributeArgument)
    {
        if (namedAttributeArgument.Kind != TypedConstantKind.Enum)
            return CreateMethodMode.Dynamic;

        // Get the underlying int value
        var value = (int?)namedAttributeArgument.Value ?? 0;

        // Get the type symbol for the enum
        if (namedAttributeArgument.Type is not INamedTypeSymbol enumType)
            return CreateMethodMode.Dynamic;

        // Find the matching member by value
        var matchingMember = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.HasConstantValue && f.ConstantValue is int memberValue && memberValue == value);

        if (matchingMember is null)
            return CreateMethodMode.Dynamic;

        return Enum.TryParse<CreateMethodMode>(matchingMember.Name, true, out var mode)
            ? mode
            : CreateMethodMode.Dynamic;
    }
}
