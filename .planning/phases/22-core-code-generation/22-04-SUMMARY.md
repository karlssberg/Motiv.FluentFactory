---
phase: 22-core-code-generation
plan: 04
subsystem: testing
tags: [roslyn, source-generator, accumulator, fluent-builder, xunit]

# Dependency graph
requires:
  - phase: 22-core-code-generation/22-03
    provides: AccumulatorStepDeclaration syntax generation and CompilationUnit dispatch
  - phase: 22-core-code-generation/22-02
    provides: AccumulatorFluentStep, AccumulatorMethod, AccumulatorTransitionMethod domain models
  - phase: 22-core-code-generation/22-01
    provides: AccumulatorStepGenerationTests stub class and CSharpSourceGeneratorVerifier infrastructure
provides:
  - FluentModelBuilder wired to exclude collection params from trie and create AccumulatorFluentStep
  - AccumulatorTransitionMethod placed on last regular trie step for mixed regular+collection targets
  - FluentStepBuilder.GetDescendentFluentSteps excludes AccumulatorMethod from traversal
  - ResolveTargetTypeReturn in FluentMethodSelector excludes AccumulatorMethod from step-chain walk
  - AccumulatorStepGenerationTests: 13 real [Fact] tests pinning GEN-01..GEN-06 plus multi-param and mixed-param scenarios
affects:
  - 22-05 (next phase, backward compat)
  - any future phase touching FluentModelBuilder or FluentMethodSelector step-chain traversal

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Exclude self-returning AccumulatorMethod from any step-chain traversal (GetDescendentFluentSteps, MarkReturnsFromMethods, ResolveTargetTypeReturn)"
    - "DynamicSuffix transition method naming: Build{TypeName} to prevent collisions when multiple all-collection targets share a root"
    - "Record-replay methodology for pinning expected generated source strings in Roslyn verifier tests"

key-files:
  created:
    - src/Converj.Generator.Tests/AccumulatorStepGenerationTests.cs (13 [Fact] tests)
  modified:
    - src/Converj.Generator/FluentModelBuilder.cs (trie exclusion, AccumulatorTransitionMethod creation, step indexing, MarkReturnsFromMethods guard)
    - src/Converj.Generator/ModelBuilding/FluentStepBuilder.cs (GetDescendentFluentSteps excludes AccumulatorMethod)
    - src/Converj.Generator/ModelBuilding/FluentMethodSelector.cs (ResolveTargetTypeReturn excludes AccumulatorMethod)
    - src/Converj.Generator.Tests/CSharpSourceGeneratorVerifier.cs (global using System.Linq for ImmutableArray.ToArray())

key-decisions:
  - "AccumulatorMethod excluded from ResolveTargetTypeReturn step-chain walk to prevent infinite loop on self-returning methods"
  - "DynamicSuffix transition method name Build{TypeName} (vs Build for FixedName) to prevent CS0111 when multiple all-collection targets share same root node"
  - "global using System.Linq injected into test compilation so ImmutableArray<T>.ToArray() resolves via LINQ extension in netstandard2.0 reference assemblies"
  - "Terminal placed exclusively on AccumulatorFluentStep, not on any preceding regular step"

patterns-established:
  - "Exclude self-returning methods from all step-chain traversal: GetDescendentFluentSteps, MarkReturnsFromMethods, ResolveTargetTypeReturn each now filter out AccumulatorMethod"
  - "Test capture methodology: run with wrong expected string, copy + lines from verifier diff, replace version number with $$VERSION$$ placeholder"

requirements-completed: [GEN-01, GEN-02, GEN-03, GEN-04, GEN-05, GEN-06]

# Metrics
duration: 90min
completed: 2026-04-14
---

# Phase 22 Plan 04: Pipeline Wiring and Source-Gen Tests Summary

**AccumulatorFluentStep wired into FluentModelBuilder with 13 Roslyn source-gen tests pinning GEN-01..GEN-06 output; fixed infinite loop in ResolveTargetTypeReturn for mixed regular+collection constructor fixtures**

## Performance

