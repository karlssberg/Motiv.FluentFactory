using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration.Helpers;

internal static class ReturnTypeConstructorArgumentsSyntax
{
    public static IEnumerable<ArgumentSyntax> Create(IFluentMethod method)
    {
        return method.ValueSources
            .Select(pair => pair.Value)
            .Select(storage =>
            {
                ExpressionSyntax node =
                    storage switch
                    {
                        PrimaryConstructorParameterStorage =>
                            IdentifierName(storage.IdentifierName),
                        FieldStorage or PropertyStorage =>
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                ThisExpression(),
                                IdentifierName(storage.IdentifierName)),
                        _ =>
                            DefaultExpression(
                                ParseTypeName(
                                    storage.Type.ToGlobalDisplayString()))
                    };

                return Argument(node);
            })
            .Concat(method.MethodParameters
                .Select(p => p.SourceName.ToCamelCase())
                .Select(IdentifierName)
                .Select(Argument));
    }
}
