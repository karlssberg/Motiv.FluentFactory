using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentFactory]
internal partial class LargeParamFactory;

[FluentConstructor<LargeParamFactory>]
internal record LargeTarget(
    int P1, int P2, int P3, int P4, int P5,
    int P6, int P7, int P8);

#endregion

public class LargeParameterCountRuntimeTests
{
    [Fact]
    public void Eight_parameters_should_all_thread_correctly()
    {
        var result = LargeParamFactory
            .WithP1(1).WithP2(2).WithP3(3).WithP4(4)
            .WithP5(5).WithP6(6).WithP7(7).WithP8(8)
            .CreateLargeTarget();

        result.P1.ShouldBe(1);
        result.P2.ShouldBe(2);
        result.P3.ShouldBe(3);
        result.P4.ShouldBe(4);
        result.P5.ShouldBe(5);
        result.P6.ShouldBe(6);
        result.P7.ShouldBe(7);
        result.P8.ShouldBe(8);
    }
}
