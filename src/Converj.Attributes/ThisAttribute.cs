using System.Diagnostics.CodeAnalysis;

namespace Converj.Attributes;

/// <summary>
/// Marks the first parameter of a constructor or static method as the extension receiver.
/// The generated fluent chain will start as an extension method on this parameter's type.
/// On actual C# extension methods (with the <c>this</c> modifier), this attribute is redundant.
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter)]
public class ThisAttribute : Attribute;
