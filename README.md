# Converj

A Roslyn source generator that turns constructors into compile-time fluent builders 
— no boilerplate, no runtime cost.

Annotate a constructor with `[FluentConstructor]`,
and the generator produces a chain of fluent methods that mirror the sequence of constructor parameters.
Required parameters are enforced, optional parameters stay optional, 
and the whole chain compiles down to zero-allocation, JIT optimizer-friendly code.

```csharp
// You write this:
[FluentFactory]
public partial class Shape;

[FluentConstructor<Shape>]
public record Square<T>(T Width);

[FluentConstructor<Shape>] 
public record Rectangle<T>(T Width, T Height);

[FluentConstructor<Shape>] 
public record Cube<T>(T Width, T Height, T Depth);

// You get this:
Square<int>       square = Shape.WithWidth(10).CreateSquare();
Rectangle<int> rectangle = Shape.WithWidth(10).WithHeight(20).CreateRectangle();
Cube<int>           cube = Shape.WithWidth(10).WithHeight(20).WithDepth(30).CreateCube();
```

Supports generics, records, primary constructors, custom method names, multiple overloads, return-type covariance, generic type parameter aliasing, and more.


## Installation

Install the NuGet package in your project:

```xml
<PackageReference Include="Converj" Version="1.0.0" />
```

Or via Package Manager Console:

```powershell
Install-Package Converj
```

## Tutorial

### Basic Fluent Factory

The simplest fluent factory requires two attributes:

1. `[FluentFactory]` - marks a partial class as the entry point for fluent methods
2. `[FluentConstructor]` - marks a constructor to generate fluent methods for using the constructor parameters as the fluent steps

```csharp
[FluentFactory]
public static partial class BookFactory;

public class Book
{
    [FluentConstructor(typeof(BookFactory))]
    public Book(string title)
    {
        Title = title;
    }

    public string Title { get; }
}

// Usage:
var book = BookFactory.WithTitle("The C# Guide").CreateBook();
```

**Generated Code:**
```csharp
    public static partial class BookFactory
    {
        public static Step_0__BookFactory WithTitle(in string title)
        {
            return new Step_0__BookFactory(title);
        }
    }

    public readonly struct Step_0__BookFactory
    {
        private readonly string _title__parameter;
        internal Step_0__BookFactory(in string title)
        {
            this._title__parameter = title;
        }

        public Book CreateBook()
        {
            return new Book(this._title__parameter);
        }
    }
```

### Multiple Parameters - Chained Steps

With multiple parameters, each becomes a separate step in the fluent chain:

```csharp
[FluentFactory]
public static partial class ProductFactory;

public class Product
{
    [FluentConstructor(typeof(ProductFactory))]
    public Product(string name, decimal price, int stock)
    {
        Name = name;
        Price = price;
        Stock = stock;
    }

    public string Name { get; }
    public decimal Price { get; }
    public int Stock { get; }
}

// Usage - each step is required and enforced by the compiler:
var product = ProductFactory
    .WithName("Laptop")
    .WithPrice(999.99m)
    .WithStock(50)
    .CreateProduct();
```

### Naming Create Methods

By default, the create method name includes the target type name (Dynamic mode): `CreateCar()`, `CreateAddress()`, etc. This automatically disambiguates when a factory serves multiple types.

Customize the create verb using the `CreateVerb` parameter:

```csharp
[FluentFactory]
public static partial class VehicleFactory;

public class Car
{
    [FluentConstructor(typeof(VehicleFactory), CreateMethod = CreateMethod.Fixed, CreateVerb = "BuildCar")]
    public Car(string make, string model)
    {
        Make = make;
        Model = model;
    }

    public string Make { get; }
    public string Model { get; }
}

// Usage with custom create method name:
var car = VehicleFactory
    .WithMake("Toyota")
    .WithModel("Camry")
    .BuildCar(); // Fixed method name
```

### Skipping the Create() Method

Use `CreateMethod = CreateMethod.None` to eliminate the final Create call:

```csharp
public class Address
{
    [FluentConstructor(typeof(AddressFactory), CreateMethod = CreateMethod.None)]
    public Address(string street, string city)
    {
        Street = street;
        City = city;
    }

    public string Street { get; }
    public string City { get; }
}

// Usage - no Create() needed:
var address = AddressFactory
    .WithStreet("123 Main St")
    .WithCity("New York");
```

