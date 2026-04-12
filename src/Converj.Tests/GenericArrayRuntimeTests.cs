using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal static partial class ArrayBuilder;

[FluentTarget(typeof(ArrayBuilder))]
internal record ArrayTarget(int[] Items);

[FluentTarget(typeof(ArrayBuilder))]
internal record StringArrayTarget(string[] Names);

#endregion

public class GenericArrayRuntimeTests
{
    [Fact]
    public void Int_array_parameter_should_thread_value()
    {
        var items = new[] { 1, 2, 3 };

        var result = ArrayBuilder.WithItems(items).CreateArrayTarget();

        result.Items.ShouldBe(items);
    }

    [Fact]
    public void String_array_parameter_should_thread_value()
    {
        var names = new[] { "a", "b", "c" };

        var result = ArrayBuilder.WithNames(names).CreateStringArrayTarget();

        result.Names.ShouldBe(names);
    }
}
