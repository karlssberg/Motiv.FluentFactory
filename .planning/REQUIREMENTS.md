# Requirements: Converj v2.1

**Defined:** 2026-04-11
**Core Value:** Developers write constructor parameters once and get a complete, type-safe fluent builder API generated automatically
**Milestone:** v2.1 — Naming Alignment Refactor

This milestone is a **pure rename refactor**. No feature additions, no behavior changes, no generated output changes, no public API changes. Every existing test must pass after every phase.

## v2.1 Requirements

### Internal Type Renames

- [ ] **NAME-01**: `FluentFactoryGenerator` is renamed to `FluentRootGenerator` (the top-level `IIncrementalGenerator`)
- [ ] **NAME-02**: `FluentFactoryCompilationUnit` is renamed to `FluentRootCompilationUnit`
- [ ] **NAME-03**: `FluentFactoryMetadata`, `FluentFactoryMetadataReader`, and `FluentFactoryDefaults` are renamed under Root vocabulary (e.g., `FluentRootMetadata`, `FluentRootMetadataReader`, `FluentRootDefaults`)
- [ ] **NAME-04**: `FluentFactoryMethodDeclaration` and `FluentRootFactoryMethodDeclaration` are renamed to reflect their actual responsibilities (executor reads code to pick accurate Root/Step/Target/Entry terminology, documents the chosen vocabulary)
- [ ] **NAME-05**: `FluentModelFactory` is renamed to `FluentModelBuilder`
- [ ] **NAME-06**: `FluentMethodFactory` is renamed to `FluentMethodBuilder`
- [ ] **NAME-07**: `IgnoredMultiMethodWarningFactory` is renamed to `IgnoredMultiMethodWarningBuilder`

### Source File Renames

- [ ] **FILE-01**: Every source file whose type was renamed in NAME-01..07 is moved via `git mv` to match the new type name (preserves blame/history)
- [ ] **FILE-02**: No `.cs` file in `src/Converj.Generator/` carries `FluentFactory`, `FluentConstructor`, or `BuilderMethod` in its path

### Diagnostic Alignment

- [x] **DIAG-01**: The diagnostic `Category` string on every descriptor (currently `"FluentFactory"`) is updated to `"Converj"` across all 47 descriptors in `src/Converj.Generator/Diagnostics/FluentDiagnostics.cs`
- [x] **DIAG-02**: User-facing diagnostic titles and message formats no longer contain the word "Factory" when the intent is to refer to the fluent root (e.g., "Factory type missing partial modifier" → "FluentRoot type missing partial modifier"). Titles referring to C# constructors or the GoF pattern stay untouched
- [ ] **DIAG-03**: Every entry in `src/Converj.Generator/AnalyzerReleases.Unshipped.md` is updated to use the new `Category` string for consistency with the descriptors
- [x] **DIAG-04**: No `MFFG`, `FluentFactory` (as category), or `FluentConstructor` string literals remain in diagnostic-producing code

### Test Fixture Renames

- [ ] **TEST-01**: `EmptyFactoryTests` is renamed to `EmptyRootTests` (class name + file name via `git mv`)
- [ ] **TEST-02**: `NestedFactoryTests` is renamed to `NestedRootTests` (class name + file name)
- [ ] **TEST-03**: `NestedFactoryRuntimeTests` in `src/Converj.Tests/` is renamed to `NestedRootRuntimeTests` (class name + file name)
- [ ] **TEST-04**: Test-local sample helper types that use legacy vocabulary (e.g., a local `class Factory { ... }` inside a test file) are renamed to Root/Target-aligned names
- [ ] **TEST-05**: No test class or test file name in `src/Converj.Generator.Tests/` or `src/Converj.Tests/` contains `Factory` or `FluentConstructor` unless the word refers to a C# constructor or a GoF pattern that was retained

### Documentation Alignment

- [ ] **DOC-01**: `CLAUDE.md` line 11 (the header comment referencing `FluentConstructor attribute`) is updated to reference `[FluentTarget]`; any other stale vocabulary in `CLAUDE.md` is aligned
- [ ] **DOC-02**: `src/Converj.Generator/CLAUDE.md` and `src/Converj.Generator.Tests/CLAUDE.md` (if they exist) are audited for stale vocabulary
- [ ] **DOC-03**: A final repo-wide grep confirms no residual `FluentFactory`, `FluentConstructor`, `BuilderMethod`, `InitialVerb`, `MFFG`, or `Motiv.FluentFactory` references remain in active source, tests, or docs (excluding `.planning/MILESTONES.md` archive and `.planning/v1.*-*.md` historical artifacts)

