# Requirements: Motiv.FluentFactory

**Defined:** 2026-03-12
**Core Value:** Developers write constructor parameters once and get a complete, type-safe fluent builder API generated automatically

## v1.3 Requirements

Requirements for edge case stress testing milestone. Each maps to roadmap phases.

### Type System

- [x] **TYPE-01**: Generator handles nullable reference type annotations on parameters
- [x] **TYPE-02**: Generator handles `ref`, `out`, and `ref readonly` parameter modifiers
- [x] **TYPE-03**: Generator handles arrays of generic types (e.g., `T[]`, `List<T>[]`)
- [x] **TYPE-04**: Generator handles partially open generic types (e.g., `Dictionary<string, T>`)
- [x] **TYPE-05**: Generator handles deeply nested generics (3+ levels, e.g., `Func<List<KeyValuePair<T, U>>, bool>`)

### Constructor Variations

- [ ] **CTOR-01**: Generator handles constructors with 5+ parameters
- [ ] **CTOR-02**: Generator handles records with explicit constructors alongside positional parameters
- [x] **CTOR-03**: Generator handles constructor chaining (`this(...)` calls)
- [x] **CTOR-04**: Generator handles named arguments in constructor chaining
- [ ] **CTOR-05**: Generator handles records mixing positional and explicit members

### Parameter Comparison

- [ ] **COMP-01**: Generator correctly distinguishes same-named types from different namespaces
- [ ] **COMP-02**: Generator handles overlapping FluentMethod names across parameters
- [ ] **COMP-03**: Generator maintains hash code contract consistency for parameter equality
- [ ] **COMP-04**: Generator handles Trie key collisions from ambiguous parameter sequences

### Diagnostics

- [ ] **DIAG-01**: Generator reports diagnostic for malformed attribute usage
- [ ] **DIAG-02**: Generator reports diagnostic for invalid generic constraint combinations
- [ ] **DIAG-03**: Generator gracefully handles user code with compilation errors

### Scope & Accessibility

- [ ] **SCOPE-01**: Generator reports diagnostic for private/protected constructors marked `[FluentConstructor]`
- [ ] **SCOPE-02**: Generator reports diagnostic for inaccessible parameter types in public factory
- [ ] **SCOPE-03**: Generator reports diagnostic for missing `partial` modifier on factory root type
- [ ] **SCOPE-04**: Generator reports diagnostic for accessibility mismatch (e.g., public factory for internal type)
- [ ] **SCOPE-05**: Generator handles nested private classes as factory targets

## Future Requirements

None — this is a focused testing milestone.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Bug fixes for discovered issues | This milestone identifies; fixes are a separate milestone |
| Performance/benchmark testing | Different concern, not edge case correctness |
| Test refactoring | Existing tests work; refactoring is separate |
| Fuzzing/property-based testing | Fixed edge case tests chosen as approach |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| TYPE-01 | Phase 11 | Tests written (11-01) |
| TYPE-02 | Phase 11 | Complete |
| TYPE-03 | Phase 11 | Tests written (11-01) |
| TYPE-04 | Phase 11 | Tests written (11-01) |
| TYPE-05 | Phase 11 | Tests written (11-01) |
| CTOR-01 | Phase 12 | Pending |
| CTOR-02 | Phase 12 | Pending |
| CTOR-03 | Phase 12 | Complete |
| CTOR-04 | Phase 12 | Complete |
| CTOR-05 | Phase 12 | Pending |
| COMP-01 | Phase 13 | Pending |
| COMP-02 | Phase 13 | Pending |
| COMP-03 | Phase 13 | Pending |
| COMP-04 | Phase 13 | Pending |
| DIAG-01 | Phase 14 | Pending |
| DIAG-02 | Phase 14 | Pending |
| DIAG-03 | Phase 14 | Pending |
| SCOPE-01 | Phase 15 | Pending |
| SCOPE-02 | Phase 15 | Pending |
| SCOPE-03 | Phase 15 | Pending |
| SCOPE-04 | Phase 15 | Pending |
| SCOPE-05 | Phase 15 | Pending |

**Coverage:**
- v1.3 requirements: 22 total
- Mapped to phases: 22
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-12*
*Last updated: 2026-03-12 after roadmap creation*
