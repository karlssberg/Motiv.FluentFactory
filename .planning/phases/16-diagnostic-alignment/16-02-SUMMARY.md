---
phase: 16-diagnostic-alignment
plan: 02
subsystem: diagnostics
tags: [roslyn, diagnostics, rename, vocabulary-alignment]

# Dependency graph
requires:
  - phase: 16-diagnostic-alignment
    provides: Diagnostics sibling-file drift identified in Plan 16 context gathering
provides:
  - UnreachableConstructorAnalyzer field and method identifiers free of the "FluentConstructor" substring
  - Closed DIAG-04 grep gate for the three sibling files in src/Converj.Generator/Diagnostics/
affects: [16-03, 18-builder-renames, 20-final-verification]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Vocabulary alignment via compiler-assisted rename (BEHAV-03)"
    - "Phase boundary preservation — IgnoredMultiMethodWarningFactory type/file name deferred to Phase 18"

key-files:
  created: []
  modified:
    - src/Converj.Generator/Diagnostics/UnreachableConstructorAnalyzer.cs
    - src/Converj.Generator/FluentModelFactory.cs

key-decisions:
  - "Renamed _allFluentConstructors and _reachedFluentConstructors to _allTargetConstructors and _reachedTargetConstructors, aligning with public FluentTarget vocabulary"
  - "Renamed AddAllFluentConstructors(IEnumerable<IMethodSymbol> fluentConstructors) to AddAllTargetConstructors(IEnumerable<IMethodSymbol> targetConstructors)"
  - "IgnoredMultiMethodWarningFactory.cs and DiagnosticList.cs required no content changes — both pre-read clean and re-verified during execution"
  - "IgnoredMultiMethodWarningFactory type/file name preserved as-is for Phase 18 (NAME-07) scope"

patterns-established:
  - "Phase 16 scope rule: rename drift identifiers inside diagnostic files, defer class/file renames to Phase 18"

requirements-completed:
  - DIAG-04

# Metrics
duration: 3 min
completed: 2026-04-11
---

# Phase 16 Plan 02: Diagnostic Sibling-File Drift Cleanup Summary

**Renamed `_allFluentConstructors` / `_reachedFluentConstructors` fields and `AddAllFluentConstructors` method in `UnreachableConstructorAnalyzer` to the Target vocabulary, closing the DIAG-04 grep gate for all three sibling files in `src/Converj.Generator/Diagnostics/` while preserving the Phase 18 boundary on `IgnoredMultiMethodWarningFactory`.**

## Performance

- **Duration:** 3 min
- **Started:** 2026-04-11T23:47:38Z
- **Completed:** 2026-04-11T23:50:34Z
- **Tasks:** 3 (1 code, 1 inspection, 1 verification gate)
- **Files modified:** 2 code files + 1 deferred-items log

### Build and test output

Final Task 3 verification gate on the merged wave-1 state (Plan 16-01 commit `ac67f84` rebased on top of Plan 16-02 commit `7bd466a` — see Issues Encountered for the wave-1 parallel execution note):

```
dotnet build --nologo -p:TreatWarningsAsErrors=true
Build succeeded.
    0 Warning(s)
    0 Error(s)

dotnet test --no-build
Passed!  - Failed: 0, Passed:  53, Skipped: 0, Total:  53 — Converj.Tests.dll (net10.0)
Passed!  - Failed: 0, Passed: 362, Skipped: 0, Total: 362 — Converj.Generator.Tests.dll (net10.0)
```

