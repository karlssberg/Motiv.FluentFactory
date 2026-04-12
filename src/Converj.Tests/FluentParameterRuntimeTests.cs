using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

internal interface IService;

internal record TestService(string Id) : IService;

[FluentRoot(TerminalMethod = TerminalMethod.FixedName)]
internal partial record ServiceBuilderForTest(IService Service);

[FluentTarget<ServiceBuilderForTest>]
internal record ServiceConsumer(IService Service, string Name);

[FluentRoot]
internal partial class FieldParameterBuilder
{
    [FluentParameter]
    private readonly int _scale;

    public FieldParameterBuilder(int scale)
    {
        _scale = scale;
    }
}

[FluentTarget<FieldParameterBuilder>]
internal record ScaledItem(int Scale, string Label);

[FluentRoot]
internal partial class PropertyParameterBuilder
{
    [FluentParameter]
    public string Prefix { get; }

    public PropertyParameterBuilder(string prefix)
    {
        Prefix = prefix;
    }
}

[FluentTarget<PropertyParameterBuilder>]
internal record PrefixedItem(string Prefix, int Order);

[FluentRoot]
internal partial class MultiFluentParamBuilder
{
    [FluentParameter]
    public int X { get; }

    [FluentParameter]
    public int Y { get; }

    public MultiFluentParamBuilder(int x, int y)
    {
        X = x;
        Y = y;
    }
}

[FluentTarget<MultiFluentParamBuilder>]
internal record CoordinateItem(int X, int Y, string Label);

#endregion

public class FluentParameterRuntimeTests
{
    [Fact]
    public void Record_primary_constructor_parameter_should_be_threaded()
    {
        IService service = new TestService("svc-1");
        var builder = new ServiceBuilderForTest(service);

        var result = builder.WithName("consumer-1").Create();

        result.Service.ShouldBeSameAs(service);
        result.Name.ShouldBe("consumer-1");
    }

    [Fact]
    public void Field_fluent_parameter_should_be_threaded()
    {
        var builder = new FieldParameterBuilder(5);

        var result = builder.WithLabel("item").CreateScaledItem();

        result.Scale.ShouldBe(5);
        result.Label.ShouldBe("item");
    }

    [Fact]
    public void Property_fluent_parameter_should_be_threaded()
    {
        var builder = new PropertyParameterBuilder("pre");

        var result = builder.WithOrder(3).CreatePrefixedItem();

        result.Prefix.ShouldBe("pre");
        result.Order.ShouldBe(3);
    }

    [Fact]
    public void Multiple_fluent_parameters_should_all_be_threaded()
    {
        var builder = new MultiFluentParamBuilder(10, 20);

        var result = builder.WithLabel("origin").CreateCoordinateItem();

        result.X.ShouldBe(10);
        result.Y.ShouldBe(20);
        result.Label.ShouldBe("origin");
    }

    [Fact]
    public void Different_builder_instances_should_thread_different_values()
    {
        var builder1 = new ServiceBuilderForTest(new TestService("svc-1"));
        var builder2 = new ServiceBuilderForTest(new TestService("svc-2"));

        var result1 = builder1.WithName("a").Create();
        var result2 = builder2.WithName("b").Create();

        ((TestService)result1.Service).Id.ShouldBe("svc-1");
        ((TestService)result2.Service).Id.ShouldBe("svc-2");
    }
}
