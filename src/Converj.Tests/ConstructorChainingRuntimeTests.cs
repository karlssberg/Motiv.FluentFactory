using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal partial class ChainingFactory;

[FluentTarget<ChainingFactory>]
internal class ChainingTarget
{
    public int X { get; }
    public int Y { get; }
    public int Z { get; }

    public ChainingTarget(int x, int y) : this(x, y, 0)
    {
    }

    public ChainingTarget(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

#endregion

public class ConstructorChainingRuntimeTests
{
    [Fact]
    public void Chained_constructor_should_thread_values_correctly()
    {
        var result = ChainingFactory.WithX(1).WithY(2).CreateChainingTarget();

        result.X.ShouldBe(1);
        result.Y.ShouldBe(2);
        result.Z.ShouldBe(0);
    }
}
