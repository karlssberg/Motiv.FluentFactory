using Line = Motiv.FluentFactory.Example.Line;
using Shape = Motiv.FluentFactory.Example.Shape;

// Examples of consuming the generated code

Motiv.FluentFactory.Example.Rectangle<int>.WithWidth(10).WithHeight(20).Create();
Motiv.FluentFactory.Example.Square<decimal>.WithWidth(10).Create();
Motiv.FluentFactory.Example.Circle<double>.WithRadius(5).Create();
Motiv.FluentFactory.Example.Cuboid<long>.WithWidth(10).WithHeight(20).WithDepth(30).Create();

Motiv.FluentFactory.Example.Shape<int>.WithWidth(10).CreateSquare();
Motiv.FluentFactory.Example.Shape<decimal>.WithWidth(10).WithHeight(20).CreateRectangle();
Motiv.FluentFactory.Example.Shape<double>.WithWidth(10).WithHeight(20).CreateDiamond();
Motiv.FluentFactory.Example.Shape<float>.WithWidth(10).WithHeight(20).WithDepth(30).CreateCuboid();
Motiv.FluentFactory.Example.Shape<long>.WithRadius(5).CreateCircle();

Shape.WithWidth(10).CreateSquare();
Shape.WithWidth(10m).WithHeight(20m).CreateRectangle();
Shape.WithWidth(10d).WithHeight(20d).CreateDiamond();
Shape.WithWidth(10f).WithHeight(20f).WithDepth(30f).CreateCuboid();
Shape.WithRadius(5L).CreateCircle();

Line.X(10).Y(20).Create();
Line.X(10d).Y(20d).Z(30d).Create();