**Note:** You cannot use `CreateVerb` or `ReturnType` together with `CreateMethod.None` as there would be no create method to name or type.

### Custom Method Names

Customize method names using `[FluentMethod]`:

```csharp
public class User
{
    [FluentConstructor(typeof(UserFactory))]
    public User(
        [FluentMethod("SetName")] string name,
        [FluentMethod("SetEmail")] string email)
    {
        Name = name;
        Email = email;
    }

    public string Name { get; }
    public string Email { get; }
}

// Usage with custom method names:
var user = UserFactory
    .SetName("Alice")
    .SetEmail("alice@example.com")
    .CreateUser();
```

### Method Prefix

By default, fluent methods are prefixed with `With` (e.g., `WithName`, `WithAge`). You can customize this with the `MethodPrefix` property on either `[FluentFactory]` or `[FluentConstructor]`:

```csharp
// Factory-level prefix applies to all constructors
[FluentFactory(MethodPrefix = "Having")]
public static partial class ConfigFactory;

public class Config
{
    [FluentConstructor(typeof(ConfigFactory))]
    public Config(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public string Host { get; }
    public int Port { get; }
}

// Usage:
var config = ConfigFactory
    .HavingHost("localhost")
    .HavingPort(8080)
    .CreateConfig();
```

Use an empty string to produce bare parameter names:

```csharp
[FluentFactory(MethodPrefix = "")]
public static partial class PointFactory;

public class Point
{
    [FluentConstructor(typeof(PointFactory))]
    public Point(int x, int y) { X = x; Y = y; }

    public int X { get; }
    public int Y { get; }
}

// Usage with bare names:
var point = PointFactory.X(10).Y(20).CreatePoint();
```

Constructor-level `MethodPrefix` overrides the factory-level default:

```csharp
[FluentFactory(MethodPrefix = "With")]
public static partial class ShapeFactory;

public class Circle
{
    [FluentConstructor(typeof(ShapeFactory), MethodPrefix = "Having")]
    public Circle(double radius) { Radius = radius; }

    public double Radius { get; }
}

// Uses "Having" from the constructor override:
var circle = ShapeFactory.HavingRadius(5.0).CreateCircle();
```

### Return Type

By default, creation methods return the concrete target type. Use `ReturnType` to return an interface or base class instead:

```csharp
public interface IAnimal
{
    string Name { get; }
}

[FluentFactory]
public static partial class AnimalFactory;

public class Dog : IAnimal
{
    [FluentConstructor(typeof(AnimalFactory), ReturnType = typeof(IAnimal))]
    public Dog(string name) { Name = name; }

    public string Name { get; }
}

public class Cat : IAnimal
{
    [FluentConstructor(typeof(AnimalFactory), ReturnType = typeof(IAnimal))]
    public Cat(string name) { Name = name; }

    public string Name { get; }
}

// Creation methods return the interface type:
IAnimal dog = AnimalFactory.WithName("Rex").CreateDog();
IAnimal cat = AnimalFactory.WithName("Whiskers").CreateCat();
```

You can also set `ReturnType` at the factory level to apply it to all constructors:

```csharp
[FluentFactory(ReturnType = typeof(IAnimal))]
public static partial class AnimalFactory;
```

**Note:** The target type must be assignable to the return type (i.e., it must implement the interface or extend the base class).

### Optional Parameters and Default Values

Parameters with default values become optional setter methods. They can be called in any order and are not required:

```csharp
[FluentFactory]
public static partial class ConnectionFactory;

public class Connection
{
    [FluentConstructor(typeof(ConnectionFactory))]
    public Connection(string host, int port = 443, int timeout = 30, bool useSsl = true)
    {
        Host = host;
        Port = port;
        Timeout = timeout;
        UseSsl = useSsl;
    }

    public string Host { get; }
    public int Port { get; }
    public int Timeout { get; }
    public bool UseSsl { get; }
}

// Required parameters are chained first, then optional ones can be set in any order:
var conn = ConnectionFactory
    .WithHost("example.com")
    .WithPort(8080)         // optional - override default
    .WithTimeout(60)        // optional - override default
    .CreateConnection();

// Or skip optional parameters entirely to use defaults:
var defaultConn = ConnectionFactory
    .WithHost("example.com")
    .CreateConnection();    // port=443, timeout=30, useSsl=true
```

### Working with Generics

The generator handles generic types seamlessly:

