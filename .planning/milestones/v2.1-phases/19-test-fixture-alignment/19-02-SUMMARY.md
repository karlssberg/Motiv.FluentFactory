---
phase: 19-test-fixture-alignment
plan: "02"
subsystem: testing
tags: [roslyn, source-generator, fluent-builder, rename, vocabulary]

requires:
  - phase: 19-test-fixture-alignment
    provides: Plan 19-01 git mv for generator test files

provides:
  - All runtime test *Factory sample types renamed to *Builder (19 types across 19 files)
  - NestedFactoryRuntimeTests class and file renamed to NestedRootRuntimeTests
  - Zero *Factory type names remaining in src/Converj.Tests/

affects:
  - 19-test-fixture-alignment
  - 20-docs-final-verification

tech-stack:
  added: []
  patterns: []

key-files:
  created: []
  modified:
    - src/Converj.Tests/NestedRootRuntimeTests.cs
    - src/Converj.Tests/AsAttributeRuntimeTests.cs
    - src/Converj.Tests/ConstructorChainingRuntimeTests.cs
    - src/Converj.Tests/CreateMethodRuntimeTests.cs
    - src/Converj.Tests/FluentMethodRuntimeTests.cs
    - src/Converj.Tests/FluentParameterRuntimeTests.cs
    - src/Converj.Tests/FluentStorageRuntimeTests.cs
    - src/Converj.Tests/GenericArrayRuntimeTests.cs
    - src/Converj.Tests/GenericAttributeRuntimeTests.cs
    - src/Converj.Tests/GenericRuntimeTests.cs
    - src/Converj.Tests/LargeParameterCountRuntimeTests.cs
    - src/Converj.Tests/MergeRuntimeTests.cs
    - src/Converj.Tests/MethodPrefixRuntimeTests.cs
    - src/Converj.Tests/NestedGenericRuntimeTests.cs
    - src/Converj.Tests/NonGenericRuntimeTests.cs
    - src/Converj.Tests/NullableRuntimeTests.cs
    - src/Converj.Tests/OptionalParameterRuntimeTests.cs
    - src/Converj.Tests/PrimaryConstructorRuntimeTests.cs
    - src/Converj.Tests/RecordVariationRuntimeTests.cs
    - src/Converj.Tests/ReturnTypeRuntimeTests.cs

key-decisions:
  - "Kept terminal method names unchanged (e.g., CreateChainingTarget) — terminal method names derive from TARGET type name, not root name"
  - "Local variable names in test methods renamed from 'factory' to 'builder' for consistency in FluentParameterRuntimeTests"

requirements-completed: [TEST-03, TEST-04]

duration: 15min
completed: 2026-04-12
---

# Phase 19 Plan 02: Runtime Test Fixture Rename Summary

**All 19 runtime test *Factory sample types renamed to *Builder and NestedFactoryRuntimeTests class/file moved to NestedRootRuntimeTests — 53 tests passing, zero Factory references remaining**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-04-12T18:30:00Z
- **Completed:** 2026-04-12T18:45:00Z
- **Tasks:** 2
- **Files modified:** 20 (1 renamed via git mv + 19 content updates)

## Accomplishments
- NestedFactoryRuntimeTests.cs renamed to NestedRootRuntimeTests.cs via `git mv`; class and test method names updated
- All 19 `*Factory` partial class/record declarations in runtime tests renamed to `*Builder`
- All `[FluentTarget<OldName>]` attributes, usage call sites, and local variable names updated consistently
- Full build succeeds with zero warnings; all 53 runtime tests pass

## Task Commits

Each task was committed atomically:

1. **Task 1: git mv NestedFactoryRuntimeTests and rename NestedFactory sample type** - `2d5ef62` (refactor)
2. **Task 2: Bulk rename *Factory types to *Builder in all runtime test files** - `88a56e8` (refactor)

**Plan metadata:** (to be added in final commit)

## Files Created/Modified
- `src/Converj.Tests/NestedRootRuntimeTests.cs` - Renamed from NestedFactoryRuntimeTests.cs; NestedBuilder replaces NestedFactory
- `src/Converj.Tests/AsAttributeRuntimeTests.cs` - AsAliasBuilder replaces AsAliasFactory
- `src/Converj.Tests/ConstructorChainingRuntimeTests.cs` - ChainingBuilder replaces ChainingFactory
- `src/Converj.Tests/CreateMethodRuntimeTests.cs` - DynamicCreateBuilder/FixedCreateBuilder/CustomVerbBuilder/NoCreateBuilder replace *Factory
- `src/Converj.Tests/FluentMethodRuntimeTests.cs` - CustomMethodBuilder replaces CustomMethodFactory
- `src/Converj.Tests/FluentParameterRuntimeTests.cs` - ServiceBuilderForTest/FieldParameterBuilder/PropertyParameterBuilder/MultiFluentParamBuilder replace *Factory
- `src/Converj.Tests/FluentStorageRuntimeTests.cs` - StorageBuilder/ExplicitStorageBuilder/NullableStorageBuilder/TwoCtorStepBuilder replace *Factory
- `src/Converj.Tests/GenericArrayRuntimeTests.cs` - ArrayBuilder replaces ArrayFactory
- `src/Converj.Tests/GenericAttributeRuntimeTests.cs` - GenericAttrBuilder replaces GenericAttrFactory
- `src/Converj.Tests/GenericRuntimeTests.cs` - GenericBuilder/ConstrainedGenericBuilder/MultiGenericBuilder replace *Factory
- `src/Converj.Tests/LargeParameterCountRuntimeTests.cs` - LargeParamBuilder replaces LargeParamFactory
- `src/Converj.Tests/MergeRuntimeTests.cs` - MergeShapeBuilder replaces MergeShapeFactory
- `src/Converj.Tests/MethodPrefixRuntimeTests.cs` - BarePrefixBuilder/CustomPrefixBuilder replace *Factory
- `src/Converj.Tests/NestedGenericRuntimeTests.cs` - NestedGenericBuilder replaces NestedGenericFactory
- `src/Converj.Tests/NonGenericRuntimeTests.cs` - NonGenericBuilder replaces NonGenericFactory
- `src/Converj.Tests/NullableRuntimeTests.cs` - NullableBuilder replaces NullableFactory
- `src/Converj.Tests/OptionalParameterRuntimeTests.cs` - OptionalParamBuilder/AllOptionalBuilder replace *Factory
- `src/Converj.Tests/PrimaryConstructorRuntimeTests.cs` - PrimaryCtorBuilder replaces PrimaryCtorFactory
- `src/Converj.Tests/RecordVariationRuntimeTests.cs` - RecordBuilder/ClassBuilder replace *Factory
- `src/Converj.Tests/ReturnTypeRuntimeTests.cs` - AnimalBuilder/VehicleBuilder replace *Factory

## Decisions Made
- Terminal method names (e.g., `CreateChainingTarget`, `CreateArrayTarget`) were preserved unchanged — these are derived from the TARGET type name, not the root type name, so renaming the root does not affect them
- Local variable names in test methods were renamed from `factory` to `builder` for internal consistency (FluentParameterRuntimeTests)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All runtime test *Factory references eliminated; combined with Plan 19-01 generator test renames, the Converj.Tests project is fully aligned with v2.0 vocabulary
- Ready for Phase 20 final verification and docs pass

---
*Phase: 19-test-fixture-alignment*
*Completed: 2026-04-12*
