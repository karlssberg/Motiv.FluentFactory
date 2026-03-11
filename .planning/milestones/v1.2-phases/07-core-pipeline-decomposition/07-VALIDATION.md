---
phase: 7
slug: core-pipeline-decomposition
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-10
---

# Phase 7 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (via Microsoft.NET.Test.Sdk + xunit packages) |
| **Config file** | Motiv.FluentFactory.Generator.Tests.csproj |
| **Quick run command** | `dotnet build src/Motiv.FluentFactory.Generator -v q` |
| **Full suite command** | `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build src/Motiv.FluentFactory.Generator -v q`
- **After every plan wave:** Run `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 07-01-xx | 01 | 1 | DECOMP-02 | integration (existing) | `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` | ✅ existing | ⬜ pending |
| 07-02-xx | 02 | 2 | DECOMP-03 | integration (existing) | `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` | ✅ existing | ⬜ pending |
| 07-03-xx | 03 | 3 | DECOMP-01 | integration (existing) | `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` | ✅ existing | ⬜ pending |
| all | all | all | XCUT-01, XCUT-02 | integration (existing) | `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` | ✅ existing | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. The tests are end-to-end source generator verification tests that compare generated .g.cs output against expected snapshots. Any behavioral change will be caught by test failure.

---

## Manual-Only Verifications

All phase behaviors have automated verification.

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
