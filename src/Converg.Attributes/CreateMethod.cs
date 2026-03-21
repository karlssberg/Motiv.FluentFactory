namespace Converg.Attributes;

/// <summary>
/// Controls how the terminal Create method is generated for a fluent constructor chain.
/// </summary>
public enum CreateMethod
{
    /// <summary>
    /// Generates a Create method with the target type name appended (e.g., <c>CreateUser()</c>).
    /// This is the default behavior.
    /// </summary>
    Dynamic = 0,

    /// <summary>
    /// Generates a Create method using the verb as-is (e.g., <c>Create()</c>).
    /// </summary>
    Fixed = 1,

    /// <summary>
    /// Ensures that the <c>Create()</c> step is not generated for the current constructor. This means the constructor
    /// will be called immediately once all the parameters have been resolved. Because other constructors may
    /// extend this constructor's parameter sequence, the containing type must have the <c>partial</c> modifier applied.
    /// This is so that the generator can continue creating the fluent step methods beyond the present sequence.
    /// </summary>
    None = 2,
}
