# Motiv.FluentFactory.Generator

A C# source generator that automatically creates fluent factory patterns for your classes and structs. Transform constructor parameters into chainable, strongly-typed builder methods that guide developers through object creation with IntelliSense support.

## 🚀 Quick Start

```csharp
using Motiv.FluentFactory.Generator;

[FluentFactory]
public static partial class PersonFactory;

public class Person
{
    [FluentConstructor(typeof(PersonFactory))]
    public Person(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public string Name { get; set; }
    public int Age { get; set; }
}

// Generated usage:
var person = PersonFactory
    .WithName("John")
    .WithAge(30)
    .Create();
```

## 📦 Installation

Install the NuGet package in your project:

```xml
<PackageReference Include="Motiv.FluentFactory.Generator" Version="1.0.0" />
```

Or via Package Manager Console:

```powershell
Install-Package Motiv.FluentFactory.Generator
```

## 📚 Tutorial

### Basic Fluent Factory

The simplest fluent factory requires two attributes:

1. `[FluentFactory]` - marks a static partial class as a factory
2. `[FluentConstructor]` - marks a constructor to generate fluent methods for

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
var book = BookFactory.WithTitle("The C# Guide").Create();
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

public struct Step_0__BookFactory
{
    public Book Create()
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
    .Create();
```

### Skipping the Create() Method

Use `FluentOptions.NoCreateMethod` to eliminate the final `Create()` call:

```csharp
public class Address
{
    [FluentConstructor(typeof(AddressFactory), Options = FluentOptions.NoCreateMethod)]
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
    .Create();
```

### Method Priorities

Control the order of fluent methods with priorities (lower numbers come first):

```csharp
public class Order
{
    [FluentConstructor(typeof(OrderFactory))]
    public Order(
        [FluentMethod("SetCustomer", Priority = 0)] string customer,
        [FluentMethod("AddItem", Priority = 2)] string item,
        [FluentMethod("SetDate", Priority = 1)] DateTime date)
    {
        Customer = customer;
        Item = item;
        Date = date;
    }

    // Properties...
}

// Usage follows priority order: Customer (0) → Date (1) → Item (2)
var order = OrderFactory
    .SetCustomer("John")
    .SetDate(DateTime.Now)
    .AddItem("Widget")
    .Create();
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
    .Create();

var stringContainer = ContainerFactory
    .WithValue("Hello")
    .WithLabel("Greeting")
    .Create();
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
var addCalc = CalculatorFactory.Add().Create();
var multiplyCalc = CalculatorFactory.Multiply().Create();
var customCalc = CalculatorFactory.Custom((a, b) => a - b).Create();
```

## 🔧 API Reference

### Attributes

#### `[FluentFactory]`
Marks a static partial class as a fluent factory. The class must be:
- `static`
- `partial`
- Can be generic

#### `[FluentConstructor(Type factoryType)]`
Marks a constructor to generate fluent methods for.

**Parameters:**
- `factoryType` - The factory class type to generate methods in
- `Options` - Configuration flags (optional)
- `CreateMethodName` - Custom name for the Create method (optional)

**Options:**
- `FluentOptions.None` - Default behavior
- `FluentOptions.NoCreateMethod` - Skip generating the Create() method

#### `[FluentMethod(string methodName)]`
Customizes the fluent method name for a parameter.

**Parameters:**
- `methodName` - The method name to generate
- `Priority` - Order priority (lower = earlier, default = 0)

#### `[MultipleFluentMethods(Type variantsType)]`
Generates multiple fluent methods based on template methods in the specified type.

**Parameters:**
- `variantsType` - Type containing `[FluentMethodTemplate]` methods
- `Priority` - Order priority (lower = earlier, default = 0)

#### `[FluentMethodTemplate]`
Marks methods in a variants type as templates for generating fluent methods.

## 🎯 Key Features

- **Type Safety** - Compile-time enforcement of required parameters
- **IntelliSense Support** - Full IDE support with method suggestions
- **Generic Support** - Works with generic classes and constraints
- **Customizable** - Custom method names, priorities, and behaviors
- **Performance** - Uses structs and aggressive inlining for minimal overhead
- **Zero Dependencies** - Only attributes are included in your output assembly

## 🤝 Contributing

This project is generated using the Motiv.FluentFactory.Generator. Contributions are welcome!

## 📄 License

Licensed under the MIT License. See LICENSE file for details.