# Phase 7: Core Pipeline Decomposition - Research

**Researched:** 2026-03-10
**Domain:** C# Roslyn source generator refactoring -- god class decomposition
**Confidence:** HIGH

## Summary

Phase 7 decomposes three god classes (FluentModelFactory 438 lines, FluentFactoryGenerator 376 lines, ConstructorAnalyzer 210 lines) into focused, single-responsibility types. This is a behavior-preserving refactoring where all existing tests must pass and generated output must remain identical.

The codebase targets netstandard2.0 (required for Roslyn analyzers/generators) and uses primary constructors, LINQ-heavy patterns, and the IIncrementalGenerator pipeline. The existing code is well-structured with clear method boundaries, making extraction straightforward -- most methods are already static or have obvious data flow. The main risk is breaking the diagnostic descriptor references (10 static fields on FluentFactoryGenerator referenced from 5 files) and maintaining the exact same wiring between the orchestrator and its new collaborators.

**Primary recommendation:** Follow the data-flow order decided in CONTEXT.md: FluentFactoryGenerator first (simplest, just moving static fields and extracting methods), then ConstructorAnalyzer (strategy pattern extraction), then FluentModelFactory (most complex, depends on stable upstream types). Run full test suite after each class is complete.

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions

**FluentModelFactory Decomposition (438 lines)**
- Orchestrator pattern: FluentModelFactory stays as a thin orchestrator that owns shared state (_regularFluentSteps, _diagnostics, _unreachableConstructorAnalyzer) and delegates to focused types
- FluentMethodSelector: Single focused type for method selection -- ChooseCandidateFluentMethod, CreateFluentMethods, ValidateMultipleFluentMethodCompatibility, MergeConstructorMetadata, NormalizedConverterMethod, and the SelectedFluentMethod record all move here
- FluentStepBuilder: Handles node-to-step conversion (ConvertNodeToFluentStep, CreateStep, GetDescendentFluentSteps) plus storage resolution (CreateRegularStepValueStorage, GetValueStorages). Calls FluentMethodSelector to get methods for each node
- Trie construction stays in orchestrator: CreateFluentStepTrie (~25 lines) is too small to extract
- Helpers move to consumers: Each helper method moves to the type that uses it, not a shared utility class
- Constructor injection for Compilation: Each extracted type that needs Compilation receives it via its constructor

**FluentFactoryGenerator Decomposition (376 lines)**
- FluentDiagnostics: All 10 diagnostic descriptors (MFFG0001-MFFG0010) move to a dedicated static class in Diagnostics/
- FluentConstructorContextFactory: Context creation (CreateConstructorContexts, CreateContainingTypeFluentConstructorContexts), metadata extraction (GetFluentFactoryMetadata, ConvertToFluentFactoryGeneratorOptions), and de-duplication (DeDuplicateFluentConstructors, ChooseOverridingConstructors) all move to a single class in Analysis/
- FluentFactoryGenerator stays at ~70 lines: After extraction, it remains a clean IIncrementalGenerator entry point with Initialize() pipeline setup and Execute()

**ConstructorAnalyzer Decomposition (210 lines)**
- Strategy per detection pattern: Each storage detection strategy (records, primary constructors, explicit constructor bodies) becomes its own type implementing a common interface
- SemanticModel as method parameter: Strategy interface methods receive SemanticModel as a parameter rather than constructor injection
- Initializer chain stays in dispatcher: The `: base()` / `: this()` chain resolution stays in ConstructorAnalyzer.FindParameterValueStorage since it recursively calls itself
- 3 strategies extracted: RecordStorageStrategy, PrimaryConstructorStorageStrategy, ExplicitConstructorStorageStrategy

**Execution Plan**
- 3 separate plans, one per class: 07-01 (FluentFactoryGenerator), 07-02 (ConstructorAnalyzer), 07-03 (FluentModelFactory)
- Follow data flow order: Generator (upstream, simplest) -> ConstructorAnalyzer (middle, moderate) -> FluentModelFactory (downstream, most complex)
- Same directories: New types go next to the class they were extracted from

