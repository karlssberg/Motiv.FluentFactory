# Pitfalls Research

**Domain:** Fluent collection accumulation in struct-based Roslyn source generator (Converj v2.2)
**Researched:** 2026-04-13
**Confidence:** HIGH — all pitfalls derived from direct codebase analysis

---

## Critical Pitfalls

### Pitfall 1: Struct readonly + List<T> field creates a false sense of immutability

**What goes wrong:**
`RegularFluentStep` is emitted as a `readonly struct` when no optional methods exist (see `FluentStepDeclaration.cs` line 82–98 — the `ReadOnlyKeyword` is conditionally added based on mutable optional methods). If a collection parameter's accumulator field is a `List<T>` with `readonly` in the field declaration, the struct copy semantics are preserved but the `List<T>` reference is shared across all copies. Calling `.AddItem(x)` on one copy mutates the list on all other copies that branched from the same step instance.

**Why it happens:**
`readonly` on a value type field prevents reassigning the reference, but does not prevent mutating the object the reference points to. A chain like:
```csharp
var step1 = builder.WithName("Alice");
var step2 = step1.WithTag("admin");
var step3 = step1.WithTag("user");   // branched from step1
step2.Create();  // both "admin" AND "user" may be present
step3.Create();  // same list is shared
```
This is the canonical struct + reference-type field trap. The existing generator avoids it for scalar fields by using `in` parameters and copying values. But `List<T>` is mutable through the copied reference.

**How to avoid:**
- Never store `List<T>` directly as a field in the step struct. Instead, use `ImmutableArray<T>` or `T[]` as the field type and create a new array on each `.AddItem()` call (copy-on-write via `ImmutableArray.Add`).
- Alternatively, emit the collector step as a non-readonly mutable struct (like the current optional parameter pattern) but treat it as a linear chain — enforce that accumulator steps cannot be branched.
- The simplest safe approach: emit accumulator field as `global::System.Collections.Immutable.ImmutableArray<TElement>` initialized to `ImmutableArray<TElement>.Empty`, and each `.AddX()` call returns `new Step(..., _items.Add(item))`.

**Warning signs:**
- Any generated field typed as `List<T>`, `IList<T>`, or any mutable reference-type collection.
- Steps with the `ReadOnlyKeyword` modifier but a `List<T>` field.
- Tests that check accumulation from a branched step pass when they should fail (mutation visible across branches).

**Phase to address:**
Phase 1 (CollectionStorage model design and field emission) — the field type choice must be correct before any syntax generation is written.

---

### Pitfall 2: Terminal conversion assumes the declared parameter type is directly constructible from ImmutableArray<T>

**What goes wrong:**
The terminal method at `StepTerminalMethodDeclaration.cs` reads fields and passes them as constructor arguments. If the declared parameter type is `IEnumerable<T>`, `IReadOnlyCollection<T>`, or `T[]`, the stored `ImmutableArray<T>` cannot be passed directly without a conversion expression. Emitting `new T(field1, field2, _items)` where `_items` is `ImmutableArray<string>` but the parameter expects `IEnumerable<string>` compiles only because of covariance — but `T[]` expects `string[]`, which is a different type.

