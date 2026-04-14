# Roadmap: Converj

## Milestones

- ✅ **v1.0 Initial Release** — Phases 1-5 (shipped 2026-03-09)
- ✅ **v1.1 Code Generation Quality** — Phase 6 (shipped 2026-03-09)
- ✅ **v1.2 Architecture Refactoring** — Phases 7-10 (shipped 2026-03-11)
- ✅ **v1.3 Edge Case Stress Testing** — Phases 11-15 (shipped 2026-03-14)
- ✅ **v2.0 Converj Rename + Feature Expansion** — organic/unplanned (shipped 2026-04-11)
- ✅ **v2.1 Naming Alignment Refactor** — Phases 16-20 (shipped 2026-04-12)
- 🚧 **v2.2 Fluent Collection Accumulation** — Phases 21-24 (in progress)

## Phases

<details>
<summary>✅ v1.0 Initial Release (Phases 1-5) — SHIPPED 2026-03-09</summary>

Phases 1-5 delivered the initial release: attribute-based API, fluent step struct generation, generic type support, method customization, multiple fluent methods, NoCreateMethod, XML docs, diagnostics, primary constructor support, and NuGet packaging.

</details>

<details>
<summary>✅ v1.1 Code Generation Quality (Phase 6) — SHIPPED 2026-03-09</summary>

- [x] Phase 6: Generated Code Hardening (2/2 plans) — completed 2026-03-09

</details>

<details>
<summary>✅ v1.2 Architecture Refactoring (Phases 7-10) — SHIPPED 2026-03-11</summary>

- [x] Phase 7: Core Pipeline Decomposition (3/3 plans) — completed 2026-03-10
- [x] Phase 8: Syntax Generator Decomposition (3/3 plans) — completed 2026-03-11
- [x] Phase 9: Extension Consolidation (1/1 plan) — completed 2026-03-11
- [x] Phase 10: Screaming Architecture Reorganization (2/2 plans) — completed 2026-03-11

</details>

<details>
<summary>✅ v1.3 Edge Case Stress Testing (Phases 11-15) — SHIPPED 2026-03-14</summary>

- [x] Phase 11: Type System Edge Cases (2/2 plans) — completed 2026-03-14
- [x] Phase 12: Constructor Variation Edge Cases (2/2 plans) — completed 2026-03-14
- [x] Phase 13: Internal Correctness (2/2 plans) — completed 2026-03-14
- [x] Phase 14: Diagnostic Edge Cases (2/2 plans) — completed 2026-03-14
- [x] Phase 15: Scope and Accessibility Diagnostics (2/2 plans) — completed 2026-03-14

</details>

<details>
<summary>✅ v2.0 Converj Rename + Feature Expansion — SHIPPED 2026-04-11 (organic/unplanned)</summary>

v2.0 shipped as 65 commits of unplanned, organic work. See `.planning/MILESTONES.md` for full details of what shipped, including package/attribute rename, new target kinds (static/extension/property), type-first builder mode, and API expansion. Known tech debt (internal vocabulary still using `FluentFactory*`/`FluentConstructor*`) carried forward to v2.1.

</details>

<details>
<summary>✅ v2.1 Naming Alignment Refactor (Phases 16-20) — SHIPPED 2026-04-12</summary>

- [x] Phase 16: Diagnostic Alignment (3/3 plans) — completed 2026-04-12
- [x] Phase 17: Core Generator Type Renames (3/3 plans) — completed 2026-04-12
- [x] Phase 18: Builder Pattern Renames (1/1 plan) — completed 2026-04-12
- [x] Phase 19: Test Fixture Alignment (3/3 plans) — completed 2026-04-12
- [x] Phase 20: Documentation Cleanup & Final Verification (2/2 plans) — completed 2026-04-12

</details>

### 🚧 v2.2 Fluent Collection Accumulation (In Progress)

