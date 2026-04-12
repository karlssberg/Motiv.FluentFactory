---
phase: 16-diagnostic-alignment
plan: 01
subsystem: diagnostics
tags: [refactor, vocabulary, naming-alignment, diagnostics]
one-liner: Aligned FluentDiagnostics.cs vocabulary to Converj/FluentRoot across all 47 descriptors and fixed the CVJG0031 [FluentFactory] defect inline
dependency-graph:
  requires: []
  provides:
    - "FluentDiagnostics with Category=\"Converj\" and FluentRoot-consistent titles/messages/XML docs"
    - "FluentParameterOnStaticRoot descriptor identifier (replaces FluentParameterOnStaticFactory)"
  affects:
    - "src/Converj.Generator/Diagnostics/FluentDiagnostics.cs"
    - "src/Converj.Generator/AnalyzerReleases.Unshipped.md (Category column sync only — full rewrite in plan 16-03)"
tech-stack:
  added: []
  patterns:
    - "Full-47 per-descriptor human audit (not grep-driven) to catch drift like CVJG0031 where the literal 'Factory' sits inside a bracketed attribute name"
key-files:
  created: []
  modified:
    - "src/Converj.Generator/Diagnostics/FluentDiagnostics.cs (all 47 descriptors audited; Category constant flipped; 7 descriptors had title/message/XML/identifier edits)"
    - "src/Converj.Generator/AnalyzerReleases.Unshipped.md (Rule 3 unblock: 18 existing entries' Category column flipped from FluentFactory to Converj to satisfy RS2001)"
decisions:
  - "Rule 3 scope expansion: updated AnalyzerReleases.Unshipped.md Category column to unblock RS2001. Full 47-row rewrite remains plan 16-03's responsibility."
  - "All descriptor identifier audits for fluent-root-sense Factory drift: only FluentParameterOnStaticFactory (CVJG0026) required rename. Remaining 46 identifiers had no drift (verified by reading every name)."
metrics:
  duration_minutes: 4
  tasks_completed: 2
  files_modified: 2
  completed_date: "2026-04-12"
---

# Phase 16 Plan 01: FluentDiagnostics Vocabulary Alignment Summary

## Objective

Audit all 47 descriptors in `FluentDiagnostics.cs`, flip the `Category` constant to `"Converj"`, rewrite every title/messageFormat/XML doc referring to the fluent root with legacy "Factory" vocabulary, rename descriptor variable identifiers carrying the same drift, and fix the CVJG0031 `[FluentFactory]` defect inline. Single-file pass closes DIAG-01, DIAG-02, and most of DIAG-04.

## Outcome

All seven required edits applied across all 47 audited descriptors. Build green with zero warnings, all 415 tests pass (362 generator tests + 53 runtime tests), zero test assertion changes. CVJG0031 defect fixed inline. All four phase-level verification cross-checks pass (`"FluentFactory"`, `MFFG`, `FluentConstructor`, `[FluentFactory]` all return zero hits in `FluentDiagnostics.cs`).

## Tasks Executed

| # | Task                                                                                 | Commit    | Files Modified                                                                                                  |
| - | ------------------------------------------------------------------------------------ | --------- | --------------------------------------------------------------------------------------------------------------- |
| 1 | Flip Category constant and audit all 47 descriptors for title/message/XML drift      | `ac67f84` | `FluentDiagnostics.cs`, `AnalyzerReleases.Unshipped.md` (Rule 3 unblock)                                        |
| 2 | Full-suite build and test gate                                                       | —         | (verification gate; no modifications)                                                                           |

## Changes Applied

### Category Constant (DIAG-01)

- Line 10: `private const string Category = "FluentFactory";` → `private const string Category = "Converj";`
- All 47 descriptors inherit the new value via the shared `Category` variable reference.

### Class-level XML Summary

- Line 6: "Diagnostic descriptors for the fluent factory source generator." → "Diagnostic descriptors for the Converj source generator."

### Title / Message / XML Doc Edits