**Why it happens:**
The current `StepTerminalMethodDeclaration.CreateStaticMethodInvocation` and the `TargetTypeObjectCreationExpression.Create` path both read the field by its stored type and pass it as-is. The field type for collection parameters will be `ImmutableArray<TElement>`, while the constructor parameter type may be `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `T[]`, `IReadOnlyList<T>`, or `IReadOnlyCollection<T>`. Each requires a different terminal-time conversion expression.

**How to avoid:**
In the terminal method generation, detect when a parameter's `FieldStorage.Type` differs from the `IParameterSymbol.Type`. When the stored type is `ImmutableArray<TElement>` and the declared type is:
- `IEnumerable<T>` — no cast needed (ImmutableArray implements it).
- `IReadOnlyList<T>` / `IReadOnlyCollection<T>` — no cast needed.
- `IList<T>` / `ICollection<T>` — emit `.ToList()` or `.ToArray()`.
- `T[]` — emit `ImmutableArray<T>.AsArray()` (zero-copy if the underlying array is exposed) or `.ToArray()`.
- `List<T>` — emit `new global::System.Collections.Generic.List<T>(field)`.

Introduce a `CollectionFieldStorage` subtype of `FieldStorage` (similar to `TupleFieldStorage`) that carries both the element type and the declared parameter type, and use it in `StepTerminalMethodDeclaration` to select the correct conversion.

**Warning signs:**
- Terminal method generation uses `fieldStorage.Type` directly without checking whether the parameter's declared type differs.
- Test compilation succeeds but the wrong type is constructed (e.g., `ImmutableArray` is passed to a parameter expecting `List<T>`).
- CS0029 or CS1503 compile errors in generated code.

**Phase to address:**
Phase 2 (Terminal method conversion emission) — verified by tests using each of the six declared collection types.

---

### Pitfall 3: Singularization produces invalid or colliding C# identifiers

**What goes wrong:**
Auto-singularization of parameter names like `Tags` → `Tag`, `Children` → `Child`, `Indices` → `Index`, `Analyses` → `Analysis`, `Boxes` → `Box`, `Data` → `Datum` involves irregular English plurals. A naive `TrimEnd('s')` approach produces `Tage`, `Indexe`, `Baxi`, or leaves `Data` unchanged. Even a dictionary-based approach can collide: if a constructor has both a `Tags` collection parameter and a `Tag` scalar parameter, the generated method `WithTag` would be ambiguous.

**Why it happens:**
Singularization is a linguistic problem that English irregular forms make hard to get right. The generator currently derives method names in `FluentMethodBuilder` via `parameter.GetFluentMethodName(methodPrefix)` which applies a simple `With` prefix — collection singularization is a new naming path not present in the codebase. It is easy to underestimate the edge cases.

**How to avoid:**
- Use a curated irregulars dictionary for the most common English plurals, combined with a few suffix rules (`ies` → `y`, `es` → ``, `s` → ``).
- Detect collisions at analysis time: if the singularized method name already exists as a method name on the same trie node (either from another parameter's `FluentMethodName` or from a sibling `[FluentMethod]` name), emit `CVJG00XX: collection method name 'WithTag' conflicts with existing method` and fall back to the explicit `MethodName` override.
- Require an explicit `MethodName` override in the attribute if auto-singularization produces an empty string or a C# keyword.

**Warning signs:**
- Parameters ending in `s` where `TrimEnd('s')` is applied (common stumbling point during initial impl).
- No collision check before registering the singularized name in the trie or method list.
- Missing test cases for `indices`, `data`, `children`, `boxes`, `analyses`.

**Phase to address:**
Phase 1 (Attribute analysis and name derivation) — singularization and collision detection must be in place before trie insertion.

---

### Pitfall 4: Trie equality comparison treats collection and non-collection parameters as identical when they share name and type

**What goes wrong:**
`FluentMethodParameter.Equals` (see `FluentParameter.cs`) is based on `FluentType` and `Names` overlap. If a constructor has a `string[]` collection parameter `tags` and a `string` scalar parameter `tag` with the same derived method name `WithTag`, the trie's `OrderedDictionary<TKey, TValue>` (keyed on `FluentMethodParameter`) would put them at the same trie node. The result is that the accumulator step and the regular step collide — the collection parameter's accumulator methods are never reachable.

**Why it happens:**
The `FluentMethodParameter` equality comparer was designed for the scalar world where two parameters with the same type and name are genuinely the same fluent method. But a `[FluentCollectionMethod]` on `string[]` and a `[FluentMethod]`-derived `WithTag` on `string` must occupy different trie positions because they have different arities: the collection one is called repeatedly, the scalar one is called once.

**How to avoid:**
- Introduce a `CollectionFluentMethodParameter` subclass (analogous to `TupleFluentMethodParameter`) that is never equal to a `FluentMethodParameter` with a scalar type, even if names overlap.
- In the trie insertion loop in `FluentModelBuilder.CreateFluentStepTrie`, skip collection parameters from the trie key sequence entirely — collection parameters do not advance the trie position because they are accumulated on the step, not in the chain. They are added as `OptionalFluentMethod`-style methods (accumulator setters) on the end step, not as trie nodes.
- The singularized method name must also be excluded from the method-name-based equality check used by `FluentMethodSignatureEqualityComparer` in `FluentMethodSelector.ChooseCandidateFluentMethod`.

**Warning signs:**
- Collection parameters appear as trie nodes (they should not, in most designs).
- `FluentMethodParameter.Equals` returns `true` for a `string[]` collection parameter and a `string` scalar when both derive the same method name.
- Test: a constructor with both `Tag tag` and `string[] tags` produces only one `WithTag` method.

**Phase to address:**
Phase 1 (Trie insertion and parameter model) — the decision of whether collection parameters enter the trie or sit outside it determines the entire architecture of the feature.

---

### Pitfall 5: MinItems=1 requires a distinct step type but the existing infrastructure does not have a "mandatory first call" step

**What goes wrong:**
When `MinItems=1`, the fluent chain must enforce that at least one `.AddX()` call occurs before `.Create()` is reachable. The current step model has `IsEndStep` (a `TerminalMethod` exists on the step) and `IsAllOptionalStep`. Neither maps to "terminal method unreachable until at least one accumulator method has been called." Emitting a terminal method on the same step as the first accumulator call means `MinItems=1` is not enforced at compile time — only at runtime.

**Why it happens:**
Compile-time enforcement of MinItems=1 requires two distinct step types: a "seed step" that only has the first `.AddX()` method (returning the "continuation step"), and the "continuation step" that has both `.AddX()` and `.Create()`. This is a different step topology than anything currently in the codebase.

**How to avoid:**
Two options — choose one and commit:
1. **Compile-time enforcement (two steps):** Generate `SeedStep` (only the accumulator method) returning `ContinuationStep` (accumulator + terminal). Adds complexity to model building and syntax generation. The `IsEndStep` flag alone is insufficient — the seed step needs no terminal.
2. **Runtime enforcement (one step + guard):** Generate a single step with the terminal method emitting a guard `if (_items.IsEmpty) throw new InvalidOperationException(...)`. Simpler generation, but breaks the zero-overhead guarantee and shifts validation from compile-time to runtime.

Option 1 is the right choice for Converj's zero-overhead guarantee. Plan the two-step topology explicitly. Note that `RegularFluentStep` already has `IsEndStep` — the seed step would have `IsEndStep = false` with one method, while the continuation step would have `IsEndStep = true`.

**Warning signs:**
- Only one step is created for a `MinItems=1` collection parameter.
- The terminal method is accessible before any `.AddX()` call in the generated API.
- The attribute's `MinItems` property is read but only changes a runtime guard rather than the step structure.

**Phase to address:**
Phase 2 (Step topology for MinItems) — must be explicitly designed before any step model changes; the two-step topology affects `FluentStepDeclaration`, `FluentStepConstructorDeclaration`, and `FluentModelBuilder`.

---

### Pitfall 6: Coexistence of [FluentCollectionMethod] and [FluentMethod] on the same parameter generates two methods with conflicting field storage strategies

**What goes wrong:**
A parameter decorated with both `[FluentCollectionMethod]` and `[FluentMethod]` is intended to support both item-by-item accumulation and a bulk-set replacement. The `[FluentMethod]` bulk-set would store the entire collection value in `FieldStorage` (typed to the declared parameter type, e.g., `IEnumerable<string>`), while the `[FluentCollectionMethod]` accumulator would store an `ImmutableArray<string>`. Both methods modify the same logical value but via incompatible field types. At terminal time, the generator must know which field to read — but if both exist, the terminal method would read the wrong one.

**Why it happens:**
`FluentModelBuilder` currently processes `FluentMethod` and `MultipleFluentMethods` attributes independently in `FluentMethodBuilder.CreateFluentMethods`. Adding a `[FluentCollectionMethod]` check in the same loop without explicitly resolving the field storage winner creates two storage entries for the same `IParameterSymbol`, or incorrectly overwrites one with the other.

**How to avoid:**
- When both attributes are present, designate `[FluentCollectionMethod]` as the authoritative storage owner (accumulator wins). The `[FluentMethod]` bulk-set method receives the declared parameter type as its argument, converts to `ImmutableArray<T>`, and updates the accumulator field rather than writing to a separate field.
- Alternatively, use a `CollectionFieldStorage` record that stores the current accumulated list AND supports a `.SetAll(IEnumerable<T>)` operation. Both methods target the same `CollectionFieldStorage`. The terminal method always reads the accumulator field.
- Add validation: if `[FluentMethod]` has a name override that collides with the singularized accumulator name, emit a diagnostic.

**Warning signs:**
- `ValueStorage` dictionary for a parameter decorated with both attributes contains two entries for the same `IParameterSymbol`.
- Terminal method reads `_items` (accumulator field) but `[FluentMethod]` bulk-set wrote to `_tags` (scalar field with a different name).
- CS0229 ambiguous member or silent wrong-field read in generated code.

**Phase to address:**
Phase 3 (Coexistence and [FluentMethod] interaction) — treat as a distinct integration phase after the standalone collection accumulator is working.

---

### Pitfall 7: Diagnostic for non-collection types fires on valid types due to incorrect collection detection

**What goes wrong:**
The new diagnostic for `[FluentCollectionMethod]` applied to a non-collection type must correctly identify which types are valid. A naive check like `parameter.Type is IArrayTypeSymbol` misses `List<T>`, `IEnumerable<T>`, `ICollection<T>`, etc. A check for `INamedTypeSymbol` with a generic `IEnumerable<T>` interface may incorrectly flag `string` (which implements `IEnumerable<char>`) or `Dictionary<K,V>` (which implements `IEnumerable<KeyValuePair<K,V>>`). The generator would silently accept or incorrectly reject these.

**Why it happens:**
Roslyn's type system requires walking `AllInterfaces` and checking unbound generic types. The existing `IsAssignableTo` method in `FluentTargetValidator` (line 712) already does this pattern correctly for checking base type chains, but it is not currently used for parameter-type validation. Writing collection detection from scratch without consulting the existing pattern leads to subtle misses.

**How to avoid:**
- Define an explicit allowlist: `T[]`, `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `List<T>` — exactly the six types specified in the project requirements.
- Use `SymbolEqualityComparer.Default` with unbound generic type construction (`symbol.ConstructUnboundGenericType()`) to match the interface, following the pattern in `IsAssignableTo`.
- `string` implements `IEnumerable<char>` — reject it because the element type is `char` and the outer type is `string` (a scalar), not a collection. The check should be: the parameter's type itself, or the declared type, must be one of the allowed collection types. Do not just check interface implementation.

