using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal partial class GenericAttrBuilder;

[FluentTarget<GenericAttrBuilder>]
internal record GenericAttrTarget(int Value, string Label);

#endregion

public class GenericAttributeRuntimeTests
{
    [Fact]
    public void Generic_attribute_form_should_thread_values_correctly()
    {
        var result = GenericAttrBuilder.WithValue(42).WithLabel("test").CreateGenericAttrTarget();

        result.Value.ShouldBe(42);
        result.Label.ShouldBe("test");
    }
}
