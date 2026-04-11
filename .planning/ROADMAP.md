# Roadmap: Converj

## Milestones

- ✅ **v1.0 Initial Release** — Phases 1-5 (shipped 2026-03-09)
- ✅ **v1.1 Code Generation Quality** — Phase 6 (shipped 2026-03-09)
- ✅ **v1.2 Architecture Refactoring** — Phases 7-10 (shipped 2026-03-11)
- ✅ **v1.3 Edge Case Stress Testing** — Phases 11-15 (shipped 2026-03-14)
- ✅ **v2.0 Converj Rename + Feature Expansion** — organic/unplanned (shipped 2026-04-11)
- 🚧 **v2.1 Naming Alignment Refactor** — Phases 16-20 (in progress)

## Phases

<details>
<summary>✅ v1.0 Initial Release (Phases 1-5) — SHIPPED 2026-03-09</summary>

Phases 1-5 delivered the initial release: attribute-based API, fluent step struct generation, generic type support, method customization, multiple fluent methods, NoCreateMethod, XML docs, diagnostics, primary constructor support, and NuGet packaging.

</details>

<details>
<summary>✅ v1.1 Code Generation Quality (Phase 6) — SHIPPED 2026-03-09</summary>

- [x] Phase 6: Generated Code Hardening (2/2 plans) — completed 2026-03-09

</details>

<details>
<summary>✅ v1.2 Architecture Refactoring (Phases 7-10) — SHIPPED 2026-03-11</summary>

- [x] Phase 7: Core Pipeline Decomposition (3/3 plans) — completed 2026-03-10
- [x] Phase 8: Syntax Generator Decomposition (3/3 plans) — completed 2026-03-11
- [x] Phase 9: Extension Consolidation (1/1 plan) — completed 2026-03-11
- [x] Phase 10: Screaming Architecture Reorganization (2/2 plans) — completed 2026-03-11

</details>

<details>
<summary>✅ v1.3 Edge Case Stress Testing (Phases 11-15) — SHIPPED 2026-03-14</summary>

- [x] Phase 11: Type System Edge Cases (2/2 plans) — completed 2026-03-14
- [x] Phase 12: Constructor Variation Edge Cases (2/2 plans) — completed 2026-03-14
- [x] Phase 13: Internal Correctness (2/2 plans) — completed 2026-03-14
- [x] Phase 14: Diagnostic Edge Cases (2/2 plans) — completed 2026-03-14
- [x] Phase 15: Scope and Accessibility Diagnostics (2/2 plans) — completed 2026-03-14

</details>

<details>
<summary>✅ v2.0 Converj Rename + Feature Expansion — SHIPPED 2026-04-11 (organic/unplanned)</summary>

v2.0 shipped as 65 commits of unplanned, organic work. See `.planning/MILESTONES.md` for full details of what shipped, including package/attribute rename, new target kinds (static/extension/property), type-first builder mode, and API expansion. Known tech debt (internal vocabulary still using `FluentFactory*`/`FluentConstructor*`) carried forward to v2.1.

</details>

### 🚧 v2.1 Naming Alignment Refactor (In Progress)

**Milestone Goal:** Align internal codebase vocabulary (type names, file names, test fixtures, diagnostic IDs, docs) with the public `FluentRoot`/`FluentTarget` API shipped in v2.0. Pure rename refactor — no feature work, no behavior changes, no API changes. Every existing test must pass at every phase boundary.

## Phase Summary Checklist

- [ ] **Phase 16: Diagnostic Alignment** - Update diagnostic category strings, titles, message formats, and AnalyzerReleases to remove `FluentFactory` / `MFFG` vocabulary
- [ ] **Phase 17: Core Generator Type Renames** - Rename the top-level `FluentFactory*` generator types (Generator, CompilationUnit, Metadata, MethodDeclaration) to `FluentRoot*` vocabulary and `git mv` their files
- [ ] **Phase 18: Builder Pattern Renames** - Rename internal GoF factory helpers (`FluentModelFactory`, `FluentMethodFactory`, `IgnoredMultiMethodWarningFactory`) to `*Builder` and `git mv` their files
- [ ] **Phase 19: Test Fixture Alignment** - Rename test classes, test files, and in-test sample types carrying legacy `*Factory*` vocabulary
- [ ] **Phase 20: Documentation Cleanup & Final Verification** - Align `CLAUDE.md` files, perform final repo-wide grep, confirm no residual vocabulary remains, and formally verify behavior preservation across the milestone