**Milestone Goal:** Add `[FluentCollectionMethod]` enabling item-by-item collection building via repeated fluent calls, with compile-time safety, zero runtime overhead, and zero impact on existing generated output.

- [ ] **Phase 21: Foundation** (5 plans) — Attribute, collection detection, singularization, diagnostics, and backward compatibility gate
- [ ] **Phase 22: Core Code Generation** — AccumulatorFluentStep emission, terminal conversion, and immutable field initialization
- [ ] **Phase 23: Composability** — `[FluentCollectionMethod]` alongside `[FluentMethod]` on the same parameter
- [ ] **Phase 24: MinItems Enforcement** — Compile-time minimum item count via two-step topology

## Phase Details

### Phase 21: Foundation
**Goal**: Developers can apply `[FluentCollectionMethod]` to collection parameters and receive immediate diagnostic feedback — the attribute exists, collection type detection is correct, singularization produces valid method names, name collision is detected, and all 415 existing tests continue to pass unmodified.
**Depends on**: Phase 20 (v2.1 complete)
**Requirements**: ATTR-01, ATTR-02, ATTR-03, NAME-01, NAME-02, NAME-03, NAME-04, BACK-01, BACK-02, BACK-03
**Success Criteria** (what must be TRUE):
  1. Developer can annotate a collection-typed parameter with `[FluentCollectionMethod]` and the project compiles without error
  2. Generator emits a diagnostic error when `[FluentCollectionMethod]` is applied to a non-collection parameter (e.g., `int`, `string`)
  3. Generator emits a diagnostic error when two accumulator method names would collide within the same root
  4. Auto-singularized method names are valid C# identifiers for common English plurals (`items` -> `AddItem`, `tags` -> `AddTag`, `categories` -> `AddCategory`)
  5. All 415 existing tests pass with zero assertion changes and non-attributed code produces byte-identical generated output
**Plans:** 3/5 plans executed
Plans:
- [ ] 21-01-PLAN.md — Wave 0 test scaffolding (six empty xUnit test classes ready for Plans 02–05)
- [ ] 21-02-PLAN.md — Public attribute + TypeName constant + CVJG0050/0051/0052 diagnostic descriptors + AnalyzerReleases.Unshipped entries
- [ ] 21-03-PLAN.md — StringExtensions.Singularize (irregulars + suffix rules) and CollectionParameterInfo carrier record
- [ ] 21-04-PLAN.md — FluentCollectionMethodAnalyzer wired into FluentTargetContext + CollectionParameters on TargetMetadata (ATTR-01/02/03, NAME-01/02/03 end-to-end)
- [ ] 21-05-PLAN.md — FilterCollectionAccumulatorCollisions (NAME-04 / CVJG0052) + byte-identical snapshot test (BACK-02) + full-suite verification (BACK-01, BACK-03)

