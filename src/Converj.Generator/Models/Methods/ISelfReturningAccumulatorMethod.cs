namespace Converj.Generator.Models.Methods;

/// <summary>
/// Marker interface implemented by every self-returning accumulator method on an
/// <see cref="Steps.AccumulatorFluentStep"/>. Used by <c>FluentModelBuilder</c> and
/// <c>FluentStepBuilder</c> to exclude these methods from descendant step traversals —
/// self-return would otherwise cause infinite recursion (see Phase 22 Plan 04 STATE.md
/// decision; Phase 23 RESEARCH.md Pitfall 8).
/// </summary>
internal interface ISelfReturningAccumulatorMethod
{
}
