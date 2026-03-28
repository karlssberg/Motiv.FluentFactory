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

Supports generics, records, primary constructors, required properties, type-first builder chains, custom method names, multiple overloads, return-type covariance, generic type parameter aliasing, and more.


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

**Note:** You cannot use `CreateVerb` or `ReturnType` together with `CreateMethod.None` as there would be no create method to name or type. Additionally, only one `[FluentConstructor]` per type per factory is allowed when `CreateMethod.None` is in effect — multiple constructors on the same type would break step ordering enforcement since all generated methods live on the same type.

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

// Record with primary constructor
[FluentConstructor(typeof(UserFactory))]
public record User(string Name, string Email);

var user = UserFactory
    .WithName("Alice")
    .WithEmail("alice@example.com")
    .CreateUser();
```

### Generic Attribute Syntax (C# 11+)

Instead of passing `typeof(...)`, you can use the generic `FluentConstructor<T>` syntax:

```csharp
[FluentFactory]
public partial class UserFactory;

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
Language constraints mean that generic attributes will only work with non-static and/or closed-generic types.

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
public partial class Line;

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

### Advanced: Factory-Scoped Parameters (Dependency Injection)

Use `[FluentParameter]` or record primary constructor parameters on the factory to thread values to all targets without exposing them in the fluent chain:

```csharp
public interface IDependency;

[FluentFactory(CreateVerb = "Build", CreateMethod = CreateMethod.Fixed)]
public partial record ServiceFactory(IDependency Dependency);

[FluentConstructor<ServiceFactory>]
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

### Required Properties

Properties marked with the `required` keyword or `[System.ComponentModel.DataAnnotations.Required]` are automatically discovered and added to the fluent chain as required steps after the constructor parameters:

```csharp
[FluentFactory]
public static partial class Factory;

