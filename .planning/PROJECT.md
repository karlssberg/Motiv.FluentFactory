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

## Current Milestone: v2.1 Naming Alignment Refactor

**Goal:** Align internal codebase vocabulary (type names, file names, test fixtures, diagnostic IDs) with the public `FluentRoot`/`FluentTarget` API shipped in v2.0.

**Target features:**
- Rename internal types carrying legacy `FluentFactory*` / `FluentConstructor*` vocabulary to `FluentRoot*` / `FluentTarget*`
- Rename source files to match renamed types
- Rename test fixture classes and sample types from old `*Factory*` terminology
- Rename diagnostic IDs from `MFFG` prefix to a Converj-aligned prefix
- Preserve all public API (no breaking changes to attribute names)
- All existing tests continue to pass after rename

### Active

<!-- Current scope. Building toward these. -->

- [ ] Internal type names aligned to Root/Target vocabulary
- [ ] Source file names aligned to renamed types
- [ ] Test fixture and sample type names aligned
- [ ] Diagnostic IDs aligned to Converj prefix

### Out of Scope

<!-- Explicit boundaries. Includes reasoning to prevent re-adding. -->

- Bug fixes for v1.3 tech debt — separate milestone; this one is pure renaming
- Feature additions — naming-only refactor, no behavior changes
- Generated output format changes — consumers would break
- Public attribute rename — already done in v2.0
- Namespace `Converj.*` restructuring — only internal type/file renames
- Test logic changes — only test class/fixture renames, not assertions

## Context

- Source generator using IIncrementalGenerator (Roslyn incremental pipeline)
- Code generation builds Roslyn syntax trees via SyntaxFactory
- Generated output is `.g.cs` files added to compilation
- Existing tests use `CSharpSourceGeneratorVerifier` with expected output comparison
- Package published as `Converj` on NuGet, version 2.0.0
- Generator project uses screaming architecture: domain types at root, implementation in subdirectories (`ConstructorAnalysis/`, `ModelBuilding/`, `SyntaxGeneration/`, `TargetAnalysis/`, `Extensions/`)
- All source files ~150 lines or less with single responsibilities
- Public attribute surface is stable as of v2.0 — internal vocabulary is the drift

## Constraints

- **Target framework**: netstandard2.0 (generator and attributes)
- **Roslyn compatibility**: Must work with Microsoft.CodeAnalysis 4.x
- **Zero runtime overhead**: Generated code uses structs and aggressive inlining
- **Behavior preservation**: This milestone is a pure rename — no observable output changes, no test assertion changes, no API breaks for consumers of public attributes

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
| Keep internal rename as a dedicated milestone | Separation from feature work reduces risk and isolates rename churn in git history | — Pending |

---
*Last updated: 2026-04-11 after v2.1 milestone start*
