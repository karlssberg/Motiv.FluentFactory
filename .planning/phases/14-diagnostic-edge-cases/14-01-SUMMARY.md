---
phase: 14-diagnostic-edge-cases
plan: "01"
subsystem: testing
tags: [roslyn, source-generator, diagnostics, attributes, generic-constraints]

# Dependency graph
requires:
  - phase: 13-internal-correctness
    provides: Trie key collision and namespace disambiguation edge case tests
provides:
  - Malformed attribute usage edge case tests (DIAG-01)
  - Invalid generic constraint edge case tests (DIAG-02)
  - Validated struct constraint propagation to generated code
affects: [14-diagnostic-edge-cases, future-generator-robustness]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "CompilerDiagnostics.None pattern for tests where C# compiler diagnostics are unpredictable"
    - "Tests asserting DESIRED behavior document shortcomings when they fail"

key-files:
  created:
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorMalformedAttributeTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorInvalidGenericConstraintTests.cs
  modified: []

key-decisions:
  - "All 3 malformed attribute tests pass — generator correctly reports MFFG0010, simultaneous MFFG0009+MFFG0007, and cascading MFFG0008+MFFG0010"
  - "Generator correctly propagates where T : struct constraint to generated factory methods and step structs"
  - "Generator processes types with undefined constraint interfaces instead of skipping (shortcoming documented)"

patterns-established:
  - "Diagnostic span assertions require exact column offsets matching Roslyn attribute location reporting"

requirements-completed: [DIAG-01, DIAG-02]

# Metrics
duration: 4min
completed: 2026-03-14
---

# Phase 14 Plan 01: Malformed Attribute & Invalid Generic Constraint Tests (DIAG-01, DIAG-02) Summary

**Five edge case tests covering malformed attribute combinations and generic constraint handling — all 3 attribute tests pass, struct constraint propagation verified, 1 shortcoming documented for error type constraints**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-14T17:31:28Z
- **Completed:** 2026-03-14T17:35:30Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Created FluentFactoryGeneratorMalformedAttributeTests.cs with 3 tests covering DIAG-01: conflicting options on primary constructor record, simultaneous MFFG0009+MFFG0007 diagnostics, cascading MFFG0008+MFFG0010 errors
- Created FluentFactoryGeneratorInvalidGenericConstraintTests.cs with 2 tests covering DIAG-02: struct constraint propagation (passes), undefined type constraint resilience (documents shortcoming)
- All 3 malformed attribute tests pass — validates generator correctly reports multiple independent diagnostic errors
- Struct constraint test confirms generator propagates `where T : struct` to factory methods and step structs

## Task Commits

Each task was committed atomically:

1. **Task 1: Create malformed attribute usage test file (DIAG-01)** - `9001efd` (test)
2. **Task 2: Create invalid generic constraint test file (DIAG-02)** - `98697cd` (test)

## Files Created/Modified
- `src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorMalformedAttributeTests.cs` - 3 tests for conflicting attribute arguments, simultaneous and cascading diagnostic validation
- `src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorInvalidGenericConstraintTests.cs` - 2 tests for generic constraint propagation and undefined type constraint handling

## Decisions Made
- Test 1 (MFFG0010 on record) uses `public partial class Factory` instead of `public static partial class Factory` to match the agent's implementation which targets a non-static factory
- Test 2 (simultaneous diagnostics) validates both MFFG0009 and MFFG0007 fire independently — test passes, confirming no short-circuit in validation
- Struct constraint test expected output corrected to include step struct (single-param constructors still generate step structs)

## Deviations from Plan

Struct constraint test initially had incorrect expected output (missing step struct). Corrected to match actual generator behavior, which is correct.

## Issues Encountered

None significant. The struct constraint test required expected output adjustment because the plan assumed a direct-return pattern for single-parameter constructors, but the generator always uses step structs.

## Next Phase Readiness
- DIAG-01 and DIAG-02 requirements covered with 5 tests total
- 1 shortcoming documented: generator processes types with undefined constraint interfaces
- Ready for phase completion

---
*Phase: 14-diagnostic-edge-cases*
*Completed: 2026-03-14*
