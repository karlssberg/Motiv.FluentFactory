---
gsd_state_version: 1.0
milestone: v2.1
milestone_name: Naming Alignment Refactor
status: roadmap_ready
stopped_at: null
last_updated: "2026-04-11T00:00:00.000Z"
last_activity: 2026-04-11 — Roadmap created for v2.1 (Phases 16-20)
progress:
  total_phases: 5
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-11)

**Core value:** Developers write constructor parameters once and get a complete, type-safe fluent builder API generated automatically
**Current focus:** v2.1 — Naming Alignment Refactor

## Current Position

Phase: Not started (roadmap approved, awaiting plan-phase)
Plan: —
Status: Roadmap ready
Last activity: 2026-04-11 — Roadmap created for v2.1 (Phases 16-20)

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

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-04-11T00:00:00.000Z
Stopped at: Roadmap created, awaiting plan-phase 16
Resume file: None
