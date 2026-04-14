---
phase: 21-foundation
plan: 05
subsystem: testing
tags: [roslyn, source-generator, diagnostics, tdd, collision-detection, snapshot-testing]

requires:
  - phase: 21-04
    provides: FluentCollectionMethodAnalyzer wired into pipeline; CVJG0050/0051 emitted; CollectionParameters populated on FluentTargetContext

provides:
  - FilterCollectionAccumulatorCollisions static method on TargetContextFilter
  - CVJG0052 diagnostic emitted when two [FluentCollectionMethod] params produce the same accumulator name
  - Skip-target-on-collision behaviour: sibling targets on the same root still generate
  - Six AccumulatorNameCollisionTests covering all NAME-04 scenarios
  - Two BACK-02 byte-identical snapshot tests locking pre-Phase-21 generated output
  - BACK-01 verification: 427 tests passing, 0 failing
  - Phase 21 fully closed

affects: [22-collection-generation]

tech-stack:
  added: []
  patterns:
    - "_skippedTargetDiagnostics separate from _diagnostics: collision errors on skipped targets stored separately to avoid triggering the error-bail-out guard, ensuring sibling targets still generate"
    - "Record-replay snapshot testing: write test with placeholder, observe verifier's actual diff, copy verbatim with $$VERSION$$ placeholder replacement"

key-files:
  created:
    - src/Converj.Generator.Tests/AccumulatorNameCollisionTests.cs (6 NAME-04 tests, fully implemented)
    - src/Converj.Generator.Tests/BackwardCompatibilitySnapshotTests.cs (2 BACK-02 snapshot tests)
  modified:
    - src/Converj.Generator/ModelBuilding/TargetContextFilter.cs (FilterCollectionAccumulatorCollisions + FindCollision)
    - src/Converj.Generator/FluentModelBuilder.cs (_skippedTargetDiagnostics field + wiring)

key-decisions:
  - "Collision diagnostics stored in _skippedTargetDiagnostics (not _diagnostics) so Error-severity CVJG0052 does not trigger the error-bail-out guard for sibling targets"
  - "First-collision-only per target: FindCollision returns first pair found (O(n²) nested loop), subsequent collisions surface on next compile after first fix — matches CVJG0011 UX"
  - "BACK-02 snapshots use $$VERSION$$ placeholder (resolved at runtime by CSharpSourceGeneratorVerifier) to avoid hardcoding assembly version"
  - "Fixture A (Cat 2-param ctor) and Fixture B (Widget 1-param ctor) chosen as snapshots: both exercise standard pipeline paths with no [FluentCollectionMethod] — confirm Phase 21 scaffolding does not perturb pre-21 output"

requirements-completed: [NAME-04, BACK-01, BACK-02, BACK-03]

duration: 45min
completed: 2026-04-14
---

# Phase 21 Plan 05: Collision Detection and Backward Compatibility Snapshot Summary

**CVJG0052 collision filter in TargetContextFilter with sibling-safe skipping; two byte-identical BACK-02 snapshot tests locking Cat and Widget fixtures; 427 tests passing closes Phase 21**

## Performance

- **Duration:** ~45 min
- **Started:** 2026-04-14T11:15:00Z
- **Completed:** 2026-04-14T12:05:00Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments

- Implemented `FilterCollectionAccumulatorCollisions` in `TargetContextFilter.cs`, emitting CVJG0052 when two `[FluentCollectionMethod]` parameters on the same target produce the same accumulator method name
- Wired the new filter between `FilterUnsupportedParameterModifierTargets` and `FilterInaccessibleTargets` in `FluentModelBuilder.CreateFluentRootCompilationUnit`
- Separated collision diagnostics from main `_diagnostics` into `_skippedTargetDiagnostics` to prevent Error-severity CVJG0052 from blocking generation of unaffected sibling targets
- Replaced the AccumulatorNameCollisionTests placeholder with 6 tests covering: derived-derived collision, derived-vs-explicit, explicit-vs-explicit, sibling-unaffected, no-collision (distinct names), and cross-target non-collision scope enforcement
- Added two BACK-02 byte-identical snapshot tests using record-replay methodology: Fixture A (Cat, 2 parameters) and Fixture B (Widget, 1 parameter)

