---
phase: 23-composability
plan: 03
subsystem: source-generator
tags: [roslyn, source-generator, accumulator, collection, property, diagnostics, CVJG0053]

# Dependency graph
requires:
  - phase: 23-composability-plan-02
    provides: AccumulatorBulkMethod, WithXs emission, COMP-01/02/03 parameter path
  - phase: 22-core-code-generation
    provides: AccumulatorFluentStep, AccumulatorStepDeclaration, terminal emission infrastructure
provides:
  - "[FluentCollectionMethod] on target-type properties — full property parity with parameter path"
  - "CollectionPropertyInfo — property-side carrier mirroring CollectionParameterInfo"
  - "FluentTargetContext.CollectionProperties — parallel to CollectionParameters"
  - "CVJG0053 — UnsupportedCollectionPropertyAccessor descriptor + AnalyzerReleases entry"
  - "AccumulatorStepDeclaration extended — property fields, constructors, AddX, object-initializer terminal"
  - "StringExtensions.ToPropertyFieldName — _camelCase__property field naming"
affects: [23-composability-plan-04, 23-composability-plan-05, 24-enforcement]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Property field naming: _camelCase__property (distinct from _camelCase__parameter)"
    - "Object-initializer terminal emission for property-backed accumulators"
    - "Parallel CollectionProperties on AccumulatorFluentStep alongside CollectionParameters"
    - "AnalyzeProperties iterates GetMembers().OfType<IPropertySymbol>() with attribute check"

key-files:
  created:
    - src/Converj.Generator/TargetAnalysis/CollectionPropertyInfo.cs
  modified:
    - src/Converj.Attributes/FluentCollectionMethodAttribute.cs
    - src/Converj.Generator/TargetAnalysis/FluentCollectionMethodAnalyzer.cs
    - src/Converj.Generator/TargetAnalysis/FluentTargetContext.cs
    - src/Converj.Generator/Models/Steps/AccumulatorFluentStep.cs
    - src/Converj.Generator/FluentModelBuilder.cs
    - src/Converj.Generator/SyntaxGeneration/AccumulatorStepDeclaration.cs
    - src/Converj.Generator/Extensions/StringExtensions.cs
    - src/Converj.Generator/Diagnostics/FluentDiagnostics.cs
    - src/Converj.Generator/AnalyzerReleases.Unshipped.md
    - src/Converj.Generator.Tests/PropertyBackedCollectionTests.cs
    - src/Converj.Generator.Tests/CollectionMethodPropertyAccessorDiagnosticTests.cs
    - src/Converj.Generator.Tests/FluentCollectionMethodAttributeTests.cs

key-decisions:
  - "Architecture: Option B-lite (parallel CollectionProperties on AccumulatorFluentStep) rather than shared ICollectionAccumulatorTarget interface — keeps parameter path entirely unchanged with zero risk of regression"
  - "CVJG0053 triggering set: (1) record primary-constructor positional property — cannot be re-assigned via object initializer; (2) property with no set or init accessor — cannot be assigned at terminal time"
  - "Property field naming uses __property suffix (_tags__property) to distinguish from parameter fields (_tags__parameter) — prevents field name collision when both appear on the same step"
  - "FluentCollectionMethodAttributeTests.Applying_to_type_is_compile_error updated: CS0592 argument changed from 'parameter' to 'property, indexer, parameter' after attribute widening (Rule 1 auto-fix)"
  - "CVJG0053 added to FluentDiagnostics in Task 1 (blocking compile dependency) rather than Task 3 as planned — both descriptor and AnalyzerReleases entry committed atomically"
  - "CVJG0053 test approach: [property: FluentCollectionMethod] attribute forwarding syntax triggers detection via AnalyzeProperties on the auto-generated positional record property"

patterns-established:
  - "Property-backed accumulator test: use SkipGeneratedSourcesCheck for tests not asserting full snapshot"
  - "Property accumulator field: _name__property in entry/copy constructors, Empty initialization, assignment"
  - "Terminal method branching: EmitTerminalForParameterBackedAccumulator (with optional property initializers) vs BuildStaticMethodInvocation"

