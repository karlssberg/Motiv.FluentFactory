using System.Numerics;
using Motiv.FluentFactory.Attributes;

namespace Motiv.FluentFactory.Example;

[FluentFactory]
internal static partial class Line;

[FluentConstructor(typeof(Line))]
internal partial record Line2D<T>(
    [FluentMethod("X")]T X,
    [FluentMethod("Y")]T Y)
    where T : INumber<T>;

[FluentConstructor(typeof(Line))]
internal partial record Line3D<T>(
    [FluentMethod("X")]T X,
    [FluentMethod("Y")]T Y,
    [FluentMethod("Z")]T Z)
    where T : INumber<T>;
