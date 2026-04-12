---
phase: 16-diagnostic-alignment
verified: 2026-04-12T00:00:00Z
status: passed
score: 5/5 success criteria verified
requirements_verified:
  - DIAG-01
  - DIAG-02
  - DIAG-03
  - DIAG-04
---

# Phase 16: Diagnostic Alignment Verification Report

**Phase Goal:** All diagnostic descriptors, messages, and release notes use Converj vocabulary; no diagnostic-producing code contains `MFFG`, `FluentFactory` (as category), or `FluentConstructor` string literals.

**Verified:** 2026-04-12
**Status:** passed
**Re-verification:** No (initial verification)
**Scope:** Phase 16 requirement DIAG-01..04, verified via observable truths derived from ROADMAP.md Phase 16 success criteria.

## Goal Achievement

### Observable Truths (from ROADMAP.md Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Every descriptor in FluentDiagnostics.cs uses `Category = "Converj"` — grep for old `"FluentFactory"` literal returns zero hits | VERIFIED | `grep "FluentFactory"` in FluentDiagnostics.cs: 0 hits. Line 10: `private const string Category = "Converj";`. All 48 descriptors inherit via `category: Category`. |
| 2 | Diagnostic titles/messages referring to the fluent root say "FluentRoot" instead of "Factory"; C# constructor / GoF references untouched | VERIFIED | Grep for `title:` or `messageFormat:` containing `[Ff]actory` in FluentDiagnostics.cs: 0 hits. Sample titles confirmed: CVJG0013 "FluentRoot type missing partial modifier", CVJG0014 "Inaccessible parameter type in FluentRoot", CVJG0015 "FluentRoot accessibility exceeds target type", CVJG0026 "FluentParameter on static FluentRoot type". "Unreachable fluent constructor" (CVJG0001) correctly retained as C# ctor reference per vocabulary policy rule 4. |
| 3 | AnalyzerReleases.Unshipped.md reflects new category string and is internally consistent with descriptors | VERIFIED | File contains 48 rows, one per descriptor. All 48 rows match regex `^CVJG\d{4} \| Converj \| (Error\|Warning\|Info\|Hidden) \|`. Notes column matches descriptor titles verbatim (sampled CVJG0001, 0013, 0015, 0026, 0049). Numeric ordering verified. Gap at CVJG0034 preserved as absence. |
| 4 | `git grep -nE "MFFG\|\"FluentFactory\"\|FluentConstructor" -- src/Converj.Generator/Diagnostics/` returns zero hits | VERIFIED | Grep returned 0 matches across all files in `src/Converj.Generator/Diagnostics/`. Hits in `FluentFactoryGenerator.cs` and `Extensions/FluentAttributeExtensions.cs` are OUTSIDE the DIAG-04 scope (deferred to Phase 17/20). |
| 5 | `dotnet build` zero warnings + `dotnet test` all tests pass | VERIFIED | `dotnet build -p:TreatWarningsAsErrors=true`: 0 warnings, 0 errors, all 6 projects built successfully. `dotnet test --no-build`: 415/415 passed (53 Converj.Tests + 362 Converj.Generator.Tests), 0 failed, 0 skipped. |

