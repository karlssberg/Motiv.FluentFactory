# Milestones
## v2.1 — Naming Alignment Refactor

**Shipped:** 2026-04-12
**Phases:** 16-20 (5 phases, 12 plans)
**Commits:** 44
**Files modified:** 137 (+5,665 / -3,050 lines)
**Timeline:** 2 days (2026-04-11 → 2026-04-12)
**LOC:** 46,689 C#

### What Shipped

Pure rename refactor aligning internal codebase vocabulary with the public `FluentRoot`/`FluentTarget` API shipped in v2.0. No feature additions, no behavior changes, no generated output changes. All 415 tests passing at every phase boundary.

**Key accomplishments:**
- Aligned all 48 diagnostic descriptors to `Category = "Converj"` and FluentRoot vocabulary; fixed CVJG0031 `[FluentFactory]` defect inline
- Rewrote AnalyzerReleases.Unshipped.md from 18 to 48 rows synchronized with FluentDiagnostics.cs
- Renamed core generator types: `FluentFactoryGenerator` → `FluentRootGenerator`, `FluentFactoryCompilationUnit` → `FluentRootCompilationUnit`, metadata trio → `FluentRoot*`
- Renamed `FluentFactoryMethodDeclaration` → `StepTerminalMethodDeclaration`, `FluentRootFactoryMethodDeclaration` → `RootTerminalMethodDeclaration`
- Renamed internal GoF helpers: `FluentModelFactory` → `FluentModelBuilder`, `FluentMethodFactory` → `FluentMethodBuilder`, `IgnoredMultiMethodWarningFactory` → `IgnoredMultiMethodWarningBuilder`
- Renamed test fixtures: `EmptyFactoryTests` → `EmptyRootTests`, `NestedFactoryTests` → `NestedRootTests`, `NestedFactoryRuntimeTests` → `NestedRootRuntimeTests`; all `class Factory` sample types → `class Builder`
- Final verification: zero residual legacy vocabulary in active source/tests/docs, all `git mv` history preserved

### Requirements: 25/25 Complete

- NAME-01..07: All internal type renames complete
- FILE-01..02: All source file renames via `git mv`, zero legacy file paths
- DIAG-01..04: All diagnostic vocabulary aligned
- TEST-01..05: All test fixture renames complete
- DOC-01..03: Documentation aligned, repo-wide grep clean
- BEHAV-01..04: Build green, tests green, compiler-assisted renames, git history preserved

---


## v2.0 — Converj Rename + Feature Expansion

**Shipped:** 2026-04-11
**Phases:** None (organic, unplanned work)
**Git range:** `7117a90..ef9a652` (65 commits)

### What Shipped

**Package and API rename:**
- Renamed `Motiv.FluentFactory` → `Converg` → `Converj`; all namespaces updated
- Renamed attributes `[FluentFactory]` → `[FluentRoot]`, `[FluentConstructor]` → `[FluentTarget]`
- Renamed `BuilderMethod` → `TerminalMethod`, `InitialVerb` → `EagerVerb`, `BuilderMethod.First` → `BuilderMethod.Eager`
- Replaced `FluentOptions` with `CreateMethod` for method generation control
- Removed unused `Priority` property from attributes
- Version bump to 2.0.0

**New target kinds:**
- Static method targets — `[FluentTarget]` on static methods, terminal calls the method
- Extension method targets — methods with `this` modifier or `[This]` on first parameter; chain starts as extension method on receiver type
- Property-backed fluent methods — `[FluentMethod]` on properties with accessors
- Required properties as fluent storage (PropertyFieldStorage parallel collection)
- Type-first builder mode — parameterless entry methods that narrow chain to a specific target type (trie-of-tries, `Builder.InitialMethod`)

**API expansion:**
- Generic `FluentConstructorAttribute<T>` support
- `[As]` alias attribute for aliasing generic type parameters and local type parameter resolution on existing partial types
- Support for optional parameters and default values
- `ReturnType` support for creation method return types
- `MethodPrefix` support for controlling fluent method naming
- Factory-level defaults for `CreateMethod` and `CreateVerb`
- Named tuple unpacking in fluent methods
- Partial parameter overlap support
- Receiver parameter propagation across type-first steps

