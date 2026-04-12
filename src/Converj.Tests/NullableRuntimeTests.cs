using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal partial class NullableBuilder;

[FluentTarget<NullableBuilder>]
internal record NullableTarget(string? Name, int? Count);

#endregion

public class NullableRuntimeTests
{
    [Fact]
    public void Nullable_parameters_should_accept_null_values()
    {
        var result = NullableBuilder.WithName(null).WithCount(null).CreateNullableTarget();

        result.Name.ShouldBeNull();
        result.Count.ShouldBeNull();
    }

    [Fact]
    public void Nullable_parameters_should_accept_non_null_values()
    {
        var result = NullableBuilder.WithName("hello").WithCount(42).CreateNullableTarget();

        result.Name.ShouldBe("hello");
        result.Count.ShouldBe(42);
    }
}
