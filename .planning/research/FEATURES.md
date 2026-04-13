# Feature Research

**Domain:** Fluent collection accumulation attribute for C# Roslyn source generator
**Researched:** 2026-04-13
**Confidence:** HIGH (codebase direct inspection + Lombok/M31.FluentAPI/FluentBuilder comparison)

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Singular item-add method generated from parameter name | Every builder library (Lombok @Singular, M31 FluentCollection, FluentBuilder) generates an `AddX(T item)` method auto-named from the parameter | LOW | Name comes from singularizing the collection parameter name; e.g., `tags` → `WithTag(string tag)` |
| Auto-singularization of method name | Lombok, M31, every comparable tool do this automatically; requiring explicit name for the common case is friction | MEDIUM | Must handle irregular English plurals gracefully (e.g., `children`→`child`, `indices`→`index`); needs explicit override escape hatch |
| Explicit singular name override via attribute property | When auto-singularize fails or the word isn't English, user provides the name | LOW | `[FluentCollectionMethod("tag")]` on a `tags` parameter |
| Internal `List<T>` accumulation, convert at terminal | Standard pattern; the accumulator lives in the step struct as `List<T>`, final conversion happens when the terminal method fires | MEDIUM | Requires new `IFluentValueStorage` variant (or decorated storage) that emits `List<T>` field instead of `T` field, and conversion expression at terminal |
| Support core collection interface types | IEnumerable\<T\>, ICollection\<T\>, IList\<T\>, T[], IReadOnlyList\<T\>, IReadOnlyCollection\<T\> are listed in PROJECT.md requirements; all are table stakes | MEDIUM | Type detection via Roslyn `ITypeSymbol` — check original definition against known special type FQNs; element type extracted from first type argument |
| Error diagnostic when applied to non-collection parameter | Without this, a misconfigured attribute silently produces broken generated code | LOW | Check applied parameter's type against supported collection interfaces; emit diagnostic if no match |
| Composability: `[FluentCollectionMethod]` + `[FluentMethod]` on same parameter | M31 FluentAPI supports both bulk-set and per-item methods on same parameter; users will want both `WithTags(IEnumerable<string> tags)` and `WithTag(string tag)` | MEDIUM | `[FluentMethod]` already exists and generates the bulk-set method; `[FluentCollectionMethod]` adds the singular accumulator method alongside it |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but valued.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Configurable minimum item count (0 or 1+) | Lombok has no minimum enforcement at compile time; a Converj min-1 requirement produces a compile-time builder step that cannot be bypassed | HIGH | Minimum 1 means the first `WithX(item)` call transitions to a new step (like a required parameter). Minimum 0 means the accumulator is optional — can be called zero or more times with no step transition. Requires trie-level differentiation |
| `[FluentCollectionMethod]` with zero minimum stays on same step | Callers can chain `.WithTag("a").WithTag("b").Build()` without a required step transition; mirrors how optional methods work today | MEDIUM | Uses existing optional-method step concept: the accumulator method returns `this` (same step) rather than advancing the chain |
| Struct-backed accumulation with zero-allocation default | Converj's zero-overhead philosophy; pre-allocate null, lazy-init on first add; convert at terminal | MEDIUM | `List<T>` field initialized to null; first `.WithTag()` call allocates; conversion at terminal checks null → empty collection |
| Element-typed overload accepts `params T[]` as secondary overload | Allows `.WithTags("a", "b", "c")` in addition to `.WithTag("a")` — particularly useful for small known sets | LOW | Can be generated as a second method alongside the single-item method; no new attribute property needed |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Dictionary/Map accumulation (`AddKey(k, v)`) | Lombok @Singular supports Maps; users may request it | Significantly more complex (two type arguments, separate key/value add method, `ContainsKey` semantics); not mentioned in PROJECT.md requirements; scope creep | Defer to a future `[FluentDictionaryMethod]` attribute |
| `ClearX()` reset method on accumulator (Lombok pattern) | Lombok @Singular generates `clearXxx()` on every collection | Converj step structs are immutable (`readonly struct`); a clear method would require mutable state or return a new step with empty list — confusing for users | The immutable struct design means "don't add any items" is the natural zero-item path; no clear needed |
| Lazy/deferred-evaluation `Add(() => item)` (FluentBuilder pattern) | FluentBuilder supports `Add(() => 2)` for lazy evaluation | Adds a `Func<T>` overload, increases generated code surface, incompatible with struct-based design and zero-overhead goal | Converj is a compile-time construct; laziness is a runtime concern better addressed in user code before calling the builder |
| Nested-builder lambda for complex items (FluentBuilder pattern) | FluentBuilder supports `Add(builder => builder.With*().Build())` | Requires generated nested builder types wired to the accumulator; massively increases code generation complexity; out of scope for v2.2 | Users can construct the complex item outside the builder call and pass it directly |
| Auto-detection of collection parameters without attribute | Seems ergonomic; avoids needing an attribute at all | Changes semantics of existing generated code for all collection-typed parameters — a breaking change; opt-in attribute is explicit and backward-compatible | Keep `[FluentCollectionMethod]` as explicit opt-in |
| `ImmutableArray<T>` / `ImmutableList<T>` as accumulation target | Some APIs prefer immutable collections | Requires custom conversion logic per immutable type; immutable collection APIs differ; scope creep for v2.2 | Support the six types in PROJECT.md; user can use `IReadOnlyList<T>` which is satisfied by the generated `List<T>` without copying |