## FilterCollectionAccumulatorCollisions Method

```csharp
// Signature in TargetContextFilter.cs
public static (ImmutableArray<FluentTargetContext> Valid, IEnumerable<Diagnostic> Diagnostics)
    FilterCollectionAccumulatorCollisions(
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
```

**Position in filter chain** (FluentModelBuilder.CreateFluentRootCompilationUnit):
1. `FilterUnsupportedParameterModifierTargets` (CVJG0011, Warning)
2. **`FilterCollectionAccumulatorCollisions`** (CVJG0052, Error — NEW)
3. `FilterInaccessibleTargets` (CVJG0012, Warning)
4. `FilterErrorTypeTargets`

## NAME-04 Test Scenarios

| Test | Scenario | Assertion |
|------|----------|-----------|
| 1 | Two params implicitly deriving same name ("babies"+"babys" → AddBaby) | CVJG0052 at first param location |
| 2 | Implicit derived name + explicit override both → same name (tags/AddTag + entries/"AddTag") | CVJG0052 at first param location |
| 3 | Two explicit overrides both → "AddItem" | CVJG0052 at first param location |
| 4 | Collision on TargetA; sibling TargetB collision-free | CVJG0052 for A + TargetB generated source present |
| 5 | Distinct explicit overrides ("AddTag"+"AddEntry") | No diagnostic; SkipGeneratedSourcesCheck |
| 6 | Same derived name on two different target types | No CVJG0052 (per-target scope only) |

## BACK-02 Snapshot Fixtures

| Fixture | Source fixture | Generated file | Size |
|---------|---------------|----------------|------|
| A (Representative) | `Cat(string name, int age)` in namespace `Animals` | `Animals.AnimalFactory.g.cs` | ~55 LOC |
| B (Noncollection baseline) | `Widget(string name)` in namespace `Widgets` | `Widgets.WidgetFactory.g.cs` | ~35 LOC |

Both fixtures contain no `[FluentCollectionMethod]` — they specifically verify that Phase 21 collection scaffolding does not perturb output for pre-21 usage patterns.

## Phase 21 Test Delta

| Metric | Pre-Phase-21 | Post-Plan-01 | Post-Plan-02 | Post-Plan-03 | Post-Plan-04 | Post-Plan-05 | Delta |
|--------|-------------|-------------|-------------|-------------|-------------|-------------|-------|
| Passing | 415 | 416 | 416 | 416 | 421 | 427 | +12 |
| Failing | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Total | 415 | 416 | 416 | 416 | 421 | 427 | +12 |

Net new Phase 21 tests: +12 (1 attributes test, 4 singularization/name tests, 1 FluentCollectionMethod attribute test, 6 collision tests − placeholders replaced).

## Task Commits

1. **Task 1: FilterCollectionAccumulatorCollisions + NAME-04 tests** - `91c8de6` (feat)
2. **Task 2: BACK-02 snapshot tests** - `7bbc3bc` (test)

## Files Created/Modified

- `src/Converj.Generator/ModelBuilding/TargetContextFilter.cs` — Added `FilterCollectionAccumulatorCollisions` static method and private `FindCollision` helper
- `src/Converj.Generator/FluentModelBuilder.cs` — Added `_skippedTargetDiagnostics` field; wired collision filter; separated skipped-target diagnostic reporting from error-bail-out guard
- `src/Converj.Generator.Tests/AccumulatorNameCollisionTests.cs` — Replaced placeholder with 6 NAME-04 tests
- `src/Converj.Generator.Tests/BackwardCompatibilitySnapshotTests.cs` — Replaced placeholder with 2 BACK-02 snapshot tests

## Decisions Made

