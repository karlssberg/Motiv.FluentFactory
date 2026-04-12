---
phase: 17-core-generator-type-renames
plan: "02"
subsystem: generator
tags: [roslyn, source-generator, rename, refactor, metadata]

# Dependency graph
requires:
  - phase: 17-01
    provides: FluentFactoryCompilationUnit renamed to FluentRootCompilationUnit; FluentModelFactory.CreateFluentFactoryCompilationUnit preserved

provides:
  - FluentRootMetadata record (renamed from FluentFactoryMetadata) at src/Converj.Generator/FluentRootMetadata.cs
  - FluentRootMetadataReader static class (renamed from FluentFactoryMetadataReader) at src/Converj.Generator/TargetAnalysis/FluentRootMetadataReader.cs
  - FluentRootDefaults sealed class (renamed from FluentFactoryDefaults) at src/Converj.Generator/TargetAnalysis/FluentRootDefaults.cs

affects:
  - 17-03
  - 18-builder-renames

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "git mv discipline for file renames to preserve history via git log --follow"
    - "Deferred method-name rename: GetFluentFactoryMetadata/GetFluentFactoryDefaults on FluentRootMetadataReader intentionally preserved for Phase 18"

key-files:
  created:
    - src/Converj.Generator/FluentRootMetadata.cs
    - src/Converj.Generator/TargetAnalysis/FluentRootMetadataReader.cs
    - src/Converj.Generator/TargetAnalysis/FluentRootDefaults.cs
  modified:
    - src/Converj.Generator/FluentTargetValidator.cs
    - src/Converj.Generator/TargetAnalysis/FluentTargetContextFactory.cs
    - src/Converj.Generator/TargetAnalysis/FluentTargetContext.cs

key-decisions:
  - "Method identifiers GetFluentFactoryMetadata and GetFluentFactoryDefaults on FluentRootMetadataReader are intentionally preserved until Phase 18 to keep this rename blast focused on type names only"

patterns-established:
  - "Type renames use git mv to preserve git log --follow history; type-name edits follow in the same commit"

requirements-completed: [NAME-03, FILE-01]

# Metrics
duration: 5min
completed: 2026-04-12
---

# Phase 17 Plan 02: Metadata Trio Rename Summary

**FluentFactoryMetadata/Reader/Defaults renamed to FluentRootMetadata/Reader/Defaults via git mv with history preserved and all three consumer call sites updated**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-04-12T01:12:00Z
- **Completed:** 2026-04-12T01:13:37Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- Three source files renamed via `git mv` so `git log --follow` preserves pre-rename history
- All three type names updated in place: `FluentRootMetadata`, `FluentRootMetadataReader`, `FluentRootDefaults`
- Six internal call sites updated across three consumer files; no test assertions changed
- Build: zero warnings (`-p:TreatWarningsAsErrors=true`); tests: 415/415 passing

## Task Commits

Each task was committed atomically:

1. **Task 1: git mv all three metadata files and rename types in place** - `41a6b40` (refactor)
2. **Task 2: Update consumers in FluentTargetValidator, FluentTargetContextFactory, FluentTargetContext** - `ff909a3` (refactor)

## Files Created/Modified

- `src/Converj.Generator/FluentRootMetadata.cs` - Renamed from FluentFactoryMetadata.cs; record renamed to FluentRootMetadata
- `src/Converj.Generator/TargetAnalysis/FluentRootMetadataReader.cs` - Renamed from FluentFactoryMetadataReader.cs; class renamed, return types updated
- `src/Converj.Generator/TargetAnalysis/FluentRootDefaults.cs` - Renamed from FluentFactoryDefaults.cs; class renamed to FluentRootDefaults
- `src/Converj.Generator/FluentTargetValidator.cs` - 2 FluentFactoryMetadataReader references updated to FluentRootMetadataReader
- `src/Converj.Generator/TargetAnalysis/FluentTargetContextFactory.cs` - 3 type-qualifier references + 1 parameter type updated
- `src/Converj.Generator/TargetAnalysis/FluentTargetContext.cs` - 1 constructor parameter type updated

## Decisions Made

- **Method-identifier deferral:** `GetFluentFactoryMetadata` and `GetFluentFactoryDefaults` on `FluentRootMetadataReader` are intentionally not renamed in this plan. Those method names contain legacy "FluentFactory" vocabulary but renaming them simultaneously with the type would create a wider edit blast. The method-name cleanup rides along with Phase 18 (which already renames `FluentModelFactory.CreateFluentFactoryCompilationUnit` for the same reason).

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## Next Phase Readiness

- NAME-03 and FILE-01 obligations for the three metadata files are satisfied
- `git grep -nP "(?<![A-Za-z])(FluentFactoryMetadata|FluentFactoryDefaults|FluentFactoryMetadataReader)(?![A-Za-z])" -- src/Converj.Generator/` returns zero hits (method-name substrings excluded by word-boundary regex as intended)
- Phase 17-03 can proceed; baseline test count is 415 (53 + 362)
- Phase 18 inherits the deferred method-name renames: `GetFluentFactoryMetadata`, `GetFluentFactoryDefaults`

---
*Phase: 17-core-generator-type-renames*
*Completed: 2026-04-12*