## Feature Dependencies

```
[FluentCollectionMethod] attribute (Converj.Attributes project)
    └──required by──> Collection type detection in TargetAnalysis
                          └──required by──> Accumulator storage strategy (new IFluentValueStorage variant)
                                                └──required by──> Singular method generation (TargetAnalysis + ModelBuilding)
                                                └──required by──> Terminal conversion expression (SyntaxGeneration)

[FluentCollectionMethod] min=0 (optional accumulator)
    └──depends on──> Existing OptionalFluentMethod / optional step infrastructure

[FluentCollectionMethod] + [FluentMethod] composability
    └──depends on──> Existing [FluentMethod] attribute handling (already built)
    └──depends on──> [FluentCollectionMethod] singular method (new, must exist first)

Auto-singularization
    └──feeds into──> Default singular method name
    └──override by──> Explicit name in [FluentCollectionMethod("singularName")]

Error diagnostic (non-collection target)
    └──depends on──> Collection type detection (must run first to detect mismatch)
```

### Dependency Notes

- **Accumulator storage requires new storage variant:** The current `IFluentValueStorage` implementations (FieldStorage, PropertyStorage, PrimaryConstructorParameterStorage, NullStorage) all store a single value of the declared type. Collection accumulation needs a field of `List<T>` where `T` is the element type, not a field of the declared collection type. A new storage implementation (e.g., `AccumulatorFieldStorage`) or a decorated wrapper around `FieldStorage` is needed.

- **Terminal conversion is a new code-gen concern:** Today `ReturnTypeConstructorArgumentsSyntax` passes field values directly as constructor arguments. For accumulator fields, the argument must be a conversion expression (e.g., `_tags__parameter ?? new global::System.Collections.Generic.List<string>()` for IEnumerable\<T\>, or `(_tags__parameter ?? new global::System.Collections.Generic.List<string>()).ToArray()` for `T[]`). This is a targeted addition to the terminal argument synthesis.

- **Auto-singularize depends on no external library:** Shipping a pluralization library as a bundled dependency inside the analyzer NuGet package is possible (embed the DLL in `analyzers/dotnet/cs/`) but adds build complexity. A hand-rolled rule table covering common English suffixes (`-s`, `-es`, `-ies→-y`, `-ves→-f/fe`, a small irregular word list) is simpler, more maintainable within the project's constraints, and sufficient for 95%+ of real parameter names. The explicit override covers the rest.

