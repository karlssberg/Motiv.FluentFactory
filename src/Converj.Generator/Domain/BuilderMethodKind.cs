namespace Converj.Generator.Domain;

/// <summary>
/// Internal mirror of the public Builder enum for generator-side processing.
/// </summary>
internal enum BuilderMethodKind
{
    DynamicSuffix = 0,
    FixedName = 1,
    None = 2,
    First = 3
}
