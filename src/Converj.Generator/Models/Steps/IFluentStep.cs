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

    /// <summary>
    /// The extension receiver parameter, threaded through all steps in the chain.
    /// When set, the entry method is generated as an extension method on this parameter's type.
    /// </summary>
    IParameterSymbol? ReceiverParameter { get; set; }
}