- **Min=1 requires trie participation:** A minimum-1 collection parameter behaves like a required parameter — the step must have at least one item added before the chain can advance. This maps onto the existing "required step" model in the trie: the first `WithTag(item)` call transitions to the next step. Min=0 is an optional accumulator that returns `this` and never transitions.

- **Composability with [FluentMethod] is additive:** `[FluentMethod]` on the same parameter generates the bulk-set overload (unchanged). `[FluentCollectionMethod]` generates the per-item accumulator. Both can coexist because they produce separate method names (e.g., `WithTags(IEnumerable<string>)` and `WithTag(string)`). No conflict at the trie level since they are different method names.

## MVP Definition

### Launch With (v2.2)

Minimum viable product — what's needed to validate the concept.

- [ ] `[FluentCollectionMethod]` attribute class in Converj.Attributes with optional `singularName` string constructor parameter and `Minimum` int property (default 0) — why essential: the whole feature is opt-in via this attribute
- [ ] Collection type detection in TargetAnalysis — checks parameter type against the six supported interfaces/array; extracts element type `T` — why essential: all downstream code generation depends on knowing element type
- [ ] Error diagnostic when applied to non-collection type — why essential: prevents silent broken output
- [ ] `AccumulatorFieldStorage` (or equivalent) — `List<T>` field, lazy-null-initialized, named with existing `ToParameterFieldName` convention — why essential: all generated methods and terminal conversion depend on this storage
- [ ] Singular item-add method in generated step struct — `public StepN WithX(T item)` that appends to the accumulator field and returns `this` (min=0) or the next step (min=1 on first call) — why essential: the core user-facing API
- [ ] Terminal conversion expression — converts the `List<T>` (or null) to the declared parameter type when the constructor/method is called — why essential: without this the terminal fires with the wrong type
- [ ] Auto-singularization via hand-rolled rule table — covers `-s/-es/-ies` plus a small irregular list — why essential: the attribute is useless without a default name; users shouldn't need to specify override for common cases
- [ ] Explicit `singularName` override respected when provided — why essential: fallback for unusual names

### Add After Validation (v2.2.x)

Features to add once core is working.

- [ ] `params T[]` secondary overload alongside the singular `WithX(T item)` — trigger: user feedback requesting multi-item convenience
- [ ] Min=1 compile-time enforcement (step transition on first add) — trigger: users who need required-minimum-one semantics report the feature gap

### Future Consideration (v2.3+)

Features to defer until product-market fit is established.

- [ ] Dictionary accumulation (`[FluentDictionaryMethod]`) — why defer: separate attribute, much higher complexity, no demand signal yet
- [ ] Immutable collection targets (ImmutableArray\<T\>, ImmutableList\<T\>) — why defer: needs per-type conversion logic; the six types in MVP cover most real APIs
- [ ] Nested-builder lambda for complex element types — why defer: requires generating new builder types wired into accumulator; major scope increase

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| `[FluentCollectionMethod]` attribute class | HIGH | LOW | P1 |
| Collection type detection + element type extraction | HIGH | LOW | P1 |
| Non-collection diagnostic | HIGH | LOW | P1 |
| AccumulatorFieldStorage (`List<T>` field) | HIGH | MEDIUM | P1 |
| Singular item-add method (min=0, returns `this`) | HIGH | MEDIUM | P1 |
| Terminal conversion to declared type | HIGH | MEDIUM | P1 |
| Auto-singularization (hand-rolled rule table) | HIGH | MEDIUM | P1 |
| Explicit `singularName` override | HIGH | LOW | P1 |
| `[FluentCollectionMethod]` + `[FluentMethod]` composability | MEDIUM | LOW | P2 |
| `params T[]` secondary overload | MEDIUM | LOW | P2 |
| Min=1 step-transition enforcement | MEDIUM | HIGH | P2 |
| Dictionary accumulation | LOW | HIGH | P3 |
| Immutable collection targets | LOW | MEDIUM | P3 |
| Nested-builder lambda for element construction | LOW | HIGH | P3 |

