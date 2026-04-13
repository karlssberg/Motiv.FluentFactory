# Converj

## What This Is

Converj is a C# Roslyn incremental source generator that creates fluent builder patterns from constructors, static methods, and extension methods. Developers annotate classes with `[FluentRoot]` and constructors/methods with `[FluentTarget]`, and the generator produces chainable, strongly-typed builder methods at compile time with zero runtime overhead. Published as a NuGet package targeting netstandard2.0.

## Core Value

Developers write constructor parameters once and get a complete, type-safe fluent builder API generated automatically — no boilerplate, no runtime cost.

## Requirements

### Validated

<!-- Shipped and confirmed valuable. -->

**v1.0 — Initial Release**
- ✓ Attribute-based API (`[FluentFactory]`, `[FluentConstructor]`, `[FluentMethod]`, `[MultipleFluentMethods]`) — v1.0
- ✓ Fluent step struct generation with chainable methods — v1.0
- ✓ Generic type support including nested generics — v1.0
- ✓ Method name customization and priority ordering — v1.0
- ✓ Multiple fluent methods per parameter via templates — v1.0
- ✓ NoCreateMethod option for custom partial types — v1.0
- ✓ XML documentation generation — v1.0
- ✓ 10 diagnostics for validation errors — v1.0
- ✓ Primary constructor support — v1.0
- ✓ NuGet packaging (generator + attributes bundled) — v1.0

**v1.1 — Code Generation Quality**
- ✓ All generated type references use fully qualified `global::` names — v1.1
- ✓ All generated types/members decorated with `[GeneratedCode]` attribute — v1.1

**v1.2 — Architecture Refactoring**
- ✓ Generator project reorganized with screaming architecture — v1.2
- ✓ Vertical slicing replaces horizontal layering — v1.2
- ✓ God classes decomposed into bite-sized, single-responsibility types — v1.2
- ✓ All existing tests continue to pass (behavior-preserving refactor) — v1.2

**v1.3 — Edge Case Stress Testing**
- ✓ Type system edge cases (TYPE-01..05) — v1.3
- ✓ Constructor variation edge cases (CTOR-01..05) — v1.3
- ✓ Parameter comparison correctness (COMP-01..04) — v1.3
- ✓ Diagnostic edge cases (DIAG-01..03) — v1.3
- ✓ Scope and accessibility diagnostics MFFG0012..MFFG0015 (SCOPE-01..05) — v1.3

**v2.0 — Converj Rename + Feature Expansion**
- ✓ Package renamed Motiv.FluentFactory → Converj — v2.0
- ✓ Attributes renamed `[FluentFactory]` → `[FluentRoot]`, `[FluentConstructor]` → `[FluentTarget]` — v2.0
- ✓ `BuilderMethod` → `TerminalMethod`, `InitialVerb` → `EagerVerb` — v2.0
- ✓ Static method targets — v2.0
- ✓ Extension method targets (`this` modifier and `[This]` attribute) — v2.0
- ✓ Property-backed fluent methods — v2.0
- ✓ Required property storage — v2.0
- ✓ Type-first builder mode (parameterless entry narrows to target) — v2.0
- ✓ Generic `FluentConstructorAttribute<T>` and `[As]` aliasing — v2.0
- ✓ Optional parameters and default values — v2.0
- ✓ `ReturnType` and `MethodPrefix` controls — v2.0
- ✓ Factory-level `CreateMethod` / `CreateVerb` defaults — v2.0
- ✓ Named tuple unpacking in fluent methods — v2.0
- ✓ Additional diagnostics: conflicting constraints, ambiguous chains, empty CreateVerb, `[This]` validation, missing storage — v2.0

**v2.1 — Naming Alignment Refactor**
- ✓ Internal type names aligned to Root/Target vocabulary (7 type renames) — v2.1
- ✓ Source file names aligned to renamed types via `git mv` — v2.1
- ✓ All 48 diagnostic descriptors aligned to `Category = "Converj"` — v2.1
- ✓ Test fixture and sample type names aligned (3 test class renames, bulk `Factory` → `Builder` in fixtures) — v2.1
- ✓ Documentation vocabulary aligned, repo-wide grep clean — v2.1
- ✓ 415 tests passing, zero behavior changes, full git history preserved — v2.1

### Active

<!-- Current scope. Building toward these. -->

## Current Milestone: v2.2 Fluent Collection Accumulation

**Goal:** Add `[FluentCollectionMethod]` parameter attribute enabling item-by-item collection building via repeated fluent calls

**Target features:**
- `[FluentCollectionMethod]` parameter attribute with auto-singularized method names and explicit override
- Configurable minimum items (0 or 1+) via attribute property
- Internal `List<T>` accumulation with terminal-time conversion to declared parameter type
- Composability with `[FluentMethod]` for combined accumulator + bulk-set on same parameter
- Error diagnostic when applied to non-collection parameter types
- Support for IEnumerable<T>, ICollection<T>, IList<T>, T[], IReadOnlyList<T>, IReadOnlyCollection<T>

### Out of Scope

<!-- Explicit boundaries. Includes reasoning to prevent re-adding. -->

- Namespace `Converj.*` restructuring — only internal type/file renames were done in v2.1
- Bug fixes for v1.3 tech debt (9 intentionally failing tests) — separate milestone
- Generated output format changes — consumers depend on current output

## Context

- Source generator using IIncrementalGenerator (Roslyn incremental pipeline)
- Code generation builds Roslyn syntax trees via SyntaxFactory
- Generated output is `.g.cs` files added to compilation
- 415 tests: 362 generator tests + 53 runtime tests, all passing
- Package published as `Converj` on NuGet, version 2.0.0
- 46,689 LOC C# across 5 projects
- Generator project uses screaming architecture: domain types at root, implementation in subdirectories (`ConstructorAnalysis/`, `ModelBuilding/`, `SyntaxGeneration/`, `TargetAnalysis/`, `Extensions/`)
- All source files ~150 lines or less with single responsibilities
- Internal vocabulary fully aligned to public `FluentRoot`/`FluentTarget` API as of v2.1
- 9 intentionally failing tests from v1.3 documenting known generator shortcomings (type system edge cases)

## Constraints

- **Target framework**: netstandard2.0 (generator and attributes)
- **Roslyn compatibility**: Must work with Microsoft.CodeAnalysis 4.x
- **Zero runtime overhead**: Generated code uses structs and aggressive inlining
- **Backward compatibility**: New attribute is additive — existing code without `[FluentCollectionMethod]` must produce identical output

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Use `global::` for all type references | Prevents namespace conflicts in consumer code | ✓ Good |
| Screaming architecture over horizontal layering | Key concepts visible at project root, details nested | ✓ Good |
| Vertical slicing over technical layers | Organize by feature/concern, not Analysis/Model/Generation | ✓ Good |
| Strategy pattern for constructor analysis | Pluggable storage detection, stateless strategies | ✓ Good |
| Thin orchestrator pattern for syntax generation | Method declarations delegate to focused helpers | ✓ Good |
| Shared `TypeParameterConstraintBuilder` | Single source of truth for constraint generation | ✓ Good |
| Rename `[FluentFactory]`→`[FluentRoot]`, `[FluentConstructor]`→`[FluentTarget]` | Generalizes beyond constructors to static/extension methods | ✓ Good |
| Keep internal rename as a dedicated milestone | Separation from feature work reduces risk and isolates rename churn in git history | ✓ Good |

---
*Last updated: 2026-04-14 after v2.2 milestone start*