- **Separate `_skippedTargetDiagnostics` from `_diagnostics`**: CVJG0052 is Error severity. If added directly to `_diagnostics`, the existing error-bail-out guard `if (_diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))` would prevent generation of sibling targets. Keeping them in a separate list and merging only at the final output (or early-return paths) preserves the "skip target, not root" semantics the plan requires.
- **First-collision-only per target**: Following CVJG0011's single-diagnostic-per-skipped-target pattern, `FindCollision` returns the first colliding pair. Subsequent collisions surface after the first is fixed.
- **`babies`/`babys` for Test 1**: The plan suggested `tags`/`tag` for the derived-collision scenario, but `tag` doesn't singularize (triggering CVJG0051 instead of collision). `babies` (Rule 2: -ies→-y) and `babys` (Rule 5: trailing-s) both derive `baby` → `AddBaby`, providing a genuine two-derived-same scenario.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] `_skippedTargetDiagnostics` separation to preserve sibling-target generation**
- **Found during:** Task 1 (collision filter wiring)
- **Issue:** CVJG0052 has Error severity. Adding it directly to `_diagnostics` before the error-bail-out guard caused the entire root's generation to be skipped even when only one target collided and siblings were clean.
- **Fix:** Introduced `_skippedTargetDiagnostics` DiagnosticList; collision diagnostics are stored there and merged into the final diagnostics output only at actual return points, never participating in the `Any(d => d.Severity == Error)` check.
- **Files modified:** `src/Converj.Generator/FluentModelBuilder.cs`
- **Verification:** Test 4 `Collision_on_one_target_does_not_affect_sibling_target` passes — CVJG0052 emitted, TargetB generated source present.
- **Committed in:** `91c8de6` (Task 1 commit)

**2. [Rule 1 - Bug] Test 1 parameter names changed from `tags`/`tag` to `babies`/`babys`**
- **Found during:** Task 1 (TDD RED phase)
- **Issue:** `tag` does not singularize (no rule fires → CVJG0051 emitted, parameter excluded from CollectionParameters). The planned `tags`/`tag` scenario produces CVJG0051, not a collision. To test the genuinely-derived-same-name scenario, I needed two names that both singularize to the same root via different rules.
- **Fix:** Used `babies` (Rule 2: -ies→-y → baby → AddBaby) and `babys` (Rule 5: trailing-s → baby → AddBaby).
- **Files modified:** `src/Converj.Generator.Tests/AccumulatorNameCollisionTests.cs`
- **Verification:** Test 1 passes with CVJG0052 referencing both `babies` and `babys`.
- **Committed in:** `91c8de6` (Task 1 commit)

---

**Total deviations:** 2 auto-fixed (both Rule 1 - Bug)
**Impact on plan:** Both fixes were necessary for correctness. No scope creep.

## Issues Encountered

None beyond the two auto-fixed items above.

## Next Phase Readiness

Phase 21 is fully closed:
- ATTR-01: `[FluentCollectionMethod]` attribute defined
- ATTR-02: Attribute compiles with parameterless and named-argument forms
- ATTR-03: CVJG0050 emitted for non-collection parameter types
- NAME-01: Singularization works (IList<string> tags → AddTag)
- NAME-02: Explicit name override works (no diagnostic)
- NAME-03: CVJG0051 emitted for unsingularizable names
- NAME-04: CVJG0052 emitted for cross-parameter name collisions (this plan)
- BACK-01: 427 tests pass, 0 fail
- BACK-02: Two byte-identical snapshot tests lock pre-Phase-21 output shape
- BACK-03: 0 intentionally-failing tests (none exist in this codebase at this milestone)

**Phase 22 (Core Code Generation) can begin.** Integration notes for Phase 22:
- `CollectionParameters` on `FluentTargetContext` contains validated `CollectionParameterInfo` entries (MethodName, ElementType, MinItems) — Phase 22 uses these to generate `AccumulatorFluentStep` structs
- Extension method targets + type-first mode with collection parameters have nuances not yet detailed — must be addressed in Phase 22 test planning

---
*Phase: 21-foundation*
*Completed: 2026-04-14*

## Self-Check: PASSED
