# Project Retrospective

*A living document updated after each milestone. Lessons feed forward into future planning.*

## Milestone: v1.2 — Architecture Refactoring

**Shipped:** 2026-03-11
**Phases:** 4 | **Plans:** 9

### What Was Built
- Decomposed 3 god classes into 11 focused single-responsibility types
- Extracted shared TypeParameterConstraintBuilder (fixing a constraint qualification bug along the way)
- Consolidated duplicate extension methods into 4 concern-based files
- Reorganized entire generator project to screaming architecture
- Split all files to ~150 lines with single responsibilities

### What Worked
- Pure refactoring with test safety net — 174 tests caught any regressions immediately
- Cross-cutting requirements (XCUT-01, XCUT-02) enforced discipline: every phase had to leave tests green and output identical
- Strategy pattern for ConstructorAnalyzer was clean decomposition — stateless strategies with semantic model parameter
- Thin orchestrator pattern kept method declarations readable while extracting complexity

### What Was Inefficient
- SUMMARY.md frontmatter `requirements_completed` arrays were empty across all 11 plans — procedural gap that required extra audit work to verify requirements
- Some plan targets for file sizes (80-120 lines) were too aggressive — practical results at 170-212 lines were acceptable
- Phase 6 was v1.1 but shared phase numbering with v1.2, causing minor confusion in roadmap organization

### Patterns Established
- Screaming architecture: domain types at root namespace, implementation details in subdirectories
- Concern-based extension organization with shared root namespace for cross-layer accessibility
- Thin orchestrator pattern for syntax generation classes
- Strategy pattern with first-match dispatch for constructor analysis

### Key Lessons
1. Behavior-preserving refactoring is fast when you have comprehensive test coverage — 4 phases in 2 days
2. File size targets should be guidelines (~150 lines) not hard rules — some types are naturally slightly larger
3. TypeParameterConstraintBuilder extraction revealed a pre-existing qualification bug, validating the decomposition approach

### Cost Observations
- Sessions: ~6 sessions across 2 days
- Notable: Refactoring phases execute faster than feature phases due to clear constraints and test safety net

---

## Milestone: v2.1 — Naming Alignment Refactor

**Shipped:** 2026-04-12
**Phases:** 5 | **Plans:** 12

### What Was Built
- Aligned all 48 diagnostic descriptors to `Category = "Converj"` and FluentRoot vocabulary
- Renamed 7 core generator types and 3 internal GoF helper types (FluentFactory* → FluentRoot*/Builder*)
- Renamed 3 test fixture classes and bulk-renamed `class Factory` → `class Builder` across all test source strings
- Rewrote AnalyzerReleases.Unshipped.md from 18 to 48 rows
- Final repo-wide grep verification: zero residual legacy vocabulary

### What Worked
- Low-risk → high-risk phase ordering (diagnostics → core types → helpers → tests → verification) meant each phase had a narrowing blast radius
- Deferred method-name renames from Phase 17 to Phase 18 avoided conflicts — clean handoff between phases
- Phase 20's final verification plan (BEHAV-01..04) caught nothing — every prior phase had already enforced build/test gates, proving the cross-cutting discipline worked
- `git mv` for every file rename preserved full blame/history — no broken git follow chains
- Compiler-assisted renames (build gate after each change) made text-heavy rename work reliable

### What Was Inefficient
- Many SUMMARY.md files lacked `one_liner` fields, requiring manual extraction during milestone completion
- 44 commits for a pure rename milestone is high — some phases could have batched more aggressively
- Phase 19 discovered 8 GoF `PropositionFactory*` types that needed manual exclusion documentation — could have been identified in roadmap research phase

### Patterns Established
- Word-boundary regex grep gates (e.g., `(?<![A-Za-z])FluentFactoryGenerator(?![A-Za-z])`) for validating renames without false positives from method-name substrings
- GoF pattern exclusion documentation: when legacy vocabulary is intentionally retained, document the specific types and rationale in the phase summary
- Phase-level deferred obligations: when a rename can't complete in one phase, explicitly log the deferred method names and which phase owns them

### Key Lessons
1. Pure rename milestones complete fast (2 days) because the constraint is absolute: no behavior changes, no assertion changes — every decision reduces to "does it compile and pass tests?"
2. Cross-cutting verification requirements (BEHAV-01..04) are most effective when each phase enforces them locally rather than deferring all verification to a final phase
3. Vocabulary alignment is best done as a dedicated milestone after a major rename — mixing it with feature work would have made rename commits harder to review

### Cost Observations
- Sessions: ~4 sessions in 2 days
- Notable: Rename refactoring is heavily parallelizable — independent type renames in different files can proceed simultaneously with no merge conflicts

---

## Cross-Milestone Trends

### Process Evolution

| Milestone | Phases | Plans | Key Change |
|-----------|--------|-------|------------|
| v1.0 | 5 | — | Initial release (pre-GSD) |
| v1.1 | 1 | 2 | First GSD milestone, established test patterns |
| v1.2 | 4 | 9 | Pure refactoring with cross-cutting quality constraints |
| v1.3 | 5 | 10 | Edge case stress testing, intentional tech debt documentation |
| v2.0 | — | — | Organic feature expansion, no formal phases |
| v2.1 | 5 | 12 | Pure rename refactor with grep-gate verification at every phase |

### Cumulative Quality

| Milestone | Tests | Key Metric |
|-----------|-------|------------|
| v1.0 | 174 | Initial test suite |
| v1.1 | 174 | Added namespace conflict + GeneratedCode tests |
| v1.2 | 174 | All passing, zero regressions through 4 refactoring phases |
| v1.3 | 174+ | 9 intentionally failing tests documenting known shortcomings |
| v2.0 | 415 | 241 new tests for static/extension/property/type-first features |
| v2.1 | 415 | Zero regressions, zero assertion changes through 5 rename phases |

### Top Lessons (Verified Across Milestones)

1. Comprehensive test coverage enables confident refactoring at speed (v1.2, v2.1)
2. Cross-cutting requirements (tests pass, output identical) prevent regressions across multi-phase work (v1.2, v2.1)
3. Pure refactoring milestones (no behavior changes) complete faster than feature milestones due to absolute constraints (v1.2: 2 days, v2.1: 2 days)
