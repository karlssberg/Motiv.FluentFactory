using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Methods;

internal class RegularMethod : IFluentMethod
{
    private readonly Lazy<ImmutableArray<FluentTypeParameter>> _lazyTypeParameters;

    public RegularMethod(
        string name,
        IParameterSymbol sourceParameterSymbol,
        IFluentReturn fluentReturn,
        INamespaceSymbol rootNamespace,
        ImmutableArray<FluentMethodParameter> availableParameterFields,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages)
    {
        _lazyTypeParameters = new Lazy<ImmutableArray<FluentTypeParameter>>(GetTypeParameters);

        Name = name;
        SourceParameter = sourceParameterSymbol;
        MethodParameters = [FluentMethodParameter.FromParameter(sourceParameterSymbol, name)];
        RootNamespace = rootNamespace;
        ValueSources = valueStorages;
        AvailableParameterFields = availableParameterFields;
        Return = fluentReturn;
        DocumentationSummary = GetDocumentationSummary(sourceParameterSymbol);
        ParameterDocumentation = null;
    }

    /// <summary>
    /// Creates a RegularMethod for a property-backed fluent method parameter.
    /// </summary>
    public RegularMethod(
        string name,
        IPropertySymbol sourceProperty,
        IFluentReturn fluentReturn,
        INamespaceSymbol rootNamespace,
        ImmutableArray<FluentMethodParameter> availableParameterFields,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages)
    {
        _lazyTypeParameters = new Lazy<ImmutableArray<FluentTypeParameter>>(GetTypeParameters);

        Name = name;
        SourceParameter = null;
        SourceProperty = sourceProperty;
        MethodParameters = [FluentMethodParameter.FromProperty(sourceProperty, name)];
        RootNamespace = rootNamespace;
        ValueSources = valueStorages;
        AvailableParameterFields = availableParameterFields;
        Return = fluentReturn;
        DocumentationSummary = null;
        ParameterDocumentation = null;
    }

    public string Name { get; }

    public ImmutableArray<FluentMethodParameter> MethodParameters { get; }

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueSources { get; }

    public string? DocumentationSummary { get; }

    public Dictionary<string, string>? ParameterDocumentation { get; }

    public IParameterSymbol? SourceParameter { get; }

    /// <summary>
    /// The source property symbol when this method is property-backed.
    /// </summary>
    public IPropertySymbol? SourceProperty { get; }

    public ImmutableArray<FluentMethodParameter> AvailableParameterFields { get; }

    public IFluentReturn Return { get; }

    public ImmutableArray<FluentTypeParameter> TypeParameters => _lazyTypeParameters.Value;

    public INamespaceSymbol RootNamespace { get; }

    public override string ToString() => $"RegularMethod: {Name}({string.Join(", ", MethodParameters.Select(p => p.ParameterSymbol?.ToFullDisplayString() ?? $"{p.SourceType} {p.SourceName}"))})";

    private static string? GetDocumentationSummary(IParameterSymbol sourceParameterSymbol)
    {
        var parameterDoc = ExtractParameterDocumentation(sourceParameterSymbol);
        if (!string.IsNullOrWhiteSpace(parameterDoc))
        {
            return parameterDoc;
        }

        return ExtractSummaryFromDocumentation(sourceParameterSymbol.ContainingSymbol.GetDocumentationCommentXml());
    }

    private static string? ExtractParameterDocumentation(IParameterSymbol parameterSymbol)
    {
        var containingSymbolDoc = parameterSymbol.ContainingSymbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(containingSymbolDoc))
            return null;

        try
        {
            var xmlDoc = XDocument.Parse(containingSymbolDoc);
            var paramElement = xmlDoc.Descendants("param")
                .FirstOrDefault(p => p.Attribute("name")?.Value == parameterSymbol.Name);

            return paramElement?.Value.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractSummaryFromDocumentation(string? xmlDoc)
    {
        if (string.IsNullOrWhiteSpace(xmlDoc))
            return null;

        try
        {
            var doc = XDocument.Parse(xmlDoc);
            var summaryElement = doc.Descendants("summary").FirstOrDefault();
            return summaryElement?.Value.Trim();
        }
        catch
        {
            return xmlDoc;
        }
    }

    private ImmutableArray<FluentTypeParameter> GetTypeParameters()
    {
        var sourceType = SourceParameter?.Type ?? SourceProperty?.Type;
        return
        [
            ..sourceType?
                  .GetGenericTypeParameters()
                  .Select(genericTypeParameter => new FluentTypeParameter(genericTypeParameter))
              ?? []
        ];
    }
}
