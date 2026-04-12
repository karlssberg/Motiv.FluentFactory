# Phase 16: Diagnostic Alignment - Context

**Gathered:** 2026-04-12
**Status:** Ready for planning

<domain>
## Phase Boundary

Strip `FluentFactory` / `MFFG` / `FluentConstructor` vocabulary from diagnostic-producing code. Diagnostic descriptor `Category`, titles, message formats, descriptor variable identifiers, XML doc comments in diagnostic files, and `AnalyzerReleases.Unshipped.md` all speak Converj / FluentRoot vocabulary. Pure rename refactor — no behavior changes, no severity/ID changes, no test assertion changes, no triggering-condition changes. `dotnet build` green with zero warnings and `dotnet test` passes every existing test at phase end.

Out of scope: public attribute API changes (frozen in v2.0), namespace restructuring, renaming the `FluentDiagnostics` class itself, the GoF-pattern `*Factory` helper renames (Phase 18), top-level generator type renames (Phase 17), test fixture renames (Phase 19), `CLAUDE.md` cleanup (Phase 20).

</domain>

<decisions>
## Implementation Decisions

### Vocabulary Policy

- **"Factory" referring to the fluent root → `FluentRoot`** (attribute-cased). Example: "Factory type missing partial modifier" → "FluentRoot type missing partial modifier". "Inaccessible parameter type in fluent factory" → "Inaccessible parameter type in FluentRoot".
- **Ambiguous tool/generator phrasings → "Converj generator" framing.** Example: "Fluent factory generation requires value-type parameters" → "Converj generator requires value-type parameters" (or equivalent "Converj ..." phrasing that preserves message intent). "cannot be used by the fluent factory" → "cannot be used by the Converj generator".
- **"Factory" referring to C# constructors or the retained GoF pattern stays untouched.** Planner must read each occurrence and judge intent before replacing.
- **CVJG0031 bug fix:** Line 367 message currently says "Set AllowPartialParameterOverlap = true on [FluentFactory] to allow this." The attribute was renamed to `[FluentRoot]` in v2.0. Fix inline during Phase 16 vocabulary pass. Document in the phase plan as a defect discovered during alignment for traceability.
- **Audit scope: all 47 descriptors.** Read every `title` and `messageFormat` field against Root/Target vocabulary, not just grep matches. Catches drift like CVJG0031 even when the literal word "Factory" isn't present.

### Category Constant

- `private const string Category = "FluentFactory";` → `private const string Category = "Converj";` (DIAG-01).
- All 47 descriptors inherit the new value via the existing `Category` variable — one-line change in `FluentDiagnostics.cs`.

### Descriptor Variable Identifiers

- **C# identifier renames are in Phase 16 scope** — single-file pass, consistent with the full-audit decision on titles/messages.
- **Audit all 47 descriptor variable names** for Root/Target drift, not only ones with "Factory" in the name.
- **Known rename:** `FluentDiagnostics.FluentParameterOnStaticFactory` → `FluentDiagnostics.FluentParameterOnStaticRoot`. Planner may discover additional identifiers requiring rename during the audit.
- **Rename strategy:** IDE / Roslyn compiler-assisted rename so all call sites (analyzers, model builders, reporters) update automatically. Satisfies BEHAV-03. Full `dotnet build` verifies no missed references.

### XML Doc Comments

- **In scope for Phase 16.** `/// <summary>` comments in `FluentDiagnostics.cs` (e.g., class summary "Diagnostic descriptors for the fluent factory source generator", per-descriptor summaries referring to "factory root types", "factory accessibility", "static factory type") are updated in the same pass as titles/messages.
- **Rationale:** DIAG-04 targets "diagnostic-producing code"; XML docs live in the diagnostic file. Splitting across Phase 20 would force re-opening `FluentDiagnostics.cs` twice.
- Vocabulary rules match the title/message policy: "factory" referring to fluent root → "FluentRoot"; ambiguous generator references → "Converj generator".

### Sibling Files in Diagnostics/

- **`UnreachableConstructorAnalyzer.cs` is in scope.** Contains legacy `FluentFactory` / `FluentConstructor` string literals. DIAG-04 covers it as diagnostic-producing code. Without including it, the DIAG-04 grep check at phase end still finds hits.
- **`DiagnosticList.cs` and `IgnoredMultiMethodWarningFactory.cs`:** inspect both during the pass. Include only if they contain drift vocabulary in strings/identifiers **relevant to diagnostic content**. The `IgnoredMultiMethodWarningFactory` class itself is a Phase 18 builder rename — **do not rename the type or file in Phase 16**, but fix any internal vocabulary drift in its string literals or XML docs.
- **`FluentDiagnostics` class and file name stay as-is.** Not in NAME-01..07. "Fluent" prefix is not drift vocabulary — it matches the [Fluent*] attribute family. No rename this phase or this milestone.

### AnalyzerReleases.Unshipped.md

