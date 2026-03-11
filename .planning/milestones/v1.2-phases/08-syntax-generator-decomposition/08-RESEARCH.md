# Phase 8: Syntax Generator Decomposition - Research

**Researched:** 2026-03-11
**Domain:** Roslyn syntax generation, code generator decomposition
**Confidence:** HIGH

## Summary

Phase 8 decomposes three syntax generation classes -- FluentStepMethodDeclaration (238 lines), FluentRootFactoryMethodDeclaration (226 lines), and FluentMethodSummaryDocXml (165 lines) -- into focused types with single responsibilities. The core extraction target is constraint-building logic that is duplicated across 4 files with an inconsistency bug (method type parameter constraints use `GlobalNamespaceStyle.Omitted` instead of `ToGlobalDisplayString()`, producing `System.X` instead of `global::System.X`).

All three classes are static, stateless syntax builders in the `Generation.SyntaxElements.Methods` namespace. The decomposition follows Phase 7's orchestrator pattern but is simpler -- no mutual recursion or Func delegate wiring needed. Extracted types will be static classes with focused responsibilities, placed in `Generation/Shared/` (for the constraint builder) or alongside their consumers (for method-specific helpers).

**Primary recommendation:** Extract `TypeParameterConstraintBuilder` first (08-01) since it removes the largest block of duplicated code and fixes the qualification bug. Then decompose FluentStepMethodDeclaration + consolidate FluentMethodSummaryDocXml (08-02), then FluentRootFactoryMethodDeclaration (08-03).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Extract a `TypeParameterConstraintBuilder` static class to `Generation/Shared/`
- Single unified method that takes `ImmutableArray<ITypeParameterSymbol>` and builds constraint clauses for all of them -- callers combine their type parameters before calling
- Constraints only -- type parameter list extraction (filtering, deduplication) stays in each consumer since the filtering logic differs between step and root contexts
- Replace constraint logic in all 4 consumer files: FluentStepMethodDeclaration, FluentRootFactoryMethodDeclaration, RootTypeDeclaration, FluentStepDeclaration
- Fix constraint qualification bug: unify on `ToGlobalDisplayString()` for all constraint types
- FluentMethodSummaryDocXml: consolidate, don't decompose -- extract duplicated local functions into shared private static methods, remove dead `GenerateCandidateConstructors`, document as appropriately sized
- FluentStepMethodDeclaration: extract all small concerns to make thin orchestrator (~50-60 lines)
- FluentRootFactoryMethodDeclaration: same approach as FluentStepMethodDeclaration
- 3 separate plans ordered by dependency: 08-01 (constraint builder), 08-02 (step method + doc xml), 08-03 (root factory method)
- Full test suite run after each plan

### Claude's Discretion
- Exact method signatures for extracted types
- Internal visibility and access modifiers
- Naming of extracted helper types
- Whether type parameter filtering and argument construction warrant separate types or can be combined per consumer

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| SYNTAX-01 | FluentStepMethodDeclaration is decomposed into focused types (type parameter handling, constraint generation, method body) | Constraint generation extracted to shared TypeParameterConstraintBuilder; type parameter filtering, argument construction, and doc preparation extractable as separate concerns |
| SYNTAX-02 | FluentRootFactoryMethodDeclaration is decomposed into focused types | Same pattern as SYNTAX-01; type parameter syntax generation and argument construction (field + method sourced) extractable |
| SYNTAX-03 | FluentMethodSummaryDocXml (165 lines) is decomposed if responsibilities can be separated | Single-concern class; consolidation (dedup local functions, remove dead code) brings to ~130 lines; document as appropriately sized |
| XCUT-01 | All existing tests continue to pass -- behavior-preserving refactor | Tests use snapshot verification of generated output; constraint bug fix will change `System.X` to `global::System.X` in method constraints, requiring test expectation updates |
| XCUT-02 | Generated .g.cs output is identical before and after refactoring | Identical except for the constraint qualification bug fix, which is an intentional correction aligned with Phase 6 decisions |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.CodeAnalysis.CSharp | (project version) | Roslyn syntax tree construction | All generated code built via SyntaxFactory |
| System.Collections.Immutable | (framework) | ImmutableArray for type parameter collections | Roslyn API convention |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `static Microsoft.CodeAnalysis.CSharp.SyntaxFactory` | N/A | Static import for fluent syntax building | Every syntax generation file uses this pattern |

### Alternatives Considered
None -- this phase does not introduce new libraries. It restructures existing code.

## Architecture Patterns

