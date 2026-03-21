using Microsoft.CodeAnalysis;

namespace Converg.Generator;

/// <summary>
/// Extension methods for fluent model domain operations including method display,
/// unreachable constructor detection, and fluent return type arguments.
/// </summary>
internal static class FluentModelExtensions
{
    private static readonly SymbolDisplayFormat FullFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                         SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                         SymbolDisplayGenericsOptions.IncludeVariance,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters |
                       SymbolDisplayMemberOptions.IncludeContainingType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                          SymbolDisplayParameterOptions.IncludeName |
                          SymbolDisplayParameterOptions.IncludeDefaultValue,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                              SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>
    /// Returns a display string representation of a fluent method, including
    /// type parameters and method parameters.
    /// </summary>
    /// <param name="method">The fluent method to display.</param>
    /// <returns>A formatted display string for the fluent method.</returns>
    public static string ToDisplayString(this IFluentMethod method) =>
        method switch
        {
            MultiMethod multiMethod => multiMethod.ParameterConverter.ToDisplayString(FullFormat),
            _ => method.SerializeFluentMethod()
        };

    private static string SerializeFluentMethod(this IFluentMethod method)
    {
        var typeParameterDisplayStrings = method.TypeParameters
            .Select(fluentTypeParameter => fluentTypeParameter.TypeParameterSymbol.ToDisplayString(FullFormat));

        var typeParameterList = method.TypeParameters.Length > 0
            ? $"<{string.Join(", ", typeParameterDisplayStrings)}>"
            : string.Empty;

        var parameterDisplayStrings = method.MethodParameters
            .Select(p => p.ParameterSymbol.ToDisplayString(FullFormat));

        return $"{method.Name}{typeParameterList}({string.Join(", ", parameterDisplayStrings)})";
    }

    /// <summary>
    /// Finds constructors from the ignored method's return that are unreachable
    /// from the selected method's return.
    /// </summary>
    /// <param name="selectedMethod">The method being selected.</param>
    /// <param name="ignoredMethod">The method being ignored.</param>
    /// <param name="allIgnoredMultiMethods">All multi-methods that are being ignored.</param>
    /// <returns>An enumerable of unreachable constructor method symbols.</returns>
    public static IEnumerable<IMethodSymbol> FindUnreachableConstructors(
        this IFluentMethod selectedMethod,
        IFluentMethod ignoredMethod,
        IEnumerable<IMethodSymbol> allIgnoredMultiMethods)
    {
        var reachableConstructors = (selectedMethod, ignoredMethod) switch
        {
            (_, MultiMethod multiMethod) when multiMethod.SiblingMultiMethods.IsSubsetOf(allIgnoredMultiMethods) =>
                selectedMethod.Return.CandidateConstructors,
            (_, MultiMethod multiMethod) =>
                [..selectedMethod.Return.CandidateConstructors, ..multiMethod.Return.CandidateConstructors],
            ({ Return: TargetTypeReturn targetTypeReturn }, _) =>
                [targetTypeReturn.Constructor],
            _ => selectedMethod.Return.CandidateConstructors
        };

        return ignoredMethod.Return.CandidateConstructors
            .Except<IMethodSymbol>(reachableConstructors, SymbolEqualityComparer.Default);
    }

    /// <summary>
    /// Gets the generic type arguments from a fluent return, mapping type parameters
    /// through the provided generic type parameter map.
    /// </summary>
    /// <param name="fluentReturn">The fluent return to extract type arguments from.</param>
    /// <param name="genericTypeParameterMap">A mapping from fluent types to resolved type symbols.</param>
    /// <returns>An enumerable of distinct type symbols representing the generic type arguments.</returns>
    public static IEnumerable<ITypeSymbol> GetGenericTypeArguments(
        this IFluentReturn fluentReturn,
        IDictionary<FluentType, ITypeSymbol> genericTypeParameterMap)
    {
        return fluentReturn.GenericConstructorParameters
            .SelectMany(parameterSymbol => parameterSymbol.Type.GetGenericTypeArguments())
            .Select(parameter => genericTypeParameterMap.TryGetValue(new FluentType(parameter), out var type)
                ? type
                : parameter)
            .DistinctBy(type => type.ToDisplayString());
    }
}
