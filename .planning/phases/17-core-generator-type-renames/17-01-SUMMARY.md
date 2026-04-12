---
phase: 17-core-generator-type-renames
plan: 01
subsystem: generator
tags: [roslyn, source-generator, rename, refactor]

# Dependency graph
requires:
  - phase: 16-diagnostic-alignment
    provides: Green build and test baseline (415 tests) before core generator renames begin
provides:
  - FluentRootGenerator: renamed IIncrementalGenerator entry point at src/Converj.Generator/FluentRootGenerator.cs
  - FluentRootCompilationUnit: renamed per-root output record at src/Converj.Generator/FluentRootCompilationUnit.cs
  - Updated internal call sites in FluentModelFactory, RootTypeDeclaration, CompilationUnit, GeneratedCodeAttributeSyntax
  - Updated all 55 test files to reference FluentRootGenerator
affects:
  - 17-02 through 17-xx (remaining Phase 17 plans inherit these renamed types)
  - 18-builder-renames (FluentModelFactory call sites use FluentRootCompilationUnit return type)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "git mv for file renames: preserves commit history so git log --follow works across rename boundary"
    - "Incremental rename discipline: type names renamed in this phase, method names on FluentModelFactory deferred to Phase 18"

key-files:
  created:
    - src/Converj.Generator/FluentRootGenerator.cs (renamed from FluentFactoryGenerator.cs)
    - src/Converj.Generator/FluentRootCompilationUnit.cs (renamed from FluentFactoryCompilationUnit.cs)
  modified:
    - src/Converj.Generator/FluentModelFactory.cs
    - src/Converj.Generator/SyntaxGeneration/RootTypeDeclaration.cs
    - src/Converj.Generator/SyntaxGeneration/CompilationUnit.cs
    - src/Converj.Generator/SyntaxGeneration/Helpers/GeneratedCodeAttributeSyntax.cs
    - src/Converj.Generator.Tests/ (55 test files)

key-decisions:
  - "FluentModelFactory.CreateFluentFactoryCompilationUnit method name intentionally left unchanged — Phase 18 owns the FluentModelFactory rename and its method names follow then"
  - "git mv used for both file renames so git log --follow preserves full commit history"

patterns-established:
  - "File-rename-then-type-rename: use git mv first, then edit type names in place to get rename tracking in git"

requirements-completed: [NAME-01, NAME-02, FILE-01]

# Metrics
duration: 15min
completed: 2026-04-12
---

# Phase 17 Plan 01: Core Generator Type Renames Summary

**FluentFactoryGenerator and FluentFactoryCompilationUnit renamed to FluentRootGenerator and FluentRootCompilationUnit via git mv, with all 4 internal call sites and 55 test files updated — 415 tests green**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-04-12T00:10:00Z
- **Completed:** 2026-04-12T00:25:00Z
- **Tasks:** 3
- **Files modified:** 61 (2 renamed + 4 generator call sites + 55 test files)

## Accomplishments

- Renamed `FluentFactoryGenerator` → `FluentRootGenerator` (git mv preserved history back to original file creation)
- Renamed `FluentFactoryCompilationUnit` → `FluentRootCompilationUnit` (git mv preserved history)
- Updated all internal generator call sites: FluentModelFactory (4 hits), RootTypeDeclaration (8 hits), CompilationUnit (3 hits), GeneratedCodeAttributeSyntax (1 hit)
- Updated all 55 test files: verifier alias + typeof() + .WithSpan path fragments
- Build zero-warning, all 415 tests passing

## Task Commits

Each task was committed atomically:

1. **Task 1: git mv + type renames in place** - `44f8e97` (refactor)
2. **Task 2: Update internal call sites** - `ee4028b` (refactor)
3. **Task 3: Update test files + full test run** - `24395ae` (refactor)

## Files Created/Modified

- `src/Converj.Generator/FluentRootGenerator.cs` — Renamed from FluentFactoryGenerator.cs; class renamed, XML docs updated to [FluentTarget] vocabulary
- `src/Converj.Generator/FluentRootCompilationUnit.cs` — Renamed from FluentFactoryCompilationUnit.cs; record renamed
- `src/Converj.Generator/FluentModelFactory.cs` — Return type updated (1 hit), constructor calls updated (3 hits); method name `CreateFluentFactoryCompilationUnit` intentionally unchanged
- `src/Converj.Generator/SyntaxGeneration/RootTypeDeclaration.cs` — 8 parameter type occurrences updated
- `src/Converj.Generator/SyntaxGeneration/CompilationUnit.cs` — 3 parameter type occurrences updated
- `src/Converj.Generator/SyntaxGeneration/Helpers/GeneratedCodeAttributeSyntax.cs` — typeof() reference updated (1 hit)
- `src/Converj.Generator.Tests/**/*.cs` — 55 files updated (verifier alias, typeof, .WithSpan paths)

## Decisions Made

- `FluentModelFactory.CreateFluentFactoryCompilationUnit` method name intentionally preserved — Phase 18 owns the `FluentModelFactory` class rename, and the method name follows in that phase to avoid conflicts. The word-boundary grep `(?<![A-Za-z])(FluentFactoryGenerator|FluentFactoryCompilationUnit)(?![A-Za-z])` naturally excludes method-name substrings, so zero grep hits on the type names confirms the plan obligations are satisfied.
- `.WithSpan` path fragments in two diagnostic test files contained `Converj.Generator.FluentFactoryGenerator` as part of Roslyn test infrastructure file paths — these were updated alongside the type rename because the Roslyn verifier uses the generator class name to construct the path.

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

- Initial `sed` replacement only matched the fully-qualified `CSharpSourceGeneratorVerifier<Converj.Generator.FluentFactoryGenerator>` form; 4 test files used a short form `CSharpSourceGeneratorVerifier<FluentFactoryGenerator>` requiring a second targeted pass. Also, 2 test files contained `.WithSpan` path fragments requiring direct Edit (sed backslash escaping failed). All resolved in Task 3 before committing.

## Test Suite Result

**415 tests passed** (362 generator tests + 53 integration tests) — matches Phase 16 baseline exactly. No new failures, no skipped tests, no modified assertions.

## git log --follow Verification

```
git log --follow --oneline -n 5 -- src/Converj.Generator/FluentRootGenerator.cs
  → 44f8e97  (rename commit)
  → 4ea68c0  feat: implement fluent factory methods for meal creation...
  → b18ed2f  refactor: rename FluentFactory and FluentConstructor attributes...
  (history continues back to original file creation)

git log --follow --oneline -n 5 -- src/Converj.Generator/FluentRootCompilationUnit.cs
  → 44f8e97  (rename commit)
  → 6c8d59d  feat: support partial parameter overlap...
  → 59bbeeb  refactor: rename Converg to Converj...
  (history continues back to original file creation)
```

## Deferred Items

`FluentModelFactory.CreateFluentFactoryCompilationUnit` method name — tracked for Phase 18 (NAME-05). Not a regression of Plan 17-01.

## Next Phase Readiness

- Phase 17-02 can proceed: `FluentRootGenerator` and `FluentRootCompilationUnit` are in place
- The green 415-test baseline is the starting point for Phase 17-02
- No blockers

---
*Phase: 17-core-generator-type-renames*
*Completed: 2026-04-12*
