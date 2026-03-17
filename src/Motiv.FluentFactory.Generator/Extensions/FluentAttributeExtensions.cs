using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.Diagnostics;

namespace Motiv.FluentFactory.Generator;

/// <summary>
/// Extension methods for reading and interpreting fluent attribute configuration
/// from parameter symbols, including method names, priorities, and multi-method templates.
/// </summary>
internal static class FluentAttributeExtensions
{
    /// <summary>
    /// Gets the fluent method name for a parameter symbol, derived from the
    /// FluentMethodAttribute or MultipleFluentMethodsAttribute.
    /// </summary>
    /// <param name="parameterSymbol">The parameter symbol to get the fluent method name for.</param>
    /// <param name="methodPrefix">The prefix to use for the method name when no explicit attribute is set. Defaults to "With".</param>
    /// <returns>The fluent method name.</returns>
    public static string GetFluentMethodName(this IParameterSymbol parameterSymbol, string methodPrefix = "With")
    {
        var regularMethodAttribute = parameterSymbol.GetAttribute(TypeName.FluentMethodAttribute);
        var multipleMethodAttribute = parameterSymbol.GetAttribute(TypeName.MultipleFluentMethodsAttribute);

        var name = (regularMethodAttribute, mutlipleMethodAttribute: multipleMethodAttribute) switch
        {
            ({ ConstructorArguments: { Length: 1 } args }, _) when args.First().Value is not null =>
                args.First().Value!.ToString(),
            (_, { ConstructorArguments: { Length: 1 } args }) when args.First().Value is INamedTypeSymbol =>
                args.First().Value!.ToString(),
            _ => $"{methodPrefix}{parameterSymbol.Name.Capitalize()}"
        };

        return name;
    }

    /// <summary>
    /// Gets the method symbols from the multiple fluent methods template class,
    /// along with any diagnostics for invalid configurations.
    /// </summary>
    /// <param name="compilation">The compilation context.</param>
    /// <param name="parameterSymbol">The parameter symbol with the MultipleFluentMethodsAttribute.</param>
    /// <returns>An enumerable of method symbols paired with their diagnostics.</returns>
    public static IEnumerable<(IMethodSymbol Method, ICollection<Diagnostic> Diagnostics)> GetMultipleFluentMethodSymbols(
        this Compilation compilation,
        IParameterSymbol parameterSymbol)
    {
        var attribute = parameterSymbol.GetAttribute(TypeName.MultipleFluentMethodsAttribute);

        var methodTemplateClass = attribute?.ConstructorArguments.FirstOrDefault();
        if (methodTemplateClass?.Value is not ITypeSymbol methodTemplateClassSymbol)
            return [];

        var multiMethodClass = methodTemplateClassSymbol.IsOpenGenericType()
            ? methodTemplateClassSymbol.OriginalDefinition
            : methodTemplateClassSymbol;

        var attributeSyntax = attribute?.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
        var location = attributeSyntax?.ArgumentList?.Arguments.FirstOrDefault()?.GetLocation()
                       ?? parameterSymbol.Locations.FirstOrDefault();

        return multiMethodClass
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method => !method.IsImplicitlyDeclared)
            .Where(method => method.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString() == TypeName.FluentMethodTemplateAttribute))
            .Select(method =>
            {
                List<Diagnostic> diagnostics = [];

                if (!method.IsStatic)
                    diagnostics.Add(Diagnostic.Create(
                        FluentDiagnostics.FluentMethodTemplateAttributeNotStatic,
                        location,
                        method.Locations,
                        method.ToFullDisplayString(),
                        methodTemplateClassSymbol.ToFullDisplayString()));

                if (!compilation.IsAssignable(method.ReturnType.OriginalDefinition, parameterSymbol.Type))
                {
                    diagnostics.AddRange(
                    [
                        Diagnostic.Create(
                            FluentDiagnostics.IncompatibleFluentMethodTemplate,
                            location,
                            method.Locations,
                            ImmutableDictionary.Create<string, string?>()
                                .Add("FluentMethodTemplate", method.ToFullDisplayString())
                                .Add("FluentConstructorParameter", parameterSymbol.ToFullDisplayString()),
                            method.ToFullDisplayString(),
                            parameterSymbol.ToFullDisplayString())
                    ]);
                }

                return (method, diagnostics as ICollection<Diagnostic>);
            });
    }

    /// <summary>
    /// Gets the fluent method priority from the FluentMethodAttribute or
    /// MultipleFluentMethodsAttribute Priority named argument.
    /// </summary>
    /// <param name="parameterSymbol">The parameter symbol to get the priority for.</param>
    /// <returns>The priority value, or 0 if not specified.</returns>
    public static int GetFluentMethodPriority(this IParameterSymbol parameterSymbol)
    {
        const string priorityPropertyName = "Priority";

        var attribute = parameterSymbol.GetAttribute(TypeName.MultipleFluentMethodsAttribute)
            ?? parameterSymbol.GetAttribute(TypeName.FluentMethodAttribute);
        if (attribute == null) return 0; // Default priority for parameters without the attribute

        // Look for Priority named argument
        var priorityArg = attribute.NamedArguments
            .FirstOrDefault(na => na.Key == priorityPropertyName);

        return priorityArg.Value switch
        {
            { Value: int value, IsNull: false } => value,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the location of a specific argument in an attribute's argument list.
    /// </summary>
    /// <param name="attributeData">The attribute data to inspect.</param>
    /// <param name="argumentIndex">The zero-based index of the argument.</param>
    /// <returns>The location of the argument, or null if not available.</returns>
    public static Location? GetLocationAtIndex(this AttributeData attributeData, int argumentIndex)
    {
        var attributeSyntax = attributeData.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
        return attributeSyntax?.ArgumentList?.Arguments.ElementAt(argumentIndex).GetLocation();
    }

    /// <summary>
    /// Gets the attribute data for a specific fluent attribute on a parameter symbol.
    /// </summary>
    /// <param name="parameterSymbol">The parameter symbol to inspect.</param>
    /// <param name="fluentMethodName">The attribute type name to search for.</param>
    /// <returns>The matching <see cref="AttributeData"/>, or null if not found.</returns>
    public static AttributeData? GetAttribute(
        this IParameterSymbol parameterSymbol,
        TypeName fluentMethodName)
    {
        var format = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None
        );

        return parameterSymbol
            .GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString(format) == fluentMethodName);
    }
}