### Claude's Discretion
- Exact method signatures for extracted types
- Internal visibility and access modifiers
- Whether small shared helpers (GetInitializerSyntax, IsInitializedFromParameter) become part of a strategy or stay as private helpers
- Exact naming of strategy interface (e.g., IStorageDetectionStrategy vs IValueStorageResolver)

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope

</user_constraints>

<phase_requirements>

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| DECOMP-01 | FluentModelFactory is decomposed into focused types with single responsibilities (method selection, step building, trie construction, storage resolution) | Plan 07-03 extracts FluentMethodSelector and FluentStepBuilder; orchestrator retains trie construction and shared state |
| DECOMP-02 | FluentFactoryGenerator pipeline stages are extracted into distinct, named types | Plan 07-01 extracts FluentDiagnostics (static class) and FluentConstructorContextFactory |
| DECOMP-03 | ConstructorAnalyzer storage detection patterns are separated into focused types | Plan 07-02 extracts 3 strategy types implementing a common interface |
| XCUT-01 | All existing tests continue to pass -- behavior-preserving refactor | Full test suite run after each plan completes; tests are end-to-end source generator verification tests |
| XCUT-02 | Generated .g.cs output is identical before and after refactoring | Tests verify exact generated output via CSharpSourceGeneratorVerifier -- passing tests guarantees identical output |

</phase_requirements>

## Architecture Patterns

### Recommended Project Structure (new files)

After phase 7, the generator project gains these new files (placed next to their origin class):

```
src/Motiv.FluentFactory.Generator/
  Diagnostics/
    FluentDiagnostics.cs           # NEW: 10 DiagnosticDescriptor static fields (from FluentFactoryGenerator)
  Analysis/
    ConstructorAnalyzer.cs          # SIMPLIFIED: thin dispatcher + initializer chain logic
    FluentConstructorContextFactory.cs  # NEW: context creation, metadata, de-duplication (from FluentFactoryGenerator)
    RecordStorageStrategy.cs        # NEW: record parameter -> property storage detection
    PrimaryConstructorStorageStrategy.cs  # NEW: primary ctor -> field/property storage detection
    ExplicitConstructorStorageStrategy.cs # NEW: explicit ctor body -> assignment storage detection
  Model/
    FluentModelFactory.cs           # SIMPLIFIED: thin orchestrator
    FluentMethodSelector.cs         # NEW: method selection, validation, merging
    FluentStepBuilder.cs            # NEW: node-to-step conversion, storage resolution
```

### Pattern 1: Orchestrator with Collaborators (FluentModelFactory)

**What:** FluentModelFactory becomes a thin orchestrator that owns shared mutable state and coordinates FluentMethodSelector and FluentStepBuilder.
**When to use:** When a class has multiple responsibilities that share state but have distinct behavior.

The orchestrator owns:
- `_regularFluentSteps` (OrderedDictionary)
- `_diagnostics` (DiagnosticList)
- `_unreachableConstructorAnalyzer` (UnreachableConstructorAnalyzer)
- `CreateFluentStepTrie()` (~25 lines, stays in orchestrator)
- `CreateFluentFactoryCompilationUnit()` (public entry point, stays)
- `GetUsingStatements()` (static helper, stays)

FluentMethodSelector receives Compilation via constructor. Its methods need `_diagnostics` and `_unreachableConstructorAnalyzer` -- these are passed as parameters or the selector returns results that the orchestrator processes.

FluentStepBuilder receives references to `_regularFluentSteps` and calls FluentMethodSelector. It needs the trie nodes and root type.

**Key design decision:** The orchestrator creates both collaborators and passes shared state references. Collaborators do not own mutable state -- they either receive it as parameters or return values that the orchestrator aggregates.

```csharp
// Orchestrator wiring sketch
internal class FluentModelFactory(Compilation compilation)
{
    private readonly DiagnosticList _diagnostics = [];
    private readonly OrderedDictionary<ParameterSequence, RegularFluentStep> _regularFluentSteps = new();
    private readonly UnreachableConstructorAnalyzer _unreachableConstructorAnalyzer = new();
    private readonly FluentMethodSelector _methodSelector = new(compilation);
    private readonly FluentStepBuilder _stepBuilder;

    // _stepBuilder needs _methodSelector, _regularFluentSteps, _unreachableConstructorAnalyzer
    // Wire in constructor or pass per-call
}
```

