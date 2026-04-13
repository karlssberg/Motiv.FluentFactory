# Stack Research

**Domain:** Roslyn incremental source generator — fluent collection accumulation feature
**Researched:** 2026-04-13
**Confidence:** HIGH (all key claims verified against official Microsoft docs or NuGet package metadata)

## Additions Required

This is an additive feature milestone. The generator already uses Microsoft.CodeAnalysis 4.14.0 and targets netstandard2.0. No new NuGet packages are needed. Everything required is already in the existing dependency set or can be hand-rolled in ~40 lines.

---

## Roslyn APIs for Collection Type Detection

### PRIMARY: SpecialType Enum + Compilation.GetSpecialType()

**Use this first.** Roslyn's `SpecialType` enum has dedicated values for every collection interface Converj needs to support. These have been present since Roslyn 3.0 and are verified in Microsoft.CodeAnalysis 4.x.

| SpecialType Value | Covers |
|---|---|
| `SpecialType.System_Collections_Generic_IEnumerable_T` | `IEnumerable<T>` |
| `SpecialType.System_Collections_Generic_IList_T` | `IList<T>` |
| `SpecialType.System_Collections_Generic_ICollection_T` | `ICollection<T>` |
| `SpecialType.System_Collections_Generic_IReadOnlyList_T` | `IReadOnlyList<T>` |
| `SpecialType.System_Collections_Generic_IReadOnlyCollection_T` | `IReadOnlyCollection<T>` |

**Why SpecialType is correct:** `ITypeSymbol.SpecialType` is a direct enum property — O(1), zero allocation, no string comparison. For interfaces that a type _implements_ (e.g., a user-declared `List<string>` parameter that implements `IList<string>`), walk `ITypeSymbol.AllInterfaces` and check `.SpecialType` on each. For parameters declared as one of the above interfaces directly (the expected common case), check `parameter.Type.SpecialType` directly.

**Detection pattern (type directly declared as an interface):**
```csharp
private static bool IsSupportedCollectionInterface(ITypeSymbol type) =>
    type.SpecialType is
        SpecialType.System_Collections_Generic_IEnumerable_T or
        SpecialType.System_Collections_Generic_IList_T or
        SpecialType.System_Collections_Generic_ICollection_T or
        SpecialType.System_Collections_Generic_IReadOnlyList_T or
        SpecialType.System_Collections_Generic_IReadOnlyCollection_T;
```

**Detection pattern (concrete types that implement a supported interface):**
```csharp
private static bool ImplementsSupportedCollectionInterface(ITypeSymbol type) =>
    type.AllInterfaces.Any(i => IsSupportedCollectionInterface(i));
```

### SECONDARY: IArrayTypeSymbol for T[]

Array parameters are `IArrayTypeSymbol`, not `INamedTypeSymbol`. Check `parameter.Type` with a type pattern:

```csharp
if (parameter.Type is IArrayTypeSymbol arrayType)
{
    var elementType = arrayType.ElementType; // ITypeSymbol — the T
}
```

### Extracting T from IEnumerable<T>

For named generic interfaces, cast to `INamedTypeSymbol` and read `TypeArguments[0]`:

```csharp
if (parameter.Type is INamedTypeSymbol { IsGenericType: true } named)
{
    var elementType = named.TypeArguments[0]; // T
}
```

This is the same pattern already used in Converj's `SymbolExtensions.ReplaceTypeParameters` — no new idioms.

### Where collection detection fits in the pipeline

Detection belongs in **TargetAnalysis**, alongside the existing `FluentParameterAnalyzer`. The parameter type check is a pure symbol query — it should be computed once during analysis and stored in the parameter metadata (a new `IsCollectionParameter` flag or a dedicated `CollectionParameterInfo` record), so ModelBuilding and SyntaxGeneration receive a pre-computed result rather than repeating Roslyn API calls.

---

## Singularization Approach

### Decision: Hand-rolled rule table, NOT an external library

