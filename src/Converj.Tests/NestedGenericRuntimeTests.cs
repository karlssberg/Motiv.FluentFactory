using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal static partial class NestedGenericBuilder;

[FluentTarget(typeof(NestedGenericBuilder))]
internal record ListTarget<T>(List<T> Items);

[FluentTarget(typeof(NestedGenericBuilder))]
internal record DeepNestedTarget<T>(List<List<T>> DeepItems);

#endregion

public class NestedGenericRuntimeTests
{
    [Fact]
    public void List_generic_parameter_should_thread_value()
    {
        var items = new List<int> { 1, 2, 3 };

        var result = NestedGenericBuilder.WithItems(items).CreateListTarget();

        result.Items.ShouldBe(items);
    }

    [Fact]
    public void Deeply_nested_generic_parameter_should_thread_value()
    {
        var deep = new List<List<string>> { new() { "a", "b" }, new() { "c" } };

        var result = NestedGenericBuilder.WithDeepItems(deep).CreateDeepNestedTarget();

        result.DeepItems.ShouldBe(deep);
    }
}
