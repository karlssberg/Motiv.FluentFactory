# Phase 7: Core Pipeline Decomposition - Context

**Gathered:** 2026-03-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Decompose the three largest god classes (FluentModelFactory 438 lines, FluentFactoryGenerator 376 lines, ConstructorAnalyzer 210 lines) into focused, single-responsibility types. All existing tests must pass with identical generated output. New types stay in their current namespace directories — folder restructuring is Phase 10.

</domain>

<decisions>
## Implementation Decisions

### FluentModelFactory Decomposition (438 lines)
- **Orchestrator pattern**: FluentModelFactory stays as a thin orchestrator that owns shared state (_regularFluentSteps, _diagnostics, _unreachableConstructorAnalyzer) and delegates to focused types
- **FluentMethodSelector**: Single focused type for method selection — ChooseCandidateFluentMethod, CreateFluentMethods, ValidateMultipleFluentMethodCompatibility, MergeConstructorMetadata, NormalizedConverterMethod, and the SelectedFluentMethod record all move here
- **FluentStepBuilder**: Handles node-to-step conversion (ConvertNodeToFluentStep, CreateStep, GetDescendentFluentSteps) plus storage resolution (CreateRegularStepValueStorage, GetValueStorages). Calls FluentMethodSelector to get methods for each node
- **Trie construction stays in orchestrator**: CreateFluentStepTrie (~25 lines) is too small to extract — orchestrator calls it once and passes the trie to step/method builders
- **Helpers move to consumers**: Each helper method moves to the type that uses it, not a shared utility class
- **Constructor injection for Compilation**: Each extracted type that needs Compilation receives it via its constructor. FluentMethodSelector gets it; FluentStepBuilder only if needed

### FluentFactoryGenerator Decomposition (376 lines)
- **FluentDiagnostics**: All 10 diagnostic descriptors (MFFG0001-MFFG0010) move to a dedicated static class in Diagnostics/. References from FluentModelFactory, IgnoredMultiMethodWarningFactory, etc. update to point here
- **FluentConstructorContextFactory**: Context creation (CreateConstructorContexts, CreateContainingTypeFluentConstructorContexts), metadata extraction (GetFluentFactoryMetadata, ConvertToFluentFactoryGeneratorOptions), and de-duplication (DeDuplicateFluentConstructors, ChooseOverridingConstructors) all move to a single class in Analysis/
- **FluentFactoryGenerator stays at ~70 lines**: After extraction, it remains a clean IIncrementalGenerator entry point with Initialize() pipeline setup and Execute(). No further decomposition needed

### ConstructorAnalyzer Decomposition (210 lines)
- **Strategy per detection pattern**: Each storage detection strategy (records, primary constructors, explicit constructor bodies) becomes its own type implementing a common interface
- **SemanticModel as method parameter**: Strategy interface methods receive SemanticModel as a parameter rather than constructor injection. Strategies are static or lightweight classes
- **Initializer chain stays in dispatcher**: The `: base()` / `: this()` chain resolution stays in ConstructorAnalyzer.FindParameterValueStorage since it recursively calls itself. Strategies handle their pattern; the dispatcher handles chaining as a post-step
- **3 strategies extracted**: RecordStorageStrategy, PrimaryConstructorStorageStrategy, ExplicitConstructorStorageStrategy

### Execution Plan
- **3 separate plans, one per class**: 07-01 (FluentFactoryGenerator), 07-02 (ConstructorAnalyzer), 07-03 (FluentModelFactory). Each independently testable with full test suite run after each
- **Follow data flow order**: Generator (upstream, simplest) -> ConstructorAnalyzer (middle, moderate) -> FluentModelFactory (downstream, most complex). Each builds on stable upstream types
- **Same directories**: New types go next to the class they were extracted from. Phase 10 handles folder restructuring

### Claude's Discretion
- Exact method signatures for extracted types
- Internal visibility and access modifiers
- Whether small shared helpers (GetInitializerSyntax, IsInitializedFromParameter) become part of a strategy or stay as private helpers
- Exact naming of strategy interface (e.g., IStorageDetectionStrategy vs IValueStorageResolver)

</decisions>

<specifics>
## Specific Ideas

- FluentMethodSelector should contain the full method creation + selection + validation pipeline as a cohesive unit
- FluentStepBuilder calls FluentMethodSelector — the orchestrator wires them together
- The orchestrator pattern means FluentModelFactory.CreateFluentFactoryCompilationUnit remains the public entry point — no change to callers
- ConstructorAnalyzer.FindParameterValueStorage remains the public entry point — strategies are internal implementation detail

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `FluentMethodSignatureEqualityComparer`: Already exists in Model/Methods/ — FluentMethodSelector will use it
- `UnreachableConstructorAnalyzer`: Already a focused type in Diagnostics/ — stays as-is, used by orchestrator
- `DiagnosticList`: Collection type in Diagnostics/ — continues to be used by orchestrator

### Established Patterns
- Primary constructor injection: Both FluentModelFactory and ConstructorAnalyzer use `class Foo(dependency)` syntax — extracted types should follow this
- Static helper methods: Several methods in FluentModelFactory are already static (ChooseCandidateFluentMethod, GetDescendentFluentSteps, CreateRegularStepValueStorage) — natural candidates for extraction
- IFluentValueStorage strategy pattern: Already exists in Model/Storage/ with 4 implementations — ConstructorAnalyzer strategies follow the same pattern

### Integration Points
- `FluentFactoryGenerator.Initialize()` pipeline step 3 creates `new FluentModelFactory(compilation)` — this call stays the same
- `FluentConstructorContext` constructor calls `new ConstructorAnalyzer(semanticModel).FindParameterValueStorage(constructor)` — this call stays the same
- `FluentModelFactory` references `FluentFactoryGenerator.AllFluentMethodTemplatesIncompatible` (diagnostic) — will change to `FluentDiagnostics.AllFluentMethodTemplatesIncompatible`

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 07-core-pipeline-decomposition*
*Context gathered: 2026-03-10*
