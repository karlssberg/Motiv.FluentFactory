# Phase 23: Composability - Context

**Gathered:** 2026-04-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver full composability for `[FluentCollectionMethod]`: a developer can apply it alongside `[FluentMethod]` (and `[MultipleFluentMethods]`) on the same target, and can also apply it to properties — not just parameters. Both attributes contribute entry methods into a single accumulator step, where they coexist as freely composable operations. Covers requirements: COMP-01, COMP-02, COMP-03.

**Amendment to ROADMAP / REQUIREMENTS:** The Phase 23 goal and COMP-03 as originally written call for *mutual exclusion* between accumulator and bulk-set paths. This phase replaces that model with *free composition* (see Implementation Decisions below). ROADMAP.md Phase 23 goal and REQUIREMENTS.md COMP-03 text must be amended as part of Phase 23 planning. Core user value (two ways to populate a collection, safely composable at compile time) is preserved.

Does NOT cover: MinItems compile-time enforcement (Phase 24), which still uses distinct step types but is now informed by Model D.

</domain>

<decisions>
## Implementation Decisions

### Path divergence topology — Model D (single accumulator step)

- **One `AccumulatorFluentStep` per collection parameter/property.** Not two. Not a branching subtree.
- **Two self-returning methods on that step:**
  - `AddX(T item)` — single-item append. Field assignment: `field.Add(item)`.
  - `WithXs(IEnumerable<T> items)` — **append-range semantics** (not replace). Field assignment: `field.AddRange(items)`.
- **Two entry transitions from the final regular step** into the accumulator step: one per method. The collection param/property remains excluded from the trie key sequence, consistent with Phase 22.
- **No mutual exclusion.** Both methods are always available on the accumulator step. User may freely interleave `AddX` / `WithXs` calls. Each call composes incrementally on the existing `ImmutableArray<T>` state.
- **No multi-param step explosion.** Because Model D collapses to one step per collection param, N parameters with combined attributes produce N independent accumulator steps — no 2^N Cartesian blow-up.
- **Terminal conversion unchanged from Phase 22.** `AccumulatorCollectionConversionExpression.ConvertToDeclaredType` continues to convert the final `ImmutableArray<T>` to the declared collection type using the existing 6-case table.

### Broad overloading rule (generator-wide)

