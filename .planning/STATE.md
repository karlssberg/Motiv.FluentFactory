---
gsd_state_version: 1.0
milestone: v2.1
milestone_name: Naming Alignment Refactor
status: completed
stopped_at: Completed 19-03-PLAN.md
last_updated: "2026-04-12T18:50:36.240Z"
last_activity: 2026-04-12 — Plan 19-03 executed (TEST-05 grep gate passed, 4 residual comment vocabulary fixes, GoF PropositionFactory* exclusions documented, 415 tests passing)
progress:
  total_phases: 5
  completed_phases: 4
  total_plans: 10
  completed_plans: 10
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-11)

**Core value:** Developers write constructor parameters once and get a complete, type-safe fluent builder API generated automatically
**Current focus:** v2.1 — Naming Alignment Refactor

## Current Position

Phase: 19-test-fixture-alignment (COMPLETE)
Plan: 19-03 complete
Status: 3 of 3 plans complete in Phase 19 — Phase 19 CLOSED
Last activity: 2026-04-12 — Plan 19-03 executed (TEST-05 grep gate passed, 4 residual comment vocabulary fixes, GoF PropositionFactory* exclusions documented, 415 tests passing)

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.

Recent decisions affecting current work:

- [v2.1]: Internal rename is a dedicated milestone, separate from feature work, to keep rename churn isolated in git history
- [v2.1]: Public API is frozen — only internal types, file names, test fixtures, and diagnostic IDs are in scope
- [v2.1]: Vocabulary target is Root/Target to match the public `[FluentRoot]`/`[FluentTarget]` attributes shipped in v2.0
- [v2.1]: Behavior-preserving refactor — no test assertion changes, no generated output changes
- [v2.1 roadmap]: 5 phases ordered low-risk→high-risk: diagnostics first (Phase 16), core generator renames (Phase 17), builder renames (Phase 18), test fixtures (Phase 19), docs + final verification (Phase 20)
- [v2.1 roadmap]: BEHAV-01..04 (behavior preservation) formally owned by Phase 20 final verification but asserted as a success criterion in every phase's build/test gate
- [v2.1 roadmap]: FILE-01 (git mv for every rename) owned by Phase 17 where file moves begin; Phase 18 inherits the same discipline via BEHAV-04. FILE-02 (no residual legacy file names) verified in Phase 20
- [Phase 16-diagnostic-alignment]: Plan 16-02: Target vocabulary (not Fluent-) chosen for renamed fields, matching v2.0 public FluentTarget attribute
- [Phase 16-01]: Rule 3 scope expansion — updated AnalyzerReleases.Unshipped.md Category column (18 entries) to unblock RS2001 when flipping the Category constant. Full 47-row rewrite remains plan 16-03's responsibility.
- [Phase 16-01]: All 46 non-renamed descriptor identifiers audited for fluent-root-sense "Factory" drift; only FluentParameterOnStaticFactory (CVJG0026) required rename. Remaining descriptors refer to C# language concepts (constructor, parameter) or Fluent* attribute family and were left untouched per vocabulary policy.
- [Phase 16-diagnostic-alignment]: Plan 16-03: Actual descriptor count is 48, not the plan's stated 47 — FluentDiagnostics.cs contains CVJG0001..CVJG0049 minus CVJG0034. Reconciled Unshipped.md to 48 rows per source of truth.
- [Phase 17-core-generator-type-renames]: FluentModelFactory.CreateFluentFactoryCompilationUnit method name intentionally preserved — Phase 18 owns FluentModelFactory rename, method names follow then to avoid conflicts
- [Phase 17-02]: GetFluentFactoryMetadata and GetFluentFactoryDefaults method identifiers on FluentRootMetadataReader intentionally preserved — method-name renames deferred to Phase 18 alongside CreateFluentFactoryCompilationUnit cleanup
- [Phase 17-03]: FluentFactoryMethodDeclaration renamed to StepTerminalMethodDeclaration (Step prefix mirrors FluentStepMethodDeclaration sibling, Terminal qualifier distinguishes terminal from transition methods)
- [Phase 17-03]: FluentRootFactoryMethodDeclaration renamed to RootTerminalMethodDeclaration (Root prefix mirrors RootTypeDeclaration, symmetric with StepTerminalMethodDeclaration)
- [Phase 17-03]: Phase 17 zero-hit gate uses word-boundary regex refinement to exclude deferred method-name substrings; Phase 18 obligation to run plain ROADMAP alternation after method renames complete
- [Phase 17]: COMPLETE — all five requirements NAME-01..04, FILE-01 satisfied; zero legacy FluentFactory* type names in src/Converj.Generator/
- [Phase 18-01]: Builder suffix replaces Factory suffix for internal helper types to eliminate GoF/public-API naming ambiguity
- [Phase 18-01]: Phase 17 deferred method renames (CreateFluentFactoryCompilationUnit, GetFluentFactoryMetadata, GetFluentFactoryDefaults) fully discharged using FluentRoot* vocabulary
- [Phase 19-02]: Terminal method names preserved unchanged — derived from TARGET type name, not root name; renaming root *Factory to *Builder does not affect terminal method identifiers
- [Phase 19-03]: GoF PropositionFactory* types in test string literals are intentional retentions — eight types confirmed as GoF design pattern, not legacy fluent root vocabulary

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-04-12T18:46:19.879Z
Stopped at: Completed 19-03-PLAN.md
Resume file: None