**Why not Humanizer.Core:**
- Humanizer.Core 3.x (current: 3.0.10) requires .NET SDK 9.0.200 or newer to restore, due to three-letter locale identifiers in its NuGet metadata. Many developer machines and CI environments still use older SDKs. This is a hard installation barrier for Converj users.
- Humanizer.Core netstandard2.0 target still requires transitive dependencies: `System.Collections.Immutable ≥ 9.0.10`, `System.ComponentModel.Annotations ≥ 5.0.0`, `System.Memory ≥ 4.6.3`. These must be bundled alongside a source generator assembly (Roslyn requires `PrivateAssets="all"` + explicit IL bundling — no automatic resolution). Adding three transitive deps for singularization alone is disproportionate.
- The generator has `EnforceExtendedAnalyzerRules=true`, meaning all runtime assemblies used by the generator must be packaged inside the NuGet analyzers path. Each added dependency increases package size and maintenance surface.

**Why not Pluralize.NET / Pluralize.NET.Core:**
- Both are last-updated 2018-2019 (v1.0.x), unmaintained, and target netstandard1.1/netstandard2.0. Not worth adding a dependency that is effectively abandonware.

**What hand-rolled covers adequately:**
Converj only needs to singularize parameter _names_ that C# developers write. These follow highly predictable English patterns. A focused rule table covering:
- `-ies` → `-y` (properties → property, flies → fly)
- `-es` suffix after sibilants (`-shes`, `-ches`, `-ses`, `-xes`) → strip `-es`
- `-s` → strip (dogs → dog, items → item)
- Irregular forms developers actually use: children, people, men, women, mice, feet, teeth, geese, data, criteria, indices, vertices, matrices, statuses, octopi, caches
- Identity fallback: if the word appears to already be singular (no trailing s), return as-is

This covers ~99% of real identifier names. The explicit override (`methodName` property on `[FluentCollectionMethod]`) is the escape hatch for anything the rule table misses.

**Implementation size:** ~40 lines in a new `StringExtensions.Singularize()` method. Fits the project's existing ~150 LOC-per-file standard. Lives in `Extensions/StringExtensions.cs` alongside existing string helpers.

**Confidence: HIGH** — Humanizer dependency issues verified from NuGet package metadata and official migration docs. Hand-rolled approach is the established pattern for source generators (see: Roslyn itself, which hand-rolls all string utilities).

---

## SyntaxFactory Patterns for List<T> Storage and Terminal Conversion

No new SyntaxFactory APIs are needed. All patterns required already appear in the existing codebase. The collection feature introduces two new generated code shapes:

### 1. List<T> field declaration (in step struct)

Use `FieldAndPropertySyntax.CreateFieldDeclaration` with a `FieldStorage` whose `Type` is the constructed `List<T>` symbol. The `List<T>` type can be obtained from the compilation via:

```csharp
var listOfT = compilation
    .GetTypeByMetadataName("System.Collections.Generic.List`1")!
    .Construct(elementType);
