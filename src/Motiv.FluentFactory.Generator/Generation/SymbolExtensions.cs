using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Motiv.FluentFactory.Generator.Generation;

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

    /// <summary>
    /// Returns the global::-qualified display string for a type symbol.
    /// Type parameters (T, TResult) are returned as their name only.
    /// C# keyword aliases (int, string, bool) are preserved via UseSpecialTypes.
    /// </summary>
    public static string ToGlobalDisplayString(this ITypeSymbol typeSymbol)
    {
        return typeSymbol switch
        {
            ITypeParameterSymbol tp => tp.Name,
            _ => typeSymbol.ToDisplayString(GlobalQualifiedFormat)
        };
    }

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

    public static IEnumerable<TypeParameterSyntax> GetGenericTypeParameterSyntaxList(this IEnumerable<ITypeSymbol> types)
    {
        return types.GetGenericTypeParameters()
            .Select(ToTypeParameterSyntax);
    }

    public static IEnumerable<TypeParameterSyntax> GetGenericTypeParameterSyntaxList(this ITypeSymbol type)
    {
        return new[] { type }.GetGenericTypeParameterSyntaxList();
    }

    public static IEnumerable<ITypeParameterSymbol> GetGenericTypeParameters(this IEnumerable<ITypeSymbol> type)
    {
        return type
            .SelectMany(symbol => symbol.GetGenericTypeParameters())
            .DistinctBy(symbol => symbol.ToDisplayString());
    }

    public static TypeParameterSyntax ToTypeParameterSyntax(this ITypeParameterSymbol typeParameter)
    {
        var typeParameterSyntax = SyntaxFactory.TypeParameter(SyntaxFactory.Identifier(typeParameter.Name));

        // Add constraints if they exist
        var constraints = new List<TypeParameterConstraintSyntax>();

        // Add value type constraint
        if (typeParameter.HasValueTypeConstraint)
        {
            constraints.Add(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.StructConstraint));
        }

        // Add reference type constraint
        if (typeParameter.HasReferenceTypeConstraint)
        {
            constraints.Add(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint));
        }

        // Add constructor constraint
        if (typeParameter.HasConstructorConstraint)
        {
            constraints.Add(SyntaxFactory.ConstructorConstraint());
        }

        // Add type constraints
        foreach (var constraintType in typeParameter.ConstraintTypes)
        {
            constraints.Add(SyntaxFactory.TypeConstraint(
                SyntaxFactory.ParseTypeName(constraintType.ToGlobalDisplayString())));
        }

        return typeParameterSyntax;
    }

    public static IEnumerable<ITypeParameterSymbol> GetGenericTypeParameters(this ITypeSymbol type)
    {
        return type switch
        {
            ITypeParameterSymbol typeParameter => [typeParameter],
            INamedTypeSymbol namedType => namedType.TypeArguments
                .SelectMany(typeArg => typeArg.GetGenericTypeParameters())
                .Distinct<ITypeParameterSymbol>(SymbolEqualityComparer.Default),
            _ => []
        };
    }

    public static IEnumerable<ITypeParameterSymbol> GetGenericTypeArguments(this ITypeSymbol type)
    {
        return GenericTypeArgumentsInternal(type).DistinctBy(t => t.Name);
    }

    private static IEnumerable<ITypeParameterSymbol> GenericTypeArgumentsInternal(ITypeSymbol type)
    {
        return type switch
        {
            ITypeParameterSymbol typeParameter => [typeParameter],
            INamedTypeSymbol namedType => namedType.TypeArguments
                .SelectMany(t => t.GetGenericTypeArguments()),
            _ => []
        };
    }

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

    public static IEnumerable<ITypeParameterSymbol> Union(
       this IEnumerable<ITypeParameterSymbol> first,
       IEnumerable<ITypeParameterSymbol> second)
   {
       return first
           .Union<ITypeParameterSymbol>(second, SymbolEqualityComparer.IncludeNullability)
           .OrderBy(symbol => symbol.Name);
   }


    public static IEnumerable<ITypeParameterSymbol> Except(
       this IEnumerable<ITypeParameterSymbol> collection,
       IEnumerable<ITypeParameterSymbol> exclusions)
   {
       var exclusionSet = new HashSet<string>(exclusions.Select(parameter => parameter.ToDisplayString()));

       foreach (var item in collection)
       {
          if (!exclusionSet.Contains(item.ToDisplayString()))
            yield return item;
       }
   }

   public static IEnumerable<SyntaxKind> AccessibilityToSyntaxKind(this Accessibility accessibility) =>
       accessibility switch
       {
           Accessibility.Public => [SyntaxKind.PublicKeyword],
           Accessibility.Private => [SyntaxKind.PrivateKeyword],
           Accessibility.Protected => [SyntaxKind.ProtectedKeyword],
           Accessibility.Internal => [SyntaxKind.InternalKeyword],
           Accessibility.ProtectedOrInternal => [SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword], // Note: This will need both protected and internal
           Accessibility.ProtectedAndInternal => [SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword],
           _ => [SyntaxKind.None]
       };

   public static string ToUnqualifiedDisplayString(this ITypeSymbol typeSymbol) =>
       typeSymbol.ToDisplayString(TypeNameOnlyFormat);

}