### Behavior Preservation

- [ ] **BEHAV-01**: `dotnet build` succeeds with zero warnings at every phase boundary
- [ ] **BEHAV-02**: `dotnet test` passes every existing test at every phase boundary (no new failing tests, no skipped tests, no modified assertions)
- [ ] **BEHAV-03**: Every rename uses compiler-assisted refactoring (IDE rename or equivalent find-replace plus compile verification) rather than blind text replace
- [ ] **BEHAV-04**: `git mv` is used for every file relocation to preserve file history

## Future Requirements

None — this is a focused naming alignment milestone. Any newly discovered naming drift encountered during execution gets captured as a follow-up milestone, not rolled in.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Public attribute API changes | Already shipped in v2.0; changing them again would break consumers |
| Namespace restructuring | Separate concern; this milestone only touches type/file/test/diagnostic vocabulary |
| Bug fixes for v1.3 tech debt | Documented separately in `v1.3-MILESTONE-AUDIT.md`; separate milestone |
| Behavior changes to any diagnostic | Titles/messages may be edited for vocabulary only; severity, ID, and triggering conditions are fixed |
| Generated output format changes | Consumers depend on current output; any change is a breaking release |
| Test assertion changes | Only fixture/class/file names change; assertions remain identical |
| Feature additions | Pure refactor milestone |
| Performance optimization | Out of scope |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| NAME-01 | Phase 17 | Pending |
| NAME-02 | Phase 17 | Pending |
| NAME-03 | Phase 17 | Pending |
| NAME-04 | Phase 17 | Pending |
| NAME-05 | Phase 18 | Pending |
| NAME-06 | Phase 18 | Pending |
| NAME-07 | Phase 18 | Pending |
| FILE-01 | Phase 17 | Pending |
| FILE-02 | Phase 20 | Pending |
| DIAG-01 | Phase 16 | Complete |
| DIAG-02 | Phase 16 | Complete |
| DIAG-03 | Phase 16 | Pending |
| DIAG-04 | Phase 16 | Complete |
| TEST-01 | Phase 19 | Pending |
| TEST-02 | Phase 19 | Pending |
| TEST-03 | Phase 19 | Pending |
| TEST-04 | Phase 19 | Pending |
| TEST-05 | Phase 19 | Pending |
| DOC-01 | Phase 20 | Pending |
| DOC-02 | Phase 20 | Pending |
| DOC-03 | Phase 20 | Pending |
| BEHAV-01 | Phase 20 | Pending |
| BEHAV-02 | Phase 20 | Pending |
| BEHAV-03 | Phase 20 | Pending |
| BEHAV-04 | Phase 20 | Pending |

**Coverage:**
- v2.1 requirements: 25 total
- Mapped to phases: 25 (100%)
- Unmapped: 0

**Phase distribution:**
- Phase 16 (Diagnostic Alignment): 4 requirements (DIAG-01..04)
- Phase 17 (Core Generator Type Renames): 5 requirements (NAME-01..04, FILE-01)
- Phase 18 (Builder Pattern Renames): 3 requirements (NAME-05..07)
- Phase 19 (Test Fixture Alignment): 5 requirements (TEST-01..05)
- Phase 20 (Documentation Cleanup & Final Verification): 8 requirements (DOC-01..03, FILE-02, BEHAV-01..04)

**Note on BEHAV-01..04:** These are cross-cutting behavior-preservation requirements. They are formally owned by Phase 20 (final verification) but every phase's success criteria include a build/test green gate, so each phase enforces them in practice. FILE-01 is similarly cross-cutting across Phases 17 and 18; it is formally assigned to Phase 17 (where file moves begin) and Phase 18 inherits the same discipline. FILE-02 (residual verification) lives in Phase 20.

---
*Requirements defined: 2026-04-11*
*Last updated: 2026-04-11 after roadmap creation (traceability filled)*
