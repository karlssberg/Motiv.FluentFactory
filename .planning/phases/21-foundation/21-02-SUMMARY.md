---
phase: 21-foundation
plan: 02
subsystem: attributes-diagnostics
tags: [attribute, diagnostics, analyzer-releases, typname-constant]
dependency_graph:
  requires: [21-01]
  provides: [FluentCollectionMethodAttribute, TypeName.FluentCollectionMethodAttribute, CVJG0050, CVJG0051, CVJG0052]
  affects: [21-04, 21-05]
tech_stack:
  added: []
  patterns: [attribute-class-mirroring-FluentMethodAttribute, DiagnosticDescriptor-additive-registration]
key_files:
  created:
    - src/Converj.Attributes/FluentCollectionMethodAttribute.cs
  modified:
    - src/Converj.Generator/Domain/TypeName.cs
    - src/Converj.Generator/Diagnostics/FluentDiagnostics.cs
    - src/Converj.Generator/AnalyzerReleases.Unshipped.md
    - src/Converj.Generator.Tests/FluentCollectionMethodAttributeTests.cs
decisions:
  - "AttributeUsage restricted to Parameter only — AttributeTargets.Parameter (not Parameter | Property as FluentMethodAttribute uses)"
  - "MinItems property defined with public getter+setter defaulting to 0 — present for v2.2 API stability, enforcement deferred to Phase 24"
  - "Diagnostic descriptors added as pure declarations — no analyzer wiring until Plans 04 and 05"
  - "[Rule 3 - Blocking] CollectionParameterInfo.cs (untracked Plan 03 file) used positional record syntax causing IsExternalInit error on netstandard2.0 — converted to sealed class with primary constructor, following existing FluentParameterMember pattern"
metrics:
  duration_minutes: 6
  tasks_completed: 2
  files_created: 1
  files_modified: 4
  completed_date: "2026-04-14"
requirements: [ATTR-01, ATTR-03, NAME-03, NAME-04]
---

# Phase 21 Plan 02: Attribute Surface and Diagnostic Descriptors Summary

**One-liner:** Public `[FluentCollectionMethod]` attribute class on `AttributeTargets.Parameter` with two constructors and `MinItems` property, plus three new CVJG005x diagnostic descriptors registered in `FluentDiagnostics` and `AnalyzerReleases.Unshipped.md`.

## Attribute Public Shape

**`src/Converj.Attributes/FluentCollectionMethodAttribute.cs`**

```
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter)]
public class FluentCollectionMethodAttribute : Attribute
{
    public FluentCollectionMethodAttribute()                  // parameterless
    public FluentCollectionMethodAttribute(string methodName) // explicit accumulator name
    public string? MethodName { get; }                        // null = derive from parameter name
    public int MinItems { get; set; } = 0;                    // Phase 24 enforcement, not Phase 21
}
```

## TypeName Constant

**`src/Converj.Generator/Domain/TypeName.cs`**

```csharp
public static readonly TypeName FluentCollectionMethodAttribute =
    new(GeneratorAttributesNamespace + "FluentCollectionMethodAttribute");
```

## Diagnostic Descriptors

**`src/Converj.Generator/Diagnostics/FluentDiagnostics.cs`**

| Field Name | ID | Severity | Requirement |
|---|---|---|---|
| `NonCollectionFluentCollectionMethod` | CVJG0050 | Error | ATTR-03 |
| `UnsingularizableParameterName` | CVJG0051 | Error | NAME-03 |
| `AccumulatorMethodNameCollision` | CVJG0052 | Error | NAME-04 |

All three: `category: "Converj"`, `isEnabledByDefault: true`. Not yet emitted — wiring deferred to Plans 04 and 05.

## AnalyzerReleases.Unshipped.md Rows Added

```
CVJG0050 | Converj | Error | FluentCollectionMethod on non-collection parameter
CVJG0051 | Converj | Error | Cannot derive accumulator method name
CVJG0052 | Converj | Error | Accumulator method name collision
```

## MinItems Note

`MinItems` is parsed and retained in `CollectionParameterInfo.MinItems` for Phase 24 enforcement. It is NOT validated or enforced by the Phase 21 generator — its presence establishes the v2.2 public API surface (REQUIREMENTS.md ATTR-01).

## Test Results

- 4 new `FluentCollectionMethodAttributeTests` facts pass (replacing Plan 01 placeholder)
- Full suite: 404 `Converj.Generator.Tests` + 53 `Converj.Tests` = 457 passing, 0 failing

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fixed CollectionParameterInfo.cs record syntax blocking build**
- **Found during:** Task 2 (overall build verification)
- **Issue:** `CollectionParameterInfo.cs` (untracked, created by Plan 03 execution out-of-order) used `sealed record` with positional parameters, generating `init`-only auto-properties that require `System.Runtime.CompilerServices.IsExternalInit` — missing on netstandard2.0
- **Fix:** Converted to `sealed class` with primary constructor syntax (`(params...)`) and explicit `{ get; } = param;` properties, exactly matching `FluentParameterMember` pattern in the codebase
- **Files modified:** `src/Converj.Generator/TargetAnalysis/CollectionParameterInfo.cs`
- **Commit:** `ec03e73` (committed by Plan 03's auto-commit during linter phase, already on branch)

## Self-Check: PASSED

- `src/Converj.Attributes/FluentCollectionMethodAttribute.cs` — FOUND
- `src/Converj.Generator.Tests/FluentCollectionMethodAttributeTests.cs` — FOUND
- Task 1 commit `6561774` — FOUND
- Task 2 commit `9796a06` — FOUND
- `FluentDiagnostics.cs` contains 3 new descriptors — VERIFIED
- `TypeName.cs` contains `FluentCollectionMethodAttribute` constant — VERIFIED
- `AnalyzerReleases.Unshipped.md` contains CVJG0050/0051/0052 rows — VERIFIED
