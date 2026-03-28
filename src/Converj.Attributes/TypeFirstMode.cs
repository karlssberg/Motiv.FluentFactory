namespace Converj.Attributes;

/// <summary>
/// Controls how the fluent builder chain is structured for consumers.
/// </summary>
public enum BuilderMode
{
    /// <summary>
    /// The default parameter-first mode. Consumers build parameters in sequence
    /// and discover available target types as they progress through the chain
    /// (e.g., <c>Factory.WithName("Rex").CreateDog()</c>).
    /// </summary>
    ParameterFirst = 0,

    /// <summary>
    /// Type-first mode. Consumers select the target type up front, then fill in
    /// only that type's parameters
    /// (e.g., <c>Factory.BuildDog().WithName("Rex").Create()</c>).
    /// </summary>
    TypeFirst = 1,
}
