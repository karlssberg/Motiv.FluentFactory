using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converg.Generator.SyntaxGeneration.Helpers;

internal static class AggressiveInliningAttributeSyntax
{
    public static AttributeSyntax Create() =>
        Attribute(
            ParseName("global::System.Runtime.CompilerServices.MethodImpl"),
            AttributeArgumentList(
                SingletonSeparatedList(
                    AttributeArgument(
                        ParseExpression("global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining")
                    )
                )
            )
        );
}