**Priority key:**
- P1: Must have for launch
- P2: Should have, add when possible
- P3: Nice to have, future consideration

## Competitor Feature Analysis

| Feature | Lombok @Singular (Java) | M31.FluentAPI FluentCollection (C#) | Converj v2.2 Plan |
|---------|------------------------|--------------------------------------|-------------------|
| Single-item add method | Yes — `addOccupation(String)` | Yes — `WithItem(T)` | Yes — `WithX(T)` |
| Bulk-set method | Yes — `occupations(Collection<String>)` | Yes — `WithItems(T...)` | Via existing `[FluentMethod]` composability |
| Auto-singularization | Yes — English plural rules | Manual `singularName` property | Yes — hand-rolled English rules + explicit override |
| Clear/reset method | Yes — `clearXxx()` | Yes — `WithZeroItems()` | No — struct immutability makes this a non-issue |
| Min item count | No | No | Yes — `Minimum` property (0 or 1+) |
| Immutable at build time | Yes — `Collections.unmodifiableList` | Depends on declared type | Yes — converts `List<T>` to declared interface at terminal |
| Array target support | No (Java arrays not supported) | No | Yes — `T[]` via `.ToArray()` |
| Map/Dictionary accumulation | Yes — `@Singular` on Map | No | Deferred to future |
| Null safety on empty accumulator | Yes — uses `emptyList()` | Not documented | Yes — null check → empty collection |
| Compile-time min enforcement | No (runtime) | No | P2 (min=1 via step transition) |

## Converj-Specific Infrastructure Dependencies

These are not features but implementation dependencies that the roadmap phases must sequence correctly:

| New Concern | Depends On (Existing) | Impact |
|-------------|----------------------|--------|
| `AccumulatorFieldStorage` | `IFluentValueStorage` interface, `FieldStorage` record pattern | New implementation; no changes to existing storages |
| Singular method code-gen | `FluentStepDeclaration`, `IFluentMethod` hierarchy | New `IFluentMethod` subtype or new method in `FluentStepDeclaration` |
| Terminal conversion | `ReturnTypeConstructorArgumentsSyntax` | Targeted addition: switch on `AccumulatorFieldStorage` variant |
| Attribute reading in TargetAnalysis | `FluentAttributeExtensions`, `SymbolAttributeExtensions` | New extension to read `[FluentCollectionMethod]` |
| Diagnostic | `FluentDiagnostics` | New descriptor added alongside existing 48 |
| Trie participation | `FluentMethodBuilder`, `Trie<FluentMethodParameter, TargetMetadata>` | Collection parameters still produce one trie node per parameter; accumulator is storage-level concern, not trie-level |

## Sources

- [Lombok @Singular documentation](https://projectlombok.org/features/BuilderSingular) — HIGH confidence (official)
- [M31.FluentAPI GitHub](https://github.com/m31coding/M31.FluentAPI) — HIGH confidence (official repo)
- [StefH/FluentBuilder GitHub](https://github.com/StefH/FluentBuilder) — MEDIUM confidence (official repo, WebFetch)
- [Pluralize.NET.Core NuGet](https://www.nuget.org/packages/Pluralize.NET.Core/) — HIGH confidence (netstandard2.0, no dependencies)
- Converj codebase direct inspection (`RegularFluentStep.cs`, `FluentStepDeclaration.cs`, `ReturnTypeConstructorArgumentsSyntax.cs`, `FieldStorage.cs`, `IFluentValueStorage.cs`, `FluentMethodBuilder.cs`, `ConstructorAnalyzer.cs`) — HIGH confidence

---
*Feature research for: Converj v2.2 FluentCollectionMethod*
*Researched: 2026-04-13*