### Pattern 2: Strategy Pattern (ConstructorAnalyzer)

**What:** Each storage detection pattern (records, primary constructors, explicit constructors) becomes a strategy implementing a common interface.
**When to use:** When a method has a chain of if/else or switch blocks handling distinct cases.

Current code in `FindParameterValueStorage` has three clear branches:
1. `if (containingType.IsRecord)` -- lines 23-41
2. `if (syntaxNode is TypeDeclarationSyntax { ParameterList: not null })` -- lines 44-51
3. `if (syntaxNode is ConstructorDeclarationSyntax ctorSyntax)` -- lines 54-58

Each becomes a strategy. The interface receives SemanticModel as a method parameter (not constructor injection) since strategies should be lightweight/stateless.

```csharp
// Strategy interface
internal interface IStorageDetectionStrategy
{
    /// <summary>
    /// Determines whether this strategy can handle the given constructor.
    /// </summary>
    bool CanHandle(IMethodSymbol constructor, SemanticModel semanticModel);

    /// <summary>
    /// Populates value storage for constructor parameters using this detection pattern.
    /// </summary>
    void PopulateStorage(
        IMethodSymbol constructor,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> results,
        SemanticModel semanticModel);
}
```

The dispatcher (ConstructorAnalyzer) iterates strategies, finds the first match, populates storage, then handles the initializer chain (`: base()` / `: this()`) as a post-step since it recursively calls `FindParameterValueStorage`.

### Pattern 3: Static Diagnostic Descriptor Holder (FluentDiagnostics)

**What:** All 10 DiagnosticDescriptor static fields move from FluentFactoryGenerator to a dedicated static class.
**When to use:** When static data pollutes a class with unrelated behavioral responsibilities.

This is the simplest extraction. The class is purely a container for static readonly fields. All references in 5 files update their using/qualifier.

```csharp
namespace Motiv.FluentFactory.Generator.Diagnostics;

/// <summary>
/// Diagnostic descriptors for the fluent factory source generator.
/// </summary>
public static class FluentDiagnostics
{
    private const string Category = "FluentFactory";

    public static readonly DiagnosticDescriptor UnreachableConstructor = new(
        id: "MFFG0001", ...);
    // ... all 10 descriptors
}
```

**References that must update:**
- `IgnoredMultiMethodWarningFactory.cs` (2 references)
- `UnreachableConstructorAnalyzer.cs` (1 reference)
- `FluentConstructorValidator.cs` (4 references)
- `FluentModelFactory.cs` (1 reference)
- `Model/SymbolExtensions.cs` (2 references)

### Anti-Patterns to Avoid

- **Passing the orchestrator itself to collaborators:** Creates circular dependency. Pass specific data/callbacks, not the whole orchestrator.
- **Making strategies own SemanticModel:** The CONTEXT.md explicitly decided SemanticModel should be a method parameter, keeping strategies lightweight.
- **Extracting too much from the orchestrator:** CreateFluentStepTrie (~25 lines) and GetUsingStatements (static) stay in the orchestrator. Do not over-decompose.
- **Creating shared utility classes for helpers:** Each helper method moves to the type that uses it (per CONTEXT.md decision).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Diagnostic descriptors | Custom diagnostic infrastructure | DiagnosticDescriptor static fields in FluentDiagnostics | Standard Roslyn pattern, already works |
| Strategy dispatch | Complex visitor or dynamic dispatch | Simple interface + foreach loop in dispatcher | Three strategies is too few for a visitor |
| State sharing | Event-based or mediator pattern | Direct parameter passing from orchestrator | The collaborators are called synchronously in a single method |

## Common Pitfalls

### Pitfall 1: Breaking Diagnostic References During FluentDiagnostics Extraction

**What goes wrong:** Moving DiagnosticDescriptor fields from FluentFactoryGenerator to FluentDiagnostics but missing a reference, causing a compile error.
**Why it happens:** 10 references spread across 5 files, easy to miss one.
**How to avoid:** After moving, do a full build. The compiler will catch any missed references since these are static field accesses. Also note that FluentFactoryGenerator is a `public` class while the new FluentDiagnostics should also be `public` (descriptors are used in test assertions).
**Warning signs:** Compile errors referencing `FluentFactoryGenerator.SomeDiagnostic`.

