using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentFactory(ReturnType = typeof(IMergeShape))]
internal partial class MergeShapeFactory;

internal interface IMergeShape;

[FluentConstructor<MergeShapeFactory>]
internal record MergeSquare(int Width) : IMergeShape;

[FluentConstructor<MergeShapeFactory>]
internal record MergeRectangle(int Width, int Height) : IMergeShape;

[FluentConstructor<MergeShapeFactory>]
internal record MergeCuboid(int Width, int Height, int Depth) : IMergeShape;

[FluentConstructor<MergeShapeFactory>]
internal record MergeCircle(double Radius) : IMergeShape;

#endregion

public class MergeRuntimeTests
{
    [Fact]
    public void Shared_first_parameter_should_branch_to_single_param_target()
    {
        IMergeShape square = MergeShapeFactory.WithWidth(10).CreateMergeSquare();

        var s = square.ShouldBeOfType<MergeSquare>();
        s.Width.ShouldBe(10);
    }

    [Fact]
    public void Shared_first_parameter_should_branch_to_two_param_target()
    {
        IMergeShape rect = MergeShapeFactory.WithWidth(10).WithHeight(20).CreateMergeRectangle();

        var r = rect.ShouldBeOfType<MergeRectangle>();
        r.Width.ShouldBe(10);
        r.Height.ShouldBe(20);
    }

    [Fact]
    public void Shared_parameters_should_branch_to_three_param_target()
    {
        IMergeShape cuboid = MergeShapeFactory.WithWidth(10).WithHeight(20).WithDepth(30).CreateMergeCuboid();

        var c = cuboid.ShouldBeOfType<MergeCuboid>();
        c.Width.ShouldBe(10);
        c.Height.ShouldBe(20);
        c.Depth.ShouldBe(30);
    }

    [Fact]
    public void Separate_entry_point_should_thread_value_to_target()
    {
        IMergeShape circle = MergeShapeFactory.WithRadius(5.0).CreateMergeCircle();

        var c = circle.ShouldBeOfType<MergeCircle>();
        c.Radius.ShouldBe(5.0);
    }
}
