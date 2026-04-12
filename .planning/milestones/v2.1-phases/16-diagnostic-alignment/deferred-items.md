# Phase 16 Deferred Items

Items discovered during plan execution that are out of scope for the plan that found them.

## Found during Plan 16-02 (2026-04-11)

### Pre-existing RS2001 analyzer release tracking errors (from Plan 16-01 work in working tree)

**Observed:** `dotnet build` fails with 18 RS2001 errors of the form:
```
error RS2001: Rule 'CVJG000X' has a changed 'Category' or 'Severity' from the last release. Either revert the update(s) in source or add a new up-to-date entry to unshipped release file.
```

**Root cause:** Plan 16-01 work in the uncommitted working tree changed `FluentDiagnostics.cs` category from `"FluentFactory"` to `"Converj"` for CVJG0001–CVJG0017 and CVJG0044. `src/Converj.Generator/AnalyzerReleases.Unshipped.md` was updated to reflect the new category, but the Roslyn release-tracking analyzer still flags the change because the **shipped** release file (`AnalyzerReleases.Shipped.md`) still lists them under the old category. The unshipped file needs entries marking these as "Changed" rules, not just overwriting the existing unshipped lines.

**Scope:** This is Plan 16-01 territory (`FluentDiagnostics.cs` is 16-01's file scope). Plan 16-02 owns only `UnreachableConstructorAnalyzer.cs`, `IgnoredMultiMethodWarningFactory.cs`, and `DiagnosticList.cs`.

**Verification that Plan 16-02 changes are clean:** With Plan 16-01's in-progress changes stashed (`git stash push -- FluentDiagnostics.cs AnalyzerReleases.Unshipped.md`), the full solution builds with **0 warnings, 0 errors**, and all **415 tests pass** (53 Converj.Tests + 362 Converj.Generator.Tests). See SUMMARY.md §Performance for the captured build/test output.

**Action:** Plan 16-01 must update `AnalyzerReleases.Unshipped.md` with proper "Changed Rules" section per the release-tracking analyzer format, not an in-place rewrite.

**RESOLUTION (2026-04-12):** Plan 16-01's parallel executor (wave 1) committed `ac67f84 refactor(16-01): align FluentDiagnostics vocabulary to Converj/FluentRoot` shortly after Plan 16-02 logged this item. The 16-01 commit correctly syncs `AnalyzerReleases.Unshipped.md` alongside the `FluentDiagnostics.cs` category change. A final `dotnet build -p:TreatWarningsAsErrors=true` + `dotnet test` on the merged wave-1 state produces 0 warnings / 0 errors / 415 tests passing. This item is closed. Kept here as an execution-history breadcrumb.
