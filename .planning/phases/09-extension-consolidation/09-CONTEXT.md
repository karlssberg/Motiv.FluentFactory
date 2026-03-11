# Phase 9: Extension Consolidation - Context

**Gathered:** 2026-03-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Merge the duplicate SymbolExtensions classes and organize all 7 extension files by the concern they serve, not by the layer they originated in. All existing tests must pass with identical generated output. This is a pure reorganization — no new functionality, no behavioral changes.

</domain>

<decisions>
## Implementation Decisions

### Merge Strategy
- Merge-then-split: combine both SymbolExtensions (Model + Generation) into one logical unit, then re-split by domain concern
- All 7 extension files are in scope — full reorganization, not just SymbolExtensions
- Merge the two StringExtensions files (Generation + root) into one StringExtensions class (~55 lines)
- Merge FluentMethodExtensions and FluentReturnExtensions into one FluentModelExtensions class (~160 lines)
- EnumerableExtensions stays as-is (already single-concern)

### Grouping by Concern
- **TypeParameterExtensions** (~100 lines): GetGenericTypeParameters, GetGenericTypeArguments, GetGenericTypeParameterSyntaxList, ToTypeParameterSyntax, Union, Except — all type parameter extraction, filtering, and conversion
- **SymbolExtensions** (~120 lines): display formatting (ToGlobalDisplayString, ToFullDisplayString, ToUnqualifiedDisplayString), type analysis (IsOpenGenericType, IsPartial, CanBeCustomStep, IsAssignable, ReplaceTypeParameters), accessibility conversion, and generic attribute helpers (HasAttribute, GetAttribute, GetAttributes)
- **FluentModelExtensions** (~160 lines): fluent method display, fluent return helpers, FindUnreachableConstructors, AND fluent-specific attribute helpers (GetFluentMethodName, GetMultipleFluentMethodSymbols, GetFluentMethodPriority, GetLocationAtIndex) — domain-specific methods that reference TypeName, FluentDiagnostics
- **StringExtensions** (~55 lines): Capitalize, ToCamelCase, ToParameterFieldName, ToIdentifier, ToFileName — all string utilities merged
- **EnumerableExtensions** (~54 lines): InterleaveWith, AppendIfNotNull, DistinctBy, AddRange — unchanged
- Combine small related concerns when combined size stays under ~150 lines

### Naming Convention
- Concern-based names: TypeParameterExtensions, SymbolExtensions, FluentModelExtensions, StringExtensions, EnumerableExtensions
- Names communicate what concern the class serves, not what type it extends

### File Placement
- Keep near primary consumers — not a new Extensions/ folder
- TypeParameterExtensions → Generation/ (primary consumers are syntax generation classes)
- SymbolExtensions → Generation/ (ToGlobalDisplayString is the most-used method, lives in Generation today)
- FluentModelExtensions → Model/ (serves fluent model domain types)
- StringExtensions → project root (general utility)
- EnumerableExtensions → project root (general utility, already there)

### Namespace
- Single shared namespace for all extension files (eliminates cross-namespace imports for extensions)
- Phase 10 may adjust namespace structure during full reorganization

### Claude's Discretion
- Exact namespace choice (e.g., `Motiv.FluentFactory.Generator` vs `Motiv.FluentFactory.Generator.Extensions`)
- Whether any private SymbolDisplayFormat fields need deduplication across merged classes
- Internal method ordering within each consolidated class
- Which using directives can be removed from consumer files after namespace unification

</decisions>

<specifics>
## Specific Ideas

- TypeParameterExtensions pairs well with the existing TypeParameterConstraintBuilder in Generation/Shared/ — both serve the type parameter concern
- The cross-layer dependency already exists (Model/SymbolExtensions imports Generation namespace) — consolidation eliminates this awkward coupling
- FluentModelExtensions absorbs fluent-specific attribute helpers that were previously in the generic SymbolExtensions, making the boundary between generic Roslyn helpers and domain-specific helpers clearer

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `TypeParameterConstraintBuilder` (Generation/Shared/): Already extracted in Phase 8, serves same type parameter concern as TypeParameterExtensions
- Multiple `SymbolDisplayFormat` static fields across files — some may be consolidatable during merge

### Established Patterns
- Extension files currently use directory-based namespaces (Model, Generation, root)
- Model/ files already import Generation namespace for ToGlobalDisplayString — cross-layer dependency is established
- Phase 7-8 established pattern of keeping new types near their consumers

### Integration Points
- 10+ consumer files reference extension methods via using directives — all must be updated
- FluentModelExtensions consumers: FluentStepBuilder, RegularFluentStep, FluentMethodSelector, syntax generation classes
- SymbolExtensions consumers: nearly every file in Model/ and Generation/
- TypeParameterExtensions consumers: syntax method declarations, step/root type declarations

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 09-extension-consolidation*
*Context gathered: 2026-03-11*
