---
phase: 18-builder-pattern-renames
plan: "01"
subsystem: refactoring
tags: [roslyn, source-generator, rename, vocabulary-alignment]

requires:
  - phase: 17-core-generator-type-renames
    provides: "Core generator type renames (FluentFactory* -> FluentRoot*), file mv discipline established"

provides:
  - "FluentModelBuilder replaces FluentModelFactory"
  - "FluentMethodBuilder replaces FluentMethodFactory"
  - "IgnoredMultiMethodWarningBuilder replaces IgnoredMultiMethodWarningFactory"
  - "CreateFluentRootCompilationUnit replaces CreateFluentFactoryCompilationUnit"
  - "GetFluentRootMetadata replaces GetFluentFactoryMetadata"
  - "GetFluentRootDefaults replaces GetFluentFactoryDefaults"
  - "Zero legacy FluentModelFactory/FluentMethodFactory/IgnoredMultiMethodWarningFactory/CreateFluentFactory*/GetFluentFactory* references in src/Converj.Generator/"

affects: [19-test-fixtures, 20-docs-final-verification]

tech-stack:
  added: []
  patterns:
    - "git mv used for all file renames to preserve log --follow history"
    - "Builder suffix replaces Factory suffix for internal GoF-style helper types"
    - "FluentRoot* vocabulary applied to method identifiers on already-renamed types"

key-files:
  created:
    - src/Converj.Generator/FluentModelBuilder.cs
    - src/Converj.Generator/ModelBuilding/FluentMethodBuilder.cs
    - src/Converj.Generator/Diagnostics/IgnoredMultiMethodWarningBuilder.cs
  modified:
    - src/Converj.Generator/FluentRootGenerator.cs
    - src/Converj.Generator/ModelBuilding/FluentMethodSelector.cs
    - src/Converj.Generator/TargetAnalysis/FluentRootMetadataReader.cs
    - src/Converj.Generator/TargetAnalysis/FluentTargetContextFactory.cs
    - src/Converj.Generator/FluentTargetValidator.cs

key-decisions:
  - "Builder suffix chosen over Factory for internal helper types to remove ambiguity with [FluentRoot]/[FluentTarget] GoF-factory vocabulary shift"
  - "Phase 17 deferred method renames (CreateFluentFactoryCompilationUnit, GetFluentFactoryMetadata, GetFluentFactoryDefaults) fully discharged in this plan"
  - "XML doc comments referencing 'the fluent factory' in a conceptual or GoF sense were left untouched per plan guidance"

patterns-established:
  - "FluentRoot* vocabulary: method identifiers on generator types now use FluentRoot prefix matching the public [FluentRoot] attribute"

requirements-completed: [NAME-05, NAME-06, NAME-07]

duration: 10min
completed: 2026-04-12
---

# Phase 18 Plan 01: Builder Pattern Renames Summary

**Three GoF-style helper types renamed from *Factory to *Builder vocabulary plus six legacy FluentFactory* method identifiers discharged from Phase 17 deferral, zeroing all Factory-vocabulary naming drift in src/Converj.Generator/**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-04-12T00:00:00Z
- **Completed:** 2026-04-12T00:10:00Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments

- Renamed FluentModelFactory -> FluentModelBuilder, FluentMethodFactory -> FluentMethodBuilder, IgnoredMultiMethodWarningFactory -> IgnoredMultiMethodWarningBuilder using git mv (history preserved)
- Updated all call sites: FluentRootGenerator.cs, FluentMethodSelector.cs (including local variable rename)
- Renamed three deferred method identifiers: CreateFluentFactoryCompilationUnit -> CreateFluentRootCompilationUnit, GetFluentFactoryMetadata -> GetFluentRootMetadata, GetFluentFactoryDefaults -> GetFluentRootDefaults
- Updated all method call sites across FluentRootGenerator, FluentTargetContextFactory, FluentTargetValidator
- Phase-level grep gate returns zero hits; 415 tests pass with zero warnings

## Task Commits

Each task was committed atomically:

1. **Task 1: git mv and rename three *Factory types to *Builder** - `42d0b93` (refactor)
2. **Task 2: Rename deferred FluentFactory* method identifiers** - `d4318b3` (refactor)

**Plan metadata:** (docs commit pending)

## Files Created/Modified

- `src/Converj.Generator/FluentModelBuilder.cs` - Renamed from FluentModelFactory.cs; class renamed to FluentModelBuilder; CreateFluentRootCompilationUnit method
- `src/Converj.Generator/ModelBuilding/FluentMethodBuilder.cs` - Renamed from FluentMethodFactory.cs; class renamed to FluentMethodBuilder
- `src/Converj.Generator/Diagnostics/IgnoredMultiMethodWarningBuilder.cs` - Renamed from IgnoredMultiMethodWarningFactory.cs; class renamed to IgnoredMultiMethodWarningBuilder
- `src/Converj.Generator/FluentRootGenerator.cs` - Updated: new FluentModelBuilder, .CreateFluentRootCompilationUnit
- `src/Converj.Generator/ModelBuilding/FluentMethodSelector.cs` - Updated: FluentMethodBuilder field type, IgnoredMultiMethodWarningBuilder local variable
- `src/Converj.Generator/TargetAnalysis/FluentRootMetadataReader.cs` - GetFluentRootMetadata, GetFluentRootDefaults
- `src/Converj.Generator/TargetAnalysis/FluentTargetContextFactory.cs` - Updated two call sites
- `src/Converj.Generator/FluentTargetValidator.cs` - Updated two GetFluentRootDefaults call sites

## Decisions Made

- Builder suffix used for renamed internal helper types to eliminate ambiguity between GoF "factory" and the public [FluentRoot]/[FluentTarget] API renamed away from "FluentFactory" in v2.0
- Phase 17 deferred obligation fully discharged: CreateFluentFactoryCompilationUnit, GetFluentFactoryMetadata, GetFluentFactoryDefaults all renamed to FluentRoot* vocabulary

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All FluentFactory* type names and method identifiers removed from src/Converj.Generator/
- Phase 18 plan 01 success criteria fully satisfied
- Ready for Phase 19 (test fixtures) and eventually Phase 20 (docs + final verification)

---
*Phase: 18-builder-pattern-renames*
*Completed: 2026-04-12*
