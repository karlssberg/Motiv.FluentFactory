using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator;

internal static class StringExtensions
{
    public static string ToFileName(this INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol
            .ToDisplayString()
            .Replace("<", "__")
            .Replace(">", "__")
            .Replace(',', '_');
    }
}
