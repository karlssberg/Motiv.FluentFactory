using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Methods;

internal class CreationMethod : IFluentMethod
{
    private readonly Lazy<ImmutableArray<FluentTypeParameter>> _lazyTypeParameters;


    public CreationMethod(
        INamespaceSymbol rootNamespace,
        ConstructorMetadata constructorMetadata,
        ImmutableArray<FluentMethodParameter> availableParameterFields,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueSources,
        string? createMethodName = null)
    {
        _lazyTypeParameters = new Lazy<ImmutableArray<FluentTypeParameter>>(GetFluentTypeParameter);

        RootNamespace = rootNamespace;
        AvailableParameterFields = availableParameterFields;
        ValueSources = valueSources;
        IsStaticMethodTarget = constructorMetadata.IsStaticMethodTarget;
        ReceiverParameter = constructorMetadata.ReceiverParameter;
        Name = createMethodName ?? "Create";
        Return = new TargetTypeReturn(
            constructorMetadata.Constructor,
            [..constructorMetadata.CandidateConstructors],
            new ParameterSequence(availableParameterFields),
            constructorMetadata.ReturnType,
            IsStaticMethodTarget ? constructorMetadata.Constructor.ReturnType as INamedTypeSymbol : null);
    }

    /// <summary>
    /// Whether this creation method targets a static method instead of a constructor.
    /// </summary>
    public bool IsStaticMethodTarget { get; }

    /// <summary>
    /// The extension receiver parameter, if this target uses extension method syntax.
    /// </summary>
    public IParameterSymbol? ReceiverParameter { get; }

    public string Name { get; }

    public ImmutableArray<FluentMethodParameter> MethodParameters { get; } = [];

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueSources { get; }

    public string? DocumentationSummary
    {
        get
        {
            var constructorNames = Return.CandidateConstructors
                .Select(ctor => ctor.ToFullDisplayString().Replace("<", "&lt;").Replace(">", "&gt;"));

            if (IsStaticMethodTarget)
            {
                return Return.CandidateConstructors switch
                {
                    { Length: 1 } =>
                        $"""
                         Calls static method {constructorNames.First()}.

                         """,
                    _ => null
                };
            }

            return Return.CandidateConstructors switch
            {
                { Length: 1 } =>
                    $"""
                     Creates a new instance using constructor {constructorNames.First()}.

                     """,
                { Length: > 1 } =>
                    $"""
                     Creates a new instance using constructors:
                       {string.Join("\n  ", constructorNames)}.

                     """,
                _ => null
            };
        }
    }

    public Dictionary<string, string>? ParameterDocumentation => null; // Creation methods don't use template methods

    public IParameterSymbol? SourceParameter => null;

    public ImmutableArray<FluentMethodParameter> AvailableParameterFields { get; }

    /// <summary>
    /// Property initializer assignments for the object initializer in the creation expression.
    /// Each entry maps a target property name to the field name on the step struct.
    /// </summary>
    public ImmutableArray<(string PropertyName, string FieldName)> PropertyInitializers { get; set; } = [];

    public IFluentReturn Return { get; }

    public ImmutableArray<FluentTypeParameter> TypeParameters => _lazyTypeParameters.Value;

    public INamespaceSymbol RootNamespace { get; }

    private ImmutableArray<FluentTypeParameter> GetFluentTypeParameter()
    {
        return
        [
            ..SourceParameter?.Type
                  .GetGenericTypeParameters()
                  .Select(genericTypeParameter => new FluentTypeParameter(genericTypeParameter))
              ?? []
        ];
    }
}
