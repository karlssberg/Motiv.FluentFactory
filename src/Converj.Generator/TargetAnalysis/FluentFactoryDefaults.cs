using Microsoft.CodeAnalysis;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Holds root-level default values for TerminalMethod, TerminalVerb, MethodPrefix, and ReturnType,
/// read from the [FluentRoot] attribute. Null means "not set at root level".
/// </summary>
internal sealed class FluentFactoryDefaults(
    TerminalMethodKind? terminalMethod,
    string? terminalVerb,
    string? methodPrefix,
    INamedTypeSymbol? returnType,
    bool allowPartialParameterOverlap = false)
{
    public TerminalMethodKind? TerminalMethod { get; } = terminalMethod;
    public string? TerminalVerb { get; } = terminalVerb;
    public string? MethodPrefix { get; } = methodPrefix;
    public INamedTypeSymbol? ReturnType { get; } = returnType;
    public bool AllowPartialParameterOverlap { get; } = allowPartialParameterOverlap;
}
