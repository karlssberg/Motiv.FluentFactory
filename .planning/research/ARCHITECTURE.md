# Architecture Research: [FluentCollectionMethod] Integration

**Domain:** Roslyn incremental source generator — fluent collection accumulation
**Researched:** 2026-04-13
**Confidence:** HIGH (based on direct codebase analysis)

---

## Existing 4-Step Pipeline Summary

Before describing integration points, here is the baseline pipeline each target traverses:

```
Step 1: Syntax Filtering (FluentRootGenerator.cs)
    ForAttributeWithMetadataName → (SyntaxNode, filePath) tuples

Step 2: Target Analysis (TargetAnalysis/)
    FluentTargetContextFactory → FluentTargetContext[]
    ConstructorAnalyzer, FluentParameterAnalyzer, FluentStorageAnalyzer
    → TargetMetadata (parameters, storage, terminal method kind)

Step 3: Model Building (ModelBuilding/ + FluentModelBuilder.cs)
    CreateFluentStepTrie → Trie<FluentMethodParameter, TargetMetadata>
    ConvertNodeToFluentFluentMethods → IFluentMethod[]
    FluentStepBuilder.ConvertNodeToFluentStep → IFluentStep[]
    → FluentRootCompilationUnit

Step 4: Syntax Generation (SyntaxGeneration/)
    CompilationUnit → per-root .g.cs file
    FluentStepDeclaration, FluentStepConstructorDeclaration, FluentStepMethodDeclaration
    StepTerminalMethodDeclaration, TargetTypeObjectCreationExpression
```

---

## New Component Overview

```
Converj.Attributes/
└── FluentCollectionMethodAttribute.cs   [NEW — Step 1 entry point]

Converj.Generator/
├── TargetAnalysis/
│   └── FluentCollectionMethodAnalyzer.cs  [NEW — Step 2]
├── Models/
│   ├── Parameters/
│   │   └── CollectionFluentMethodParameter.cs  [NEW — Step 3 model]
│   ├── Steps/
│   │   └── AccumulatorFluentStep.cs            [NEW — Step 3 model]
│   └── Methods/
│       └── CollectionAccumulatorMethod.cs      [NEW — Step 3 model]
├── ModelBuilding/
│   └── (FluentModelBuilder.cs modified — Step 3 orchestration)
└── SyntaxGeneration/
    ├── AccumulatorStepDeclaration.cs           [NEW — Step 4]
    └── AccumulatorStepMethodDeclaration.cs     [NEW — Step 4]
```

---

## Stage-by-Stage Integration

### Step 1: Syntax Filtering — No Changes Required

`FluentRootGenerator.cs` triggers on `[FluentTarget]`, not on parameter attributes. `[FluentCollectionMethod]` is a parameter attribute, so no new `ForAttributeWithMetadataName` hook is needed. The existing trigger already captures the method/constructor node that owns the annotated parameter.

**Change:** None to `FluentRootGenerator.cs`.

---

### Step 2: Target Analysis — Detection and Validation

**Where it happens:** `TargetAnalysis/` — specifically inside `FluentTargetContext` construction, which reads parameter attributes.

**New component:** `FluentCollectionMethodAnalyzer` (static class, mirrors `FluentParameterAnalyzer` pattern)

Responsibilities:
- Scan parameters of each `[FluentTarget]` method/constructor for `[FluentCollectionMethod]`
- Validate the parameter type is a recognized collection type (see collection type table below)
- Extract the element type `T` from the collection type
- Extract `MinimumItems` property from the attribute (default 0)
- Extract optional `MethodName` override (for singularization)
- Emit diagnostic `CVJG00XX` when `[FluentCollectionMethod]` is on a non-collection parameter

**Where results are stored:** `FluentTargetContext` gains a new property:
```csharp
ImmutableArray<CollectionParameterInfo> CollectionParameters { get; }
```

`CollectionParameterInfo` is a small record carrying: `IParameterSymbol Parameter`, `ITypeSymbol ElementType`, `string SingularMethodName`, `int MinimumItems`, `bool HasFluentMethodAlso`.

The `HasFluentMethodAlso` flag enables composability: when the same parameter also has `[FluentMethod]`, both an accumulator method and a bulk-set method are generated.

**Supported collection types** (validated in the analyzer):
| Declared type | Terminal conversion |
|---|---|
| `T[]` | `_list.ToArray()` |
| `IEnumerable<T>` | `_list` (List<T> implements it) |
| `ICollection<T>` | `_list` |
| `IList<T>` | `_list` |
| `IReadOnlyList<T>` | `_list.AsReadOnly()` or `_list` |
| `IReadOnlyCollection<T>` | `_list.AsReadOnly()` or `_list` |

