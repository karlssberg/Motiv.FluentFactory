# Roadmap: Motiv.FluentFactory

## Milestones

- ✅ **v1.0 Initial Release** — Phases 1-5 (shipped 2026-03-09)
- ✅ **v1.1 Code Generation Quality** — Phase 6 (shipped 2026-03-09)
- ✅ **v1.2 Architecture Refactoring** — Phases 7-10 (shipped 2026-03-11)
- 🚧 **v1.3 Edge Case Stress Testing** — Phases 11-15 (in progress)

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

### 🚧 v1.3 Edge Case Stress Testing (In Progress)

**Milestone Goal:** Systematically stress test the generator with edge cases to uncover bugs and shortcomings before NuGet publish. Tests that fail indicate discovered shortcomings — that is a success for this milestone.

## Phase Summary Checklist

- [x] **Phase 11: Type System Edge Cases** - Tests covering nullable annotations, parameter modifiers, arrays of generics, and deeply nested generic types
- [ ] **Phase 12: Constructor Variation Edge Cases** - Tests covering large parameter counts, records with explicit constructors, and constructor chaining
- [ ] **Phase 13: Internal Correctness** - Tests covering parameter equality, hash code contracts, overlapping method names, and Trie key collision behavior
- [ ] **Phase 14: Diagnostic Edge Cases** - Tests covering malformed attribute usage, invalid generic constraint combinations, and user code with compilation errors
- [ ] **Phase 15: Scope and Accessibility Diagnostics** - New diagnostic rules and tests covering constructor accessibility, inaccessible parameter types, missing partial modifier, and accessibility mismatches

## Phase Details

### Phase 11: Type System Edge Cases
**Goal**: The generator's behavior under unusual type system inputs is documented and observable via passing or failing tests
**Depends on**: Phase 10 (v1.2 complete — clean architecture baseline)
**Requirements**: TYPE-01, TYPE-02, TYPE-03, TYPE-04, TYPE-05
**Success Criteria** (what must be TRUE):
  1. A test exists that exercises nullable reference type annotations on parameters and produces a known result (pass or documented failure)
  2. A test exists that exercises `ref`, `out`, and `ref readonly` parameter modifiers and produces a known result
  3. A test exists that exercises arrays of generic types (e.g., `T[]`, `List<T>[]`) and produces a known result
  4. A test exists that exercises partially open generic types (e.g., `Dictionary<string, T>`) and produces a known result
  5. A test exists that exercises deeply nested generics (3+ levels) and produces a known result
**Plans:** 2/2 plans complete

Plans:
- [x] 11-01-PLAN.md — Nullable, generic array, partially open generic, and deep nested generic tests (TYPE-01, TYPE-03, TYPE-04, TYPE-05)
- [x] 11-02-PLAN.md — Parameter modifier diagnostic implementation and tests (TYPE-02)

### Phase 12: Constructor Variation Edge Cases
**Goal**: The generator's behavior under unusual constructor patterns is documented and observable via passing or failing tests
**Depends on**: Phase 11
**Requirements**: CTOR-01, CTOR-02, CTOR-03, CTOR-04, CTOR-05
**Success Criteria** (what must be TRUE):
  1. A test exists for constructors with 5+ parameters and produces a known result
  2. A test exists for records with explicit constructors alongside positional parameters and produces a known result
  3. A test exists for constructor chaining via `this(...)` calls and produces a known result
  4. A test exists for named arguments in constructor chaining and produces a known result
  5. A test exists for records mixing positional and explicit members and produces a known result
**Plans:** 2 plans

Plans:
- [ ] 12-01-PLAN.md — Large parameter count tests and record variation tests (CTOR-01, CTOR-02, CTOR-05)
- [ ] 12-02-PLAN.md — Constructor chaining and named argument tests (CTOR-03, CTOR-04)

### Phase 13: Internal Correctness
**Goal**: The generator's internal parameter comparison and Trie merging logic is exercised with edge case inputs that reveal correctness issues
**Depends on**: Phase 12
**Requirements**: COMP-01, COMP-02, COMP-03, COMP-04
**Success Criteria** (what must be TRUE):
  1. A test exists that exercises same-named types from different namespaces to verify the generator distinguishes them correctly
  2. A test exists that exercises overlapping FluentMethod names across parameters and produces a known result
  3. A test exists that validates hash code contract consistency — equal parameters must produce equal hash codes
  4. A test exists that exercises Trie key collisions from ambiguous parameter sequences and produces a known result
**Plans**: TBD

### Phase 14: Diagnostic Edge Cases
**Goal**: The generator's error reporting is exercised with malformed inputs, verifying it produces appropriate diagnostics rather than crashing or silently misbehaving
**Depends on**: Phase 13
**Requirements**: DIAG-01, DIAG-02, DIAG-03
**Success Criteria** (what must be TRUE):
  1. A test exists for malformed attribute usage (e.g., conflicting attribute arguments) that verifies the expected diagnostic is emitted
  2. A test exists for invalid generic constraint combinations that verifies the expected diagnostic is emitted or the generator handles it gracefully
  3. A test exists with user code containing compilation errors that verifies the generator exits cleanly without throwing an unhandled exception
**Plans**: TBD

### Phase 15: Scope and Accessibility Diagnostics
**Goal**: The generator emits diagnostics when constructors or types violate accessibility rules, and those diagnostic rules are implemented and tested
**Depends on**: Phase 14
**Requirements**: SCOPE-01, SCOPE-02, SCOPE-03, SCOPE-04, SCOPE-05
**Success Criteria** (what must be TRUE):
  1. The generator emits a diagnostic when `[FluentConstructor]` is applied to a private or protected constructor, and a test verifies this
  2. The generator emits a diagnostic when a parameter type is inaccessible from a public factory, and a test verifies this
  3. The generator emits a diagnostic when the factory root type is missing the `partial` modifier, and a test verifies this
  4. The generator emits a diagnostic for accessibility mismatch (e.g., public factory over internal type), and a test verifies this
  5. A test exists for nested private classes as factory targets and documents the generator's behavior (pass or diagnostic)
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 11 → 12 → 13 → 14 → 15

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1-5. Initial Release | v1.0 | — | Complete | 2026-03-09 |
| 6. Generated Code Hardening | v1.1 | 2/2 | Complete | 2026-03-09 |
| 7. Core Pipeline Decomposition | v1.2 | 3/3 | Complete | 2026-03-10 |
| 8. Syntax Generator Decomposition | v1.2 | 3/3 | Complete | 2026-03-11 |
| 9. Extension Consolidation | v1.2 | 1/1 | Complete | 2026-03-11 |
| 10. Screaming Architecture Reorganization | v1.2 | 2/2 | Complete | 2026-03-11 |
| 11. Type System Edge Cases | v1.3 | Complete    | 2026-03-14 | 2026-03-14 |
| 12. Constructor Variation Edge Cases | v1.3 | 0/2 | Not started | - |
| 13. Internal Correctness | v1.3 | 0/TBD | Not started | - |
| 14. Diagnostic Edge Cases | v1.3 | 0/TBD | Not started | - |
| 15. Scope and Accessibility Diagnostics | v1.3 | 0/TBD | Not started | - |
