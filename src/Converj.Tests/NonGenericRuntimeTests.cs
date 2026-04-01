using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal partial class NonGenericFactory;

[FluentTarget<NonGenericFactory>]
internal record NonGenericTarget(int Number, string Text);

#endregion

public class NonGenericRuntimeTests
{
    [Fact]
    public void Should_thread_values_to_target()
    {
        var result = NonGenericFactory.WithNumber(42).WithText("hello").CreateNonGenericTarget();

        result.Number.ShouldBe(42);
        result.Text.ShouldBe("hello");
    }
}