| Rule      | Field                | Before                                                                                                                                                     | After                                                                                                                                                          |
| --------- | -------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| CVJG0011  | messageFormat        | "... Fluent factory generation requires value-type parameters. This constructor will be skipped."                                                          | "... The Converj generator requires value-type parameters. This constructor will be skipped."                                                                  |
| CVJG0012  | messageFormat        | "... cannot be used by the fluent factory. Only public and internal constructors are supported."                                                           | "... cannot be used by the Converj generator. Only public and internal constructors are supported."                                                            |
| CVJG0013  | XML / title / message | "factory root types" / "Factory type missing partial modifier" / "Factory type '{0}' must be declared as partial ..."                                      | "FluentRoot types" / "FluentRoot type missing partial modifier" / "FluentRoot type '{0}' must be declared as partial ..."                                      |
| CVJG0014  | XML / title / message | "less accessible than the factory type" / "Inaccessible parameter type in fluent factory" / "... less accessible than the factory type '{3}' ..."         | "less accessible than the FluentRoot type" / "Inaccessible parameter type in FluentRoot" / "... less accessible than the FluentRoot type '{3}' ..."           |
| CVJG0015  | XML / title / message | "factory accessibility exceeding target type accessibility" / "Factory accessibility exceeds target type" / "Factory '{0}' is {1} ... the generated factory may expose ..." | "FluentRoot accessibility exceeding target type accessibility" / "FluentRoot accessibility exceeds target type" / "FluentRoot '{0}' is {1} ... the generated FluentRoot may expose ..." |
| CVJG0026  | XML / title / message | "static factory type" / "FluentParameter on static factory type" / "... requires a non-static factory type."                                               | "static FluentRoot type" / "FluentParameter on static FluentRoot type" / "... requires a non-static FluentRoot type."                                          |
| CVJG0031  | messageFormat (defect fix) | "... Set AllowPartialParameterOverlap = true on **[FluentFactory]** to allow this."                                                                    | "... Set AllowPartialParameterOverlap = true on **[FluentRoot]** to allow this."                                                                               |

### Descriptor Variable Identifier Renames (DIAG-04 partial)

| Before (CVJG0026 field name)     | After                      | External Call Sites |
| -------------------------------- | -------------------------- | ------------------- |
| `FluentParameterOnStaticFactory` | `FluentParameterOnStaticRoot` | 0 (pre-read confirmed; post-rename `dotnet build` confirmed) |

### Descriptors Audited and Left Untouched (with rationale)

All 46 non-renamed descriptors were read top to bottom. Untouched because their titles/messages/XML docs refer to C# language concepts (constructor, parameter, method, property), fluent-attribute family concepts that are not drift vocabulary (`FluentMethod`, `FluentParameter`, `FluentStorage`, `FluentRoot`, `FluentTarget`, `[This]`), or `TerminalMethod`/`TerminalVerb` surface area. Notable examples:

- **CVJG0001 `UnreachableConstructor`** — "fluent constructor" here describes a C# ctor that is unreachable as a fluent target. Descriptive English, not drift. Untouched per vocabulary policy rule 4.
- **CVJG0012 `InaccessibleConstructor`** — same. "Inaccessible constructor" describes a C# ctor with insufficient accessibility.
- **CVJG0002–0006 FluentMethodTemplate family** — references to fluent-method-templates and fluent-constructor-parameters are framework concepts, not fluent-root-sense "Factory" drift.
- **CVJG0016, CVJG0022, CVJG0023, CVJG0043 (ambiguity family)** — talk about "fluent method chain", which is the generator's output vocabulary, not drift.
- **CVJG0025–CVJG0032 (FluentParameter family)** — already use `[FluentRoot]` / `[FluentParameter]` vocabulary correctly (except CVJG0031 defect and CVJG0026 identifier, both fixed).
- **CVJG0037 `MultipleTargetsWithBuilderNone`** — uses `[FluentTarget]` / `TerminalMethod.None` correctly.
- **CVJG0044 `InstanceMethodTarget`**, **CVJG0046–0048 `[This]` family**, **CVJG0049 `RootMustBeStaticForExtensionTargets`** — all use current v2.0 vocabulary.

## Drift Discovered Beyond Known Hits

The plan's `<interfaces>` block listed seven known drift points (lines 6, 10, 155, 156, 177, 178, 302, 367). **No additional drift was discovered beyond the known list** — the full-47 audit confirmed the pre-read was complete for this file. Every descriptor either matched the known hits or was correctly untouched per vocabulary policy.

However, one **scope discovery** during Task 1: the `AnalyzerReleases.Unshipped.md` file needed to be touched to unblock the build (see Deviations below). This was not listed in the plan's `files_modified` but is owned by plan 16-03 for its full rewrite.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 — Blocking] AnalyzerReleases.Unshipped.md Category column sync**

