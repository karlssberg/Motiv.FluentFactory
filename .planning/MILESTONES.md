# Milestones
## v1.2 — Architecture Refactoring

**Shipped:** 2026-03-11
**Phases:** 7-10 (4 phases, 9 plans)
**Git range:** `286be64..c5d3865` (32 commits)
**Stats:** 104 files changed, +3,815 / -2,357 lines, 5,708 LOC total

### What Shipped
- Decomposed 3 god classes (FluentModelFactory, FluentFactoryGenerator, ConstructorAnalyzer) into 11 focused single-responsibility types
- Extracted shared TypeParameterConstraintBuilder, fixing a constraint qualification bug
- Decomposed syntax generation classes into thin orchestrators with focused helpers
- Consolidated duplicate extension methods into 4 concern-based extension files
- Reorganized project to screaming architecture with domain concepts at root
- Split all files to ~150 lines with single responsibilities

---


## v1.0 — Initial Release

**Shipped:** 2026-03-09
**Phases:** 1-5 (inferred from existing codebase)

### What Shipped
- Attribute-based API for fluent factory generation
- Fluent step struct generation with chainable methods
- Generic type support including nested generics
- Method name customization and priority ordering
- Multiple fluent methods per parameter via templates
- NoCreateMethod option for custom partial types
- XML documentation generation
- 10 diagnostics for validation errors
- Primary constructor support
- NuGet packaging with bundled generator and attributes

## v1.1 — Code Generation Quality

**Shipped:** 2026-03-09
**Phases:** 6

### What Shipped
- All generated type references use fully qualified `global::` names to avoid namespace conflicts
- All generated types/members decorated with `[GeneratedCode("Motiv.FluentFactory", "version")]` attribute
