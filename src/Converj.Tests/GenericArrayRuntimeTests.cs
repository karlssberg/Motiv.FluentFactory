using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal static partial class ArrayFactory;

[FluentTarget(typeof(ArrayFactory))]
internal record ArrayTarget(int[] Items);

[FluentTarget(typeof(ArrayFactory))]
internal record StringArrayTarget(string[] Names);

#endregion

public class GenericArrayRuntimeTests
{
    [Fact]
    public void Int_array_parameter_should_thread_value()
    {
        var items = new[] { 1, 2, 3 };

        var result = ArrayFactory.WithItems(items).CreateArrayTarget();

        result.Items.ShouldBe(items);
    }

    [Fact]
    public void String_array_parameter_should_thread_value()
    {
        var names = new[] { "a", "b", "c" };

        var result = ArrayFactory.WithNames(names).CreateStringArrayTarget();

        result.Names.ShouldBe(names);
    }
}
