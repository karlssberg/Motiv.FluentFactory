using System.ComponentModel.DataAnnotations;
using System.Numerics;
using Converj.Attributes;
using static Converj.Example.Point;

namespace Converj.Example;

[FluentRoot]
internal partial class Polygon<T> where T : INumber<T>
{
    [FluentParameter]
    protected T Scale { get; set; }

    public Polygon(T scale)
    {
        Scale = scale;
    }
}

[FluentRoot]
internal static partial class Point;

[FluentTarget(typeof(Point), MethodPrefix = "", TerminalMethod = TerminalMethod.None)]
internal record Point<T>(T X, T Y, T Z) where T : INumber<T>;

[FluentTarget(typeof(Polygon<>))]
internal record Triangle<T>(T Scale) where T : INumber<T>
{
    [Required]
    [FluentMethod]
    public Point<T> Point1 { get; init; } = null!;
    public required Point<T> Point2 { get; init; }
    
    [FluentMethod]
    public Point<T>? Point3 { get; init; }
}

internal class Test
{
    public void TestMethod()
    {
        new Polygon<double>(1.0)
            .WithPoint1(X(1.0).Y(2.0).Z(3.0))
            .WithPoint2(X(2.0).Y(4.0).Z(3.0))
            .WithPoint3(X(3.0).Y(8.0).Z(3.0))
            .CreateTriangle();
        
        new Polygon<double>(1.0)
            .WithPoint1(X(1.0).Y(2.0).Z(3.0))
            .WithPoint2(X(2.0).Y(4.0).Z(3.0))
            .CreateTriangle();
    }
}