**Propagation to TargetMetadata:** `TargetMetadata` already pulls from `FluentTargetContext`. Add:
```csharp
ImmutableArray<CollectionParameterInfo> CollectionParameters { get; }
```

**Diagnostic:** `CVJG00XX — FluentCollectionMethodOnNonCollection`

---

### Step 3: Model Building — Trie, Steps, and Methods

This is the most impacted stage. Three distinct sub-problems:

#### 3a. Trie Insertion — Collection Parameters Are Excluded

The trie key is a sequence of `FluentMethodParameter`. Collection parameters marked with `[FluentCollectionMethod]` must **not** participate in the trie as regular key segments. They do not advance the chain; instead they produce a self-returning accumulator step.

**Change in `FluentModelBuilder.CreateFluentStepTrie`:**

In the `ToFluentMethodParameter` local function, detect collection parameters and skip them from the `requiredParameters` sequence fed to `trie.Insert`. They are carried separately in `TargetMetadata.CollectionParameters`.

No changes to `Trie<TKey, TValue>` itself — its generic structure is unchanged.

#### 3b. New Model Types

**`CollectionFluentMethodParameter`** (new class, extends `FluentMethodParameter`)

Carries: element type, singular method name, declared collection type, `MinimumItems`. Used as the method parameter descriptor for the accumulator methods. Does not participate in trie keying.

**`AccumulatorFluentStep`** (new class, implements `IFluentStep`)

