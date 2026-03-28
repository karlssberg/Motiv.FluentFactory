using System.Numerics;
using Converj.Attributes;

namespace Converj.Example;

[FluentFactory(CreateMethod = CreateMethod.None, MethodPrefix = "", )]
internal partial class Line;

[FluentConstructor<Line>]
internal partial record Line1D<[As("T")]TNum>(TNum X) where TNum : INumber<TNum>;

[FluentConstructor<Line>]
internal partial record Line2D<T>(T X, T Y) where T : INumber<T>;

[FluentConstructor<Line>]
internal partial record Line3D<T>(T X, T Y, T Z) where T : INumber<T>;
/