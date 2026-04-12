using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot(ReturnType = typeof(IMergeShape))]
internal partial class MergeShapeBuilder;

internal interface IMergeShape;

[FluentTarget<MergeShapeBuilder>]
internal record MergeSquare(int Width) : IMergeShape;

[FluentTarget<MergeShapeBuilder>]
internal record MergeRectangle(int Width, int Height) : IMergeShape;

[FluentTarget<MergeShapeBuilder>]
internal record MergeCuboid(int Width, int Height, int Depth) : IMergeShape;

[FluentTarget<MergeShapeBuilder>]
internal record MergeCircle(double Radius) : IMergeShape;

#endregion

public class MergeRuntimeTests
{
    [Fact]
    public void Shared_first_parameter_should_branch_to_single_param_target()
    {
        IMergeShape square = MergeShapeBuilder.WithWidth(10).CreateMergeSquare();

        var s = square.ShouldBeOfType<MergeSquare>();
        s.Width.ShouldBe(10);
    }

    [Fact]
    public void Shared_first_parameter_should_branch_to_two_param_target()
    {
        IMergeShape rect = MergeShapeBuilder.WithWidth(10).WithHeight(20).CreateMergeRectangle();

        var r = rect.ShouldBeOfType<MergeRectangle>();
        r.Width.ShouldBe(10);
        r.Height.ShouldBe(20);
    }

    [Fact]
    public void Shared_parameters_should_branch_to_three_param_target()
    {
        IMergeShape cuboid = MergeShapeBuilder.WithWidth(10).WithHeight(20).WithDepth(30).CreateMergeCuboid();

        var c = cuboid.ShouldBeOfType<MergeCuboid>();
        c.Width.ShouldBe(10);
        c.Height.ShouldBe(20);
        c.Depth.ShouldBe(30);
    }

    [Fact]
    public void Separate_entry_point_should_thread_value_to_target()
    {
        IMergeShape circle = MergeShapeBuilder.WithRadius(5.0).CreateMergeCircle();

        var c = circle.ShouldBeOfType<MergeCircle>();
        c.Radius.ShouldBe(5.0);
    }
}
