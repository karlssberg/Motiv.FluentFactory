using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.ConstructorAnalysis;

/// <summary>
/// Extracts fluent factory metadata from symbol attributes and converts
/// attribute arguments to generator option flags.
/// </summary>
internal static class FluentFactoryMetadataReader
{
    private const string CreateMethodKey = "CreateMethod";
    private const string CreateVerbKey = "CreateVerb";
    private const string MethodPrefixKey = "MethodPrefix";
    private const string ReturnTypeKey = "ReturnType";
    private const string AllowPartialParameterOverlapKey = "AllowPartialParameterOverlap";
    private const string BuilderModeKey = "BuilderMode";
    private const string TypeFirstVerbKey = "TypeFirstVerb";

    /// <summary>
    /// Extracts fluent factory metadata from a symbol's FluentConstructor attributes.
    /// </summary>
    /// <param name="symbol">The symbol to extract metadata from.</param>
    /// <returns>An enumerable of fluent factory metadata for each attribute found.</returns>
    public static IEnumerable<FluentFactoryMetadata> GetFluentFactoryMetadata(ISymbol symbol)
    {
        return symbol.GetAttributes()
            .Where(IsFluentConstructorAttribute)
            .Select(attribute =>
            {
                var typeSymbol = ExtractRootTypeSymbol(attribute);
                if (typeSymbol is null)
                    return FluentFactoryMetadata.Invalid;

                var args = ReadNamedArguments(attribute.NamedArguments);

                return new FluentFactoryMetadata(typeSymbol)
                {
                    CreateMethod = args.CreateMethod,
                    RootTypeFullName = typeSymbol.ToDisplayString(),
                    CreateVerb = args.CreateVerb,
                    MethodPrefix = args.MethodPrefix,
                    ReturnType = args.ReturnType,
                    AttributeData = attribute,
                    BuilderMode = args.BuilderMode,
                    TypeFirstVerb = args.TypeFirstVerb,
                };
            });
    }

    /// <summary>
    /// Determines whether an attribute is a FluentConstructor attribute (generic or non-generic).
    /// </summary>
    /// <param name="attribute">The attribute data to check.</param>
    /// <returns><c>true</c> if the attribute is a FluentConstructor attribute; otherwise, <c>false</c>.</returns>
    private static bool IsFluentConstructorAttribute(AttributeData attribute)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass is null)
            return false;

        if (attributeClass.ToDisplayString() == TypeName.FluentConstructorAttribute)
            return true;

        // Check for generic variant: FluentConstructorAttribute<T>
        return attributeClass is { IsGenericType: true }
               && attributeClass.OriginalDefinition.MetadataName == "FluentConstructorAttribute`1"
               && attributeClass.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "Converj.Attributes";
    }

    /// <summary>
    /// Extracts the root type symbol from a FluentConstructor attribute.
    /// For the non-generic attribute, reads from constructor arguments.
    /// For the generic attribute, reads from the type argument.
    /// </summary>
    /// <param name="attribute">The attribute data to extract from.</param>
    /// <returns>The root type symbol, or null if extraction fails.</returns>
    private static INamedTypeSymbol? ExtractRootTypeSymbol(AttributeData attribute)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass is null)
            return null;

        // Generic variant: extract from type argument
        if (attributeClass is { IsGenericType: true } && attributeClass.TypeArguments.Length > 0)
            return attributeClass.TypeArguments[0] as INamedTypeSymbol;

        // Non-generic variant: extract from constructor arguments
        if (attribute.ConstructorArguments.Length == 0)
            return null;

        var typeArg = attribute.ConstructorArguments.FirstOrDefault();
        return typeArg is { IsNull: false, Value: INamedTypeSymbol typeSymbol } ? typeSymbol : null;
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
            return new FluentFactoryDefaults(null, null, null, null);

        var args = ReadNamedArguments(attribute.NamedArguments);

        return new FluentFactoryDefaults(
            args.CreateMethod, args.CreateVerb, args.MethodPrefix, args.ReturnType, args.AllowPartialParameterOverlap,
            args.BuilderMode ?? BuilderMode.ParameterFirst, args.TypeFirstVerb);
    }

    /// <summary>
    /// Reads CreateMethod, CreateVerb, and MethodPrefix from an attribute's named arguments.
    /// Returns null for each value not explicitly present.
    /// </summary>
    /// <param name="namedArguments">The named arguments from an attribute.</param>
    /// <returns>A tuple of nullable CreateMethod, CreateVerb, and MethodPrefix values.</returns>
    private static ParsedNamedArguments ReadNamedArguments(
        ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
    {
        var result = new ParsedNamedArguments();

        foreach (var arg in namedArguments)
        {
            switch (arg.Key)
            {
                case CreateMethodKey:
                    result.CreateMethod = ConvertToCreateMethodMode(arg.Value);
                    break;
                case CreateVerbKey:
                    result.CreateVerb = arg.Value.Value as string;
                    break;
                case MethodPrefixKey:
                    result.MethodPrefix = arg.Value.Value as string;
                    break;
                case ReturnTypeKey:
                    result.ReturnType = arg.Value.Value as INamedTypeSymbol;
                    break;
                case AllowPartialParameterOverlapKey:
                    result.AllowPartialParameterOverlap = arg.Value.Value is true;
                    break;
                case BuilderModeKey:
                    result.BuilderMode = ConvertToBuilderMode(arg.Value);
                    break;
                case TypeFirstVerbKey:
                    result.TypeFirstVerb = arg.Value.Value as string;
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Converts a typed constant from an attribute argument to the internal BuilderMode enum.
    /// </summary>
    private static BuilderMode? ConvertToBuilderMode(TypedConstant value)
    {
        if (value.Kind != TypedConstantKind.Enum)
            return null;

        return value.Value is int intValue
            ? Enum.TryParse<BuilderMode>(intValue.ToString(), out var mode) ? mode : null
            : null;
    }

    private sealed class ParsedNamedArguments
    {
        public CreateMethodMode? CreateMethod { get; set; }
        public string? CreateVerb { get; set; }
        public string? MethodPrefix { get; set; }
        public INamedTypeSymbol? ReturnType { get; set; }
        public bool AllowPartialParameterOverlap { get; set; }
        public BuilderMode? BuilderMode { get; set; }
        public string? TypeFirstVerb { get; set; }
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