### Pitfall 2: Losing Mutable State Semantics in FluentModelFactory Decomposition

**What goes wrong:** FluentModelFactory mutates `_diagnostics`, `_regularFluentSteps`, and `_unreachableConstructorAnalyzer` across method calls. If collaborators receive copies instead of references, side effects are lost.
**Why it happens:** C# value types and immutable collections can accidentally break shared mutation.
**How to avoid:** Pass the mutable collections by reference (they are already reference types -- DiagnosticList, OrderedDictionary, UnreachableConstructorAnalyzer). Collaborator methods either mutate the passed-in collection or return results that the orchestrator aggregates.
**Warning signs:** Tests pass individually but fail when multiple constructors interact (the state accumulation is the key behavior).

### Pitfall 3: Strategy Ordering Matters in ConstructorAnalyzer

**What goes wrong:** The three detection branches in ConstructorAnalyzer are not interchangeable -- records must be checked first, then primary constructors, then explicit constructors.
**Why it happens:** A record with a primary constructor would match both the record check and the primary constructor check.
**How to avoid:** The strategy list must be ordered: [RecordStorageStrategy, PrimaryConstructorStorageStrategy, ExplicitConstructorStorageStrategy]. Use first-match semantics.
**Warning signs:** Record types getting field storage instead of property storage.

### Pitfall 4: Initializer Chain Recursion Must Stay in Dispatcher

**What goes wrong:** Trying to move the `: base()` / `: this()` chain resolution into a strategy.
**Why it happens:** It looks like it belongs with the explicit constructor strategy.
**How to avoid:** Per CONTEXT.md decision, the chain resolution stays in ConstructorAnalyzer.FindParameterValueStorage because it recursively calls itself to resolve the full chain. Strategies only handle their single pattern.
**Warning signs:** Stack overflow or incorrect storage resolution for constructors that chain.

### Pitfall 5: FluentConstructorContextFactory Must Preserve Static Method Semantics

**What goes wrong:** The methods being extracted from FluentFactoryGenerator (CreateConstructorContexts, GetFluentFactoryMetadata, etc.) are all static. If they become instance methods unnecessarily, it changes the threading model.
**Why it happens:** Reflex to make everything instance-based when creating a new class.
**How to avoid:** FluentConstructorContextFactory should be a static class or have all static methods. These methods are called from the IIncrementalGenerator pipeline which requires statelessness.
**Warning signs:** Thread-safety issues in the generator pipeline (hard to detect in tests since tests are single-threaded).

### Pitfall 6: netstandard2.0 Constraints

**What goes wrong:** Using C# features not available in netstandard2.0 compilation target.
**Why it happens:** The generator targets netstandard2.0 even though the codebase uses modern C# syntax.
**How to avoid:** The project already uses modern C# features (primary constructors, collection expressions, pattern matching) via LangVersion settings. New code should follow the same patterns already established in the codebase. Do not introduce new dependencies.
**Warning signs:** Build errors on the generator project.

## Code Examples

### FluentMethodSelector Extraction Shape

```csharp
// Source: Derived from FluentModelFactory.cs analysis
namespace Motiv.FluentFactory.Generator.Model;

/// <summary>
/// Handles fluent method selection, validation, and merging for a given compilation.
/// </summary>
internal class FluentMethodSelector(Compilation compilation)
{
    /// <summary>
    /// Creates fluent methods for a trie node's children parameters.
    /// </summary>
    public IEnumerable<IFluentMethod> CreateFluentMethods(
        INamedTypeSymbol rootType,
        Trie<FluentMethodParameter, ConstructorMetadata>.Node node,
        ICollection<FluentMethodParameter> fluentParameterInstances,
        IFluentStep? nextStep,
        IList<ConstructorMetadata> constructorMetadataList,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> valueStorages,
        DiagnosticList diagnostics)
    { ... }

    // ChooseCandidateFluentMethod - static, returns SelectedFluentMethod array
    // ValidateMultipleFluentMethodCompatibility - needs diagnostics param
    // MergeConstructorMetadata - static
    // NormalizedConverterMethod - static
    // SelectedFluentMethod record - moves here
}
```

