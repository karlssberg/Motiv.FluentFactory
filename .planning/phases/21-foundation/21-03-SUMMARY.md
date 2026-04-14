---
phase: 21-foundation
plan: 03
subsystem: generator
tags: [singularization, source-generator, csharp, roslyn, collections]

# Dependency graph
requires:
  - phase: 21-01
    provides: test class stubs including SingularizationTests placeholder
provides:
  - Hand-rolled StringExtensions.Singularize() with 5-rule chain, Irregulars dict, VesExceptions dict
  - CollectionParameterInfo sealed record as ImmutableArray-safe carrier for Phase 22
  - 34 SingularizationTests theories covering NAME-01 and NAME-03 unit-level requirements
affects: [21-04, 22-foundation]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Singularize() rule chain: irregulars dict first, then suffix rules in order (-ies, suffix cluster, -ves curated, trailing -s), null fallback"
    - "netstandard2.0 record workaround: explicit get-only property body to avoid IsExternalInit dependency"
    - "PreserveCase helper mirrors first-char case from input to singularized output"

key-files:
  created:
    - src/Converj.Generator/TargetAnalysis/CollectionParameterInfo.cs
  modified:
    - src/Converj.Generator/Extensions/StringExtensions.cs
    - src/Converj.Generator.Tests/SingularizationTests.cs

key-decisions:
  - "Added -ses to suffix cluster (buses → bus) to match CONTEXT.md spec; -ses fires when length > 4 and not covered by -sses"
  - "CollectionParameterInfo uses explicit get-only property body (not auto-generated init setters) to compile under netstandard2.0 without IsExternalInit polyfill"
  - "PreserveCase applied to -ies rule output (whole stem), not just suffix character, to handle Categories → Category correctly"
  - "Removed FluentCollectionMethodAnalyzer cref from CollectionParameterInfo XML doc — analyzer not yet created (Plan 04)"

patterns-established:
  - "Singularize() returns null (not throws) for unmatched inputs — callers emit CVJG0051 diagnostic"
  - "VesExceptions curated list for -ves: words NOT in list fall through to trailing-s rule (moves → move)"
  - "series → sery (deterministic -ies rule output; analyzer layer vetoes via CVJG0051)"

requirements-completed: [NAME-01, NAME-03]

# Metrics
duration: 5min
completed: 2026-04-14
---

# Phase 21 Plan 03: Singularize Extension + CollectionParameterInfo Carrier Summary

**Hand-rolled `Singularize()` extension on `string` with 13-entry Irregulars dict, 8-entry VesExceptions curated list, and 5-rule suffix chain; plus `CollectionParameterInfo` immutable sealed record for Phase 22 consumption**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-04-14T11:32:31Z
- **Completed:** 2026-04-14T11:37:18Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- Implemented `Singularize()` with full 5-rule chain following RESEARCH.md Pattern 4: irregulars first, -ies, suffix cluster (-sses/-shes/-ches/-xes/-zes/-ses), -ves curated list, trailing -s, null fallback
- Replaced the Plan 01 `SingularizationTests` placeholder with 34 `[Theory]`/`[InlineData]` tests grouped into `RegularSuffixes`, `Irregulars`, `VesExceptions`, `VesNotInCuratedList`, `FallbackToNull`, `EmptyInput_ReturnsNull`, `NullInput_ReturnsNull`, `Series_ProducesSerY`, and `CasePreservation`
- Created `CollectionParameterInfo` sealed record in `TargetAnalysis/` as an immutable Roslyn-pipeline-safe carrier (value equality via record, no mutable fields)

## Public Signature of Singularize()

```csharp
/// <summary>
/// Attempts to singularize an English plural identifier following the rule chain:
/// (1) irregulars dict, (2) -ies→-y, (3) -sses/-shes/-ches/-xes/-zes/-ses→trim es,
/// (4) -ves→-f/-fe via curated exceptions, (5) trailing -s (not -ss)→trim.
/// Returns null when no rule fires (the caller emits CVJG0051).
/// Covers requirements NAME-01 (regular suffixes) and NAME-03 (irregulars + fallback).
/// </summary>
public static string? Singularize(this string? input)
```

## Irregulars Dictionary (13 entries, OrdinalIgnoreCase)

| Plural | Singular |
|--------|----------|
| children | child |
| people | person |
| men | man |
| women | woman |
| indices | index |
| matrices | matrix |
| analyses | analysis |
| theses | thesis |
| criteria | criterion |
| feet | foot |
| mice | mouse |
| geese | goose |
| teeth | tooth |

## VesExceptions Dictionary (8 entries, OrdinalIgnoreCase)

| Plural | Singular |
|--------|----------|
| knives | knife |
| wolves | wolf |
| leaves | leaf |
| lives | life |
| calves | calf |
| halves | half |
| selves | self |
| shelves | shelf |

## CollectionParameterInfo Signature

