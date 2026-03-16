using System.Numerics;
using Motiv.FluentFactory.Attributes;

namespace Motiv.FluentFactory.Example;

[FluentFactory(CreateMethod = CreateMethod.Fixed)]
internal static partial class Line;

[FluentConstructor(typeof(Line), CreateVerb = "Create2D")]
internal partial record Line2D<T>(
    [FluentMethod("X")]T X,
    [FluentMethod("Y")]T Y)
    where T : INumber<T>;

[FluentConstructor(typeof(Line), CreateVerb = "Create3D")]
internal partial record Line3D<T>(
    [FluentMethod("X")]T X,
    [FluentMethod("Y")]T Y,
    [FluentMethod("Z")]T Z)
    where T : INumber<T>;