```csharp
[FluentFactory]
public static partial class ContainerFactory;

public class Container<T>
{
    [FluentConstructor(typeof(ContainerFactory))]
    public Container(T value, string label)
    {
        Value = value;
        Label = label;
    }

    public T Value { get; }
    public string Label { get; }
}

// Usage with type inference:
var intContainer = ContainerFactory
    .WithValue(42)
    .WithLabel("Number")
    .CreateContainer();

var stringContainer = ContainerFactory
    .WithValue("Hello")
    .WithLabel("Greeting")
    .CreateContainer();
```

### Records and Primary Constructors

The generator supports records with positional parameters and C# 12+ primary constructors:

```csharp
[FluentFactory]
public static partial class UserFactory;

// Record with positional parameters
public record User
{
    [FluentConstructor(typeof(UserFactory))]
    public User(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public string Name { get; }
    public string Email { get; }
}

var user = UserFactory
    .WithName("Alice")
    .WithEmail("alice@example.com")
    .CreateUser();
```

### Generic Attribute Syntax (C# 11+)

Instead of passing `typeof(...)`, you can use the generic `FluentConstructor<T>` syntax:

```csharp
[FluentFactory]
public static partial class UserFactory;

public class User
{
    // Generic syntax - cleaner and type-safe
    [FluentConstructor<UserFactory>]
    public User(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public string Name { get; }
    public string Email { get; }
}

// Usage is identical:
var user = UserFactory.WithName("Alice").WithEmail("alice@example.com").CreateUser();
```

The generic form also works at the type level:

```csharp
[FluentConstructor<UserFactory>]
public record User(string Name, string Email);
```

### FluentConstructor on Types

`[FluentConstructor]` can be applied directly to classes, structs, and records (not just constructors). This is especially useful with records that use positional parameters:

```csharp
[FluentFactory]
public static partial class Line;

[FluentConstructor(typeof(Line), CreateMethod = CreateMethod.None)]
public partial record Line1D([FluentMethod("X")] int X);

[FluentConstructor(typeof(Line), CreateMethod = CreateMethod.None)]
public partial record Line2D([FluentMethod("X")] int X, [FluentMethod("Y")] int Y);

[FluentConstructor(typeof(Line), CreateMethod = CreateMethod.None)]
public partial record Line3D([FluentMethod("X")] int X, [FluentMethod("Y")] int Y, [FluentMethod("Z")] int Z);
```

### Advanced: Custom Partial Types as Fluent Steps

When using `CreateMethod.None`, you can create custom partial types that function as both fluent steps and construction targets. This advanced pattern allows you to build complex fluent chains where each type can be both an intermediate step and a final result:

```csharp
[FluentFactory(CreateMethod = CreateMethod.None, MethodPrefix = "")]
public partial class Line;

[FluentConstructor<Line>]
public partial record Line1D<T>(T X) where T : INumber<T>;

[FluentConstructor<Line>]
public partial record Line2D<T>(T X, T Y) where T : INumber<T>;

[FluentConstructor<Line>]
public partial record Line3D<T>(T X, T Y, T Z) where T : INumber<T>;
```

**Generated Code:**
```csharp

public partial class Line
{
    public static Line1D<T> X<T>(in T x)
        where T : INumber<T>
    {
        return new Line1D<T>(x);
    }
}

public partial record Line1D<T>
{
    public Line2D<T> Y(in T y)
    {
        return new Line2D<T>(this.X, y);
    }
}

public partial record Line2D<T>
{
    public Line3D<T> Z(in T z)
    {
        return new Line3D<T>(this.X, this.Y, z);
    }
}

```

**Usage:**
```csharp
Line1D line1D = Line.X(5);
Line2D line2D = Line.X(5).Y(10);
Line3D line3D = Line.X(5).Y(10).Z(3);
```

This pattern is powerful because:
- Each type (`Line1D`, `Line2D`, `Line3D`) can be used as a standalone result
- The fluent chain naturally extends from simpler to more complex types
- No final `Create()` method is needed - each step returns the constructed object
- The types must be declared as `partial` to allow the generator to add fluent methods

### Advanced: Type Parameter Aliasing with `[As]`

When multiple constructors use different generic type parameter names for the same conceptual type, the generator can't merge them into a shared fluent chain. The `[As]` attribute solves this by aliasing type parameters to a common name:

