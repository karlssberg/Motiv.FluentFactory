using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentFactory]
internal partial class PrimaryCtorFactory;

[FluentConstructor<PrimaryCtorFactory>]
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
        var result = PrimaryCtorFactory.WithValue(7).WithName("primary").CreatePrimaryCtorTarget();

        result.Value.ShouldBe(7);
        result.Name.ShouldBe("primary");
    }
}
