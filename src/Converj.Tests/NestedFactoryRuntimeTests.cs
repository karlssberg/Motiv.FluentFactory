using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

internal partial class Outer
{
    [FluentRoot]
    internal partial class NestedFactory;

    [FluentTarget<NestedFactory>]
    internal record NestedTarget(int Value, string Name);
}

#endregion

public class NestedFactoryRuntimeTests
{
    [Fact]
    public void Nested_factory_should_thread_values_to_target()
    {
        var result = Outer.NestedFactory.WithValue(99).WithName("nested").CreateOuter_NestedTarget();

        result.Value.ShouldBe(99);
        result.Name.ShouldBe("nested");
    }
}