### Recommended Project Structure
```
Generation/
├── Shared/
│   ├── TypeParameterConstraintBuilder.cs    # NEW: shared constraint clause builder
│   ├── AggressiveInliningAttributeSyntax.cs # existing
│   ├── FluentStepCreationExpression.cs      # existing
│   └── TargetTypeObjectCreationExpression.cs # existing
├── SyntaxElements/
│   ├── Methods/
│   │   ├── FluentStepMethodDeclaration.cs        # SLIMMED: thin orchestrator
│   │   ├── FluentRootFactoryMethodDeclaration.cs  # SLIMMED: thin orchestrator
│   │   ├── FluentMethodSummaryDocXml.cs           # CONSOLIDATED: deduped
│   │   ├── FluentFactoryMethodDeclaration.cs      # unchanged
│   │   └── ExistingPartialTypeMethodDeclaration.cs # unchanged
│   ├── RootTypeDeclaration.cs    # UPDATED: uses TypeParameterConstraintBuilder
│   └── FluentStepDeclaration.cs  # UPDATED: uses TypeParameterConstraintBuilder
```

### Pattern 1: Static Shared Syntax Builder
**What:** A static class in `Generation/Shared/` providing a single public method for a reusable syntax-building concern.
**When to use:** When identical syntax construction logic appears in 2+ files.
**Example:**
```csharp
// Follows same pattern as AggressiveInliningAttributeSyntax.cs
namespace Motiv.FluentFactory.Generator.Generation.Shared;

internal static class TypeParameterConstraintBuilder
{
    /// <summary>
    /// Builds constraint clauses for the given type parameter symbols.
    /// All type constraints use global::-qualified names via ToGlobalDisplayString().
    /// </summary>
    public static ImmutableArray<TypeParameterConstraintClauseSyntax> Create(
        ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        // Single loop over all type parameters
        // Uses ToGlobalDisplayString() for ALL constraint types (fixes qualification bug)
    }
}
```

### Pattern 2: Thin Orchestrator (Static Method)
**What:** A public `Create` method that delegates to focused helper methods/types, assembling the final syntax node.
**When to use:** When a method has multiple distinct concerns (type parameter filtering, argument building, doc generation, constraint generation).
**Example:**
```csharp
public static MethodDeclarationSyntax Create(IFluentMethod method, ...)
{
    var arguments = StepArgumentBuilder.Create(method, knownParameters);
    var returnExpr = FluentStepCreationExpression.Create(method, arguments);
    var declaration = BuildBaseDeclaration(method, returnExpr);
    declaration = AttachTypeParameters(declaration, method, ambientTypeParameters);
    return declaration;
}
```

### Anti-Patterns to Avoid
- **Extracting types that are only used once with trivial logic:** If a helper would be 5 lines wrapping a single LINQ expression, keep it as a private method in the orchestrator.
- **Breaking the public API contract:** `FluentStepMethodDeclaration.Create()` and `FluentRootFactoryMethodDeclaration.Create()` signatures must remain identical -- callers (RootTypeDeclaration, FluentStepDeclaration) must not change.
- **Different constraint ordering between consumers:** The 4 constraint consumers currently have subtly different ordering (value/reference first vs reference/value first). The shared builder must pick one canonical order and all consumers must produce the same output.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Constraint clause building | Per-file constraint loops | `TypeParameterConstraintBuilder.Create()` | Currently duplicated 4x with inconsistency bug |
| XML doc trivia line conversion | Per-method `ConvertLine`/`ConvertLineEndings` local functions | Shared private static methods in FluentMethodSummaryDocXml | Currently duplicated in both `Create` and `CreateWithParameters` |

**Key insight:** The constraint building duplication is the primary cause of the qualification bug -- each copy evolved independently, leading to `ToGlobalDisplayString()` in target type blocks but `GlobalNamespaceStyle.Omitted` in method type blocks.

## Common Pitfalls

### Pitfall 1: Constraint Ordering Mismatch Breaking Test Snapshots
**What goes wrong:** The 4 existing implementations have subtly different constraint ordering. RootTypeDeclaration and FluentStepDeclaration put reference type before value type. FluentStepMethodDeclaration and FluentRootFactoryMethodDeclaration put value type before reference type.
**Why it happens:** Each implementation was written independently.
**How to avoid:** When unifying into TypeParameterConstraintBuilder, verify which ordering the current test snapshots expect. The method declarations (value before reference) produce the output that tests validate, so use that ordering. Run tests after extraction to confirm.
**Warning signs:** Test failures in constraint-related generic tests.

