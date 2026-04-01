using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

internal interface IService;

internal record TestService(string Id) : IService;

[FluentRoot(BuilderMethod = BuilderMethod.FixedName)]
internal partial record ServiceFactoryForTest(IService Service);

[FluentTarget<ServiceFactoryForTest>]
internal record ServiceConsumer(IService Service, string Name);

[FluentRoot]
internal partial class FieldParameterFactory
{
    [FluentParameter]
    private readonly int _scale;

    public FieldParameterFactory(int scale)
    {
        _scale = scale;
    }
}

[FluentTarget<FieldParameterFactory>]
internal record ScaledItem(int Scale, string Label);

[FluentRoot]
internal partial class PropertyParameterFactory
{
    [FluentParameter]
    public string Prefix { get; }

    public PropertyParameterFactory(string prefix)
    {
        Prefix = prefix;
    }
}

[FluentTarget<PropertyParameterFactory>]
internal record PrefixedItem(string Prefix, int Order);

[FluentRoot]
internal partial class MultiFluentParamFactory
{
    [FluentParameter]
    public int X { get; }

    [FluentParameter]
    public int Y { get; }

    public MultiFluentParamFactory(int x, int y)
    {
        X = x;
        Y = y;
    }
}

[FluentTarget<MultiFluentParamFactory>]
internal record CoordinateItem(int X, int Y, string Label);

#endregion

public class FluentParameterRuntimeTests
{
    [Fact]
    public void Record_primary_constructor_parameter_should_be_threaded()
    {
        IService service = new TestService("svc-1");
        var factory = new ServiceFactoryForTest(service);

        var result = factory.WithName("consumer-1").Create();

        result.Service.ShouldBeSameAs(service);
        result.Name.ShouldBe("consumer-1");
    }

    [Fact]
    public void Field_fluent_parameter_should_be_threaded()
    {
        var factory = new FieldParameterFactory(5);

        var result = factory.WithLabel("item").CreateScaledItem();

        result.Scale.ShouldBe(5);
        result.Label.ShouldBe("item");
    }

    [Fact]
    public void Property_fluent_parameter_should_be_threaded()
    {
        var factory = new PropertyParameterFactory("pre");

        var result = factory.WithOrder(3).CreatePrefixedItem();

        result.Prefix.ShouldBe("pre");
        result.Order.ShouldBe(3);
    }

    [Fact]
    public void Multiple_fluent_parameters_should_all_be_threaded()
    {
        var factory = new MultiFluentParamFactory(10, 20);

        var result = factory.WithLabel("origin").CreateCoordinateItem();

        result.X.ShouldBe(10);
        result.Y.ShouldBe(20);
        result.Label.ShouldBe("origin");
    }

    [Fact]
    public void Different_factory_instances_should_thread_different_values()
    {
        var factory1 = new ServiceFactoryForTest(new TestService("svc-1"));
        var factory2 = new ServiceFactoryForTest(new TestService("svc-2"));

        var result1 = factory1.WithName("a").Create();
        var result2 = factory2.WithName("b").Create();

        ((TestService)result1.Service).Id.ShouldBe("svc-1");
        ((TestService)result2.Service).Id.ShouldBe("svc-2");
    }
}