## Phase Details

### Phase 16: Diagnostic Alignment
**Goal**: All diagnostic descriptors, messages, and release notes use Converj vocabulary; no diagnostic-producing code contains `MFFG`, `FluentFactory` (as category), or `FluentConstructor` string literals
**Depends on**: v2.0 shipped (Converj rename baseline in place)
**Requirements**: DIAG-01, DIAG-02, DIAG-03, DIAG-04
**Success Criteria** (what must be TRUE):
  1. Every descriptor in `src/Converj.Generator/Diagnostics/FluentDiagnostics.cs` (all 47) uses `Category = "Converj"` — verifiable by grep for the old `"FluentFactory"` category literal returning zero hits in that file
  2. Diagnostic titles and message formats that referred to "Factory" for the fluent root now say "FluentRoot" (or equivalent); titles referring to C# constructors or the retained GoF pattern are untouched — verifiable by reviewing the descriptor list
  3. `src/Converj.Generator/AnalyzerReleases.Unshipped.md` reflects the new category string and is internally consistent with the descriptors
  4. `git grep -n "MFFG\|\"FluentFactory\"\|FluentConstructor" -- src/Converj.Generator/Diagnostics/` returns zero hits
  5. `dotnet build` succeeds with zero warnings and `dotnet test` passes all existing tests (no assertion changes required for pure category/title edits)
**Plans**: TBD

### Phase 17: Core Generator Type Renames
**Goal**: The top-level generator types that still carry `FluentFactory*` vocabulary are renamed to `FluentRoot*` equivalents, and their source files are `git mv`'d to match; build and tests remain green
**Depends on**: Phase 16
**Requirements**: NAME-01, NAME-02, NAME-03, NAME-04, FILE-01
**Success Criteria** (what must be TRUE):
  1. The `IIncrementalGenerator` entry type is named `FluentRootGenerator` and its source file is `FluentRootGenerator.cs`
  2. The compilation-unit top-level output type is named `FluentRootCompilationUnit` and lives in a file of the same name
  3. `FluentFactoryMetadata`, `FluentFactoryMetadataReader`, and `FluentFactoryDefaults` are renamed under Root vocabulary (e.g., `FluentRootMetadata`, `FluentRootMetadataReader`, `FluentRootDefaults`) with matching file names
  4. `FluentFactoryMethodDeclaration` and `FluentRootFactoryMethodDeclaration` are renamed to reflect their actual responsibilities using the chosen Root/Step/Target/Entry vocabulary, documented in the phase's plan
  5. Every file moved in this phase was moved with `git mv` (verifiable via `git log --follow` preserving history), and `git grep -n "FluentFactoryGenerator\|FluentFactoryCompilationUnit\|FluentFactoryMetadata\|FluentFactoryDefaults\|FluentFactoryMethodDeclaration" -- src/Converj.Generator/` returns zero hits
  6. `dotnet build` succeeds with zero warnings and `dotnet test` passes all existing tests
**Plans**: TBD

### Phase 18: Builder Pattern Renames
**Goal**: Internal GoF-style factory helper types that build fluent models are renamed to `*Builder` to avoid confusion with the public `[FluentRoot]`/`[FluentTarget]` (née FluentFactory) vocabulary
**Depends on**: Phase 17
**Requirements**: NAME-05, NAME-06, NAME-07
**Success Criteria** (what must be TRUE):
  1. `FluentModelFactory` is renamed to `FluentModelBuilder` and its file is `git mv`'d to `FluentModelBuilder.cs`
  2. `FluentMethodFactory` is renamed to `FluentMethodBuilder` and its file is `git mv`'d to `FluentMethodBuilder.cs`
  3. `IgnoredMultiMethodWarningFactory` is renamed to `IgnoredMultiMethodWarningBuilder` and its file is `git mv`'d to match
  4. `git grep -n "FluentModelFactory\|FluentMethodFactory\|IgnoredMultiMethodWarningFactory" -- src/Converj.Generator/` returns zero hits
  5. `dotnet build` succeeds with zero warnings and `dotnet test` passes all existing tests
**Plans**: TBD