### ConstructorAnalyzer Strategy Dispatch Shape

```csharp
// Source: Derived from ConstructorAnalyzer.cs analysis
namespace Motiv.FluentFactory.Generator.Analysis;

internal class ConstructorAnalyzer(SemanticModel semanticModel)
{
    private static readonly IStorageDetectionStrategy[] Strategies =
    [
        new RecordStorageStrategy(),
        new PrimaryConstructorStorageStrategy(),
        new ExplicitConstructorStorageStrategy()
    ];

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> FindParameterValueStorage(
        IMethodSymbol constructor)
    {
        var results = CreateDefaultStorage(constructor);

        // Dispatch to first matching strategy
        var strategy = Strategies.FirstOrDefault(s => s.CanHandle(constructor, semanticModel));
        strategy?.PopulateStorage(constructor, results, semanticModel);

        // Handle initializer chain (stays here - recursive)
        ResolveInitializerChain(constructor, results);

        return results;
    }
}
```

### FluentDiagnostics Static Class Shape

```csharp
// Source: Derived from FluentFactoryGenerator.cs lines 16-132
namespace Motiv.FluentFactory.Generator.Diagnostics;

/// <summary>
/// Diagnostic descriptors for the fluent factory source generator.
/// </summary>
public static class FluentDiagnostics
{
    private const string Category = "FluentFactory";

    public static readonly DiagnosticDescriptor UnreachableConstructor = new(...);
    public static readonly DiagnosticDescriptor ContainsSupersededFluentMethodTemplate = new(...);
    public static readonly DiagnosticDescriptor IncompatibleFluentMethodTemplate = new(...);
    public static readonly DiagnosticDescriptor AllFluentMethodTemplatesIncompatible = new(...);
    public static readonly DiagnosticDescriptor FluentMethodTemplateAttributeNotStatic = new(...);
    public static readonly DiagnosticDescriptor FluentMethodTemplateSuperseded = new(...);
    public static readonly DiagnosticDescriptor InvalidCreateMethodName = new(...);
    public static readonly DiagnosticDescriptor DuplicateCreateMethodName = new(...);
    public static readonly DiagnosticDescriptor FluentConstructorTargetTypeMissingFluentFactory = new(...);
    public static readonly DiagnosticDescriptor CreateMethodNameWithNoCreateMethod = new(...);
}
```

## Dependency Map

Understanding the call chain is critical for safe decomposition:

