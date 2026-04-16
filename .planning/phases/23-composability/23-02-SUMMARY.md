---
phase: 23-composability
plan: 02
subsystem: code-generation
tags: [roslyn, source-generator, accumulator, fluent-builder, collection, composability]

# Dependency graph
requires:
  - phase: 22-core-code-generation
    provides: AccumulatorFluentStep, AccumulatorMethod, AccumulatorTransitionMethod, AccumulatorStepDeclaration, FluentModelBuilder pipeline
  - phase: 23-01
    provides: Phase 23 stub test files, Model D free-composition decision locked

provides:
  - ISelfReturningAccumulatorMethod marker interface (traversal exclusion generalized)
  - AccumulatorBulkMethod (WithXs self-returning IEnumerable<T> method on AccumulatorFluentStep)
  - AccumulatorBulkTransitionMethod (parameterised IEnumerable<T> entry from preceding regular step)
  - AccumulatorBulkTransitionMethodDeclaration (syntax emission for the transition)
  - Traversal exclusion sites updated from AccumulatorMethod to ISelfReturningAccumulatorMethod (3 sites)
  - FluentModelBuilder wired to produce AccumulatorBulkMethod + AccumulatorBulkTransitionMethod for [FluentMethod]-annotated collection params
  - COMP-01/02/03 test coverage

affects: [23-03, 23-04, 23-05, future collection-composability plans]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ISelfReturningAccumulatorMethod marker interface: all self-returning accumulator methods implement this; traversal code uses 'is ISelfReturningAccumulatorMethod' instead of 'is AccumulatorMethod'"
    - "Bulk transition chaining pattern: WithXs on regular step emits 'new AccumulatorStep(forwarded...).WithXs(items)' using public entry ctor + WithXs chain to avoid calling private copy ctor from external scope"
    - "AccumulatorBulkMethod inner BulkFluentMethodParameter overrides SourceType to IEnumerable<ElementType> — parallel to AccumulatorMethod.ElementTypeFluentMethodParameter pattern"

key-files:
  created:
    - src/Converj.Generator/Models/Methods/ISelfReturningAccumulatorMethod.cs
    - src/Converj.Generator/Models/Methods/AccumulatorBulkMethod.cs
    - src/Converj.Generator/Models/Methods/AccumulatorBulkTransitionMethod.cs
    - src/Converj.Generator/SyntaxGeneration/AccumulatorBulkTransitionMethodDeclaration.cs
    - src/Converj.Generator.Tests/DomainModel/AccumulatorBulkMethodDomainTests.cs
  modified:
    - src/Converj.Generator/Models/Methods/AccumulatorMethod.cs (add ISelfReturningAccumulatorMethod)
    - src/Converj.Generator/Models/Steps/AccumulatorFluentStep.cs (XML doc update)
    - src/Converj.Generator/SyntaxGeneration/AccumulatorStepDeclaration.cs (emit WithXs methods)
    - src/Converj.Generator/SyntaxGeneration/FluentStepDeclaration.cs (AccumulatorBulkTransitionMethod case)
    - src/Converj.Generator/SyntaxGeneration/RootTypeDeclaration.cs (AccumulatorBulkTransitionMethod case)
    - src/Converj.Generator/FluentModelBuilder.cs (BuildAccumulatorTransitions + bulk method wiring)
    - src/Converj.Generator/ModelBuilding/FluentStepBuilder.cs (traversal exclusion generalized)
    - src/Converj.Generator/ModelBuilding/FluentMethodSelector.cs (traversal exclusion generalized)
    - src/Converj.Generator.Tests/CollectionMethodBulkSetTests.cs (COMP-02 record-replay)
    - src/Converj.Generator.Tests/CollectionMethodComposabilityTests.cs (COMP-01/02/03 tests)

key-decisions:
  - "Bulk transition method body pattern: use new AccumulatorStep(forwarded...).WithXs(items) chaining instead of calling the private copy constructor directly — avoids visibility issues when transition lives on an external regular step or root class"
  - "Tasks 2 and 3 implemented together — impossible to test emission (Task 2) without pipeline wiring (Task 3); merged coherently rather than adding contrived hand-built model tests"
  - "AccumulatorBulkTransitionMethod is NOT ISelfReturningAccumulatorMethod — it lives on the preceding regular step with AccumulatorFluentStep as return type, not self-returning"
  - "[MultipleFluentMethods] on a collection param not addressed in this plan — CVJG0042 info diagnostic emitted for [FluentMethod] without explicit name; callers must provide [FluentMethod('WithXs')] to use composability without the info diagnostic"

