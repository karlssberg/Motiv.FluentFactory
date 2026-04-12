using System.Numerics;
using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot(TerminalMethod = TerminalMethod.None, MethodPrefix = "")]
internal partial class AsAliasBuilder;

[FluentTarget<AsAliasBuilder>]
internal partial record AsStep1<[As("T")] TNum>(TNum X) where TNum : INumber<TNum>;

[FluentTarget<AsAliasBuilder>]
internal partial record AsStep2<T>(T X, T Y) where T : INumber<T>;

#endregion

public class AsAttributeRuntimeTests
{
    [Fact]
    public void As_attribute_should_unify_type_parameters_for_single_step()
    {
        AsStep1<int> result = AsAliasBuilder.X(10);

        result.X.ShouldBe(10);
    }

    [Fact]
    public void As_attribute_should_chain_through_unified_type_parameters()
    {
        AsStep2<double> result = AsAliasBuilder.X(1.5).Y(2.5);

        result.X.ShouldBe(1.5);
        result.Y.ShouldBe(2.5);
    }
}
