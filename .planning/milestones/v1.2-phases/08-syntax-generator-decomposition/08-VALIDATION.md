---
phase: 8
slug: syntax-generator-decomposition
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-11
---

# Phase 8 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (via Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing) |
| **Config file** | Motiv.FluentFactory.Generator.Tests.csproj |
| **Quick run command** | `dotnet test src/Motiv.FluentFactory.Generator.Tests --no-build -x` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test src/Motiv.FluentFactory.Generator.Tests --no-build -x`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 08-01-xx | 01 | 1 | SYNTAX-01 | integration (snapshot) | `dotnet test src/Motiv.FluentFactory.Generator.Tests` | ✅ Existing | ⬜ pending |
| 08-01-xx | 01 | 1 | SYNTAX-02 | integration (snapshot) | `dotnet test src/Motiv.FluentFactory.Generator.Tests` | ✅ Existing | ⬜ pending |
| 08-01-xx | 01 | 1 | SYNTAX-03 | integration (snapshot) | `dotnet test src/Motiv.FluentFactory.Generator.Tests` | ✅ Existing | ⬜ pending |
| 08-xx-xx | xx | x | XCUT-01 | full suite | `dotnet test` | ✅ Existing | ⬜ pending |
| 08-xx-xx | xx | x | XCUT-02 | integration (snapshot) | `dotnet test src/Motiv.FluentFactory.Generator.Tests` | ✅ Existing | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test stubs or fixtures needed.

The constraint qualification bug fix requires updating 3+ test expectations in FluentFactoryGeneratorGenericTests.cs (lines ~833, ~931, ~954) to use `global::System.IComparable<T>` instead of `System.IComparable<T>`.

---

## Manual-Only Verifications

All phase behaviors have automated verification.

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
