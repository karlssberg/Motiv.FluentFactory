---
phase: 21-foundation
plan: 04
subsystem: generator
tags: [roslyn, source-generator, collection-detection, diagnostics, tdd]

# Dependency graph
requires:
  - phase: 21-02
    provides: FluentCollectionMethodAttribute, CVJG0050/0051/0052 diagnostic descriptors, TypeName.FluentCollectionMethodAttribute
  - phase: 21-03
    provides: CollectionParameterInfo record, StringExtensions.Singularize(), StringExtensions.Capitalize()
provides:
  - FluentCollectionMethodAnalyzer: per-parameter analyzer detecting [FluentCollectionMethod] on constructor and static method parameters
  - FluentTargetContext.CollectionParameters / CollectionDiagnostics: wired for all target types
  - TargetMetadata.CollectionParameters: downstream propagation ready for Phase 22 consumers
  - CVJG0050 emission for non-allowlisted collection types (string, List<T>, Dictionary, HashSet, Stack, Queue)
  - CVJG0051 emission for unsingularizable names (data, events → keyword)
  - NAME-02 explicit override bypasses singularization entirely
affects: [21-05, 22-collection-accumulation]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "SyntaxFacts.GetKeywordKind for C# keyword detection in accumulator name derivation"
    - "ImmutableArray.CreateBuilder<T>() for accumulation in analyzer loops (no LINQ hot path)"
    - "TestBehaviors.SkipGeneratedSourcesCheck for diagnostic-only tests where generated output is verified in snapshot plan"

key-files:
  created:
    - src/Converj.Generator/TargetAnalysis/FluentCollectionMethodAnalyzer.cs
  modified:
    - src/Converj.Generator/TargetAnalysis/FluentTargetContext.cs
    - src/Converj.Generator/TargetMetadata.cs
    - src/Converj.Generator/FluentModelBuilder.cs
    - src/Converj.Generator.Tests/CollectionTypeDetectionTests.cs
    - src/Converj.Generator.Tests/AccumulatorNameOverrideTests.cs
    - src/Converj.Generator.Tests/SingularizationTests.cs

key-decisions:
  - "Analyze() runs unconditionally for all target types (constructors, static, extension) — instance method targets are already filtered upstream"
  - "CollectionDiagnostics is a separate DiagnosticList property parallel to PropertyDiagnostics for diagnostic source clarity"
  - "TestBehaviors.SkipGeneratedSourcesCheck used for ATTR-02 positive tests — generated output snapshot deferred to Plan 05"
  - "SyntaxFacts.GetKeywordKind check guards against keyword singularizations (events→event, params→param)"
  - "DetectCollection does NOT walk AllInterfaces — prevents string from being accepted as IEnumerable<char>"

patterns-established:
  - "Analyzer pattern: per-parameter static analysis returning ImmutableArray<T> + DiagnosticList, plugged into context constructor"
  - "Diagnostic location: prefer attr.ApplicationSyntaxReference?.GetSyntax().GetLocation(), fall back to parameter.Locations.FirstOrDefault()"

requirements-completed: [ATTR-01, ATTR-02, ATTR-03, NAME-01, NAME-02, NAME-03]

# Metrics
duration: 45min
completed: 2026-04-14
---

# Phase 21 Plan 04: FluentCollectionMethodAnalyzer Integration Summary

**`FluentCollectionMethodAnalyzer` wired into Step 2 pipeline: CVJG0050/0051 emit for invalid/unsingularizable collection parameters; `CollectionParameters` flows through `FluentTargetContext` → `TargetMetadata` for Phase 22 consumption**

## Performance

- **Duration:** 45 min
- **Started:** 2026-04-14T12:00:00Z
- **Completed:** 2026-04-14T12:45:00Z
- **Tasks:** 1 (TDD)
- **Files modified:** 7

## Accomplishments

- Created `FluentCollectionMethodAnalyzer` with `DetectCollection` (array + 5 SpecialType generics, no AllInterfaces walk), `TryDeriveAccumulatorName` (Singularize + SyntaxFacts keyword check), `ReadMinItemsNamedArg`, and `GetAttributeLocation` helpers
- Extended `FluentTargetContext` with `CollectionParameters` and `CollectionDiagnostics` properties; analyzer runs before the static/instance-method short-circuit, covering all target types
- Extended `TargetMetadata.CollectionParameters` to propagate results downstream without re-analysis
- Updated `FluentModelBuilder` to collect `CollectionDiagnostics` alongside `PropertyDiagnostics` (before the error-exit gate, so CVJG0050/0051 errors block generation correctly)
- Filled `CollectionTypeDetectionTests` with 6 ATTR-02 positive + 7 ATTR-03 negative + 1 Pitfall-1 regression tests
- Filled `AccumulatorNameOverrideTests` with NAME-02 (explicit override) + NAME-03 (unsingularizable + keyword veto) tests
- Appended NAME-01 end-to-end integration test to `SingularizationTests`
- Full suite: 421 tests pass (up from 415)

