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
- ✓ All generated type references use fully qualified `global::` names — v1.1
- ✓ All generated types/members decorated with `[GeneratedCode]` attribute — v1.1
- ✓ Generator project reorganized with screaming architecture — v1.2
- ✓ Vertical slicing replaces horizontal layering — v1.2
- ✓ God classes decomposed into bite-sized, single-responsibility types — v1.2
- ✓ All existing tests continue to pass (behavior-preserving refactor) — v1.2

### Active

<!-- Current scope. Building toward these. -->

(None — planning next milestone)

### Out of Scope

<!-- Explicit boundaries. Includes reasoning to prevent re-adding. -->

- Test refactoring — production code is well-organized, test refactoring can be its own milestone if needed
- Generated output changes — current output format works well, changes would break consumers

## Context

- Source generator using IIncrementalGenerator (Roslyn incremental pipeline)
- Code generation builds Roslyn syntax trees via SyntaxFactory
- Generated output is .g.cs files added to compilation
- Existing tests use CSharpSourceGeneratorVerifier with expected output comparison
- Package version is 1.0.0, published as Motiv.FluentFactory on NuGet
- Shipped v1.2 with 5,708 LOC C# in the generator project
- Generator project uses screaming architecture: domain types at root, implementation in subdirectories (ConstructorAnalysis, ModelBuilding, SyntaxGeneration, Extensions)
- All source files ~150 lines or less with single responsibilities

## Constraints

- **Target framework**: netstandard2.0 (generator and attributes)
- **Roslyn compatibility**: Must work with Microsoft.CodeAnalysis 4.x
- **Zero runtime overhead**: Generated code uses structs and aggressive inlining

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Use `global::` for all type references | Prevents namespace conflicts in consumer code | ✓ Good |
| Tool name "Motiv.FluentFactory" for GeneratedCode attribute | Matches NuGet package name for discoverability | ✓ Good |
| Screaming architecture over horizontal layering | Key concepts visible at project root, details nested | ✓ Good |
| Vertical slicing over technical layers | Organize by feature/concern, not Analysis/Model/Generation | ✓ Good |
| Strategy pattern for constructor analysis | Pluggable storage detection, stateless strategies with semantic model parameter | ✓ Good |
| Thin orchestrator pattern for syntax generation | Method declarations delegate to focused helpers, easier to understand | ✓ Good |
| Shared TypeParameterConstraintBuilder | Single source of truth for constraint generation, fixed qualification bug | ✓ Good |
| Concern-based extension organization | Extensions grouped by domain concern (symbols, type params, fluent model, strings) | ✓ Good |

---
*Last updated: 2026-03-11 after v1.2 milestone*
