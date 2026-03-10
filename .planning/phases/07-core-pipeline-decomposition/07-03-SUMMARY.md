---
phase: 07-core-pipeline-decomposition
plan: 03
subsystem: model
tags: [roslyn, source-generator, refactoring, orchestrator-pattern, decomposition]

# Dependency graph
requires:
  - phase: 07-01
    provides: FluentDiagnostics centralized diagnostic descriptors
provides:
  - FluentMethodSelector -- focused method selection, validation, and merging type
  - FluentStepBuilder -- focused node-to-step conversion and storage resolution type
  - FluentModelFactory simplified to thin orchestrator (~161 lines)
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns: [orchestrator-with-collaborators, callback-delegate-mutual-recursion]

key-files:
  created:
    - src/Motiv.FluentFactory.Generator/Model/FluentMethodSelector.cs
    - src/Motiv.FluentFactory.Generator/Model/FluentStepBuilder.cs
  modified:
    - src/Motiv.FluentFactory.Generator/Model/FluentModelFactory.cs

key-decisions:
  - "Used Func delegates for mutual recursion wiring between orchestrator, selector, and step builder"
  - "FluentMethodSelector receives Compilation, DiagnosticList, UnreachableConstructorAnalyzer via primary constructor"
  - "FluentStepBuilder receives OrderedDictionary<ParameterSequence, RegularFluentStep> via primary constructor"
  - "Collaborators initialized in CreateFluentFactoryCompilationUnit after state clear to share same instances"

patterns-established:
  - "Callback delegate pattern: orchestrator wires mutual recursion via Func<> delegates rather than circular dependencies"
  - "Shared state by reference: mutable collections (DiagnosticList, OrderedDictionary) passed to collaborators via constructor"

requirements-completed: [DECOMP-01, XCUT-01, XCUT-02]

# Metrics
duration: 5min
completed: 2026-03-10
---

# Phase 7 Plan 3: FluentModelFactory Decomposition Summary

**FluentModelFactory (438 lines) decomposed into thin orchestrator (~161 lines) plus FluentMethodSelector (method selection/validation) and FluentStepBuilder (step conversion/storage) collaborator types**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-10T21:53:30Z
- **Completed:** 2026-03-10T21:58:10Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- Extracted FluentMethodSelector with 7 members: ConvertNodeToFluentMethods, ChooseCandidateFluentMethod, CreateFluentMethods, ValidateMultipleFluentMethodCompatibility, MergeConstructorMetadata, NormalizedConverterMethod, SelectedFluentMethod record
- Extracted FluentStepBuilder with 3 members: ConvertNodeToFluentStep, CreateRegularStepValueStorage, GetDescendentFluentSteps
- FluentModelFactory reduced from 438 to 161 lines, retaining only shared state, trie construction, creation methods, and orchestration dispatch
- All 174 tests pass with identical generated output

## Task Commits

Each task was committed atomically:

1. **Task 1: Extract FluentMethodSelector from FluentModelFactory** - `e33b2d6` (feat)
2. **Task 2: Extract FluentStepBuilder and run full test suite** - `2d4ea9d` (feat)

## Files Created/Modified
- `src/Motiv.FluentFactory.Generator/Model/FluentMethodSelector.cs` - Method selection, validation, merging, and the SelectedFluentMethod record (216 lines)
- `src/Motiv.FluentFactory.Generator/Model/FluentStepBuilder.cs` - Node-to-step conversion, storage resolution, descendant traversal (147 lines)
- `src/Motiv.FluentFactory.Generator/Model/FluentModelFactory.cs` - Thin orchestrator with shared state and coordination (161 lines)

## Decisions Made
- Used `Func<>` delegates to wire mutual recursion: orchestrator -> selector (needs step builder) -> step builder (needs orchestrator). This avoids circular constructor dependencies while keeping the recursive data flow intact
- Collaborators (`_methodSelector`, `_stepBuilder`) initialized with `null!` fields and assigned in `CreateFluentFactoryCompilationUnit` after state is cleared, ensuring they share the same diagnostic/step collection instances
- `ConvertNodeToCreationMethods` stays in orchestrator per plan (mutates `_unreachableConstructorAnalyzer`, only ~22 lines)
- `GetDescendentFluentSteps` made `public static` on FluentStepBuilder so orchestrator can call it

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added missing using directive for Generation namespace in FluentStepBuilder**
- **Found during:** Task 2 (FluentStepBuilder extraction)
- **Issue:** `ToParameterFieldName` extension method lives in `Motiv.FluentFactory.Generator.Generation` namespace, not automatically available
- **Fix:** Added `using Motiv.FluentFactory.Generator.Generation;` to FluentStepBuilder.cs
- **Verification:** Build succeeded
- **Committed in:** 2d4ea9d (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Trivial missing using directive. No scope creep.

## Issues Encountered
- Field initializer ordering: Could not initialize `_methodSelector` as a field initializer because C# field initializers cannot reference other instance fields. Resolved by initializing collaborators in the public entry point method after state is cleared.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Phase 7 complete: all three god classes decomposed (FluentFactoryGenerator, ConstructorAnalyzer, FluentModelFactory)
- Ready for subsequent phases (folder restructuring in Phase 10)
- No blockers or concerns

---
*Phase: 07-core-pipeline-decomposition*
*Completed: 2026-03-10*