- **Found during:** Task 1 build verification
- **Issue:** Flipping `Category = "Converj"` caused Roslyn analyzer rule **RS2001** to flag all 18 currently-tracked unshipped rules as "Rule has a changed Category or Severity from the last release." Build failed with 18 errors. Plan 16-01's `dotnet build /warnaserror` verify step could not pass until the Unshipped release file reflects the new category.
- **Why in scope:** The RS2001 errors were DIRECTLY caused by this plan's Category change. Rule 3 (auto-fix blocking issues) applies.
- **Fix:** Updated the `Category` column of the 18 existing entries from `FluentFactory` to `Converj`. **Did not rewrite the file**, did not add new entries, did not touch other columns. The full 47-row rewrite (with fresh titles and complete coverage) remains plan 16-03's responsibility per the context document.
- **Files modified:** `src/Converj.Generator/AnalyzerReleases.Unshipped.md`
- **Commit:** `ac67f84` (combined with Task 1)
- **Impact on downstream plans:** Plan 16-03 will rewrite this file in full to 47 rows with post-rename titles. My minimal edit is a subset and will be overwritten cleanly.

### Authentication Gates

None.

### Defects Fixed (Discovered During Alignment)

**CVJG0031 `[FluentFactory]` → `[FluentRoot]`** — The message text instructed users to "Set AllowPartialParameterOverlap = true on `[FluentFactory]` to allow this." The attribute was renamed to `[FluentRoot]` in v2.0; this one message was missed during the v2.0 rename. The plan anticipated this and called it the canonical "why grep-only isn't sufficient" example. Fixed inline.

## Verification Results

### Task 1 Verification (`dotnet build src/Converj.Generator/Converj.Generator.csproj -p:TreatWarningsAsErrors=true`)

```
Converj.Attributes -> C:\Dev\Converj\src\Converj.Attributes\bin\Debug\netstandard2.0\Converj.Attributes.dll
Converj.Generator -> C:\Dev\Converj\src\Converj.Generator\bin\Debug\netstandard2.0\Converj.Generator.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Task 2 Verification (Full solution build + test)

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Test suite:

```
Passed!  - Failed:     0, Passed:    53, Skipped:     0, Total:    53 — Converj.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:   362, Skipped:     0, Total:   362 — Converj.Generator.Tests.dll (net10.0)
```

Grand total: **415 passed, 0 failed, 0 skipped.** Pre-read prediction held: zero test assertions matched on any of the touched title/message literals.

### Phase-level Cross-checks

| Check                                                                                    | Result     |
| ---------------------------------------------------------------------------------------- | ---------- |
| DIAG-01: `"FluentFactory"` substring in `FluentDiagnostics.cs`                          | 0 hits     |
| DIAG-02: Any fluent-root title/message containing "Factory" after full re-read          | 0 hits     |
| DIAG-04 partial: `MFFG` or `FluentConstructor` substring in `FluentDiagnostics.cs`       | 0 hits     |
| CVJG0031 defect: `[FluentFactory]` substring in `FluentDiagnostics.cs`                   | 0 hits     |
| Behavior preservation: `dotnet build -p:TreatWarningsAsErrors=true && dotnet test`       | both green |

## Requirements Closed

- **DIAG-01** — Category constant renamed; all 47 descriptors pick it up.
- **DIAG-02** — All 47 descriptors audited; titles/messages/XML docs aligned to FluentRoot / Converj generator vocabulary.
- **DIAG-04 (partial)** — Descriptor identifier `FluentParameterOnStaticFactory` renamed. Remaining DIAG-04 scope (call sites in `UnreachableConstructorAnalyzer.cs`, `IgnoredMultiMethodWarningFactory.cs`, `DiagnosticList.cs`) is plan 16-02.

## Self-Check: PASSED

- [x] `src/Converj.Generator/Diagnostics/FluentDiagnostics.cs` exists and contains `private const string Category = "Converj";`
- [x] `src/Converj.Generator/AnalyzerReleases.Unshipped.md` exists and uses `Converj` in the Category column
- [x] Commit `ac67f84` exists on `main` branch (`refactor(16-01): align FluentDiagnostics vocabulary to Converj/FluentRoot`)
- [x] Zero `"FluentFactory"` / `[FluentFactory]` / `MFFG` / `FluentConstructor` substrings remain in `FluentDiagnostics.cs`
- [x] `FluentParameterOnStaticFactory` identifier removed; `FluentParameterOnStaticRoot` present
- [x] Full build succeeds with zero warnings, zero errors
- [x] All 415 tests pass (0 failed, 0 skipped)
