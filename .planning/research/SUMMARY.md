# Project Research Summary

**Project:** Converj v2.2 - [FluentCollectionMethod] Collection Accumulation
**Domain:** Roslyn incremental source generator - fluent builder extension
**Researched:** 2026-04-13
**Confidence:** HIGH

## Executive Summary

Converj v2.2 adds a [FluentCollectionMethod] parameter attribute that transforms a collection-typed constructor or static method parameter into a per-item accumulator chain. Instead of passing a full collection at once, callers write .WithTag().WithTag().Create(). The feature is well-understood from comparators (Lombok @Singular for Java, M31.FluentAPI FluentCollection for C#) and has a clear integration path into the existing 4-step incremental pipeline. No new NuGet packages are required - the implementation is pure Roslyn plus ImmutableArray<T> from packages already in the dependency set.

The recommended approach is: attribute, analysis, new AccumulatorFluentStep model type (not bolted onto RegularFluentStep), new syntax emitters. Collection parameters must be excluded from the trie key sequence; they live as accumulator methods on a dedicated step appended after the last trie end-node. Internal accumulation uses ImmutableArray<T> (not List<T>) for struct-safe copy-on-write semantics. Auto-singularization of parameter names (tags to WithTag) uses a hand-rolled rule table; no external library is warranted given the generator packaging constraints.

The critical risk is the struct immutability trap: if the accumulator field is a List<T> in a value-type step struct, every chain branch shares the same mutable list reference. This bug is invisible in simple linear tests and only surfaces when a caller reuses a step variable. The second risk is incorrect terminal-time conversion - the stored ImmutableArray<T> must be explicitly converted to whichever of the six supported declared types the parameter uses. Both risks are neutralized by choosing ImmutableArray<T> as the field type from the outset and writing a CollectionFieldStorage record that carries both element type and declared parameter type through to terminal emission.

## Key Findings

### Recommended Stack

No new dependencies are needed. SpecialType enum values cover all required collection interfaces with O(1) lookups; IArrayTypeSymbol covers T[]. ImmutableArray<T> - already in the project - is the correct accumulator field type for struct steps.

Humanizer.Core is ruled out: it requires SDK 9.0.200 to restore and has three transitive dependencies requiring explicit IL-bundling inside the analyzer NuGet path. Hand-rolled singularization in ~40 lines of StringExtensions.Singularize() covers 99% of real C# identifier names and is the established pattern for source generators.

**Core technologies:**
- SpecialType enum (Microsoft.CodeAnalysis 4.14.0, already in use): collection interface detection - O(1), zero allocation
- IArrayTypeSymbol pattern match: array parameter detection - same Roslyn API set already in use
- ImmutableArray<T> (System.Collections.Immutable, already in use): safe accumulator field in generated step structs - copy-on-write semantics prevent shared-mutation bugs
- Hand-rolled StringExtensions.Singularize(): English plural to singular for method name derivation - no external library
- List<T>.Construct(elementType) via Compilation.GetTypeByMetadataName: generating terminal conversion expressions - existing Roslyn pattern

### Expected Features

**Must have for v2.2 launch (table stakes):**
- [FluentCollectionMethod] attribute with optional singularName string parameter and Minimum int property
- Collection type detection and element type extraction (six supported types: T[], IEnumerable<T>, ICollection<T>, IList<T>, IReadOnlyList<T>, IReadOnlyCollection<T>)
- Error diagnostic when [FluentCollectionMethod] is applied to a non-collection parameter
- AccumulatorFieldStorage using ImmutableArray<T> internally with ImmutableArray<T>.Empty initialization
- Singular item-add method (e.g., WithTag(in Tag tag)) on the generated accumulator step
- Terminal-time conversion from ImmutableArray<T> to each of the six declared types
- Auto-singularization via hand-rolled rule table (-s/-es/-ies, plus an irregular word list)
- Explicit singularName override respected when provided

**Should have (v2.2.x after validation):**
- params T[] secondary overload alongside the singular WithTag(T item)
- [FluentCollectionMethod] + [FluentMethod] composability on the same parameter
- Min=1 compile-time enforcement via two-step topology (seed step + continuation step)

**Defer to v2.3+:**
- Dictionary/Map accumulation ([FluentDictionaryMethod])
- ImmutableArray<T> / ImmutableList<T> as declared target types
- Nested-builder lambda for complex element types

### Architecture Approach

The feature integrates into the existing 4-step pipeline with surgical changes. Step 1 (Syntax Filtering) is untouched. Step 2 (Target Analysis) gains FluentCollectionMethodAnalyzer. Step 3 (Model Building) excludes collection parameters from the trie key sequence and appends a new AccumulatorFluentStep after each trie end-node that has collection parameters. Step 4 (Syntax Generation) gains two new emitters and a dispatch branch in CompilationUnit.

**Major new or modified components:**
1. FluentCollectionMethodAttribute (Converj.Attributes) - public attribute; MethodName and MinimumItems properties
2. FluentCollectionMethodAnalyzer (TargetAnalysis) - validates collection type via SpecialType; extracts element type; emits diagnostic on mismatch
3. AccumulatorFluentStep (Models/Steps) - new IFluentStep; holds ImmutableArray<TElement> accumulator field; carries forwarded regular parameter fields
4. CollectionAccumulatorMethod (Models/Methods) - new self-returning IFluentMethod
5. AccumulatorStepDeclaration + AccumulatorStepMethodDeclaration (SyntaxGeneration) - emit the step struct, add-item methods, and terminal conversion expressions
6. Targeted changes to FluentModelBuilder, FluentStepBuilder, CompilationUnit, FluentTargetContext, TargetMetadata, FluentDiagnostics, TypeName

**Key structural invariant:** Collection parameters must never enter the trie as key segments. They are always post-trie accumulator methods on an AccumulatorFluentStep inserted between the last RegularFluentStep and the TargetTypeReturn.

### Critical Pitfalls

1. **Struct readonly + List<T> shared mutation** - A List<T> field in a value-type step struct causes every chain branch to share the same mutable list. Use ImmutableArray<T> as the accumulator field type; each .AddX() call returns a new struct copy via ImmutableArray.Add. Decide field type before writing any syntax generation.

2. **Terminal conversion mismatch** - The accumulator stores ImmutableArray<T> internally but the declared parameter type may be T[], IList<T>, IReadOnlyCollection<T>, etc. Use a CollectionFieldStorage record that carries both element type and declared type; emit the correct conversion per type in AccumulatorStepMethodDeclaration.

3. **Singularization edge cases and method name collisions** - Naive TrimEnd produces invalid identifiers for irregular plurals (indices, children, data). Use a curated irregulars dictionary plus suffix rules. Detect collisions at analysis time and emit a diagnostic requiring an explicit MethodName override.

4. **Trie equality collision with same-name scalar parameter** - If a constructor has both Tag tag (scalar) and IEnumerable<Tag> tags (collection), the trie equality comparer may treat them as the same key node. CollectionFluentMethodParameter must never be equal to a scalar FluentMethodParameter; collection parameters must be excluded from trie key insertion.

5. **Incremental caching broken by List<T> in model objects** - Any model type flowing through ForAttributeWithMetadataName transforms that carries a List<T> field silently breaks incrementality. Use ImmutableArray<T> exclusively in all analysis and model phases.

## Implications for Roadmap

### Phase 1: Foundation - Attribute, Analysis, and Model Design

**Rationale:** All downstream phases depend on the attribute existing, collection type detection being correct, and the model type hierarchy being settled. The three most dangerous design decisions (accumulator field type, trie exclusion, singularization correctness) must all be resolved here before any syntax generation code is written.

**Delivers:** [FluentCollectionMethod] attribute class; FluentCollectionMethodAnalyzer with validated collection type detection and element type extraction; CollectionParameterInfo record; FluentTargetContext/TargetMetadata extended with CollectionParameters; AccumulatorFluentStep, CollectionAccumulatorMethod, CollectionFluentMethodParameter model types; FluentDiagnostics entry for non-collection mismatch; TypeName constant; StringExtensions.Singularize() rule table.

**Addresses:** [FluentCollectionMethod] attribute definition; collection type detection; non-collection diagnostic; auto-singularization with explicit override; AccumulatorFieldStorage using ImmutableArray<T>.

**Avoids:** Struct readonly + List<T> shared mutation (Pitfall 1); trie equality collision (Pitfall 4); incorrect non-collection diagnostic from over-broad type detection (Pitfall 7); incremental caching breakage (Pitfall 8); singularization edge cases (Pitfall 3).

### Phase 2: Core Code Generation - AccumulatorStep Emission and Terminal Conversion

**Rationale:** With the model types from Phase 1 fully defined, syntax generation is mechanical work. Terminal conversion is the second-highest risk area and should be verified against all six declared types before composability concerns are addressed.

**Delivers:** AccumulatorStepDeclaration and AccumulatorStepMethodDeclaration syntax emitters; FluentModelBuilder changes (trie exclusion, accumulator step insertion after end-nodes); FluentStepBuilder traversal exclusion for CollectionAccumulatorMethod; CompilationUnit dispatch branch; full basic test coverage (single collection parameter, each of the six declared types, mixed chain with regular parameters, extension method targets, type-first mode).

**Addresses:** Singular item-add method on generated step; terminal conversion to declared type; internal accumulation with ImmutableArray<T>.Empty initialization.

**Avoids:** Terminal conversion mismatch for non-matching declared types (Pitfall 2); FluentStepConstructorDeclaration initializing accumulator field to default instead of ImmutableArray.Empty.

### Phase 3: Composability and Extended Features

**Rationale:** Only add complexity once the standalone accumulator is proven correct by the full existing test suite (415 tests) passing unchanged. Composability involves a field-storage ownership decision that is cleaner to reason about after the core is stable.

**Delivers:** Coexistence of [FluentCollectionMethod] and [FluentMethod] on the same parameter; params T[] secondary overload; tests for composability and coexistence.

**Addresses:** [FluentCollectionMethod] + [FluentMethod] composability; params T[] overload.

**Avoids:** [FluentCollectionMethod] + [FluentMethod] field storage conflict (Pitfall 6) - by explicitly designating accumulator as the storage owner.

### Phase 4: Min=1 Compile-Time Enforcement (Conditional)

**Rationale:** Requires a two-step topology not present anywhere in the codebase (SeedStep with no terminal + ContinuationStep with terminal). Treat as a separate milestone; only pursue if demand is confirmed after Phases 1-3 ship.

**Delivers:** SeedAccumulatorStep (no terminal) -> ContinuationAccumulatorStep (accumulator + terminal) step pair for MinimumItems=1; compile-time enforcement that terminal is unreachable until at least one .AddX() call.

**Avoids:** MinItems=1 single-step topology that emits a runtime guard instead of compile-time enforcement (Pitfall 5).

### Phase Ordering Rationale

- Phase 1 before Phase 2: every syntax generation decision depends on knowing the field type, trie exclusion rule, and singularization logic. Getting these wrong costs a full Phase 2 rewrite.
- Phase 2 before Phase 3: composability analysis assumes the standalone accumulator step works correctly.
- Phase 4 last and optional: two-step topology for Min=1 is architecturally independent of the core accumulator.
- Backward compatibility verified at every phase: full existing test suite must pass unchanged after each phase.

### Research Flags

Phases with well-documented patterns (skip additional research):
- **Phase 1 (attribute + analysis):** SpecialType enum usage is fully documented; FluentCollectionMethodAnalyzer mirrors FluentParameterAnalyzer directly.
- **Phase 2 (syntax generation):** All required SyntaxFactory node types already appear in the codebase; no new idioms.
- **Phase 3 (composability):** Additive work on top of existing infrastructure; field ownership decision is clear from pitfall analysis.

Phase meriting a design review before implementation (not external research):
- **Phase 4 (Min=1 two-step topology):** No existing IFluentStep has IsEndStep=false with no terminal method. The seed/continuation step pair is novel to the codebase and warrants a design session before writing code.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All claims verified against official Microsoft docs, NuGet package metadata, and direct codebase inspection. No new dependencies required. |
| Features | HIGH | Based on direct codebase inspection + verified competitor analysis (Lombok official docs, M31.FluentAPI official repo). MVP scope is clear. |
| Architecture | HIGH | Based on direct analysis of all affected source files. Integration points and component boundaries are precise with line-level references. |
| Pitfalls | HIGH | All pitfalls derived from direct codebase analysis. The struct immutability trap and trie exclusion rule are well-established correctness concerns. |

**Overall confidence:** HIGH

### Gaps to Address

- **Attribute property name inconsistency:** Research uses both MinimumItems and Minimum in different files. Settle on one name before Phase 1 implementation begins.
- **ImmutableArray<T> O(N) copy cost:** ImmutableArray<T>.Add() is copy-on-write and O(N). For typical builder use (under 50 items) this is inconsequential but should be documented; the params T[] overload in Phase 3 provides the bulk alternative.
- **Extension method targets + type-first mode with collection parameters:** Architecture research lists these as test cases but does not detail their integration nuances. Cover explicitly in Phase 2 test planning.

## Sources

### Primary (HIGH confidence)
- https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.specialtype - all required collection interface SpecialType values verified in 4.x
- https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.compilation.getspecialtype - API signature and return type
- https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.iarraytypesymbol - array type pattern
- https://www.nuget.org/packages/Humanizer.Core - transitive deps and SDK 9.0.200 restore requirement confirmed; dependency ruled out
- https://projectlombok.org/features/BuilderSingular - feature comparison baseline
- https://github.com/m31coding/M31.FluentAPI - feature comparison baseline
- Converj codebase direct inspection - FluentStepDeclaration.cs, FluentModelBuilder.cs, FluentMethodBuilder.cs, FluentStepBuilder.cs, Trie.cs, RegularFluentStep.cs, FieldStorage.cs, IFluentValueStorage.cs, FluentMethodParameter.cs, FluentTargetValidator.cs, Directory.Packages.props, Converj.Generator.csproj

### Secondary (MEDIUM confidence)
- https://github.com/StefH/FluentBuilder - feature comparison; lazy/deferred patterns confirmed as anti-features for Converj
- Humanizer SDK 9.0.200 requirement - migration docs fetched indirectly via search; consistent with NuGet metadata

---
*Research completed: 2026-04-13*
*Ready for roadmap: yes*
