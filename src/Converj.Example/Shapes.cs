using System.Numerics;
using Converj.Attributes;

namespace Converj.Example;


[FluentFactory(MethodPrefix = "")]
internal partial class Shape<T> where T : INumber<T>;

[FluentFactory(MethodPrefix = "")]
internal partial class Shape;

[FluentFactory(CreateMethod = CreateMethod.Fixed)]
[FluentConstructor(typeof(Square<>))]
[FluentConstructor(typeof(Shape))]
[FluentConstructor(typeof(Shape<>))]
internal partial record Square<[As("T")] TUnit>(TUnit Width) where TUnit : INumber<TUnit>;

[FluentFactory(CreateMethod = CreateMethod.Fixed)]
[FluentConstructor(typeof(Rectangle<>))]
[FluentConstructor(typeof(Shape))]
[FluentConstructor(typeof(Shape<>))]
internal partial record Rectangle<T>(T Width, T Height) where T : INumber<T>;

[FluentFactory(CreateMethod = CreateMethod.Fixed)]
[FluentConstructor(typeof(Circle<>))]
[FluentConstructor(typeof(Shape))]
[FluentConstructor(typeof(Shape<>))]
internal partial record Circle<T>(T Radius) where T : INumber<T>;

[FluentFactory(CreateMethod = CreateMethod.Fixed)]
[FluentConstructor(typeof(Diamond<>))]
[FluentConstructor(typeof(Shape))]
[FluentConstructor(typeof(Shape<>))]
internal partial record Diamond<T>(T Width, T Height) where T : INumber<T>;

[FluentFactory]
[FluentConstructor(typeof(Cuboid<>), CreateMethod = CreateMethod.Fixed)]
[FluentConstructor(typeof(Shape))]
[FluentConstructor(typeof(Shape<>))]
internal partial record Cuboid<T>(T Width, T Height, T Depth) where T : INumber<T>;
