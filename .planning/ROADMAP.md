# Roadmap: Motiv.FluentFactory

## Milestones

- [x] **v1.0 Initial Release** - Phases 1-5 (shipped 2026-03-09)
- [x] **v1.1 Code Generation Quality** - Phase 6 (completed 2026-03-09)
- [ ] **v1.2 Architecture Refactoring** - Phases 7-10 (in progress)

## Phases

<details>
<summary>v1.0 Initial Release (Phases 1-5) - SHIPPED 2026-03-09</summary>

Phases 1-5 delivered the initial release: attribute-based API, fluent step struct generation, generic type support, method customization, multiple fluent methods, NoCreateMethod, XML docs, diagnostics, primary constructor support, and NuGet packaging.

</details>

<details>
<summary>v1.1 Code Generation Quality (Phase 6) - COMPLETED 2026-03-09</summary>

### Phase 6: Generated Code Hardening
**Goal**: Generated code is robust for consumption in any namespace context and clearly marked as tool-generated
**Depends on**: v1.0 (Phases 1-5)
**Requirements**: QUAL-01, QUAL-02
**Success Criteria** (what must be TRUE):
  1. A consumer project with conflicting type names in its namespace compiles without errors when using the generated fluent factory
  2. Every type reference in generated .g.cs files uses `global::` prefix (no bare or partially qualified type names)
  3. Every generated type and member carries a `[GeneratedCode("Motiv.FluentFactory", "version")]` attribute
  4. Existing tests pass with updated generated output (no regressions)
**Plans:** 2 plans

Plans:
- [x] 06-01-PLAN.md -- Replace type rendering with global:: qualification, remove usings, add headers, add GeneratedCode attribute
- [x] 06-02-PLAN.md -- Bulk-update test expected outputs and add namespace conflict + GeneratedCode validation tests

</details>

### v1.2 Architecture Refactoring (In Progress)

**Milestone Goal:** Reorganize the Generator project for screaming architecture with vertical slicing, and decompose god classes into bite-sized, single-responsibility types.

**Cross-cutting requirements:** XCUT-01 (all tests pass) and XCUT-02 (generated output unchanged) apply to every phase below. Each phase must leave the test suite green and generated output identical.

- [x] **Phase 7: Core Pipeline Decomposition** - Decompose FluentModelFactory, FluentFactoryGenerator, and ConstructorAnalyzer into focused single-responsibility types (completed 2026-03-10)
- [ ] **Phase 8: Syntax Generator Decomposition** - Decompose FluentStepMethodDeclaration, FluentRootFactoryMethodDeclaration, and FluentMethodSummaryDocXml into focused types
- [ ] **Phase 9: Extension Consolidation** - Merge duplicate SymbolExtensions and organize extension methods by concern
- [ ] **Phase 10: Screaming Architecture Reorganization** - Restructure folders for vertical slicing with domain concepts at root and details nested

## Phase Details

### Phase 7: Core Pipeline Decomposition
**Goal**: The three largest god classes are decomposed into focused types, each with a single clear responsibility
**Depends on**: Phase 6 (v1.1 complete)
**Requirements**: DECOMP-01, DECOMP-02, DECOMP-03, XCUT-01, XCUT-02
**Success Criteria** (what must be TRUE):
  1. FluentModelFactory's four responsibilities (method selection, step building, trie construction, storage resolution) exist as separate, focused types
  2. FluentFactoryGenerator's pipeline stages are individually named types that can be understood in isolation
  3. ConstructorAnalyzer's storage detection patterns are separated into distinct types rather than living in one large class
  4. All existing tests pass with identical generated output after decomposition
**Plans:** 3/3 plans complete

Plans:
- [ ] 07-01-PLAN.md -- Extract FluentDiagnostics and FluentConstructorContextFactory from FluentFactoryGenerator
- [ ] 07-02-PLAN.md -- Decompose ConstructorAnalyzer into strategy pattern with three detection strategies
- [ ] 07-03-PLAN.md -- Extract FluentMethodSelector and FluentStepBuilder from FluentModelFactory

### Phase 8: Syntax Generator Decomposition
**Goal**: The large syntax generation classes are decomposed into focused types that each handle one aspect of code generation
**Depends on**: Phase 7
**Requirements**: SYNTAX-01, SYNTAX-02, SYNTAX-03, XCUT-01, XCUT-02
**Success Criteria** (what must be TRUE):
  1. FluentStepMethodDeclaration's concerns (type parameter handling, constraint generation, method body construction) exist as separate focused types
  2. FluentRootFactoryMethodDeclaration is decomposed into types with clear single responsibilities
  3. FluentMethodSummaryDocXml responsibilities are separated if distinct concerns exist, or documented as appropriately sized if not
  4. All existing tests pass with identical generated output after decomposition
**Plans:** 3 plans

Plans:
- [ ] 08-01-PLAN.md -- Extract TypeParameterConstraintBuilder to shared, replace in all 4 consumers, fix constraint qualification bug
- [ ] 08-02-PLAN.md -- Decompose FluentStepMethodDeclaration into thin orchestrator, consolidate FluentMethodSummaryDocXml
- [ ] 08-03-PLAN.md -- Decompose FluentRootFactoryMethodDeclaration into thin orchestrator

### Phase 9: Extension Consolidation
**Goal**: Extension methods are unified and organized by the concern they serve rather than by which layer they originated in
**Depends on**: Phase 8
**Requirements**: EXT-01, EXT-02, XCUT-01, XCUT-02
**Success Criteria** (what must be TRUE):
  1. A single SymbolExtensions location exists (the duplicate across Model and Generation namespaces is eliminated)
  2. Extension methods are grouped by the domain concern they serve, not by the old horizontal layer they lived in
  3. All existing tests pass with identical generated output after consolidation
**Plans**: TBD

Plans:
- [ ] 09-01: TBD

### Phase 10: Screaming Architecture Reorganization
**Goal**: The project folder structure communicates what the system does at a glance, with key concepts at the root and implementation details nested in subdirectories
**Depends on**: Phase 9
**Requirements**: ORG-01, ORG-02, ORG-03, ORG-04, DECOMP-04, XCUT-01, XCUT-02
**Success Criteria** (what must be TRUE):
  1. Opening the Generator project root reveals the domain concepts (generator entry point, core model types, pipeline stages) without navigating into subdirectories
  2. Implementation details (syntax helpers, internal data structures, utility extensions) are nested in subdirectories beneath their parent concepts
  3. The old horizontal layer folders (Analysis/, Model/, Generation/) are replaced with feature/concern-based groupings
  4. No individual source file exceeds approximately 150 lines
  5. All existing tests pass with identical generated output after reorganization
**Plans**: TBD

Plans:
- [ ] 10-01: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 7 -> 8 -> 9 -> 10

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 6. Generated Code Hardening | v1.1 | 2/2 | Complete | 2026-03-09 |
| 7. Core Pipeline Decomposition | 3/3 | Complete   | 2026-03-10 | - |
| 8. Syntax Generator Decomposition | v1.2 | 0/3 | Planned | - |
| 9. Extension Consolidation | v1.2 | 0/? | Not started | - |
| 10. Screaming Architecture Reorganization | v1.2 | 0/? | Not started | - |