**Warning signs:**
- `string` parameters pass the collection-type check.
- `Dictionary<K,V>` parameters pass the collection-type check.
- The diagnostic is never emitted in tests that apply `[FluentCollectionMethod]` to a `string` parameter.

**Phase to address:**
Phase 1 (Attribute analysis and validation) — the type detection logic must be locked down before the pipeline proceeds to model building.

---

### Pitfall 8: Incremental generator caching breaks when List<T> is used in a model object that participates in equality checks

**What goes wrong:**
Roslyn's incremental generator pipeline caches transformation outputs and only re-runs downstream stages when inputs change. The caching uses structural equality. If any model object carrying collection state uses `List<T>` (a mutable reference type without structural equality), the cache will always miss — every compilation triggers full regeneration — or, worse, will always hit — sharing stale list contents across compilations.

**Why it happens:**
The existing pipeline models (`TargetMetadata`, `FluentParameterBinding`, etc.) use `ImmutableArray<T>` for all collections precisely to support Roslyn's equality-based caching. Adding `List<T>` to any model type that participates in `ForAttributeWithMetadataName` transforms or in `Combine` operations silently breaks incrementality.

**How to avoid:**
- Use `ImmutableArray<T>` everywhere in the analysis and model phases.
- `List<T>` is only acceptable as a local variable inside a single method call, never as a field on a type that flows through the incremental pipeline.
- Review the `TargetMetadata` and `FluentTargetContext` types: if collection parameter metadata needs to carry element type information, add it as an `ImmutableArray<ITypeSymbol>` field and implement proper equality.

