# Phase 11: Type System Edge Cases - Context

**Gathered:** 2026-03-13
**Status:** Ready for planning

<domain>
## Phase Boundary

Write tests that exercise the generator under unusual type system inputs (nullable annotations, parameter modifiers, arrays of generics, partially open generics, deeply nested generics) and document behavior via passing or failing tests. Tests assert DESIRED behavior — failing tests indicate discovered shortcomings to fix before release.

</domain>

<decisions>
## Implementation Decisions

### Test Expectation Strategy
- Tests assert the DESIRED/CORRECT generated output, not current behavior
- Failing tests are expected and acceptable — they represent discovered issues
- No Skip annotations, no issue tracking files — the failing test IS the tracking
- Claude decides ambiguous expected outputs (e.g., what correct generated code looks like for edge cases)
- Each test method gets an XML doc comment (`/// <summary>`) explaining the edge case it exercises

### Test Organization
- One test file per requirement: 5 files total
  - FluentFactoryGeneratorNullableTests.cs (TYPE-01)
  - FluentFactoryGeneratorParameterModifierTests.cs (TYPE-02)
  - FluentFactoryGeneratorGenericArrayTests.cs (TYPE-03)
  - FluentFactoryGeneratorPartiallyOpenGenericTests.cs (TYPE-04)
  - FluentFactoryGeneratorDeepNestedGenericTests.cs (TYPE-05)
- Standard naming convention: `FluentFactoryGenerator___Tests` (matches existing classes)
- 2-4 focused test scenarios per file — targeted, not exhaustive

### TYPE-05 Deep Nested Generics
- Existing tests in FluentFactoryGeneratorNestedGenericTests.cs already cover 3+ level delegate nesting
- Add 1-2 COMPLEMENTARY tests for non-delegate deep nesting (e.g., `Dictionary<string, List<KeyValuePair<T,U>>>`)
- New tests go in the new FluentFactoryGeneratorDeepNestedGenericTests.cs file, not the existing file

### Parameter Modifier Semantics (TYPE-02)
- ALL parameter modifiers (`ref`, `out`, `in`, `ref readonly`) should trigger a diagnostic
- Rationale: fluent builder pattern stores values in struct fields, so any modifier semantics are lost
- Generator should SKIP the constructor entirely when it has modified parameters (not generate with modifier dropped)
- Other constructors on the same class still get generated normally
- Tests assert: diagnostic emitted AND no fluent factory generated for that constructor

### Claude's Discretion
- Specific test scenarios within each requirement (which nullable types, which generic combinations)
- Expected generated output for each test case
- Diagnostic message wording and codes for parameter modifier diagnostics

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CSharpSourceGeneratorVerifier<FluentFactoryGenerator>` (test verifier): All tests use this pattern with raw string literals for input/expected output
- `FluentDiagnostics`: Existing diagnostic descriptors — new modifier diagnostic should follow the same pattern
- `SymbolDisplayExtensions.GlobalQualifiedFormat`: Already includes `IncludeNullableReferenceTypeModifier` — nullable annotations preserved in type display strings

### Established Patterns
- Test pattern: raw string input code → expected generated output → `VerifyCS.Test` → `RunAsync()`
- Generated file naming: `{Namespace}.{FactoryTypeName}.g.cs`
- Diagnostic tests use `test.TestState.ExpectedDiagnostics.Add(...)` with descriptors from `FluentDiagnostics`
- Test verifier configures `/warnaserror:nullable` and adds global usings for attributes

### Integration Points
- `FluentStepMethodDeclaration.cs` (line 111): Currently hardcodes all params as `in` — TYPE-02 tests will expose this
- `FluentFactoryMethodDeclaration.cs` (line 68): Also hardcodes `in` for source parameters
- `GenericAnalysis.cs` (lines 32-34): Handles `IArrayTypeSymbol` recursively — TYPE-03 tests will exercise this
- `FluentParameterComparer.cs`: Uses `Type.Name` (short name) for equality — relevant context for TYPE-04

### Known Generator Behaviors
- Generator preserves nullable annotations in display strings but has zero test coverage
- Generator completely ignores RefKind on IParameterSymbol — all become `in`
- GenericAnalysis recursively processes array element types and named type arguments
- No existing handling or diagnostics for parameter modifiers

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 11-type-system-edge-cases*
*Context gathered: 2026-03-13*
