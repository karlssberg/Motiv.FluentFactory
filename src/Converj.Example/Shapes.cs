using System.Numerics;
using Converj.Attributes;

namespace Converj.Example;


[FluentRoot(MethodPrefix = "")]
internal partial class Shape<T> where T : INumber<T>;

[FluentRoot(MethodPrefix = "")]
internal partial class Shape;

[FluentRoot(BuilderMethod = BuilderMethod.FixedName)]
[FluentTarget(typeof(Square<>))]
[FluentTarget<Shape>]
[FluentTarget(typeof(Shape<>))]
internal partial record Square<[As("T")] TUnit>(TUnit Width) where TUnit : INumber<TUnit>;

[FluentRoot(BuilderMethod = BuilderMethod.FixedName)]
[FluentTarget(typeof(Rectangle<>))]
[FluentTarget<Shape>]
[FluentTarget(typeof(Shape<>))]
internal partial record Rectangle<T>(T Width, T Height) where T : INumber<T>;

[FluentRoot(BuilderMethod = BuilderMethod.FixedName)]
[FluentTarget(typeof(Circle<>))]
[FluentTarget<Shape>]
[FluentTarget(typeof(Shape<>))]
internal partial record Circle<T>(T Radius) where T : INumber<T>;

[FluentRoot(BuilderMethod = BuilderMethod.FixedName)]
[FluentTarget(typeof(Diamond<>))]
[FluentTarget<Shape>]
[FluentTarget(typeof(Shape<>))]
internal partial record Diamond<T>(T Width, T Height) where T : INumber<T>;

[FluentRoot]
[FluentTarget(typeof(Cuboid<>), BuilderMethod = BuilderMethod.FixedName)]
[FluentTarget<Shape>]
[FluentTarget(typeof(Shape<>))]
internal partial record Cuboid<T>(T Width, T Height, T Depth) where T : INumber<T>;
