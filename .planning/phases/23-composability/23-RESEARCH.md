# Phase 23: Composability - Research

**Researched:** 2026-04-16
**Domain:** Roslyn Incremental Source Generator (C#/.NET) — Model D accumulator composability (single step, two self-returning methods), signature-distinct overloading generator-wide, property-backed accumulators, new diagnostic CVJG0053
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Amendment to ROADMAP / REQUIREMENTS.** The Phase 23 goal and COMP-03 as originally written call for *mutual exclusion* between accumulator and bulk-set paths. Phase 23 replaces that model with *free composition*. ROADMAP.md Phase 23 goal and REQUIREMENTS.md COMP-03 text must be amended as part of Phase 23 planning. Core user value (two ways to populate a collection, safely composable at compile time) is preserved.

#### Path divergence topology — Model D (single accumulator step)

- **One `AccumulatorFluentStep` per collection parameter/property.** Not two. Not a branching subtree.
- **Two self-returning methods on that step:**
  - `AddX(T item)` — single-item append. Field assignment: `field.Add(item)`.
  - `WithXs(IEnumerable<T> items)` — **append-range semantics** (not replace). Field assignment: `field.AddRange(items)`.
- **Two entry transitions from the final regular step** into the accumulator step: one per method. The collection param/property remains excluded from the trie key sequence, consistent with Phase 22.
- **No mutual exclusion.** Both methods are always available on the accumulator step. User may freely interleave `AddX` / `WithXs` calls. Each call composes incrementally on the existing `ImmutableArray<T>` state.
- **No multi-param step explosion.** Because Model D collapses to one step per collection param, N parameters with combined attributes produce N independent accumulator steps — no 2^N Cartesian blow-up.
- **Terminal conversion unchanged from Phase 22.** `AccumulatorCollectionConversionExpression.ConvertToDeclaredType` continues to convert the final `ImmutableArray<T>` to the declared collection type using the existing 6-case table.

#### Broad overloading rule (generator-wide)

- **Rule:** When two or more generated fluent methods would otherwise collide on the same step type, but their parameter lists are *signature-distinct* (different parameter-type sequences — return type and parameter names do not count), emit them as C# method overloads instead of raising a collision diagnostic.
- **Scope:** Generator-wide. Applies to `AddX` vs `WithXs` on the same param, to accumulator-vs-accumulator on the same target with different element types, to `[FluentMethod]`-name-overlaps that happen to differ in parameter shape, and to every other place collision detection runs.
- **Retrofit scope (part of Phase 23):** CVJG0052 and every pre-v2.2 collision diagnostic that currently errors on same-name-different-signature must be updated to fire only when signatures are *also* identical. Audit task is in scope for Phase 23.
- **CVJG0052 new semantics:** Fires only when two generated fluent methods share both name AND identical parameter-type sequence on the same step type within the same target. Message updated to reflect the stricter trigger.
- **Definition of signature distinctness:** Parameter count differs, or any parameter's type differs. Includes generic-arity differences, array vs. `IEnumerable<T>`, ref/in/out modifiers. Does NOT include parameter-name-only differences or return-type-only differences.
- **Ambiguous overloads at call sites** are left to the C# compiler. Generator emits both overloads; compiler errors on ambiguous invocations. User disambiguates via explicit `MethodName` overrides.

#### Default-name collision behavior

- **No new diagnostic for default names.** `AddX` (singular) and `WithXs` (plural/as-declared) diverge by construction for every non-pathological parameter name.
- **Parameter names that fail singularization** (e.g., `data`, `info`, `metadata`) already trigger CVJG0051 from Phase 21 — no new handling needed.
- **Explicit `MethodName` overrides that would collide** are handled by the broad overload rule.

#### `[FluentMethod]` option composition

- **All existing `[FluentMethod]` options compose unchanged.** `MethodName`, `MethodPrefix`, `EagerVerb`, `ReturnType`, `NoCreateMethod`, `TerminalMethod`, and any other configuration continue to mean what they currently mean for the bulk entry method (`WithXs`).
- **No compatibility matrix.** No new diagnostic for option incompatibility.

#### `[MultipleFluentMethods]` composition

- **Composes under Model D with no new machinery.** Each template-generated bulk method becomes an additional entry transition into the same `AccumulatorFluentStep`. Signature-distinct by template design → broad overload rule admits them as overloads. Semantic: each bulk variant seeds via `AddRange` on the accumulator's `ImmutableArray<T>` field.
- **No scope addition on top of what `[MultipleFluentMethods]` already does.**

#### `[FluentCollectionMethod]` widened to properties (full parity with parameters)

- **`AttributeUsage` widened.** `FluentCollectionMethodAttribute` changes from `Parameter` to `Parameter | Property`.
- **Full parity with parameter case.** On a property-backed collection, Model D produces: `AccumulatorFluentStep` with `AddX` + `WithXs`, `ImmutableArray<T>` field on the step, terminal-time assignment to the property (using the existing `PropertyFieldStorage` emission path from v2.0 rather than a constructor argument).
- **Required properties supported.** Combined attribute on a `required` property works — the accumulator satisfies the required contract at terminal time via object-initializer syntax.
- **Init-only properties supported** when the target's construction pattern permits object-initializer emission (which is the dominant case).

#### New diagnostic CVJG0053 — unsupported property accessor

- **Fires when:** `[FluentCollectionMethod]` is applied to a property whose accessor shape makes terminal-time assignment impossible. Representative case: a record primary-constructor-backed property where re-assignment via object initializer conflicts with the primary constructor's synthesized positional parameter.
- **Severity:** Error. Consistent with existing property-feature diagnostics.
- **Registered in `AnalyzerReleases.Unshipped.md`.**
- **Exact triggering set** is Claude's Discretion during research/planning.

#### MinItems semantics under Model D (Phase 24 expectation-setting)

- **Phase 23 scope:** parse and carry `MinItems` through the pipeline unchanged. No enforcement.
- **Phase 24 expectation locked here (Reading C):** When `[FluentCollectionMethod(MinItems = N)]` is combined with `[FluentMethod]`, `WithXs` transitions *unconditionally* from the empty/minimum-not-met step to the satisfied step that exposes `Create`.

### Claude's Discretion

- Exact descriptor number for the new property-accessor diagnostic (CVJG0053 assumed next free; confirm during planning).
- Exact wording of diagnostic messages.
- Exact file names for new storage/step/method types introduced to support property-backed accumulators.
- Exact test-file organization under `src/Converj.Generator.Tests/`.
- Whether the `[MultipleFluentMethods]`-combined-with-`[FluentCollectionMethod]` case gets its own dedicated test class or folds into the primary combined-annotation suite.
- Exact retrofit audit list: which pre-v2.2 collision diagnostics need message updates vs. trigger narrowing vs. no change.
- Amendment wording for ROADMAP.md Phase 23 goal and REQUIREMENTS.md COMP-03.

### Deferred Ideas (OUT OF SCOPE)

- Cross-root name-collision detection.
- Runtime MinItems guard.
- `AddRange` as a distinct attribute/method separate from `WithXs` (FUTURE-01, v3+).
- `params T[]` overload on accumulator (FUTURE-03, v3+).
- Dictionary accumulation `[FluentDictionaryMethod]` (FUTURE-02, v3+).
- `MethodPrefix` override on `[FluentCollectionMethod]`.

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| COMP-01 | `[FluentCollectionMethod]` can be applied alongside `[FluentMethod]` on the same parameter | `FluentMethodAttribute` already targets `Parameter \| Property`; `FluentCollectionMethodAttribute` already parameter-targeted. No attribute-level blocking exists today. Wiring change: when both attributes co-occur, the `[FluentMethod]` entry is redirected from the regular trie to become a `WithXs` transition into the same `AccumulatorFluentStep` that `[FluentCollectionMethod]` produces. |
| COMP-02 | When both attributes are present, both accumulator and bulk-set methods are generated | Generates `AddX(T item)` via existing `AccumulatorMethod` (Phase 22) PLUS new `WithXs(IEnumerable<T> items)` method. Both live on the single `AccumulatorFluentStep`. Two transition entries from the preceding regular step (one per method name). |
| COMP-03 (amended) | Both methods freely compose on the accumulator step — interleaving `AddX` and `WithXs` each call appends incrementally via `ImmutableArray<T>.Add` / `.AddRange` with copy-constructor return (`GEN-03` independence preserved by value semantics) | Single `AccumulatorFluentStep` with two self-returning methods. `WithXs` field assignment uses `this._field.AddRange(items)`; `AddX` continues using `this._field.Add(item)`. Both return a new copy of the same struct via the existing private copy constructor. |

**Amendment requirement:** COMP-03 wording in REQUIREMENTS.md and the Phase 23 goal in ROADMAP.md describe mutual exclusion. The amendment task replaces that with free composition. Execute as first plan step before generator changes.

</phase_requirements>

## Summary

Phase 23 adds composability on top of the Phase 22 accumulator infrastructure. Phase 22 delivered the scaffolding (a dedicated `AccumulatorFluentStep`, a self-returning `AccumulatorMethod` emitting `AddX`, a private copy constructor pattern, and the 6-case collection conversion). Phase 23 extends this without replacing it:

1. **Add a second self-returning method** to `AccumulatorFluentStep`: `WithXs(IEnumerable<T>)` that calls `_field.AddRange(items)` and returns a new struct copy.
2. **Wire a second entry transition** into the accumulator step alongside the existing `BuildTarget` transition — specifically a `WithXs` method emitted on the preceding regular step. This transition is triggered when a parameter carries both `[FluentCollectionMethod]` AND `[FluentMethod]` (or `[MultipleFluentMethods]`), routing the bulk method to the accumulator step instead of keeping it in the regular trie.
3. **Widen `FluentCollectionMethodAttribute.AttributeUsage`** from `Parameter` to `Parameter | Property`. Extend `FluentCollectionMethodAnalyzer` to analyze property-backed collection targets, reusing the existing `PropertyStorage`/`PropertyFieldStorage` emission path for terminal-time assignment via object initializers.
4. **Broaden collision detection generator-wide** to allow signature-distinct overloads. `FluentMethodSignatureEqualityComparer` already compares by name + parameter types + type-parameter list; most places that emit collision diagnostics need to switch from name-only comparison to the signature comparer. CVJG0052 retightens to fire only on name AND signature collision.
5. **Introduce CVJG0053** for unsupported property accessor shapes (dominant case: record primary-constructor positional property conflicts).
6. **Carry `MinItems` through** unchanged (no enforcement — Phase 24).

The delivered architecture avoids mutual exclusion (originally spec'd) in favor of free composition: both methods always available; each call incrementally appends to the `ImmutableArray<T>` backing field. Struct value-type semantics preserve `GEN-03` branching independence without additional work.

**Primary recommendation:** Add a parallel `AccumulatorBulkMethod` (name TBD — e.g., `AccumulatorRangeMethod`) implementing `IFluentMethod` with `IEnumerable<T>` parameter type, and an `AccumulatorBulkTransitionMethod` for the entry-side. Extend `AccumulatorStepDeclaration` to emit both add-methods and both constructors. Do NOT retrofit `AccumulatorMethod` with a union discriminator — the screaming-architecture precedent set by Phase 22 (`AccumulatorMethod` + `AccumulatorTransitionMethod` as distinct types) dictates a parallel pair. Introduce a property-target counterpart (`CollectionPropertyInfo` suggested) rather than overloading `CollectionParameterInfo` with a nullable `Parameter`/`Property` discriminator, to keep analyses and storage paths cleanly screaming.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `System.Collections.Immutable` | per `Directory.Packages.props` | `ImmutableArray<T>.AddRange(IEnumerable<T>)` — bulk-append semantics | Already the accumulator backing type; `AddRange` is the canonical bulk-append operation |
| `Microsoft.CodeAnalysis` / `Microsoft.CodeAnalysis.CSharp` | per `Directory.Packages.props` | `IPropertySymbol`, `IArrayTypeSymbol`, `SpecialType`, `AttributeData.NamedArguments`, `SyntaxFactory` | Generator foundation — unchanged from Phase 22 |
| `xUnit` + `CSharpSourceGeneratorVerifier<FluentRootGenerator>` | project-local | Source-gen output-pinning + diagnostic tests | Phase 22 established record-replay methodology |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `AutoFixture` + `NSubstitute` | per `Directory.Packages.props` | Unit-level tests for new analyzer/model-builder helpers | Only where algorithmic isolation warranted; Phase 22 showed source-gen output assertions suffice for most pipeline tests |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `ImmutableArray<T>.AddRange` | `ImmutableArray<T>` Builder + `.ToImmutable()` | Avoided — single-call `AddRange` is allocation-equivalent for typical builder scale (1–20 items) and keeps emission code trivial |
| `CollectionPropertyInfo` (new type) | Discriminator on `CollectionParameterInfo` (nullable `Property`) | CVJG0053 triggers only on property targets; a separate type keeps diagnostics precise and avoids `null`-checking scatter. Planner's call. |
| Retrofit `AccumulatorMethod` with a `Kind` enum | Parallel `AccumulatorBulkMethod` type | Screaming architecture precedent (Phase 22 uses distinct types for `AccumulatorMethod` vs `AccumulatorTransitionMethod`); two small classes read better than one polymorphic one |

**Installation:** No new NuGet packages.

## Architecture Patterns

### Recommended Project Structure — New Files

```
src/Converj.Attributes/
└── FluentCollectionMethodAttribute.cs                 (modify — widen AttributeUsage)

src/Converj.Generator/
├── Models/
│   ├── Methods/
│   │   ├── AccumulatorBulkMethod.cs                   (new — self-returning WithXs on accumulator step)
│   │   └── AccumulatorBulkTransitionMethod.cs         (new — WithXs entry transition on preceding regular step)
│   └── Steps/
│       └── AccumulatorFluentStep.cs                   (modify — accept both AddX and WithXs methods)
├── ModelBuilding/
│   ├── FluentMethodSignatureEqualityComparer.cs       (review — already signature-based; confirm semantics)
│   ├── TargetContextFilter.cs                         (modify — CVJG0052 narrows to signature-collision; may need signature-aware FindCollision)
│   └── ...                                            (other sites identified in retrofit audit)
├── SyntaxGeneration/
│   └── AccumulatorStepDeclaration.cs                  (modify — emit second AddX-family method and support property-target terminal assignment)
├── TargetAnalysis/
│   ├── CollectionParameterInfo.cs                     (keep for parameters)
│   ├── CollectionPropertyInfo.cs                      (new — property-backed counterpart)
│   └── FluentCollectionMethodAnalyzer.cs              (modify — extend to analyze property targets; emit CVJG0053)
├── Diagnostics/
│   └── FluentDiagnostics.cs                           (modify — add CVJG0053; update CVJG0052 message)
├── FluentModelBuilder.cs                              (modify — wire bulk transition; property-target construction path)
└── AnalyzerReleases.Unshipped.md                      (modify — register CVJG0053, note CVJG0052 message change)

src/Converj.Generator.Tests/
├── CollectionMethodComposabilityTests.cs              (new — COMP-01/02/03 combined-annotation suite)
├── CollectionMethodBulkSetTests.cs                    (new — WithXs method-level assertions)
├── PropertyBackedCollectionTests.cs                   (new — property-target parity suite)
├── CollectionMethodPropertyAccessorDiagnosticTests.cs (new — CVJG0053 triggering cases)
└── CollectionMethodOverloadingTests.cs                (new — broad overload rule audit tests)
```

### Pattern 1: Second self-returning method on `AccumulatorFluentStep`

**What:** Emit a `public {StepType} WithXs(IEnumerable<T> items)` method alongside the existing `AddX(in T item)` method. Same return pattern (new struct via copy constructor). Field update uses `.AddRange(items)` instead of `.Add(item)`.

**When to use:** Every collection parameter/property that carries `[FluentCollectionMethod]` and also carries `[FluentMethod]` or `[MultipleFluentMethods]`, OR (per broad overloading rule) any time both entries land on the accumulator step and are signature-distinct.

**Example (conceptual):**
```csharp
// Source: AccumulatorStepDeclaration.CreateAddMethod (extend to emit both)
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public global::Test.Accumulator_0__Test_Builder WithTags(
    global::System.Collections.Generic.IEnumerable<string> items)
{
    return new global::Test.Accumulator_0__Test_Builder(
        this._tags__parameter.AddRange(items));
}
```

### Pattern 2: Bulk entry transition from preceding regular step

**What:** When a parameter/property has both attributes, the preceding regular step exposes *two* transition methods into the accumulator step:
- `BuildTarget()` — parameterless (existing, from Phase 22 — `AccumulatorTransitionMethod`)
- `WithTags(IEnumerable<T> items)` — parameterised, new. Constructs an `AccumulatorFluentStep` seeded with an `ImmutableArray<T>` created via `ImmutableArray.CreateRange(items)` or the existing constructor + `AddRange` path.

**Where:** `AccumulatorFluentStep` already has two constructors: an entry constructor (forwarded params only, fields init to `.Empty`) and a private copy constructor (all fields). Extend the entry constructor to accept an optional `IEnumerable<T>` seed per collection param — OR add a third overloaded constructor — OR have `WithXs` on the *preceding* step call the entry constructor then immediately call `WithXs` on the returned step. The third approach is most conservative: no new constructor, just `new AccumulatorStep().WithTags(items)` in the transition body.

**Recommendation:** Use the "call-then-WithXs" pattern (third approach) to keep `AccumulatorStepDeclaration` unchanged on the constructor side. The transition method body becomes:
```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public global::Test.Accumulator_0__Test_Builder WithTags(IEnumerable<string> items)
    => new global::Test.Accumulator_0__Test_Builder(/* forwarded fields */).WithTags(items);
```

### Pattern 3: Widen `FluentCollectionMethodAttribute` to Property

**What:** Single line change from `[AttributeUsage(AttributeTargets.Parameter)]` to `[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]`.

**Analyzer extension:** `FluentCollectionMethodAnalyzer.Analyze` today takes an `IMethodSymbol` and iterates `method.Parameters`. Extend with a sibling entry point that iterates `INamedTypeSymbol.GetMembers()` of the target type, filtering `IPropertySymbol` with the attribute. Result feeds `CollectionPropertyInfo` into `FluentTargetContext`.

**Storage path:** The accumulator step already holds `ImmutableArray<T>` backing fields. For property targets, at terminal time instead of passing the converted field as a constructor argument, emit a property assignment inside an object initializer:
```csharp
return new global::Test.Target() { Tags = this._tags__parameter.ToArray() };
```
`TargetTypeObjectCreationExpression.Create` already supports this for `PropertyInitializers` via `WithInitializer(InitializerExpression(ObjectInitializerExpression, ...))`. Extend it (or the accumulator's terminal-method builder) so that collection-property-backed parameters use the initializer instead of the constructor-argument list.

### Pattern 4: Broad overload rule — narrow collision detection to signature collisions

**What:** `FluentMethodSignatureEqualityComparer.Default` already compares by `(Name, ParamTypes, TypeParams)`. Most collision sites use name-only comparison (e.g., `TargetContextFilter.FindCollision` uses `string.Equals(parameters[i].MethodName, parameters[j].MethodName)`). Switch these to the signature-based comparator where the rule applies.

**Specific sites identified for the retrofit audit:**
- `TargetContextFilter.FilterCollectionAccumulatorCollisions` / `FindCollision` — `CollectionParameterInfo.MethodName` compared by string equality only. After Phase 23, two `[FluentCollectionMethod]` parameters with the same derived name but different element types (e.g., `IList<string> tags` and `IList<int> tags` — unlikely but legal) produce signature-distinct `AddX` overloads and must NOT emit CVJG0052. Planner: run an audit pass.
- `FluentTargetValidator.ValidateTerminalVerb` (line ~209) — groups by `(ResolvedName, ContainingType)` only. Consider whether terminal methods can ever be signature-distinct (likely they cannot, since terminals are parameterless — probably no change needed; confirm during audit).
- `FluentMethodSelector.ChooseCandidateFluentMethod` — already uses `FluentMethodSignatureEqualityComparer.Default`. No change.
- Every other `DiagnosticDescriptor` in `FluentDiagnostics.cs` whose trigger is "same name, same step, same target" must be audited. Candidates from grep: CVJG0008 (`DuplicateTerminalMethodName`), CVJG0016 (`AmbiguousFluentMethodChain`), CVJG0022 (`OptionalParameterAmbiguousFluentMethodChain`), CVJG0023 (`ConflictingTypeConstraints`), CVJG0040 (`PropertyNameClash`), CVJG0041 (`DuplicateFluentPropertyMethodName`), CVJG0043 (`AmbiguousEntryMethod`). Audit output: classify each as (a) narrow to signature collision, (b) message-update only, or (c) no change.

### Anti-Patterns to Avoid

- **Mutual-exclusion via two distinct step types for accumulator-path vs. bulk-path.** CONTEXT.md explicitly replaces this with Model D. Don't reintroduce twin-step topology.
- **Unconditional `AddRange` in a for-loop.** `ImmutableArray<T>` has a dedicated `AddRange(IEnumerable<T>)` overload — use it; don't enumerate and call `.Add` per item in the generated code.
- **Hand-rolled property set vs. object initializer.** For init-only and `required` properties, object initializer is the only legal assignment site. Use the existing `PropertyInitializers` path.
- **Introducing a new collision diagnostic** when broad overloading subsumes the case. Extend `FluentMethodSignatureEqualityComparer` usage; don't invent a per-feature diagnostic.
- **Retrofitting `AccumulatorMethod` with a `Kind` enum for `WithXs`.** Parallel types (`AccumulatorBulkMethod`) are the screaming-architecture precedent. Per the project's method-decomposition guideline, each type should have a single clear responsibility.
- **Mutation of existing `AccumulatorFluentStep.FluentMethods` shape assumptions.** `AccumulatorStepDeclaration.CreateAddMethods` uses `.OfType<AccumulatorMethod>()`. Adding `AccumulatorBulkMethod` requires either a second `.OfType<...>()` call or a shared marker interface (`IAccumulatorSelfReturningMethod`). Planner's call; marker interface keeps the generator loop singular.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Accumulating `IEnumerable<T>` into `ImmutableArray<T>` | `foreach (var i in items) arr = arr.Add(i);` | `arr.AddRange(items)` | Single call; exposed by `System.Collections.Immutable`; avoids per-element struct copies in generated code |
| Required-property terminal assignment | Synthesized constructor + setter call | Existing `TargetTypeObjectCreationExpression.Create`'s `PropertyInitializers` path | v2.0 already implemented object-initializer emission for required/init-only properties; accumulator property-targets reuse it |
| Signature equality of two fluent methods | Hand-rolled name-plus-type-list compare | `FluentMethodSignatureEqualityComparer.Default` | Already implemented; covers name + parameter type sequence + type parameter list |
| Iterating properties on a target type | Manual `GetMembers()` filtering scattered across analyzers | Existing `FluentPropertyAnalyzer` pattern | `FluentPropertyAnalyzer` establishes the member-iteration + `[Required]`/`required` detection idiom; mirror its shape for collection-property analysis |
| Record-primary-ctor property detection | Hand-rolled `IsPrimaryConstructorParameter` search | Existing `PrimaryConstructorStorageStrategy` / `RecordStorageStrategy` | Both already encode the logic for detecting primary-constructor vs. explicit-constructor storage; CVJG0053 triggers reuse this detection |

**Key insight:** The accumulator scaffolding from Phase 22 is rich. Almost every Phase 23 requirement is a composition of existing pieces: `FluentMethodSignatureEqualityComparer` + `PropertyFieldStorage` + `TargetTypeObjectCreationExpression` initializers + `AccumulatorStepDeclaration.CreateAddMethod`. The discipline is resisting the urge to build parallel infrastructure.

## Common Pitfalls

### Pitfall 1: `WithXs` overload ambiguity when element type is `IEnumerable<U>`

**What goes wrong:** For `[FluentCollectionMethod, FluentMethod] IEnumerable<IEnumerable<U>> nested` the generator emits `AddNested(IEnumerable<U> item)` and `WithNesteds(IEnumerable<IEnumerable<U>> items)`. These do not collide at the C# level, but a call site like `.WithNesteds(someList)` where `someList : IEnumerable<U>` hits C# overload resolution ambiguity.
**Why it happens:** `IEnumerable<T>` is covariant and the two overloads differ only in generic depth.
**How to avoid:** Accept the ambiguity — CONTEXT.md locks this as "leave to C# compiler." User resolves with explicit `MethodName` override. Document in the `WithXs` XML doc.
**Warning signs:** Test fixtures where the element type itself is an `IEnumerable<U>` should include at least one end-to-end compile assertion to surface the behavior, not to prevent it.

### Pitfall 2: `AddRange(IEnumerable<T>)` allocates an intermediate array for non-`ImmutableArray<T>` inputs

**What goes wrong:** `ImmutableArray<T>.AddRange(IEnumerable<T>)` without an efficient count hint may materialize into a temporary array before appending. Generated code calls it once per `WithXs`, which is acceptable but surfaces in hot-path benchmarks.
**Why it happens:** `ImmutableArray<T>` is immutable; appending requires allocating a new block sized to `Length + items.Count()`.
**How to avoid:** Accept the allocation — it's a single call; the Converj philosophy per Phase 22 RESEARCH.md (`List<T>` ruled out; `ImmutableArray<T>` chosen despite worse amortized-Add costs) already trades throughput for branch-copy correctness.
**Warning signs:** None in Phase 23 scope.

### Pitfall 3: Record primary-constructor + `[FluentCollectionMethod]` on a positional property

**What goes wrong:** `record Target(IList<string> Tags)` makes `Tags` both a primary-constructor parameter AND a property. Applying `[FluentCollectionMethod]` to the property is ambiguous: primary-ctor parameter storage is already chosen by `RecordStorageStrategy`. A terminal-time object-initializer assignment `{ Tags = ... }` collides with the positional argument.
**Why it happens:** C# records synthesize a property with an init-accessor AND require the primary-ctor argument; both paths cannot simultaneously assign.
**How to avoid:** CVJG0053 — detect this case in `FluentCollectionMethodAnalyzer` when analyzing property-target collections on a record type where the property name matches a primary-ctor parameter. Skip the property target and emit error diagnostic.
**Warning signs:** Test fixture with a record positional-collection property plus `[FluentCollectionMethod]` must surface CVJG0053, not silently generate contradictory code.

### Pitfall 4: `[MultipleFluentMethods]` variant with template-generated method name collision against `AddX`

**What goes wrong:** A `[MultipleFluentMethods]` template generates `WithXs_FromList`, `WithXs_FromEnumerable`, etc. One of these may, by template-substitution accident, collapse to the same name as the `AddX` method.
**Why it happens:** `[MultipleFluentMethods]` uses method-template reflection; generator doesn't control the final names.
**How to avoid:** Broad overload rule covers this. `AddX(in T item)` and `WithXs_FromList(IEnumerable<T> items)` are signature-distinct. If names collide AND signatures collide → CVJG0052 fires as normal.
**Warning signs:** Audit test that a `[MultipleFluentMethods]` template producing a method named literally `AddTag` (against `[FluentCollectionMethod]` parameter `tags`) with `IList<T>` parameter emits CVJG0052 correctly.

### Pitfall 5: Broad overload retrofit breaks existing diagnostic tests

**What goes wrong:** Existing `CVJG0052` test fixtures assume the old "name-only" trigger. Tightening to "name + signature" may cause some existing tests to stop emitting the diagnostic.
**Why it happens:** The retrofit deliberately narrows the trigger condition.
**How to avoid:** Audit all existing CVJG0052 (and other collision-diagnostic) tests during planning. Each test either (a) still produces identical signatures and still emits, (b) produces distinct signatures and should now produce valid overloaded output, or (c) is a legitimate collision that is now better-typed. Document every test disposition in the plan.
**Warning signs:** `BACK-01` already requires "415 existing tests continue to pass." If retrofit changes any observable output, BACK-01 compliance requires an amendment or plan-time audit trail. CONTEXT.md frames this as an expected cost; planner must list the tests that change.

### Pitfall 6: `FluentMethodSignatureEqualityComparer` ignores parameter modifiers (`in`, `ref`, `out`)

**What goes wrong:** `AddX(in T item)` (accumulator) and `AddX(T item)` (hypothetical alternative) would be considered equal under the current comparer, but the C# compiler treats them as distinct.
**Why it happens:** `FluentMethodParameter.FluentType` probably does not carry `RefKind`. Confirm during planning.
**How to avoid:** CONTEXT.md says "includes ... ref/in/out modifiers" as part of signature-distinctness. If the comparer does NOT already track modifier kind, extend it. Otherwise no action.
**Warning signs:** Inspect `FluentType` equality. If `in T` and `T` are currently equal, the retrofit must extend the comparer to include modifier.

### Pitfall 7: `[FluentCollectionMethod]` on a property still requires a collection type

**What goes wrong:** `[FluentCollectionMethod] public string Name { get; set; }` — analyzer must catch this via CVJG0050 (non-collection).
**Why it happens:** CVJG0050 is implemented for parameters. Property code path must call the same `DetectCollection` check.
**How to avoid:** Share `FluentCollectionMethodAnalyzer.DetectCollection` between parameter and property entry points. Emit CVJG0050 with property location when property type is non-collection.
**Warning signs:** No additional diagnostic needed — reuse CVJG0050 message/descriptor.

### Pitfall 8: `AccumulatorMethod` exclusion filters in traversal must now also exclude the bulk method

**What goes wrong:** Phase 22 Plan 04 explicitly excluded `AccumulatorMethod` from `GetDescendentFluentSteps`, `MarkReturnsFromMethods`, `ResolveTargetTypeReturn` (STATE.md line 62). `AccumulatorBulkMethod` is self-returning too; the same infinite-loop risk applies.
**Why it happens:** Self-returning methods create a cycle in the step graph if followed naively.
**How to avoid:** Introduce a shared marker interface (e.g., `ISelfReturningAccumulatorMethod`) implemented by both `AccumulatorMethod` and `AccumulatorBulkMethod`. Update the three exclusion sites to filter on the marker. This generalizes the guard instead of adding a new type check per site.
**Warning signs:** Build succeeds but tests hang → infinite traversal loop.

## Code Examples

### Extended `AccumulatorStepDeclaration` — emit both methods

```csharp
// Source (modified): src/Converj.Generator/SyntaxGeneration/AccumulatorStepDeclaration.cs
private static ImmutableArray<MethodDeclarationSyntax> CreateAddMethods(AccumulatorFluentStep step)
{
    var stepGlobalName = step.IdentifierDisplayString();

    var singleAdds = step.FluentMethods
        .OfType<AccumulatorMethod>()
        .Select(method => CreateAddMethod(step, method, stepGlobalName));

    var bulkAdds = step.FluentMethods
        .OfType<AccumulatorBulkMethod>()
        .Select(method => CreateBulkMethod(step, method, stepGlobalName));

    return [..singleAdds, ..bulkAdds];
}

private static MethodDeclarationSyntax CreateBulkMethod(
    AccumulatorFluentStep step,
    AccumulatorBulkMethod method,
    string stepGlobalName)
{
    var cp = method.CollectionParameter;
    var elementTypeName = cp.ElementType.ToGlobalDisplayString();
    var fieldName = cp.Parameter.Name.ToParameterFieldName();

    var ctorArgs = BuildCopyConstructorArgumentsForBulk(step, targetFieldName: fieldName);

    return MethodDeclaration(ParseTypeName(stepGlobalName), Identifier(method.Name))
        .WithAttributeLists(SingletonList(
            AttributeList(SingletonSeparatedList(AggressiveInliningAttributeSyntax.Create()))))
        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
        .WithParameterList(ParameterList(SingletonSeparatedList(
            Parameter(Identifier("items"))
                .WithType(ParseTypeName($"global::System.Collections.Generic.IEnumerable<{elementTypeName}>")))))
        .WithBody(Block(
            ReturnStatement(
                ObjectCreationExpression(ParseTypeName(stepGlobalName))
                    .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(
                        ctorArgs.InterleaveWith(Token(SyntaxKind.CommaToken))))))));
}

// BuildCopyConstructorArgumentsForBulk emits:
//   this._items__parameter.AddRange(items)
// for the target field, forwarded otherwise — parallel to BuildCopyConstructorArguments.
```

### `FluentCollectionMethodAttribute` widening

```csharp
// Source: src/Converj.Attributes/FluentCollectionMethodAttribute.cs
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class FluentCollectionMethodAttribute : Attribute { /* unchanged body */ }
```

### CVJG0053 descriptor

```csharp
// Source: src/Converj.Generator/Diagnostics/FluentDiagnostics.cs (append)
public static readonly DiagnosticDescriptor UnsupportedCollectionPropertyAccessor = new(
    id: "CVJG0053",
    title: "FluentCollectionMethod on property with unsupported accessor shape",
    messageFormat:
        "Property '{0}' on type '{1}' cannot be used as a [FluentCollectionMethod] target: "
        + "{2}. Remove [FluentCollectionMethod] or restructure the type.",
    category: "Converj",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);
```

### COMP-01/02/03 happy-path test fixture

```csharp
// Source (planned): src/Converj.Generator.Tests/CollectionMethodComposabilityTests.cs
[Fact]
internal async Task COMP_01_and_02_combined_attributes_produce_both_methods()
{
    const string code = """
        using System.Collections.Generic;
        namespace Test;
        [FluentRoot]
        public static partial class Builder { }
        public class Target
        {
            [FluentTarget(typeof(Builder))]
            public Target(
                [FluentCollectionMethod, FluentMethod] IList<string> tags) { }
        }
        """;
    // Expected emission assertions (captured via record-replay):
    //   - global::Test.Accumulator_0__Test_Builder AddTag(in string item)
    //   - global::Test.Accumulator_0__Test_Builder WithTags(IEnumerable<string> items)
    //   - Build step exposes WithTags(...) AND BuildTarget() transitions
    // Record-replay captures exact expected generated source.
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| COMP-03 mutual exclusion (twin step types per path) | Model D free composition (single accumulator step, two self-returning methods) | Phase 23 planning (2026-04-16) | REQUIREMENTS.md COMP-03 and ROADMAP.md Phase 23 goal require text amendment |
| Name-only collision detection across fluent methods | Signature-distinct overloading permitted; collision requires name AND signature match | Phase 23 broad overload rule | CVJG0052 message update; retrofit audit across all collision-diagnostic sites |
| `[FluentCollectionMethod]` parameters only | Parameters AND properties | Phase 23 | `AttributeUsage` widened; new analyzer path; new diagnostic CVJG0053 |

**Deprecated/outdated:**
- Phase 21 `FluentCollectionMethodAttribute` docstring "Only valid on constructor parameters" — must be rewritten to reflect properties also.

## Open Questions

1. **Should `AccumulatorBulkMethod` and `AccumulatorMethod` share a marker interface?**
   - What we know: Both are self-returning; both must be excluded from step-graph traversal (Pitfall 8). Phase 22 Plan 04 excluded `AccumulatorMethod` at three sites.
   - What's unclear: Whether the traversal filter pattern wants a single `ISelfReturningAccumulatorMethod` marker or duplicated `is not X and not Y` checks.
   - Recommendation: Introduce a marker. Generalizes cleanly; avoids the "add another exclusion per new method type" tax.

2. **Does `FluentMethodSignatureEqualityComparer` already distinguish `in T` from `T`?**
   - What we know: The comparer compares `FluentType` sequences. Whether `FluentType` carries `RefKind` is unverified.
   - What's unclear: Requires reading `FluentMethodParameter.FluentType` and `FluentType`'s own equality.
   - Recommendation: Plan-time check during the retrofit audit. If modifier not tracked, extend the comparer as a separate plan.

3. **Property-backed accumulator + `required` property interaction with `TerminalMethod.None`**
   - What we know: CVJG0039 blocks required-property support under `TerminalMethod.None`. Phase 23's property-target accumulator likely inherits that limitation.
   - What's unclear: Whether CVJG0039 message/scope needs a companion update for collection-property cases.
   - Recommendation: Test a `TerminalMethod.None` fixture with a required collection property; if CVJG0039 fires cleanly, no change. If it silently generates broken code, tighten the check.

4. **Exact behavior of `WithXs` when element type is itself generic with open type parameters**
   - What we know: Accumulator step carries `GenericTargetParameters` already.
   - What's unclear: Whether `WithXs`-emitted `IEnumerable<T>` with open `T` generates correctly in the open-generic-method case.
   - Recommendation: Add at least one open-generic test fixture in `CollectionMethodComposabilityTests.cs`.

5. **Should `[MultipleFluentMethods]` on a `[FluentCollectionMethod]`-marked parameter be allowed?**
   - What we know: CONTEXT.md says it composes under Model D. Each template-generated method becomes a transition.
   - What's unclear: Whether existing `[MultipleFluentMethods]` template resolution in `FluentMethodSelector` knows to route to the accumulator step rather than the regular trie.
   - Recommendation: Test a fixture with `[FluentCollectionMethod, MultipleFluentMethods(...)]`. If the selector fails to route, add an explicit redirect path in `FluentMethodBuilder` similar to the `[FluentMethod]` redirect.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (net9.0) with Roslyn `CSharpSourceGeneratorTest` verifier |
| Config file | `src/Converj.Generator.Tests/Converj.Generator.Tests.csproj` |
| Quick run command | `dotnet test --filter "FullyQualifiedName~CollectionMethodComposability" --no-build` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| COMP-01 | Both attributes accepted on same parameter (no diagnostic, both methods generated) | source-gen output | `dotnet test --filter "FullyQualifiedName~CollectionMethodComposabilityTests.COMP_01" --no-build` | Wave 0 |
| COMP-02 | Generated output contains both `AddX` and `WithXs` on accumulator step | source-gen output | `dotnet test --filter "FullyQualifiedName~CollectionMethodComposabilityTests.COMP_02" --no-build` | Wave 0 |
| COMP-03 (amended) | Both methods freely compose; interleaved calls produce correct accumulated state | source-gen output + runtime round-trip | `dotnet test --filter "FullyQualifiedName~CollectionMethodComposabilityTests.COMP_03" --no-build` | Wave 0 |
| Amendment | ROADMAP.md + REQUIREMENTS.md text updated | manual doc diff | manual review | Wave 0 (doc plan) |
| Property parity | `[FluentCollectionMethod]` on property produces same emission shape | source-gen output | `dotnet test --filter "FullyQualifiedName~PropertyBackedCollectionTests" --no-build` | Wave 0 |
| CVJG0053 | Record primary-ctor collection property emits CVJG0053 | diagnostic test | `dotnet test --filter "FullyQualifiedName~CollectionMethodPropertyAccessorDiagnosticTests" --no-build` | Wave 0 |
| Broad overload | Two signature-distinct AddX methods coexist (same name, different element types via aliased `MethodName`) | source-gen output | `dotnet test --filter "FullyQualifiedName~CollectionMethodOverloadingTests" --no-build` | Wave 0 |
| CVJG0052 narrowing | CVJG0052 fires only on name AND signature collision | diagnostic test | `dotnet test --filter "FullyQualifiedName~AccumulatorNameCollisionTests" --no-build` | Exists — needs updates |
| BACK-01 regression | All 440 existing tests continue to pass | full suite | `dotnet test` | Exists |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~{NewTestClass}" --no-build`
- **Per wave merge:** `dotnet test --filter "Category!=Slow"` (entire suite minus slow-only; Converj suite has no slow gates, so equivalent to full)
- **Phase gate:** `dotnet test` full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `src/Converj.Generator.Tests/CollectionMethodComposabilityTests.cs` — covers COMP-01, COMP-02, COMP-03 (amended)
- [ ] `src/Converj.Generator.Tests/CollectionMethodBulkSetTests.cs` — covers `WithXs` method-level emission pinning
- [ ] `src/Converj.Generator.Tests/PropertyBackedCollectionTests.cs` — covers property parity
- [ ] `src/Converj.Generator.Tests/CollectionMethodPropertyAccessorDiagnosticTests.cs` — covers CVJG0053 triggering cases
- [ ] `src/Converj.Generator.Tests/CollectionMethodOverloadingTests.cs` — covers broad overload rule generator-wide
- [ ] Update `src/Converj.Generator.Tests/AccumulatorNameCollisionTests.cs` — narrow CVJG0052 trigger to signature collision
- [ ] Update `.planning/REQUIREMENTS.md` — amend COMP-03 text; add amendment note to file header
- [ ] Update `.planning/ROADMAP.md` Phase 23 goal text
- [ ] No framework install needed (xUnit already present; `CSharpSourceGeneratorVerifier` already project-local)

## Sources

### Primary (HIGH confidence)
- `C:\Dev\Converj\.planning\phases\23-composability\23-CONTEXT.md` — User decisions (Model D, broad overload rule, property widening, CVJG0053)
- `C:\Dev\Converj\.planning\REQUIREMENTS.md` — COMP-01/02/03 requirements and BACK-01 constraint
- `C:\Dev\Converj\.planning\STATE.md` — Phase 22 decisions, `AccumulatorMethod` traversal exclusions (line 62)
- `C:\Dev\Converj\src\Converj.Generator\Models\Steps\AccumulatorFluentStep.cs` — existing accumulator step shape
- `C:\Dev\Converj\src\Converj.Generator\Models\Methods\AccumulatorMethod.cs` — single-item add method pattern
- `C:\Dev\Converj\src\Converj.Generator\Models\Methods\AccumulatorTransitionMethod.cs` — transition method pattern
- `C:\Dev\Converj\src\Converj.Generator\SyntaxGeneration\AccumulatorStepDeclaration.cs` — emission site; extension needed for bulk method
- `C:\Dev\Converj\src\Converj.Generator\SyntaxGeneration\AccumulatorCollectionConversionExpression.cs` — terminal conversion unchanged (confirmed)
- `C:\Dev\Converj\src\Converj.Generator\SyntaxGeneration\Helpers\TargetTypeObjectCreationExpression.cs` — property initializer emission path reused for property-target accumulators
- `C:\Dev\Converj\src\Converj.Generator\ModelBuilding\TargetContextFilter.cs` (line 107–155) — existing `FilterCollectionAccumulatorCollisions` + `FindCollision` — needs signature-awareness
- `C:\Dev\Converj\src\Converj.Generator\ModelBuilding\FluentMethodSignatureEqualityComparer.cs` — existing signature-based comparator (HIGH confidence: already name + param-types + type-params)
- `C:\Dev\Converj\src\Converj.Generator\ModelBuilding\FluentMethodSelector.cs` (line 72, 85) — already uses signature comparer for method selection
- `C:\Dev\Converj\src\Converj.Generator\FluentModelBuilder.cs` (lines 58–63, 127–135, 317–459) — orchestrates accumulator wiring; bulk transition insertion point
- `C:\Dev\Converj\src\Converj.Generator\TargetAnalysis\FluentCollectionMethodAnalyzer.cs` — parameter analyzer; property analyzer mirrors shape
- `C:\Dev\Converj\src\Converj.Generator\TargetAnalysis\FluentPropertyAnalyzer.cs` — property-iteration pattern to mirror
- `C:\Dev\Converj\src\Converj.Generator\TargetAnalysis\CollectionParameterInfo.cs` — parameter-side carrier; `CollectionPropertyInfo` mirrors
- `C:\Dev\Converj\src\Converj.Generator\Diagnostics\FluentDiagnostics.cs` — diagnostic registry; CVJG0052 (line 615–622) is the descriptor to update
- `C:\Dev\Converj\src\Converj.Generator\AnalyzerReleases.Unshipped.md` — release tracking; CVJG0053 appended
- `C:\Dev\Converj\src\Converj.Attributes\FluentCollectionMethodAttribute.cs` — `AttributeUsage` widening site
- `C:\Dev\Converj\src\Converj.Attributes\FluentMethodAttribute.cs` — already `Parameter | Property` (no change)
- `C:\Dev\Converj\.planning\phases\22-core-code-generation\22-RESEARCH.md` — Phase 22 architecture foundation (Conversion Table, trie exclusion pattern)

### Secondary (MEDIUM confidence)
- `C:\Dev\Converj\src\Converj.Generator\FluentModelBuilder.cs` (line 931, 941) — AccumulatorMethod traversal exclusion sites to generalize for AccumulatorBulkMethod
- `C:\Dev\Converj\src\Converj.Generator\Models\Storage\PropertyStorage.cs` — storage type reused for property-backed accumulators

### Tertiary (LOW confidence — flagged for planning verification)
- Exact list of collision diagnostics requiring narrowing to signature-based comparison (full audit deferred to planning; candidates: CVJG0008, CVJG0016, CVJG0022, CVJG0023, CVJG0040, CVJG0041, CVJG0043).
- Whether `FluentMethodSignatureEqualityComparer` currently tracks `RefKind` — confirmation deferred to planning.
- Exact CVJG0053 triggering set — record primary-ctor property confirmed; other accessor shapes require case-by-case verification during planning.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all infrastructure exists from Phase 22; no new external dependencies
- Architecture: HIGH — Model D is a minimal extension of Phase 22; file layout follows established patterns
- Pitfalls: HIGH — pitfalls derived from direct reading of Phase 22 code paths and STATE.md decisions
- Retrofit audit scope: MEDIUM — candidate list identified, per-site narrowing decisions deferred to planning
- Property-accessor CVJG0053 triggering set: MEDIUM — record primary-ctor case confirmed; secondary cases need planning-time exploration

**Research date:** 2026-04-16
**Valid until:** 2026-05-16 (stable domain; only deprecation risk is if Roslyn or `System.Collections.Immutable` API changes, neither expected)
