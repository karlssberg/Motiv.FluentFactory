using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converg.Generator;

/// <summary>
/// A fluent method for an optional constructor parameter that returns the same step type,
/// allowing optional parameters to be set in any order before calling Create.
/// </summary>
internal class OptionalFluentMethod : IFluentMethod
{
    public OptionalFluentMethod(
        string name,
        IParameterSymbol sourceParameter,
        IFluentStep containingStep,
        INamespaceSymbol rootNamespace,
        ImmutableArray<FluentMethodParameter> availableParameterFields,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages)
    {
        Name = name;
        SourceParameter = sourceParameter;
        MethodParameters = [new FluentMethodParameter(sourceParameter, name)];
        RootNamespace = rootNamespace;
        ValueSources = valueStorages;
        AvailableParameterFields = availableParameterFields;
        Return = containingStep;
    }

    public string Name { get; }

    public IParameterSymbol SourceParameter { get; }

    public ImmutableArray<FluentMethodParameter> AvailableParameterFields { get; }

    public IFluentReturn Return { get; }

    public ImmutableArray<FluentTypeParameter> TypeParameters { get; } = [];

    public INamespaceSymbol RootNamespace { get; }

    public ImmutableArray<FluentMethodParameter> MethodParameters { get; }

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueSources { get; }

    public string? DocumentationSummary => null;

    public Dictionary<string, string>? ParameterDocumentation => null;
}
