---
phase: 07-core-pipeline-decomposition
plan: 01
subsystem: generator
tags: [source-generator, roslyn, refactoring, diagnostics]

# Dependency graph
requires:
  - phase: 06-global-qualification
    provides: Fully qualified type output with global:: prefix
provides:
  - FluentDiagnostics static class with all 10 diagnostic descriptors
  - FluentConstructorContextFactory with context creation and de-duplication
  - Simplified FluentFactoryGenerator (~97 lines, pipeline-only)
affects: [07-02, 07-03, 08-generation-layer]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Diagnostic descriptors centralized in a single static class"
    - "Pipeline helper methods extracted to static factory classes"

key-files:
  created:
    - src/Motiv.FluentFactory.Generator/Diagnostics/FluentDiagnostics.cs
    - src/Motiv.FluentFactory.Generator/Analysis/FluentConstructorContextFactory.cs
  modified:
    - src/Motiv.FluentFactory.Generator/FluentFactoryGenerator.cs
    - src/Motiv.FluentFactory.Generator/Diagnostics/UnreachableConstructorAnalyzer.cs
    - src/Motiv.FluentFactory.Generator/Diagnostics/IgnoredMultiMethodWarningFactory.cs
    - src/Motiv.FluentFactory.Generator/Model/SymbolExtensions.cs
    - src/Motiv.FluentFactory.Generator/Model/FluentConstructorValidator.cs
    - src/Motiv.FluentFactory.Generator/Model/FluentModelFactory.cs

key-decisions:
  - "Made FluentDiagnostics public static to match original descriptor visibility"
  - "Made FluentConstructorContextFactory internal static since methods were previously private"
  - "Made GetFluentFactoryMetadata and ConvertToFluentFactoryGeneratorOptions public for testability"

patterns-established:
  - "Diagnostic descriptors in FluentDiagnostics: all MFFG diagnostic descriptors centralized"
  - "Pipeline helpers in Analysis namespace factory classes"

requirements-completed: [DECOMP-02, XCUT-01, XCUT-02]

# Metrics
duration: 4min
completed: 2026-03-10
---

# Phase 7 Plan 1: Core Pipeline Decomposition Summary

**Extracted FluentDiagnostics (10 descriptors) and FluentConstructorContextFactory (5 methods) from FluentFactoryGenerator, reducing it from 376 to 97 lines**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-10T21:45:31Z
- **Completed:** 2026-03-10T21:49:54Z
- **Tasks:** 2
- **Files modified:** 12

## Accomplishments
- Created FluentDiagnostics.cs with all 10 DiagnosticDescriptor fields centralized in one static class
- Created FluentConstructorContextFactory.cs with context creation, metadata extraction, and de-duplication logic
- Simplified FluentFactoryGenerator.cs to a clean 97-line IIncrementalGenerator entry point with only Initialize() and Execute()
- Updated all 10 diagnostic references across 5 source files and 5 test files
- All 174 existing tests pass with identical generated output

## Task Commits

Each task was committed atomically:

1. **Task 1a: Create FluentDiagnostics and FluentConstructorContextFactory** - `286be64` (refactor)
2. **Task 1b: Simplify FluentFactoryGenerator** - `84ebf9d` (refactor)
3. **Task 2a: Update source diagnostic references** - `0288400` (refactor)
4. **Task 2b: Update test diagnostic references** - `3634e43` (refactor)

## Files Created/Modified
- `src/Motiv.FluentFactory.Generator/Diagnostics/FluentDiagnostics.cs` - Public static class with all 10 DiagnosticDescriptor fields (MFFG0001-MFFG0010)
- `src/Motiv.FluentFactory.Generator/Analysis/FluentConstructorContextFactory.cs` - Internal static class with CreateConstructorContexts, GetFluentFactoryMetadata, ConvertToFluentFactoryGeneratorOptions, DeDuplicateFluentConstructors, ChooseOverridingConstructors
- `src/Motiv.FluentFactory.Generator/FluentFactoryGenerator.cs` - Simplified to pipeline orchestration only (Initialize + Execute)
- `src/Motiv.FluentFactory.Generator/Diagnostics/UnreachableConstructorAnalyzer.cs` - Updated to FluentDiagnostics reference
- `src/Motiv.FluentFactory.Generator/Diagnostics/IgnoredMultiMethodWarningFactory.cs` - Updated 2 FluentDiagnostics references
- `src/Motiv.FluentFactory.Generator/Model/SymbolExtensions.cs` - Updated 2 FluentDiagnostics references
- `src/Motiv.FluentFactory.Generator/Model/FluentConstructorValidator.cs` - Updated 4 FluentDiagnostics references
- `src/Motiv.FluentFactory.Generator/Model/FluentModelFactory.cs` - Updated 1 FluentDiagnostics reference

## Decisions Made
- Made FluentDiagnostics public static to match original descriptor visibility on FluentFactoryGenerator
- Made FluentConstructorContextFactory internal static since the extracted methods were previously private
- Made GetFluentFactoryMetadata and ConvertToFluentFactoryGeneratorOptions public for potential future testability

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Updated test file diagnostic references**
- **Found during:** Task 2 (test suite execution)
- **Issue:** 5 test files used `using static Motiv.FluentFactory.Generator.FluentFactoryGenerator` to import diagnostic descriptors
- **Fix:** Updated all 5 test files to `using static Motiv.FluentFactory.Generator.Diagnostics.FluentDiagnostics`
- **Files modified:** FluentFactoryDiagnosticsTests.cs, FluentFactoryGeneratorMergeDissimilarStepsTests.cs, FluentFactoryMethodCustomizationTests.cs, FluentFactoryMultipleMethodsGenerationTests.cs, FluentFactoryGeneratorMergeTests.cs
- **Verification:** All 174 tests pass
- **Committed in:** 3634e43

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Essential fix for test compilation. No scope creep.

## Issues Encountered
None beyond the test file references documented as a deviation.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- FluentFactoryGenerator is now a clean pipeline orchestrator ready for further decomposition
- FluentDiagnostics provides centralized diagnostic access for all consumers
- FluentConstructorContextFactory encapsulates context creation logic for potential future unit testing

---
*Phase: 07-core-pipeline-decomposition*
*Completed: 2026-03-10*
