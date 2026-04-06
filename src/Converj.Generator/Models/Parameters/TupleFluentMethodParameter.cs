using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Parameters;

/// <summary>
/// A fluent method parameter backed by a named value tuple, where the tuple elements
/// are unpacked into individual method parameters in the generated fluent API.
/// </summary>
internal class TupleFluentMethodParameter : FluentMethodParameter
{
    private TupleFluentMethodParameter(
        IParameterSymbol parameterSymbol,
        ImmutableArray<string> names,
        ImmutableArray<(string Name, ITypeSymbol Type)> elements)
        : base(parameterSymbol, null, parameterSymbol.Name, parameterSymbol.Type, names)
    {
        Elements = elements;
    }

    /// <summary>
    /// The named tuple elements that will be unpacked into individual method parameters.
    /// </summary>
    public ImmutableArray<(string Name, ITypeSymbol Type)> Elements { get; }

    /// <summary>
    /// Creates a tuple-backed fluent method parameter from a named tuple constructor parameter.
    /// </summary>
    public static TupleFluentMethodParameter FromTupleParameter(
        IParameterSymbol parameterSymbol,
        ImmutableArray<string> names,
        ImmutableArray<(string Name, ITypeSymbol Type)> elements) =>
        new(parameterSymbol, names, elements);
}
