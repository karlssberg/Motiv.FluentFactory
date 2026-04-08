using System.Collections.Immutable;
using Converj.Generator.Diagnostics;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Analyzes a factory root type for fields, properties, and primary constructor parameters
/// marked with [FluentParameter] and extracts their binding metadata.
/// </summary>
internal static class FluentParameterAnalyzer
{
    /// <summary>
    /// Scans the root type for members and primary constructor parameters marked with [FluentParameter].
    /// Reports diagnostics for invalid usages (e.g., properties without getters, duplicate mappings).
    /// </summary>
    /// <param name="rootType">The factory root type to analyze.</param>
    /// <param name="diagnostics">Diagnostic list to report issues to.</param>
    /// <returns>An immutable array of discovered fluent parameter members.</returns>
    public static ImmutableArray<FluentParameterMember> Analyze(
        INamedTypeSymbol rootType,
        DiagnosticList diagnostics)
    {
        var members = ImmutableArray.CreateBuilder<FluentParameterMember>();
        var seenParameterNames = new Dictionary<string, FluentParameterMember>();

        // Use OriginalDefinition so that member attribute lookup works correctly
        // for types resolved from open generic references (e.g., typeof(Container<>)).
        var definitionType = rootType.OriginalDefinition;

        AnalyzeMembers(definitionType, members, seenParameterNames, diagnostics);
        AnalyzePrimaryConstructorParameters(definitionType, members, seenParameterNames, diagnostics);

        return members.ToImmutable();
    }

    /// <summary>
    /// Scans fields and properties on the root type for [FluentParameter].
    /// </summary>
    private static void AnalyzeMembers(
        INamedTypeSymbol rootType,
        ImmutableArray<FluentParameterMember>.Builder members,
        Dictionary<string, FluentParameterMember> seenParameterNames,
        DiagnosticList diagnostics)
    {
        foreach (var member in rootType.GetMembers())
        {
            var attribute = member.GetAttributes(TypeName.FluentParameterAttribute).FirstOrDefault();
            if (attribute is null) continue;

            var parameterName = attribute.GetFirstStringArgument() ?? member.Name.StripLeadingUnderscores();

            var location = member.Locations.FirstOrDefault() ?? Location.None;

            switch (member)
            {
                case IFieldSymbol field:
                    var fieldMember = new FluentParameterMember(
                        parameterName, field.Type, field.Name, false, location);
                    AddMember(fieldMember, seenParameterNames, members, diagnostics);
                    break;

                case IPropertySymbol property:
                    if (property.GetMethod is null)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            FluentDiagnostics.FluentParameterPropertyWithoutGetter,
                            location,
                            property.Name));
                        continue;
                    }

                    var propertyMember = new FluentParameterMember(
                        parameterName, property.Type, property.Name, true, location);
                    AddMember(propertyMember, seenParameterNames, members, diagnostics);
                    break;
            }
        }
    }

    /// <summary>
    /// Scans primary constructor parameters for [FluentParameter] and resolves their storage.
    /// For records, uses the auto-generated property. For non-records, looks for an explicit
    /// field/property or marks the member as requiring a generated backing field.
    /// </summary>
    private static void AnalyzePrimaryConstructorParameters(
        INamedTypeSymbol rootType,
        ImmutableArray<FluentParameterMember>.Builder members,
        Dictionary<string, FluentParameterMember> seenParameterNames,
        DiagnosticList diagnostics)
    {
        var primaryConstructor = rootType.FindPrimaryConstructor();
        if (primaryConstructor is null) return;

        var memberStorageMap = BuildMemberStorageMap(rootType);

        foreach (var parameter in primaryConstructor.Parameters)
        {
            var attribute = parameter.GetAttributes(TypeName.FluentParameterAttribute).FirstOrDefault();
            if (attribute is null && !rootType.IsRecord) continue;

            var parameterName = attribute?.GetFirstStringArgument()
                ?? parameter.Name.StripLeadingUnderscores();

            // Already handled via field/property-level attribute
            if (seenParameterNames.ContainsKey(parameterName)) continue;

            var location = parameter.Locations.FirstOrDefault() ?? Location.None;
            var (memberName, isProperty, requiresGeneration) =
                ResolveParameterStorage(rootType, parameter, memberStorageMap);

            var isImplicit = attribute is null;
            var member = new FluentParameterMember(
                parameterName, parameter.Type, memberName, isProperty, location,
                requiresGeneration, requiresGeneration ? parameter.Name : null, isImplicit);

            AddMember(member, seenParameterNames, members, diagnostics);
        }
    }

    /// <summary>
    /// Pre-computes a map from primary constructor parameter names to their storage members,
    /// scanning type members once rather than per-parameter.
    /// </summary>
    private static Dictionary<string, (string MemberName, bool IsProperty)> BuildMemberStorageMap(
        INamedTypeSymbol rootType)
    {
        var map = new Dictionary<string, (string, bool)>();

        foreach (var member in rootType.GetMembers())
        {
            var initializer = member.GetInitializerSyntax();
            if (initializer is not IdentifierNameSyntax id) continue;

            var paramName = id.Identifier.ValueText;
            if (map.ContainsKey(paramName)) continue;

            switch (member)
            {
                case IFieldSymbol field:
                    map[paramName] = (field.Name, false);
                    break;
                case IPropertySymbol { GetMethod: not null } property:
                    map[paramName] = (property.Name, true);
                    break;
            }
        }

        return map;
    }

    /// <summary>
    /// Resolves how a primary constructor parameter's value can be accessed on the type.
    /// </summary>
    private static (string MemberName, bool IsProperty, bool RequiresGeneration) ResolveParameterStorage(
        INamedTypeSymbol rootType,
        IParameterSymbol parameter,
        Dictionary<string, (string MemberName, bool IsProperty)> memberStorageMap)
    {
        // Records auto-generate properties for primary constructor parameters
        if (rootType.IsRecord)
        {
            var property = rootType.FindRecordProperty(parameter);
            if (property is not null)
                return (property.Name, true, false);
        }

        // Check pre-computed map for explicit field/property initialized from this parameter
        if (memberStorageMap.TryGetValue(parameter.Name, out var storage))
            return (storage.MemberName, storage.IsProperty, false);

        var generatedFieldName = $"_{parameter.Name}__fluentParameter";
        return (generatedFieldName, false, true);
    }

    /// <summary>
    /// Adds a member to the collection, checking for duplicate parameter name mappings.
    /// </summary>
    private static void AddMember(
        FluentParameterMember member,
        Dictionary<string, FluentParameterMember> seenParameterNames,
        ImmutableArray<FluentParameterMember>.Builder members,
        DiagnosticList diagnostics)
    {
        if (seenParameterNames.TryGetValue(member.TargetParameterName, out var existing))
        {
            diagnostics.Add(Diagnostic.Create(
                FluentDiagnostics.DuplicateFluentParameterMapping,
                member.Location,
                member.TargetParameterName,
                existing.MemberIdentifierName,
                member.MemberIdentifierName));
            return;
        }

        seenParameterNames[member.TargetParameterName] = member;
        members.Add(member);
    }

}
