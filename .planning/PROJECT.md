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

### Active

<!-- Current scope. Building toward these. -->

- [ ] Generator project reorganized with screaming architecture (key concepts at root, details in subdirectories)
- [ ] Vertical slicing replaces horizontal layering (by feature/concern, not by technical layer)
- [ ] God classes decomposed into bite-sized, single-responsibility types
- [ ] All existing tests continue to pass (behavior-preserving refactor)

### Out of Scope

<!-- Explicit boundaries. Includes reasoning to prevent re-adding. -->

- New features or attribute changes — pure refactoring milestone
- Runtime API changes — internal generator structure only
- Test refactoring — focus is on production code organization
- Generated output changes — refactoring must not alter generated .g.cs files

## Current Milestone: v1.2 Architecture Refactoring

**Goal:** Reorganize the Generator project for screaming architecture with vertical slicing, and decompose god classes into bite-sized, single-responsibility types.

**Target features:**
- Screaming architecture — important concepts at project root, implementation details in subdirectories
- Vertical slicing — organize by feature/concern instead of horizontal layers (Analysis/, Model/, Generation/)
- God class decomposition — break FluentModelFactory (438 lines), FluentFactoryGenerator (376 lines), and other large classes into focused types
- Bite-sized files — each class has a single, clear responsibility

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
| Use `global::` for all type references | Prevents namespace conflicts in consumer code | ✓ Good |
| Tool name "Motiv.FluentFactory" for GeneratedCode attribute | Matches NuGet package name for discoverability | ✓ Good |
| Screaming architecture over horizontal layering | Key concepts visible at project root, details nested | — Pending |
| Vertical slicing over technical layers | Organize by feature/concern, not Analysis/Model/Generation | — Pending |

---
*Last updated: 2026-03-10 after milestone v1.2 initialization*
