# Milestones

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
