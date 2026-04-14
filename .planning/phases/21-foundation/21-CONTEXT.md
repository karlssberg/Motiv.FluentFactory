# Phase 21: Foundation - Context

**Gathered:** 2026-04-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver the foundation for `[FluentCollectionMethod]` in v2.2: the public attribute class, collection-type detection with element-type extraction, parameter-name singularization, all Phase 21 diagnostics, and a byte-identical backward-compatibility guard for existing code.

Covers requirements: ATTR-01, ATTR-02, ATTR-03, NAME-01, NAME-02, NAME-03, NAME-04, BACK-01, BACK-02, BACK-03.

Does NOT cover: accumulator step struct emission (Phase 22), `[FluentMethod]` coexistence (Phase 23), MinItems compile-time enforcement (Phase 24).

</domain>

<decisions>
## Implementation Decisions

### Attribute API surface

- **Class:** `public class FluentCollectionMethodAttribute : Attribute` in `Converj.Attributes` namespace, marked `[ExcludeFromCodeCoverage]`.
- **Constructors (mirror `FluentMethodAttribute` pattern):**
  - Parameterless: `[FluentCollectionMethod]` — auto-singularize from parameter name.
  - Single-string: `[FluentCollectionMethod(string methodName)]` — explicit override.
- **Properties:**
  - `public string? MethodName { get; }` — set by the single-string constructor (matches `FluentMethodAttribute.MethodName` vocabulary).
  - `public int MinItems { get; set; } = 0;` — **defined now in Phase 21** so the public attribute shape is stable across v2.2. Phase 21 parses and carries the value through to `CollectionParameterInfo` but **does not enforce** it. Phase 24 wires compile-time enforcement. No mid-milestone API breaking change.
- **AttributeUsage:** `[AttributeUsage(AttributeTargets.Parameter)]`. Parameter-only. Properties are out of scope for v2.2 — no property-backed accumulator design exists. Adding `Property` later is additive.

### Singularization rules

- **Regular suffix rules (extended set) applied in order:**
  1. `-ies` → `-y` (`categories` → `Category`)
  2. `-sses`, `-shes`, `-ches`, `-xes`, `-zes` → trim `-es` (`boxes` → `Box`, `classes` → `Class`, `buses` → `Bus`)
  3. `-ves` → `-f` / `-fe` via small curated list (`knives` → `Knife`, `wolves` → `Wolf`) — not a blanket rule; limited to known safe words to avoid false rewrites (e.g., `moves`, `loves`)
  4. `-s` (trailing only, not `-ss`) → trim (`tags` → `Tag`, `items` → `Item`)
- **Irregulars dictionary (static readonly, ~10–15 entries):** `children`→`child`, `people`→`person`, `men`→`man`, `women`→`woman`, `indices`→`index`, `matrices`→`matrix`, `analyses`→`analysis`, `theses`→`thesis`, `criteria`→`criterion`, `feet`→`foot`, `mice`→`mouse`, `geese`→`goose`, `teeth`→`tooth`. Dictionary lookup happens **before** suffix rules.
- **`data` special case:** Left as-is (falls through to error-fallback). `data` is overwhelmingly used as singular in modern C#; forcing `Datum` would surprise users. User provides explicit `[FluentCollectionMethod("AddDatum")]` or similar.
- **Output transform:** After singularization, result is passed through `.Capitalize()` to form `Add{Singular}`. Method prefix is hardcoded `Add` for Phase 21 (no override API yet).

### Singularization fallback

- **When singularization cannot produce a valid distinct identifier**, emit a **new error diagnostic** (see below) and do not generate the accumulator method. Trigger conditions:
  - Input matches no irregular and no suffix rule (already singular: `data`, `info`, `metadata`).
  - Result equals input (no transformation possible).
  - Result is empty after trimming.
  - Result is a C# keyword or reserved identifier.
- **No silent fallback.** User must provide `[FluentCollectionMethod("Name")]`. Predictable API surface; no surprise method names.

### Phase 21 model scaffolding scope (minimum)

- **Ships in Phase 21:**
  - `FluentCollectionMethodAttribute` (attributes project)
  - `FluentCollectionMethodAnalyzer` (validates collection type, extracts element type, runs singularization, emits per-parameter diagnostics)
  - `CollectionParameterInfo` record (immutable, `ImmutableArray`-safe — carries parameter symbol reference, element type, declared collection type, derived method name, `MinItems` value)
  - Extended `TargetMetadata` / `FluentTargetContext` to carry `ImmutableArray<CollectionParameterInfo>` alongside existing parameter collections
  - `StringExtensions.Singularize()`
  - All new diagnostic descriptors
  - Byte-identical snapshot test
