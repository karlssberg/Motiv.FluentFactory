using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Motiv.FluentFactory.Generator;

/// <summary>
/// Extension methods for symbol display formatting, type analysis,
/// accessibility conversion, and attribute inspection.
/// </summary>
internal static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat TypeNameOnlyFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
    );

    private static readonly SymbolDisplayFormat GlobalQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat FullFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                         SymbolDisplayGenericsOptions.IncludeTypeConstraints,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters |
                       SymbolDisplayMemberOptions.IncludeContainingType |
                       SymbolDisplayMemberOptions.IncludeType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                          SymbolDisplayParameterOptions.IncludeName |
                          SymbolDisplayParameterOptions.IncludeDefaultValue,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                              SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>
    /// Returns the global::-qualified display string for a type symbol.
    /// Type parameters (T, TResult) are returned as their name only.
    /// C# keyword aliases (int, string, bool) are preserved via UseSpecialTypes.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to format.</param>
    /// <returns>The global::-qualified display string.</returns>
    public static string ToGlobalDisplayString(this ITypeSymbol typeSymbol)
    {
        return typeSymbol switch
        {
            ITypeParameterSymbol tp => tp.Name,
            _ => typeSymbol.ToDisplayString(GlobalQualifiedFormat)
        };
    }

    /// <summary>
    /// Returns the full display string for a symbol including namespace,
    /// containing types, type parameters, constraints, and member details.
    /// </summary>
    /// <param name="symbol">The symbol to format.</param>
    /// <returns>The full display string.</returns>
    public static string ToFullDisplayString(this ISymbol symbol)
    {
        return symbol.ToDisplayString(FullFormat);
    }

    /// <summary>
    /// Returns the unqualified (name-only) display string for a type symbol.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to format.</param>
    /// <returns>The unqualified display string.</returns>
    public static string ToUnqualifiedDisplayString(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(TypeNameOnlyFormat);

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
    /// Converts a <see cref="Accessibility"/> value to the corresponding syntax kind keywords.
    /// </summary>
    /// <param name="accessibility">The accessibility to convert.</param>
    /// <returns>An enumerable of <see cref="SyntaxKind"/> values representing the access modifier keywords.</returns>
    public static IEnumerable<SyntaxKind> AccessibilityToSyntaxKind(this Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public => [SyntaxKind.PublicKeyword],
            Accessibility.Private => [SyntaxKind.PrivateKeyword],
            Accessibility.Protected => [SyntaxKind.ProtectedKeyword],
            Accessibility.Internal => [SyntaxKind.InternalKeyword],
            Accessibility.ProtectedOrInternal => [SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword],
            Accessibility.ProtectedAndInternal => [SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword],
            _ => [SyntaxKind.None]
        };

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

    /// <summary>
    /// Determines whether a symbol has an attribute matching the specified type name.
    /// </summary>
    /// <param name="type">The symbol to check.</param>
    /// <param name="attribute">The attribute type name to look for.</param>
    /// <returns><c>true</c> if the symbol has the specified attribute; otherwise, <c>false</c>.</returns>
    public static bool HasAttribute(this ISymbol type, TypeName attribute) =>
        GetAttributes(type, attribute).Any();

    /// <summary>
    /// Determines whether a symbol has an attribute of the specified generic type.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to look for.</typeparam>
    /// <param name="type">The symbol to check.</param>
    /// <returns><c>true</c> if the symbol has the specified attribute; otherwise, <c>false</c>.</returns>
    public static bool HasAttribute<TAttribute>(this ISymbol type) where TAttribute : Attribute =>
        GetAttributes<TAttribute>(type).Any();

    /// <summary>
    /// Gets all attribute data instances matching the specified type name from a symbol.
    /// </summary>
    /// <param name="type">The symbol to inspect.</param>
    /// <param name="attribute">The attribute type name to match.</param>
    /// <returns>An enumerable of matching <see cref="AttributeData"/> instances.</returns>
    public static IEnumerable<AttributeData> GetAttributes(this ISymbol type, TypeName attribute) =>
        type.GetAttributes()
            .Where(attr => attr.AttributeClass?.ToDisplayString() == attribute);

    /// <summary>
    /// Gets all attribute data instances of the specified generic type from a symbol.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to match.</typeparam>
    /// <param name="type">The symbol to inspect.</param>
    /// <returns>An enumerable of matching <see cref="AttributeData"/> instances.</returns>
    public static IEnumerable<AttributeData> GetAttributes<TAttribute>(this ISymbol type) where TAttribute : Attribute =>
        type.GetAttributes()
            .Where(attr => attr.AttributeClass?.ToDisplayString() == typeof(TAttribute).FullName);
}