**Score:** 5/5 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/Converj.Generator/Diagnostics/FluentDiagnostics.cs` | Category=Converj, all titles/messages/XML drift-free, FluentParameterOnStaticRoot identifier | VERIFIED | Line 10 sets `Category = "Converj"`. Line 6 XML summary says "Diagnostic descriptors for the Converj source generator." Line 302 defines `FluentParameterOnStaticRoot`. 48 descriptors total (CVJG0001–0049 minus CVJG0034). Build green. Wired: 48 descriptors × `category: Category` parameter; `FluentParameterOnStaticRoot` is the renamed identifier with zero residual external call sites. |
| `src/Converj.Generator/Diagnostics/UnreachableConstructorAnalyzer.cs` | Drift identifiers renamed to Target vocabulary | VERIFIED | `_allTargetConstructors` (line 7), `_reachedTargetConstructors` (line 8), `AddAllTargetConstructors(IEnumerable<IMethodSymbol> targetConstructors)` (line 42). Zero `FluentConstructor` substrings. All six internal usage sites reference the new identifiers. Wired: external call site `FluentModelFactory.cs:67` updated to `AddAllTargetConstructors`. |
| `src/Converj.Generator/Diagnostics/IgnoredMultiMethodWarningFactory.cs` | Content drift-free; class/file name deferred to Phase 18 | VERIFIED | Grep for drift patterns returns 0 hits in this file. Class name `IgnoredMultiMethodWarningFactory` preserved as Phase 18 (NAME-07) scope per plan. |
| `src/Converj.Generator/Diagnostics/DiagnosticList.cs` | Confirmed clean | VERIFIED | Grep for drift patterns returns 0 hits. |
| `src/Converj.Generator/AnalyzerReleases.Unshipped.md` | 48 rows, Converj category, notes verbatim, numeric order | VERIFIED | 48 rows match `^CVJG\d{4} \| Converj \|` pattern. Notes column verbatim match sampled against descriptor titles. Zero `FluentFactory` substrings. Leading comments and `### New Rules` header preserved. |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| FluentDiagnostics.Category constant | All 48 DiagnosticDescriptor fields | `category: Category` parameter | WIRED | `grep "category: Category"` in FluentDiagnostics.cs: 48 hits; all descriptors inherit the new "Converj" value through single source of truth. |
| FluentParameterOnStaticRoot descriptor | Zero external call sites expected | IDE rename propagation | WIRED | grep for `FluentParameterOnStaticFactory` returns 0 hits solution-wide; renamed identifier has single definition in FluentDiagnostics.cs line 302. |
| UnreachableConstructorAnalyzer renamed fields/methods | FluentModelFactory external caller | Roslyn IDE rename propagation | WIRED | `FluentModelFactory.cs:67` calls `AddAllTargetConstructors`. Build succeeds (confirms compiler resolved all references). |
| AnalyzerReleases.Unshipped.md Notes column | FluentDiagnostics.cs descriptor titles | Verbatim copy, one row per descriptor | WIRED | 48 rows match the 48 descriptors byte-for-byte on (id, severity, title). Roslyn release-tracking analyzer (RS2001) would fail the build if category/severity drifted — build is green, confirming consistency. |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| DIAG-01 | 16-01-PLAN | Diagnostic Category string updated to "Converj" across all 48 descriptors in FluentDiagnostics.cs | SATISFIED | Line 10: `private const string Category = "Converj";`. 0 hits for old `"FluentFactory"` category literal in entire generator project. |
| DIAG-02 | 16-01-PLAN | User-facing titles/messages no longer contain "Factory" when referring to fluent root | SATISFIED | Zero `[Ff]actory` hits in `title:` or `messageFormat:` lines of FluentDiagnostics.cs. Legitimate "fluent constructor" references (CVJG0001, CVJG0012 describing C# ctors) correctly preserved per policy. CVJG0031 `[FluentFactory]` → `[FluentRoot]` defect fixed inline. |
| DIAG-03 | 16-03-PLAN | AnalyzerReleases.Unshipped.md reflects new Category string, internally consistent with descriptors | SATISFIED | 48 rows, Converj category, notes verbatim from descriptor titles, numeric order, gap at CVJG0034 preserved. Build passes Roslyn release-tracking analyzer (RS2001) — would fail if inconsistent with shipped descriptors. |
| DIAG-04 | 16-01/16-02/16-03 PLANs | `git grep -nE "MFFG\|\"FluentFactory\"\|FluentConstructor" -- src/Converj.Generator/Diagnostics/` returns zero hits | SATISFIED | Verified at HEAD: zero hits across entire Diagnostics/ directory. Descriptor identifier renames (`FluentParameterOnStaticFactory` → `Root`) and analyzer field renames (`_allFluentConstructors` → `_allTargetConstructors`) both applied via compiler-assisted rename per BEHAV-03. |

**Orphaned requirements:** None. All four phase requirements (DIAG-01..04) are accounted for across plans 16-01, 16-02, 16-03 with full coverage.

**Out-of-scope `FluentConstructor` hits (deferred, NOT blocking Phase 16):**
- `src/Converj.Generator/FluentFactoryGenerator.cs:11,104` — XML doc comment "FluentConstructor attribute". Deferred to Phase 17 (NAME-01 renames this file) or Phase 20 (documentation alignment).
- `src/Converj.Generator/Extensions/FluentAttributeExtensions.cs:91` — Diagnostic property bag key `"FluentConstructorParameter"`. Outside the DIAG-04 grep scope (`src/Converj.Generator/Diagnostics/`). Phase 20 repo-wide grep will catch this.

Both locations are outside the DIAG-04 success-criterion scope as defined in ROADMAP.md success criterion 4 (`src/Converj.Generator/Diagnostics/`) and do not fail Phase 16.

### Anti-Patterns Found

None. No TODO/FIXME/PLACEHOLDER comments introduced. No empty implementations. No stub returns. All renames were compiler-assisted, verified by green build under `/warnaserror`.

### Human Verification Required

None. All Phase 16 success criteria are automatable (grep + build + test). Nothing requires human testing for this vocabulary/rename phase.

### Gaps Summary

No gaps. All 5 ROADMAP success criteria verified at HEAD:
1. FluentDiagnostics.cs Category constant flipped to "Converj" — all 48 descriptors inherit via shared reference.
2. All titles and messages referring to the fluent root aligned to "FluentRoot" vocabulary; legitimate C# ctor references left untouched per policy rule 4.
3. AnalyzerReleases.Unshipped.md completed from pre-existing 18-row state to full 48-row coverage, notes verbatim from descriptor titles, numeric order, Converj category everywhere.
4. Literal DIAG-04 grep pattern returns zero hits across `src/Converj.Generator/Diagnostics/`.
5. BEHAV-01/02 satisfied: `dotnet build -p:TreatWarningsAsErrors=true` is clean, all 415 tests pass (0 failed, 0 skipped).

Defect fixes recorded during phase execution (traceability):
- CVJG0031 message text was instructing users to `Set AllowPartialParameterOverlap = true on [FluentFactory]`; this was missed during v2.0 attribute rename. Fixed inline during 16-01 to reference `[FluentRoot]`.
- AnalyzerReleases.Unshipped.md was 18/48 (pre-existing defect — file drifted from descriptor source over v2.0); fixed during 16-03 to full 48-row coverage.

---

*Verified: 2026-04-12*
*Verifier: Claude (gsd-verifier)*
