---
phase: 15-scope-and-accessibility-diagnostics
plan: 01
subsystem: testing
tags: [roslyn, source-generator, diagnostics, csharp, xunit]

# Dependency graph
requires:
  - phase: 11-type-system-edge-cases
    provides: MFFG0011 unsupported parameter modifier pattern for filtering constructors
provides:
  - MFFG0012 inaccessible constructor diagnostic (private/protected constructors emitting Warning)
  - MFFG0013 missing partial modifier diagnostic (factory root type without partial emitting Error)
  - FilterInaccessibleConstructors method in FluentModelFactory filtering bad constructors before generation
  - ValidateMissingPartialModifier method in FluentConstructorValidator chained into GetDiagnostics()
affects: [16-any-future-diagnostic-phases]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Filter-then-diagnose: inaccessible constructors filtered in FluentModelFactory (same pattern as MFFG0011), not in FluentConstructorValidator"
    - "Validate-then-error-shortcircuit: MFFG0013 Error in GetDiagnostics() prevents generation via FluentModelFactory line 45-48 check"
    - "Empirical span discovery: write test with approximate spans, run, read actual spans from failure output, update test"

key-files:
  created:
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorScopeAndAccessibilityTests.cs
  modified:
    - src/Motiv.FluentFactory.Generator/Diagnostics/FluentDiagnostics.cs
    - src/Motiv.FluentFactory.Generator/FluentModelFactory.cs
    - src/Motiv.FluentFactory.Generator/FluentConstructorValidator.cs
    - src/Motiv.FluentFactory.Generator/AnalyzerReleases.Unshipped.md

key-decisions:
  - "MFFG0012 implemented via FilterInaccessibleConstructors in FluentModelFactory (same pattern as MFFG0011) rather than in FluentConstructorValidator — ensures constructors are filtered out before generation, matching 'no generated source output' requirement"
  - "MFFG0013 implemented via ValidateMissingPartialModifier in FluentConstructorValidator chained into GetDiagnostics() — Error severity triggers existing short-circuit at FluentModelFactory line 45-48, preventing generation"
  - "CompilerDiagnostics.None used in MFFG0013 test to suppress C# compiler errors that would otherwise occur when generator tries to add partial class to non-partial type"

patterns-established:
  - "Accessibility check uses constructor.DeclaredAccessibility (IMethodSymbol), not root type accessibility"
  - "Partial modifier check uses rootType.DeclaringSyntaxReferences -> TypeDeclarationSyntax -> Modifiers.Any(SyntaxKind.PartialKeyword)"

requirements-completed: [SCOPE-01, SCOPE-03]

# Metrics
duration: 5min
completed: 2026-03-14
---

# Phase 15 Plan 01: Scope and Accessibility Diagnostics Summary

**MFFG0012 (inaccessible constructor Warning) and MFFG0013 (missing partial modifier Error) diagnostics implemented with TDD, filtering bad constructors before generation**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-14T18:50:07Z
- **Completed:** 2026-03-14T18:55:27Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- MFFG0012 fires for private and protected constructors with [FluentConstructor], skipping generation for those constructors
- MFFG0013 fires when factory root type lacks `partial` modifier, preventing generation entirely via Error short-circuit
- 3 new tests all pass covering both SCOPE-01 and SCOPE-03 requirements
- Zero regressions (9 pre-existing test failures unchanged)

## Task Commits

Each task was committed atomically:

1. **Task 1: TDD — MFFG0012 inaccessible constructor diagnostic (SCOPE-01)** - `0ee2bd5` (feat)
2. **Task 2: TDD — MFFG0013 missing partial modifier diagnostic (SCOPE-03)** - `a990b4b` (feat)

_Note: TDD tasks completed in combined test+implementation commits due to immediate test success after implementation_

## Files Created/Modified
- `src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorScopeAndAccessibilityTests.cs` - New test file with 3 tests for SCOPE-01 and SCOPE-03
- `src/Motiv.FluentFactory.Generator/Diagnostics/FluentDiagnostics.cs` - Added InaccessibleConstructor (MFFG0012) and MissingPartialModifier (MFFG0013) descriptors
- `src/Motiv.FluentFactory.Generator/FluentModelFactory.cs` - Added FilterInaccessibleConstructors method filtering private/protected constructors before processing
- `src/Motiv.FluentFactory.Generator/FluentConstructorValidator.cs` - Added ValidateMissingPartialModifier and HasPartialModifier methods, chained into GetDiagnostics()
- `src/Motiv.FluentFactory.Generator/AnalyzerReleases.Unshipped.md` - Added MFFG0011, MFFG0012, MFFG0013 rows

## Decisions Made
- MFFG0012 filtering implemented in FluentModelFactory (same pattern as MFFG0011) rather than FluentConstructorValidator — ensures constructors are filtered and skipped before generation
- MFFG0013 validation in FluentConstructorValidator chained into GetDiagnostics() — Error severity uses existing short-circuit to prevent generation
- `CompilerDiagnostics.None` used in MFFG0013 test to suppress C# compiler errors when generator interacts with non-partial type

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] MFFG0012 implemented in FluentModelFactory instead of FluentConstructorValidator**
- **Found during:** Task 1 (MFFG0012 implementation)
- **Issue:** Plan said to chain ValidateConstructorAccessibility into GetDiagnostics() via Concat in FluentConstructorValidator. But GetDiagnostics() is called AFTER the empty-check short-circuit in FluentModelFactory, and a Warning doesn't trigger the error short-circuit at line 45-48, so generation would still proceed with inaccessible constructors.
- **Fix:** Implemented FilterInaccessibleConstructors in FluentModelFactory (same pattern as FilterUnsupportedParameterModifierConstructors for MFFG0011), which filters before generation and returns diagnostics to the caller
- **Files modified:** src/Motiv.FluentFactory.Generator/FluentModelFactory.cs
- **Verification:** Tests pass with no generated output when MFFG0012 fires
- **Committed in:** 0ee2bd5 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - bug/correctness)
**Impact on plan:** Auto-fix necessary for correct behavior. Without it, generator would produce broken code calling private/protected constructors.

## Issues Encountered
- RS1032 build error: single-sentence diagnostic message format cannot end with period. Fixed by removing trailing period from MissingPartialModifier messageFormat.

## Next Phase Readiness
- SCOPE-01 and SCOPE-03 complete, ready for remaining scope/accessibility diagnostics (SCOPE-02, SCOPE-04)
- No blockers

---
*Phase: 15-scope-and-accessibility-diagnostics*
*Completed: 2026-03-14*

## Self-Check: PASSED

- FOUND: test file (FluentFactoryGeneratorScopeAndAccessibilityTests.cs)
- FOUND: MFFG0012 in FluentDiagnostics.cs
- FOUND: MFFG0013 in FluentDiagnostics.cs
- FOUND: FilterInaccessibleConstructors in FluentModelFactory.cs
- FOUND: ValidateMissingPartialModifier in FluentConstructorValidator.cs
- FOUND: commit 0ee2bd5 (Task 1)
- FOUND: commit a990b4b (Task 2)
- FOUND: 15-01-SUMMARY.md
