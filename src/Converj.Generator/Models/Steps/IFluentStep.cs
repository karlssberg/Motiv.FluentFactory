using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Steps;

internal interface IFluentStep : IFluentReturn
{
    string Name { get; }

    string FullName { get; }

    IList<IFluentMethod> FluentMethods { get; }

    Accessibility Accessibility { get; }

    TypeKind TypeKind { get; }

    bool IsRecord { get; }

    /// <summary>
    /// Parameters threaded from the factory root type via [FluentParameter] bindings.
    /// These are carried as additional fields on the step but are not part of the fluent step chain.
    /// </summary>
    ImmutableArray<FluentParameterBinding> ThreadedParameters { get; set; }
}
