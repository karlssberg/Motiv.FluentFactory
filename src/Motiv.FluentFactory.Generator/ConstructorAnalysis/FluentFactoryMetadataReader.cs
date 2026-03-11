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
}
