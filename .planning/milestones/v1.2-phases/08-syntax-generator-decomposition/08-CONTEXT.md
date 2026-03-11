# Phase 8: Syntax Generator Decomposition - Context

**Gathered:** 2026-03-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Decompose FluentStepMethodDeclaration (238 lines), FluentRootFactoryMethodDeclaration (226 lines), and FluentMethodSummaryDocXml (165 lines) into focused types. Extract shared constraint-building logic. Fix constraint qualification inconsistency. All existing tests must pass after decomposition. New types stay in their current namespace directories — folder restructuring is Phase 10.

</domain>

<decisions>
## Implementation Decisions

### Shared Constraint Builder
- Extract a `TypeParameterConstraintBuilder` static class to `Generation/Shared/`
- Single unified method that takes `ImmutableArray<ITypeParameterSymbol>` and builds constraint clauses for all of them — callers combine their type parameters before calling
- Constraints only — type parameter list extraction (filtering, deduplication) stays in each consumer since the filtering logic differs between step and root contexts
- Replace constraint logic in all 4 consumer files: FluentStepMethodDeclaration, FluentRootFactoryMethodDeclaration, RootTypeDeclaration, FluentStepDeclaration

### Constraint Qualification Bug Fix
- Target type constraints use `ToGlobalDisplayString()` (with `global::`) but method type constraints use `SymbolDisplayFormat` with `GlobalNamespaceStyle.Omitted` (no `global::`) — same inconsistency in both FluentStepMethodDeclaration and FluentRootFactoryMethodDeclaration
- Fix during extraction: unify on `ToGlobalDisplayString()` for all constraint types, aligning with Phase 6's decision that all type references use `global::`
- This is a bug fix, not a behavior change — update any affected test expectations
- If no tests currently exercise method type constraints, the fix is free

### FluentMethodSummaryDocXml (165 lines)
- Consolidate, don't decompose — single-concern class (XML doc generation) is appropriately sized
- Extract duplicated `ConvertLine`/`ConvertLineEndings` local functions into shared private static methods within the same class
- Verify `GenerateCandidateConstructors` usage — remove if dead code
- Document as "appropriately sized" per SYNTAX-03 ("documented as appropriately sized if not")
- After consolidation, expected size ~130 lines

### FluentStepMethodDeclaration Decomposition (238 lines)
- Extract all small concerns to make it a thin orchestrator (~50-60 lines):
  - Type parameter filtering → extracted type
  - Argument construction → extracted type
  - Documentation preparation → extracted type
  - Constraint generation → already handled by shared TypeParameterConstraintBuilder
- Follows Phase 7 orchestrator pattern

### FluentRootFactoryMethodDeclaration Decomposition (226 lines)
- Same approach as FluentStepMethodDeclaration:
  - Type parameter syntax generation → extracted type
  - Argument construction (method-sourced + field-sourced) → extracted type
  - Constraint generation → already handled by shared TypeParameterConstraintBuilder
- Thin orchestrator after extraction

### Execution Plan
- **3 separate plans, ordered by dependency:**
  - 08-01: Extract TypeParameterConstraintBuilder + fix qualification bug + replace in all 4 consumers + update tests
  - 08-02: Decompose FluentStepMethodDeclaration + consolidate FluentMethodSummaryDocXml (dedup local functions, remove dead code)
  - 08-03: Decompose FluentRootFactoryMethodDeclaration
- Full test suite run after each plan

### Claude's Discretion
- Exact method signatures for extracted types
- Internal visibility and access modifiers
- Naming of extracted helper types (e.g., FluentStepArgumentBuilder vs StepActivationArgumentFactory)
- Whether type parameter filtering and argument construction warrant separate types or can be combined per consumer

</decisions>

<specifics>
## Specific Ideas

- The shared TypeParameterConstraintBuilder should eliminate the two-block pattern (target types + method types) entirely — callers combine parameters and make a single call
- After constraint extraction, FluentStepMethodDeclaration and FluentRootFactoryMethodDeclaration should look structurally similar — both thin orchestrators assembling a MethodDeclarationSyntax from focused helpers
- Phase 7 established the orchestrator pattern with Func delegates for wiring — these static syntax builders may use simpler composition (method calls) since they don't have mutual recursion

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AggressiveInliningAttributeSyntax`: Already in Generation/Shared/ — used by both method declaration classes
- `FluentStepCreationExpression`: Already in Generation/Shared/ — used by FluentStepMethodDeclaration
- `TargetTypeObjectCreationExpression`: Already in Generation/Shared/ — used by FluentRootFactoryMethodDeclaration
- `FluentMethodSummaryDocXml`: Used by 4 consumers — FluentStepMethodDeclaration, FluentRootFactoryMethodDeclaration, FluentFactoryMethodDeclaration, ExistingPartialTypeMethodDeclaration

### Established Patterns
- Generation/Shared/ contains static utility classes for reusable syntax building — TypeParameterConstraintBuilder follows this pattern
- All syntax generation uses `static Microsoft.CodeAnalysis.CSharp.SyntaxFactory` for fluent syntax tree construction
- Phase 7 used orchestrator pattern with extracted collaborators — same approach here but simpler (static methods, no state)

### Integration Points
- `RootTypeDeclaration.cs` calls both `FluentRootFactoryMethodDeclaration.Create` and `FluentStepMethodDeclaration.Create` — these entry points must remain stable
- `FluentStepDeclaration.cs` calls `FluentStepMethodDeclaration.Create` — entry point must remain stable
- `FluentFactoryMethodDeclaration.cs` and `ExistingPartialTypeMethodDeclaration.cs` use `FluentMethodSummaryDocXml` — these callers are unaffected by internal consolidation

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 08-syntax-generator-decomposition*
*Context gathered: 2026-03-11*
