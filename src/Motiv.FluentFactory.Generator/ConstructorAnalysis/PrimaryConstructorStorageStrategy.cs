using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Motiv.FluentFactory.Generator.ConstructorAnalysis;

/// <summary>
/// Detects storage for primary constructor parameters by analyzing direct parameter access
/// and member initializations from primary constructor parameters.
/// </summary>
internal class PrimaryConstructorStorageStrategy : IStorageDetectionStrategy
{
    /// <inheritdoc />
    public bool CanHandle(IMethodSymbol constructor, SemanticModel semanticModel)
    {
        var syntaxNode = constructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        return syntaxNode is TypeDeclarationSyntax { ParameterList: not null };
    }

    /// <inheritdoc />
    public void PopulateStorage(
        IMethodSymbol constructor,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> results,
        SemanticModel semanticModel)
    {
        PopulateWithPrimaryConstructorParametersDirectAccess(constructor, results, constructor.ContainingNamespace);
        PopulateWithMembersInitializedFromPrimaryConstructors(constructor, results, constructor.ContainingNamespace, semanticModel);
    }

    private static void PopulateWithPrimaryConstructorParametersDirectAccess(
        IMethodSymbol primaryConstructor,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> result,
        INamespaceSymbol constructorContainingNamespace)
    {
        foreach (var parameter in primaryConstructor.Parameters)
        {
            result[parameter] =
                new PrimaryConstructorParameterStorage(
                    parameter.Name,
                    parameter.Type,
                    constructorContainingNamespace)
                {
                    DefinitionExists = true
                };
        }
    }

    private static void PopulateWithMembersInitializedFromPrimaryConstructors(
        IMethodSymbol primaryConstructor,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> result,
        INamespaceSymbol constructorContainingNamespace,
        SemanticModel semanticModel)
    {
        foreach (var member in primaryConstructor.ContainingType.GetMembers())
        {
            switch (member)
            {
                case IFieldSymbol fieldSymbol:
                {
                    var initializer = GetInitializerSyntax(fieldSymbol);
                    if (initializer is null) break;

                    foreach (var parameter in GetParameterSymbols(initializer))
                        result[parameter] =
                            new FieldStorage(fieldSymbol.Name, fieldSymbol.Type, constructorContainingNamespace)
                            {
                                DefinitionExists = true
                            };
                    break;
                }
                case IPropertySymbol propertySymbol:
                {
                    var initializer = GetInitializerSyntax(propertySymbol);
                    if (initializer is null) break;

                    foreach (var parameter in GetParameterSymbols(initializer))
                        result[parameter] =
                            new PropertyStorage(propertySymbol.Name, propertySymbol.Type, constructorContainingNamespace)
                            {
                                DefinitionExists = true
                            };
                    break;
                }
            }
        }

        return;

        IEnumerable<IParameterSymbol> GetParameterSymbols(ExpressionSyntax initializer)
        {
            return from parameter in primaryConstructor.Parameters
                let isInitialized = IsInitializedFromParameter(initializer, parameter, semanticModel)
                where isInitialized
                select parameter;
        }
    }

    private static bool IsInitializedFromParameter(
        ExpressionSyntax initializer,
        IParameterSymbol parameter,
        SemanticModel semanticModel)
    {
        if (initializer is not IdentifierNameSyntax identifier)
            return false;

        var symbolInfo = semanticModel.GetSymbolInfo(identifier);
        return SymbolEqualityComparer.Default.Equals(symbolInfo.Symbol, parameter);
    }

    private static ExpressionSyntax? GetInitializerSyntax(ISymbol symbol)
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
}
