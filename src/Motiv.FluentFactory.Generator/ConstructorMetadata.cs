using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Motiv.FluentFactory.Generator.ConstructorAnalysis;

namespace Motiv.FluentFactory.Generator;

[DebuggerDisplay("{ToDisplayString()}")]
internal class ConstructorMetadata(FluentConstructorContext constructorContext)
{
    public IMethodSymbol Constructor { get; set; } = constructorContext.Constructor;

    public IList<IMethodSymbol> CandidateConstructors { get; } = [constructorContext.Constructor];

    public CreateMethodMode CreateMethod { get; set; } = constructorContext.CreateMethod;

    public INamedTypeSymbol? ReturnType { get; } = constructorContext.ReturnType;

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; } =
        constructorContext.ValueStorage;
        
    public FluentConstructorContext Context { get; } = constructorContext;

    public ConstructorMetadata Clone()
    {
        return new ConstructorMetadata(Context);
    }

    public string ToDisplayString() => Constructor.ToDisplayString();
}
