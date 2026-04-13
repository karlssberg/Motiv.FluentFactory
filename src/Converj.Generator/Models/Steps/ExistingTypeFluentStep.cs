using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Converj.Generator.Extensions;
using Converj.Generator.TargetAnalysis;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.Models.Steps;

[DebuggerDisplay("{ToString()}")]
internal class ExistingTypeFluentStep(
    TargetMetadata targetMetadata
   ) : IFluentStep
{
#if DEBUG
    public int InstanceId => RuntimeHelpers.GetHashCode(this);
#endif
    public string Name { get; } = targetMetadata.Method.ContainingType.ToUnqualifiedDisplayString();

    public string FullName => targetMetadata.Method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    /// <summary>
    ///     The known constructor parameters up until this step.
    ///     Potentially more parameters are required to satisfy a constructor signature.
    /// </summary>
    public ParameterSequence KnownTargetParameters { get; set; } = [];

    public IList<IFluentMethod> FluentMethods { get; set; } = [];

    public ImmutableArray<IParameterSymbol> GenericTargetParameters =>
    [
        ..targetMetadata.Method.Parameters
            .Where(parameter => parameter.Type.IsOpenGenericType())
    ];

    public Accessibility Accessibility { get; } = targetMetadata.Method.ContainingType.DeclaredAccessibility;

    public TypeKind TypeKind { get;  } = targetMetadata.Method.ContainingType.TypeKind;

    public bool IsRecord { get; } = targetMetadata.Method.ContainingType.IsRecord;

    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> ValueStorage { get; set; } = [];

    public ImmutableArray<FluentParameterBinding> ThreadedParameters { get; set; } = [];

    /// <summary>
    /// The extension receiver parameter, threaded through all steps in the chain.
    /// </summary>
    public IParameterSymbol? ReceiverParameter { get; set; }

    public ImmutableArray<IMethodSymbol> CandidateTargets { get; set; }

    public ImmutableArray<IMethodSymbol> UnavailableTargets { get; set; } = [];
    
    public FluentTargetContext TargetContext => targetMetadata.Context;

    public string IdentifierDisplayString()
    {
        return targetMetadata.Method.ContainingType.ToGlobalDisplayString();
    }

    public string IdentifierDisplayString(
        IDictionary<FluentType, ITypeSymbol> genericTypeArgumentMap)
    {
        var distinctGenericParameters = this.GetGenericTypeArguments(genericTypeArgumentMap).ToArray();

        var existingStepConstructed = targetMetadata.Method.ContainingType.Construct(distinctGenericParameters);

        return existingStepConstructed.ToGlobalDisplayString();
    }

    public INamespaceSymbol Namespace => targetMetadata.Method.ContainingNamespace;
}
