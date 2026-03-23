using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Converj.Generator.ConstructorAnalysis;

namespace Converj.Generator;

[DebuggerDisplay("{ToString()}")]
internal class ExistingTypeFluentStep(
    ConstructorMetadata constructorMetadata
   ) : IFluentStep
{
#if DEBUG
    public int InstanceId => RuntimeHelpers.GetHashCode(this);
#endif
    public string Name { get; } = constructorMetadata.Constructor.ContainingType.ToUnqualifiedDisplayString();

    public string FullName => constructorMetadata.Constructor.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    /// <summary>
    ///     The known constructor parameters up until this step.
    ///     Potentially more parameters are required to satisfy a constructor signature.
    /// </summary>
    public ParameterSequence KnownConstructorParameters { get; set; } = [];

    public IList<IFluentMethod> FluentMethods { get; set; } = [];

    public ImmutableArray<IParameterSymbol> GenericConstructorParameters =>
    [
        ..constructorMetadata.Constructor.Parameters
            .Where(parameter => parameter.Type.IsOpenGenericType())
    ];

    public Accessibility Accessibility { get; } = constructorMetadata.Constructor.ContainingType.DeclaredAccessibility;

    public TypeKind TypeKind { get;  } = constructorMetadata.Constructor.ContainingType.TypeKind;

    public bool IsRecord { get; } = constructorMetadata.Constructor.ContainingType.IsRecord;

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; set; } = [];

    public ImmutableArray<FluentParameterBinding> ThreadedParameters { get; set; } = [];

    public ImmutableArray<IMethodSymbol> CandidateConstructors { get; set; }
    
    public FluentConstructorContext ConstructorContext => constructorMetadata.Context;

    public string IdentifierDisplayString()
    {
        return constructorMetadata.Constructor.ContainingType.ToGlobalDisplayString();
    }

    public string IdentifierDisplayString(
        IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap)
    {
        var distinctGenericParameters = this.GetGenericTypeArguments(genericTypeArgumentMap).ToArray();

        var existingStepConstructed = constructorMetadata.Constructor.ContainingType.Construct(distinctGenericParameters);

        return existingStepConstructed.ToGlobalDisplayString();
    }

    public INamespaceSymbol Namespace => constructorMetadata.Constructor.ContainingNamespace;
}
