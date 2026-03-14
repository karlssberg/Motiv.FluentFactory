---
phase: 15-scope-and-accessibility-diagnostics
plan: 02
subsystem: testing
tags: [roslyn, source-generator, diagnostics, csharp, xunit, accessibility]

# Dependency graph
requires:
  - phase: 15-scope-and-accessibility-diagnostics
    plan: 01
    provides: MFFG0012/MFFG0013 pattern for accessibility diagnostic chaining in FluentConstructorValidator
provides:
  - MFFG0014 InaccessibleParameterType Warning (parameter type less accessible than factory)
  - MFFG0015 AccessibilityMismatch Warning (factory more accessible than target type)
  - ValidateParameterTypeAccessibility method in FluentConstructorValidator
  - ValidateAccessibilityMismatch method in FluentConstructorValidator
  - Tests covering SCOPE-02, SCOPE-04, and SCOPE-05
affects: [any-future-diagnostic-phases]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Warning-level diagnostics do not block generation — generator produces output with accessible flag set as warning"
    - "CompilerDiagnostics.None required for tests where user code or generated code has C# compiler accessibility errors"
    - "Accessibility comparison: (int)typeA.DeclaredAccessibility < (int)typeB.DeclaredAccessibility"
    - "Skip SpecialType != None (built-ins) and NotApplicable (type params) in parameter type accessibility check"
    - "Empirical span discovery: use PLACEHOLDER generated source, run test, read actual diff, update test"

key-files:
  created: []
  modified:
    - src/Motiv.FluentFactory.Generator/Diagnostics/FluentDiagnostics.cs
    - src/Motiv.FluentFactory.Generator/FluentConstructorValidator.cs
    - src/Motiv.FluentFactory.Generator/AnalyzerReleases.Unshipped.md
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorScopeAndAccessibilityTests.cs

key-decisions:
  - "MFFG0014 and MFFG0015 are Warning severity — generator still produces output, unlike Error-severity MFFG0013 which blocks generation"
  - "CompilerDiagnostics.None required in MFFG0014 and MFFG0015 tests because user source code itself produces CS0051/CS0050 compiler errors for accessibility mismatches"
  - "Nested private class (SCOPE-05) correctly triggers MFFG0015 — Roslyn reports Private for nested class DeclaredAccessibility, comparison fires correctly"
  - "MFFG0014 location uses parameter.Locations (parameter identifier span), MFFG0015 uses constructor.Locations (constructor name span)"

patterns-established:
  - "Accessibility enum int comparison: Private(1) < Internal(4) < Public(6) in Microsoft.CodeAnalysis.Accessibility"
  - "Skip parameter types with SpecialType != None (built-ins always public) and NotApplicable (type parameters)"
  - "Warning diagnostics with generated source: tests must include GeneratedSources even when diagnostic fires"

requirements-completed: [SCOPE-02, SCOPE-04, SCOPE-05]

# Metrics
duration: 9min
completed: 2026-03-14
---

# Phase 15 Plan 02: Scope and Accessibility Diagnostics Summary

**MFFG0014 (inaccessible parameter type) and MFFG0015 (accessibility mismatch) Warning diagnostics with validation logic, plus documented nested private class behavior via 4 new passing tests**

## Performance

- **Duration:** 9 min
- **Started:** 2026-03-14T18:58:19Z
- **Completed:** 2026-03-14T19:06:53Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- MFFG0014 fires when a constructor parameter type is less accessible than the factory (e.g., internal param in a public factory)
- MFFG0015 fires when the target type is less accessible than the factory (e.g., public factory wrapping internal class)
- Internal factory wrapping internal target type does NOT trigger MFFG0015 (no false positive)
- Nested private class as factory target correctly triggers MFFG0015 (SCOPE-05 documented)
- 4 new tests all pass, full test suite green (9 pre-existing failures unchanged)

## Task Commits

Each task was committed atomically:

1. **Task 1 RED: failing tests for MFFG0014 and MFFG0015** - `9bdb240` (test)
2. **Task 1 GREEN: MFFG0014 and MFFG0015 implementation** - `f918118` (feat)
3. **Task 2 RED: failing test for nested private class** - `926e4a1` (test)
4. **Task 2 GREEN: nested private class documentation test** - `aba735e` (feat)