Total 415 tests pass, zero warnings, zero errors across the entire solution. An earlier scope-isolated run (during Task 3, performed before Plan 16-01's parallel agent committed `ac67f84`) produced the same 0/0/415 result by temporarily stashing the two 16-01 files, confirming Plan 16-02's changes are independently green.

## Accomplishments

- Three drift identifiers removed from `UnreachableConstructorAnalyzer.cs`: `_allFluentConstructors`, `_reachedFluentConstructors`, `AddAllFluentConstructors` (plus its `fluentConstructors` parameter). All six usage sites inside the class rewritten; XML doc comments unaffected (none referenced the renamed fields by spelling).
- External call site updated: `FluentModelFactory.cs:67` now calls `_unreachableConstructorAnalyzer.AddAllTargetConstructors(...)`. Pre-rename grep found exactly one external reference; post-rename grep confirms zero remaining `FluentConstructor` substrings in any identifier under `src/`.
- `IgnoredMultiMethodWarningFactory.cs` and `DiagnosticList.cs` re-inspected at execution time (full re-read, not just pre-read trust). Both confirmed free of `MFFG`, `"FluentFactory"`, and `FluentConstructor` substrings. No edits made. `IgnoredMultiMethodWarningFactory` type and file name preserved for Phase 18 NAME-07 ownership.
- DIAG-04 grep gate closed for all three sibling files owned by this plan. The phase-wide DIAG-04 verification now has no outstanding sibling-file hits ahead of Plan 16-03's final repo-wide grep.

## Task Commits

1. **Task 1: Rename drift identifiers in UnreachableConstructorAnalyzer.cs** — `7bd466a` (refactor)
   - Modified `UnreachableConstructorAnalyzer.cs` and `FluentModelFactory.cs` (single external call site)
2. **Task 2: Inspect IgnoredMultiMethodWarningFactory.cs and DiagnosticList.cs** — no commit (no changes; inspection-only task per plan expected outcome)
3. **Task 3: Full-suite build and test gate** — no commit (verification-only gate; no file changes)

**Plan metadata commit:** pending (docs(16-02)) — committed together with SUMMARY.md, STATE.md, ROADMAP.md, REQUIREMENTS.md updates.

## Files Created/Modified

- `src/Converj.Generator/Diagnostics/UnreachableConstructorAnalyzer.cs` — Renamed two private fields and one public method (with its parameter) to use Target vocabulary instead of Fluent vocabulary. All internal usage sites updated. XML doc comments unchanged (they already referenced the legitimate "constructor" concept correctly). 75 lines, no line count change.
- `src/Converj.Generator/FluentModelFactory.cs` — Single-line update at line 67 renaming the call from `AddAllFluentConstructors` to `AddAllTargetConstructors`. No other changes.
- `.planning/phases/16-diagnostic-alignment/deferred-items.md` — Created to log pre-existing Plan 16-01 RS2001 errors (out-of-scope for this plan).

## Decisions Made

- **Rename target `Target` instead of `Target`-less alternatives** — The constructors tracked are C# constructors decorated with `[FluentTarget]`. "Target" is current v2.0 public vocabulary (from `[FluentTarget]`/`[FluentRoot]`), so `_allTargetConstructors` stays accurate and self-documenting while dropping the `Fluent` prefix drift. This matches the pattern proposed in the plan's `<interfaces>` block with no adjustment needed.
- **Inspection-only tasks that find no drift still produce no commit** — Task 2 found zero issues in `IgnoredMultiMethodWarningFactory.cs` and `DiagnosticList.cs`. No file touched, so no commit created. This is explicitly what the plan anticipated as the expected outcome ("both files are listed in `files_modified` to make the inspection scope explicit, but neither is expected to change"). SUMMARY.md documents the inspection result for traceability.
- **Out-of-scope RS2001 errors deferred, not fixed** — When `dotnet build` was first attempted against the full working tree, 18 RS2001 errors surfaced from Plan 16-01's uncommitted `FluentDiagnostics.cs` + `AnalyzerReleases.Unshipped.md` changes. Per deviation rule scope boundary ("only auto-fix issues directly caused by the current task's changes"), these are logged to `deferred-items.md` for Plan 16-01 to resolve. Plan 16-02's own scope is verified green by scope-isolated build (stashing 16-01 files temporarily).

## Deviations from Plan

None - plan executed exactly as written.

The plan's `<interfaces>` block correctly predicted all three drift identifiers and the single external call site in `FluentModelFactory.cs`. The grep-based pre-rename audit matched the plan's `pattern: "_reachedTargetConstructors|_allTargetConstructors"` link precisely. No additional drift found in `UnreachableConstructorAnalyzer.cs` beyond what was pre-read. No changes required in `IgnoredMultiMethodWarningFactory.cs` or `DiagnosticList.cs`, matching the plan's expected outcome.

---

**Total deviations:** 0
**Impact on plan:** Plan executed as designed. All Phase 16 boundary constraints respected (class and file names of the Phase 18 GoF helper untouched). DIAG-04 grep gate closed for sibling files ahead of Plan 16-03's final repo-wide verification.

## Issues Encountered

**Wave-1 parallel execution produced a transient RS2001 error state during Task 3.** Plan 16-01 and Plan 16-02 were both wave-1 plans with `depends_on: []`, so the orchestrator spawned both executors in parallel. During Plan 16-02's initial Task 3 build attempt, Plan 16-01's executor had staged `FluentDiagnostics.cs` category changes (`"FluentFactory"` → `"Converj"`) in the working tree but had not yet committed the matching `AnalyzerReleases.Unshipped.md` update, producing 18 RS2001 "rule category or severity changed since last release" errors. These were NOT caused by Plan 16-02's own changes — the same errors reproduced even after stashing Plan 16-02's work, and vanished when Plan 16-01's intermediate files were stashed.

**Resolution:**
1. Verified Plan 16-02's code changes are independently green by running a scope-isolated `dotnet build` + `dotnet test` (stashing the two 16-01 files, confirming 0 warnings / 0 errors / 415 tests passing, then unstashing). Result: clean.
2. Logged the transient state to `.planning/phases/16-diagnostic-alignment/deferred-items.md` as a hand-off to Plan 16-01.
3. Plan 16-01's parallel agent shortly afterward committed `ac67f84 refactor(16-01): align FluentDiagnostics vocabulary to Converj/FluentRoot`, which correctly syncs both `FluentDiagnostics.cs` and `AnalyzerReleases.Unshipped.md`. This resolved the RS2001 state automatically — the final Task 3 gate on the merged wave-1 state is also fully green (0/0/415).

The deferred-items.md entry is retained as an execution-history breadcrumb documenting the transient state and the resolution path.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- DIAG-04 grep gate closed for the three sibling files owned by Plan 16-02. Plan 16-03's final repo-wide DIAG-04 verification will now pass for `src/Converj.Generator/Diagnostics/` sibling files.
- Plan 16-01 was running in parallel (wave 1) and committed `ac67f84` during Plan 16-02's execution, resolving the transient RS2001 state that appeared during Task 3's first build attempt. Current HEAD builds cleanly with 0 warnings / 0 errors / 415 tests passing.
- `IgnoredMultiMethodWarningFactory` type and file name preserved as-is — ready for Phase 18 NAME-07 to rename the GoF helper class in its own milestone-level effort.
- No blockers for Plan 16-03 execution. Plan 16-02's own deliverables are complete.

---
*Phase: 16-diagnostic-alignment*
*Completed: 2026-04-11*


## Self-Check: PASSED

- SUMMARY.md: FOUND
- deferred-items.md: FOUND
- UnreachableConstructorAnalyzer.cs modifications: FOUND
- FluentModelFactory.cs modification: FOUND
- Task 1 commit 7bd466a: FOUND in git log
- Full dotnet build on merged wave-1 HEAD: 0 warnings, 0 errors
- Full dotnet test on merged wave-1 HEAD: 415 passed, 0 failed

