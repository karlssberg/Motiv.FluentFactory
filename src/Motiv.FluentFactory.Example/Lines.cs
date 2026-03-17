using System.Numerics;
using Motiv.FluentFactory.Attributes;

namespace Motiv.FluentFactory.Example;

interface ILine;

[FluentFactory(CreateMethod = CreateMethod.None, MethodPrefix = "")]
internal static partial class Line;

[FluentConstructor(typeof(Line))]
internal partial record Line1D<T>(T X) where T : INumber<T>;

[FluentConstructor(typeof(Line))]
internal partial record Line2D<T>(T X, T Y) where T : INumber<T>;

[FluentConstructor(typeof(Line))]
internal partial record Line3D<T>(T X, T Y, T Z) where T : INumber<T>;