## Task Commits

1. **Task 1: FluentCollectionMethodAnalyzer + wiring + tests** - `1df3958` (feat)

## Files Created/Modified

- `src/Converj.Generator/TargetAnalysis/FluentCollectionMethodAnalyzer.cs` — New static analyzer class; `Analyze()`, `DetectCollection()`, `TryDeriveAccumulatorName()`, `ReadMinItemsNamedArg()`, `GetAttributeLocation()` helpers
- `src/Converj.Generator/TargetAnalysis/FluentTargetContext.cs` — Added `CollectionParameters` and `CollectionDiagnostics` properties; analyzer call before static/instance short-circuit
- `src/Converj.Generator/TargetMetadata.cs` — Added `CollectionParameters` property sourced from `targetContext.CollectionParameters`
- `src/Converj.Generator/FluentModelBuilder.cs` — Added collection diagnostics collection loop after property diagnostics
- `src/Converj.Generator.Tests/CollectionTypeDetectionTests.cs` — Replaced placeholder; 14 tests covering ATTR-02/ATTR-03 and Pitfall 1
- `src/Converj.Generator.Tests/AccumulatorNameOverrideTests.cs` — Replaced placeholder; 4 tests for NAME-02/NAME-03
- `src/Converj.Generator.Tests/SingularizationTests.cs` — Appended 1 end-to-end integration test (tags → AddTag)

## Decisions Made

- Analyzer runs unconditionally for all target types — instance methods are already filtered upstream, so no extra guard needed
- Used `TestBehaviors.SkipGeneratedSourcesCheck` for positive diagnostic tests (ATTR-02, NAME-02) since Phase 21 doesn't yet emit accumulator step code; generated output snapshot is Plan 05 work
- `CollectionDiagnostics` is a separate `DiagnosticList` (parallel to `PropertyDiagnostics`) for clarity — `FluentModelBuilder` collects both in sequence
- `FluentModelExtensions.GetDiagnostics()` (cross-context validator) was NOT modified — collection diagnostics are context-scoped and collected separately in `FluentModelBuilder`, matching the property diagnostic pattern

## Deviations from Plan

None — plan executed exactly as written. The `FluentModelExtensions.GetDiagnostics()` approach documented in the plan was superseded by the simpler per-context `PropertyDiagnostics` pattern already in `FluentModelBuilder`; no change to that file was needed.

## Requirements Status

| Requirement | Status |
|-------------|--------|
| ATTR-01 | Closed — attribute recognized; parameter carries CollectionParameterInfo |
| ATTR-02 | Closed — 6 allowlisted types detected; all produce no CVJG0050 |
| ATTR-03 | Closed — string, List<T>, Dictionary, HashSet, Stack, Queue produce CVJG0050 |
| NAME-01 | Closed — suffix rules produce Add{Singular}; integration test verifies tags→AddTag |
| NAME-02 | Closed — explicit name bypasses singularization; tested with AddEntry and AddDatum |
| NAME-03 | Closed — data/events produce CVJG0051 (no rule / keyword veto) |
| NAME-04 | Pending — collision detection (CVJG0052) deferred to Plan 05 |
| BACK-02 | Pending — byte-identical backward-compat snapshot deferred to Plan 05 |

## Issues Encountered

- Test framework requires exact diagnostic spans; computed spans from `GetAttributeLocation` pointing to `[FluentCollectionMethod]` attribute syntax matched `(13, 20, 13, 42)` consistently for `BuildSource`-based fixtures
- `string?` nullability on `SyntaxFacts.GetKeywordKind` required explicit null-forgiving operator since `string.IsNullOrWhiteSpace` doesn't narrow the type in netstandard2.0

## Next Phase Readiness

- `CollectionParameters` is populated on `TargetMetadata` and ready for Phase 22's accumulator step generation
- CVJG0050/0051 fire correctly; CVJG0052 (name collision) remains for Plan 05
- Plan 05 snapshot test should verify the actual generated `AddTag` method name end-to-end

---
*Phase: 21-foundation*
*Completed: 2026-04-14*
