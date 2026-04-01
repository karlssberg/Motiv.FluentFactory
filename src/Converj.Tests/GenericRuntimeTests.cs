using System.Numerics;
using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal static partial class GenericFactory;

[FluentTarget(typeof(GenericFactory))]
internal record GenericTarget<T>(T Value);

[FluentRoot]
internal static partial class ConstrainedGenericFactory;

[FluentTarget(typeof(ConstrainedGenericFactory))]
internal record NumericTarget<T>(T X, T Y) where T : INumber<T>;

[FluentRoot]
internal static partial class MultiGenericFactory;

[FluentTarget(typeof(MultiGenericFactory))]
internal record PairTarget<TKey, TValue>(TKey Key, TValue Value);

#endregion

public class GenericRuntimeTests
{
    [Fact]
    public void Generic_parameter_should_thread_value_with_int()
    {
        var result = GenericFactory.WithValue(42).CreateGenericTarget();

        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Generic_parameter_should_thread_value_with_string()
    {
        var result = GenericFactory.WithValue("hello").CreateGenericTarget();

        result.Value.ShouldBe("hello");
    }

    [Fact]
    public void Constrained_generic_should_thread_values_with_int()
    {
        var result = ConstrainedGenericFactory.WithX(10).WithY(20).CreateNumericTarget();

        result.X.ShouldBe(10);
        result.Y.ShouldBe(20);
    }

    [Fact]
    public void Constrained_generic_should_thread_values_with_double()
    {
        var result = ConstrainedGenericFactory.WithX(1.5).WithY(2.5).CreateNumericTarget();

        result.X.ShouldBe(1.5);
        result.Y.ShouldBe(2.5);
    }

    [Fact]
    public void Multiple_generic_parameters_should_thread_values()
    {
        var result = MultiGenericFactory.WithKey("name").WithValue(42).CreatePairTarget();

        result.Key.ShouldBe("name");
        result.Value.ShouldBe(42);
    }
}
