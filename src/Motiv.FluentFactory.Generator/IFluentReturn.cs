using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator;

internal interface IFluentReturn
{
    /// <summary>
    /// Returns the display string for this return type, using global:: qualification.
    /// </summary>
    string IdentifierDisplayString();

    /// <summary>
    /// Returns the display string for this return type with generic type argument mappings applied.
    /// </summary>
    string IdentifierDisplayString(IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap);

    INamespaceSymbol Namespace { get; }

    /// <summary>
    /// The known constructor parameters up until this step.
    /// Potentially more parameters are required to satisfy a constructor signature.
    /// </summary>
    ParameterSequence KnownConstructorParameters { get; }

    ImmutableArray<IParameterSymbol> GenericConstructorParameters { get; }

    OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; }

    ImmutableArray<IMethodSymbol> CandidateConstructors { get; }
}
