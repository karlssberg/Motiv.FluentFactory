using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Motiv.FluentFactory.Generator.Generation.Shared;

internal static class AggressiveInliningAttributeSyntax
{
    public static AttributeSyntax Create() =>
        Attribute(
            ParseName("System.Runtime.CompilerServices.MethodImpl"),
            AttributeArgumentList(
                SingletonSeparatedList(
                    AttributeArgument(
                        ParseExpression("System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining")
                    )
                )
            )
        );
}