### Pitfall 2: Qualification Bug Fix Changes More Tests Than Expected
**What goes wrong:** Fixing `GlobalNamespaceStyle.Omitted` to `ToGlobalDisplayString()` changes `System.IComparable<T>` to `global::System.IComparable<T>` in method-level constraints.
**Why it happens:** Tests snapshot the full generated output including constraints.
**How to avoid:** Search all test files for `where T` patterns without `global::` prefix. Currently confirmed in FluentFactoryGeneratorGenericTests.cs lines 833, 931, 954. Update these expectations.
**Warning signs:** Tests that pass before extraction but fail after.

### Pitfall 3: Different Type Parameter Filtering Logic Per Consumer
**What goes wrong:** Trying to extract type parameter filtering into the shared constraint builder when filtering logic differs between consumers.
**Why it happens:** FluentStepMethodDeclaration filters out known constructor parameters and ambient type parameters. FluentRootFactoryMethodDeclaration filters based on root type genericity. RootTypeDeclaration uses root type parameters directly. FluentStepDeclaration has its own `GetConstraintTypeParameters` logic.
**How to avoid:** Per the locked decision, constraint building only -- type parameter filtering stays in each consumer. Each consumer assembles its `ImmutableArray<ITypeParameterSymbol>` independently, then passes it to the shared builder.
**Warning signs:** Trying to make the shared builder accept filtering parameters or context objects.

### Pitfall 4: Dead Code Removal Breaking Callers
**What goes wrong:** Removing `GenerateCandidateConstructors` from FluentMethodSummaryDocXml when it has hidden callers.
**Why it happens:** Grep may miss reflection-based or generated callers.
**How to avoid:** Verified via grep: `GenerateCandidateConstructors` (non-`TypeSeeAlsoLinks` variant) is only defined at FluentMethodSummaryDocXml.cs:55, never called from any other file. Safe to remove.
**Warning signs:** Compilation errors after removal.

### Pitfall 5: Constraint Builder Ordering Difference Between Root Type and Method Declarations
**What goes wrong:** RootTypeDeclaration puts constructor constraint LAST (after type constraints), while method declarations put constructor constraint BEFORE type constraints.
**Why it happens:** Independent implementations.
**How to avoid:** The unified builder must pick ONE ordering. Check test expectations carefully. The RootTypeDeclaration/FluentStepDeclaration order (reference, value, types, constructor) appears in struct-level `where` clauses; the method declaration order (value, reference, constructor, types) appears in method-level `where` clauses. If they currently produce different orderings and both have test coverage, the builder may need to match the most commonly tested ordering, or tests need updating.
**Warning signs:** Constraint clause syntax differs between struct and method declarations in test snapshots.

## Code Examples

### Constraint Duplication Across Files (Current State)
All 4 files contain nearly identical constraint-building loops. The core pattern:
```csharp
// This block appears in FluentStepMethodDeclaration (lines 133-219),
// FluentRootFactoryMethodDeclaration (lines 83-172),
// RootTypeDeclaration (lines 82-117),
// FluentStepDeclaration (lines 122-157)
foreach (var typeParam in typeParameters)
{
    var constraints = new List<TypeParameterConstraintSyntax>();
    if (typeParam.HasValueTypeConstraint) constraints.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));
    if (typeParam.HasReferenceTypeConstraint) constraints.Add(ClassOrStructConstraint(SyntaxKind.ClassConstraint));
    if (typeParam.HasConstructorConstraint) constraints.Add(ConstructorConstraint());
    foreach (var constraintType in typeParam.ConstraintTypes)
        constraints.Add(TypeConstraint(ParseTypeName(constraintType.ToGlobalDisplayString()))); // BUG: method blocks use Omitted
    if (constraints.Count > 0)
        constraintClauses.Add(TypeParameterConstraintClause(IdentifierName(typeParam.Name)).WithConstraints(SeparatedList(constraints)));
}
```

### The Bug (Lines 205-206 in FluentStepMethodDeclaration, Lines 158-159 in FluentRootFactoryMethodDeclaration)
```csharp
// BUG: method type parameter constraints use Omitted (no global::)
var typeName = constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
    .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
// SHOULD BE:
constraints.Add(TypeConstraint(ParseTypeName(constraintType.ToGlobalDisplayString())));
```

### Test Evidence of Bug (FluentFactoryGeneratorGenericTests.cs line 833)
```csharp
// Current test expectation (without global::):
public static global::Test.Namespace.Step_0__Test_Namespace_Factory<T> WithValue<T>(in T value)
    where T : struct, System.IComparable<T>  // <-- missing global::
// vs struct-level (with global::):
public struct Step_0__Test_Namespace_Factory<T> where T : struct, global::System.IComparable<T>
```

