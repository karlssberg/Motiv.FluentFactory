---
phase: 17-core-generator-type-renames
plan: 03
subsystem: generator
tags: [roslyn, source-generator, rename, refactor, syntax-generation]

# Dependency graph
requires:
  - phase: 17-core-generator-type-renames
    provides: FluentFactoryGenerator and FluentFactoryCompilationUnit renamed (17-01), FluentFactoryMetadata/Reader/Defaults renamed (17-02)
provides:
  - StepTerminalMethodDeclaration (renamed from FluentFactoryMethodDeclaration) at SyntaxGeneration/StepTerminalMethodDeclaration.cs
  - RootTerminalMethodDeclaration (renamed from FluentRootFactoryMethodDeclaration) at SyntaxGeneration/RootTerminalMethodDeclaration.cs
  - Phase 17 complete: all five FluentFactory* type names eliminated from src/Converj.Generator/
affects: [18-builder-renames]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Terminal method vocabulary: Step* prefix for step-resident methods, Root* prefix for root-resident methods, consistent with TerminalMethod/TerminalMethodKind enum"

key-files:
  created:
    - src/Converj.Generator/SyntaxGeneration/StepTerminalMethodDeclaration.cs
    - src/Converj.Generator/SyntaxGeneration/RootTerminalMethodDeclaration.cs
  modified:
    - src/Converj.Generator/SyntaxGeneration/FluentStepDeclaration.cs
    - src/Converj.Generator/SyntaxGeneration/RootTypeDeclaration.cs

key-decisions:
  - "FluentFactoryMethodDeclaration renamed to StepTerminalMethodDeclaration: Step prefix mirrors sibling FluentStepMethodDeclaration, Terminal qualifier distinguishes terminal from transition methods"
  - "FluentRootFactoryMethodDeclaration renamed to RootTerminalMethodDeclaration: Root prefix tells reader the declaration lives on the root type, symmetric with StepTerminalMethodDeclaration"
  - "Phase 17 zero-hit gate uses word-boundary regex refinement (negative look-arounds) to exclude the three deferred method-name substrings: CreateFluentFactoryCompilationUnit, GetFluentFactoryMetadata, GetFluentFactoryDefaults — Phase 18 obligation"
  - "Example/Generated directory rename (FluentFactoryGenerator to FluentRootGenerator) included in this commit as carry-forward from 17-01 staged work"

patterns-established:
  - "Terminal vocabulary: StepTerminalMethodDeclaration (step-resident) and RootTerminalMethodDeclaration (root-resident) establish Step*/Root* + Terminal naming for terminal-emission helpers, consistent with public TerminalMethod enum"

requirements-completed: [NAME-04, FILE-01]

# Metrics
duration: 15min
completed: 2026-04-12
---

# Phase 17 Plan 03: Terminal Method Declaration Renames Summary

