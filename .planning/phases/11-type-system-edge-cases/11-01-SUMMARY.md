---
phase: 11-type-system-edge-cases
plan: "01"
subsystem: tests
tags: [edge-cases, nullable, generics, arrays, type-system]
dependency_graph:
  requires: []
  provides: [TYPE-01-tests, TYPE-03-tests, TYPE-04-tests, TYPE-05-tests]
  affects: [FluentFactoryGenerator]
tech_stack:
  added: []
  patterns: [CSharpSourceGeneratorVerifier, VerifyCS.Test, raw-string-literal-tests]
key_files:
  created:
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorNullableTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorGenericArrayTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorPartiallyOpenGenericTests.cs
    - src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorDeepNestedGenericTests.cs
  modified: []
decisions:
  - "Tests assert DESIRED output, not current behavior — failing tests document discovered shortcomings"
  - "All 11 new edge case tests fail as expected, confirming generator has not yet been updated for these scenarios"
  - "Nullable T? with class constraint: generator emits `in T` (drops ?) and duplicates `where T : class`"
  - "Nullable mixed parameters: generator does not emit nullable annotation in field/parameter types"
  - "No existing regressions: 18 existing tests continue to pass"
metrics:
  duration: "2m"
  completed_date: "2026-03-14"
  tasks_completed: 2
  files_created: 4
  files_modified: 0
---

# Phase 11 Plan 01: Type System Edge Cases — Nullable and Generic Array/Partial/Deep Nested Tests Summary

**One-liner:** Four test files documenting desired generator behavior for nullable annotations, generic arrays, partially open generics, and deep nested non-delegate generics.

## What Was Built

Created four new test files in `src/Motiv.FluentFactory.Generator.Tests/` each following the established `CSharpSourceGeneratorVerifier<FluentFactoryGenerator>` test pattern with raw string literal input/expected output and XML doc comments per method:

| File | Requirement | Tests | Current Status |
|------|-------------|-------|----------------|
| `FluentFactoryGeneratorNullableTests.cs` | TYPE-01 | 3 | All failing (expected) |
| `FluentFactoryGeneratorGenericArrayTests.cs` | TYPE-03 | 3 | All failing (expected) |
| `FluentFactoryGeneratorPartiallyOpenGenericTests.cs` | TYPE-04 | 3 | All failing (expected) |
| `FluentFactoryGeneratorDeepNestedGenericTests.cs` | TYPE-05 | 2 | All failing (expected) |

## Discovered Shortcomings (Failing Tests)

All 11 new tests fail — this is the intended outcome, as tests document DESIRED behavior vs. current behavior.

### Nullable Tests (TYPE-01)

**Test 1 (single nullable reference):** Failing — generator likely drops the `?` from `string?` in generated parameter/field types.

**Test 2 (mixed nullable/non-nullable):** Failing — generator does not preserve `?` annotations on `string?` fields and parameters in generated step structs.

**Test 3 (nullable generic T? with class constraint):** Failing with diff:
- Expected: `in T? value` with `where T : class`
- Actual: `in T value` with `where T : class where T : class` (drops `?`, duplicates constraint)

### Generic Array Tests (TYPE-03)

All 3 tests failing — generator likely does not correctly handle `IArrayTypeSymbol` in the display format used for generated method signatures, despite `GenericAnalysis.cs` having recursive array handling.

### Partially Open Generic Tests (TYPE-04)

All 3 tests failing — generator likely does not correctly isolate open type arguments from partially closed generics like `Dictionary<string, T>`.

### Deep Nested Non-Delegate Tests (TYPE-05)

Both tests failing — generator does not produce correct multi-level `global::` qualified output for non-delegate deep nesting like `Dictionary<string, List<KeyValuePair<T, U>>>`.

## Deviations from Plan

None — plan executed exactly as written. All 11 tests failing was anticipated and is the milestone success indicator.

## Existing Tests: No Regressions

All 18 pre-existing tests (NonGeneric, Generic, NestedGeneric) continue to pass.

## Self-Check: PASSED

Files exist:
- FOUND: src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorNullableTests.cs
- FOUND: src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorGenericArrayTests.cs
- FOUND: src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorPartiallyOpenGenericTests.cs
- FOUND: src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorDeepNestedGenericTests.cs

Commits exist:
- FOUND: 7e17c45 (Task 1 — nullable + generic array tests)
- FOUND: 444cc96 (Task 2 — partially open + deep nested tests)