- **Fill all 47 descriptors.** Current file lists only 18. Completing the file is in Phase 16 scope — the 29 missing descriptors (mostly CVJG0018–0049) are all unshipped, and this file is the source of truth for analyzer release tracking ahead of v2.1 release.
- **Document the gap fill as a defect discovered during alignment** in the phase plan for traceability ("AnalyzerReleases.Unshipped.md was incomplete prior to Phase 16").
- **Notes column:** copy the post-rename descriptor `title` verbatim. Keeps the release-notes file synchronized with the descriptor source of truth.
- **Row ordering:** numeric by rule ID (CVJG0001 → CVJG0049). Predictable, easy to diff, matches the descriptor-file pattern.
- **Category column:** `Converj` for every row. Matches DIAG-01.

### Claude's Discretion

- Exact wording of rewrites for ambiguous "fluent factory" phrasings — planner/executor chooses between "Converj generator ..." variants that preserve message intent without changing diagnostic behavior.
- Per-descriptor XML summary wording — executor chooses phrasing consistent with the policy ("FluentRoot" for root references, "Converj generator" for tool references).
- Discovery of additional descriptor variable identifiers during the full-47 audit — executor flags and renames any found, using the same naming pattern as `FluentParameterOnStaticRoot`.
- Commit granularity within Phase 16 — planner may split into sub-plans (e.g., Category constant + category pass; vocabulary pass over titles/messages/XML; identifier renames; AnalyzerReleases completion) or keep as one atomic change. No hard constraint from requirements.

</decisions>

<specifics>
## Specific Ideas

- **Full-audit mindset.** User explicitly chose "Audit all 47" for both title/message drift and descriptor variable identifiers. The planner should frame tasks around a complete sweep, not a grep-driven minimum.
- **CVJG0031 as a worked example.** The broken `[FluentFactory]` reference demonstrates why grep-only isn't sufficient — the literal doesn't contain the word "Factory" in a title, but the message text does, inside a bracketed attribute name. Use this as the canonical example in the phase plan of why a human read of each descriptor is required.
- **Compiler-assisted safety net.** BEHAV-03 is the enforcement mechanism: if an identifier rename misses a call site, `dotnet build` fails. The planner should treat `dotnet build` + `dotnet test` green as the literal phase-completion gate.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets

- **`FluentDiagnostics.cs`** (`src/Converj.Generator/Diagnostics/`): 580-line single-file home for all 47 descriptors. Uses a single `private const string Category = "FluentFactory"` which all descriptors reference — one change, all descriptors pick it up. Diagnostic IDs already use `CVJG` prefix (MFFG rename is already done — confirmed by grep). Titles, messages, and XML docs are the drift surface.
- **`AnalyzerReleases.Unshipped.md`** (`src/Converj.Generator/`): tracking file with `Rule ID | Category | Severity | Notes` columns. Partial coverage (18/47 rows). Planner will extend to 47.
- **`UnreachableConstructorAnalyzer.cs`** (`src/Converj.Generator/Diagnostics/`): second file producing diagnostic output, contains legacy string literals — in Phase 16 scope.
- **Roslyn IDE rename** (via Visual Studio / Rider / `dotnet format` analyzers): satisfies BEHAV-03 for descriptor variable renames.

### Established Patterns

- **Central `Category` constant** — single source of truth; pattern is already correct, just needs the value updated.
- **Diagnostic IDs are stable** (`CVJG0001` through `CVJG0049`, with one gap at 0034). Phase 16 does not touch IDs.
- **Descriptor-per-static-field** — each `DiagnosticDescriptor` is a `public static readonly` field on `FluentDiagnostics`, referenced by name from call sites. IDE rename follows all references.
- **Generator tests use `CSharpSourceGeneratorVerifier`** — existing tests may assert on descriptor IDs and title/message text. Planner must decide whether any test strings need to be updated when titles/messages shift, or whether no test fixture currently asserts on the wording being changed. Pure `Category` string change has no test impact (not asserted). Title/message wording changes may require test updates — planner/researcher to verify before execution.

### Integration Points

- **Call sites referencing descriptor variables:** `src/Converj.Generator/Diagnostics/UnreachableConstructorAnalyzer.cs`, model builders, `FluentTargetValidator.cs`, syntax generators — anywhere `FluentDiagnostics.*` is read. IDE rename follows the reference graph automatically.
- **`AnalyzerReleases.Unshipped.md`** is coupled to descriptor definitions — Phase 16 keeps them in sync.
- **Test project `src/Converj.Generator.Tests/`** may contain `ExpectedDiagnostics` assertions that reference descriptor IDs (stable) or match on title/message text (brittle). Researcher should grep the test project for any match on title/message literals touched by the vocabulary pass before executor runs rename.

</code_context>

<deferred>
## Deferred Ideas

- **Rename `FluentDiagnostics` class/file itself to `ConverjDiagnostics`** — user explicitly rejected this for Phase 16 and for v2.1 milestone. Out of scope; "Fluent" prefix is not drift vocabulary. Do not re-propose in this milestone.
- **`IgnoredMultiMethodWarningFactory` class/file rename** — Phase 18 territory (NAME-07). Phase 16 touches its string literals only, not the type or file name.
- **Namespace `Converj.Generator.Diagnostics` restructuring** — explicitly out of v2.1 scope per PROJECT.md / REQUIREMENTS.md.

</deferred>

---

*Phase: 16-diagnostic-alignment*
*Context gathered: 2026-04-12*
