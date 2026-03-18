using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator;

/// <summary>
/// A fluent method that creates an all-optional step using named arguments,
/// enabling users to start building from any optional parameter.
/// </summary>
internal class OptionalGatewayMethod : IFluentMethod
{
    public OptionalGatewayMethod(
        string name,
        IParameterSymbol sourceParameter,
        IFluentReturn fluentReturn,
        INamespaceSymbol rootNamespace,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages)
    {
        Name = name;
        SourceParameter = sourceParameter;
        MethodParameters = [new FluentMethodParameter(sourceParameter, name)];
        RootNamespace = rootNamespace;
        ValueSources = valueStorages;
        AvailableParameterFields = [];
        Return = fluentReturn;
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