- **Duration:** ~90 min
- **Started:** 2026-04-14T17:00:00Z
- **Completed:** 2026-04-14T18:30:00Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- FluentModelBuilder now excludes `[FluentCollectionMethod]` params from the parameter trie and routes targets with collection params through `BuildAccumulatorTransition`, producing an `AccumulatorTransitionMethod` on the last regular step and an `AccumulatorFluentStep` with `AddX` methods and terminal
- 13 `[Fact]` tests in `AccumulatorStepGenerationTests` pin generated output for GEN-01..GEN-06 (AddX self-return, 6 collection type conversions, readonly struct shape, ImmutableArray.Empty init, element-type parameter, AggressiveInlining), plus multi-collection and mixed regular+collection scenarios
- Fixed infinite loop in `FluentMethodSelector.ResolveTargetTypeReturn` where walking a step chain through an `AccumulatorTransitionMethod` would follow `AccumulatorMethod.Return == step` forever; fixed by excluding `AccumulatorMethod` from the chain walk filter

## Task Commits

1. **Task 1: Wire AccumulatorFluentStep into FluentModelBuilder pipeline** - `6210aa8` (feat)
2. **Task 2: Add GEN-01..GEN-06 source-gen tests + fix ResolveTargetTypeReturn loop** - `129ae90` (feat)

## Files Created/Modified

- `src/Converj.Generator/FluentModelBuilder.cs` - Trie exclusion filter, `BuildAccumulatorTransition`, `BuildAccumulatorValueStorage`, `BuildTerminalFieldsInParameterOrder`, step indexing for AccumulatorFluentStep, visited-set guard in `MarkReturnsFromMethods`
- `src/Converj.Generator/ModelBuilding/FluentStepBuilder.cs` - `GetDescendentFluentSteps` excludes `AccumulatorMethod` from child traversal
- `src/Converj.Generator/ModelBuilding/FluentMethodSelector.cs` - `ResolveTargetTypeReturn` excludes `AccumulatorMethod` from step-chain walk (bug fix)
- `src/Converj.Generator.Tests/CSharpSourceGeneratorVerifier.cs` - Added `global using System.Linq` to test compilation for ImmutableArray.ToArray()
- `src/Converj.Generator.Tests/AccumulatorStepGenerationTests.cs` - Replaced placeholder with 13 pinned source-gen tests

## Decisions Made

- AccumulatorMethod excluded from `ResolveTargetTypeReturn` step-chain walk: the method returns the same step it lives on, so any traversal that follows method.Return would re-enter the same step forever. All three traversal functions now share the same exclusion pattern.
- `Build{TypeName}` naming for DynamicSuffix targets: prevents CS0111 duplicate method error when multiple all-collection constructors share a root node (e.g., `BuildTargetA()` and `BuildTargetB()`).
- Terminal placed exclusively on AccumulatorFluentStep: the last regular step only has the transition method; no terminal on the regular step.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed infinite loop in FluentMethodSelector.ResolveTargetTypeReturn**
- **Found during:** Task 2 (Collection_plus_regular_parameter test capture)
- **Issue:** When a constructor has both regular and collection params, the `ResolveTargetTypeReturn` method walks the step chain looking for a `TargetTypeReturn`. It excluded `TerminalMethod` and `OptionalFluentMethod` but not `AccumulatorMethod`. Since `AccumulatorMethod.Return` is the same `AccumulatorFluentStep` instance, the while loop cycled back to the same step indefinitely, hanging the test host process.
- **Fix:** Added `and not AccumulatorMethod` to the `FirstOrDefault` filter inside `ResolveTargetTypeReturn`.
- **Files modified:** `src/Converj.Generator/ModelBuilding/FluentMethodSelector.cs`
- **Verification:** `Collection_plus_regular_parameter` test now fails (with "wrong expected string") instead of hanging; after pinning the expected string it passes. Full suite: 440 tests pass.
- **Committed in:** `129ae90` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - Bug)
**Impact on plan:** Fix was necessary for correctness of the mixed regular+collection scenario. No scope creep.

## Issues Encountered

- The test host crashed/hung with "Test host process crashed" for the `Collection_plus_regular_parameter` test before the bug was identified. The TRX showed 0 tests executed with "Starting: Converj.Generator.Tests", indicating a hang during test execution rather than a compile-time error. Root cause traced to `ResolveTargetTypeReturn` infinite loop via code analysis.

## Next Phase Readiness

- All 440 generator tests pass; 53 integration tests pass; total 493 green
- GEN-01..GEN-06 requirements are satisfied and pinned with snapshot tests
- BACK-02 byte-identical snapshots confirmed unchanged
- Phase 22-05 (backward compat verification) can proceed

---
*Phase: 22-core-code-generation*
*Completed: 2026-04-14*