**Warning signs:**
- A model type that flows through `ForAttributeWithMetadataName` has a `List<T>` field.
- Incremental pipeline tests show full regeneration on every keystroke.
- Adding a `[FluentCollectionMethod]` parameter causes all unrelated generator outputs to regenerate.

**Phase to address:**
Phase 1 (Analysis model design) — decide on `ImmutableArray<T>` vs `List<T>` before writing any analysis code.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Runtime guard for MinItems=1 instead of two-step topology | Simpler generation code | Breaks zero-overhead guarantee; errors surface at runtime, not compile time | Never — Converj's core promise is compile-time safety |
| `List<T>` as accumulator field in step struct | No conversion in each AddX call | Shared mutable state across struct copies; incorrect branched chains | Never — struct copy semantics make this a correctness bug |
| `string.TrimEnd('s')` for singularization | Trivial to implement | Produces invalid identifiers for irregular plurals; confuses users | Never for public API; acceptable only if explicit override is always required |
| Skip CollectionFieldStorage subtype; reuse FieldStorage with a flag | Less model complexity | Terminal method emitter must contain bespoke collection-conversion logic scattered everywhere | Only if the collection type conversion logic is isolated in a single helper |
| Emit conversion only for the T[] case, skip others | 80% of real-world cases covered | ICollection<T>/IList<T> parameters silently receive ImmutableArray<T>, causing compile errors in consumer code | Never |

