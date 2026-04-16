using System.Reflection;
using Converj.Generator.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Converj.Generator.Tests.DomainModel;

/// <summary>
/// Unit tests pinning <see cref="FluentType"/> equality semantics — the fundamental building
/// block used by <see cref="Converj.Generator.ModelBuilding.FluentMethodSignatureEqualityComparer"/>.
/// Also documents the RefKind deferral decision from Phase 23 Plan 04.
/// </summary>
public class FluentMethodSignatureEqualityComparerTests
{
    private static Compilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            ]);
    }

    /// <summary>
    /// Two FluentType instances wrapping the same underlying type symbol are equal.
    /// This is the foundation for signature-same detection in collision logic.
    /// </summary>
    [Fact]
    public void FluentType_same_type_symbol_is_equal()
    {
        var comp = CreateCompilation("public class C {}");
        var intType = comp.GetSpecialType(SpecialType.System_Int32);

        var a = new FluentType(intType);
        var b = new FluentType(intType);

        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Two FluentType instances wrapping distinct type symbols are NOT equal.
    /// This is what enables the broad overload rule: string vs int → different signatures.
    /// </summary>
    [Fact]
    public void FluentType_different_type_symbols_are_not_equal()
    {
        var comp = CreateCompilation("public class C {}");
        var intType = comp.GetSpecialType(SpecialType.System_Int32);
        var stringType = comp.GetSpecialType(SpecialType.System_String);

        var a = new FluentType(intType);
        var b = new FluentType(stringType);

        Assert.False(a.Equals(b));
    }

    /// <summary>
    /// RefKind documentation test: FluentType wraps the type symbol only — RefKind is NOT
    /// part of the key. <c>int</c> and <c>in int</c> produce the same FluentType.
    ///
    /// Deferral reason (Phase 23 Plan 04):
    /// - ref/out parameters are rejected by FilterUnsupportedParameterModifierTargets
    ///   before model building, so they never reach the comparer.
    /// - The generator already emits 'in' for all value-type parameters in generated step
    ///   methods regardless of the user's source modifier, making user-side RefKind
    ///   incapable of creating distinct fluent-API signatures.
    /// - Full RefKind propagation through the domain model is deferred post-v2.2.
    /// </summary>
    [Fact]
    public void FluentType_does_not_distinguish_RefKind_in_modifier_deferred()
    {
        var comp = CreateCompilation("""
            public class Holder
            {
                public void Regular(int p) { }
                public void WithIn(in int p) { }
            }
            """);

        var holder = comp.GetTypeByMetadataName("Holder")!;
        var regularParam = holder.GetMembers("Regular").OfType<IMethodSymbol>().Single().Parameters[0];
        var inParam = holder.GetMembers("WithIn").OfType<IMethodSymbol>().Single().Parameters[0];

        // Both parameters have the same underlying type — FluentType does not track RefKind.
        var fluentTypeRegular = new FluentType(regularParam.Type);
        var fluentTypeIn = new FluentType(inParam.Type);

        Assert.True(fluentTypeRegular.Equals(fluentTypeIn),
            "FluentType(int) == FluentType(in int): RefKind is not tracked (documented deferral).");
    }
}
