using System.Collections.Immutable;
using Converj.Generator.Diagnostics;
using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converj.Generator.TargetAnalysis;

/// <summary>
/// Analyzes a target type for required and opted-in properties that should participate
/// in the fluent builder chain. Required properties (C# <c>required</c> keyword or
/// <c>[Required]</c> attribute) are auto-discovered. Optional properties are opted in
/// via <c>[FluentMethod]</c>.
/// </summary>
internal static class FluentPropertyAnalyzer
{
    /// <summary>
    /// Discovers properties on the target type that should participate in the fluent chain.
    /// Filters out properties that are already initialized by the constructor.
    /// </summary>
    /// <param name="constructor">The constructor being analyzed.</param>
    /// <param name="targetType">The target type whose properties to analyze.</param>
    /// <param name="methodPrefix">The method prefix for generating fluent method names.</param>
    /// <param name="diagnostics">Diagnostic list to report issues to.</param>
    /// <returns>An immutable array of discovered fluent property members.</returns>
    public static ImmutableArray<FluentPropertyMember> Analyze(
        IMethodSymbol constructor,
        INamedTypeSymbol targetType,
        string methodPrefix,
        DiagnosticList diagnostics)
    {
        var initializedPropertyNames = GetConstructorInitializedPropertyNames(constructor, targetType);
        var members = ImmutableArray.CreateBuilder<FluentPropertyMember>();

        foreach (var member in targetType.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.IsStatic || member.IsIndexer) continue;

            var isRequired = IsRequiredProperty(member);
            var fluentMethodAttr = member.GetAttributes(TypeName.FluentMethodAttribute).FirstOrDefault();
            var isOptedIn = fluentMethodAttr is not null;

            // Skip properties that are not required and not opted in
            if (!isRequired && !isOptedIn) continue;

            // Auto-skip properties initialized by the constructor
            if (initializedPropertyNames.Contains(member.Name)) continue;

            // Validate the property has a setter or init accessor
            if (member.SetMethod is null)
            {
                if (isOptedIn)
                {
                    var location = member.Locations.FirstOrDefault() ?? Location.None;
                    diagnostics.Add(Diagnostic.Create(
                        FluentDiagnostics.FluentMethodOnPropertyWithoutSetter,
                        location,
                        member.Name));
                }
                continue;
            }

            var fluentMethodName = DeriveFluentMethodName(member, fluentMethodAttr, methodPrefix);
            var location2 = member.Locations.FirstOrDefault() ?? Location.None;

            members.Add(new FluentPropertyMember(fluentMethodName, member, isRequired, location2));
        }

        return members.ToImmutable();
    }

    /// <summary>
    /// Determines whether a property is required via the C# <c>required</c> keyword
    /// or the <c>[System.ComponentModel.DataAnnotations.RequiredAttribute]</c>.
    /// </summary>
    private static bool IsRequiredProperty(IPropertySymbol property) =>
        property.IsRequired || property.HasAttribute(TypeName.RequiredAttribute);

    /// <summary>
    /// Derives the fluent method name for a property, using the [FluentMethod] attribute's
    /// explicit name if provided, or the method prefix + property name otherwise.
    /// </summary>
    private static string DeriveFluentMethodName(
        IPropertySymbol property,
        AttributeData? fluentMethodAttr,
        string methodPrefix)
    {
        var explicitName = fluentMethodAttr?.GetFirstStringArgument();
        return explicitName ?? $"{methodPrefix}{property.Name.Capitalize()}";
    }

    /// <summary>
    /// Gets the set of property names on the target type that are initialized by the given constructor,
    /// including record primary constructor parameters, primary constructor parameter assignments,
    /// and explicit <c>this.Property = param</c> assignments in the constructor body.
    /// </summary>
    private static HashSet<string> GetConstructorInitializedPropertyNames(
        IMethodSymbol constructor,
        INamedTypeSymbol targetType)
    {
        var initialized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Record types: primary constructor parameters create properties with matching names
        if (targetType.IsRecord)
        {
            var recordProperty = constructor.Parameters
                .Select(targetType.FindRecordProperty)
                .OfType<IPropertySymbol>();
            
            foreach (var recordProp in recordProperty)
            {
                initialized.Add(recordProp.Name);
            }
        }

        // Primary constructor parameters assigned to properties via initializers
        AddPrimaryConstructorInitializedProperties(constructor, targetType, initialized);

        // Explicit constructor body assignments: this.Property = param
        AddExplicitBodyAssignments(constructor, initialized);

        return initialized;
    }

    /// <summary>
    /// Checks members for property initializers that reference primary constructor parameters.
    /// </summary>
    private static void AddPrimaryConstructorInitializedProperties(
        IMethodSymbol constructor,
        INamedTypeSymbol targetType,
        HashSet<string> initialized)
    {
        var constructorParamNames = new HashSet<string>(constructor.Parameters.Select(p => p.Name));

        foreach (var member in targetType.GetMembers())
        {
            if (member is not IPropertySymbol property) continue;

            var initializerSyntax = member.GetInitializerSyntax();
            if (initializerSyntax is IdentifierNameSyntax id
                && constructorParamNames.Contains(id.Identifier.ValueText))
            {
                initialized.Add(property.Name);
            }
        }
    }

    /// <summary>
    /// Walks the constructor body for explicit <c>this.Property = param</c> or <c>Property = param</c> assignments.
    /// </summary>
    private static void AddExplicitBodyAssignments(
        IMethodSymbol constructor,
        HashSet<string> initialized)
    {
        var syntaxRef = constructor.DeclaringSyntaxReferences.FirstOrDefault();
        var ctorSyntax = syntaxRef?.GetSyntax() as ConstructorDeclarationSyntax;

        if (ctorSyntax?.Body is null && ctorSyntax?.ExpressionBody is null) return;

        var constructorParamNames = new HashSet<string>(constructor.Parameters.Select(p => p.Name));

        var statements = ctorSyntax.Body?.Statements ?? [];
        foreach (var statement in statements)
        {
            if (statement is not ExpressionStatementSyntax
                {
                    Expression: AssignmentExpressionSyntax assignment
                }) continue;

            // Check for this.PropertyName = paramName or PropertyName = paramName
            var assignedName = assignment.Left switch
            {
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: var name } => name.Identifier.ValueText,
                IdentifierNameSyntax id => id.Identifier.ValueText,
                _ => null
            };

            if (assignedName is null) continue;

            var rightName = assignment.Right switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                _ => null
            };

            if (rightName is not null && constructorParamNames.Contains(rightName))
            {
                initialized.Add(assignedName);
            }
        }
    }
}
