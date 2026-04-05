using Microsoft.CodeAnalysis;
using Converj.Generator.Diagnostics;

namespace Converj.Generator.ConstructorAnalysis;

/// <summary>
/// Analyzes a target type for fields and properties marked with [FluentStorage],
/// which provide explicit storage for constructor parameters that cannot be auto-discovered.
/// Used when a type has multiple constructors with TerminalMethod.None and acts as an
/// intermediate step in the fluent chain.
/// </summary>
internal static class FluentStorageAnalyzer
{
    /// <summary>
    /// Scans the target type for members marked with [FluentStorage] and returns
    /// a dictionary mapping parameter names to their storage locations.
    /// Reports diagnostics for invalid usages (e.g., properties without getters, duplicate mappings).
    /// </summary>
    /// <param name="targetType">The target type to analyze for [FluentStorage] members.</param>
    /// <param name="diagnostics">Diagnostic list to report issues to.</param>
    /// <returns>A dictionary mapping parameter names (case-insensitive) to their storage.</returns>
    public static Dictionary<string, IFluentValueStorage> Analyze(
        INamedTypeSymbol targetType,
        DiagnosticList diagnostics)
    {
        var storageMap = new Dictionary<string, IFluentValueStorage>(StringComparer.OrdinalIgnoreCase);
        var seenNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var definitionType = targetType.OriginalDefinition;

        foreach (var member in definitionType.GetMembers())
        {
            var attribute = member.GetAttributes(TypeName.FluentStorageAttribute).FirstOrDefault();
            if (attribute is null) continue;

            var parameterName = attribute.GetFirstStringArgument() ?? member.Name.StripLeadingUnderscores();
            var location = member.Locations.FirstOrDefault() ?? Location.None;

            switch (member)
            {
                case IFieldSymbol field:
                    if (!TryAddStorage(parameterName, field.Name, location, seenNames, diagnostics))
                        continue;

                    storageMap[parameterName] = new FieldStorage(
                        field.Name, field.Type, field.ContainingNamespace)
                    {
                        DefinitionExists = true,
                        Accessibility = field.DeclaredAccessibility
                    };
                    break;

                case IPropertySymbol property:
                    if (property.GetMethod is null)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            FluentDiagnostics.FluentStoragePropertyWithoutGetter,
                            location,
                            property.Name));
                        continue;
                    }

                    if (!TryAddStorage(parameterName, property.Name, location, seenNames, diagnostics))
                        continue;

                    storageMap[parameterName] = new PropertyStorage(
                        property.Name, property.Type, property.ContainingNamespace)
                    {
                        DefinitionExists = true,
                        Accessibility = property.DeclaredAccessibility
                    };
                    break;
            }
        }

        return storageMap;
    }

    /// <summary>
    /// Attempts to register a storage mapping, reporting a duplicate diagnostic if the
    /// parameter name has already been mapped.
    /// </summary>
    /// <returns>True if the mapping was added; false if it was a duplicate.</returns>
    private static bool TryAddStorage(
        string parameterName,
        string memberName,
        Location location,
        Dictionary<string, string> seenNames,
        DiagnosticList diagnostics)
    {
        if (seenNames.TryGetValue(parameterName, out var existingMemberName))
        {
            diagnostics.Add(Diagnostic.Create(
                FluentDiagnostics.DuplicateFluentStorageMapping,
                location,
                parameterName,
                existingMemberName,
                memberName));
            return false;
        }

        seenNames[parameterName] = memberName;
        return true;
    }

}
