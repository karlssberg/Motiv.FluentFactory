using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

internal partial class Outer
{
    [FluentRoot]
    internal partial class NestedBuilder;

    [FluentTarget<NestedBuilder>]
    internal record NestedTarget(int Value, string Name);
}

#endregion

public class NestedRootRuntimeTests
{
    [Fact]
    public void Nested_builder_should_thread_values_to_target()
    {
        var result = Outer.NestedBuilder.WithValue(99).WithName("nested").CreateOuter_NestedTarget();

        result.Value.ShouldBe(99);
        result.Name.ShouldBe("nested");
    }
}
