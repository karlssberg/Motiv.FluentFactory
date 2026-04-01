using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal partial class OptionalParamFactory;

[FluentTarget<OptionalParamFactory>]
internal record OptionalTarget(string Name, int Count = 10);

[FluentRoot]
internal partial class AllOptionalFactory;

[FluentTarget<AllOptionalFactory>]
internal record AllOptionalTarget(int X = 1, int Y = 2, int Z = 3);

#endregion

public class OptionalParameterRuntimeTests
{
    [Fact]
    public void Optional_parameter_should_use_default_when_not_specified()
    {
        var result = OptionalParamFactory.WithName("test").CreateOptionalTarget();

        result.Name.ShouldBe("test");
        result.Count.ShouldBe(10);
    }

    [Fact]
    public void Optional_parameter_should_use_provided_value_when_specified()
    {
        var result = OptionalParamFactory.WithName("test").WithCount(99).CreateOptionalTarget();

        result.Name.ShouldBe("test");
        result.Count.ShouldBe(99);
    }

    [Fact]
    public void All_optional_parameters_should_use_defaults_when_not_specified()
    {
        var result = AllOptionalFactory.CreateAllOptionalTarget();

        result.X.ShouldBe(1);
        result.Y.ShouldBe(2);
        result.Z.ShouldBe(3);
    }

    [Fact]
    public void All_optional_parameters_should_accept_overrides()
    {
        var result = AllOptionalFactory.WithX(10).WithY(20).WithZ(30).CreateAllOptionalTarget();

        result.X.ShouldBe(10);
        result.Y.ShouldBe(20);
        result.Z.ShouldBe(30);
    }

    [Fact]
    public void Partial_optional_override_should_preserve_other_defaults()
    {
        var result = AllOptionalFactory.WithY(50).CreateAllOptionalTarget();

        result.X.ShouldBe(1);
        result.Y.ShouldBe(50);
        result.Z.ShouldBe(3);
    }
}