**FluentFactoryMethodDeclaration and FluentRootFactoryMethodDeclaration renamed to StepTerminalMethodDeclaration and RootTerminalMethodDeclaration via git mv, closing Phase 17 with zero legacy FluentFactory* type names in src/Converj.Generator/**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-04-12T~01:20Z
- **Completed:** 2026-04-12T~01:35Z
- **Tasks:** 2
- **Files modified:** 20 (2 renamed, 2 call sites updated, 16 Example/Generated carry-forward)

## Accomplishments

- Renamed `FluentFactoryMethodDeclaration` → `StepTerminalMethodDeclaration` via `git mv`, preserving full history
- Renamed `FluentRootFactoryMethodDeclaration` → `RootTerminalMethodDeclaration` via `git mv`, preserving full history
- Added class-level XML doc summaries to both types documenting their distinct responsibilities
- Updated the `Create` doc on `RootTerminalMethodDeclaration` to reflect inline construction purpose
- Updated both call sites: `FluentStepDeclaration.cs` and `RootTypeDeclaration.cs`
- Phase 17 success criterion #5 (word-boundary grep) returns zero hits across `src/Converj.Generator/`
- All 415 tests pass (362 generator + 53 integration), zero build warnings

## Task Commits

Each task was committed atomically:

1. **Task 1 + Task 2: git mv, rename types, update call sites, verify gates** - `48250bc` (refactor)

**Plan metadata:** (see final commit below)

## Final Phase 17 Verification Gate Results

All six checks passed:

1. `dotnet build -warnaserror` — succeeded with zero warnings
2. `dotnet test` — 415 tests passed (362 + 53), zero failures, zero skipped
3. **Phase 17 success criterion #5 (word-boundary grep)** across `src/Converj.Generator/`:
   - Pattern: `(?<![A-Za-z])(FluentFactoryGenerator|FluentFactoryCompilationUnit|FluentFactoryMetadata|FluentFactoryDefaults|FluentFactoryMethodDeclaration)(?![A-Za-z])`
   - Result: **zero hits**
4. **Wider grep** across `src/**/*.cs` (including `FluentRootFactoryMethodDeclaration`): **zero hits**
5. `git log --follow` verified for all seven renamed files:
   - `FluentRootGenerator.cs` (17-01) — history from `44f8e97` through prior commits
   - `FluentRootCompilationUnit.cs` (17-01) — history from `44f8e97` through prior commits
   - `FluentRootMetadata.cs` (17-02) — history from `41a6b40` through prior commits
   - `FluentRootMetadataReader.cs` (17-02) — history from `41a6b40` through prior commits
   - `FluentRootDefaults.cs` (17-02) — history from `41a6b40` through prior commits
   - `StepTerminalMethodDeclaration.cs` (17-03) — history from `48250bc` through prior commits
   - `RootTerminalMethodDeclaration.cs` (17-03) — history from `48250bc` through prior commits
6. NAME-04 satisfied with Step*/Root* + Terminal vocabulary; rename mapping documented in plan body and summary

## Rename Mapping Applied

| Old name | New name | File moved |
|----------|----------|------------|
| `FluentFactoryMethodDeclaration` | `StepTerminalMethodDeclaration` | `SyntaxGeneration/StepTerminalMethodDeclaration.cs` |
| `FluentRootFactoryMethodDeclaration` | `RootTerminalMethodDeclaration` | `SyntaxGeneration/RootTerminalMethodDeclaration.cs` |

**Rationale:**
- `Step` prefix: mirrors sibling `FluentStepMethodDeclaration`, communicates that this declaration lives inside a step struct
- `Root` prefix: mirrors `RootTypeDeclaration`, communicates that this declaration lives on the root type
- `Terminal` qualifier: aligns with public `TerminalMethod`/`TerminalMethodKind` enum vocabulary, distinguishes terminal (creation) methods from transition methods

## Call Sites Updated

| File | Old call | New call |
|------|----------|---------|
| `FluentStepDeclaration.cs` | `FluentFactoryMethodDeclaration.Create(createMethod, step)` | `StepTerminalMethodDeclaration.Create(createMethod, step)` |
| `RootTypeDeclaration.cs` | `FluentRootFactoryMethodDeclaration.Create(method, file.RootType)` | `RootTerminalMethodDeclaration.Create(method, file.RootType)` |

## Files Created/Modified

- `src/Converj.Generator/SyntaxGeneration/StepTerminalMethodDeclaration.cs` — Renamed from FluentFactoryMethodDeclaration.cs; class renamed; class-level XML doc added
- `src/Converj.Generator/SyntaxGeneration/RootTerminalMethodDeclaration.cs` — Renamed from FluentRootFactoryMethodDeclaration.cs; class renamed; class-level XML doc added; Create method doc updated
- `src/Converj.Generator/SyntaxGeneration/FluentStepDeclaration.cs` — Call site updated (1 line)
- `src/Converj.Generator/SyntaxGeneration/RootTypeDeclaration.cs` — Call site updated (1 line)
- `src/Converj.Example/Generated/Converj.Generator/Converj.Generator.FluentRootGenerator/` — 16 generated .g.cs files moved from FluentFactoryGenerator directory (carry-forward from 17-01 staged work)

## Decisions Made

- Word-boundary regex refinement used for Phase 17 success criterion #5: `(?<![A-Za-z])...(?![A-Za-z])` ensures only standalone type-name tokens are matched, excluding the three deferred method identifiers (`CreateFluentFactoryCompilationUnit`, `GetFluentFactoryMetadata`, `GetFluentFactoryDefaults`) that are Phase 18 scope
- Example/Generated directory rename included as carry-forward from Phase 17-01 staged work (not a deviation — correctly scoped to Phase 17)

## Deviations from Plan

None — plan executed exactly as written.

## Phase 17 Cumulative Status: COMPLETE

All five Phase 17 requirements now satisfied:

| Requirement | Description | Satisfied in |
|-------------|-------------|--------------|
| NAME-01 | `FluentFactoryGenerator` → `FluentRootGenerator` | 17-01 |
| NAME-02 | `FluentFactoryCompilationUnit` → `FluentRootCompilationUnit` | 17-01 |
| NAME-03 | `FluentFactoryMetadata`/`Reader`/`Defaults` → `FluentRoot*` trio | 17-02 |
| NAME-04 | `FluentFactoryMethodDeclaration` / `FluentRootFactoryMethodDeclaration` → Terminal variants | 17-03 |
| FILE-01 | `git mv` used for all seven renamed files | 17-01, 17-02, 17-03 |

## Phase 18 Handoff Note

Three method-name renames are intentionally deferred to Phase 18:

- `CreateFluentFactoryCompilationUnit` — on `FluentModelFactory` (which Phase 18 also renames)
- `GetFluentFactoryMetadata` — on `FluentRootMetadataReader`
- `GetFluentFactoryDefaults` — on `FluentRootMetadataReader`

**Phase 18 obligation:** Once these method renames are complete alongside the `*Factory` → `*Builder` renames, Phase 18 MUST run the plain ROADMAP.md alternation (no word-boundary refinement) as its final gate:
```
git grep -n "FluentFactoryGenerator\|FluentFactoryCompilationUnit\|FluentFactoryMetadata\|FluentFactoryDefaults\|FluentFactoryMethodDeclaration" -- src/Converj.Generator/
```
That is the moment ROADMAP success criterion #5 can be satisfied verbatim.

## Issues Encountered

None.

## Next Phase Readiness

- Phase 18 (builder renames: `*Factory` → `*Builder`) is unblocked
- Phase 17 git history is clean: 7 renamed files all tracked via `git log --follow`
- Zero legacy `FluentFactory*` type names remain in `src/Converj.Generator/`

---
*Phase: 17-core-generator-type-renames*
*Completed: 2026-04-12*
