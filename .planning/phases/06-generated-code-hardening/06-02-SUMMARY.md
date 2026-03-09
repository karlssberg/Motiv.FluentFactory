---
phase: 06-generated-code-hardening
plan: 02
subsystem: testing
tags: [roslyn, source-generator, global-qualification, generated-code-attribute, test-verification]

# Dependency graph
requires:
  - phase: 06-01
    provides: "Global:: qualification, GeneratedCode attribute, auto-generated header in generator output"
provides:
  - "All 15 existing test files updated with correct expected output for new generator format"
  - "NamespaceConflictTests proving global:: prevents compilation errors with conflicting type names"
  - "GeneratedCodeAttributeTests verifying attribute presence and version string"
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Test expected output format: no usings, global:: types, GeneratedCode attribute, auto-generated footer"
    - "Test files must use LF line endings to match generator output (NormalizeWhitespace eol=LF)"

key-files:
  created:
    - src/Motiv.FluentFactory.Generator.Tests/NamespaceConflictTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/GeneratedCodeAttributeTests.cs
  modified:
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorNonGenericTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorGenericTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorNestedGenericTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorMergeTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorMergeDissimilarStepsTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorTargetTypeTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorPrimaryConstructorTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorBugDiscoveryTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryMethodCustomizationTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryMultipleMethodsGenerationTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryRootTypeTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryXmlDocumentationTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryXmlHeaderTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryDiagnosticsTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/NamespaceTests.cs
    - src/Motiv.FluentFactory.Generator/FluentFactoryGenerator.cs
    - src/Motiv.FluentFactory.Generator/Generation/SyntaxElements/CompilationUnit.cs

key-decisions:
  - "Fixed generator line ending mismatch: NormalizeWhitespace(eol: LF) and CompilationUnit LineFeed trivia to ensure consistent LF output regardless of platform"
  - "Used iterative diff-parsing approach to bulk-update 112 expected strings across 15 test files"
  - "Documented pre-existing MergeDissimilarStepsTests CS0246 bug as deferred item (was failing before Plan 01)"

patterns-established:
  - "New test files must use LF line endings to match generator output"
  - "Raw string literal expected outputs follow format: namespace block, auto-generated footer, one trailing blank line"

requirements-completed: [QUAL-01, QUAL-02]

# Metrics
duration: 60min
completed: 2026-03-09
---

# Phase 06 Plan 02: Test Expected Output Updates Summary

**Bulk-updated 112 expected strings across 15 test files for global:: qualified output, plus 4 new validation tests for namespace conflicts and GeneratedCode attribute**

## Performance

- **Duration:** ~60 min
- **Completed:** 2026-03-09T22:22:00Z
- **Tasks:** 2
- **Files modified:** 19 (17 modified, 2 created)

## Accomplishments
- Updated all 15 existing test files (112 expected strings) to match new generator output format
- Fixed generator line ending mismatch (CRLF vs LF) that was causing all expected string comparisons to fail
- Created NamespaceConflictTests proving global:: prevents compilation errors with shadowed types (String, List)
- Created GeneratedCodeAttributeTests verifying attribute on root type and step struct, plus version string validation
- Achieved 173/174 tests passing (1 pre-existing failure unrelated to changes)

## Task Commits

Each task was committed atomically:

1. **Task 1: Bulk-update all existing test expected outputs** - `e10f3ef` (fix)
2. **Task 2: Add namespace conflict and GeneratedCode attribute tests** - `299fe25` (test)

## Files Created/Modified
- `src/Motiv.FluentFactory.Generator/FluentFactoryGenerator.cs` - Fixed NormalizeWhitespace to use LF line endings
- `src/Motiv.FluentFactory.Generator/Generation/SyntaxElements/CompilationUnit.cs` - Fixed leading trivia to use LineFeed
- `src/Motiv.FluentFactory.Generator.Tests/NamespaceConflictTests.cs` - New: validates global:: prevents shadowed type conflicts
- `src/Motiv.FluentFactory.Generator.Tests/GeneratedCodeAttributeTests.cs` - New: validates GeneratedCode attribute and version
- 15 existing test files - Updated expected output strings for global:: qualification format

## Decisions Made
- **Line ending fix:** Generator used `NormalizeWhitespace()` defaulting to CRLF, but test files have LF (git autocrlf=input). Fixed by adding `eol: "\n"` parameter and changing `CarriageReturnLineFeed` to `LineFeed` in CompilationUnit.cs
- **Bulk update strategy:** Built a Node.js script (update-expected.mjs) to parse test diff output and automatically reconstruct actual generator output from the Error Message sections, then replace expected strings in test files iteratively
- **Pre-existing bug:** Documented MergeDissimilarStepsTests CS0246 failure as deferred item (verified it fails against the original generator too)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed generator CRLF/LF line ending mismatch**
- **Found during:** Task 1 (initial test run showed all 112 tests failing with CR/LF markers)
- **Issue:** `NormalizeWhitespace()` defaults to CRLF output, but test files use LF (git autocrlf=input). Also `CompilationUnit.cs` used `CarriageReturnLineFeed` for trivia
- **Fix:** Added `eol: "\n"` to `NormalizeWhitespace()` call in FluentFactoryGenerator.cs; changed `CarriageReturnLineFeed` to `LineFeed` in CompilationUnit.cs
- **Files modified:** FluentFactoryGenerator.cs, CompilationUnit.cs
- **Verification:** All 112 previously-failing tests pass after fix + expected string updates
- **Committed in:** e10f3ef (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Line ending fix was necessary for any expected strings to match. No scope creep.

## Issues Encountered
- Initial approach of regex-based transformation (transform-tests.mjs) failed due to too many edge cases in type qualification patterns
- Extract-actual.mjs approach partially worked but had xUnit prefix parsing issues with context line detection
- Final approach using Error Message section (no xUnit prefix) diff parsing worked reliably
- Tests with multiple generated files required iterative update passes (script processes one diff per test per pass)
- New test files created by Write tool had CRLF endings on Windows, needed explicit conversion to LF

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All Plan 01 + Plan 02 changes verified with 173/174 passing tests
- 1 pre-existing test failure documented in deferred-items.md (MergeDissimilarStepsTests CS0246 - unbound generic step type reference)
- Phase 06 generated code hardening is complete

---
*Phase: 06-generated-code-hardening*
*Completed: 2026-03-09*
