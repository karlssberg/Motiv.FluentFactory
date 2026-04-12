---
phase: 19-test-fixture-alignment
plan: 01
subsystem: testing
tags: [roslyn, source-generator, rename, refactoring]

requires:
  - phase: 18-builder-pattern-renames
    provides: production code already renamed to Builder vocabulary
provides:
  - Generator test fixtures aligned with v2.0 Builder vocabulary
affects: [19-03]

tech-stack:
  added: []
  patterns: []

key-files:
  created:
    - src/Converj.Generator.Tests/EmptyRootTests.cs
    - src/Converj.Generator.Tests/NestedRootTests.cs
  modified:
    - src/Converj.Generator.Tests/*.cs (53 files)

key-decisions:
  - "GoF Factory property names (e.g., Func<> Factory) preserved — only fluent root sample types renamed"
  - "Compound Factory names (AnimalFactory, WrapperFactory, etc.) renamed alongside bare Factory"

patterns-established: []

requirements-completed: [TEST-01, TEST-02, TEST-04]

duration: 15min
completed: 2026-04-12
---

# Plan 19-01: Generator Test Fixture Renames Summary

**Renamed all generator test `Factory` sample types to `Builder`, including class/file renames, step struct names, .g.cs file references, and FluentConstructor→FluentTarget comment updates across 53 files**

## Performance

- **Duration:** 15 min
- **Tasks:** 2
- **Files modified:** 53

## Accomplishments
- EmptyFactoryTests → EmptyRootTests and NestedFactoryTests → NestedRootTests (class + file via git mv)
- All `partial class Factory` sample types renamed to `partial class Builder` across generator test fixtures
- All expected generated output updated: step struct names (`__Factory` → `__Builder`), .g.cs file names, partial class extensions
- All `FluentConstructor` references in comments/method names updated to `FluentTarget`
- Compound Factory types renamed: AnimalFactory→AnimalBuilder, WrapperFactory→WrapperBuilder, etc.
- GoF `PropositionFactory` types and `Factory` property names preserved unchanged
- Diagnostic span adjusted for `NonFactoryType→NonRootType` rename (3-char shift)

## Task Commits

1. **Task 1: git mv EmptyFactoryTests and NestedFactoryTests** - `b5433eb` (refactor)
2. **Task 2: Bulk rename Factory to Builder** - `dc347aa` (refactor)

## Files Created/Modified
- `src/Converj.Generator.Tests/EmptyRootTests.cs` - Renamed from EmptyFactoryTests
- `src/Converj.Generator.Tests/NestedRootTests.cs` - Renamed from NestedFactoryTests
- 51 other test files - Factory→Builder in source strings and expected output

## Decisions Made
- Renamed compound `*Factory` names (AnimalFactory, FactoryA, etc.) beyond the plan's explicit verification criteria for thoroughness
- Preserved `Factory` as a property name in MethodCustomizationTests (GoF delegate factory pattern)

## Deviations from Plan
None significant — extended scope to catch compound Factory names the plan didn't explicitly list.

## Issues Encountered
- Agent crash required manual completion of Task 2
- MalformedAttributeTests span offset needed adjustment after NonFactoryType→NonRootType rename (3-char difference)

## Next Phase Readiness
- Generator tests fully aligned with Builder vocabulary
- Ready for 19-03 grep gate verification

---
*Phase: 19-test-fixture-alignment*
*Completed: 2026-04-12*
