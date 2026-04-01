using System.Numerics;
using Converj.Attributes;

namespace Converj.Example;

[FluentRoot(BuilderMethod = BuilderMethod.None, MethodPrefix = "")]
internal partial class Line;

[FluentTarget<Line>]
internal partial record Line1D<[As("T")]TNum>(TNum X) where TNum : INumber<TNum>;

[FluentTarget<Line>]
internal partial record Line2D<T>(T X, T Y) where T : INumber<T>;

[FluentTarget<Line>]
internal partial record Line3D<T>(T X, T Y, T Z) where T : INumber<T>;