- **Does NOT ship in Phase 21:**
  - `CollectionFluentMethodParameter` (trie parameter type) — Phase 22
  - `AccumulatorFluentStep` (`IFluentStep` impl) — Phase 22
  - `CollectionAccumulatorMethod` (`IFluentMethod` impl) — Phase 22
  - `CollectionFieldStorage` (`IFluentValueStorage` impl) — Phase 22
  - Trie-exclusion wiring in `FluentModelBuilder` — Phase 22
  - Any `SyntaxGeneration/` changes — Phase 22
- **Rationale:** Keeps Phase 21 boundary clean. Dormant scaffolding is harder to reason about than code that does nothing. Phase 22 introduces types at the moment they're exercised.

### Name-collision detection (NAME-04)

- **Scope:** Within the same target (same constructor or same static/extension method) only. Two `[FluentCollectionMethod]`-derived accumulator names on the same target that collide → error. Cross-target collisions on the same root are out of scope for Phase 21 (covered under Phase 23 composability semantics).
- **Behavior on collision:** Emit new error diagnostic, then **skip the offending target entirely** (consistent with existing `CVJG0011` UnsupportedParameterModifier pattern). Other targets on the same root continue to generate normally. Build fails (error severity) but the rest of the output remains useful.
- **Detection location:** Model-build time inside `FluentModelBuilder`, not in the analyzer. Collision requires knowing all sibling accumulator names on a target — that grouping happens during model assembly, not per-parameter analysis.

### Non-collection detection (ATTR-03)

- **Strict allowlist** of exactly six declared types (matches REQUIREMENTS.md):
  - `T[]` (via `IArrayTypeSymbol` pattern match)
  - `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>` (via `SpecialType` enum where available, else `ConstructUnboundGenericType()` + `SymbolEqualityComparer.Default`)
- **Explicitly rejected:** `string` (even though it implements `IEnumerable<char>`), `Dictionary<K,V>`, `HashSet<T>`, `Stack<T>`, `Queue<T>`, `List<T>`, `IAsyncEnumerable<T>`, any interface chain not terminating in one of the six.
- **Check logic:** The parameter's declared type must itself equal one of the allowed types — interface implementation chains are NOT walked. Avoids Pitfall 7 (string flagged as valid collection).
- **Element type extraction:**
  - `T[]` → `IArrayTypeSymbol.ElementType`
  - Generic collection → `INamedTypeSymbol.TypeArguments[0]`

### New diagnostic descriptors (all Category = `"Converj"`, severity Error)

Starting at CVJG0050 (next free after existing 0049; 0034 is the only gap but leave it reserved).

- **`CVJG0050 NonCollectionFluentCollectionMethod`** — `[FluentCollectionMethod]` applied to a parameter whose type is not one of the six allowed collection types. (ATTR-03)
- **`CVJG0051 UnsingularizableParameterName`** — Parameter name cannot be auto-singularized and no explicit `MethodName` was provided. Message includes the parameter name and instructs to provide `[FluentCollectionMethod("Name")]`. (NAME-03)
- **`CVJG0052 AccumulatorMethodNameCollision`** — Two generated accumulator method names collide on the same target. Message names both parameters and the derived name; instructs to rename via explicit `MethodName` override. (NAME-04)

All three registered in `AnalyzerReleases.Unshipped.md`.

### Backward-compatibility guard

- **In addition to the 415 existing tests passing**, add one dedicated byte-identical snapshot test: take a representative existing fixture root and assert the generated output matches a pinned expected string. Explicitly locks BACK-02 (byte-identical output for non-attributed code) and surfaces silent drift the existing tests might not catch.
- Test location: `src/Converj.Generator.Tests/` — new file following the existing `_Tests.cs` naming convention.

### File locations

- `src/Converj.Attributes/FluentCollectionMethodAttribute.cs` (new)
- `src/Converj.Generator/TargetAnalysis/FluentCollectionMethodAnalyzer.cs` (new)
- `src/Converj.Generator/Extensions/StringExtensions.cs` — extend with `Singularize()` (append to existing file, keep irregulars dictionary as a `private static readonly` field)
- `src/Converj.Generator/Diagnostics/FluentDiagnostics.cs` — append CVJG0050–0052 descriptors
- `src/Converj.Generator/AnalyzerReleases.Unshipped.md` — add three rule entries
- `src/Converj.Generator/CollectionParameterInfo.cs` or under an appropriate existing slice (planner decides exact location)
- `src/Converj.Generator.Tests/` — new singularization unit tests, new analyzer tests for ATTR-01/02/03 and NAME-01/02/03/04, new byte-identical snapshot test

### Claude's Discretion

