using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.Generation.SyntaxElements.Methods;

internal static class FluentMethodSummaryDocXml
{
    private static readonly SymbolDisplayFormat FullyQualifiedWithoutGlobal = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// Creates XML documentation trivia with a summary block from the given lines of text.
    /// </summary>
    public static SyntaxTriviaList Create(
        IEnumerable<object?> linesOfText)
    {
        IEnumerable<SyntaxTrivia> triviaList =
        [
            Comment($"/// <summary>"),
            CarriageReturnLineFeed,
            ..linesOfText.SelectMany(ConvertLine),
            Comment($"/// </summary>"),
            CarriageReturnLineFeed
        ];

        return TriviaList(triviaList);
    }

    /// <summary>
    /// Creates XML documentation trivia with a summary block and parameter documentation.
    /// </summary>
    public static SyntaxTriviaList CreateWithParameters(
        IEnumerable<object?> linesOfText,
        Dictionary<string, string>? parameterDocumentation,
        IEnumerable<string> parameterNames)
    {
        var triviaList = new List<SyntaxTrivia>
        {
            Comment($"/// <summary>"),
            CarriageReturnLineFeed
        };

        triviaList.AddRange(linesOfText.SelectMany(ConvertLine));

        triviaList.Add(Comment($"/// </summary>"));
        triviaList.Add(CarriageReturnLineFeed);

        // Add parameter documentation if available
        if (parameterDocumentation != null)
        {
            foreach (var paramName in parameterNames)
            {
                if (parameterDocumentation.TryGetValue(paramName, out var paramDoc))
                {
                    triviaList.Add(Comment($"/// <param name=\"{paramName}\">{paramDoc}</param>"));
                    triviaList.Add(CarriageReturnLineFeed);
                }
            }
        }

        return TriviaList(triviaList);
    }

    /// <summary>
    /// Generates seealso XML documentation links for the containing types of candidate constructors.
    /// </summary>
    public static IEnumerable<SyntaxTrivia> GenerateCandidateConstructorTypeSeeAlsoLinks(
        IEnumerable<IMethodSymbol> candidateConstructors)
    {
        return candidateConstructors
            .Select(ctor => ctor.ContainingType)
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .OrderBy(type => type.Name)
            .Select(CreateSeeAlsoLink);
    }

    private static SyntaxTrivia CreateSeeAlsoLink(INamedTypeSymbol typeSymbol)
    {
        var crefValue = GetCrefAttributeValue(typeSymbol);
        var commentText = $"///     <seealso cref=\"{crefValue}\"/>";
        return Comment(commentText);
    }

    /// <summary>
    /// Gets the formatted cref attribute value from a type symbol.
    /// </summary>
    private static string GetCrefAttributeValue(INamedTypeSymbol typeSymbol)
    {
        if (!typeSymbol.IsGenericType)
        {
            return typeSymbol.ToDisplayString(FullyQualifiedWithoutGlobal);
        }

        var baseTypeName = typeSymbol.ToDisplayString(
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.None));

        var typeArgs = string.Join(", ", typeSymbol.TypeArguments.Select(t =>
            t.ToDisplayString(FullyQualifiedWithoutGlobal)));

        return $"{baseTypeName}{{{typeArgs}}}";
    }

    private static IEnumerable<SyntaxTrivia> ConvertLine(object? line)
    {
        return line switch
        {
            null => [],
            "" => [Comment("///")],
            SyntaxTrivia trivia => [trivia],
            _ when string.IsNullOrWhiteSpace(line.ToString()) => [Comment("///")],
            _ => ConvertLineEndings(line.ToString())
        };
    }

    private static IEnumerable<SyntaxTrivia> ConvertLineEndings(string? line)
    {
        if (line is null) return [];

        return line
            .Split(["\r\n", "\n", "\r"], default)
            .SelectMany(IEnumerable<SyntaxTrivia> (embeddedLines) =>
                embeddedLines switch
                {
                    null => [],
                    "" => [Comment("///")],
                    _ => [Comment($"/// {embeddedLines}")]
                });
    }
}
