using Microsoft.CodeAnalysis;
using Motiv.FluentFactory.Generator.Model.Methods;
using Motiv.FluentFactory.Generator.Model.Steps;

namespace Motiv.FluentFactory.Generator.Diagnostics;

internal class UnreachableConstructorAnalyzer
{
    private readonly List<IMethodSymbol> _allFluentConstructors = [];
    private readonly HashSet<IMethodSymbol> _reachedFluentConstructors = [];

    public void AddReachableMethod(IFluentMethod method)
    {
        switch (method.Return)
        {
            case TargetTypeReturn targetTypeReturn:
                _reachedFluentConstructors.Add(targetTypeReturn.Constructor);
                break;
            case ExistingTypeFluentStep existingTypeFluentStep:
                _reachedFluentConstructors.Add(existingTypeFluentStep.ConstructorContext.Constructor);
                break;
        }
    }

    public void AddAllFluentConstructors(IEnumerable<IMethodSymbol> fluentConstructors)
    {
        _allFluentConstructors.AddRange(fluentConstructors);
    }

    public void Clear()
    {
        _reachedFluentConstructors.Clear();
        _allFluentConstructors.Clear();
    }

    public IEnumerable<Diagnostic> GetUnreachableConstructorsDiagnostics()
    {
        return GetUnreachableConstructors()
            .Select(constructor =>
                Diagnostic.Create(
                    FluentFactoryGenerator.UnreachableConstructor,
                    constructor.Locations.FirstOrDefault(),
                    constructor.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private IEnumerable<IMethodSymbol> GetUnreachableConstructors()
    {
        return _allFluentConstructors
            .Where(constructor => !_reachedFluentConstructors.Contains(constructor));
    }
}
