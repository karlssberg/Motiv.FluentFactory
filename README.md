# Converj

Stop writing builders by hand. Curry them instead.

Converj is a C# source generator that creates fluent builder patterns from your constructors and static methods.
Annotate with [FluentRoot] and [FluentTarget], and get zero-allocation,
strongly-typed builder chains — generated at compile time.

Use Converj if:

* You want to create complex fluent interfaces
* You despise boilerplate ceremony
* Performance matters

Annotate methods or constructors with `[FluentTarget]`,
and the generator produces a chain of fluent methods starting from `[FluentRoot]` that mirror the parameter sequence.
Required parameters are enforced, optional parameters stay optional,
and the whole chain compiles down to zero-allocation, JIT optimizer-friendly code.


```csharp
// You write this:
[FluentRoot]
public partial class Shape;

[FluentTarget<Shape>]
public record Square<T>(T Width);

[FluentTarget<Shape>] 
public record Rectangle<T>(T Width, T Height);

[FluentTarget<Shape>] 
public record Cube<T>(T Width, T Height, T Depth);

// You get this:
Square<int>       square = Shape.WithWidth(10).CreateSquare();
Rectangle<int> rectangle = Shape.WithWidth(10).WithHeight(20).CreateRectangle();
Cube<int>           cube = Shape.WithWidth(10).WithHeight(20).WithDepth(30).CreateCube();
```

Supports generics, records, primary constructors, required properties, type-first builder chains, static method targets, extension methods, custom method names, multiple overloads, return-type covariance, generic type parameter aliasing, named tuple unpacking, and more.


## Installation

Install the NuGet package in your project:

```xml
<PackageReference Include="Converj" Version="2.0.0" />
```

Via the command line:

```bash
dotnet add package Converj
```

Or via Package Manager Console:

```powershell
Install-Package Converj
```

## Tutorial

### Basic Fluent Factory

The simplest fluent factory requires two attributes:

1. `[FluentRoot]` - marks a partial class as the entry point for fluent methods
2. `[FluentTarget]` - marks a constructor to generate fluent methods for using the constructor parameters as the fluent steps

```csharp
[FluentRoot]
public static partial class BookFactory;

public class Book
{
    [FluentTarget(typeof(BookFactory))]
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
[FluentRoot]
public static partial class ProductFactory;

public class Product
{
    [FluentTarget(typeof(ProductFactory))]
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

By default, the create method name includes the target type name (DynamicSuffix mode): `CreateCar()`, `CreateAddress()`, etc. This automatically disambiguates when a factory serves multiple types.

Customize the create verb using the `TerminalVerb` parameter:

```csharp
[FluentRoot]
public static partial class VehicleFactory;