**Diagnostics additions:**
- Conflicting type constraints
- Ambiguous fluent method chains from optional parameters
- Empty `CreateVerb` scenarios
- `[This]` attribute validation and root constraints
- Constructor parameters without storage in custom steps
- `FluentStorage` support with refined parameter handling

**Internal refactoring:**
- Decomposed `FluentModelFactory` via extraction of `TargetContextFilter`, `PropertyStepEnricher`, `ParameterBindingResolver`
- Extracted `BuildCreationMethod`, `BuildPropertyStepChain`
- Threaded `FluentStepBuilder` as parameter (eliminated mutable swap)
- Organized generator root into `Domain/`, `Models/` subdirectories
- Decomposed `FluentMethodFactory.CreateFluentMethods` into helpers
- Made all generated structs `readonly`
- Renamed `CandidateConstructors` → `CandidateTargets`

**Docs + infra:**
- Added project `CLAUDE.md`
- README updates for renamed attributes, generic syntax, static/extension method targets
- `#nullable enable` and auto-generated headers on all generated source files
- x64/x86 configurations
- `.worktrees` in `.gitignore`

### Known Tech Debt (→ v2.1)

- Internal type names still carry old `FluentFactory*` / `FluentConstructor*` vocabulary
- Source file names still reflect old API (`FluentFactoryGenerator.cs`, `FluentFactoryMetadata.cs`, `FluentModelFactory.cs`, etc.)
- Test fixture names still use `*Factory*` terminology
- Diagnostic IDs still use `MFFG` prefix ("Motiv FluentFactory Generator")

---

## v1.3 — Edge Case Stress Testing

**Shipped:** 2026-03-14
**Phases:** 11-15 (5 phases, 10 plans)
**Status:** Complete with documented tech debt (see `v1.3-MILESTONE-AUDIT.md`)

### What Shipped

- **Type System (TYPE-01..05):** Tests for nullable annotations, ref/out/ref-readonly modifiers, arrays of generics, partially open generics, deeply nested generics
- **Constructor Variations (CTOR-01..05):** Tests for 5+ parameter constructors, records with explicit constructors, `this(...)` chaining, named arguments, positional + explicit member mixes
- **Parameter Comparison (COMP-01..04):** Same-named types from different namespaces, overlapping FluentMethod names, hash code contract consistency, Trie key collisions
- **Diagnostics (DIAG-01..03):** Malformed attribute usage, invalid generic constraint combinations, compilation error resilience
- **Scope & Accessibility (SCOPE-01..05):** MFFG0012 (inaccessible constructor), MFFG0013 (missing partial modifier), MFFG0014 (inaccessible parameter types), MFFG0015 (accessibility mismatch), nested private class handling

**Tech debt carried forward:** 9 intentionally failing tests documenting generator shortcomings (type system crashes, Trie signature collisions, IErrorTypeSymbol handling, nested private class generation).

---

## v1.2 — Architecture Refactoring

**Shipped:** 2026-03-11
**Phases:** 7-10 (4 phases, 9 plans)
**Git range:** `286be64..c5d3865` (32 commits)
**Stats:** 104 files changed, +3,815 / -2,357 lines, 5,708 LOC total

### What Shipped
- Decomposed 3 god classes (FluentModelFactory, FluentFactoryGenerator, ConstructorAnalyzer) into 11 focused single-responsibility types
- Extracted shared TypeParameterConstraintBuilder, fixing a constraint qualification bug
- Decomposed syntax generation classes into thin orchestrators with focused helpers
- Consolidated duplicate extension methods into 4 concern-based extension files
- Reorganized project to screaming architecture with domain concepts at root
- Split all files to ~150 lines with single responsibilities

---


## v1.0 — Initial Release

**Shipped:** 2026-03-09
**Phases:** 1-5 (inferred from existing codebase)

### What Shipped
- Attribute-based API for fluent factory generation
- Fluent step struct generation with chainable methods
- Generic type support including nested generics
- Method name customization and priority ordering
- Multiple fluent methods per parameter via templates
- NoCreateMethod option for custom partial types
- XML documentation generation
- 10 diagnostics for validation errors
- Primary constructor support
- NuGet packaging with bundled generator and attributes

## v1.1 — Code Generation Quality

**Shipped:** 2026-03-09
**Phases:** 6

### What Shipped
- All generated type references use fully qualified `global::` names to avoid namespace conflicts
- All generated types/members decorated with `[GeneratedCode("Motiv.FluentFactory", "version")]` attribute
