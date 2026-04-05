# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

Converj is a C# Roslyn incremental source generator that creates fluent builder patterns from constructors and static methods. Users annotate classes with `[FluentRoot]` and constructors/methods with `[FluentTarget]`, and the generator produces chainable, strongly-typed builder methods at compile time.

## Build & Test Commands

```bash
dotnet build                          # Build entire solution
dotnet test                           # Run all tests
dotnet test --filter "FullyQualifiedName~NonGenericTests"   # Run a single test class
dotnet test --filter "DisplayName~Should_generate_when_applied_to_a_class_constructor"  # Run a single test method
```

## Solution Structure

Five projects under `src/`:

- **Converj.Attributes** (netstandard2.0) — User-facing attributes (`[FluentRoot]`, `[FluentTarget]`, `[FluentMethod]`, etc.)
- **Converj.Generator** (netstandard2.0) — The Roslyn `IIncrementalGenerator` and all code generation logic
- **Converj** (netstandard2.0) — NuGet packaging wrapper that bundles generator + attributes as an analyzer
- **Converj.Generator.Tests** (net9.0) — xUnit tests using Roslyn's `CSharpSourceGeneratorTest` verifier
- **Converj.Example** (net9.0) — Demo project

Global settings in `Directory.Build.props`: C# latest, nullable enabled, warnings as errors. Package versions managed centrally in `Directory.Packages.props`.

## Generator Architecture

The generator follows a 4-step incremental pipeline in `FluentFactoryGenerator.cs`:

1. **Syntax Filtering** — Find `[FluentTarget]` on types, constructors, and methods via `ForAttributeWithMetadataName`
2. **Target Analysis** (`ConstructorAnalysis/`) — Extract metadata from Roslyn symbols: storage strategies (field, property, primary constructor, record), parameters, root metadata
3. **Model Building** (`ModelBuilding/`) — Construct fluent API intermediate representation. Uses a `Trie<FluentMethodParameter, ConstructorMetadata>` to merge shared parameter prefixes across multiple targets
4. **Syntax Generation** (`SyntaxGeneration/`) — Emit Roslyn syntax trees producing `{Namespace}.{RootName}.g.cs` files

Key domain types at the generator root level:
- `FluentFactoryCompilationUnit` — Top-level output unit per root
- `IFluentMethod` / `IFluentReturn` — Polymorphic method-to-return chain
- `IFluentStep` — Builder step abstraction (regular struct steps vs existing partial types)
- `IFluentValueStorage` — Strategy for how parameters are stored (field, property, primary constructor, null)

Generated code uses structs with `[MethodImpl(AggressiveInlining)]` for zero-overhead builder chains. All type references in generated code are fully qualified with `global::` prefix.

### Key Enums

- **`TerminalMethod`** — Controls chain structure: `DynamicSuffix` (default, terminal named from target), `FixedName` (terminal verb as-is), `None` (no terminal, inline call)
- Internal mirror: `TerminalMethodKind` in `Domain/TerminalMethodKind.cs`

### Entry Method Attribute

- **`[FluentEntryMethod("name")]`** — Companion attribute applied alongside `[FluentTarget]` to enable type-first mode. Generates a parameterless entry method that narrows the chain to a specific target type (e.g., `Factory.BuildDog().WithName("Rex").Create()`). The `name` constructor parameter is required and sets the full method identifier.

### Static Method Targets

`[FluentTarget]` can be applied to static methods on any class. The static method's parameters feed into the root's trie like constructor parameters, but the terminal step calls the static method instead of `new T(...)`. Default terminal name is the method name, default builder mode is `FixedName`.

## Test Patterns

Tests use `CSharpSourceGeneratorVerifier<FluentFactoryGenerator>` (defined in the test project), which wraps Roslyn's `CSharpSourceGeneratorTest`. Each test:

1. Defines input C# source as a raw string literal (`"""..."""`)
2. Defines expected generated output as a raw string literal
3. Adds both to a `VerifyCS.Test` instance and calls `RunAsync()`

```csharp
using VerifyCS = Converj.Generator.Tests.CSharpSourceGeneratorVerifier<FluentFactoryGenerator>;

[Fact]
internal async Task Should_generate_when_...()
{
    const string code = """...""";
    const string expected = """...""";

    var test = new VerifyCS.Test
    {
        TestState = { Sources = { code }, GeneratedSources = { (typeof(FluentFactoryGenerator), "FileName.g.cs", expected) } }
    };
    await test.RunAsync();
}
```

Generated source file naming convention: `{Namespace}.{RootTypeName}.g.cs`

For diagnostic tests, use `test.TestState.ExpectedDiagnostics.Add(...)` with descriptors from `FluentDiagnostics`.

## C# Coding Standards

- **LINQ over loops** for queries; loops for commands. Exceptions: readers, hot paths
- **CQS** — methods either change state or return data, not both
- **Method decomposition** — root methods orchestrate, extract details into well-named private methods
- **Cancellation tokens** on async/long-lived operations
- **XML doc headers** on all public methods
- **Modern C#** — raw string literals, pattern matching, switch expressions
- **Testing** — xUnit, AutoFixture with NSubstitute where appropriate

## TDD Workflow

1. Write a failing test first
2. Confirm it fails for the right reason
3. Write minimum code to pass
4. Confirm it passes
5. Refactor while keeping tests green

Always run the full test suite before considering work complete. For bug fixes, write a reproducing test first.
