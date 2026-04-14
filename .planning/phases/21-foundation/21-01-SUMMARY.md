---
phase: 21-foundation
plan: 01
subsystem: testing
tags: [xunit, roslyn, source-generator, test-scaffolding]

# Dependency graph
requires: []
provides:
  - Six compilable xUnit test class stubs in Converj.Generator.Tests for collection accumulation feature
  - FluentCollectionMethodAttributeTests (ATTR-01 scaffold)
  - CollectionTypeDetectionTests (ATTR-02/ATTR-03 scaffold)
  - SingularizationTests (NAME-01/NAME-03 scaffold)
  - AccumulatorNameOverrideTests (NAME-02 scaffold)
  - AccumulatorNameCollisionTests (NAME-04 scaffold)
  - BackwardCompatibilitySnapshotTests (BACK-02 scaffold)
affects: [21-02, 21-03, 21-04, 21-05]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "xUnit stub pattern: minimal class with single Placeholder [Fact] for discoverability, no production references"
    - "Omit unused using directives in stubs to satisfy WarningsAsErrors; Plans 02-05 add them back with real tests"

key-files:
  created:
    - src/Converj.Generator.Tests/FluentCollectionMethodAttributeTests.cs
    - src/Converj.Generator.Tests/CollectionTypeDetectionTests.cs
    - src/Converj.Generator.Tests/SingularizationTests.cs
    - src/Converj.Generator.Tests/AccumulatorNameOverrideTests.cs
    - src/Converj.Generator.Tests/AccumulatorNameCollisionTests.cs
    - src/Converj.Generator.Tests/BackwardCompatibilitySnapshotTests.cs
  modified: []

key-decisions:
  - "Omit 'using static FluentDiagnostics' and unused VerifyCS alias from stubs to avoid WarningsAsErrors compiler errors before the referenced types exist"
  - "Each stub has exactly one Placeholder [Fact] to ensure dotnet test --filter returns a discoverable result"

patterns-established:
  - "Stub pattern: VerifyCS alias using only, namespace declaration, public class, one Placeholder [Fact]"

requirements-completed: [ATTR-01, ATTR-02, ATTR-03, NAME-01, NAME-02, NAME-03, NAME-04, BACK-02]

# Metrics
duration: 1min
completed: 2026-04-14
---

# Phase 21 Plan 01: Test Scaffolding Summary

**Six xUnit test class stubs created for collection accumulation feature, establishing dotnet test --filter targets before any Phase 21 production code ships**

## Performance

- **Duration:** 1 min
- **Started:** 2026-04-14T11:29:09Z
- **Completed:** 2026-04-14T11:30:00Z
- **Tasks:** 2
- **Files modified:** 6 (all new)

## Accomplishments
- All six Wave 0 test class stubs created and compiling clean under WarningsAsErrors
- Full test suite delta: +6 passing tests (362 → 368), zero new failures
- All stub classes discoverable via `dotnet test --filter "FullyQualifiedName~{ClassName}"`
- No production code touched; strictly scoped to Tests project

## Task Commits

Each task was committed atomically:

1. **Task 1: Create attribute + detection + naming test class stubs** - `d5a5de5` (test)
2. **Task 2: Create collision + backward-compat test class stubs** - `09439b5` (test)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `src/Converj.Generator.Tests/FluentCollectionMethodAttributeTests.cs` - Stub for ATTR-01 (FluentCollectionMethodAttribute behavior)
- `src/Converj.Generator.Tests/CollectionTypeDetectionTests.cs` - Stub for ATTR-02/ATTR-03 (collection type detection)
- `src/Converj.Generator.Tests/SingularizationTests.cs` - Stub for NAME-01/NAME-03 (singularization logic)
- `src/Converj.Generator.Tests/AccumulatorNameOverrideTests.cs` - Stub for NAME-02 (accumulator name override)
- `src/Converj.Generator.Tests/AccumulatorNameCollisionTests.cs` - Stub for NAME-04 (name collision diagnostics)
- `src/Converj.Generator.Tests/BackwardCompatibilitySnapshotTests.cs` - Stub for BACK-02 (backward compat snapshot)

## Decisions Made
- Omitted `using static Converj.Generator.Diagnostics.FluentDiagnostics;` from all stubs — unused using would fail under `<WarningsAsErrors>true</WarningsAsErrors>`. Plans 02-05 will add it back when real diagnostic tests are written.
- Each stub has one `Placeholder()` [Fact] method rather than being truly empty, ensuring `dotnet test --filter "FullyQualifiedName~{ClassName}"` returns an executed (not skipped) result.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All six test file targets exist; Plans 02-05 can append [Fact] methods to them directly
- 21-VALIDATION.md Wave 0 file-exists checklist items can be marked complete
- Plans 02-05 should replace the Placeholder [Fact] when adding the first real test to each class

---
*Phase: 21-foundation*
*Completed: 2026-04-14*
