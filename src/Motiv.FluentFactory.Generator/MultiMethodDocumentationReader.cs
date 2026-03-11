using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator;

/// <summary>
/// Extracts XML documentation summaries and parameter documentation from
/// symbols for use in generated multi-method fluent builder methods.
/// </summary>
internal static class MultiMethodDocumentationReader
{
    /// <summary>
    /// Gets the documentation summary for a multi-method, checking the template method,
    /// the parameter-specific documentation, and the constructor documentation in that order.
    /// </summary>
    /// <param name="sourceParameterSymbol">The source parameter symbol.</param>
    /// <param name="parameterConverter">The template method symbol.</param>
    /// <returns>The documentation summary, or null if not available.</returns>
    internal static string? GetDocumentationSummary(IParameterSymbol sourceParameterSymbol, IMethodSymbol parameterConverter)
    {
        // First, try to get documentation from the template method (for MultipleFluentMethods)
        var templateMethodDoc = ExtractSummaryFromDocumentation(parameterConverter.GetDocumentationCommentXml());
        if (!string.IsNullOrWhiteSpace(templateMethodDoc))
        {
            return templateMethodDoc;
        }

        // Second, try to get parameter-specific documentation
        var parameterDoc = ExtractParameterDocumentation(sourceParameterSymbol);
        if (!string.IsNullOrWhiteSpace(parameterDoc))
        {
            return parameterDoc;
        }

        // Fallback to constructor documentation summary
        return ExtractSummaryFromDocumentation(sourceParameterSymbol.ContainingSymbol.GetDocumentationCommentXml());
    }

    /// <summary>
    /// Gets parameter documentation from a template method's XML documentation.
    /// </summary>
    /// <param name="templateMethod">The template method to extract parameter documentation from.</param>
    /// <returns>A dictionary mapping parameter names to descriptions, or null if not available.</returns>
    internal static Dictionary<string, string>? GetParameterDocumentation(IMethodSymbol templateMethod)
    {
        var templateMethodDoc = templateMethod.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(templateMethodDoc))
            return null;

        try
        {
            // Parse XML to extract parameter documentation
            var doc = XDocument.Parse(templateMethodDoc);
            var paramElements = doc.Descendants("param");

            var paramDocs = new Dictionary<string, string>();
            foreach (var paramElement in paramElements)
            {
                var name = paramElement.Attribute("name")?.Value;
                var description = paramElement.Value.Trim();

                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(description))
                {
                    paramDocs[name!] = description;
                }
            }

            return paramDocs.Count > 0 ? paramDocs : null;
        }
        catch
        {
            // If XML parsing fails, return null
            return null;
        }
    }

    private static string? ExtractParameterDocumentation(IParameterSymbol parameterSymbol)
    {
        // Get the XML documentation from the containing method/constructor
        var containingSymbolDoc = parameterSymbol.ContainingSymbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(containingSymbolDoc))
            return null;

        try
        {
            // Parse XML to extract parameter-specific documentation
            var xmlDoc = XDocument.Parse(containingSymbolDoc);
            var paramElement = xmlDoc.Descendants("param")
                .FirstOrDefault(p => p.Attribute("name")?.Value == parameterSymbol.Name);

            return paramElement?.Value.Trim();
        }
        catch
        {
            // If XML parsing fails, return null to fall back to constructor documentation
            return null;
        }
    }

    private static string? ExtractSummaryFromDocumentation(string? xmlDoc)
    {
        if (string.IsNullOrWhiteSpace(xmlDoc))
            return null;

        try
        {
            // Parse XML to extract summary documentation
            var doc = XDocument.Parse(xmlDoc);
            var summaryElement = doc.Descendants("summary").FirstOrDefault();
            return summaryElement?.Value.Trim();
        }
        catch
        {
            // If XML parsing fails, return the raw documentation
            return xmlDoc;
        }
    }
}