### Phase 19: Test Fixture Alignment
**Goal**: Test classes, test source files, and test-local sample helper types stop using legacy `*Factory*` vocabulary for anything that refers to the fluent root
**Depends on**: Phase 18
**Requirements**: TEST-01, TEST-02, TEST-03, TEST-04, TEST-05
**Success Criteria** (what must be TRUE):
  1. `EmptyFactoryTests` is renamed to `EmptyRootTests` (class name + file name via `git mv`) and `NestedFactoryTests` is renamed to `NestedRootTests` (class name + file name via `git mv`)
  2. `NestedFactoryRuntimeTests` in `src/Converj.Tests/` is renamed to `NestedRootRuntimeTests` (class name + file name via `git mv`)
  3. Test-local sample helper types that used legacy vocabulary (e.g., a local `class Factory { ... }` inside a test file) have been renamed to Root/Target-aligned names
  4. `git grep -n "Factory\|FluentConstructor" -- src/Converj.Generator.Tests/ src/Converj.Tests/` returns zero hits for any occurrence that refers to the fluent root (occurrences referring to a C# constructor or an intentionally retained GoF pattern are allowed and documented)
  5. `dotnet build` succeeds with zero warnings and `dotnet test` passes every existing test with no modified assertions
**Plans**: TBD

### Phase 20: Documentation Cleanup & Final Verification
**Goal**: All project-level documentation uses current vocabulary, no residual legacy terminology remains anywhere in active source/tests/docs, and milestone-wide behavior preservation is formally verified
**Depends on**: Phase 19
**Requirements**: DOC-01, DOC-02, DOC-03, FILE-02, BEHAV-01, BEHAV-02, BEHAV-03, BEHAV-04
**Success Criteria** (what must be TRUE):
  1. `CLAUDE.md` at repo root references `[FluentTarget]` instead of `FluentConstructor attribute` at line 11, and any other stale vocabulary in that file is aligned
  2. `src/Converj.Generator/CLAUDE.md` and `src/Converj.Generator.Tests/CLAUDE.md` (if they exist) contain no stale `FluentFactory` / `FluentConstructor` / `BuilderMethod` / `InitialVerb` / `MFFG` / `Motiv.FluentFactory` vocabulary
  3. A repo-wide `git grep -n "FluentFactory\|FluentConstructor\|BuilderMethod\|InitialVerb\|MFFG\|Motiv\.FluentFactory"` (scoped to active source, tests, and docs, excluding `.planning/MILESTONES.md` and `.planning/v1.*-*.md` historical artifacts) returns zero hits
  4. No `.cs` file path under `src/Converj.Generator/` contains `FluentFactory`, `FluentConstructor`, or `BuilderMethod` — verifiable via `git ls-files 'src/Converj.Generator/**/*.cs'` piped through grep
  5. Milestone-level behavior preservation is verified: final `dotnet build` succeeds with zero warnings, final `dotnet test` passes every existing test, `git log --follow` shows preserved history across the renamed files (evidence that `git mv` was used, not blind delete+create), and every rename in phases 16-19 was performed via compiler-assisted rename (IDE/roslyn) rather than blind text replace
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 16 → 17 → 18 → 19 → 20

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1-5. Initial Release | v1.0 | — | Complete | 2026-03-09 |
| 6. Generated Code Hardening | v1.1 | 2/2 | Complete | 2026-03-09 |
| 7. Core Pipeline Decomposition | v1.2 | 3/3 | Complete | 2026-03-10 |
| 8. Syntax Generator Decomposition | v1.2 | 3/3 | Complete | 2026-03-11 |
| 9. Extension Consolidation | v1.2 | 1/1 | Complete | 2026-03-11 |
| 10. Screaming Architecture Reorganization | v1.2 | 2/2 | Complete | 2026-03-11 |
| 11. Type System Edge Cases | v1.3 | 2/2 | Complete | 2026-03-14 |
| 12. Constructor Variation Edge Cases | v1.3 | 2/2 | Complete | 2026-03-14 |
| 13. Internal Correctness | v1.3 | 2/2 | Complete | 2026-03-14 |
| 14. Diagnostic Edge Cases | v1.3 | 2/2 | Complete | 2026-03-14 |
| 15. Scope and Accessibility Diagnostics | v1.3 | 2/2 | Complete | 2026-03-14 |
| — v2.0 organic work — | v2.0 | — | Complete | 2026-04-11 |
| 16. Diagnostic Alignment | v2.1 | 0/? | Not started | - |
| 17. Core Generator Type Renames | v2.1 | 0/? | Not started | - |
| 18. Builder Pattern Renames | v2.1 | 0/? | Not started | - |
| 19. Test Fixture Alignment | v2.1 | 0/? | Not started | - |
| 20. Documentation Cleanup & Final Verification | v2.1 | 0/? | Not started | - |
