using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal partial class PrimaryCtorBuilder;

[FluentTarget<PrimaryCtorBuilder>]
internal class PrimaryCtorTarget(int value, string name)
{
    public int Value { get; } = value;
    public string Name { get; } = name;
}

#endregion

public class PrimaryConstructorRuntimeTests
{
    [Fact]
    public void Primary_constructor_target_should_receive_threaded_values()
    {
        var result = PrimaryCtorBuilder.WithValue(7).WithName("primary").CreatePrimaryCtorTarget();

        result.Value.ShouldBe(7);
        result.Name.ShouldBe("primary");
    }
}
