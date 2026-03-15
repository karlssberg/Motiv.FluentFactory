using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator.ConstructorAnalysis;

/// <summary>
/// Extracts fluent factory metadata from symbol attributes and converts
/// attribute arguments to generator option flags.
/// </summary>
internal static class FluentFactoryMetadataReader
{
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

                // Grab the CreateMethod enum value
                var createMethodArgument = attribute.NamedArguments
                    .FirstOrDefault(namedArg => namedArg.Key == "CreateMethod")
                    .Value;
                var createMethod = ConvertToCreateMethodMode(createMethodArgument);

                // Grab the create verb
                var createVerbArgument = attribute.NamedArguments
                    .FirstOrDefault(namedArg => namedArg.Key == "CreateVerb")
                    .Value;
                var createVerb = createVerbArgument.Value as string;

                return new FluentFactoryMetadata(typeSymbol)
                {
                    CreateMethod = createMethod,
                    RootTypeFullName = typeSymbol.ToDisplayString(),
                    CreateVerb = createVerb,
                    AttributeData = attribute,
                };
            });
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
