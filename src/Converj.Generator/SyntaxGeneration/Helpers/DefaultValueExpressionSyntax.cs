using Converj.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration.Helpers;

/// <summary>
/// Creates Roslyn expression syntax for a parameter's explicit default value.
/// </summary>
internal static class DefaultValueExpressionSyntax
{
    /// <summary>
    /// Converts a parameter symbol's explicit default value into the corresponding Roslyn expression syntax.
    /// </summary>
    /// <param name="parameter">The parameter symbol with an explicit default value.</param>
    /// <returns>An expression syntax representing the default value.</returns>
    public static ExpressionSyntax Create(IParameterSymbol parameter)
    {
        var value = parameter.ExplicitDefaultValue;
        var type = parameter.Type;

        if (value is null)
            return CreateNullOrDefaultExpression(type);

        return value switch
        {
            int i => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i)),
            long l => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(l)),
            float f => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(f)),
            double d => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(d)),
            decimal m => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(m)),
            bool b => LiteralExpression(b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
            char c => LiteralExpression(SyntaxKind.CharacterLiteralExpression, Literal(c)),
            string s => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(s)),
            _ when type.TypeKind == TypeKind.Enum => CreateEnumExpression(type, value),
            _ => DefaultExpression(ParseTypeName(type.ToGlobalDisplayString()))
        };
    }

    private static ExpressionSyntax CreateNullOrDefaultExpression(ITypeSymbol type)
    {
        return type.IsReferenceType || type.NullableAnnotation == NullableAnnotation.Annotated
            ? LiteralExpression(SyntaxKind.NullLiteralExpression)
            : DefaultExpression(ParseTypeName(type.ToGlobalDisplayString()));
    }

    private static ExpressionSyntax CreateEnumExpression(ITypeSymbol enumType, object value)
    {
        var enumMembers = enumType.GetMembers().OfType<IFieldSymbol>();
        var matchingMember = enumMembers.FirstOrDefault(f =>
            f.HasConstantValue && Equals(f.ConstantValue, value));

        if (matchingMember is not null)
        {
            return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ParseTypeName(enumType.ToGlobalDisplayString()),
                IdentifierName(matchingMember.Name));
        }

        // Fallback: cast the underlying integer value
        return CastExpression(
            ParseTypeName(enumType.ToGlobalDisplayString()),
            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(Convert.ToInt32(value))));
    }
}
