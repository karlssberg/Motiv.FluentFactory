using System.Drawing;
using System.Numerics;
using Converj.Attributes;

namespace Converj.Example;

internal static class ShapeExtensionsDemo
{
    internal static void Invoke()
    {
        var diamond = Shape.Width(10).Height(20).CreateDiamond();
        var coloredDiamond = diamond.WithColor(Color.Red).ToColoredDiamond();
        var circle = coloredDiamond.ConvertToCircle();
        var coloredCircle = diamond.WithColor(Color.Green).ToColoredCircle();
    }
}

[FluentRoot(TerminalMethod = TerminalMethod.FixedName)]
internal static partial class ShapeExtensions;

internal static class ShapeExtensionMethods
{
    [FluentTarget(typeof(ShapeExtensions), TerminalVerb = "ToColoredDiamond")]
    internal record ColoredDiamond<T>([This] Diamond<T> Diamond, Color Color) where T : INumber<T>;

    [FluentTarget(typeof(ShapeExtensions), TerminalVerb = "ToColoredCircle")]
    internal record ColoredCircle<T>([This] Diamond<T> Diamond, Color Color)
        : Circle<T>(Diamond.Width) where T : INumber<T>;

    [FluentTarget(typeof(ShapeExtensions))]
    internal static Circle<T> ConvertToCircle<T>([This] ColoredDiamond<T> coloredDiamond) where T : INumber<T> => 
        new(coloredDiamond.Diamond.Width);
}
