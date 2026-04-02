namespace Converj.Attributes;

/// <summary>
/// Controls how the fluent builder chain is structured and how the terminal step is generated.
/// </summary>
public enum BuilderMethod
{
    /// <summary>
    /// Generates a terminal method with the target type name appended (e.g., <c>CreateUser()</c>).
    /// Consumers build parameters in sequence and discover available target types as they progress
    /// through the chain (e.g., <c>Factory.WithName("Rex").CreateDog()</c>).
    /// This is the default behavior.
    /// </summary>
    DynamicSuffix = 0,

    /// <summary>
    /// Generates a terminal method using the verb as-is (e.g., <c>Create()</c>).
    /// </summary>
    FixedName = 1,

    /// <summary>
    /// Ensures that the terminal step is not generated for the current target. This means the target
    /// will be invoked immediately once all the parameters have been resolved. Because other targets may
    /// extend this target's parameter sequence, the containing type must have the <c>partial</c> modifier applied.
    /// This is so that the generator can continue creating the fluent step methods beyond the present sequence.
    /// </summary>
    None = 2,

    /// <summary>
    /// Type-first mode. Consumers select the target type up front via an initial method,
    /// then fill in only that type's parameters
    /// (e.g., <c>Factory.BuildDog().WithName("Rex").Create()</c>).
    /// The initial method name is controlled by <see cref="FluentTargetAttribute.InitialVerb"/>
    /// or <see cref="FluentRootAttribute.InitialVerb"/>.
    /// </summary>
    Eager = 3,
}
