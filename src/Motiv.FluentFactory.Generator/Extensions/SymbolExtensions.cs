using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Motiv.FluentFactory.Generator;

/// <summary>
/// Extension methods for type analysis, assignability checking,
/// and type parameter replacement.
/// </summary>
internal static class SymbolExtensions
{
    /// <summary>
    /// Determines whether a type symbol is an open generic type (contains unbound type parameters).
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the type contains unbound generic type parameters; otherwise, <c>false</c>.</returns>
    public static bool IsOpenGenericType(this ITypeSymbol type)
    {
        return type switch
        {
            ITypeParameterSymbol => true,
            INamedTypeSymbol namedType => ContainsUnboundGenericTypes(namedType),
            _ => false
        };
    }

    private static bool ContainsUnboundGenericTypes(INamedTypeSymbol namedType)
    {
        return namedType.TypeArguments.Any(t => t switch
               {
                   ITypeParameterSymbol => true,
                   INamedTypeSymbol { IsGenericType: true } typeSymbol => typeSymbol.IsOpenGenericType(),
                   _ => false
               }) ||
               // Also check if this is a generic type definition itself
               namedType.IsUnboundGenericType;
    }

    /// <summary>
    /// Determines whether a type symbol is declared as partial.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <returns><c>true</c> if the type is declared with the partial modifier; otherwise, <c>false</c>.</returns>
    public static bool IsPartial(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
    }

    /// <summary>
    /// Determines whether a named type symbol can be used as a custom step
    /// (must be partial and non-static).
    /// </summary>
    /// <param name="containingType">The named type symbol to check.</param>
    /// <returns><c>true</c> if the type can be a custom step; otherwise, <c>false</c>.</returns>
    public static bool CanBeCustomStep(this INamedTypeSymbol containingType)
    {
        var isPartial = containingType.IsPartial();
        var isStatic = containingType.IsStatic;
        return isPartial && !isStatic;
    }

    /// <summary>
    /// Determines whether a type from one compilation context is assignable to a type
    /// from another context, including generic type parameter constraint checking.
    /// </summary>
    /// <param name="compilation">The compilation context for type conversion checks.</param>
    /// <param name="typeDefinition">The source type being assigned from.</param>
    /// <param name="typeUsage">The target type being assigned to.</param>
    /// <returns><c>true</c> if the assignment is valid; otherwise, <c>false</c>.</returns>
    public static bool IsAssignable(this Compilation compilation, ITypeSymbol? typeDefinition, ITypeSymbol? typeUsage)
    {
        if (typeDefinition is null || typeUsage is null)
            return false;

        // If they're exactly the same type, return true
        if (SymbolEqualityComparer.Default.Equals(typeDefinition, typeUsage))
            return true;

        switch (typeDefinition, typeUsage)
        {
            case (_, ITypeParameterSymbol usageTypeParam):
            {
                // Check if source satisfies all target's constraints
                if (usageTypeParam.ConstraintTypes
                    .Select(constraint => compilation.ClassifyCommonConversion(typeDefinition, constraint))
                    .Any(paramConversion => paramConversion is { Exists: false }))
                {
                    return false;
                }

                if (usageTypeParam.HasValueTypeConstraint && !typeDefinition.IsValueType)
                    return false;

                if (usageTypeParam.HasReferenceTypeConstraint && !typeDefinition.IsReferenceType)
                    return false;

                if (usageTypeParam.HasConstructorConstraint && !typeDefinition.IsReferenceType)
                    return false;

                return true;
            }
            case (ITypeParameterSymbol definitionTypeParam, _):
            {
                if (definitionTypeParam.ConstraintTypes
                    .Select(constraint => compilation.ClassifyCommonConversion(constraint, typeUsage))
                    .Any(typeConversion => typeConversion is { Exists: true, IsImplicit: true }))
                {
                    return true;
                }

                // Check special constraints against target
                if (definitionTypeParam.HasValueTypeConstraint && !typeUsage.IsValueType)
                    return false;

                return !definitionTypeParam.HasReferenceTypeConstraint || typeUsage.IsReferenceType;
            }
            case (INamedTypeSymbol definitionTypeName, INamedTypeSymbol usageNamedType):
            {
                // Check if they have the same number of type arguments
                if (definitionTypeName.TypeArguments.Length != usageNamedType.TypeArguments.Length)
                    return false;
                // All other parameters are contravariant
                for (var i = 0; i < definitionTypeName.TypeArguments.Length; i++)
                {
                    var paramTypeArg = definitionTypeName.TypeArguments[i];
                    var argTypeArg = usageNamedType.TypeArguments[i];

                    if (!compilation.IsAssignable(paramTypeArg, argTypeArg))
                        return false;
                }
                return true;
            }
            default:
                // Fall back to checking conversion
                var conversion = compilation.ClassifyConversion(typeDefinition, typeUsage);
                return conversion is { Exists: true, IsImplicit: true };
        }
    }

    /// <summary>
    /// Replaces type parameters in a generic named type symbol using the provided replacements map.
    /// Recursively processes nested generic type arguments.
    /// </summary>
    /// <param name="type">The named type symbol to transform.</param>
    /// <param name="replacements">A mapping from type parameters to their replacement types.</param>
    /// <returns>A new named type symbol with type parameters replaced.</returns>
    public static INamedTypeSymbol ReplaceTypeParameters(
        this INamedTypeSymbol type,
        ImmutableDictionary<ITypeParameterSymbol, ITypeSymbol> replacements)
    {
        if (!type.IsGenericType)
            return type;

        var newTypeArgs = type.TypeArguments.Select(arg =>
            arg is ITypeParameterSymbol tp && replacements.TryGetValue(tp, out var replacement)
                ? replacement
                : arg is INamedTypeSymbol namedArg
                    ? ReplaceTypeParameters(namedArg, replacements)
                    : arg);

        return type.OriginalDefinition.Construct(newTypeArgs.ToArray());
    }

}
