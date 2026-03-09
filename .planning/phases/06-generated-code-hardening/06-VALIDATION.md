---
phase: 6
slug: generated-code-hardening
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-09
---

# Phase 6 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (existing project) |
| **Config file** | `src/Motiv.FluentFactory.Generator.Tests/Motiv.FluentFactory.Generator.Tests.csproj` |
| **Quick run command** | `dotnet test src/Motiv.FluentFactory.Generator.Tests -x --no-build` |
| **Full suite command** | `dotnet test src/Motiv.FluentFactory.Generator.Tests` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test src/Motiv.FluentFactory.Generator.Tests -x --no-build`
- **After every plan wave:** Run `dotnet test src/Motiv.FluentFactory.Generator.Tests`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 06-01-01 | 01 | 1 | QUAL-01 | unit (generator verifier) | `dotnet test --filter "FullyQualifiedName~NamespaceConflict"` | No — Wave 0 | ⬜ pending |
| 06-01-02 | 01 | 1 | QUAL-01 | unit (generator verifier) | `dotnet test src/Motiv.FluentFactory.Generator.Tests` | Yes — existing tests need output updates | ⬜ pending |
| 06-01-03 | 01 | 1 | QUAL-02 | unit (generator verifier) | `dotnet test --filter "FullyQualifiedName~GeneratedCode"` | No — Wave 0 | ⬜ pending |
| 06-01-04 | 01 | 1 | QUAL-02 | unit | `dotnet test --filter "FullyQualifiedName~Version"` | No — Wave 0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Namespace conflict test — source with conflicting type names (e.g., user type named `String` or `List`) proves `global::` prevents compilation errors. Uses existing `CSharpSourceGeneratorVerifier`.
- [ ] `[GeneratedCode]` presence verification test — verifies attribute appears on generated type declarations
- [ ] Version string format test — verifies version is non-empty and matches semver-like pattern

*Existing infrastructure covers test framework and verifier — only new test cases needed.*

---

## Manual-Only Verifications

*All phase behaviors have automated verification.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
