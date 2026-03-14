---
phase: 14-diagnostic-edge-cases
plan: "02"
subsystem: testing
tags: [roslyn, source-generator, diagnostics, resilience, compilation-errors]

# Dependency graph
requires:
  - phase: 13-internal-correctness
    provides: Trie key collision and namespace disambiguation edge case tests
provides:
  - Compilation error resilience edge case tests (DIAG-03)
  - Documented generator behavior when encountering IErrorTypeSymbol parameters
affects: [14-diagnostic-edge-cases, future-generator-robustness]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "CompilerDiagnostics.None pattern for tests where exact C# compiler diagnostics are hard to predict"
    - "Tests asserting DESIRED behavior document shortcomings when they fail (milestone success philosophy)"

key-files:
  created:
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorCompilationErrorResilienceTests.cs
  modified: []

key-decisions:
  - "Generator generates output even when constructor parameters have IErrorTypeSymbol types (shortcoming documented by Tests 1 and 3)"
  - "Generator correctly handles syntax errors in factory declarations (Test 2 passes)"
  - "CompilerDiagnostics.None used to suppress C# compiler error verification when exact spans are hard to predict"

patterns-established:
  - "Resilience tests use CompilerDiagnostics.None to focus on generator behavior, not C# compiler output"

requirements-completed: [DIAG-03]

# Metrics
duration: 2min
completed: 2026-03-14
---

# Phase 14 Plan 02: Compilation Error Resilience Tests (DIAG-03) Summary

**Three resilience tests documenting generator behavior when user code has C# compilation errors, revealing that the generator processes constructors with IErrorTypeSymbol parameter types instead of skipping them**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-14T17:31:28Z
- **Completed:** 2026-03-14T17:33:55Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Created FluentFactoryGeneratorCompilationErrorResilienceTests.cs with 3 test methods covering DIAG-03
- Documented that the generator handles syntax errors in factory declarations correctly (Test 2 passes)
- Documented shortcoming: generator generates output for constructors with undefined parameter types instead of skipping (Tests 1 and 3 fail, no unhandled exception thrown)
- All tests compile; test failures document desired vs actual behavior per milestone philosophy

## Task Commits

Each task was committed atomically:

1. **Task 1: Create compilation error resilience test file (DIAG-03)** - `82ef4fe` (test)

**Plan metadata:** (included in docs commit)

## Files Created/Modified
- `src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorCompilationErrorResilienceTests.cs` - 3 resilience tests for DIAG-03: nonexistent parameter type, syntax error in factory, mixed valid/invalid parameter types

## Decisions Made
- Used `CompilerDiagnostics.None` on all three tests to suppress C# compiler error verification and focus purely on whether the generator throws an unhandled exception
- Tests assert DESIRED behavior (no generated output) but Tests 1 and 3 fail because the generator actually generates output for constructors with IErrorTypeSymbol types — this documents a shortcoming
- Test 2 (syntax error in factory) passes because the parser cannot find [FluentConstructor] in malformed source, so the generator correctly produces no output

## Deviations from Plan

None - plan executed exactly as written. Tests correctly document current generator behavior, including the discovered shortcomings for Tests 1 and 3.

## Issues Encountered

Discovered behavior: The Roslyn generator's `ForAttributeWithMetadataName` pipeline still finds `[FluentConstructor]` when parameter types are error types (e.g., `NonExistentType`). The generator then processes the constructor and generates code using the error type's display string. This means Tests 1 and 3 fail with "unexpected generated file" rather than passing (which would indicate the generator skips broken constructors). No unhandled exception is thrown — the generator is resilient to crashes, but its output includes error types.

## Next Phase Readiness
- DIAG-03 requirement covered with 3 resilience tests
- Shortcomings documented: generator does not skip constructors with IErrorTypeSymbol parameter types
- Future work: generator could check for IErrorTypeSymbol and skip affected constructors

---
*Phase: 14-diagnostic-edge-cases*
*Completed: 2026-03-14*
