namespace Converj.Generator.Domain;

/// <summary>
/// Internal mirror of the public TerminalMethod enum for generator-side processing.
/// </summary>
internal enum TerminalMethodKind
{
    DynamicSuffix = 0,
    FixedName = 1,
    None = 2,
}
