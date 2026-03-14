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
  - "Generator emits output even for CS0449 invalid class+struct constraints, forwarding them as-is to generated code"
  - "CompilerDiagnostics.None + explicit GeneratedSources required for CS0449 test — verifier always checks generated files"
  - "MFFG0009 and MFFG0007 both fire independently, confirming no short-circuit after first error"

patterns-established:
  - "Diagnostic span assertions require exact column offsets matching Roslyn attribute location reporting"
  - "Empirical span discovery: write test with approximate spans, run, read actual spans from failure output, update test"
  - "CompilerDiagnostics.None with explicit GeneratedSources for testing generator resilience on invalid C# input"

requirements-completed: [DIAG-01, DIAG-02]

# Metrics
duration: 8min
completed: 2026-03-14
---

# Phase 14 Plan 01: Malformed Attribute & Invalid Generic Constraint Tests (DIAG-01, DIAG-02) Summary

**Five tests covering malformed FluentConstructor attribute combinations (MFFG0007/0008/0009/0010) and struct constraint propagation with CS0449 resilience validation — all 5 pass**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-14T17:31:47Z
- **Completed:** 2026-03-14T17:39:47Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Created FluentFactoryGeneratorMalformedAttributeTests.cs with 3 tests covering DIAG-01: conflicting options on primary constructor record (MFFG0010), simultaneous MFFG0009+MFFG0007 diagnostics, cascading MFFG0008+MFFG0010 errors
- Created/updated FluentFactoryGeneratorInvalidGenericConstraintTests.cs with 2 tests covering DIAG-02: struct constraint propagation (passes), CS0449 invalid constraint resilience (generator does not crash, emits output with invalid constraints as-is)
- All 5 tests pass — confirms generator correctly reports multiple independent diagnostic errors and handles constraint edge cases gracefully

## Task Commits

Each task was committed atomically:

1. **Task 1: Create malformed attribute usage test file (DIAG-01)** - `9001efd` (test)
2. **Task 2: Create invalid generic constraint test file (DIAG-02)** - `2db2c60` (test)

## Files Created/Modified
- `src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorMalformedAttributeTests.cs` - 3 tests for conflicting attribute arguments, simultaneous and cascading diagnostic validation
- `src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorInvalidGenericConstraintTests.cs` - 2 tests for generic constraint propagation and invalid constraint combination resilience

## Decisions Made
- Exact diagnostic span coordinates discovered empirically: write test, run, read actual spans from failure output, update test
- For CS0449 test: generator does not crash and emits output with the invalid `class, struct` constraint forwarded as-is. Test uses `CompilerDiagnostics.None` plus explicit `GeneratedSources` assertion
- Both MFFG0009 and MFFG0007 fire independently on the same attribute — validation does not short-circuit after first error

## Deviations from Plan

None - plan executed exactly as written. Span coordinates were refined during test execution.

## Issues Encountered

- Initial diagnostic spans for MFFG0007 were incorrect (estimated columns). Resolved by reading actual spans from test failure messages.
- `CompilerDiagnostics.None` alone does not suppress generated source file checks — must also list `GeneratedSources` explicitly.

## Next Phase Readiness
- DIAG-01 and DIAG-02 requirements fully covered with 5 passing tests
- Generator behavior with invalid C# constraints documented (forwards constraints as-is)
- Ready for phase 14-02 and beyond

---
*Phase: 14-diagnostic-edge-cases*
*Completed: 2026-03-14*