This is the accumulator struct step — a mutable (non-readonly) struct that:
- Holds a `List<T> _items__parameter` field (private, non-readonly)
- Carries all `KnownTargetParameters` from the preceding step as readonly fields (forwarded from the previous step's constructor)
- Exposes one or more item-addition methods returning `this` (same `AccumulatorFluentStep`)
- Exposes a terminal method that converts `_items__parameter` to the declared type and calls `new T(...)` or the static method

Why a new type rather than reusing `RegularFluentStep`: the accumulator step has fundamentally different field semantics (a `List<T>` field initialized in the constructor, mutation via `Add` calls, and a terminal that converts the list). Forcing this into `RegularFluentStep` would require special-casing throughout the step builder and all three syntax generation files. A dedicated type with dedicated syntax generation is cleaner and more maintainable.

`AccumulatorFluentStep` implements `IFluentStep`:
- `Name` / `FullName` — follows existing `Step_{Index}__{RootIdentifier}` convention, with a suffix `_Acc` to prevent collisions (e.g., `Step_0__MyRoot_Acc`)
- `KnownTargetParameters` — the accumulated regular parameters from the preceding trie path
- `FluentMethods` — the item-addition methods plus the terminal
- `ValueStorage` — regular parameter fields forwarded from preceding steps
- `ThreadedParameters`, `ReceiverParameter` — propagated normally

**`CollectionAccumulatorMethod`** (new class, implements `IFluentMethod`)

The item-addition method. Returns the same `AccumulatorFluentStep` (self-returning). Key properties:
- `Name` — singular method name (e.g., `WithDog` for a `dogs` parameter)
- `MethodParameters` — single `CollectionFluentMethodParameter` for the element type `T`
- `Return` — the same `AccumulatorFluentStep` that owns this method (circular reference)
- `AvailableParameterFields` — empty (no accumulated field for the item; it is added to the list in the method body)

#### 3c. Wiring in FluentModelBuilder

**`ConvertNodeToTerminalMethods` — new accumulator step creation:**

At trie end nodes, after creating the existing `TerminalMethod`, check whether `TargetMetadata.CollectionParameters` is non-empty. If so, wrap the normal terminal flow by inserting an `AccumulatorFluentStep` between the preceding `RegularFluentStep` and the actual `TargetTypeReturn`.

Concretely:
1. Build the `AccumulatorFluentStep`, giving it all `KnownTargetParameters` from the trie node
2. Add `CollectionAccumulatorMethod` instances to it (one per collection parameter, or more if the attribute allows multiple method names)
3. Add the terminal method to the `AccumulatorFluentStep` (it reads `this._items__parameter` and forwards all other fields)
4. Return the regular step methods as before, but their `IFluentReturn` is now the `AccumulatorFluentStep` rather than the `TargetTypeReturn`

**`FluentStepBuilder.GetDescendentFluentSteps`** — extend to traverse into `AccumulatorFluentStep`:

The current traversal excludes `OptionalFluentMethod` but otherwise follows `method.Return` that is an `IFluentStep`. Since `AccumulatorFluentStep` implements `IFluentStep`, traversal works automatically. Verify that `CollectionAccumulatorMethod.Return` does not create infinite loops — the traversal guard `Where(m => m is not OptionalFluentMethod)` should also exclude `CollectionAccumulatorMethod` since its return is the same step (like optional methods).

**`FluentModelBuilder.AddOptionalMethodsToStep`** — no changes needed; accumulator steps are added after the optional-method post-processing pass.

**`PropertyStepEnricher`** — no changes needed; required property insertion happens on `RegularFluentStep` end steps, not on accumulator steps.

---

### Step 4: Syntax Generation — New Emitters

#### 4a. `AccumulatorStepDeclaration` (new static class)

Emits the accumulator struct declaration. Key differences from `FluentStepDeclaration.Create(RegularFluentStep)`:

- Struct is **not** `readonly` (it has a mutable `List<T>` field)
- Field declarations include:
  - All regular forwarded parameter fields (same as `FieldAndPropertySyntax.CreateDeclarations(step.ValueStorage)`)
  - One `private global::System.Collections.Generic.List<T> _items__parameter` field per collection parameter
- Constructor initializes each `_items__parameter` with `new global::System.Collections.Generic.List<T>()`
- Methods are emitted via `AccumulatorStepMethodDeclaration`

#### 4b. `AccumulatorStepMethodDeclaration` (new static class)

Two method kinds emitted from the accumulator step:

**Item-addition method** (one per collection parameter):
```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public global::Ns.AccumulatorStep WithDog(in global::Ns.Dog dog)
{
    _items__parameter.Add(dog);
    return this;
}
```
This is a mutating pattern returning `this`. The struct is not readonly, so mutation is valid.

**Terminal method on AccumulatorFluentStep:**

Reads regular parameter fields from `this`, converts each `_items__parameter` field, then calls `new T(...)` or the static method. The conversion expression depends on the declared parameter type:
- `T[]` → `this._items__parameter.ToArray()`
- `IEnumerable<T>`, `ICollection<T>`, `IList<T>` → `this._items__parameter`
- `IReadOnlyList<T>`, `IReadOnlyCollection<T>` → `this._items__parameter.AsReadOnly()`

The `ToArray()` and `AsReadOnly()` calls are emitted using `global::` qualified method access to stay consistent with the codebase's `global::` convention. However, since these are instance method calls on `List<T>`, they use standard invocation syntax.

#### 4c. `CompilationUnit` / `FluentStepDeclaration` dispatch — extend switch

`FluentStepDeclaration` (and the dispatch in `CompilationUnit`) currently processes `RegularFluentStep` and `ExistingTypeFluentStep`. Add an `AccumulatorFluentStep` case:

```csharp
step switch
{
    RegularFluentStep regular => FluentStepDeclaration.Create(regular),
    ExistingTypeFluentStep existing => ExistingPartialTypeStepDeclaration.Create(existing),
    AccumulatorFluentStep accumulator => AccumulatorStepDeclaration.Create(accumulator),
    _ => throw new UnreachableSwitchException(step)
}
```

#### 4d. `FluentStepMethodDeclaration` — accumulator method forwarding

When a regular step method's `Return` is an `AccumulatorFluentStep`, `FluentStepCreationExpression.Create` needs to produce a constructor call for the accumulator step type. The constructor receives all the regular parameters forwarded from the current step. This works via the existing `CreateStepConstructorArguments` path — `AccumulatorFluentStep` exposes `KnownTargetParameters` and `ValueStorage` just like `RegularFluentStep`.

No changes to `FluentStepMethodDeclaration` itself, provided `AccumulatorFluentStep` correctly implements `IFluentReturn.IdentifierDisplayString()` and `IFluentStep`.

---

## Data Flow: Collection Parameter Through All 4 Steps

```
User writes:
  [FluentTarget(typeof(AnimalFactory))]
  public Animal(string name, [FluentCollectionMethod] IEnumerable<Tag> tags) { ... }

Step 1 (Syntax Filtering):
  Constructor node captured by existing [FluentTarget] trigger
  No new filtering needed

Step 2 (Target Analysis):
  FluentCollectionMethodAnalyzer.Analyze(constructor)
  → CollectionParameterInfo { Parameter=tags, ElementType=Tag,
                               SingularMethodName="WithTag", MinimumItems=0 }
  → Stored on FluentTargetContext.CollectionParameters
  → Propagated to TargetMetadata.CollectionParameters

Step 3 (Model Building):
  CreateFluentStepTrie:
    requiredParameters = [name]   ← tags is excluded from trie key
    trie.Insert([name], metadata)

  ConvertNodeToTerminalMethods (at end node for [name]):
    TargetMetadata.CollectionParameters = [tags info]
    → Build AccumulatorFluentStep:
         KnownTargetParameters = [name]
         ValueStorage = { name → FieldStorage("_name__parameter", string) }
         CollectionFields = [FieldStorage("_items__parameter", List<Tag>)]
    → Add CollectionAccumulatorMethod (WithTag, returns AccumulatorFluentStep)
    → Add TerminalMethod on AccumulatorFluentStep (Create, reads _name__parameter + _items__parameter.ToArray())
    → The regular step method WithName(...) returns AccumulatorFluentStep (not TargetTypeReturn)

Step 4 (Syntax Generation):
  RegularFluentStep for [name]:
    WithName(in string name) → new AccStep_0__AnimalFactory(name)

  AccumulatorFluentStep (AccStep_0__AnimalFactory):
    private string _name__parameter;
    private List<Tag> _items__parameter;
    internal AccStep_0__AnimalFactory(in string name) {
        _name__parameter = name;
        _items__parameter = new List<Tag>();
    }
    public AccStep_0__AnimalFactory WithTag(in Tag tag) {
        _items__parameter.Add(tag);
        return this;
    }
    public Animal CreateAnimal() =>
        new Animal(_name__parameter, _items__parameter);
```

---

## Composability: [FluentCollectionMethod] + [FluentMethod]

When a parameter has both attributes, the regular step for the preceding trie path has two forward-methods pointing to different return types:
- The `[FluentMethod]` (bulk set) path returns `TargetTypeReturn` directly (or the next regular step)
- The `[FluentCollectionMethod]` path returns `AccumulatorFluentStep`

This means the same `RegularFluentStep` has two different `IFluentMethod` entries for the `tags` parameter, leading to two methods:
- `WithTags(IEnumerable<Tag> tags)` → terminal (bulk set)
- `WithTag(Tag tag)` → accumulator step entry

**Implementation note:** In `FluentMethodBuilder.CreateFluentMethods`, after the existing regular-method and multi-method creation, add a new branch that checks `CollectionParameters` and creates `CollectionAccumulatorMethod` entries. The check for `ShouldCreateRegularMethod` is already attribute-driven, so detecting `[FluentCollectionMethod]` alone (without `[FluentMethod]`) suppresses the regular bulk method.

---

## New vs. Modified Components

| Component | Status | Notes |
|---|---|---|
| `FluentRootGenerator.cs` | Unchanged | Collection detection is at analysis time, not filter time |
| `Converj.Attributes/FluentCollectionMethodAttribute.cs` | **New** | Public attribute: MethodName, MinimumItems |
| `TargetAnalysis/FluentCollectionMethodAnalyzer.cs` | **New** | Validates collection type, extracts element type |
| `FluentTargetContext.cs` | **Modified** | Add `CollectionParameters` property |
| `TargetMetadata.cs` | **Modified** | Add `CollectionParameters` propagated from context |
| `Models/Parameters/CollectionFluentMethodParameter.cs` | **New** | Element-typed parameter descriptor |
| `Models/Steps/AccumulatorFluentStep.cs` | **New** | Self-returning mutable struct model |
| `Models/Methods/CollectionAccumulatorMethod.cs` | **New** | Item-addition method model (self-returning) |
| `FluentModelBuilder.cs` | **Modified** | Trie exclusion of collection params; accumulator step creation |
| `ModelBuilding/FluentStepBuilder.GetDescendentFluentSteps` | **Modified** | Exclude `CollectionAccumulatorMethod` from traversal (like `OptionalFluentMethod`) |
| `SyntaxGeneration/AccumulatorStepDeclaration.cs` | **New** | Non-readonly struct with List<T> field |
| `SyntaxGeneration/AccumulatorStepMethodDeclaration.cs` | **New** | Add-item method + terminal method emitters |
| `SyntaxGeneration/CompilationUnit.cs` | **Modified** | Dispatch `AccumulatorFluentStep` to new declaration |
| `Diagnostics/FluentDiagnostics.cs` | **Modified** | Add `FluentCollectionMethodOnNonCollection` descriptor |
| `Domain/TypeName.cs` | **Modified** | Add `FluentCollectionMethodAttribute` fully qualified name |

---

## Trie Changes

**None to the `Trie<TKey, TValue>` class itself.** The trie is generic and has no domain knowledge of collection parameters.

The changes are in how `FluentModelBuilder` populates the trie:
- Collection parameters are excluded from the `requiredParameters` sequence passed to `trie.Insert`
- They are carried as metadata on `TargetMetadata.CollectionParameters` and handled post-trie during terminal method creation

---

## Suggested Build Order

Dependencies flow: Attributes → Analysis → Models → ModelBuilding → SyntaxGeneration → Tests.

```
1. Attribute definition
   FluentCollectionMethodAttribute.cs
   Reason: Everything else depends on the attribute existing

2. Attribute recognition
   TypeName.cs — add FluentCollectionMethodAttribute constant
   Reason: Analysis code uses this constant

3. Analysis types
   CollectionParameterInfo.cs (record)
   FluentCollectionMethodAnalyzer.cs
   Reason: Must exist before FluentTargetContext can use them

4. Context and metadata propagation
   FluentTargetContext.cs — add CollectionParameters
   TargetMetadata.cs — add CollectionParameters
   Reason: Model building reads these

5. New model types (no dependencies on generation)
   CollectionFluentMethodParameter.cs
   AccumulatorFluentStep.cs
   CollectionAccumulatorMethod.cs
   Reason: Model builder creates instances; syntax generation consumes them

6. Model builder wiring
   FluentModelBuilder.cs — trie exclusion + accumulator step creation
   FluentStepBuilder.cs — exclude CollectionAccumulatorMethod from traversal
   FluentMethodBuilder.cs — add collection method creation branch
   Reason: Depends on steps 1–5; drives step 7

7. Syntax generation
   AccumulatorStepDeclaration.cs
   AccumulatorStepMethodDeclaration.cs
   CompilationUnit.cs — dispatch to new declaration
   Reason: Depends on model types from step 5; final output layer

8. Diagnostics
   FluentDiagnostics.cs — add new descriptor
   Reason: Can be added at any point but referenced from step 3; add alongside step 3

9. Tests (throughout, TDD order: test → implement → verify)
   - Diagnostic test: [FluentCollectionMethod] on non-collection parameter
   - Basic accumulation: single collection parameter
   - Element type extraction: IEnumerable<T>, IList<T>, T[], IReadOnlyList<T>
   - MinimumItems=1 validation (if validated at compile time vs runtime)
   - Composability: [FluentCollectionMethod] + [FluentMethod] on same parameter
   - Multiple collection parameters on same constructor
   - Collection parameter with other regular parameters (mixed chain)
   - Extension method target with collection parameter
   - Type-first mode with collection parameter
```

---

## Structural Invariants to Preserve

- `AccumulatorFluentStep` must implement `IFluentStep` in full, including `IdentifierDisplayString()` with `global::` prefix
- The accumulator struct must NOT be `readonly` — mutation via `_items__parameter.Add()` requires it
- `CollectionAccumulatorMethod.Return` returns the same `AccumulatorFluentStep` instance — document this circular reference clearly
- `[MethodImpl(AggressiveInlining)]` applies to item-addition methods just as to all other generated methods
- `[GeneratedCode]` attribute on the accumulator struct (same as all generated types)
- `global::System.Collections.Generic.List<T>` for the internal field type (netstandard2.0 compatible)
- `global::System.Linq.Enumerable.ToArray(this._items__parameter)` or `this._items__parameter.ToArray()` for T[] conversion — both are valid; prefer instance method form for readability
- Backward compatibility: zero changes to generated output when no `[FluentCollectionMethod]` is present

---

## Anti-Patterns to Avoid

**Do not add collection logic to `RegularFluentStep`.**
Adding `CollectionFieldStorage` as a parallel array to `RegularFluentStep` (like `PropertyFieldStorage`) would work mechanically but bleeds accumulator concerns into every syntax emitter that touches regular steps. A dedicated `AccumulatorFluentStep` keeps the boundary clean.

**Do not insert collection parameters into the trie.**
They are not ordered discriminators — the accumulator is always appended at the end of the chain. Inserting them into the trie would generate incorrect intermediate steps and break parameter-prefix merging.

**Do not use `ImmutableArray` as the internal accumulator field type.**
`ImmutableArray.Builder` or `List<T>` are the right choices. `List<T>` is simpler, well-known, and already available on netstandard2.0 without additional imports.

**Do not generate `MinimumItems` validation in the terminal method for the initial milestone.**
Compile-time validation of minimum items is not possible (the count is runtime). If `MinimumItems` enforcement is desired, emit a runtime guard (`if (_items__parameter.Count < N) throw...`) — but only if the PROJECT.md scope explicitly includes it. Avoid adding uncalled-for complexity.

---

*Architecture research for: Converj v2.2 [FluentCollectionMethod] integration*
*Researched: 2026-04-13*
