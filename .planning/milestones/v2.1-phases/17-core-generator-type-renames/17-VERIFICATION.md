---
phase: 17-core-generator-type-renames
verified: 2026-04-12T02:00:00Z
status: passed
score: 5/5 requirements verified
gaps: []
---

# Phase 17: Core Generator Type Renames — Verification Report

**Phase Goal:** Rename core generator pipeline types from `FluentFactory*` vocabulary to
`FluentRoot*` / `*TerminalMethodDeclaration` vocabulary that matches the user-facing `[FluentRoot]`
attribute. Use `git mv` for all file renames so `git log --follow` preserves history.

**Verified:** 2026-04-12T02:00:00Z
**Status:** passed
**Re-verification:** No — initial verification

---

## Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | `FluentRootGenerator` class exists at `src/Converj.Generator/FluentRootGenerator.cs` and is the `[Generator]` entry point | VERIFIED | `public class FluentRootGenerator : IIncrementalGenerator` at line 14 |
| 2 | `FluentRootCompilationUnit` record exists at `src/Converj.Generator/FluentRootCompilationUnit.cs` | VERIFIED | `internal record FluentRootCompilationUnit(INamedTypeSymbol RootType)` at line 6 |
| 3 | Metadata trio `FluentRootMetadata` / `FluentRootMetadataReader` / `FluentRootDefaults` exist at their new paths | VERIFIED | All three type declarations confirmed; files at expected paths |
| 4 | `StepTerminalMethodDeclaration` and `RootTerminalMethodDeclaration` exist at their new paths | VERIFIED | `internal static class StepTerminalMethodDeclaration` and `internal static class RootTerminalMethodDeclaration` confirmed |
| 5 | Zero word-boundary hits for all old `FluentFactory*` type names in `src/Converj.Generator/` and `src/Converj.Generator.Tests/` (`.cs` files) | VERIFIED | `git grep -nP "(?<![A-Za-z])(FluentFactoryGenerator|FluentFactoryCompilationUnit|FluentFactoryMetadata|FluentFactoryDefaults|FluentFactoryMetadataReader|FluentFactoryMethodDeclaration|FluentRootFactoryMethodDeclaration)(?![A-Za-z])"` returns zero hits |
| 6 | All 7 renamed files used `git mv` — history preserved via `git log --follow` | VERIFIED | Every file shows pre-rename commits in `git log --follow` output |
| 7 | `dotnet build` succeeds with zero warnings | VERIFIED | `Build succeeded. 0 Warning(s) 0 Error(s)` |
| 8 | `dotnet test` passes all 415 tests | VERIFIED | 362 generator + 53 integration, 0 failed, 0 skipped |

**Score:** 5/5 requirements verified

---

## Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/Converj.Generator/FluentRootGenerator.cs` | `IIncrementalGenerator` entry point renamed from `FluentFactoryGenerator` | VERIFIED | Correct class declaration; `Execute` method takes `FluentRootCompilationUnit builder` |
| `src/Converj.Generator/FluentRootCompilationUnit.cs` | Top-level per-root output record renamed from `FluentFactoryCompilationUnit` | VERIFIED | Correct record declaration with all original members |
| `src/Converj.Generator/FluentRootMetadata.cs` | Per-target metadata record renamed from `FluentFactoryMetadata` | VERIFIED | `internal record FluentRootMetadata` confirmed |
| `src/Converj.Generator/TargetAnalysis/FluentRootMetadataReader.cs` | Static reader renamed from `FluentFactoryMetadataReader` | VERIFIED | `internal static class FluentRootMetadataReader` confirmed; deferred method names `GetFluentFactoryMetadata` / `GetFluentFactoryDefaults` intentionally preserved for Phase 18 |
| `src/Converj.Generator/TargetAnalysis/FluentRootDefaults.cs` | Root-level defaults renamed from `FluentFactoryDefaults` | VERIFIED | `internal sealed class FluentRootDefaults` confirmed |
| `src/Converj.Generator/SyntaxGeneration/StepTerminalMethodDeclaration.cs` | Terminal method emitter on step structs, renamed from `FluentFactoryMethodDeclaration` | VERIFIED | `internal static class StepTerminalMethodDeclaration` confirmed; class-level XML doc added |
| `src/Converj.Generator/SyntaxGeneration/RootTerminalMethodDeclaration.cs` | Terminal method emitter on root type, renamed from `FluentRootFactoryMethodDeclaration` | VERIFIED | `internal static class RootTerminalMethodDeclaration` confirmed; class-level XML doc added |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `FluentRootGenerator.Execute` | `FluentRootCompilationUnit` | second parameter type | WIRED | `FluentRootCompilationUnit builder` at line 136 of `FluentRootGenerator.cs` |
| `FluentModelFactory.CreateFluentFactoryCompilationUnit` | `FluentRootCompilationUnit` | return type + 3 constructor calls | WIRED | Return type `FluentRootCompilationUnit`; 3× `new FluentRootCompilationUnit(rootType)` confirmed |
| `FluentStepDeclaration` | `StepTerminalMethodDeclaration.Create` | `CreationMethod` switch arm | WIRED | `StepTerminalMethodDeclaration.Create(createMethod, step)` at line 27 |
| `RootTypeDeclaration.BuildRootMethodSyntax` | `RootTerminalMethodDeclaration.Create` | `{ Return: TargetTypeReturn }` switch arm | WIRED | `RootTerminalMethodDeclaration.Create(method, file.RootType)` at line 140 |
| `FluentRootMetadataReader.GetFluentFactoryMetadata` | `FluentRootMetadata` | return type `IEnumerable<FluentRootMetadata>` | WIRED | Confirmed in `FluentTargetContextFactory.cs` line 33 |
| `FluentRootMetadataReader.GetFluentFactoryDefaults` | `FluentRootDefaults` | return type `FluentRootDefaults` | WIRED | Confirmed in `FluentTargetValidator.cs` lines 74, 638 |
| `FluentTargetContext` constructor | `FluentRootMetadata` | constructor parameter type | WIRED | `FluentRootMetadata metadata` at line 18 of `FluentTargetContext.cs` |
| `GeneratedCodeAttributeSyntax` | `FluentRootGenerator` | `typeof(FluentRootGenerator).Assembly` | WIRED | Line 17 of `GeneratedCodeAttributeSyntax.cs` |
| Test verifier alias | `FluentRootGenerator` | `CSharpSourceGeneratorVerifier<Converj.Generator.FluentRootGenerator>` | WIRED | Confirmed in `MalformedAttributeTests.cs` (and all 55 test files per plan 17-01 summary) |

