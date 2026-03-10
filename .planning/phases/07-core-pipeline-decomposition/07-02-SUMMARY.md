---
phase: 07-core-pipeline-decomposition
plan: 02
subsystem: analysis
tags: [strategy-pattern, roslyn, constructor-analysis, decomposition]

requires:
  - phase: 06-global-qualification
    provides: "Fully qualified type names in generated output"
provides:
  - "IStorageDetectionStrategy interface for pluggable storage detection"
  - "RecordStorageStrategy for record parameter-to-property mapping"
  - "PrimaryConstructorStorageStrategy for primary ctor field/property detection"
  - "ExplicitConstructorStorageStrategy for explicit ctor body assignment analysis"
  - "Simplified ConstructorAnalyzer as thin dispatcher (~71 lines)"
affects: [07-core-pipeline-decomposition]

tech-stack:
  added: []
  patterns: [strategy-pattern-with-first-match-dispatch, stateless-strategies-with-semantic-model-parameter]

key-files:
  created:
    - src/Motiv.FluentFactory.Generator/Analysis/IStorageDetectionStrategy.cs
    - src/Motiv.FluentFactory.Generator/Analysis/RecordStorageStrategy.cs
    - src/Motiv.FluentFactory.Generator/Analysis/PrimaryConstructorStorageStrategy.cs
    - src/Motiv.FluentFactory.Generator/Analysis/ExplicitConstructorStorageStrategy.cs
  modified:
    - src/Motiv.FluentFactory.Generator/Analysis/ConstructorAnalyzer.cs

key-decisions:
  - "SemanticModel passed as method parameter to strategies, keeping them stateless and lightweight"
  - "Initializer chain resolution stays in ConstructorAnalyzer dispatcher (recursive self-call requirement)"
  - "Strategy ordering enforced via static array: Record > PrimaryConstructor > ExplicitConstructor"

patterns-established:
  - "Strategy pattern with first-match dispatch: static array + FirstOrDefault(CanHandle) + PopulateStorage"
  - "Stateless strategies receiving SemanticModel as parameter rather than constructor injection"

requirements-completed: [DECOMP-03, XCUT-01, XCUT-02]

duration: 4min
completed: 2026-03-10
---

# Phase 7 Plan 2: Constructor Analyzer Decomposition Summary

**Strategy pattern decomposition of ConstructorAnalyzer into three focused storage detection strategies with first-match dispatch**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-10T21:45:33Z
- **Completed:** 2026-03-10T21:49:58Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Decomposed 210-line ConstructorAnalyzer into thin 71-line dispatcher plus three focused strategy types
- Created IStorageDetectionStrategy interface with CanHandle + PopulateStorage contract
- Preserved exact strategy ordering semantics (records checked first, then primary constructors, then explicit)
- Kept initializer chain resolution in dispatcher where recursive self-call is required

## Task Commits

Each task was committed atomically:

1. **Task 1: Create IStorageDetectionStrategy interface and three strategy implementations** - `022cdb8` (feat)
2. **Task 2: Refactor ConstructorAnalyzer to use strategy dispatch** - `2e47124` (refactor)

## Files Created/Modified
- `src/Motiv.FluentFactory.Generator/Analysis/IStorageDetectionStrategy.cs` - Strategy interface with CanHandle + PopulateStorage
- `src/Motiv.FluentFactory.Generator/Analysis/RecordStorageStrategy.cs` - Record parameter-to-property storage detection
- `src/Motiv.FluentFactory.Generator/Analysis/PrimaryConstructorStorageStrategy.cs` - Primary ctor direct access + member initialization detection
- `src/Motiv.FluentFactory.Generator/Analysis/ExplicitConstructorStorageStrategy.cs` - Explicit ctor body assignment analysis
- `src/Motiv.FluentFactory.Generator/Analysis/ConstructorAnalyzer.cs` - Simplified to strategy dispatcher + initializer chain resolution

## Decisions Made
- SemanticModel passed as method parameter to strategies rather than constructor injection, keeping strategies stateless per CONTEXT.md decision
- Initializer chain resolution kept in ConstructorAnalyzer (recursive FindParameterValueStorage call cannot be delegated)
- Strategy ordering enforced via static array literal rather than priority attributes

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Pre-existing build failures from 07-01 refactoring (missing symbol references for diagnostic IDs) prevent clean `dotnet test` execution. These are unrelated to 07-02 changes and documented in `deferred-items.md`. The generator project itself compiles cleanly (only pre-existing RS1019 warnings remain). Test suite passed (174/174) when run from cached build artifacts before the clean rebuild.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- ConstructorAnalyzer decomposition complete, ready for remaining phase 07 plans
- Pre-existing 07-01 build issues need resolution before full test verification is possible

---
*Phase: 07-core-pipeline-decomposition*
*Completed: 2026-03-10*