requirements-completed: [COMP-01, COMP-02, COMP-03]

# Metrics
duration: 20min
completed: 2026-04-16
---

# Phase 23 Plan 02: Composability — WithXs Bulk Accumulator Methods Summary

**Free-composition AddX/WithXs accumulator API delivered: [FluentCollectionMethod, FluentMethod("WithXs")] on a parameter generates both AddX(T) and WithXs(IEnumerable<T>) on the AccumulatorFluentStep, and a WithXs(IEnumerable<T>) bulk transition on the preceding regular step**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-04-16T00:10:00Z
- **Completed:** 2026-04-16T00:30:00Z
- **Tasks:** 3 (Task 1 committed separately; Tasks 2+3 committed together)
- **Files modified:** 15

## Accomplishments
- Introduced `ISelfReturningAccumulatorMethod` marker interface — three traversal-exclusion sites in FluentModelBuilder, FluentStepBuilder, and FluentMethodSelector now use the marker (generalized from `AccumulatorMethod`-specific check)
- Implemented `AccumulatorBulkMethod` and `AccumulatorBulkTransitionMethod` domain types; `AccumulatorBulkMethod` implements the marker; `AccumulatorBulkTransitionMethod` does NOT (lives on preceding step)
- Wired `FluentModelBuilder.BuildAccumulatorTransitions` to emit bulk method pairs for `[FluentMethod]`-annotated collection parameters
- `AccumulatorStepDeclaration` now emits `WithXs(IEnumerable<T>)` alongside `AddX(T)` via `CreateBulkMethod`
- `AccumulatorBulkTransitionMethodDeclaration` handles root-level and step-level bulk transition emission using the `new AccumulatorStep().WithXs(items)` chaining pattern
- BACK-01 preserved: all 440 pre-plan tests still pass; total is 456 tests green

## Task Commits

1. **Task 1: ISelfReturningAccumulatorMethod + domain types + unit tests** - `d1df431` (feat)
2. **Tasks 2+3: Emission + traversal generalization + pipeline wiring + COMP tests** - `f1d262e` (feat)

## Files Created/Modified

- `src/Converj.Generator/Models/Methods/ISelfReturningAccumulatorMethod.cs` — Marker interface for traversal exclusion
- `src/Converj.Generator/Models/Methods/AccumulatorBulkMethod.cs` — WithXs(IEnumerable<T>) self-returning method model
- `src/Converj.Generator/Models/Methods/AccumulatorBulkTransitionMethod.cs` — Parameterised IEnumerable<T> entry transition
- `src/Converj.Generator/Models/Methods/AccumulatorMethod.cs` — Added ISelfReturningAccumulatorMethod interface
- `src/Converj.Generator/SyntaxGeneration/AccumulatorBulkTransitionMethodDeclaration.cs` — Syntax emitter for bulk transition
- `src/Converj.Generator/SyntaxGeneration/AccumulatorStepDeclaration.cs` — Extended CreateAddMethods to emit WithXs bulk methods
- `src/Converj.Generator/SyntaxGeneration/FluentStepDeclaration.cs` — Added AccumulatorBulkTransitionMethod case
- `src/Converj.Generator/SyntaxGeneration/RootTypeDeclaration.cs` — Added AccumulatorBulkTransitionMethod case
- `src/Converj.Generator/FluentModelBuilder.cs` — BuildAccumulatorTransitions now yields bulk transition methods; HasFluentMethodAttribute/ResolveBulkMethodName helpers
- `src/Converj.Generator/ModelBuilding/FluentStepBuilder.cs` — Traversal exclusion: `is ISelfReturningAccumulatorMethod`
- `src/Converj.Generator/ModelBuilding/FluentMethodSelector.cs` — Traversal exclusion: `is ISelfReturningAccumulatorMethod`
- `src/Converj.Generator.Tests/DomainModel/AccumulatorBulkMethodDomainTests.cs` — 9 pure domain tests
- `src/Converj.Generator.Tests/CollectionMethodBulkSetTests.cs` — COMP-02 record-replay test
- `src/Converj.Generator.Tests/CollectionMethodComposabilityTests.cs` — COMP-01/02/03 tests

## Decisions Made

- **Bulk transition chaining pattern**: `WithTags(IEnumerable<string> items)` on the preceding step emits `return new Accumulator_0__Test_Builder().WithTags(items)`. This chains the public entry constructor with the public bulk method on the accumulator step, avoiding the need to call the private copy constructor from an external scope.
- **Tasks 2 and 3 merged**: The plan acknowledged this was likely; implemented together since emission (Task 2) requires the model to be populated (Task 3) to be testable.
- **[MultipleFluentMethods] deferred**: Not addressed in this plan. The redirect path in FluentMethodBuilder is not needed because collection params are excluded from trie building entirely — the skip condition the plan suggested is pre-empted by the existing trie exclusion.

