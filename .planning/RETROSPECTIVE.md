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

## Cross-Milestone Trends

### Process Evolution

| Milestone | Phases | Plans | Key Change |
|-----------|--------|-------|------------|
| v1.0 | 5 | — | Initial release (pre-GSD) |
| v1.1 | 1 | 2 | First GSD milestone, established test patterns |
| v1.2 | 4 | 9 | Pure refactoring with cross-cutting quality constraints |

### Cumulative Quality

| Milestone | Tests | Key Metric |
|-----------|-------|------------|
| v1.0 | 174 | Initial test suite |
| v1.1 | 174 | Added namespace conflict + GeneratedCode tests |
| v1.2 | 174 | All passing, zero regressions through 4 refactoring phases |

### Top Lessons (Verified Across Milestones)

1. Comprehensive test coverage enables confident refactoring at speed
2. Cross-cutting requirements (tests pass, output identical) prevent regressions across multi-phase work