### Phase 22: Core Code Generation
**Goal**: A developer using `[FluentCollectionMethod]` gets a working fluent chain — the generated step struct accumulates items via repeated `AddX()` calls, each call returns to the same step for further chaining, and the terminal method converts the internal `ImmutableArray<T>` to the declared parameter type.
**Depends on**: Phase 21
**Requirements**: GEN-01, GEN-02, GEN-03, GEN-04, GEN-05, GEN-06
**Success Criteria** (what must be TRUE):
  1. Developer can call `.AddItem(x).AddItem(y).AddItem(z).Create()` and the constructed object receives all three items in the declared collection type
  2. Branching the chain at an accumulator step produces independent results — two branches accumulating different items do not interfere with each other
  3. Generated accumulator step struct is `readonly` and all accumulator methods carry `[MethodImpl(AggressiveInlining)]`
  4. Accumulator field is initialized to `ImmutableArray<T>.Empty` (never uninitialized/default) so calling the terminal on a zero-item chain does not throw
  5. Generated code compiles for all six declared collection types: `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, and `T[]`
**Plans**: TBD

### Phase 23: Composability
**Goal**: A developer can apply both `[FluentCollectionMethod]` and `[FluentMethod]` to the same parameter and receive two distinct fluent paths — an item-by-item accumulator path and a bulk-set path — where choosing one path in a chain makes the other unavailable for that parameter.
**Depends on**: Phase 22
**Requirements**: COMP-01, COMP-02, COMP-03
**Success Criteria** (what must be TRUE):
  1. Developer can use `.AddTag("x").AddTag("y").Create()` (accumulator path) and `.WithTags(allTags).Create()` (bulk-set path) on the same root type, with both producing correct output
  2. After calling an accumulator method on a parameter, the bulk-set method for that parameter is not available in the chain (compile-time, no method present on the returned step type)
  3. After calling the bulk-set method on a parameter, the accumulator method for that parameter is not available in the chain (compile-time, no method present on the returned step type)
**Plans**: TBD

### Phase 24: MinItems Enforcement
**Goal**: A developer using `[FluentCollectionMethod(MinItems = N)]` where N >= 1 cannot call the terminal method until at least N items have been accumulated — enforced at compile time via distinct step types, not runtime guards.
**Depends on**: Phase 23
**Requirements**: MIN-01, MIN-02, MIN-03, MIN-04
**Success Criteria** (what must be TRUE):
  1. `[FluentCollectionMethod(MinItems = 0)]` (the default) makes the terminal method available immediately, before any `AddX()` call
  2. `[FluentCollectionMethod(MinItems = 1)]` makes the terminal method absent from the step type returned by the root entry point — it only appears after at least one `AddX()` call
  3. Attempting to call the terminal without meeting the minimum count is a compile error, not a runtime exception
  4. Generator emits a diagnostic error when `MinItems` is set to a negative value
**Plans**: TBD

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1-5. Initial Release | v1.0 | — | Complete | 2026-03-09 |
| 6. Generated Code Hardening | v1.1 | 2/2 | Complete | 2026-03-09 |
| 7. Core Pipeline Decomposition | v1.2 | 3/3 | Complete | 2026-03-10 |
| 8. Syntax Generator Decomposition | v1.2 | 3/3 | Complete | 2026-03-11 |
| 9. Extension Consolidation | v1.2 | 1/1 | Complete | 2026-03-11 |
| 10. Screaming Architecture Reorganization | v1.2 | 2/2 | Complete | 2026-03-11 |
| 11. Type System Edge Cases | v1.3 | 2/2 | Complete | 2026-03-14 |
| 12. Constructor Variation Edge Cases | v1.3 | 2/2 | Complete | 2026-03-14 |
| 13. Internal Correctness | v1.3 | 2/2 | Complete | 2026-03-14 |
| 14. Diagnostic Edge Cases | v1.3 | 2/2 | Complete | 2026-03-14 |
| 15. Scope and Accessibility Diagnostics | v1.3 | 2/2 | Complete | 2026-03-14 |
| — v2.0 organic work — | v2.0 | — | Complete | 2026-04-11 |
| 16. Diagnostic Alignment | v2.1 | 3/3 | Complete | 2026-04-12 |
| 17. Core Generator Type Renames | v2.1 | 3/3 | Complete | 2026-04-12 |
| 18. Builder Pattern Renames | v2.1 | 1/1 | Complete | 2026-04-12 |
| 19. Test Fixture Alignment | v2.1 | 3/3 | Complete | 2026-04-12 |
| 20. Documentation Cleanup & Final Verification | v2.1 | 2/2 | Complete | 2026-04-12 |
| 21. Foundation | 3/5 | In Progress|  | - |
| 22. Core Code Generation | v2.2 | 0/TBD | Not started | - |
| 23. Composability | v2.2 | 0/TBD | Not started | - |
| 24. MinItems Enforcement | v2.2 | 0/TBD | Not started | - |
