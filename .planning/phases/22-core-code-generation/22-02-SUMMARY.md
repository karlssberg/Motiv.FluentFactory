---
phase: 22-core-code-generation
plan: 02
subsystem: code-generation
tags: [roslyn, source-generator, fluent-builder, collection-accumulator, domain-model]

requires:
  - phase: 22-01
    provides: AccumulatorStepGenerationTests stub; 428-test baseline established

provides:
  - AccumulatorFluentStep: new IFluentStep implementation (struct-typed, Accumulator_{Index}__ naming)
  - AccumulatorMethod: new IFluentMethod with self-return and element-type MethodParameters (GEN-01, GEN-05)
  - AccumulatorTransitionMethod: new IFluentMethod bridging last regular step to AccumulatorFluentStep

affects: [22-03, 22-04]

tech-stack:
  added: []
  patterns:
    - "AccumulatorFluentStep uses Accumulator_ prefix (not Step_) to guarantee no naming collision with regular steps"
    - "AccumulatorMethod.MethodParameters uses ElementTypeFluentMethodParameter inner subclass to carry element type without modifying FluentMethodParameter factory methods"
    - "AccumulatorTransitionMethod accepts name as constructor parameter so Plan 22-04 controls the terminal verb"

key-files:
  created:
    - src/Converj.Generator/Models/Steps/AccumulatorFluentStep.cs
    - src/Converj.Generator/Models/Methods/AccumulatorMethod.cs
    - src/Converj.Generator/Models/Methods/AccumulatorTransitionMethod.cs
  modified: []

key-decisions:
  - "AccumulatorFluentStep.Name pattern: Accumulator_{Index}__{RootIdentifier} — distinct Accumulator_ prefix per RESEARCH.md Pitfall 7 to avoid collision with Step_ regular steps"
  - "GEN-05 element-type parameter: ElementTypeFluentMethodParameter private inner subclass of FluentMethodParameter overrides SourceType to CollectionParameterInfo.ElementType; avoids modifying existing FluentMethodParameter API"
  - "AccumulatorMethod.SourceParameter points to the collection IParameterSymbol (not null) for identity/tracing; MethodParameters type is the element type"

patterns-established:
  - "New IFluentStep implementations must not inherit RegularFluentStep (RESEARCH.md Pitfall 5 — non-readonly guard fires)"
  - "AccumulatorFluentStep.TypeKind = Struct enforces GEN-06 struct generation via existing FluentStepDeclaration path"

requirements-completed: [GEN-01, GEN-05, GEN-06]

duration: ~5min
completed: 2026-04-14
---

# Phase 22 Plan 02: AccumulatorFluentStep, AccumulatorMethod, AccumulatorTransitionMethod domain models

**Three new domain model classes (AccumulatorFluentStep, AccumulatorMethod, AccumulatorTransitionMethod) establishing the IFluentStep/IFluentMethod contracts for collection accumulator code generation in Phase 22**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-04-14T16:18:30Z
- **Completed:** 2026-04-14T16:21:36Z
- **Tasks:** 2
- **Files modified:** 3 new files, 0 existing files touched

## Accomplishments
- `AccumulatorFluentStep` fully implements `IFluentStep` with `Accumulator_{Index}__` naming prefix, `TypeKind.Struct`, forwarded parameter/storage fields, and `CollectionParameters` array for Plan 22-03 consumption
- `AccumulatorMethod` satisfies GEN-01 (self-return to `AccumulatorFluentStep`) and GEN-05 (`MethodParameters` typed as `CollectionParameterInfo.ElementType` via inner `ElementTypeFluentMethodParameter` subclass)
- `AccumulatorTransitionMethod` provides the bridge from last regular trie step to accumulator step, with name injected via constructor for Plan 22-04 control
- Zero regressions: `dotnet test` shows 428 passing / 0 failing (improved from the 9 failing referenced in the plan, which were resolved by Plan 22-01)

## Task Commits

1. **Task 1: Create AccumulatorFluentStep model** - `7350f84` (feat)
2. **Task 2: Create AccumulatorMethod and AccumulatorTransitionMethod models** - `e33990e` (feat)

**Plan metadata:** _(docs commit — see below)_

## Files Created/Modified
- `src/Converj.Generator/Models/Steps/AccumulatorFluentStep.cs` — New IFluentStep implementation for collection accumulation; struct-typed; carries CollectionParameters, ForwardedTargetParameters, ValueStorage
- `src/Converj.Generator/Models/Methods/AccumulatorMethod.cs` — New IFluentMethod with self-return and element-type MethodParameters; inner ElementTypeFluentMethodParameter subclass
- `src/Converj.Generator/Models/Methods/AccumulatorTransitionMethod.cs` — New IFluentMethod bridge from last regular step to AccumulatorFluentStep

## Decisions Made
- **AccumulatorFluentStep.Name** uses `Accumulator_{Index}__{RootIdentifier}` — the `Accumulator_` prefix is distinct from `Step_` to guarantee no collision per RESEARCH.md Pitfall 7.
- **GEN-05 element-type parameter**: rather than modifying `FluentMethodParameter` factory methods, a private inner `ElementTypeFluentMethodParameter` subclass overrides `SourceType` to `CollectionParameterInfo.ElementType`. The `ParameterSymbol` field still points to the collection parameter for identity tracing. This preserves zero changes to existing files.
- **AccumulatorMethod.SourceParameter** returns `CollectionParameter.Parameter` (the collection `IParameterSymbol`) rather than null, so call-site tracing and documentation generation can identify which parameter drives which `AddX` method.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed unresolvable `<see cref="AccumulatorMethod"/>` XML doc reference in AccumulatorFluentStep**
- **Found during:** Task 1 build verification
- **Issue:** XML doc on `FluentMethods` property used `<see cref="AccumulatorMethod"/>` which CS1574 rejects because `AccumulatorMethod` doesn't exist yet at Task 1 compile time.
- **Fix:** Changed to `<c>AccumulatorMethod</c>` (inline code, no cref resolution required).
- **Files modified:** `src/Converj.Generator/Models/Steps/AccumulatorFluentStep.cs`
- **Verification:** `dotnet build src/Converj.Generator/Converj.Generator.csproj` — 0 warnings, 0 errors.
- **Committed in:** `7350f84` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — XML doc cref forward reference)
**Impact on plan:** Minimal; purely cosmetic XML doc adjustment. No behavioral or interface change.

## Issues Encountered
- The plan stated the expected test baseline was "428 passing, 9 failing". The actual baseline after Plan 22-01 is 428 passing, 0 failing. This is a better outcome (no regressions), and zero pipeline impact is confirmed.

## Next Phase Readiness
- Plan 22-03 (syntax generation for accumulator steps) can immediately import `AccumulatorFluentStep`, `AccumulatorMethod`, and `AccumulatorTransitionMethod` as stable contracts.
- Plan 22-04 (pipeline wiring in `FluentModelBuilder`) can instantiate these models and call `AccumulatorFluentStep.FluentMethods.Add(...)` without reopening any model file.
- All three GEN-0x prerequisites are now structurally in place.

---
*Phase: 22-core-code-generation*
*Completed: 2026-04-14*
