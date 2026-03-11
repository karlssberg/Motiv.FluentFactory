using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.SyntaxGeneration.Helpers;

/// <summary>
/// Creates a [global::System.CodeDom.Compiler.GeneratedCode] attribute list for generated type declarations.
/// </summary>
internal static class GeneratedCodeAttributeSyntax
{
    /// <summary>
    /// Creates an attribute list containing the GeneratedCode attribute with the generator name and version.
    /// </summary>
    public static AttributeListSyntax Create()
    {
        var version = typeof(FluentFactoryGenerator).Assembly.GetName().Version?.ToString() ?? "0.0.0";

        var attribute = Attribute(
            ParseName("global::System.CodeDom.Compiler.GeneratedCode"),
            AttributeArgumentList(
                SeparatedList(new[]
                {
                    AttributeArgument(
                        LiteralExpression(SyntaxKind.StringLiteralExpression,
                            Literal("Motiv.FluentFactory"))),
                    AttributeArgument(
                        LiteralExpression(SyntaxKind.StringLiteralExpression,
                            Literal(version)))
                })));

        return AttributeList(SingletonSeparatedList(attribute));
    }
}