```csharp
internal sealed record CollectionParameterInfo(
    IParameterSymbol Parameter,
    ITypeSymbol ElementType,
    ITypeSymbol DeclaredCollectionType,
    string MethodName,
    int MinItems)
{
    public IParameterSymbol Parameter { get; } = Parameter;
    public ITypeSymbol ElementType { get; } = ElementType;
    public ITypeSymbol DeclaredCollectionType { get; } = DeclaredCollectionType;
    public string MethodName { get; } = MethodName;
    public int MinItems { get; } = MinItems;
}
```

## SingularizationTests Theory Method Names

- `RegularSuffixes` — items, tags, categories, boxes, classes, dishes, buses, events
- `Irregulars` — children, people, indices, matrices, analyses, criteria, feet, mice, teeth
- `VesExceptions` — knives, wolves, leaves, lives
- `VesNotInCuratedList` — moves, loves
- `FallbackToNull` — data, info, metadata
- `EmptyInput_ReturnsNull` — empty string
- `NullInput_ReturnsNull` — null input
- `Series_ProducesSerY` — "series" → "sery" (deterministic -ies rule; annotated why)
- `CasePreservation` — Items→Item, Children→Child, Categories→Category, Boxes→Box, Knives→Knife

## Task Commits

1. **Task 1: Add Singularize() to StringExtensions with full unit-test coverage** - `0c2d676` (feat)
2. **Task 2: Create CollectionParameterInfo record** - `ec03e73` (feat)

## Files Created/Modified

- `src/Converj.Generator/Extensions/StringExtensions.cs` — Appended `Singularize()`, `Irregulars` dict, `VesExceptions` dict, `PreserveCase()` helper
- `src/Converj.Generator.Tests/SingularizationTests.cs` — Replaced placeholder with 34 theory tests
- `src/Converj.Generator/TargetAnalysis/CollectionParameterInfo.cs` — New sealed record for Phase 22 consumption

## Decisions Made

- `-ses` added to the suffix cluster (alongside `-sses/-shes/-ches/-xes/-zes`) to correctly handle "buses" → "bus" matching the CONTEXT.md spec; guard `length > 4` prevents short false matches
- `CollectionParameterInfo` uses an explicit property body with `get;` (not auto-init setters) to compile under `netstandard2.0` without an `IsExternalInit` polyfill — consistent with all other records in the Generator project (e.g., `PropertyStorage`, `FluentRootCompilationUnit`)
- `FluentCollectionMethodAnalyzer` cref removed from XML doc — the analyzer doesn't exist until Plan 04
- Analyzer wiring (`.Singularize().Capitalize()` call chain, `CollectionParameterInfo` construction) is explicitly Plan 04's responsibility — this plan ships only the leaf dependencies

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Extended suffix cluster to include -ses for "buses" → "bus"**
- **Found during:** Task 1 (writing test for "buses")
- **Issue:** Spec lists "buses → Bus" but the 4-suffix cluster (-sses/-shes/-ches/-xes/-zes) doesn't match "buses" (ends in "-ses" not "-sses"). The trailing-s rule would produce "buse" which is wrong.
- **Fix:** Added `input.EndsWith("ses") && input.Length > 4` branch to the Rule 3 cluster. This matches the CONTEXT.md intent.
- **Files modified:** src/Converj.Generator/Extensions/StringExtensions.cs
- **Committed in:** 0c2d676 (Task 1 commit)

**2. [Rule 3 - Blocking] Fixed IsExternalInit compile error for CollectionParameterInfo**
- **Found during:** Task 2 (dotnet build verification)
- **Issue:** Pure positional record auto-generates init-only setters which require `IsExternalInit` from .NET 5+; Generator project targets netstandard2.0 without this polyfill.
- **Fix:** Added explicit get-only property body to mirror pattern used by all other records in the Generator project (PropertyStorage, etc.).
- **Files modified:** src/Converj.Generator/TargetAnalysis/CollectionParameterInfo.cs
- **Committed in:** ec03e73 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (1 bug fix, 1 blocking issue)
**Impact on plan:** Both fixes were required for correctness and compilation. No scope creep.

## Issues Encountered

None beyond the two auto-fixed deviations above.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- `Singularize()` is ready for Plan 04's `FluentCollectionMethodAnalyzer` to call `.Singularize()?.Capitalize()` and construct `CollectionParameterInfo` instances
- `CollectionParameterInfo` is ready for Plan 04 to wire into `FluentTargetContext` and `TargetMetadata`
- Keyword collision detection (CVJG0051 trigger: `"events".Singularize()` returns `"event"` → analyzer checks via `SyntaxFacts.GetKeywordKind`) is Plan 04's responsibility
- BACK-01 baseline preserved: 404 passing tests in Generator.Tests (370 pre-plan + 34 new Singularization theories), 53 in Converj.Tests

---
*Phase: 21-foundation*
*Completed: 2026-04-14*
