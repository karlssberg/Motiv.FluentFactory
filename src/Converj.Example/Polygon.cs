using System.Numerics;
using Converj.Attributes;

namespace Converj.Example;

[FluentFactory]
internal partial class Polygon<T>(T scale) where T : INumber<T>
{
    [FluentParameter]
    protected T Scale { get; set; } = scale;
}

[FluentFactory]
internal static partial class Point;

[FluentConstructor(typeof(Point), MethodPrefix = "", CreateMethod = CreateMethod.None)]
internal record Point<T>(T X, T Y, T Z) where T : INumber<T>;

[FluentConstructor(typeof(Polygon<>))]
internal record Triangle<T>(T Scale, Point<T> Point1, Point<T> Point2, Point<T> Point3) where T : INumber<T>;

internal class Test
{
    public void TestMethod()
    {
        var point1 = Point.X(1.0).Y(2.0).Z(3.0);
        var point2 = Point.X(2.0).Y(4.0).Z(3.0);
        var point3 = Point.X(3.0).Y(8.0).Z(3.0);
        
        new Polygon<double>(1.0)
            .WithPoint1(point1)
            .WithPoint2(point2)
            .WithPoint3(point3)
            .CreateTriangle();
    }
}