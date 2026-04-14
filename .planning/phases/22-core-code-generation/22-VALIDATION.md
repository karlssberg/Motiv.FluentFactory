---
phase: 22
slug: core-code-generation
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-14
---

# Phase 22 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (per `Directory.Packages.props`) |
| **Config file** | none — auto-discovered |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~AccumulatorStepGenerationTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30 seconds full suite, ~5 seconds filtered |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~AccumulatorStepGenerationTests"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd:verify-work`:** Full suite must be green (427+ tests passing, 0 failing)
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 22-01-01 | 01 | 1 | Wave 0 stubs | stub | n/a (file creation) | ❌ W0 | ⬜ pending |
| 22-04-02 | 04 | 1 | GEN-01 | unit (generated-source) | `dotnet test --filter "FullyQualifiedName~AccumulatorStepGenerationTests"` | ✅ | ✅ green |
| 22-04-02 | 04 | 1 | GEN-02 | unit (generated-source, ×6 collection types) | `dotnet test --filter "FullyQualifiedName~AccumulatorStepGenerationTests"` | ✅ | ✅ green |
| 22-04-02 | 04 | 1 | GEN-03 | unit (generated-source, struct shape) | `dotnet test --filter "FullyQualifiedName~AccumulatorStepGenerationTests"` | ✅ | ✅ green |
| 22-04-02 | 04 | 1 | GEN-04 | unit (generated-source, .Empty init) | `dotnet test --filter "FullyQualifiedName~AccumulatorStepGenerationTests"` | ✅ | ✅ green |
| 22-04-02 | 04 | 1 | GEN-05 | unit (generated-source, element-type param) | `dotnet test --filter "FullyQualifiedName~AccumulatorStepGenerationTests"` | ✅ | ✅ green |
| 22-04-02 | 04 | 1 | GEN-06 | unit (generated-source, readonly + AggressiveInlining) | `dotnet test --filter "FullyQualifiedName~AccumulatorStepGenerationTests"` | ✅ | ✅ green |
| 22-05-01 | 05 | TBD | BACK-02 | snapshot (byte-identical) | `dotnet test --filter "FullyQualifiedName~BackwardCompatibilitySnapshotTests"` | ✅ (Phase 21) | ✅ green |
| 22-04-01 | 04 | 1 | BACK-01 | regression (full suite) | `dotnet test` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Note on GEN-03:** Verified structurally via struct shape (readonly struct + ImmutableArray-based accumulator guarantees branch independence at language level). Two independent chain variables in the test fixture each call different `Add` sequences; test asserts struct definition, not runtime behavior (Roslyn verifier tests assert generated source, not execution).

Task IDs will be finalized by planner and kept in sync with PLAN.md frontmatter.

---

## Wave 0 Requirements

- [ ] `src/Converj.Generator.Tests/AccumulatorStepGenerationTests.cs` — placeholder `[Fact]` stub class covering GEN-01 through GEN-06 (follows Phase 21 stub pattern: VerifyCS alias + one `Placeholder` [Fact])
- [ ] Reuse existing `src/Converj.Generator.Tests/BackwardCompatibilitySnapshotTests.cs` — already present from Phase 21, no Wave 0 gap

---

## Manual-Only Verifications

*All phase behaviors have automated verification via Roslyn source-generator test framework.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** complete
