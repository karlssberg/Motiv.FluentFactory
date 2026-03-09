# Deferred Items - Phase 06

## Pre-existing Bugs

### 1. MergeDissimilarStepsTests - CS0246 Compiler Error in Generated Code

**Test:** `Given_class_constructors_with_different_parameters_including_multiple_fluent_methods_Should_ensure_type_converters_are_generated_with_existing_types`

**Issue:** The generator produces invalid code referencing `Step_1__TestFactory_Factory<>` (unbound generic type). The generated code at lines 19, 21, 28, 30 of `TestFactory.Factory.g.cs` references this type but it cannot be found, producing CS0246 errors. The test expects 2 diagnostics but receives 6 (2 original warnings + 4 CS0246 errors).

**Verified pre-existing:** This test was already failing BEFORE Plan 01 changes (tested by stashing Plan 01 changes and running against the original generator).

**Root cause:** Likely in the step type naming/generic parameter handling when multiple constructors from different namespaces with generic types target the same factory. The `IdentifierDisplayString()` method in `RegularFluentStep` or `ExistingTypeFluentStep` may be producing an invalid open generic reference.

**Impact:** 1 test out of 174 fails. Does not block any planned work.
