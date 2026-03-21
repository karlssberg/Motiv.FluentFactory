using Converg.Example;
using Line = Converg.Example.Line;
using Shape = Converg.Example.Shape;

// Examples of consuming the generated code

Square<decimal> square1 = Square<decimal>.WithWidth(10).Create();
Rectangle<int> rectangle1 = Rectangle<int>.WithWidth(10).WithHeight(20).Create();
Circle<double> circle1 = Circle<double>.WithRadius(5).Create();
Cuboid<long> cuboid1 = Cuboid<long>.WithWidth(10).WithHeight(20).WithDepth(30).Create();

Square<int> square2 = Shape<int>.Width(10).CreateSquare();
Rectangle<decimal> rectangle2 = Shape<decimal>.Width(10).Height(20).CreateRectangle();
Diamond<double> diamond2 = Shape<double>.Width(10).Height(20).CreateDiamond();
Cuboid<float> cuboid2 = Shape<float>.Width(10).Height(20).Depth(30).CreateCuboid();
Circle<long> circle2 = Shape<long>.Radius(5).CreateCircle();

Square<int> square3 = Shape.Width(10).CreateSquare();
Rectangle<decimal> rectangle3 = Shape.Width(10m).Height(20m).CreateRectangle();
Diamond<double> diamond3 = Shape.Width(10d).Height(20d).CreateDiamond();
Cuboid<float> cuboid3 = Shape.Width(10f).Height(20f).Depth(30f).CreateCuboid();
Circle<long> circle3 = Shape.Radius(5L).CreateCircle();

Line1D<decimal> line1D = Line.X(10m);
Line2D<int> line2D = Line.X(10).Y(20);
Line3D<double> line3D = Line.X(10d).Y(20d).Z(30d);

Animal dog = Animal.CreateDog();
Animal cat = Animal.CreateCat();
Animal fish = Animal.WithEyeCount(2).CreateFish();