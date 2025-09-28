using System.Numerics;
using Motiv.FluentFactory.Attributes;

namespace Motiv.FluentFactory.Example;

[FluentFactory]
internal partial class Shape<T> where T : INumber<T>;

[FluentFactory]
internal partial class Shape;

[FluentFactory]
[FluentConstructor(typeof(Square<>))]
[FluentConstructor(typeof(Shape), CreateMethodName = "CreateSquare")]
[FluentConstructor(typeof(Shape<>), CreateMethodName = "CreateSquare")]
internal partial record Square<T>(T Width) where T : INumber<T>;

[FluentFactory]
[FluentConstructor(typeof(Rectangle<>))]
[FluentConstructor(typeof(Shape), CreateMethodName = "CreateRectangle")]
[FluentConstructor(typeof(Shape<>), CreateMethodName = "CreateRectangle")]
internal partial record Rectangle<T>(T Width, T Height) where T : INumber<T>;

[FluentFactory]
[FluentConstructor(typeof(Circle<>))]
[FluentConstructor(typeof(Shape), CreateMethodName = "CreateCircle")]
[FluentConstructor(typeof(Shape<>), CreateMethodName = "CreateCircle")]
internal partial record Circle<T>(T Radius) where T : INumber<T>;

[FluentFactory]
[FluentConstructor(typeof(Diamond<>))]
[FluentConstructor(typeof(Shape), CreateMethodName = "CreateDiamond")]
[FluentConstructor(typeof(Shape<>), CreateMethodName = "CreateDiamond")]
internal partial record Diamond<T>(T Width, T Height) where T : INumber<T>;

[FluentFactory]
[FluentConstructor(typeof(Cuboid<>))]
[FluentConstructor(typeof(Shape), CreateMethodName = "CreateCuboid")]
[FluentConstructor(typeof(Shape<>), CreateMethodName = "CreateCuboid")]
internal partial record Cuboid<T>(T Width, T Height, T Depth) where T : INumber<T>;
