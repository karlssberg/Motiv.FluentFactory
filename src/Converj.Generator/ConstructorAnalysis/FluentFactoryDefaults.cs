using Microsoft.CodeAnalysis;

namespace Converj.Generator.ConstructorAnalysis;

/// <summary>
/// Holds root-level default values for Builder, TerminalVerb, MethodPrefix, and ReturnType,
/// read from the [FluentRoot] attribute. Null means "not set at root level".
/// </summary>
internal sealed class FluentFactoryDefaults(
    BuilderMethodKind? builder,
    string? terminalVerb,
    string? methodPrefix,
    INamedTypeSymbol? returnType,
    bool allowPartialParameterOverlap = false,
    string? initialVerb = null)
{
    public BuilderMethodKind? Builder { get; } = builder;
    public string? TerminalVerb { get; } = terminalVerb;
    public string? MethodPrefix { get; } = methodPrefix;
    public INamedTypeSymbol? ReturnType { get; } = returnType;
    public bool AllowPartialParameterOverlap { get; } = allowPartialParameterOverlap;
    public string? InitialVerb { get; } = initialVerb;
}
