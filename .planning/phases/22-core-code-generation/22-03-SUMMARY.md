---
phase: 22-core-code-generation
plan: 03
subsystem: code-generation
tags: [roslyn, source-generator, csharp, immutablearray, syntax-generation, accumulator]

requires:
  - phase: 22-02
    provides: AccumulatorFluentStep model with CollectionParameters/ForwardedTargetParameters/ValueStorage

provides:
  - AccumulatorStepDeclaration.Create — readonly struct syntax for AccumulatorFluentStep
  - AccumulatorCollectionConversionExpression.ConvertToDeclaredType — six-case ImmutableArray<T> to declared type conversion
  - CompilationUnit.CreateTypeDeclarations dispatches AccumulatorFluentStep to AccumulatorStepDeclaration.Create

affects:
  - 22-04 (FluentModelBuilder wiring — exercises AccumulatorStepDeclaration via end-to-end pipeline)
  - 22-05 (backward compat tests — no regression on existing steps)

tech-stack:
  added: []
  patterns:
    - AccumulatorStepDeclaration follows screaming-architecture pattern alongside FluentStepDeclaration and ExistingPartialTypeStepDeclaration
    - Two-constructor pattern (entry ctor for .Empty init, private copy ctor for AddX return paths)
    - ReadOnlyKeyword added unconditionally to accumulator step modifiers (no hasMutableOptionalMethods branching)

key-files:
  created:
    - src/Converj.Generator/SyntaxGeneration/AccumulatorCollectionConversionExpression.cs
    - src/Converj.Generator/SyntaxGeneration/AccumulatorStepDeclaration.cs
  modified:
    - src/Converj.Generator/SyntaxGeneration/CompilationUnit.cs

key-decisions:
  - "ReadOnlyKeyword emitted unconditionally by AccumulatorStepDeclaration (Pitfall 5 — no hasMutableOptionalMethods branching)"
  - "Entry constructor initialises all ImmutableArray<T> fields to .Empty, not default (Pitfall 1/GEN-04)"
  - "_elementType parameter on ConvertToDeclaredType kept but renamed _elementType (unused, reserved for Phase 23)"
  - "Terminal method implemented inline in AccumulatorStepDeclaration rather than reusing StepTerminalMethodDeclaration — accumulator needs AccumulatorCollectionConversionExpression dispatch not present in existing emitter"
  - "Collection field identification in terminal arg building uses SourceName-keyed dictionary lookup over step.CollectionParameters"

patterns-established:
  - "AccumulatorStepDeclaration: orchestrator root method + private sub-methods per struct member type"
  - "AccumulatorCollectionConversionExpression: OriginalDefinition.SpecialType switch + IArrayTypeSymbol rank check (no AllInterfaces walk)"
  - "CompilationUnit switch arm added for AccumulatorFluentStep before NotSupportedException default"

requirements-completed: [GEN-02, GEN-04, GEN-06]

duration: 4min
completed: 2026-04-14
---

# Phase 22 Plan 03: Accumulator Step Syntax Generation Summary

**AccumulatorStepDeclaration emits readonly struct with two-constructor pattern and six-case ImmutableArray<T> conversion table; CompilationUnit wired to dispatch AccumulatorFluentStep**

## Performance

- **Duration:** 4 min
- **Started:** 2026-04-14T16:24:05Z
- **Completed:** 2026-04-14T16:27:30Z
- **Tasks:** 2
- **Files modified:** 3 (2 created, 1 modified)

## Accomplishments

- `AccumulatorCollectionConversionExpression.ConvertToDeclaredType` implements all six GEN-02 conversion cases: `T[]`/`ICollection<T>`/`IList<T>` → `.ToArray()`; `IEnumerable<T>`/`IReadOnlyCollection<T>`/`IReadOnlyList<T>` → identity pass-through
- `AccumulatorStepDeclaration.Create` emits an unconditionally `readonly struct` with `[GeneratedCode]`, two constructors (entry initialises to `.Empty`, private copy used by `AddX`), per-collection `ImmutableArray<T>` fields, `AddX` methods with `[AggressiveInlining]`, and a terminal method using the conversion helper
- `CompilationUnit.CreateTypeDeclarations` now exhaustively dispatches all three step kinds: `ExistingTypeFluentStep`, `RegularFluentStep`, and `AccumulatorFluentStep`
- Test suite delta: zero — 428 passing, 0 failing (no `AccumulatorFluentStep` instances produced by pipeline yet, dispatch arm not yet exercised)

## Task Commits

1. **Task 1: AccumulatorCollectionConversionExpression** - `544f1d4` (feat)
2. **Task 2: AccumulatorStepDeclaration + CompilationUnit dispatch** - `9195be6` (feat)

## Files Created/Modified

- `src/Converj.Generator/SyntaxGeneration/AccumulatorCollectionConversionExpression.cs` — Six-case conversion table from `ImmutableArray<T>` to declared collection type
- `src/Converj.Generator/SyntaxGeneration/AccumulatorStepDeclaration.cs` — Full struct syntax emission for `AccumulatorFluentStep`
- `src/Converj.Generator/SyntaxGeneration/CompilationUnit.cs` — Added `AccumulatorFluentStep` case to first-pass switch and final yield switch

## Decisions Made

- **ReadOnlyKeyword unconditional**: `AccumulatorStepDeclaration` does not check for mutable optional methods before emitting `readonly` — GEN-06 is unconditional and accumulator steps never host `OptionalFluentMethod` or `OptionalPropertyFluentMethod` (those live on preceding regular steps)
- **Dedicated terminal method implementation**: Rather than reusing `StepTerminalMethodDeclaration`, the terminal method is implemented inline in `AccumulatorStepDeclaration`. The accumulator terminal needs to call `AccumulatorCollectionConversionExpression.ConvertToDeclaredType` for collection arguments — the existing emitter has no injection point for this
- **`_elementType` parameter preserved**: The `ConvertToDeclaredType` signature retains the element type parameter (renamed to `_elementType` to suppress unused-parameter warning) because Phase 23 bulk-set composability will need it

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## Next Phase Readiness

- Plan 22-04 can now construct a hand-crafted `AccumulatorFluentStep` via the pipeline and call `AccumulatorStepDeclaration.Create` to verify generated output matches GEN-01 through GEN-06 assertions
- `CompilationUnit.CreateTypeDeclarations` switch is exhaustive — no further changes needed for the dispatch path

---
*Phase: 22-core-code-generation*
*Completed: 2026-04-14*