public class Car
{
    [FluentTarget(typeof(VehicleFactory), TerminalMethod = TerminalMethod.FixedName, TerminalVerb = "BuildCar")]
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

Use `TerminalMethod = TerminalMethod.None` to eliminate the final Create call:

```csharp
public class Address
{
    [FluentTarget(typeof(AddressFactory), TerminalMethod = TerminalMethod.None)]
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

**Note:** You cannot use `TerminalVerb` or `ReturnType` together with `TerminalMethod.None` as there would be no create method to name or type. Additionally, only one `[FluentTarget]` per type per factory is allowed when `TerminalMethod.None` is in effect — multiple constructors on the same type would break step ordering enforcement since all generated methods live on the same type.

### Custom Method Names

Customize method names using `[FluentMethod]`:

```csharp
public class User
{
    [FluentTarget(typeof(UserFactory))]
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

By default, fluent methods are prefixed with `With` (e.g., `WithName`, `WithAge`). You can customize this with the `MethodPrefix` property on either `[FluentRoot]` or `[FluentTarget]`:

```csharp
// Factory-level prefix applies to all constructors
[FluentRoot(MethodPrefix = "Having")]
public static partial class ConfigFactory;

public class Config
{
    [FluentTarget(typeof(ConfigFactory))]
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
[FluentRoot(MethodPrefix = "")]
public static partial class PointFactory;

public class Point
{
    [FluentTarget(typeof(PointFactory))]
    public Point(int x, int y) { X = x; Y = y; }

    public int X { get; }
    public int Y { get; }
}

// Usage with bare names:
var point = PointFactory.X(10).Y(20).CreatePoint();
```

Constructor-level `MethodPrefix` overrides the factory-level default:

```csharp
[FluentRoot(MethodPrefix = "With")]
public static partial class ShapeFactory;

public class Circle
{
    [FluentTarget(typeof(ShapeFactory), MethodPrefix = "Having")]
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

[FluentRoot]
public static partial class AnimalFactory;

public class Dog : IAnimal
{
    [FluentTarget(typeof(AnimalFactory), ReturnType = typeof(IAnimal))]
    public Dog(string name) { Name = name; }

    public string Name { get; }
}

public class Cat : IAnimal
{
    [FluentTarget(typeof(AnimalFactory), ReturnType = typeof(IAnimal))]
    public Cat(string name) { Name = name; }

    public string Name { get; }
}

// Creation methods return the interface type:
IAnimal dog = AnimalFactory.WithName("Rex").CreateDog();
IAnimal cat = AnimalFactory.WithName("Whiskers").CreateCat();
```

You can also set `ReturnType` at the factory level to apply it to all constructors:

```csharp
[FluentRoot(ReturnType = typeof(IAnimal))]
public static partial class AnimalFactory;
```

**Note:** The target type must be assignable to the return type (i.e., it must implement the interface or extend the base class).

### Optional Parameters and Default Values

Parameters with default values become optional setter methods. They can be called in any order and are not required:

```csharp
[FluentRoot]
public static partial class ConnectionFactory;

public class Connection
{
    [FluentTarget(typeof(ConnectionFactory))]
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
[FluentRoot]
public static partial class ContainerFactory;

public class Container<T>
{
    [FluentTarget(typeof(ContainerFactory))]
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
[FluentRoot]
public static partial class UserFactory;

// Record with primary constructor
[FluentTarget(typeof(UserFactory))]
public record User(string Name, string Email);

var user = UserFactory
    .WithName("Alice")
    .WithEmail("alice@example.com")
    .CreateUser();
```

### Generic Attribute Syntax (C# 11+)

Instead of passing `typeof(...)`, you can use the generic `FluentTarget<T>` syntax:

```csharp
[FluentRoot]
public partial class UserFactory;

public class User
{
    // Generic syntax - cleaner and type-safe
    [FluentTarget<UserFactory>]
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
Language constraints mean that generic attributes will only work with non-static and/or closed-generic types.

### FluentTarget on Types

`[FluentTarget]` can be applied directly to classes, structs, and records (not just constructors). This is especially useful with records that use positional parameters:

```csharp
[FluentRoot]
public static partial class Line;

[FluentTarget(typeof(Line), TerminalMethod = TerminalMethod.None)]
public partial record Line1D([FluentMethod("X")] int X);

[FluentTarget(typeof(Line), TerminalMethod = TerminalMethod.None)]
public partial record Line2D([FluentMethod("X")] int X, [FluentMethod("Y")] int Y);

[FluentTarget(typeof(Line), TerminalMethod = TerminalMethod.None)]
public partial record Line3D([FluentMethod("X")] int X, [FluentMethod("Y")] int Y, [FluentMethod("Z")] int Z);
```

### Static Method Targets

`[FluentTarget]` can be applied to static methods on any class. The method's parameters feed into the root's trie like constructor parameters, but the terminal step calls the static method instead of `new T(...)`:

```csharp
[FluentRoot]
public partial class Vehicle;

[FluentTarget<Vehicle>]
public static string DispatchVehicle(
    [FluentMethod("WithCar")] Car? car = null,
    [FluentMethod("WithTrain")] Train? train = null)
{
    return (car, train) switch
    {
        (not null, _) => $"Dispatched car: {car}",
        (_, not null) => $"Dispatched train: {train}",
        _ => "No vehicle to dispatch"
    };
}

// Usage:
string result = Vehicle
    .WithCar(myCar)
    .DispatchVehicle();
```

The default terminal name is the method name itself, and the default terminal mode is `FixedName`.

### Extension Methods

Extension methods generate fluent chains that start as extension methods on the receiver type. There are two ways to mark a parameter as the extension receiver:

**Auto-detected from `this` modifier:**
```csharp
[FluentRoot(TerminalMethod = TerminalMethod.FixedName)]
public static partial class StringExtensions;

[FluentTarget(typeof(StringExtensions), TerminalVerb = "Pad")]
public static string PadString(this string input, int width) => input.PadRight(width);

// Usage:
string padded = "hello".WithWidth(80).Pad();
```

**Explicit via `[This]` attribute (for constructors or non-extension static methods):**
```csharp
[FluentRoot(TerminalMethod = TerminalMethod.FixedName)]
public static partial class ShapeExtensions;

[FluentTarget(typeof(ShapeExtensions), TerminalVerb = "ToColoredDiamond")]
public record ColoredDiamond<T>([This] Diamond<T> Diamond, Color Color) where T : INumber<T>;

// Usage - starts from the receiver type:
var coloredDiamond = diamond.WithColor(Color.Red).ToColoredDiamond();
```

The receiver parameter is extracted from the parameter list before trie building and threaded through all step structs. The root must be a `static partial class` when used with extension method targets.

### Advanced: Custom Partial Types as Fluent Steps

When using `TerminalMethod.None`, you can create custom partial types that function as both fluent steps and construction targets. This advanced pattern allows you to build complex fluent chains where each type can be both an intermediate step and a final result:

```csharp
[FluentRoot(TerminalMethod = TerminalMethod.None, MethodPrefix = "")]
public partial class Line;

[FluentTarget<Line>]
public partial record Line1D<T>(T X) where T : INumber<T>;

[FluentTarget<Line>]
public partial record Line2D<T>(T X, T Y) where T : INumber<T>;

[FluentTarget<Line>]
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
[FluentRoot(TerminalMethod = TerminalMethod.None, MethodPrefix = "")]
public partial class Line;

[FluentTarget<Line>]
public partial record Line1D<T>(T X) where T : INumber<T>;

// TNum is aliased to "T" so the generator merges this with Line1D
[FluentTarget<Line>]
public partial record Line2D<[As("T")] TNum>(TNum X, TNum Y) where TNum : INumber<TNum>;

[FluentTarget<Line>]
public partial record Line3D<T>(T X, T Y, T Z) where T : INumber<T>;
```

Without `[As("T")]`, the generator would treat `TNum` and `T` as different type parameters and fail to merge the chains. With it:

```csharp
Line1D<decimal> line1D = Line.X(10m);
Line2D<int>     line2D = Line.X(5).Y(10);
Line3D<double>  line3D = Line.X(1.0).Y(2.0).Z(3.0);
```

### Advanced: Factory-Scoped Parameters (Dependency Injection)

Use `[FluentParameter]` or record primary constructor parameters on the factory to thread values to all targets without exposing them in the fluent chain:

```csharp
public interface IDependency;

[FluentRoot(TerminalVerb = "Build", TerminalMethod = TerminalMethod.FixedName)]
public partial record ServiceFactory(IDependency Dependency);

[FluentTarget<ServiceFactory>]
public record CustomizableService(IDependency Dependency, string Name);

// Usage - Dependency is provided by the factory, not the caller:
var factory = new ServiceFactory(myDependency);
var service = factory.WithName("MyService").Build();
// service.Dependency == myDependency (threaded from factory)
```

Record primary constructor parameters are auto-threaded when they match target constructor parameter names. For non-record factories, use `[FluentParameter]` explicitly on fields or properties.

### Advanced: Self-Referential Factory

A type can be both the factory and a constructor target. This is useful when you want the fluent API to start from the type itself:

```csharp
[FluentRoot(TerminalMethod = TerminalMethod.FixedName)]
[FluentTarget(typeof(Square<>))]
public partial record Square<T>(T Width) where T : INumber<T>;

// Usage - the type IS the factory:
Square<decimal> square = Square<decimal>.WithWidth(10m).Create();
```

### Advanced: Multiple Method Variants

Create multiple factory methods for the same parameter using `[MultipleFluentMethods]`:

```csharp
public class Calculator
{
    [FluentTarget(typeof(CalculatorFactory))]
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

### Required Properties

Properties marked with the `required` keyword or `[System.ComponentModel.DataAnnotations.Required]` are automatically discovered and added to the fluent chain as required steps after the constructor parameters:

```csharp
[FluentRoot]
public static partial class Factory;

public class Person
{
    [FluentTarget(typeof(Factory))]
    public Person(string name)
    {
        Name = name;
    }

    public string Name { get; set; }

    public required string Email { get; set; }
}

// Email is required — the compiler enforces it:
var person = Factory
    .WithName("Alice")
    .WithEmail("alice@example.com")
    .CreatePerson();
```

The generated code uses object initializers to set the required properties:

```csharp
return new Person(this._name__parameter)
{
    Email = this._email__parameter
};
```

Properties that are already initialized by a constructor parameter (e.g., record positional parameters) are automatically skipped. You can rename the generated method using `[FluentMethod("CustomName")]`:

```csharp
[FluentMethod("SetEmail")]
public required string Email { get; set; }
```

**Note:** Required properties are not supported with `TerminalMethod.None` since property initialization requires a creation method with object initializer syntax.

### Optional Properties with `[FluentMethod]`

Non-required properties can be opted into the fluent chain using `[FluentMethod]`. These become optional setter methods on the final step that can be called in any order:

```csharp
public class Person
{
    [FluentTarget(typeof(Factory))]
    public Person(string name)
    {
        Name = name;
    }

    public string Name { get; set; }

    [FluentMethod]
    public string? Nickname { get; set; }

    [FluentMethod("SetAge")]
    public int Age { get; set; }
}

// Optional properties can be set or skipped:
var person = Factory
    .WithName("Alice")
    .WithNickname("Ali")  // optional
    .SetAge(30)           // optional, custom name
    .CreatePerson();
```

### Advanced: Type-First Entry Methods

By default, Converj uses parameter-first chains where consumers discover available types as they progress through shared parameter steps. Type-first entry methods invert this — consumers select the target type up front via a parameterless entry method, then fill in only that type's parameters:

```csharp
[FluentRoot]
public static partial class Factory;

[FluentTarget(typeof(Factory))]
[FluentEntryMethod("BuildDog")]
public class Dog
{
    public Dog(string name) { Name = name; }
    public string Name { get; set; }
}

[FluentTarget(typeof(Factory))]
[FluentEntryMethod("BuildCat")]
public class Cat
{
    public Cat(string name, int lives) { Name = name; Lives = lives; }
    public string Name { get; set; }
    public int Lives { get; set; }
}

// Type-first: choose the type, then fill its parameters
Dog dog = Factory.BuildDog().WithName("Rex").Create();
Cat cat = Factory.BuildCat().WithName("Whiskers").WithLives(9).Create();
```

The `[FluentEntryMethod]` attribute takes the full method identifier as its constructor parameter (e.g., `"BuildDog"`). It is a companion attribute applied alongside `[FluentTarget]`. Targets with `[FluentEntryMethod]` are excluded from parameter-first trie merging and get their own isolated chain.

### Advanced: Partial Parameter Overlap

When using factory-scoped parameters (`[FluentParameter]`), a parameter must match all target constructors by default. Set `AllowPartialParameterOverlap` to allow a factory parameter to match only a subset of constructors:

```csharp
[FluentRoot(AllowPartialParameterOverlap = true)]
public partial record ServiceFactory(IDependency Dependency);

[FluentTarget<ServiceFactory>]
public record ServiceA(IDependency Dependency, string Name);

// ServiceB doesn't use IDependency — that's OK with AllowPartialParameterOverlap
[FluentTarget<ServiceFactory>]
public record ServiceB(string Name);
```

### Factory-Level Defaults

Set defaults on `[FluentRoot]` that apply to all constructors. Individual `[FluentTarget]` attributes can override any factory-level default:

```csharp
[FluentRoot(
    TerminalMethod = TerminalMethod.DynamicSuffix,
    TerminalVerb = "Build",
    MethodPrefix = "With",
    ReturnType = typeof(IShape))]
public static partial class ShapeFactory;

public class Circle : IShape
{
    // Inherits factory defaults: BuildCircle(), "With" prefix, returns IShape
    [FluentTarget(typeof(ShapeFactory))]
    public Circle(double radius) { Radius = radius; }

    public double Radius { get; }
}

public class Square : IShape
{
    // Overrides: custom verb, custom prefix
    [FluentTarget(typeof(ShapeFactory), TerminalVerb = "Make", MethodPrefix = "Having")]
    public Square(double side) { Side = side; }

    public double Side { get; }
}
```

## API Reference

### Attributes

#### `[FluentRoot]`
Marks a partial type as a fluent factory root. The type must be `partial`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TerminalMethod` | `TerminalMethod` | `DynamicSuffix` | Controls create method generation for all targets |
| `TerminalVerb` | `string?` | `"Create"` | Default verb for create method names |
| `MethodPrefix` | `string?` | `"With"` | Default prefix for fluent method names |
| `ReturnType` | `Type?` | _none_ | Default return type for creation methods |
| `AllowPartialParameterOverlap` | `bool` | `false` | Allow `[FluentParameter]` to match only a subset of target constructors |

#### `[FluentTarget(Type rootType)]` / `[FluentTarget<TFluentRoot>]`
Marks a constructor, class, struct, or static method as a target for fluent builder generation. Can be applied multiple times (`AllowMultiple = true`). The generic form `FluentTarget<T>` is available for C# 11+ projects as a type-safe alternative to `typeof(...)`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RootType` | `Type` | _(required)_ | The root type to generate fluent methods on |
| `TerminalMethod` | `TerminalMethod` | _inherits_ | Overrides root-level setting |
| `TerminalVerb` | `string?` | _inherits_ | Overrides root-level setting |
| `MethodPrefix` | `string?` | _inherits_ | Overrides root-level setting |
| `ReturnType` | `Type?` | _inherits_ | Overrides root-level setting |

#### `[FluentEntryMethod(string name)]`
Companion attribute applied alongside `[FluentTarget]` to enable type-first mode. Generates a parameterless entry method that narrows the chain to a specific target type. Targets with this attribute are excluded from parameter-first trie merging.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | _(required)_ | The full method identifier for the entry method (e.g., `"BuildDog"`) |

#### `[This]`
Marks the first parameter of a constructor or static method as the extension receiver. The generated fluent chain starts as an extension method on this parameter's type. On actual C# extension methods (with the `this` modifier), this attribute is redundant.

#### `[As(string name)]`
Aliases a generic type parameter name for matching across multiple constructors. Applied to generic type parameters.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | _(required)_ | The canonical name to match this type parameter against |

#### `[FluentMethod]` / `[FluentMethod(string methodName)]`
When applied to a constructor parameter, customizes the fluent method name. When applied to a property, opts the property into the fluent chain (required for non-required properties) and optionally overrides the method name.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MethodName` | `string?` | _none_ | The method name to generate. Optional on properties (uses prefix + property name by default) |

#### `[MultipleFluentMethods(Type variantsType)]`
Generates multiple fluent methods for a single parameter based on template methods in the specified type.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `VariantsType` | `Type` | _(required)_ | Type containing `[FluentMethodTemplate]` methods |

#### `[FluentMethodTemplate]`
Marks static methods in a variants type as templates for generating fluent methods. Must be applied to static methods whose return type is assignable to the target parameter type.

#### `[FluentParameter]` / `[FluentParameter(string targetParameterName)]`
Marks a factory field, property, or record primary constructor parameter as a value to thread through to target constructors. The factory instance holds the value, and the generator automatically passes it to matching target constructor parameters.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ParameterName` | `string?` | _member name_ | Name of the target constructor parameter to bind to |

Record primary constructor parameters on factories are auto-threaded without needing an explicit `[FluentParameter]` attribute.

#### `[FluentStorage]` / `[FluentStorage(string parameterName)]`
Marks a property on a custom step type as storage for a constructor parameter value. Used with `TerminalMethod.None` to bridge values between steps.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ParameterName` | `string?` | _property name_ | Name of the constructor parameter this property stores |

### TerminalMethod Enum

| Value | Description |
|-------|-------------|
| `DynamicSuffix` | _(default)_ Appends target type name to verb (e.g., `CreateUser()`) |
| `FixedName` | Uses verb as-is (e.g., `Create()` or `Build()`) |
| `None` | No create method; target invoked at final step. Target type must be `partial`. Only one `[FluentTarget]` per type per root is allowed |

## Key Features

- **Type Safety** - Compile-time enforcement of required parameters
- **IntelliSense Support** - Full IDE support with method suggestions
- **Generic Support** - Works with generic classes, type inference, constraints, and type parameter aliasing via `[As]`
- **Generic Attribute Syntax** - Use `[FluentTarget<T>]` for type-safe root references (C# 11+)
- **Records and Primary Constructors** - Supports record types and C# 12+ primary constructors
- **Static Method Targets** - Apply `[FluentTarget]` to static methods to generate fluent chains that call the method
- **Extension Methods** - Generate fluent chains as extension methods via `[This]` or the `this` modifier
- **Required Properties** - `required` properties and `[Required]` properties are auto-discovered and enforced in the fluent chain
- **Optional Properties** - Opt non-required properties into the chain with `[FluentMethod]`
- **Type-First Entry Methods** - Select the target type up front with `[FluentEntryMethod]` (e.g., `Factory.BuildDog().WithName("Rex").Create()`)
- **Named Tuple Unpacking** - Tuple parameters with named elements are unpacked into individual fluent method parameters
- **Optional Parameters** - Parameters with defaults become optional setter methods
- **Customizable Naming** - Custom method names, prefixes, create verbs, and priorities
- **Return Type Control** - Create methods can return interfaces or base types
- **Multiple Method Variants** - Generate multiple methods for a single parameter via templates
- **Custom Partial Steps** - Use your own types as fluent steps with `TerminalMethod.None`
- **Self-Referential Factories** - A type can serve as both factory and construction target
- **Factory-Level Defaults** - Set defaults on the root, override per-target
- **Trie-Based Merging** - Shared parameter prefixes across targets are intelligently merged
- **Performance** - Uses readonly structs, `in` parameters, and `[MethodImpl(AggressiveInlining)]` for zero overhead
- **Zero Dependencies** - Only attributes are included in your output assembly

## Diagnostics

The generator produces diagnostics to help you fix configuration issues:

| Code | Severity | Description |
|------|----------|-------------|
| CVJG0001 | Error | Unreachable fluent constructor due to parameter conflicts |
| CVJG0002 | Warning | Template method superseded by higher priority method |
| CVJG0003 | Warning | Template return type not assignable to parameter type |
| CVJG0004 | Error | All template methods incompatible with parameter type |
| CVJG0005 | Error | Template method must be static |
| CVJG0006 | Info | Template superseded by higher priority parameter |
| CVJG0007 | Error | Invalid TerminalVerb (not a valid C# identifier) |
| CVJG0008 | Error | Duplicate terminal method name across targets |
| CVJG0009 | Error | FluentTarget root type missing FluentRoot attribute |
| CVJG0010 | Error | TerminalVerb used with TerminalMethod.None |
| CVJG0011 | Warning | Unsupported parameter modifier (ref/out) |
| CVJG0012 | Warning | Inaccessible constructor (private/protected) |
| CVJG0013 | Error | Root type missing `partial` modifier |
| CVJG0014 | Warning | Parameter type less accessible than root |
| CVJG0015 | Warning | Root more accessible than target type |
| CVJG0016 | Error | Ambiguous fluent method chain |
| CVJG0017 | Warning | Empty TerminalVerb with TerminalMethod.None (no effect) |
| CVJG0018 | Error | Invalid MethodPrefix (not a valid C# identifier) |
| CVJG0019 | Error | Target type not assignable to ReturnType |
| CVJG0020 | Warning | ReturnType same as concrete target type (unnecessary) |
| CVJG0021 | Error | ReturnType used with TerminalMethod.None |
| CVJG0022 | Error | Optional parameters cause ambiguous chain |
| CVJG0023 | Error | Conflicting type constraints produce duplicate method signatures |
| CVJG0024 | Error | Constructor parameter has no storage in custom step |
| CVJG0025 | Warning | FluentParameter on type without FluentRoot |
| CVJG0026 | Warning | FluentParameter on static factory type |
| CVJG0027 | Error | FluentParameter property has no getter |
| CVJG0028 | Error | Duplicate FluentParameter mapping to same target name |
| CVJG0029 | Error | FluentParameter type mismatch with target parameter |
| CVJG0030 | Warning | FluentParameter has no matching target constructor parameter |
| CVJG0031 | Error | FluentParameter partial overlap (set AllowPartialParameterOverlap) |
| CVJG0032 | Info | FluentParameter overrides FluentMethod on target parameter |
| CVJG0033 | Error | Static/instance method name collision |
| CVJG0035 | Error | FluentStorage property must have a getter |
| CVJG0036 | Error | Duplicate FluentStorage mapping to same parameter name |
| CVJG0037 | Error | Multiple FluentTargets with TerminalMethod.None on same type |
| CVJG0038 | Error | FluentMethod on property without set or init accessor |
| CVJG0039 | Warning | Required property cannot be used with TerminalMethod.None |
| CVJG0040 | Error | Property produces fluent method name that clashes with constructor parameter |
| CVJG0041 | Error | Duplicate fluent property method name conflicts with another property or parameter |
| CVJG0042 | Info | FluentMethod without explicit name has no effect on constructor parameter |
| CVJG0043 | Error | Ambiguous entry method between multiple types |
| CVJG0044 | Warning | FluentTarget on instance method (must be static) |
| CVJG0045 | Warning | Tuple parameter has unnamed elements |
| CVJG0046 | Error | [This] must be on the first parameter |
| CVJG0047 | Error | [This] not supported on instance methods |
| CVJG0048 | Info | [This] redundant on extension method parameter |
| CVJG0049 | Error | FluentRoot must be static for extension method targets |

## Contributing

Contributions are welcome!

## License

Licensed under the MIT License. See LICENSE file for details.