requirements-completed: [COMP-01, COMP-02]

# Metrics
duration: 20min
completed: 2026-04-16
---

# Phase 23 Plan 03: Property-Backed Collection Accumulator Summary

**[FluentCollectionMethod] widened to target-type properties with object-initializer terminal emission, CollectionPropertyInfo carrier, and CVJG0053 for unsupported accessor shapes**

## Performance

- **Duration:** 20 min
- **Started:** 2026-04-16T00:33:35Z
- **Completed:** 2026-04-16T00:53:49Z
- **Tasks:** 3
- **Files modified:** 12

## Accomplishments

- `[FluentCollectionMethod]` now accepted on `AttributeTargets.Parameter | AttributeTargets.Property`, enabling property-side accumulator declarations
- Property-backed accumulator steps emit `_name__property` fields, `AddX` self-returning methods, and terminal with `{ PropName = field.ToArray() }` object-initializer syntax
- CVJG0053 descriptor declared and wired: fires for record primary-constructor positional properties and read-only (no-setter) properties; sibling targets unaffected (skip-on-error isolation)
- 462 total tests passing (456 pre-existing + 6 new)

## Task Commits

1. **Task 1: Widen attribute + CollectionPropertyInfo + extend analyzer** - `e0d7cb6` (feat)
2. **Task 2: Wire property accumulators + object-initializer terminal** - `e7dfd34` (feat)
3. **Task 3: CVJG0053 diagnostic tests** - `02718a8` (test)

## Files Created/Modified

- `src/Converj.Attributes/FluentCollectionMethodAttribute.cs` — `AttributeTargets.Parameter | AttributeTargets.Property`
- `src/Converj.Generator/TargetAnalysis/CollectionPropertyInfo.cs` — NEW: property-side carrier record
- `src/Converj.Generator/TargetAnalysis/FluentCollectionMethodAnalyzer.cs` — `AnalyzeProperties` entry point, CVJG0053 detection, `IsRecordPrimaryConstructorPositionalProperty`
- `src/Converj.Generator/TargetAnalysis/FluentTargetContext.cs` — `CollectionProperties` property, `AnalyzeProperties` wiring
- `src/Converj.Generator/Models/Steps/AccumulatorFluentStep.cs` — `CollectionProperties` parallel to `CollectionParameters`
- `src/Converj.Generator/FluentModelBuilder.cs` — accumulator trigger extended to `CollectionProperties.Length > 0`, step populated with `CollectionProperties`
- `src/Converj.Generator/SyntaxGeneration/AccumulatorStepDeclaration.cs` — `CreatePropertyAccumulatorFieldDeclarations`, `BuildPropertyEmptyInitializations`, `CreatePropertyAddMethods`, `EmitTerminalWithPropertyInitializers`, `BuildCopyConstructorArgumentsWithProperties`
- `src/Converj.Generator/Extensions/StringExtensions.cs` — `ToPropertyFieldName` extension
- `src/Converj.Generator/Diagnostics/FluentDiagnostics.cs` — `UnsupportedCollectionPropertyAccessor` (CVJG0053)
- `src/Converj.Generator/AnalyzerReleases.Unshipped.md` — CVJG0053 entry
- `src/Converj.Generator.Tests/PropertyBackedCollectionTests.cs` — 4 tests (1 diagnostic-only, 3 source-gen)
- `src/Converj.Generator.Tests/CollectionMethodPropertyAccessorDiagnosticTests.cs` — 4 CVJG0053 tests

## Decisions Made

**Architecture: Option B-lite (parallel collections) vs Option A (shared interface)**
Chose parallel `CollectionProperties` on `AccumulatorFluentStep` alongside existing `CollectionParameters`. This keeps the entire parameter path (AccumulatorMethod, AccumulatorBulkMethod, existing terminal logic) entirely unchanged, eliminating regression risk. The emission side in `AccumulatorStepDeclaration` handles both paths with explicit branching via parallel helper methods.

**CVJG0053 triggering set (final):**
1. Record primary-constructor positional property — detected via `IsRecord && FindPrimaryConstructor().Parameters.Any(p => name matches)`
2. Property with no set or init accessor (`SetMethod is null`) — cannot be assigned at terminal time

