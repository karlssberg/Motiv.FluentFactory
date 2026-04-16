---
phase: 23-composability
plan: 04
subsystem: source-generator
tags: [roslyn, source-generator, diagnostics, overloads, collision-detection, CVJG0052]

# Dependency graph
requires:
  - phase: 23-composability-plan-03
    provides: CollectionPropertyInfo, CVJG0053, property-backed accumulator emission
  - phase: 23-composability-plan-02
    provides: AccumulatorBulkMethod, WithXs emission
  - phase: 22-core-code-generation
    provides: AccumulatorFluentStep, FindCollision, FilterCollectionAccumulatorCollisions
provides:
  - "Broad overload rule operational: signature-distinct same-name [FluentCollectionMethod] methods emit as C# overloads"
  - "CVJG0052 narrowed to name+signature collision; title and message updated"
  - "TargetContextFilter.FindCollision extended with HaveIdenticalAccumulatorSignature (name AND ElementType)"
  - "AnalyzerReleases.Unshipped.md CVJG0052 entry updated with Phase 23 Plan 04 notation"
  - "CollectionMethodOverloadingTests: 3 tests covering broad overload rule"
  - "DomainModel/FluentMethodSignatureEqualityComparerTests: FluentType equality + RefKind deferral doc"
  - "Retrofit audit table: all CVJG0008/0016/0022/0023/0040/0041/0043 descriptors classified"
affects: [23-composability-plan-05, 24-enforcement]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Accumulator signature comparison: SymbolEqualityComparer.Default on ElementType inside FindCollision"
    - "Broad overload rule: name-only detection → name+ElementType detection for collection-accumulator collisions"

key-files:
  created:
    - src/Converj.Generator.Tests/DomainModel/FluentMethodSignatureEqualityComparerTests.cs
  modified:
    - src/Converj.Generator/ModelBuilding/TargetContextFilter.cs
    - src/Converj.Generator/Diagnostics/FluentDiagnostics.cs
    - src/Converj.Generator/AnalyzerReleases.Unshipped.md
    - src/Converj.Generator.Tests/CollectionMethodOverloadingTests.cs

key-decisions:
  - "TargetContextFilter.FindCollision extended with HaveIdenticalAccumulatorSignature helper — compares MethodName AND ElementType via SymbolEqualityComparer.Default. Preserves first-collision-only UX pattern."
  - "AnalyzerReleases.Unshipped.md: 'Changed Rules' section is not valid in unshipped files (RS2007 build error). CVJG0052 Notes column updated instead to document the narrowing."
  - "RefKind deferral: FluentType derives its key from type display string only (no RefKind). Since ref/out are filtered upstream by FilterUnsupportedParameterModifierTargets, and 'in' produces identical generated signatures anyway, RefKind tracking deferred post-v2.2."
  - "CVJG0040/0041 are declared descriptors with no emission code — classified as unemitted, no action required."

requirements-completed: [COMP-01, COMP-02, COMP-03]

# Metrics
duration: 25min
completed: 2026-04-16
---

# Phase 23 Plan 04: Collision Detection Retrofit and Broad Overload Rule Summary

**CVJG0052 narrowed from name-only to name+ElementType collision; retrofit audit of all collision diagnostics complete; 467 tests passing with full broad-overload-rule coverage**

## Performance

- **Duration:** 25 min
- **Started:** 2026-04-16T01:00:00Z
- **Completed:** 2026-04-16T01:08:08Z
- **Tasks:** 2 (Task 1 implemented + committed; Task 2 audit-only, no code changes)
- **Files modified:** 5

## Accomplishments

- `TargetContextFilter.FindCollision` now compares both `MethodName` AND `ElementType` — signature-distinct same-name collection methods no longer trigger CVJG0052
- CVJG0052 title updated to "FluentCollectionMethod accumulator method has a name and signature collision within the same target"; message updated to reference "identical name AND parameter signature"
- `CollectionMethodOverloadingTests` with 3 scenarios: overloads emit correctly (no CVJG0052), same-signature still fires CVJG0052, generated output contains both overloaded methods
- Complete retrofit audit of CVJG0008/0016/0022/0023/0040/0041/0043 — all classified (see audit table below)
- `FluentMethodSignatureEqualityComparerTests` documenting `FluentType` equality semantics and RefKind deferral
- 467 tests passing (462 pre-existing + 5 new)

## CVJG0052 Message Before/After

**Before:**
- Title: `"Accumulator method name collision"`
- MessageFormat: `"Parameters '{0}' and '{1}' on '{2}' both produce accumulator method '{3}'. Disambiguate via [FluentCollectionMethod(\"AlternateName\")]."`

**After:**
- Title: `"FluentCollectionMethod accumulator method has a name and signature collision within the same target"`
- MessageFormat: `"Parameters '{0}' and '{1}' on '{2}' produce accumulator methods with identical name AND parameter signature '{3}'. Disambiguate via [FluentCollectionMethod(\"AlternateName\")]."`

## Retrofit Audit Table

