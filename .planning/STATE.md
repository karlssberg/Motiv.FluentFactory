---
gsd_state_version: 1.0
milestone: v2.1
milestone_name: Naming Alignment Refactor
status: Phase 16 in progress (plan 16-01 complete; plan 16-02 complete; plan 16-03 pending)
stopped_at: Completed 16-01-PLAN.md
last_updated: "2026-04-12T00:54:00Z"
last_activity: 2026-04-12 — Plan 16-01 executed (FluentDiagnostics.cs vocabulary alignment, CVJG0031 defect fixed)
progress:
  total_phases: 5
  completed_phases: 0
  total_plans: 3
  completed_plans: 2
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-11)

**Core value:** Developers write constructor parameters once and get a complete, type-safe fluent builder API generated automatically
**Current focus:** v2.1 — Naming Alignment Refactor

## Current Position

Phase: 16-diagnostic-alignment
Plan: 16-01 complete; 16-02 complete; 16-03 pending
Status: Phase 16 in progress
Last activity: 2026-04-12 — Plan 16-01 executed (FluentDiagnostics.cs vocabulary alignment, CVJG0031 defect fixed inline)

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

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-04-12T00:54:00Z
Stopped at: Completed 16-01-PLAN.md (FluentDiagnostics vocabulary alignment)
Resume file: .planning/phases/16-diagnostic-alignment/16-03-PLAN.md
