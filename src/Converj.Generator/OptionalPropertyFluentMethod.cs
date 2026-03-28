using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator;

/// <summary>
/// A fluent method for an optional [FluentMethod] property that returns the same step type,
/// allowing the property to be optionally set before calling Create.
/// </summary>
internal class OptionalPropertyFluentMethod : IFluentMethod
{
    public OptionalPropertyFluentMethod(
        string name,
        IPropertySymbol sourceProperty,
        FieldStorage fieldStorage,
        IFluentStep containingStep,
        INamespaceSymbol rootNamespace)
    {
        Name = name;
        SourceProperty = sourceProperty;
        FieldStorage = fieldStorage;
        RootNamespace = rootNamespace;
        Return = containingStep;
        MethodParameters = [];
        AvailableParameterFields = [];
        ValueSources = [];
    }

    public string Name { get; }

    public IPropertySymbol SourceProperty { get; }

    public FieldStorage FieldStorage { get; }

    public ImmutableArray<FluentMethodParameter> AvailableParameterFields { get; }

    public IFluentReturn Return { get; }

    public ImmutableArray<FluentTypeParameter> TypeParameters { get; } = [];

    public INamespaceSymbol RootNamespace { get; }

    public ImmutableArray<FluentMethodParameter> MethodParameters { get; }

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueSources { get; }

    public string? DocumentationSummary => null;

    public Dictionary<string, string>? ParameterDocumentation => null;

    public IParameterSymbol? SourceParameter => null;
}
