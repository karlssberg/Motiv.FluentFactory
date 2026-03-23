using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentFactory]
internal partial class GenericAttrFactory;

[FluentConstructor<GenericAttrFactory>]
internal record GenericAttrTarget(int Value, string Label);

#endregion

public class GenericAttributeRuntimeTests
{
    [Fact]
    public void Generic_attribute_form_should_thread_values_correctly()
    {
        var result = GenericAttrFactory.WithValue(42).WithLabel("test").CreateGenericAttrTarget();

        result.Value.ShouldBe(42);
        result.Label.ShouldBe("test");
    }
}
