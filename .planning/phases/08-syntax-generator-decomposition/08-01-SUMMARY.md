---
phase: 08-syntax-generator-decomposition
plan: 01
subsystem: generator
tags: [roslyn, source-generator, type-constraints, global-qualification]

requires:
  - phase: 06-global-qualification
    provides: "ToGlobalDisplayString() extension method and global:: convention"
provides:
  - "TypeParameterConstraintBuilder shared class in Generation/Shared/"
  - "Unified constraint ordering: reference, value, types, constructor"
  - "Fixed qualification bug: all constraints use global:: prefix"
affects: [08-02, 08-03]

tech-stack:
  added: []
  patterns: ["Shared builder pattern for syntax generation (TypeParameterConstraintBuilder)"]

key-files:
  created:
    - "src/Motiv.FluentFactory.Generator/Generation/Shared/TypeParameterConstraintBuilder.cs"
  modified:
    - "src/Motiv.FluentFactory.Generator/Generation/SyntaxElements/Methods/FluentStepMethodDeclaration.cs"
    - "src/Motiv.FluentFactory.Generator/Generation/SyntaxElements/Methods/FluentRootFactoryMethodDeclaration.cs"
    - "src/Motiv.FluentFactory.Generator/Generation/SyntaxElements/RootTypeDeclaration.cs"
    - "src/Motiv.FluentFactory.Generator/Generation/SyntaxElements/FluentStepDeclaration.cs"
    - "src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorGenericTests.cs"

key-decisions:
  - "Canonical constraint ordering: reference type, value type, type constraints, constructor (matches C# convention and struct declaration pattern)"
  - "Used ToGlobalDisplayString() for all constraint type names, fixing the Omitted qualification bug"

patterns-established:
  - "Shared constraint builder: all type parameter constraint clauses go through TypeParameterConstraintBuilder.Create()"

requirements-completed: [SYNTAX-01, SYNTAX-02, XCUT-01, XCUT-02]

duration: 5min
completed: 2026-03-11
---

# Phase 08 Plan 01: TypeParameterConstraintBuilder Extraction Summary

**Extracted shared TypeParameterConstraintBuilder eliminating 4x duplicated constraint-building code and fixing global:: qualification bug**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-11T00:25:28Z
- **Completed:** 2026-03-11T00:30:24Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- Created TypeParameterConstraintBuilder.cs in Generation/Shared/ with a single Create method that builds constraint clauses from ITypeParameterSymbol arrays
- Replaced inline constraint-building loops in all 4 consumer files (FluentStepMethodDeclaration, FluentRootFactoryMethodDeclaration, RootTypeDeclaration, FluentStepDeclaration) with delegation to shared builder
- Fixed qualification bug where method-level type parameter constraints used GlobalNamespaceStyle.Omitted (System.X) instead of ToGlobalDisplayString() (global::System.X)
- Unified constraint ordering across all declaration types to: reference type, value type, type constraints, constructor

## Task Commits

Each task was committed atomically:

1. **Task 1: Create TypeParameterConstraintBuilder and replace in all 4 consumers** - `5935405` (feat)
2. **Task 2: Fix test expectations for qualification bug and run full test suite** - `5935405` (included in Task 1 commit)

Note: Both tasks were completed in a single commit (5935405) as the extraction and test fixes were done together. Additional decomposition of FluentStepMethodDeclaration and FluentRootFactoryMethodDeclaration was performed in supporting commits 4963cf6 and c3e6866.

## Files Created/Modified
- `src/Motiv.FluentFactory.Generator/Generation/Shared/TypeParameterConstraintBuilder.cs` - Shared constraint clause builder with unified ordering and global:: qualification
- `src/Motiv.FluentFactory.Generator/Generation/SyntaxElements/Methods/FluentStepMethodDeclaration.cs` - Replaced GetConstraintClauses with GetCombinedTypeParameters + TypeParameterConstraintBuilder.Create
- `src/Motiv.FluentFactory.Generator/Generation/SyntaxElements/Methods/FluentRootFactoryMethodDeclaration.cs` - Replaced GetConstraintClauses and BuildConstraintClause with GetCombinedTypeParameters + TypeParameterConstraintBuilder.Create
- `src/Motiv.FluentFactory.Generator/Generation/SyntaxElements/RootTypeDeclaration.cs` - Replaced inline LINQ constraint builder with TypeParameterConstraintBuilder.Create
- `src/Motiv.FluentFactory.Generator/Generation/SyntaxElements/FluentStepDeclaration.cs` - Replaced inline LINQ constraint builder with TypeParameterConstraintBuilder.Create
- `src/Motiv.FluentFactory.Generator.Tests/FluentFactoryGeneratorGenericTests.cs` - Updated 2 test expectations from `System.IComparable<T>` to `global::System.IComparable<T>`

## Decisions Made
- Canonical constraint ordering chosen as: reference type (class), value type (struct), type constraints, constructor (new()). This matches the existing struct declaration ordering and C# convention where new() comes last.
- Both tasks merged into implementation commit since extraction and test fix are inseparable (fixing the bug changes output, requiring test updates simultaneously).

## Deviations from Plan

None - plan executed as written. The implementation was completed in prior commits during the same session.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- TypeParameterConstraintBuilder is ready for use by any future syntax generation that needs constraint clauses
- All 174 tests pass with updated expectations
- No remaining occurrences of GlobalNamespaceStyle.Omitted in constraint-building code

---
*Phase: 08-syntax-generator-decomposition*
*Completed: 2026-03-11*