- **Rule:** When two or more generated fluent methods would otherwise collide on the same step type, but their parameter lists are *signature-distinct* (different parameter-type sequences — return type and parameter names do not count), emit them as C# method overloads instead of raising a collision diagnostic.
- **Scope:** Generator-wide. Applies to `AddX` vs `WithXs` on the same param, to accumulator-vs-accumulator on the same target with different element types, to `[FluentMethod]`-name-overlaps that happen to differ in parameter shape, and to every other place collision detection runs.
- **Retrofit scope (part of Phase 23):** CVJG0052 (accumulator-accumulator collision on same target) and every pre-v2.2 collision diagnostic that currently errors on same-name-different-signature must be updated to fire only when signatures are *also* identical. Audit task is in scope for Phase 23.
- **CVJG0052 new semantics:** Fires only when two generated fluent methods share both name AND identical parameter-type sequence on the same step type within the same target. Message updated to reflect the stricter trigger.
- **Definition of signature distinctness:** Parameter count differs, or any parameter's type differs. Includes generic-arity differences, array vs. `IEnumerable<T>`, ref/in/out modifiers. Does NOT include parameter-name-only differences or return-type-only differences.
- **Ambiguous overloads at call sites** (e.g., when element type is itself `IEnumerable<U>` and `AddX(IEnumerable<U>)` + `WithXs(IEnumerable<IEnumerable<U>>)` create C#-level ambiguity at some call shapes) are left to the C# compiler. Generator emits both overloads; compiler errors on ambiguous invocations. User disambiguates via explicit `MethodName` overrides.

### Default-name collision behavior

- **No new diagnostic for default names.** `AddX` (singular) and `WithXs` (plural/as-declared) diverge by construction for every non-pathological parameter name.
- **Parameter names that fail singularization** (e.g., `data`, `info`, `metadata`) already trigger CVJG0051 from Phase 21 — no new handling needed.
- **Explicit `MethodName` overrides that would collide** are handled by the broad overload rule: if signatures differ, overload; if signatures match, CVJG0052 fires.

### `[FluentMethod]` option composition

- **All existing `[FluentMethod]` options compose unchanged.** `MethodName`, `MethodPrefix`, `EagerVerb`, `ReturnType`, `NoCreateMethod`, `TerminalMethod`, and any other configuration continue to mean what they currently mean for the bulk entry method (`WithXs`).
- **No compatibility matrix.** No new diagnostic for option incompatibility. If a combination produces a nonsensical chain, existing feature-level diagnostics surface it.

### `[MultipleFluentMethods]` composition

- **Composes under Model D with no new machinery.** Each template-generated bulk method from `[MultipleFluentMethods]` becomes an additional entry transition into the same `AccumulatorFluentStep`. Signature-distinct by template design → broad overload rule admits them all as overloads. Semantic: each bulk variant seeds via `AddRange` on the accumulator's `ImmutableArray<T>` field.
- **No scope addition on top of what `[MultipleFluentMethods]` already does.** The template-generated methods run through the same Model D plumbing as a single `[FluentMethod]`.

### `[FluentCollectionMethod]` widened to properties (full parity with parameters)

- **`AttributeUsage` widened.** `FluentCollectionMethodAttribute` changes from `Parameter` to `Parameter | Property`. Phase 21's deliberate parameter-only constraint is lifted in Phase 23.
- **Full parity with parameter case.** On a property-backed collection, Model D produces: `AccumulatorFluentStep` with `AddX` + `WithXs`, `ImmutableArray<T>` field on the step, terminal-time assignment to the property (using the existing `PropertyFieldStorage` emission path from v2.0 rather than a constructor argument).
- **Required properties supported.** Combined attribute on a `required` property works — the accumulator satisfies the required contract at terminal time via object-initializer syntax (matching the existing v2.0 required-property code path).
- **Init-only properties supported** when the target's construction pattern permits object-initializer emission (which is the dominant case).

### New diagnostic CVJG0053 — unsupported property accessor

- **Fires when:** `[FluentCollectionMethod]` is applied to a property whose accessor shape makes terminal-time assignment impossible. Representative case: a record primary-constructor-backed property where re-assignment via object initializer conflicts with the primary constructor's synthesized positional parameter.
- **Severity:** Error. Consistent with existing property-feature diagnostics.
- **Registered in `AnalyzerReleases.Unshipped.md`.**
- **Exact triggering set** is Claude's Discretion during research/planning — gather the edge cases by cross-referencing existing property-storage diagnostics and the record-primary-ctor handling in `ConstructorAnalysis/`.

### MinItems semantics under Model D (Phase 24 expectation-setting)

- **Phase 23 scope:** parse and carry `MinItems` through the pipeline unchanged (already done in Phase 21). No enforcement in Phase 23.
- **Phase 24 expectation locked here (Reading C):** When `[FluentCollectionMethod(MinItems = N)]` is combined with `[FluentMethod]`, `WithXs` transitions *unconditionally* from the empty/minimum-not-met step to the satisfied step that exposes `Create`. Compile-time enforcement treats a `WithXs` call as satisfying the minimum regardless of the runtime list length.
- **Rationale:** Aligns with user intent ("bulk-set is an escape hatch that assumes the developer knows what they're passing"), preserves compile-time-only enforcement, avoids runtime guards.
- **Implication for Phase 24:** Phase 24 will introduce distinct step types for the minimum-not-met vs. minimum-met states. Under Model D, both `AddX` (single) and `WithXs` (bulk) are exit edges from the minimum-not-met step. For `MinItems >= 2`, Phase 24 must decide how `AddX` progresses incrementally (likely a chain of N-1 intermediate steps for `AddX`, all of which also expose `WithXs` → minimum-met transition).

### Claude's Discretion

- Exact descriptor number for the new property-accessor diagnostic (CVJG0053 assumed next free; confirm during planning).
- Exact wording of diagnostic messages (follow existing `FluentDiagnostics.cs` style).
- Exact file names for new storage/step/method types introduced to support property-backed accumulators.
- Exact test-file organization under `src/Converj.Generator.Tests/` (follow established `_Tests.cs` per-requirement naming).
- Whether the `[MultipleFluentMethods]`-combined-with-`[FluentCollectionMethod]` case gets its own dedicated test class or folds into the primary combined-annotation suite.
- Exact retrofit audit list: which pre-v2.2 collision diagnostics need message updates vs. trigger narrowing vs. no change. Planner produces audit list during research.
- Amendment wording for ROADMAP.md Phase 23 goal and REQUIREMENTS.md COMP-03 (planner drafts; user reviews).

</decisions>

<specifics>
## Specific Ideas

- "Bulk then add individual items" was the user's original ergonomic instinct — Model D realizes that instinct and generalizes it by making the two methods fully composable rather than exclusive.
- "Be as flexible as the language already is" — rationale for the broad overload rule. The generator's collision detection should not forbid combinations that C# itself would accept as valid overloads.
- Byte-identical snapshot tests were considered and declined — generator is deterministic, existing behavior tests cover observable API surface, and cosmetic drift surfaces in PR review anyway.
- Phase 22's verified architecture (`AccumulatorFluentStep`, `AccumulatorMethod`, `AccumulatorTransitionMethod`, `AccumulatorStepDeclaration`, `AccumulatorCollectionConversionExpression`) is the backbone; Phase 23 adds a second self-returning method (`WithXs`) plus a second transition entry and a property-backed storage variant.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets

- **`AccumulatorFluentStep`** (`src/Converj.Generator/Models/Steps/AccumulatorFluentStep.cs`): The single step type central to Model D. Needs a new method (`WithXs`) added alongside the existing `AddX` (`AccumulatorMethod`) and a second entry transition.
- **`AccumulatorMethod`** (`src/Converj.Generator/Models/Methods/AccumulatorMethod.cs`): Pattern for the new bulk-append method — self-returning, `IFluentMethod` implementation. The new `WithXs` method mirrors this but takes `IEnumerable<T>` rather than element type, and emits `AddRange` rather than `Add`.
- **`AccumulatorTransitionMethod`** (`src/Converj.Generator/Models/Methods/AccumulatorTransitionMethod.cs`): Pattern for the new bulk-entry transition. Needs a sibling or parameterised variant for the `WithXs` entry path.
- **`AccumulatorStepDeclaration`** (`src/Converj.Generator/SyntaxGeneration/AccumulatorStepDeclaration.cs`): Emission site. Needs to emit two self-returning methods instead of one, both marked `[MethodImpl(AggressiveInlining)]`.
- **`AccumulatorCollectionConversionExpression`** (`src/Converj.Generator/SyntaxGeneration/AccumulatorCollectionConversionExpression.cs`): Unchanged. Same six-case terminal conversion applies.
- **`FluentModelBuilder`** / **`FluentStepBuilder`**: Model-building rewire site. Phase 22 already excludes collection params from the trie; Phase 23 adds the `WithXs` entry wiring and (if combined with `[FluentMethod]`) threads the `[FluentMethod]`-configured bulk method into the accumulator transition list.
- **`FluentMethodAttribute`** + **`MultipleFluentMethodsAttribute`**: Existing [FluentMethod] family. Configurations flow through unchanged; only the *destination* of the bulk method changes (from a regular step to the accumulator step) when combined with `[FluentCollectionMethod]`.
- **`FluentCollectionMethodAttribute`**: `AttributeUsage` needs widening from `Parameter` to `Parameter | Property`.
- **`PropertyFieldStorage`** (v2.0): Existing property-backed storage strategy. The terminal-time property assignment emitted by the accumulator reuses this path for the property-target case.
- **`CollectionParameterInfo`**: Phase 21's carrier record. Model D needs a property-target counterpart (either reuse with a discriminator or introduce `CollectionPropertyInfo` — planner's call).
- **`FluentDiagnostics`** (`Diagnostics/FluentDiagnostics.cs`): Central descriptor registry. New CVJG0053 descriptor appended. CVJG0052 message updated.
- **`AnalyzerReleases.Unshipped.md`**: Register CVJG0053 and the CVJG0052 message change.