## Files Created/Modified
- `src/Motiv.FluentFactory.Generator/Diagnostics/FluentDiagnostics.cs` - Added InaccessibleParameterType (MFFG0014) and AccessibilityMismatch (MFFG0015) descriptors
- `src/Motiv.FluentFactory.Generator/FluentConstructorValidator.cs` - Added ValidateParameterTypeAccessibility and ValidateAccessibilityMismatch methods, chained into GetDiagnostics()
- `src/Motiv.FluentFactory.Generator/AnalyzerReleases.Unshipped.md` - Added MFFG0014 and MFFG0015 rows
- `src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorScopeAndAccessibilityTests.cs` - Added 4 new tests covering SCOPE-02, SCOPE-04, SCOPE-05

## Decisions Made
- MFFG0014 and MFFG0015 are Warning severity — generation proceeds despite the warning (unlike MFFG0013 Error which blocks generation). This is intentional: the generated code is technically valid but exposes inaccessible types to consumers.
- CompilerDiagnostics.None used in both warning tests because the user source code itself produces C# compiler errors (CS0051/CS0050) for accessibility violations in the input code.
- Nested private class (SCOPE-05) triggers MFFG0015 via the existing accessibility comparison since Roslyn reports `Private` for nested class `DeclaredAccessibility`. No special handling needed.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] RS1032 diagnostic message format error**
- **Found during:** Task 1 (GREEN phase, first build)
- **Issue:** MFFG0014 and MFFG0015 messageFormat strings contained a period mid-sentence (two sentences separated by a period), violating RS1032 rule requiring either single-sentence without trailing period or multi-sentence with trailing period
- **Fix:** Rewrote both messageFormat strings as single sentences using "and" conjunction instead of period
- **Files modified:** src/Motiv.FluentFactory.Generator/Diagnostics/FluentDiagnostics.cs
- **Verification:** Build passed after fix
- **Committed in:** f918118 (Task 1 GREEN commit)

**2. [Rule 1 - Bug] Warning-level diagnostics do not suppress code generation**
- **Found during:** Task 1 (test execution, discovering generated source was produced)
- **Issue:** Plan implied tests would only need ExpectedDiagnostics without GeneratedSources (like MFFG0012 which filters constructors). But MFFG0014/MFFG0015 are Warnings that don't block generation — generator still produces output, requiring tests to include GeneratedSources
- **Fix:** Added correct GeneratedSources to both MFFG0014 and MFFG0015 tests using empirical span discovery pattern; added CompilerDiagnostics.None to suppress C# compiler errors from accessibility violations in test input code
- **Files modified:** src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorScopeAndAccessibilityTests.cs
- **Verification:** All 3 new tests pass
- **Committed in:** f918118 (Task 1 GREEN commit)

---

**Total deviations:** 2 auto-fixed (1 Rule 1 bug fix, 1 Rule 1 test expectation correction)
**Impact on plan:** Both auto-fixes necessary for correctness. No scope creep.

## Issues Encountered
- RS1032 build error on both new diagnostic descriptors (period in single-sentence messageFormat). Fixed by using "and" conjunction to keep messages as single sentences.
- MFFG0014/MFFG0015 tests required CompilerDiagnostics.None because the test input C# code itself has accessibility violations (using an internal type in a public constructor is a CS0051 error).

## Next Phase Readiness
- SCOPE-02, SCOPE-04, SCOPE-05 complete — all scope and accessibility diagnostics implemented
- Phase 15 complete (SCOPE-01 through SCOPE-05 all covered)
- No blockers

---
*Phase: 15-scope-and-accessibility-diagnostics*
*Completed: 2026-03-14*

## Self-Check: PASSED

- FOUND: test file (FluentFactoryGeneratorScopeAndAccessibilityTests.cs)
- FOUND: MFFG0014 in FluentDiagnostics.cs
- FOUND: MFFG0015 in FluentDiagnostics.cs
- FOUND: ValidateParameterTypeAccessibility in FluentConstructorValidator.cs
- FOUND: ValidateAccessibilityMismatch in FluentConstructorValidator.cs
- FOUND: commit 9bdb240 (Task 1 RED)
- FOUND: commit f918118 (Task 1 GREEN)
- FOUND: commit 926e4a1 (Task 2 RED)
- FOUND: commit aba735e (Task 2 GREEN)
- FOUND: 15-02-SUMMARY.md