public class Person
{
    [FluentConstructor(typeof(Factory))]
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

**Note:** Required properties are not supported with `CreateMethod.None` since property initialization requires a creation method with object initializer syntax.

### Optional Properties with `[FluentMethod]`

Non-required properties can be opted into the fluent chain using `[FluentMethod]`. These become optional setter methods on the final step that can be called in any order:

```csharp
public class Person
{
    [FluentConstructor(typeof(Factory))]
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

### Advanced: Type-First Builder Mode

By default, Converj uses parameter-first chains where consumers discover available types as they progress through shared parameter steps. Type-first mode inverts this — consumers select the target type up front, then fill in only that type's parameters:

```csharp
[FluentFactory(BuilderMode = BuilderMode.TypeFirst)]
public static partial class Factory;

public class Dog
{
    [FluentConstructor(typeof(Factory))]
    public Dog(string name) { Name = name; }
    public string Name { get; set; }
}

public class Cat
{
    [FluentConstructor(typeof(Factory))]
    public Cat(string name, int lives) { Name = name; Lives = lives; }
    public string Name { get; set; }
    public int Lives { get; set; }
}

// Type-first: choose the type, then fill its parameters
Dog dog = Factory.BuildDog().WithName("Rex").Create();
Cat cat = Factory.BuildCat().WithName("Whiskers").WithLives(9).Create();
```

The entry method verb defaults to `"Build"` but can be customized with `TypeFirstVerb`:

```csharp
[FluentFactory(BuilderMode = BuilderMode.TypeFirst, TypeFirstVerb = "Make")]
public static partial class Factory;

// Usage:
Dog dog = Factory.MakeDog().WithName("Rex").Create();
```

Both `BuilderMode` and `TypeFirstVerb` can be overridden per-constructor:

```csharp
[FluentFactory]
public static partial class Factory;

public class Dog
{
    // Only this constructor uses type-first mode
    [FluentConstructor(typeof(Factory), BuilderMode = BuilderMode.TypeFirst)]
    public Dog(string name) { Name = name; }
    public string Name { get; set; }
}
```

**Note:** Type-first constructors are excluded from parameter-first trie merging. Each type-first constructor gets its own isolated chain.

### Advanced: Partial Parameter Overlap

When using factory-scoped parameters (`[FluentParameter]`), a parameter must match all target constructors by default. Set `AllowPartialParameterOverlap` to allow a factory parameter to match only a subset of constructors:

```csharp
[FluentFactory(AllowPartialParameterOverlap = true)]
public partial record ServiceFactory(IDependency Dependency);

[FluentConstructor<ServiceFactory>]
public record ServiceA(IDependency Dependency, string Name);

// ServiceB doesn't use IDependency — that's OK with AllowPartialParameterOverlap
[FluentConstructor<ServiceFactory>]
public record ServiceB(string Name);
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
| `AllowPartialParameterOverlap` | `bool` | `false` | Allow `[FluentParameter]` to match only a subset of target constructors |
| `BuilderMode` | `BuilderMode` | `ParameterFirst` | Controls builder chain structure for all constructors |
| `TypeFirstVerb` | `string?` | `"Build"` | Verb for type-first entry method names (e.g., `BuildDog()`) |

#### `[FluentConstructor(Type rootType)]` / `[FluentConstructor<TFluentFactory>]`
Marks a constructor, class, or struct to generate fluent methods for. Can be applied multiple times (`AllowMultiple = true`). The generic form `FluentConstructor<T>` is available for C# 11+ projects as a type-safe alternative to `typeof(...)`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RootType` | `Type` | _(required)_ | The factory type to generate methods in |
| `CreateMethod` | `CreateMethod` | _inherits_ | Overrides factory-level setting |
| `CreateVerb` | `string?` | _inherits_ | Overrides factory-level setting |
| `MethodPrefix` | `string?` | _inherits_ | Overrides factory-level setting |
| `ReturnType` | `Type?` | _inherits_ | Overrides factory-level setting |
| `BuilderMode` | `BuilderMode` | _inherits_ | Overrides factory-level setting |
| `TypeFirstVerb` | `string?` | _inherits_ | Overrides factory-level setting |

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
| `TargetParameterName` | `string?` | _member name_ | Name of the target constructor parameter to bind to |

Record primary constructor parameters on factories are auto-threaded without needing an explicit `[FluentParameter]` attribute.

#### `[FluentStorage]` / `[FluentStorage(string parameterName)]`
Marks a property on a custom step type as storage for a constructor parameter value. Used with `CreateMethod.None` to bridge values between steps.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ParameterName` | `string?` | _property name_ | Name of the constructor parameter this property stores |

### CreateMethod Enum

| Value | Description |
|-------|-------------|
| `Dynamic` | _(default)_ Appends target type name to verb (e.g., `CreateUser()`) |
| `Fixed` | Uses verb as-is (e.g., `Create()` or `Build()`) |
| `None` | No create method; constructor called at final step. Target type must be `partial`. Only one `[FluentConstructor]` per type per factory is allowed |

### BuilderMode Enum

| Value | Description |
|-------|-------------|
| `ParameterFirst` | _(default)_ Consumers build parameters in sequence and discover available types as they progress (e.g., `Factory.WithName("Rex").CreateDog()`) |
| `TypeFirst` | Consumers select the target type up front, then fill in that type's parameters (e.g., `Factory.BuildDog().WithName("Rex").Create()`) |

## Key Features

- **Type Safety** - Compile-time enforcement of required parameters
- **IntelliSense Support** - Full IDE support with method suggestions
- **Generic Support** - Works with generic classes, type inference, constraints, and type parameter aliasing via `[As]`
- **Generic Attribute Syntax** - Use `[FluentConstructor<T>]` for type-safe factory references (C# 11+)
- **Records and Primary Constructors** - Supports record types and C# 12+ primary constructors
- **Required Properties** - `required` properties and `[Required]` properties are auto-discovered and enforced in the fluent chain
- **Optional Properties** - Opt non-required properties into the chain with `[FluentMethod]`
- **Type-First Builder Mode** - Choose the target type up front with `BuilderMode.TypeFirst` (e.g., `Factory.BuildDog().WithName("Rex").Create()`)
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
| CVJG0001 | Error | Unreachable fluent constructor due to parameter conflicts |
| CVJG0002 | Warning | Template method superseded by higher priority method |
| CVJG0003 | Warning | Template return type not assignable to parameter type |
| CVJG0004 | Error | All template methods incompatible with parameter type |
| CVJG0005 | Error | Template method must be static |
| CVJG0006 | Info | Template superseded by higher priority parameter |
| CVJG0007 | Error | Invalid CreateVerb (not a valid C# identifier) |
| CVJG0008 | Error | Duplicate create method name across constructors |
| CVJG0009 | Error | FluentConstructor target type missing FluentFactory attribute |
| CVJG0010 | Error | CreateVerb used with CreateMethod.None |
| CVJG0011 | Warning | Unsupported parameter modifier (ref/out) |
| CVJG0012 | Warning | Inaccessible constructor (private/protected) |
| CVJG0013 | Error | Factory type missing `partial` modifier |
| CVJG0014 | Warning | Parameter type less accessible than factory |
| CVJG0015 | Warning | Factory more accessible than target type |
| CVJG0016 | Error | Ambiguous fluent method chain |
| CVJG0017 | Warning | Empty CreateVerb with CreateMethod.None (no effect) |
| CVJG0018 | Error | Invalid MethodPrefix (not a valid C# identifier) |
| CVJG0019 | Error | Target type not assignable to ReturnType |
| CVJG0020 | Warning | ReturnType same as concrete target type (unnecessary) |
| CVJG0021 | Error | ReturnType used with CreateMethod.None |
| CVJG0022 | Error | Optional parameters cause ambiguous chain |
| CVJG0023 | Error | Conflicting type constraints produce duplicate method signatures |
| CVJG0024 | Error | Constructor parameter has no storage in custom step |
| CVJG0025 | Error | Custom step type missing `partial` modifier |
| CVJG0026 | Warning | FluentMethod on custom step parameter (no effect) |
| CVJG0027 | Warning | Parameter modifier on custom step parameter (unsupported) |
| CVJG0028 | Error | Duplicate custom step parameter name |
| CVJG0029 | Error | Custom step parameter type mismatch |
| CVJG0030 | Error | FluentParameter has no matching target constructor parameter |
| CVJG0031 | Error | FluentParameter type not compatible with target parameter |
| CVJG0032 | Error | Duplicate FluentParameter for same target name |
| CVJG0033 | Error | Static/instance method name collision |
| CVJG0035 | Error | FluentStorage property must have a getter |
| CVJG0036 | Error | Duplicate FluentStorage mapping to same parameter name |
| CVJG0037 | Error | Multiple FluentConstructors with CreateMethod.None on same type — only one allowed |
| CVJG0038 | Error | FluentMethod on property without set or init accessor |
| CVJG0039 | Warning | Required property cannot be used with CreateMethod.None |
| CVJG0040 | Error | Property produces fluent method name that clashes with constructor parameter |
| CVJG0041 | Error | Duplicate fluent property method name conflicts with another property or parameter |
| CVJG0042 | Info | FluentMethod without explicit name has no effect on constructor parameter |
| CVJG0043 | Error | Ambiguous type-first entry method between multiple types |

## Contributing

This project is generated using the Converj. Contributions are welcome!

## License

Licensed under the MIT License. See LICENSE file for details.
