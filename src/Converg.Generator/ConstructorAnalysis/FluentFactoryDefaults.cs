using Microsoft.CodeAnalysis;

namespace Converg.Generator.ConstructorAnalysis;

/// <summary>
/// Holds factory-level default values for CreateMethod, CreateVerb, MethodPrefix, and ReturnType,
/// read from the [FluentFactory] attribute. Null means "not set at factory level".
/// </summary>
internal sealed class FluentFactoryDefaults(
    CreateMethodMode? createMethod,
    string? createVerb,
    string? methodPrefix,
    INamedTypeSymbol? returnType)
{
    public CreateMethodMode? CreateMethod { get; } = createMethod;
    public string? CreateVerb { get; } = createVerb;
    public string? MethodPrefix { get; } = methodPrefix;
    public INamedTypeSymbol? ReturnType { get; } = returnType;
}