Properties with `private set` are not rejected (they can be assigned via object initializer from within the assembly, and the generated code is in the same compilation).

**Interaction with existing property-storage diagnostics:**
- CVJG0038 (`FluentMethodOnPropertyWithoutSetter`) fires for `[FluentMethod]` on no-setter properties
- CVJG0053 fires for `[FluentCollectionMethod]` on no-setter properties  
- No double-firing: `[FluentMethod]` and `[FluentCollectionMethod]` are separate attributes analyzed by separate paths

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] FluentCollectionMethodAttributeTests.Applying_to_type_is_compile_error expected argument updated**
- **Found during:** Task 1 (attribute widening)
- **Issue:** Test expected CS0592 with argument `"parameter"` but after widening, C# compiler produces `"property, indexer, parameter"`
- **Fix:** Updated the `.WithArguments(...)` call to match the new compiler message
- **Files modified:** `src/Converj.Generator.Tests/FluentCollectionMethodAttributeTests.cs`
- **Committed in:** e0d7cb6 (Task 1 commit)

**2. [Rule 3 - Blocking] CVJG0053 descriptor added in Task 1 (not Task 3)**
- **Found during:** Task 1 (AnalyzeProperties implementation)
- **Issue:** `FluentCollectionMethodAnalyzer.AnalyzeProperties` references `FluentDiagnostics.UnsupportedCollectionPropertyAccessor` which was planned for Task 3; compile fails without it
- **Fix:** Added CVJG0053 descriptor and AnalyzerReleases entry in Task 1 commit
- **Files modified:** `FluentDiagnostics.cs`, `AnalyzerReleases.Unshipped.md`
- **Committed in:** e0d7cb6 (Task 1 commit)

---

**Total deviations:** 2 auto-fixed (1 Rule 1, 1 Rule 3)
**Impact on plan:** Both necessary for correctness. Task 3's scope reduced to diagnostic test pinning only (descriptor was pre-committed in Task 1).

## Issues Encountered

- Transient MSBuild file-lock errors on `Converj.Attributes.AssemblyInfoInputs.cache` — resolved by retrying build (pre-existing environment issue, not caused by this plan's changes)
- CVJG0053 test scenario required careful interpretation: the attribute must be applied via `[property: FluentCollectionMethod]` forwarding syntax to trigger the property-path CVJG0053 check, not via the parameter-path `[FluentCollectionMethod]` on the parameter itself

## CVJG0053 Triggering Set (for Plan 23-04 collision audit)

| Shape | Triggers | Reason |
|-------|----------|--------|
| Record positional property | CVJG0053 | Cannot re-assign via object initializer |
| No-setter property (`get` only) | CVJG0053 | Cannot be assigned at terminal time |
| `get; init;` property (class or record body) | None | Fully supported |
| `get; set;` property | None | Fully supported |
| `private set` property | None | Not rejected (assigned in generated code) |

## Interaction Notes for Plan 23-04

- CVJG0038 (FluentMethodOnPropertyWithoutSetter) and CVJG0053 target different attributes — no double-firing risk
- CVJG0039 (FluentMethodPropertyWithBuilderNone), CVJG0040 (PropertyNameClash), CVJG0041 (DuplicateFluentPropertyMethodName) all fire in `FluentPropertyAnalyzer` path — independent from `AnalyzeProperties` path used by CVJG0053
- Plan 23-04 should audit: name collision between a property-backed accumulator method (e.g., `AddTag`) and a parameter-backed accumulator method with the same derived name on the same step

## Next Phase Readiness

- COMP-01 and COMP-02 requirements fulfilled: `[FluentCollectionMethod]` works on both parameters and properties
- Plan 23-04: `[FluentMethod]` (bulk/WithXs) on property-backed accumulators not yet implemented — the `HasFluentMethodAttribute` check only handles `CollectionParameterInfo`, not `CollectionPropertyInfo`. Property-backed bulk transition methods are deferred to Plan 23-04.
- 462 tests passing, no known blockers

---
*Phase: 23-composability*
*Completed: 2026-04-16*
