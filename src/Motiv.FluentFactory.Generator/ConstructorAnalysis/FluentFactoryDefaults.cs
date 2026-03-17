namespace Motiv.FluentFactory.Generator.ConstructorAnalysis;

/// <summary>
/// Holds factory-level default values for CreateMethod and CreateVerb,
/// read from the [FluentFactory] attribute. Null means "not set at factory level".
/// </summary>
internal sealed class FluentFactoryDefaults(CreateMethodMode? createMethod, string? createVerb, string? methodPrefix)
{
    public CreateMethodMode? CreateMethod { get; } = createMethod;
    public string? CreateVerb { get; } = createVerb;
    public string? MethodPrefix { get; } = methodPrefix;
}