- Exact file location of `CollectionParameterInfo` record (root of Generator project vs. `TargetAnalysis/` vs. `Models/` — whichever matches screaming-architecture conventions best).
- Exact test file names and per-requirement test count (aim for 1 file per requirement or per concern — follow the established `_Tests.cs` pattern).
- Exact expected output string for the byte-identical snapshot test (pick one representative existing fixture — `Cat`, `Dog`, or similar).
- Exact wording of diagnostic message formats (follow existing descriptor style in `FluentDiagnostics.cs`).
- How to wire `CollectionParameterInfo` through `TargetMetadata` and `FluentTargetContext` without breaking existing incremental caching (`ImmutableArray<T>` fields mandatory — Pitfall 8).

</decisions>

<specifics>
## Specific Ideas

- Attribute shape mirrors `FluentMethodAttribute` (two constructors + `MethodName` property) so consumers see a consistent Converj attribute family.
- Singularization is a static readonly dictionary + ordered suffix-rule chain — zero per-invocation allocation. Lookup happens once per collection parameter during analysis.
- Diagnostics follow the existing CVJG numbering sequence (next free = 0050). All new descriptors share the `Category = "Converj"` constant.
- Irregulars list is curated to cover ~95% of real C# identifier names; users of exotic plurals fall through to the explicit-override path.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets

- **`FluentMethodAttribute`** (`src/Converj.Attributes/FluentMethodAttribute.cs`): Template for the new attribute's shape — two constructors, nullable `MethodName` property, `[AttributeUsage]` pattern.
- **`FluentDiagnostics`** (`src/Converj.Generator/Diagnostics/FluentDiagnostics.cs`): Central descriptor registry. CVJG0011 `UnsupportedParameterModifier` is the closest pattern for the skip-target-on-error behavior.
- **`StringExtensions`** (`src/Converj.Generator/Extensions/StringExtensions.cs`): Existing `.Capitalize()` / `.ToCamelCase()` pattern — `Singularize()` lives here.
- **`CSharpSourceGeneratorVerifier<FluentRootGenerator>`**: Established test verifier pattern. Analyzer tests go through this. Diagnostic tests use `test.TestState.ExpectedDiagnostics.Add(...)`.
- **`AnalyzerReleases.Unshipped.md`**: Where new diagnostic rule entries are registered.

### Established Patterns

- **Attribute family convention:** Two-ctor shape (parameterless + single-positional-string) + public `MethodName` property with private setter via ctor.
- **Diagnostic numbering:** Sequential CVJGxxxx, Category `"Converj"`, severity Error for validation failures, Warning for best-effort issues.
- **Skip-on-error target behavior:** `CVJG0011` demonstrates emitting a diagnostic AND excluding the target from generation while other targets on the same root proceed.
- **Incremental-pipeline safety:** All model types use `ImmutableArray<T>` (Pitfall 8) — `CollectionParameterInfo` and any `TargetMetadata` additions must follow.
- **Screaming architecture slices:** `TargetAnalysis/`, `ModelBuilding/`, `SyntaxGeneration/`, `Extensions/`, `Diagnostics/`. New analyzer goes in `TargetAnalysis/`.

### Integration Points

- **`TargetMetadata.cs`** (`src/Converj.Generator/TargetMetadata.cs`): Add `ImmutableArray<CollectionParameterInfo>` field for collection parameters.
- **`FluentTargetContext`**: Extended alongside `TargetMetadata` to carry the same info downstream (Phase 22 consumer).
- **`FluentRootGenerator.cs`** Step 2 (Target Analysis): The per-parameter analyzer runs here; `FluentCollectionMethodAnalyzer` plugs in alongside existing parameter analyzers.
- **`FluentModelBuilder.cs`**: Hosts the NAME-04 collision check during model assembly (not in Phase 22's trie-exclusion logic yet — Phase 21 only needs to detect, not exclude).

</code_context>

<deferred>
## Deferred Ideas

- **Property-backed accumulators** — `[FluentCollectionMethod]` on properties. Not designed; no clear semantic for "accumulate into a property." Revisit after v2.2 ships if demand surfaces.
- **`AddRange` variant** — Single call accepting `IEnumerable<T>`. Listed as `FUTURE-01` in REQUIREMENTS.md; target v3+.
- **`params T[]` overload** — Variadic accumulator call. Listed as `FUTURE-03`; target v3+.
- **Dictionary accumulation** — `[FluentDictionaryMethod]`. Listed as `FUTURE-02`; target v3+.
- **MethodPrefix override on `[FluentCollectionMethod]`** — current design hardcodes `Add` prefix. Attribute could gain a prefix override later (additive).
- **Cross-target collision detection on the same root** — Phase 21 scopes collision detection to within a single target. Cross-target collision semantics align with Phase 23 composability work.

</deferred>

---

*Phase: 21-foundation*
*Context gathered: 2026-04-14*
