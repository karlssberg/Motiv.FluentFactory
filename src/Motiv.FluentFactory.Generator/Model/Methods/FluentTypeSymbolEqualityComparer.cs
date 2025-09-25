using Microsoft.CodeAnalysis;
using Motiv.FluentFactory.Generator.Generation;

namespace Motiv.FluentFactory.Generator.Model.Methods;

internal class FluentTypeSymbolEqualityComparer : IEqualityComparer<ITypeSymbol> {

    public static FluentTypeSymbolEqualityComparer Default { get; } = new();
    public bool Equals(ITypeSymbol? x, ITypeSymbol? y)
    {
        return (x, y) switch
        {
            (null, null) => true,
            (null, _) => false,
            (_, null) => false,
            _ when x.IsOpenGenericType() && y.IsOpenGenericType() =>  x.Name == y.Name,
            _ => SymbolEqualityComparer.Default.Equals(x, y)
        };
    }

    public int GetHashCode(ITypeSymbol obj)
    {
        return obj.IsOpenGenericType()
            ? obj.Name.GetHashCode()
            : SymbolEqualityComparer.Default.GetHashCode(obj);
    }
}
