---
phase: 13-internal-correctness
plan: 01
subsystem: testing
tags: [roslyn, source-generator, edge-cases, namespace-disambiguation, hash-code-contract, trie]

# Dependency graph
requires:
  - phase: 12-constructor-variation-edge-cases
    provides: edge case test infrastructure and CRLF-to-LF conversion pattern
provides:
  - Namespace disambiguation edge case tests (COMP-01): same-named types from different namespaces
  - Hash code contract edge case tests (COMP-03): hash collision and truly-equal parameter merge scenarios
affects: [14-diagnostic-edge-cases, 15-scope-boundary-tests]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Tests assert DESIRED correct output; failing tests document discovered shortcomings"
    - "CRLF-to-LF conversion required after Write tool creates test files on Windows"

key-files:
  created:
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorNamespaceDisambiguationTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorHashCodeContractTests.cs
  modified: []

key-decisions:
  - "Generator correctly distinguishes same-named types from different namespaces in both multi-constructor and single-constructor scenarios (COMP-01 passes)"
  - "Generator loses TargetA when two constructors share identical single-parameter signatures (same type + same FluentMethod name) -- only the last-registered target survives in the Trie merge (COMP-03 Test 2 fails, documenting shortcoming)"
  - "Hash collision path (same type hash, different FluentMethod names) works correctly -- both SetValue and WithText are produced as separate overloads (COMP-03 Test 1 passes)"

patterns-established:
  - "Namespace disambiguation: FluentType.ToDisplayString() (full namespace) correctly distinguishes same-named types; tests confirm this works"
  - "Hash code contract: FluentMethodParameter.GetHashCode() delegates to FluentType.GetHashCode() only (ignores Names) creating valid hash collisions; Equals uses Names.Overlaps for correct disambiguation"

requirements-completed: [COMP-01, COMP-03]

# Metrics
duration: 15min
completed: 2026-03-14
---

# Phase 13 Plan 01: Internal Correctness Summary

**Namespace disambiguation edge case tests (COMP-01) pass; hash collision tests (COMP-03) reveal generator silently drops TargetA when two constructors share identical single-param signatures**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-03-14T16:43:00Z
- **Completed:** 2026-03-14T16:58:31Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Created 2 namespace disambiguation tests (COMP-01) -- both pass, confirming `FluentType.ToDisplayString()` correctly distinguishes `NamespaceA.Config` vs `NamespaceB.Config`
- Created 2 hash code contract tests (COMP-03) -- Test 1 (hash collision, different method names) passes; Test 2 (truly equal parameters) fails, documenting a generator shortcoming where `TargetA` is silently overwritten by `TargetB` in the Trie when both constructors have identical single-parameter signatures
- 3 of 4 new tests pass; 1 failing test correctly documents desired vs actual behavior

## Task Commits

Each task was committed atomically:

1. **Task 1: Create namespace disambiguation test file (COMP-01)** - `8dd4b21` (test)
2. **Task 2: Create hash code contract test file (COMP-03)** - `4d0f2a5` (test)

## Files Created/Modified

- `src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorNamespaceDisambiguationTests.cs` - Two tests for same-named types from different namespaces (both pass)
- `src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorHashCodeContractTests.cs` - Two tests for hash code contract consistency (1 pass, 1 fail documenting shortcoming)

## Decisions Made

- Namespace disambiguation tests use `[FluentConstructor(typeof(Factory), Options = FluentOptions.NoCreateMethod)]` pattern, matching existing merge test conventions
- COMP-03 Test 2 expected output modeled a step-struct with two `Create*` methods (desired behavior for merged single-param constructors with identical signatures) -- actual generator output only emits one method returning TargetB, confirming the shortcoming

## Deviations from Plan

None - plan executed exactly as written. Failing tests are the intended outcome per plan specification: "Tests assert DESIRED output. Failing tests indicate discovered shortcomings."

## Issues Encountered

- COMP-03 Test 2 revealed a generator shortcoming: when two constructors share identical single-parameter signatures (same type + same FluentMethod name), only the last-registered target survives. The `WithValue` factory method returns `global::Test.TargetB` and constructs `new global::Test.TargetB(value)` -- TargetA is silently discarded.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- 2 new test files establish patterns for namespace and parameter equality edge cases
- COMP-03 Test 2 failure documents a real generator bug for future fix planning
- Existing 11 Phase 11 failures remain unchanged (pre-existing shortcomings from nullable/generic edge cases)
- Total test suite: 194 passing, 12 failing (11 pre-existing + 1 new from COMP-03)

---
*Phase: 13-internal-correctness*
*Completed: 2026-03-14*
