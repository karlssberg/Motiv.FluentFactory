using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.ConstructorAnalysis;

/// <summary>
/// Extracts fluent factory metadata from symbol attributes and converts
/// attribute arguments to generator option flags.
/// </summary>
internal static class FluentFactoryMetadataReader
{
    private const string BuilderKey = "BuilderMethod";
    private const string TerminalVerbKey = "TerminalVerb";
    private const string MethodPrefixKey = "MethodPrefix";
    private const string ReturnTypeKey = "ReturnType";
    private const string AllowPartialParameterOverlapKey = "AllowPartialParameterOverlap";
    private const string InitialVerbKey = "InitialVerb";
    private const string EagerVerbKey = "EagerVerb";

    /// <summary>
    /// Extracts fluent factory metadata from a symbol's FluentTarget attributes.
    /// </summary>
    /// <param name="symbol">The symbol to extract metadata from.</param>
    /// <returns>An enumerable of fluent factory metadata for each attribute found.</returns>
    public static IEnumerable<FluentFactoryMetadata> GetFluentFactoryMetadata(ISymbol symbol)
    {
        return symbol.GetAttributes()
            .Where(IsFluentTargetAttribute)
            .Select(attribute =>
            {
                var typeSymbol = ExtractRootTypeSymbol(attribute);
                if (typeSymbol is null)
                    return FluentFactoryMetadata.Invalid;

                var args = ReadNamedArguments(attribute.NamedArguments);

                return new FluentFactoryMetadata(typeSymbol)
                {
                    Builder = args.Builder,
                    RootTypeFullName = typeSymbol.ToDisplayString(),
                    TerminalVerb = args.TerminalVerb,
                    MethodPrefix = args.MethodPrefix,
                    ReturnType = args.ReturnType,
                    AttributeData = attribute,
                    InitialVerb = args.InitialVerb,
                };
            });
    }

    /// <summary>
    /// Determines whether an attribute is a FluentTarget attribute (generic or non-generic).
    /// </summary>
    /// <param name="attribute">The attribute data to check.</param>
    /// <returns><c>true</c> if the attribute is a FluentTarget attribute; otherwise, <c>false</c>.</returns>
    private static bool IsFluentTargetAttribute(AttributeData attribute)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass is null)
            return false;

        if (attributeClass.ToDisplayString() == TypeName.FluentTargetAttribute)
            return true;

        // Check for generic variant: FluentTargetAttribute<T>
        return attributeClass is { IsGenericType: true }
               && attributeClass.OriginalDefinition.MetadataName == "FluentTargetAttribute`1"
               && attributeClass.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "Converj.Attributes";
    }

    /// <summary>
    /// Extracts the root type symbol from a FluentTarget attribute.
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
    /// Reads root-level defaults from the [FluentRoot] attribute on the root type.
    /// </summary>
    /// <param name="rootType">The root type symbol that has the [FluentRoot] attribute.</param>
    /// <returns>Root defaults with nullable values (null = not explicitly set).</returns>
    public static FluentFactoryDefaults GetFluentFactoryDefaults(INamedTypeSymbol rootType)
    {
        var attribute = rootType.GetAttributes(TypeName.FluentRootAttribute).FirstOrDefault();

        if (attribute is null)
            return new FluentFactoryDefaults(null, null, null, null);

        var args = ReadNamedArguments(attribute.NamedArguments);

        return new FluentFactoryDefaults(
            args.Builder, args.TerminalVerb, args.MethodPrefix, args.ReturnType, args.AllowPartialParameterOverlap,
            args.InitialVerb);
    }

    /// <summary>
    /// Reads Builder, TerminalVerb, and MethodPrefix from an attribute's named arguments.
    /// Returns null for each value not explicitly present.
    /// </summary>
    /// <param name="namedArguments">The named arguments from an attribute.</param>
    /// <returns>A tuple of nullable Builder, TerminalVerb, and MethodPrefix values.</returns>
    private static ParsedNamedArguments ReadNamedArguments(
        ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
    {
        var result = new ParsedNamedArguments();

        foreach (var arg in namedArguments)
        {
            switch (arg.Key)
            {
                case BuilderKey:
                    result.Builder = ConvertToBuilderMethodKind(arg.Value);
                    break;
                case TerminalVerbKey:
                    result.TerminalVerb = arg.Value.Value as string;
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
                case InitialVerbKey:
                case EagerVerbKey:
                    result.InitialVerb = arg.Value.Value as string;
                    break;
            }
        }

        return result;
    }

    private sealed class ParsedNamedArguments
    {
        public BuilderMethodKind? Builder { get; set; }
        public string? TerminalVerb { get; set; }
        public string? MethodPrefix { get; set; }
        public INamedTypeSymbol? ReturnType { get; set; }
        public bool AllowPartialParameterOverlap { get; set; }
        public string? InitialVerb { get; set; }
    }

    /// <summary>
    /// Converts a typed constant from an attribute argument to BuilderMethodKind.
    /// </summary>
    /// <param name="namedAttributeArgument">The typed constant representing the Builder argument.</param>
    /// <returns>The parsed BuilderMethodKind value.</returns>
    public static BuilderMethodKind ConvertToBuilderMethodKind(
        TypedConstant namedAttributeArgument)
    {
        if (namedAttributeArgument.Kind != TypedConstantKind.Enum)
            return BuilderMethodKind.DynamicSuffix;

        // Get the underlying int value
        var value = (int?)namedAttributeArgument.Value ?? 0;

        // Get the type symbol for the enum
        if (namedAttributeArgument.Type is not INamedTypeSymbol enumType)
            return BuilderMethodKind.DynamicSuffix;

        // Find the matching member by value
        var matchingMember = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.HasConstantValue && f.ConstantValue is int memberValue && memberValue == value);

        if (matchingMember is null)
            return BuilderMethodKind.DynamicSuffix;

        return Enum.TryParse<BuilderMethodKind>(matchingMember.Name, true, out var mode)
            ? mode
            : BuilderMethodKind.DynamicSuffix;
    }
}
