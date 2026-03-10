using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.Model;
using Motiv.FluentFactory.Generator.Model.Storage;

namespace Motiv.FluentFactory.Generator.Analysis;

/// <summary>
/// Detects storage for explicit constructor parameters by analyzing assignment expressions
/// in the constructor body.
/// </summary>
internal class ExplicitConstructorStorageStrategy : IStorageDetectionStrategy
{
    /// <inheritdoc />
    public bool CanHandle(IMethodSymbol constructor, SemanticModel semanticModel)
    {
        var syntaxNode = constructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        return syntaxNode is ConstructorDeclarationSyntax { Body: not null };
    }

    /// <inheritdoc />
    public void PopulateStorage(
        IMethodSymbol constructor,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> results,
        SemanticModel semanticModel)
    {
        var syntaxNode = constructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (syntaxNode is not ConstructorDeclarationSyntax ctorSyntax) return;

        var assignments = ctorSyntax.Body?
            .DescendantNodes()
            .OfType<AssignmentExpressionSyntax>();

        foreach (var assignment in assignments ?? [])
        {
            var rightSymbol = semanticModel.GetSymbolInfo(assignment.Right).Symbol;
            if (rightSymbol is not IParameterSymbol paramSymbol ||
                !results.ContainsKey(paramSymbol)) continue;

            var memberSymbol = semanticModel.GetSymbolInfo(assignment.Left).Symbol;
            results[paramSymbol] = memberSymbol switch
            {
                IPropertySymbol propertySymbol =>
                    new PropertyStorage(propertySymbol.Name, propertySymbol.Type, constructor.ContainingNamespace)
                    {
                        DefinitionExists = true
                    },
                IFieldSymbol fieldSymbol =>
                    new FieldStorage(fieldSymbol.Name, fieldSymbol.Type, constructor.ContainingNamespace)
                    {
                        DefinitionExists = true
                    },
                _ => results[paramSymbol]
            };
        }
    }
}