```
FluentFactoryGenerator.Initialize()
  |-- CreateConstructorContexts()        --> moves to FluentConstructorContextFactory
  |     |-- GetFluentFactoryMetadata()   --> moves to FluentConstructorContextFactory
  |     |-- ConvertToFluentFactoryGeneratorOptions() --> moves to FluentConstructorContextFactory
  |     |-- CreateContainingTypeFluentConstructorContexts() --> moves to FluentConstructorContextFactory
  |-- DeDuplicateFluentConstructors()    --> moves to FluentConstructorContextFactory
  |     |-- ChooseOverridingConstructors() --> moves to FluentConstructorContextFactory
  |-- new FluentModelFactory(compilation)
  |     |-- CreateFluentFactoryCompilationUnit()  --> stays (orchestrator entry point)
  |           |-- CreateFluentStepTrie()           --> stays (orchestrator, ~25 lines)
  |           |-- ConvertNodeToFluentFluentMethods() --> stays (orchestrator dispatch)
  |           |     |-- ConvertNodeToFluentMethods() --> FluentMethodSelector
  |           |     |     |-- CreateFluentMethods()  --> FluentMethodSelector
  |           |     |     |-- ChooseCandidateFluentMethod() --> FluentMethodSelector
  |           |     |-- ConvertNodeToCreationMethods() --> stays or FluentStepBuilder
  |           |-- ConvertNodeToFluentStep()   --> FluentStepBuilder
  |           |     |-- CreateStep()          --> FluentStepBuilder
  |           |     |-- GetValueStorages()    --> FluentStepBuilder
  |           |     |-- CreateRegularStepValueStorage() --> FluentStepBuilder
  |           |-- GetDescendentFluentSteps()  --> FluentStepBuilder (static)
  |-- Execute()                              --> stays (generator)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single god class with all diagnostics | Static diagnostic holder class | Common Roslyn pattern | Cleaner separation of data vs behavior |
| If/else chains for type detection | Strategy pattern with interface | Well-established OO pattern | Extensible, testable detection |
| Monolithic factory methods | Orchestrator + collaborators | Standard decomposition pattern | Each piece independently understandable |

## Open Questions

1. **FluentStepBuilder's relationship to ConvertNodeToCreationMethods**
   - What we know: ConvertNodeToCreationMethods (lines 360-382) mutates `_unreachableConstructorAnalyzer`. It is called from the orchestrator's `ConvertNodeToFluentFluentMethods`.
   - What's unclear: Whether this moves to FluentStepBuilder or stays in the orchestrator alongside `ConvertNodeToFluentFluentMethods`.
   - Recommendation: Keep it in the orchestrator since it directly mutates `_unreachableConstructorAnalyzer` and is small (~22 lines). The orchestrator's `ConvertNodeToFluentFluentMethods` dispatches to both the selector (via `ConvertNodeToFluentMethods`) and creation methods. This is an orchestration concern.

2. **How FluentMethodSelector returns diagnostics**
   - What we know: `ValidateMultipleFluentMethodCompatibility` and `ConvertNodeToFluentMethods` both add to `_diagnostics`. The selector also calls `_unreachableConstructorAnalyzer.AddReachableMethod`.
   - What's unclear: Whether the selector should receive DiagnosticList and UnreachableConstructorAnalyzer as constructor parameters or method parameters.
   - Recommendation: Constructor parameters since they are used across multiple method calls within a single orchestration cycle. This matches the primary constructor injection pattern used throughout the codebase.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (via Microsoft.NET.Test.Sdk + xunit packages) |
| Config file | Motiv.FluentFactory.Generator.Tests.csproj (implicit xUnit config) |
| Quick run command | `dotnet test src/Motiv.FluentFactory.Generator.Tests --no-build -v q` |
| Full suite command | `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DECOMP-01 | FluentModelFactory decomposed into focused types | integration (existing) | `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` | Existing tests cover via generated output verification |
| DECOMP-02 | FluentFactoryGenerator pipeline stages extracted | integration (existing) | `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` | Existing tests cover via generated output verification |
| DECOMP-03 | ConstructorAnalyzer storage detection separated | integration (existing) | `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` | Existing tests cover via generated output verification |
| XCUT-01 | All existing tests pass | integration | `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` | All 19 test files exist |
| XCUT-02 | Generated output identical | integration | `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` | CSharpSourceGeneratorVerifier validates exact output |

### Sampling Rate
- **Per task commit:** `dotnet build src/Motiv.FluentFactory.Generator -v q` (compile check)
- **Per wave merge:** `dotnet test src/Motiv.FluentFactory.Generator.Tests -v q` (full suite)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
None -- existing test infrastructure covers all phase requirements. The tests are end-to-end source generator verification tests that compare generated .g.cs output against expected snapshots. Any behavioral change will be caught by test failure.

## Sources

### Primary (HIGH confidence)
- Direct code analysis of FluentModelFactory.cs (438 lines), FluentFactoryGenerator.cs (376 lines), ConstructorAnalyzer.cs (210 lines)
- Direct code analysis of all referenced types (FluentConstructorContext, IgnoredMultiMethodWarningFactory, UnreachableConstructorAnalyzer, FluentConstructorValidator, SymbolExtensions, FluentFactoryMetadata, Trie)
- Project file analysis (Motiv.FluentFactory.Generator.csproj -- netstandard2.0, InternalsVisibleTo Tests)
- Test project analysis (19 test files, xUnit + Shouldly + CSharpSourceGeneratorVerifier)

### Secondary (MEDIUM confidence)
- Established C# refactoring patterns (orchestrator, strategy, static holder) -- well-known OO patterns

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - direct codebase analysis, no external dependencies introduced
- Architecture: HIGH - all three decomposition patterns are well-established and match the CONTEXT.md decisions exactly
- Pitfalls: HIGH - identified from direct code reading (mutable state sharing, strategy ordering, diagnostic references, initializer chain recursion)

**Research date:** 2026-03-10
**Valid until:** Indefinite -- this is a refactoring of existing code with no external dependency changes