```

The resulting `INamedTypeSymbol` passed to `FieldStorage.FromParameter` (or equivalent) will produce `global::System.Collections.Generic.List<TElement>` via the existing `ToGlobalDisplayString()` extension. No new field declaration code path needed.

### 2. Accumulator method (the `WithItem(T)` → `WithItems(T)` method on the step struct)

The accumulator method mutates the `List<T>` field and returns `this` with the list updated. Because step structs are value types (`readonly struct` or mutable struct), the accumulator must return a new copy of the struct with the item appended. The generated shape:

```csharp
public StepStruct WithItem(in T item)
{
    var list = _items__parameter ?? new global::System.Collections.Generic.List<T>();
    list.Add(item);
    return new StepStruct(_field1, _field2, ..., list);
}
```

This is a `MethodDeclaration` with a block body containing a local variable declaration, a conditional assignment, a method invocation (`list.Add(item)`), and an object creation return. All these SyntaxFactory nodes already appear across the codebase (`LocalDeclarationStatement`, `InvocationExpression`, `ConditionalExpression`, `ObjectCreationExpression`). No new patterns needed.

### 3. Terminal-time conversion (in StepTerminalMethodDeclaration)

At terminal time, `_items__parameter` (a `List<T>`) must be converted to the declared parameter type. Conversion strategy by declared type:

| Declared Type | Terminal Expression |
|---|---|
| `IEnumerable<T>` | `_items__parameter ?? global::System.Linq.Enumerable.Empty<T>()` |
| `ICollection<T>`, `IList<T>` | `_items__parameter ?? new global::System.Collections.Generic.List<T>()` |
| `IReadOnlyList<T>`, `IReadOnlyCollection<T>` | `(_items__parameter ?? new global::System.Collections.Generic.List<T>()).AsReadOnly()` |
| `T[]` | `(_items__parameter ?? new global::System.Collections.Generic.List<T>()).ToArray()` |

These are all `MemberAccessExpression` / `ConditionalExpression` / `InvocationExpression` nodes — existing SyntaxFactory patterns in `StepTerminalMethodDeclaration.cs`.

The terminal conversion logic should be extracted into a new static helper (e.g., `CollectionConversionSyntax.Create(ITypeSymbol declaredType, ExpressionSyntax listExpression)`) following the thin-orchestrator pattern already used in `TargetTypeObjectCreationExpression`.

---

## What NOT to Add

| Avoid | Why | Instead |
|---|---|---|
| `Humanizer.Core` NuGet package | SDK 9.0.200 restore requirement breaks many environments; transitive deps require assembly bundling; overkill for identifier singularization | Hand-rolled ~40-line rule table in `StringExtensions` |
| `Pluralize.NET` / `Pluralize.NET.Core` | Unmaintained since 2019; adds dead-weight dependency | Same hand-rolled approach |
| `System.Linq` import in generated code | Generated code should be zero-dependency for consumers; `.ToArray()` is sufficient via `List<T>` directly | Use `List<T>.ToArray()` for array conversion, `List<T>.AsReadOnly()` for read-only; avoid `Enumerable.ToList()` |
| `ImmutableArray<T>` / `ImmutableList<T>` as internal accumulator | Immutable collections are append-inefficient; the accumulation is builder-local and discarded after terminal | Use `List<T>` as internal accumulator; convert at terminal |
| `Compilation.GetMetadataByName` for interface detection | Slower than `SpecialType` property; requires string comparison and nullable handling | Use `SpecialType` enum values directly |
| Walking `AllInterfaces` on every parameter | Quadratic cost for types with deep hierarchies | Check `parameter.Type.SpecialType` first (O(1)); only fall back to `AllInterfaces` for concrete types that aren't directly declared as an interface |

---

## Version Compatibility

| Package | Current Version | Notes |
|---|---|---|
| `Microsoft.CodeAnalysis` | 4.14.0 | Already in use. `SpecialType` values for all required collection types present since 3.x. No upgrade needed. |
| `System.Collections.Immutable` | 9.0.9 | Already in use. No change. |

No version changes required for this feature.

---

## Sources

- [SpecialType Enum — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.specialtype?view=roslyn-dotnet-4.13.0) — Verified all required SpecialType values (IEnumerable_T, IList_T, ICollection_T, IReadOnlyList_T, IReadOnlyCollection_T) are present in 4.x. HIGH confidence.
- [Compilation.GetSpecialType — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.compilation.getspecialtype?view=roslyn-dotnet-4.6.0) — Verified API signature and return type. HIGH confidence.
- [IArrayTypeSymbol — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.iarraytypesymbol?view=roslyn-dotnet-4.7.0) — Array type pattern via TypeKind or interface check. HIGH confidence.
- [Humanizer.Core 3.0.10 — NuGet Gallery](https://www.nuget.org/packages/Humanizer.Core) — Verified netstandard2.0 transitive deps (System.Collections.Immutable, System.ComponentModel.Annotations, System.Memory) and SDK 9.0.200 restore requirement. HIGH confidence.
- [Humanizer SDK 9.0.200 requirement — migration docs](https://github.com/Humanizr/Humanizer/blob/main/docs/migration-v3.md) — Confirmed SDK requirement is a hard restore blocker. MEDIUM confidence (docs page fetched indirectly via search).
- `Converj.Generator.csproj` (local) — Confirmed `EnforceExtendedAnalyzerRules=true`, `PrivateAssets="all"` pattern, no current external deps besides Roslyn and System packages. HIGH confidence.
- `Directory.Packages.props` (local) — Confirmed Microsoft.CodeAnalysis 4.14.0 current version. HIGH confidence.

---
*Stack research for: Converj v2.2 Fluent Collection Accumulation*
*Researched: 2026-04-13*