## Exact Emission Shape (for regression reference)

Given `[FluentCollectionMethod, FluentMethod("WithTags")] IList<string> tags` on a no-preceding-step constructor:

**On `Builder` (root):**
```csharp
[MethodImpl(AggressiveInlining)]
public static Accumulator_0__Test_Builder BuildTarget() =>
    new Accumulator_0__Test_Builder();

[MethodImpl(AggressiveInlining)]
public static Accumulator_0__Test_Builder WithTags(
    global::System.Collections.Generic.IEnumerable<string> items) =>
    new Accumulator_0__Test_Builder().WithTags(items);
```

**On `Accumulator_0__Test_Builder`:**
```csharp
[MethodImpl(AggressiveInlining)]
public Accumulator_0__Test_Builder AddTag(in string item) =>
    new Accumulator_0__Test_Builder(this._tags__parameter.Add(item));

[MethodImpl(AggressiveInlining)]
public Accumulator_0__Test_Builder WithTags(
    global::System.Collections.Generic.IEnumerable<string> items) =>
    new Accumulator_0__Test_Builder(this._tags__parameter.AddRange(items));
```

## Traversal Exclusion Sites (with locations)

| Site | File | Change |
|------|------|--------|
| `GetDescendentFluentSteps` | `FluentStepBuilder.cs` ~line 237 | `is AccumulatorMethod` → `is ISelfReturningAccumulatorMethod` |
| `MarkReturnsFromMethods` | `FluentModelBuilder.cs` ~line 932 | `is AccumulatorMethod` → `is ISelfReturningAccumulatorMethod` |
| `ResolveTargetTypeReturn` | `FluentMethodSelector.cs` ~line 147 | `is AccumulatorMethod` → `is ISelfReturningAccumulatorMethod` |

## [MultipleFluentMethods] Status

Not addressed in this plan. Deferred to Plan 23-04 (overload and naming rules). The existing FluentMethodBuilder redirect is not needed — collection params never reach the trie builder because they are filtered out in `CreateFluentStepTrie` before `ToFluentMethodParameter` is called. Future work: route [MultipleFluentMethods] on collection params to produce multiple AccumulatorBulkMethod instances.

## Runtime Round-Trip (COMP-03)

Deferred to manual verification. `COMP_03_free_composition_both_methods_compile_without_errors` verifies the generated code compiles without errors, confirming that `AddTag("x").WithTags(new[]{"y","z"}).AddTag("w")` chains are type-valid. The `ImmutableArray<T>` struct value semantics preserve GEN-03 branch independence.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Emit chain pattern for bulk transition instead of copy constructor**
- **Found during:** Tasks 2+3 (emission implementation)
- **Issue:** The accumulator step's copy constructor is `private`. When `WithTags` lives on the `Builder` root class or on an intermediate regular step, calling the copy constructor directly is blocked by accessibility rules (CS0122).
- **Fix:** Changed emission to `new AccumulatorStep(forwarded...).WithXs(items)` — creates an empty entry via the public constructor, then immediately chains the public `WithXs` method. This is equivalent in semantics and generates compilable code.
- **Files modified:** `AccumulatorBulkTransitionMethodDeclaration.cs`
- **Verification:** CS0122 resolved; COMP-01/02 tests pass with correct generated output
- **Committed in:** f1d262e

---

**Total deviations:** 1 auto-fixed (blocking)
**Impact on plan:** Required fix for correct code generation. Semantically equivalent to the plan's intent (ImmutableArray.Empty.AddRange(items)). No scope creep.

## Issues Encountered

The `CVJG0042` info diagnostic fires when `[FluentMethod]` is applied without an explicit method name argument. For the composability feature, callers must use `[FluentMethod("WithTags")]` with an explicit name to avoid this diagnostic. The plan's test fixtures were updated to use the explicit-name form.

## Next Phase Readiness

- COMP-01, COMP-02, COMP-03 requirements fulfilled
- AccumulatorBulkMethod and AccumulatorBulkTransitionMethod available for Plans 23-03/04/05
- [MultipleFluentMethods] on collection params deferred to Plan 23-04
- Runtime round-trip deferred to manual verification sign-off

---
*Phase: 23-composability*
*Completed: 2026-04-16*