```csharp
[FluentFactory(CreateMethod = CreateMethod.None, MethodPrefix = "")]
public static partial class Line;

[FluentConstructor<Line>]
public partial record Line1D<T>(T X) where T : INumber<T>;

// TNum is aliased to "T" so the generator merges this with Line1D
[FluentConstructor<Line>]
public partial record Line2D<[As("T")] TNum>(TNum X, TNum Y) where TNum : INumber<TNum>;

[FluentConstructor<Line>]
public partial record Line3D<T>(T X, T Y, T Z) where T : INumber<T>;
```

Without `[As("T")]`, the generator would treat `TNum` and `T` as different type parameters and fail to merge the chains. With it:

```csharp
Line1D<decimal> line1D = Line.X(10m);
Line2D<int>     line2D = Line.X(5).Y(10);
Line3D<double>  line3D = Line.X(1.0).Y(2.0).Z(3.0);
```

### Advanced: Self-Referential Factory

A type can be both the factory and a constructor target. This is useful when you want the fluent API to start from the type itself:

```csharp
[FluentFactory(CreateMethod = CreateMethod.Fixed)]
[FluentConstructor(typeof(Square<>))]
public partial record Square<T>(T Width) where T : INumber<T>;

// Usage - the type IS the factory:
Square<decimal> square = Square<decimal>.WithWidth(10m).Create();
```

### Advanced: Multiple Method Variants

Create multiple factory methods for the same parameter using `[MultipleFluentMethods]`:

```csharp
public class Calculator
{
    [FluentConstructor(typeof(CalculatorFactory))]
    public Calculator([MultipleFluentMethods(typeof(OperationMethods))] Func<int, int, int> operation)
    {
        Operation = operation;
    }

    public Func<int, int, int> Operation { get; }
}

public static class OperationMethods
{
    [FluentMethodTemplate]
    public static Func<int, int, int> Add()
    {
        return (a, b) => a + b;
    }

    [FluentMethodTemplate]
    public static Func<int, int, int> Multiply()
    {
        return (a, b) => a * b;
    }

    [FluentMethodTemplate]
    public static Func<int, int, int> Custom(Func<int, int, int> operation)
    {
        return operation;
    }
}

// Multiple ways to create:
var addCalc = CalculatorFactory.Add().CreateCalculator();
var multiplyCalc = CalculatorFactory.Multiply().CreateCalculator();
var customCalc = CalculatorFactory.Custom((a, b) => a - b).CreateCalculator();
```

### Factory-Level Defaults

Set defaults on `[FluentFactory]` that apply to all constructors. Individual `[FluentConstructor]` attributes can override any factory-level default:

```csharp
[FluentFactory(
    CreateMethod = CreateMethod.Dynamic,
    CreateVerb = "Build",
    MethodPrefix = "With",
    ReturnType = typeof(IShape))]
public static partial class ShapeFactory;

public class Circle : IShape
{
    // Inherits factory defaults: BuildCircle(), "With" prefix, returns IShape
    [FluentConstructor(typeof(ShapeFactory))]
    public Circle(double radius) { Radius = radius; }

    public double Radius { get; }
}

public class Square : IShape
{
    // Overrides: custom verb, custom prefix
    [FluentConstructor(typeof(ShapeFactory), CreateVerb = "Make", MethodPrefix = "Having")]
    public Square(double side) { Side = side; }

    public double Side { get; }
}
```

## API Reference

### Attributes

#### `[FluentFactory]`
Marks a static partial type as a fluent factory. The type must be `partial`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CreateMethod` | `CreateMethod` | `Dynamic` | Controls create method generation for all constructors |
| `CreateVerb` | `string?` | `"Create"` | Default verb for Create method names |
| `MethodPrefix` | `string?` | `"With"` | Default prefix for fluent method names |
| `ReturnType` | `Type?` | _none_ | Default return type for creation methods |

#### `[FluentConstructor(Type rootType)]` / `[FluentConstructor<TFluentFactory>]`
Marks a constructor, class, or struct to generate fluent methods for. Can be applied multiple times (`AllowMultiple = true`). The generic form `FluentConstructor<T>` is available for C# 11+ projects as a type-safe alternative to `typeof(...)`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RootType` | `Type` | _(required)_ | The factory type to generate methods in |
| `CreateMethod` | `CreateMethod` | _inherits_ | Overrides factory-level setting |
| `CreateVerb` | `string?` | _inherits_ | Overrides factory-level setting |
| `MethodPrefix` | `string?` | _inherits_ | Overrides factory-level setting |
| `ReturnType` | `Type?` | _inherits_ | Overrides factory-level setting |