---

## Integration Gotchas

Specific to integrating collection accumulation with existing Converj subsystems.

| Integration Point | Common Mistake | Correct Approach |
|-------------------|----------------|------------------|
| Trie key insertion | Inserting collection parameters as trie keys, causing spurious branching in the method chain | Collection parameters must not advance trie depth; they live as accumulator methods on the existing end step |
| `FluentMethodSignatureEqualityComparer` | Treating accumulator method and scalar setter method with same name as duplicates (discarding one) | Add a distinct method marker type (e.g., `CollectionAccumulatorMethod`) so the comparer treats them as different signatures |
| `FluentStepDeclaration` readonly modifier logic | Adding `CollectionAccumulatorMethod` to `OptionalFluentMethod` check, causing accumulator steps to become mutable structs unnecessarily | Accumulator steps should be readonly structs (copy-on-write via ImmutableArray.Add) — the `hasMutableOptionalMethods` check must not include accumulator methods |
| `FluentStepConstructorDeclaration` field initialization | Initializing the `ImmutableArray<TElement>` accumulator field with `default` instead of `ImmutableArray<TElement>.Empty` | Emit `global::System.Collections.Immutable.ImmutableArray<T>.Empty` as the initializer; `default` is technically `IsDefault = true` which differs from empty and causes `foreach` to throw |
| `AddOptionalMethodsToStep` in `FluentModelBuilder` | Adding accumulator methods inside `AddOptionalMethodsToStep` alongside optional parameter setters | Accumulator methods need a distinct code path; they should not be filtered by `knownParamFieldNames` the same way optional parameters are |
| `PropertyStepEnricher.InsertRequiredPropertySteps` | Not forwarding accumulator field storage through the property step enricher | If a collection parameter is on a target that also has required properties, the enricher's step insertion must carry the accumulator field forward |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| `ImmutableArray<T>.Add` in a hot loop (consumer code, not generator code) | Quadratic allocation if user calls `.AddX()` N times | Document that `.AddX()` is O(N) copy; for bulk use, `[FluentMethod]` bulk-set is the right tool | At ~100+ items per chain; irrelevant for typical fluent builder use |
| Singularization dictionary lookup per parameter during every incremental run | Slow incremental pipeline for projects with many collection parameters | Make the singularizer a static readonly dictionary; not a per-invocation allocation | Unlikely to be observable, but good hygiene |
| Emitting `ImmutableArray<T>.ToArray()` or `.ToList()` in the terminal method | Allocation at creation time even when not needed | Use `AsArray()` when the declared type is `T[]` (zero-copy if the ImmutableArray was built from array); use the collection expression `[.._items]` for list types | Not a breaking issue but contradicts zero-overhead goal |

---

## "Looks Done But Isn't" Checklist