---

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| NAME-01 | 17-01 | `FluentFactoryGenerator` → `FluentRootGenerator` | SATISFIED | Class confirmed at `FluentRootGenerator.cs`; zero hits for old name |
| NAME-02 | 17-01 | `FluentFactoryCompilationUnit` → `FluentRootCompilationUnit` | SATISFIED | Record confirmed at `FluentRootCompilationUnit.cs`; zero hits for old name |
| NAME-03 | 17-02 | `FluentFactoryMetadata`/`Reader`/`Defaults` → `FluentRoot*` trio | SATISFIED | All three types confirmed at new paths; zero hits for old names |
| NAME-04 | 17-03 | `FluentFactoryMethodDeclaration` / `FluentRootFactoryMethodDeclaration` → `StepTerminalMethodDeclaration` / `RootTerminalMethodDeclaration` | SATISFIED | Both types confirmed; call sites updated in `FluentStepDeclaration.cs` and `RootTypeDeclaration.cs` |
| FILE-01 | 17-01, 17-02, 17-03 | All 7 renamed files used `git mv` | SATISFIED | `git log --follow` shows pre-rename history for all 7 files; each shows commits from before the Phase 17 rename commit |

No orphaned requirements — all 5 Phase 17 requirements are covered by plans and verified in the codebase.

---

## Anti-Patterns Found

None. No TODOs, FIXMEs, placeholder implementations, or stub patterns detected in the renamed files or their call sites.

One documentation comment in `MalformedAttributeTests.cs` (line 18) references the string `FluentFactoryGeneratorBugDiscoveryTests` — this is a plain-text cross-reference to another test class by name, not a type-name usage. It does not represent a stale type reference and is not actionable.

**Intentional deferrals (Phase 18, documented in summaries):**
- `FluentModelFactory.CreateFluentFactoryCompilationUnit` — method name preserved; `FluentModelFactory` itself is renamed in Phase 18
- `FluentRootMetadataReader.GetFluentFactoryMetadata` — method name preserved; rides with Phase 18 method-rename pass
- `FluentRootMetadataReader.GetFluentFactoryDefaults` — method name preserved; rides with Phase 18 method-rename pass

These are not regressions. The word-boundary grep (`(?<![A-Za-z])...(?![A-Za-z])`) explicitly excludes method-name substrings and returns zero hits.

---

## Human Verification Required

None. All Phase 17 goals are mechanically verifiable:
- Type renames are confirmed by grepping declarations
- Wiring is confirmed by grepping call sites and parameter types
- History preservation is confirmed by `git log --follow`
- Build and test results are deterministic

---

## Gaps Summary

No gaps. All 5 requirements are satisfied, all 7 files are correctly renamed with history preserved, all call sites are updated, build is clean, and all 415 tests pass.

---

_Verified: 2026-04-12T02:00:00Z_
_Verifier: Claude (gsd-verifier)_
