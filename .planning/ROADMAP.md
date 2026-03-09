# Roadmap: Motiv.FluentFactory

## Milestones

- [x] **v1.0 Initial Release** - Phases 1-5 (shipped 2026-03-09)
- [ ] **v1.1 Code Generation Quality** - Phase 6 (in progress)

## Phases

<details>
<summary>v1.0 Initial Release (Phases 1-5) - SHIPPED 2026-03-09</summary>

Phases 1-5 delivered the initial release: attribute-based API, fluent step struct generation, generic type support, method customization, multiple fluent methods, NoCreateMethod, XML docs, diagnostics, primary constructor support, and NuGet packaging.

</details>

### v1.1 Code Generation Quality

- [ ] **Phase 6: Generated Code Hardening** - Fully qualify all type references and add GeneratedCode attribution to all generated output

## Phase Details

### Phase 6: Generated Code Hardening
**Goal**: Generated code is robust for consumption in any namespace context and clearly marked as tool-generated
**Depends on**: v1.0 (Phases 1-5)
**Requirements**: QUAL-01, QUAL-02
**Success Criteria** (what must be TRUE):
  1. A consumer project with conflicting type names in its namespace compiles without errors when using the generated fluent factory
  2. Every type reference in generated .g.cs files uses `global::` prefix (no bare or partially qualified type names)
  3. Every generated type and member carries a `[GeneratedCode("Motiv.FluentFactory", "version")]` attribute
  4. Existing tests pass with updated generated output (no regressions)
**Plans**: TBD

Plans:
- [ ] 06-01: TBD
- [ ] 06-02: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 6

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 6. Generated Code Hardening | v1.1 | 0/? | Not started | - |