- [ ] **Collection field initialization:** The generated step constructor initializes the accumulator field to `ImmutableArray<TElement>.Empty`, not `default`. Verify: call `.Create()` without any `.AddX()` call and confirm zero items (not a NullReferenceException from a default ImmutableArray foreach).
- [ ] **MinItems=1 two-step topology:** The terminal method is unreachable until at least one `.AddX()` call. Verify: the generated seed step has NO terminal method and NO reference to `Create()` in its generated syntax.
- [ ] **Struct copy isolation:** Branching the chain (assigning a step to two variables and calling `.AddX()` on each independently) results in independent lists. Verify: a test that branches and adds different items to each branch, then creates from both, gets different results.
- [ ] **T[] terminal conversion:** When the declared parameter type is `T[]` and the accumulator field is `ImmutableArray<T>`, the generated terminal method contains a conversion. Verify by inspecting the generated source for `.ToArray()` or equivalent.
- [ ] **Non-collection diagnostic:** Applying `[FluentCollectionMethod]` to a `string` parameter emits the diagnostic. Verify: a test with `[FluentCollectionMethod] string name` produces exactly one `CVJGXXXX` error.
- [ ] **[FluentMethod] coexistence:** A parameter with both `[FluentCollectionMethod]` and `[FluentMethod]` generates two methods (accumulator and bulk-set) without a field-storage conflict. Verify: the generated source contains both `WithTag(in string item)` and `WithTags(in IEnumerable<string> tags)`, and `Create()` reads only one field.
- [ ] **Backward compatibility:** A root with no `[FluentCollectionMethod]` parameters generates identical output before and after the feature is added. Verify with the full existing 415-test suite — zero failures.

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| List<T> in step struct discovered late | HIGH | Rewrite field storage for all collection parameters from List<T> to ImmutableArray<T>; regenerate all AddX methods; update terminal conversion; all impacted tests must be rewritten |
| Wrong trie insertion (collection params as trie keys) | MEDIUM | Remove collection params from trie key sequence; reroute them to AddOptionalMethodsToStep analog; regenerate affected steps |
| Singularization collision causes invalid C# | LOW | Add explicit collision detection diagnostic; require user to provide MethodName override; no generator rewrite needed |
| MinItems=1 generates one step instead of two | MEDIUM | Add SeedStep concept to RegularFluentStep (IsEndStep=false variant with no terminal); split the step creation path in FluentStepBuilder |
| ImmutableArray.Empty init missing (default ImmutableArray) | LOW | Change initializer in FluentStepConstructorDeclaration collection path; one-line fix with a targeted test |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Struct readonly + List<T> shared mutation | Phase 1 — CollectionStorage model design | Struct-branch isolation test |
| Terminal conversion for non-matching declared type | Phase 2 — Terminal method emission | Six separate tests, one per allowed collection type |
| Singularization edge cases and collisions | Phase 1 — Attribute analysis and name derivation | Tests for `indices`, `data`, `children`, `boxes`; collision test |
| Trie equality collision with scalar same-name parameter | Phase 1 — Trie insertion decision | Test with constructor having both `Tag tag` and `string[] tags` |
| MinItems=1 two-step topology | Phase 2 — Step topology design | Test that seed step has no terminal; test compile error when MinItems=1 and no AddX called |
| [FluentCollectionMethod] + [FluentMethod] field storage conflict | Phase 3 — Coexistence integration | Test that bulk-set and accumulator coexist; Create() uses single field |
| Non-collection type diagnostic | Phase 1 — Validation | Diagnostic test for `string`, `int`, `Dictionary<K,V>` parameters |
| Incremental caching broken by List<T> in model | Phase 1 — Analysis model design | Incrementality test: second compilation with unchanged source produces no regeneration |

---

## Sources

- Direct code analysis: `FluentStepDeclaration.cs`, `FluentStepConstructorDeclaration.cs`, `OptionalFluentMethodDeclaration.cs`, `FluentModelBuilder.cs`, `FluentMethodBuilder.cs`, `FluentStepBuilder.cs`, `Trie.cs`, `RegularFluentStep.cs`, `FieldStorage.cs`, `IFluentValueStorage.cs`, `FluentMethodParameter.cs`, `FluentTargetValidator.cs`
- C# language specification: struct copy semantics with reference-type fields (well-established correctness concern)
- Roslyn incremental generator documentation: caching requires structural equality on pipeline model types
- ImmutableArray<T>.IsDefault vs IsEmpty: documented difference in `System.Collections.Immutable`

---

*Pitfalls research for: Converj v2.2 — [FluentCollectionMethod] collection accumulation feature*
*Researched: 2026-04-13*