| Descriptor | Trigger (before) | Trigger (after) | Tests changed | Notes |
|------------|------------------|-----------------|---------------|-------|
| CVJG0052 AccumulatorMethodNameCollision | Name-only match on `CollectionParameterInfo.MethodName` | Name + ElementType match via `HaveIdenticalAccumulatorSignature` | None — all 6 existing tests exercise same-signature collisions (still valid); 3 new tests added | PRIMARY narrowing target — done in Task 1 |
| CVJG0008 DuplicateTerminalMethodName | `(ResolvedName, TypeName)` group on terminal verbs | No change | None | Terminal methods are parameterless — signatures cannot differ; no narrowing applicable |
| CVJG0016 AmbiguousFluentMethodChain | Chain key `methodName:paramType` for all parameters | No change | None | Already signature-aware — chain key includes type as well as name; compliant |
| CVJG0022 OptionalParameterAmbiguousFluentMethodChain | Chain key on required params: `methodName:paramType` | No change | None | Already signature-aware — same chain key format as CVJG0016; compliant |
| CVJG0023 ConflictingTypeConstraints | First-parameter type with `GetEffectiveSignatureString()` (constraint-ignoring) | No change | None | Orthogonal: detects constraint-only differences on same underlying signature — not an overload scenario |
| CVJG0040 PropertyNameClash | N/A — descriptor declared, no emission code exists | No change | None | Unemitted — no generator code calls `Diagnostic.Create(FluentDiagnostics.PropertyNameClash, ...)` |
| CVJG0041 DuplicateFluentPropertyMethodName | N/A — descriptor declared, no emission code exists | No change | None | Unemitted — no generator code calls `Diagnostic.Create(FluentDiagnostics.DuplicateFluentPropertyMethodName, ...)` |
| CVJG0043 AmbiguousEntryMethod | Entry method name equality | No change | None | Entry methods are parameterless — signatures cannot differ; no narrowing applicable |

### FluentMethodSelector.ChooseCandidateFluentMethod

Already uses `FluentMethodSignatureEqualityComparer.Default` for grouping before selection — compliant with the broad overload rule as-is. Documented as "no change".

## FluentMethodSignatureEqualityComparer — RefKind Status

**Decision: Deferred post-v2.2.**

`FluentType` derives its equality key from `typeSymbol.GetEffectiveDisplayString()` — no `RefKind` component. This means `(T)` and `(in T)` compare as equal in the comparer.

**Why deferral is correct:**
1. `ref` and `out` parameters are rejected by `FilterUnsupportedParameterModifierTargets` before model building — they never reach the comparer.
2. The `in` modifier on user constructor parameters is accepted, but the generator ALREADY emits `in` for all value-type parameters in generated chain methods regardless of user modifier. So the generated fluent API signature is `WithX(in T value)` regardless — user-side `in` cannot create a distinct signature at the fluent-API level.
3. `ref readonly` parameters are filtered alongside `ref`.

Full RefKind propagation through `FluentMethodParameter` is deferred as noted in the `FluentMethodSignatureEqualityComparerTests.RefKind_in_modifier_is_not_distinguished_by_comparer_deferred` test.

## Task Commits

1. **Task 1: Narrow CVJG0052 + message update + overload tests + comparer doc** - `ebb919b` (feat)
2. **Task 2: Retrofit audit** - no code changes; audit findings captured in this SUMMARY

## Files Created/Modified

- `src/Converj.Generator/ModelBuilding/TargetContextFilter.cs` — `FindCollision` extended with `HaveIdenticalAccumulatorSignature` (name + ElementType comparison)
- `src/Converj.Generator/Diagnostics/FluentDiagnostics.cs` — CVJG0052 title + messageFormat updated
- `src/Converj.Generator/AnalyzerReleases.Unshipped.md` — CVJG0052 Notes column updated
- `src/Converj.Generator.Tests/CollectionMethodOverloadingTests.cs` — 3 tests: overloads-no-diagnostic, same-signature-still-fires, generated-output-has-both-overloads
- `src/Converj.Generator.Tests/DomainModel/FluentMethodSignatureEqualityComparerTests.cs` — NEW: FluentType equality + RefKind deferral documentation tests

## Decisions Made

**AnalyzerReleases.Unshipped.md format constraint:**
The RS2007 analyzer enforcement rejects `### Changed Rules` in unshipped release files — only `### New Rules` and `### Removed Rules` are valid. The CVJG0052 change is documented via an updated Notes column on the existing New Rules entry.

**HaveIdenticalAccumulatorSignature uses SymbolEqualityComparer.Default:**
Comparing `ITypeSymbol` element types via `SymbolEqualityComparer.Default` is the correct Roslyn pattern — it compares symbols by identity/metadata, not by display string. This handles generic type arguments correctly (e.g., `IList<string>` vs `IList<int>` — different symbols).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] AnalyzerReleases.Unshipped.md: Changed Rules section invalid**
- **Found during:** Task 1 (AnalyzerReleases.Unshipped.md update)
- **Issue:** Plan called for `### Changed Rules` section per Microsoft AnalyzerReleases spec, but RS2007 build error confirms that section is only valid in shipped release files, not unshipped.
- **Fix:** Updated Notes column on existing CVJG0052 `### New Rules` entry instead.
- **Files modified:** `src/Converj.Generator/AnalyzerReleases.Unshipped.md`
- **Committed in:** ebb919b (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - build-blocking format constraint)
**Impact on plan:** Minimal — release notes are documented; just in a valid format.

## Issues Encountered

- Transient MSBuild file-lock error on `Converj.Attributes.AssemblyInfoInputs.cache` — pre-existing environment issue, resolved by retry (same as Plan 23-03).

## Interaction Notes for Plan 23-05

- CVJG0052 test fixtures: all 6 existing `AccumulatorNameCollisionTests` tests exercise same-name + same-element-type collisions and continue to pass unchanged.
- CVJG0040 and CVJG0041 are unimplemented descriptors — if Plan 23-05 or later implements them, the broad overload rule applies: fire only when name AND signature match.
- The 3 new `CollectionMethodOverloadingTests` are regression guards for the broad overload rule.

## Next Phase Readiness

- COMP-01, COMP-02, COMP-03 requirements fulfilled: broad overload rule operational, all collision diagnostics audited.
- 467 tests passing, no known blockers.
- Plan 23-05 can proceed with final integration/regression testing.

---
*Phase: 23-composability*
*Completed: 2026-04-16*