#### `[As(string name)]`
Aliases a generic type parameter name for matching across multiple constructors. Applied to generic type parameters.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | _(required)_ | The canonical name to match this type parameter against |

#### `[FluentMethod(string methodName)]`
Customizes the fluent method name for a constructor parameter.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MethodName` | `string` | _(required)_ | The method name to generate |

#### `[MultipleFluentMethods(Type variantsType)]`
Generates multiple fluent methods for a single parameter based on template methods in the specified type.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `VariantsType` | `Type` | _(required)_ | Type containing `[FluentMethodTemplate]` methods |

#### `[FluentMethodTemplate]`
Marks static methods in a variants type as templates for generating fluent methods. Must be applied to static methods whose return type is assignable to the target parameter type.

### CreateMethod Enum

| Value | Description |
|-------|-------------|
| `Dynamic` | _(default)_ Appends target type name to verb (e.g., `CreateUser()`) |
| `Fixed` | Uses verb as-is (e.g., `Create()` or `Build()`) |
| `None` | No create method; constructor called at final step. Target type must be `partial` |

## Key Features

- **Type Safety** - Compile-time enforcement of required parameters
- **IntelliSense Support** - Full IDE support with method suggestions
- **Generic Support** - Works with generic classes, type inference, constraints, and type parameter aliasing via `[As]`
- **Generic Attribute Syntax** - Use `[FluentConstructor<T>]` for type-safe factory references (C# 11+)
- **Records and Primary Constructors** - Supports record types and C# 12+ primary constructors
- **Optional Parameters** - Parameters with defaults become optional setter methods
- **Customizable Naming** - Custom method names, prefixes, create verbs, and priorities
- **Return Type Control** - Create methods can return interfaces or base types
- **Multiple Method Variants** - Generate multiple methods for a single parameter via templates
- **Custom Partial Steps** - Use your own types as fluent steps with `CreateMethod.None`
- **Self-Referential Factories** - A type can serve as both factory and construction target
- **Factory-Level Defaults** - Set defaults on the factory, override per-constructor
- **Trie-Based Merging** - Shared parameter prefixes across constructors are intelligently merged
- **Performance** - Uses readonly structs, `in` parameters, and `[MethodImpl(AggressiveInlining)]` for zero overhead
- **Zero Dependencies** - Only attributes are included in your output assembly

## Diagnostics

The generator produces diagnostics to help you fix configuration issues:

| Code | Severity | Description |
|------|----------|-------------|
| MFFG0001 | Error | Unreachable fluent constructor due to parameter conflicts |
| MFFG0002 | Warning | Template method superseded by higher priority method |
| MFFG0003 | Warning | Template return type not assignable to parameter type |
| MFFG0004 | Error | All template methods incompatible with parameter type |
| MFFG0005 | Error | Template method must be static |
| MFFG0006 | Info | Template superseded by higher priority parameter |
| MFFG0007 | Error | Invalid CreateVerb (not a valid C# identifier) |
| MFFG0008 | Error | Duplicate create method name across constructors |
| MFFG0009 | Error | FluentConstructor target type missing FluentFactory attribute |
| MFFG0010 | Error | CreateVerb used with CreateMethod.None |
| MFFG0011 | Warning | Unsupported parameter modifier (ref/out) |
| MFFG0012 | Warning | Inaccessible constructor (private/protected) |
| MFFG0013 | Error | Factory type missing `partial` modifier |
| MFFG0014 | Warning | Parameter type less accessible than factory |
| MFFG0015 | Warning | Factory more accessible than target type |
| MFFG0016 | Error | Ambiguous fluent method chain |
| MFFG0017 | Warning | Empty CreateVerb with CreateMethod.None (no effect) |
| MFFG0018 | Error | Invalid MethodPrefix (not a valid C# identifier) |
| MFFG0019 | Error | Target type not assignable to ReturnType |
| MFFG0020 | Warning | ReturnType same as concrete target type (unnecessary) |
| MFFG0021 | Error | ReturnType used with CreateMethod.None |
| MFFG0022 | Error | Optional parameters cause ambiguous chain |
| MFFG0023 | Error | Conflicting type constraints produce duplicate method signatures |

## Contributing

This project is generated using the Converj. Contributions are welcome!

## License

Licensed under the MIT License. See LICENSE file for details.