### Duplicated Local Functions in FluentMethodSummaryDocXml
```csharp
// These identical local functions appear in BOTH Create() and CreateWithParameters():
IEnumerable<SyntaxTrivia> ConvertLine(object? line) { ... }
IEnumerable<SyntaxTrivia> ConvertLineEndings(string line) { ... }
```

### Existing Shared Pattern to Follow (AggressiveInliningAttributeSyntax.cs)
```csharp
namespace Motiv.FluentFactory.Generator.Generation.Shared;

internal static class AggressiveInliningAttributeSyntax
{
    public static AttributeSyntax Create() =>
        Attribute(
            ParseName("global::System.Runtime.CompilerServices.MethodImpl"),
            ...);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Per-file constraint loops with inconsistent formatting | Shared TypeParameterConstraintBuilder (after this phase) | Phase 8 | Eliminates 4x duplication, fixes qualification bug |
| Duplicated ConvertLine/ConvertLineEndings local functions | Shared private static methods (after this phase) | Phase 8 | Cleaner FluentMethodSummaryDocXml |

**Deprecated/outdated:**
- `GenerateCandidateConstructors` in FluentMethodSummaryDocXml: Dead code, never called externally. Remove in 08-02.

## Open Questions

1. **Constraint ordering canonical form**
   - What we know: RootTypeDeclaration/FluentStepDeclaration order: reference, value, types, constructor. Method declarations order: value, reference, constructor, types.
   - What's unclear: Whether unifying to one order will break tests for both struct-level and method-level constraints.
   - Recommendation: Check test snapshots for both orderings. If struct-level tests exist with different ordering, the builder may need to preserve the existing test expectations. Since tests validate full generated output, match whatever the current tests expect and update only the qualification bug.

2. **Whether `SymbolExtensions.ToTypeParameterSyntax` also has dead constraint code**
   - What we know: The method at line 73-106 of SymbolExtensions.cs builds constraint syntax but discards it -- it only returns `typeParameterSyntax` without attaching constraints.
   - What's unclear: Whether this was intentional or a bug.
   - Recommendation: Note this during implementation but do not fix it in this phase unless it causes issues. It may be dead code from an earlier approach.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (via Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing) |
| Config file | Motiv.FluentFactory.Generator.Tests.csproj |
| Quick run command | `dotnet test src/Motiv.FluentFactory.Generator.Tests --no-build -x` |
| Full suite command | `dotnet test` |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SYNTAX-01 | FluentStepMethodDeclaration decomposed | integration (snapshot) | `dotnet test src/Motiv.FluentFactory.Generator.Tests` | Existing tests cover via generated output snapshots |
| SYNTAX-02 | FluentRootFactoryMethodDeclaration decomposed | integration (snapshot) | `dotnet test src/Motiv.FluentFactory.Generator.Tests` | Existing tests cover via generated output snapshots |
| SYNTAX-03 | FluentMethodSummaryDocXml consolidated | integration (snapshot) | `dotnet test src/Motiv.FluentFactory.Generator.Tests` | Existing tests cover XML doc output |
| XCUT-01 | All existing tests pass | full suite | `dotnet test` | Existing |
| XCUT-02 | Generated output identical (except bug fix) | integration (snapshot) | `dotnet test src/Motiv.FluentFactory.Generator.Tests` | Existing |

### Sampling Rate
- **Per task commit:** `dotnet test src/Motiv.FluentFactory.Generator.Tests`
- **Per wave merge:** `dotnet test` (full solution)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
None -- existing test infrastructure covers all phase requirements. The constraint qualification bug fix requires updating 3+ test expectations in FluentFactoryGeneratorGenericTests.cs (lines ~833, ~931, ~954) to use `global::System.IComparable<T>` instead of `System.IComparable<T>`.

## Sources

### Primary (HIGH confidence)
- Direct code inspection of all 4 constraint consumer files
- Direct code inspection of FluentMethodSummaryDocXml.cs (both methods)
- Direct code inspection of test files (FluentFactoryGeneratorGenericTests.cs)
- Grep verification of `GenerateCandidateConstructors` callers (dead code confirmed)
- Grep verification of `GlobalNamespaceStyle.Omitted` usage (bug confirmed in 4 locations)

### Secondary (MEDIUM confidence)
- Phase 7 patterns (orchestrator with extracted collaborators) from STATE.md

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - no new libraries, pure internal refactoring
- Architecture: HIGH - follows established Generation/Shared/ pattern visible in existing codebase
- Pitfalls: HIGH - all identified through direct code inspection and test snapshot analysis

**Research date:** 2026-03-11
**Valid until:** 2026-04-11 (stable -- internal refactoring, no external dependencies)
