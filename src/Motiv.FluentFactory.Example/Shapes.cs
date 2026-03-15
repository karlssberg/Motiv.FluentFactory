using System.Numerics;
using Motiv.FluentFactory.Attributes;

namespace Motiv.FluentFactory.Example;

[FluentFactory]
internal partial class Shape<T> where T : INumber<T>;

[FluentFactory]
internal partial class Shape;

[FluentFactory]
[FluentConstructor(typeof(Square<>), CreateMethod = CreateMethod.Fixed)]
[FluentConstructor(typeof(Shape))]
[FluentConstructor(typeof(Shape<>))]
internal partial record Square<T>(T Width) where T : INumber<T>;

[FluentFactory]
[FluentConstructor(typeof(Rectangle<>), CreateMethod = CreateMethod.Fixed)]
[FluentConstructor(typeof(Shape))]
[FluentConstructor(typeof(Shape<>))]
internal partial record Rectangle<T>(T Width, T Height) where T : INumber<T>;

[FluentFactory]
[FluentConstructor(typeof(Circle<>), CreateMethod = CreateMethod.Fixed)]
[FluentConstructor(typeof(Shape))]
[FluentConstructor(typeof(Shape<>))]
internal partial record Circle<T>(T Radius) where T : INumber<T>;

[FluentFactory]
[FluentConstructor(typeof(Diamond<>), CreateMethod = CreateMethod.Fixed)]
[FluentConstructor(typeof(Shape))]
[FluentConstructor(typeof(Shape<>))]
internal partial record Diamond<T>(T Width, T Height) where T : INumber<T>;

[FluentFactory]
[FluentConstructor(typeof(Cuboid<>), CreateMethod = CreateMethod.Fixed)]
[FluentConstructor(typeof(Shape))]
[FluentConstructor(typeof(Shape<>))]
internal partial record Cuboid<T>(T Width, T Height, T Depth) where T : INumber<T>;