### Established Patterns

- **Post-trie accumulator topology**: Phase 22 pattern. Collection params excluded from trie key sequence; accumulator lives as a tail transition from the final regular step.
- **`ImmutableArray<T>` state**: All model/carrier types use immutable arrays (Pitfall 8). Any new records or extensions follow.
- **Skip-on-error targets**: CVJG0011 / CVJG0052 precedent for emitting a diagnostic and excluding the offending target while sibling targets continue to generate.
- **Property storage pattern**: v2.0 established the `PropertyFieldStorage` strategy with object-initializer or setter-assignment emission depending on accessor shape. Phase 23 accumulator-on-property follows the same shape.
- **Broad overload rule is new**: No prior phase permitted signature-distinct same-name methods. Retrofit touches the `FluentMethodSignatureEqualityComparer.cs` and `FilterCollectionAccumulatorCollisions` paths introduced in earlier phases.

### Integration Points

- **`FluentTargetContext`** / **`TargetMetadata`**: Already carry `CollectionParameterInfo`. Extend to carry property-target accumulator info if the planner splits parameter vs. property into separate carriers.
- **`FluentRootGenerator.cs`** Step 2 (Target Analysis): `FluentCollectionMethodAnalyzer` runs here. Extend to handle property targets alongside parameter targets.
- **`FluentMethodBuilder` / `FluentMethodSelector`**: When a `[FluentMethod]` targets a collection param/property that also carries `[FluentCollectionMethod]`, the method must be routed to the accumulator transition list instead of the regular trie path. Currently the collision/inclusion logic assumes bulk-set stays in the trie — Phase 23 changes that.
- **`FluentStepBuilder`**: Self-loop guard on `AccumulatorMethod` (added in Phase 22) needs to also guard the new `WithXs` self-returning method (or be generalized to exclude all self-returning accumulator methods from descendant traversal).

</code_context>

<deferred>
## Deferred Ideas

- **Cross-root name-collision detection.** Broad overload handles same-step same-target collisions generator-wide; cross-root concerns (multiple fluent roots with name overlap in consumer code) are not a generator concern.
- **Runtime MinItems guard.** Explicitly rejected. Compile-time step-type enforcement is the Phase 24 path.
- **`AddRange` as a distinct attribute / method** separate from `WithXs`. `WithXs` now IS the bulk-append under Model D. A separate `AddRange` attribute remains in REQUIREMENTS.md FUTURE-01 for v3+ consideration but is unnecessary under Model D.
- **`params T[]` overload on accumulator.** Listed as REQUIREMENTS.md FUTURE-03; still deferred to v3+. Broad overload rule makes this trivially additive when the time comes.
- **Dictionary accumulation** (`[FluentDictionaryMethod]`). FUTURE-02; v3+.
- **MethodPrefix override on `[FluentCollectionMethod]`.** Hardcoded `Add` prefix continues. Additive change deferrable post-v2.2.

</deferred>

---

*Phase: 23-composability*
*Context gathered: 2026-04-16*
