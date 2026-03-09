# Motiv.FluentFactory

## What This Is

A C# source generator library that automatically creates fluent factory/builder patterns from constructor parameters. Developers mark classes with `[FluentFactory]` and constructors with `[FluentConstructor]`, and the generator produces chainable, strongly-typed builder methods at compile time with zero runtime overhead. Published as a NuGet package targeting netstandard2.0.

## Core Value

Developers write constructor parameters once and get a complete, type-safe fluent builder API generated automatically — no boilerplate, no runtime cost.

## Requirements

### Validated

<!-- Shipped and confirmed valuable. -->

- ✓ Attribute-based API ([FluentFactory], [FluentConstructor], [FluentMethod], [MultipleFluentMethods]) — v1.0
- ✓ Fluent step struct generation with chainable methods — v1.0
- ✓ Generic type support including nested generics — v1.0
- ✓ Method name customization and priority ordering — v1.0
- ✓ Multiple fluent methods per parameter via templates — v1.0
- ✓ NoCreateMethod option for custom partial types — v1.0
- ✓ XML documentation generation — v1.0
- ✓ 10 diagnostics for validation errors — v1.0
- ✓ Primary constructor support — v1.0
- ✓ NuGet packaging (generator + attributes bundled) — v1.0

### Active

<!-- Current scope. Building toward these. -->

- [ ] All generated type references use fully qualified `global::` names to avoid namespace conflicts
- [ ] All generated types/members decorated with `[GeneratedCode("Motiv.FluentFactory", "version")]` attribute

### Out of Scope

<!-- Explicit boundaries. Includes reasoning to prevent re-adding. -->

- New attribute features — focus is on code generation quality, not new capabilities
- Runtime API changes — this milestone is generator output only

## Context

- Source generator using IIncrementalGenerator (Roslyn incremental pipeline)
- Code generation builds Roslyn syntax trees via SyntaxFactory
- Generated output is .g.cs files added to compilation
- Existing tests use CSharpSourceGeneratorVerifier with expected output comparison
- Package version is 1.0.0, published as Motiv.FluentFactory on NuGet

## Constraints

- **Target framework**: netstandard2.0 (generator and attributes)
- **Roslyn compatibility**: Must work with Microsoft.CodeAnalysis 4.x
- **Zero runtime overhead**: Generated code uses structs and aggressive inlining

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Use `global::` for all type references | Prevents namespace conflicts in consumer code | — Pending |
| Tool name "Motiv.FluentFactory" for GeneratedCode attribute | Matches NuGet package name for discoverability | — Pending |

---
*Last updated: 2026-03-09 after milestone v1.1 initialization*
