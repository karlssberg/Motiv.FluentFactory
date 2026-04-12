using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot(MethodPrefix = "")]
internal partial class BarePrefixBuilder;

[FluentTarget<BarePrefixBuilder>]
internal record BarePrefixTarget(int Width, int Height);

[FluentRoot(MethodPrefix = "Having")]
internal partial class CustomPrefixBuilder;

[FluentTarget<CustomPrefixBuilder>]
internal record CustomPrefixTarget(string Name, int Value);

#endregion

public class MethodPrefixRuntimeTests
{
    [Fact]
    public void Empty_prefix_should_use_bare_parameter_names()
    {
        var result = BarePrefixBuilder.Width(10).Height(20).CreateBarePrefixTarget();

        result.Width.ShouldBe(10);
        result.Height.ShouldBe(20);
    }

    [Fact]
    public void Custom_prefix_should_use_specified_prefix()
    {
        var result = CustomPrefixBuilder.HavingName("test").HavingValue(42).CreateCustomPrefixTarget();

        result.Name.ShouldBe("test");
        result.Value.ShouldBe(42);
    }
}
