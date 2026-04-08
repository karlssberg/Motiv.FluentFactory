using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converj.Generator.Extensions;

/// <summary>
/// Shared utilities for working with primary constructors and their parameter storage.
/// </summary>
internal static class PrimaryConstructorExtensions
{
    /// <summary>
    /// Finds the primary constructor of a type, if one exists.
    /// </summary>
    public static IMethodSymbol? FindPrimaryConstructor(this INamedTypeSymbol type)
    {
        foreach (var constructor in type.InstanceConstructors)
        {
            var syntaxRef = constructor.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef?.GetSyntax() is TypeDeclarationSyntax { ParameterList: not null })
                return constructor;
        }

        return null;
    }

    /// <summary>
    /// Gets the initializer expression for a field or property declaration.
    /// </summary>
    public static ExpressionSyntax? GetInitializerSyntax(this ISymbol symbol)
    {
        var declaringSyntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

        return symbol switch
        {
            IFieldSymbol when declaringSyntax is VariableDeclaratorSyntax fieldDeclarator =>
                fieldDeclarator.Initializer?.Value,
            IPropertySymbol when declaringSyntax is PropertyDeclarationSyntax propertyDeclaration =>
                propertyDeclaration.Initializer?.Value,
            _ => null
        };
    }

    /// <summary>
    /// Finds a record's auto-generated property matching a constructor parameter by name (case-insensitive).
    /// </summary>
    public static IPropertySymbol? FindRecordProperty(this INamedTypeSymbol type, IParameterSymbol parameter) =>
        type.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));
}
