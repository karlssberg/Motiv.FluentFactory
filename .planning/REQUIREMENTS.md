# Requirements: Converj v2.2 — Fluent Collection Accumulation

**Defined:** 2026-04-14
**Core Value:** Developers write constructor parameters once and get a complete, type-safe fluent builder API generated automatically

## v2.2 Requirements

Requirements for this milestone. Each maps to roadmap phases.

### Attribute & Detection

- [x] **ATTR-01**: Developer can apply `[FluentCollectionMethod]` to a collection-typed constructor or target-method parameter
- [x] **ATTR-02**: Generator recognizes `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, and `T[]` as valid collection parameter types
- [x] **ATTR-03**: Generator emits an error diagnostic when `[FluentCollectionMethod]` is applied to a non-collection parameter

### Method Naming

- [x] **NAME-01**: Generated accumulator method uses `Add` prefix followed by a singularized form of the parameter name (e.g., `items` → `AddItem`, `tags` → `AddTag`)
- [x] **NAME-02**: Developer can override the generated method name via attribute argument: `[FluentCollectionMethod("AddEntry")]`
- [x] **NAME-03**: Singularization handles common English plural suffixes (`-s`, `-es`, `-ies`→`-y`) and a small set of irregulars
- [x] **NAME-04**: Generator emits an error diagnostic when two generated accumulator method names would collide within the same root

### Core Code Generation

- [ ] **GEN-01**: Calling the accumulator method returns to the same step position, allowing repeated invocation in a chain
- [ ] **GEN-02**: Terminal method materializes the accumulated items into the declared parameter type (`IEnumerable<T>`, `IList<T>`, `T[]`, `IReadOnlyList<T>`, etc.)
- [ ] **GEN-03**: Chain branching at the accumulator step produces independent results (no shared mutation between branches)
- [ ] **GEN-04**: Generated step struct field uses `ImmutableArray<T>` with `.Empty` initialization (never `default`) to guarantee safe enumeration
- [ ] **GEN-05**: Element type of accumulator method parameter matches the collection's generic element type
- [ ] **GEN-06**: Generated accumulator step struct remains `readonly` and supports `[MethodImpl(AggressiveInlining)]`

### Composability

- [ ] **COMP-01**: `[FluentCollectionMethod]` can be applied alongside `[FluentMethod]` on the same parameter
- [ ] **COMP-02**: When both attributes are present, both an accumulator method (singularized, from `[FluentCollectionMethod]`) and a bulk-set method (plural, from `[FluentMethod]`) are generated
- [ ] **COMP-03**: In a chain, accumulator and bulk-set methods are mutually exclusive per parameter — choosing one path excludes the other

### MinItems Enforcement

- [ ] **MIN-01**: Developer can specify minimum accumulation count via `[FluentCollectionMethod(MinItems = N)]`
- [ ] **MIN-02**: Default `MinItems` is 0 (empty accumulation is valid; terminal is immediately available)
- [ ] **MIN-03**: When `MinItems >= 1`, terminal method is not available in the chain until the minimum number of items has been accumulated (compile-time enforcement via distinct seed and continuation step types)
- [ ] **MIN-04**: Generator emits an error diagnostic when `MinItems` is negative

### Backward Compatibility

- [x] **BACK-01**: All 415 existing tests continue to pass with zero assertion changes
- [x] **BACK-02**: Code not using `[FluentCollectionMethod]` produces byte-identical generated output as before v2.2
- [x] **BACK-03**: The 9 intentionally failing tests from v1.3 remain in their current state (not fixed in this milestone, not further broken)

## v3 Requirements

Deferred to future milestones.

### Collection Extensions

- **FUTURE-01**: `AddRange` variant — single call accepting `IEnumerable<T>` to add multiple items at once
- **FUTURE-02**: Dictionary accumulation — `[FluentDictionaryMethod]` for `IDictionary<K,V>` with key/value pairs
- **FUTURE-03**: `params T[]` overload on accumulator method for variadic calls

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| `ClearX()` / reset methods | Conflicts with immutable-step design; "add zero items" is already the default state |
| Runtime validation of MinItems | Compile-time enforcement via distinct step types is the Converj guarantee |
| `ImmutableList<T>` backing | Worse constant factors for typical builder scale (1-20 items); `ImmutableArray<T>` chosen |
| `List<T>` field storage | Silent correctness bug at chain branch points due to shared reference in struct copies |
| Non-generic collections (`ArrayList`, `IEnumerable`) | Type-unsafe; no element type information for accumulator method parameter |
| `Dictionary<K,V>` support | Different semantics (key/value pairs); out of v2.2, see FUTURE-02 |
| Humanizer NuGet dependency | Requires SDK 9.0.200+, brings transitive deps into analyzer package; hand-rolled singularization sufficient |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| ATTR-01 | Phase 21 | Complete |
| ATTR-02 | Phase 21 | Complete |
| ATTR-03 | Phase 21 | Complete |
| NAME-01 | Phase 21 | Complete |
| NAME-02 | Phase 21 | Complete |
| NAME-03 | Phase 21 | Complete |
| NAME-04 | Phase 21 | Complete |
| BACK-01 | Phase 21 | Complete |
| BACK-02 | Phase 21 | Complete |
| BACK-03 | Phase 21 | Complete |
| GEN-01 | Phase 22 | Pending |
| GEN-02 | Phase 22 | Pending |
| GEN-03 | Phase 22 | Pending |
| GEN-04 | Phase 22 | Pending |
| GEN-05 | Phase 22 | Pending |
| GEN-06 | Phase 22 | Pending |
| COMP-01 | Phase 23 | Pending |
| COMP-02 | Phase 23 | Pending |
| COMP-03 | Phase 23 | Pending |
| MIN-01 | Phase 24 | Pending |
| MIN-02 | Phase 24 | Pending |
| MIN-03 | Phase 24 | Pending |
| MIN-04 | Phase 24 | Pending |

**Coverage:**
- v2.2 requirements: 23 total
- Mapped to phases: 23 ✓
- Unmapped: 0

---
*Requirements defined: 2026-04-14*
*Last updated: 2026-04-14 — traceability updated after roadmap creation*
