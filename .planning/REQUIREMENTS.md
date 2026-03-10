# Requirements: Motiv.FluentFactory

**Defined:** 2026-03-10
**Core Value:** Developers write constructor parameters once and get a complete, type-safe fluent builder API generated automatically

## v1.2 Requirements

Requirements for Architecture Refactoring milestone. Each maps to roadmap phases.

### God Class Decomposition

- [ ] **DECOMP-01**: FluentModelFactory is decomposed into focused types with single responsibilities (method selection, step building, trie construction, storage resolution)
- [ ] **DECOMP-02**: FluentFactoryGenerator pipeline stages are extracted into distinct, named types
- [ ] **DECOMP-03**: ConstructorAnalyzer storage detection patterns are separated into focused types
- [ ] **DECOMP-04**: No individual class exceeds ~150 lines (bite-sized threshold)

### File Organization

- [ ] **ORG-01**: Project root surfaces key domain concepts (generator entry point, core model types, pipeline stages)
- [ ] **ORG-02**: Implementation details (syntax helpers, extensions, data structures) are nested in subdirectories
- [ ] **ORG-03**: Vertical slicing replaces horizontal layering — files grouped by feature/concern, not by technical layer
- [ ] **ORG-04**: Folder and file names communicate what the system does, not how it's structured

### Extension Consolidation

- [ ] **EXT-01**: Duplicate SymbolExtensions (Model + Generation namespaces) are merged into a single location
- [ ] **EXT-02**: Extension methods are organized by the concern they serve, not by the layer they live in

### Syntax Generator Decomposition

- [ ] **SYNTAX-01**: FluentStepMethodDeclaration is decomposed into focused types (type parameter handling, constraint generation, method body)
- [ ] **SYNTAX-02**: FluentRootFactoryMethodDeclaration is decomposed into focused types
- [ ] **SYNTAX-03**: FluentMethodSummaryDocXml (165 lines) is decomposed if responsibilities can be separated

### Cross-Cutting

- [ ] **XCUT-01**: All existing tests continue to pass — behavior-preserving refactor
- [ ] **XCUT-02**: Generated .g.cs output is identical before and after refactoring

## Previous Milestones

### v1.1 — Code Generation Quality

- [x] **QUAL-01**: All type references in generated code use fully qualified `global::` names to prevent namespace conflicts
- [x] **QUAL-02**: All generated types/members are decorated with `[global::System.CodeDom.Compiler.GeneratedCode("Motiv.FluentFactory", "version")]` attribute

## Future Requirements

None — this is a focused refactoring milestone.

## Out of Scope

| Feature | Reason |
|---------|--------|
| New attributes or features | Pure refactoring — no new capabilities |
| Runtime API changes | Internal generator structure only |
| Test refactoring | Focus is on production code organization |
| Generated output changes | Refactoring must not alter generated .g.cs files |
| Performance optimization | Not a goal — maintain current performance |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| DECOMP-01 | Phase 7 | Pending |
| DECOMP-02 | Phase 7 | Pending |
| DECOMP-03 | Phase 7 | Pending |
| DECOMP-04 | Phase 10 | Pending |
| ORG-01 | Phase 10 | Pending |
| ORG-02 | Phase 10 | Pending |
| ORG-03 | Phase 10 | Pending |
| ORG-04 | Phase 10 | Pending |
| EXT-01 | Phase 9 | Pending |
| EXT-02 | Phase 9 | Pending |
| SYNTAX-01 | Phase 8 | Pending |
| SYNTAX-02 | Phase 8 | Pending |
| SYNTAX-03 | Phase 8 | Pending |
| XCUT-01 | Phase 7, 8, 9, 10 | Pending |
| XCUT-02 | Phase 7, 8, 9, 10 | Pending |

**Coverage:**
- v1.2 requirements: 15 total
- Mapped to phases: 15
- Unmapped: 0

---
*Requirements defined: 2026-03-10*
*Last updated: 2026-03-10 after roadmap creation*
