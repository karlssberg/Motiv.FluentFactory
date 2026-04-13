using System.Collections.Immutable;
using Converj.Generator.Diagnostics;
using Converj.Generator.TargetAnalysis;
using Microsoft.CodeAnalysis;

namespace Converj.Generator.ModelBuilding;

/// <summary>
/// Resolves bindings between [FluentParameter] members and target constructor parameters,
/// and provides methods for querying and propagating those bindings.
/// </summary>
internal class ParameterBindingResolver(Compilation compilation, INamedTypeSymbol rootType, DiagnosticList diagnostics)
{
    private ImmutableArray<FluentParameterBinding> _threadedParameters = [];

    /// <summary>
    /// Gets the resolved threaded parameter bindings.
    /// </summary>
    public ImmutableArray<FluentParameterBinding> ThreadedParameters => _threadedParameters;

    /// <summary>
    /// Creates bindings between [FluentParameter] members and target constructor parameters.
    /// </summary>
    public void ResolveBindings(
        ImmutableArray<FluentParameterMember> fluentParameterMembers,
        ImmutableArray<FluentTargetContext> fluentTargetContexts)
    {
        if (fluentParameterMembers.IsEmpty)
        {
            _threadedParameters = [];
            return;
        }

        var bindings = ImmutableArray.CreateBuilder<FluentParameterBinding>();

        var allTargetParams = fluentTargetContexts
            .Where(ctx => !IsSelfReferencing(ctx.Method))
            .SelectMany(ctx => ctx.Method.Parameters)
            .ToList();

        foreach (var member in fluentParameterMembers)
        {
            var matchingParams = allTargetParams
                .Where(p => string.Equals(p.Name, member.TargetParameterName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingParams.Count == 0)
            {
                if (!member.IsImplicit)
                {
                    diagnostics.Add(Diagnostic.Create(
                        FluentDiagnostics.FluentParameterNoMatch,
                        member.Location,
                        member.MemberIdentifierName,
                        member.TargetParameterName));
                }

                continue;
            }

            // Check type assignability for all matches
            var allAssignable = true;
            foreach (var targetParam in matchingParams)
            {
                if (!AreTypesCompatible(compilation, member.Type, targetParam.Type))
                {
                    diagnostics.Add(Diagnostic.Create(
                        FluentDiagnostics.FluentParameterTypeMismatch,
                        member.Location,
                        member.MemberIdentifierName,
                        member.Type.ToDisplayString(),
                        targetParam.Name,
                        targetParam.Type.ToDisplayString()));
                    allAssignable = false;
                }
            }

            if (!allAssignable) continue;

            // Create bindings for each matching parameter (deduped by target method)
            var addedTargets = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            foreach (var targetParam in matchingParams)
            {
                var target = (IMethodSymbol)targetParam.ContainingSymbol;
                if (addedTargets.Add(target))
                {
                    bindings.Add(new FluentParameterBinding(member, targetParam));
                }
            }
        }

        _threadedParameters = bindings.ToImmutable();
    }

    /// <summary>
    /// Gets the set of pre-satisfied parameter names for a given constructor based on threaded bindings.
    /// Matches by parameter name against all bindings (constructor identity is validated during binding creation).
    /// </summary>
    public HashSet<string> GetPreSatisfiedParameterNames(IMethodSymbol constructor)
    {
        var constructorParamNames = new HashSet<string>(constructor.Parameters.Select(p => p.Name));
        return new HashSet<string>(
            _threadedParameters
                .Where(b => constructorParamNames.Contains(b.TargetParameter.Name))
                .Select(b => b.TargetParameter.Name));
    }

    /// <summary>
    /// Gets FluentMethodParameter entries for threaded parameters of a constructor,
    /// ordered by their position in the target constructor.
    /// </summary>
    public ImmutableArray<FluentMethodParameter> GetThreadedParameterFields(
        IMethodSymbol constructor,
        HashSet<string> preSatisfiedNames)
    {
        if (preSatisfiedNames.Count == 0)
            return [];

        return
        [
            ..constructor.Parameters
                .Where(p => preSatisfiedNames.Contains(p.Name))
                .Select(p => FluentMethodParameter.FromParameter(p, p.Name))
        ];
    }

    /// <summary>
    /// Merges threaded parameter storage into value sources, ordered by the target constructor's parameter positions.
    /// This ensures the final constructor call has all arguments in the correct order.
    /// </summary>
    public OrderedDictionary<IParameterSymbol, IFluentValueStorage> MergeThreadedValueSources(
        IMethodSymbol constructor,
        OrderedDictionary<IParameterSymbol, IFluentValueStorage> stepValueSources,
        HashSet<string> preSatisfiedNames)
    {
        if (preSatisfiedNames.Count == 0)
            return stepValueSources;

        // Build a new OrderedDictionary in the target constructor's parameter order
        var merged = new OrderedDictionary<IParameterSymbol, IFluentValueStorage>();

        foreach (var ctorParam in constructor.Parameters)
        {
            if (preSatisfiedNames.Contains(ctorParam.Name))
            {
                merged.Add(ctorParam, FieldStorage.FromParameter(ctorParam, rootType.ContainingNamespace));
            }
            else
            {
                // Step-acquired param — find it in the existing value sources
                var existingEntry = stepValueSources
                    .FirstOrDefault(kvp => kvp.Key.Name == ctorParam.Name);
                if (existingEntry.Value is not null)
                {
                    merged.Add(existingEntry.Key, existingEntry.Value);
                }
            }
        }

        return merged;
    }

    /// <summary>
    /// Sets ThreadedParameters on all steps and adds threaded param field storage to each step's ValueStorage.
    /// </summary>
    public void PropagateThreadedParametersToSteps(ImmutableArray<IFluentStep> steps)
    {
        foreach (var step in steps)
        {
            step.ThreadedParameters = _threadedParameters;

            foreach (var binding in _threadedParameters)
            {
                if (!step.ValueStorage.ContainsKey(binding.TargetParameter))
                {
                    step.ValueStorage.Add(binding.TargetParameter,
                        FieldStorage.FromParameter(binding.TargetParameter, rootType.ContainingNamespace));
                }
            }
        }
    }

    /// <summary>
    /// Propagates the extension receiver parameter to all steps in the chain.
    /// Prepends it to KnownTargetParameters and adds field storage.
    /// </summary>
    public static void PropagateReceiverToSteps(ImmutableArray<IFluentStep> steps, IParameterSymbol receiverParameter)
    {
        foreach (var step in steps)
        {
            step.ReceiverParameter = receiverParameter;

            // Prepend receiver to known parameters so it gets a field and constructor parameter
            if (step is RegularFluentStep regularStep)
            {
                regularStep.KnownTargetParameters = new ParameterSequence(
                    [receiverParameter, ..regularStep.KnownTargetParameters]);
            }

            if (!step.ValueStorage.ContainsKey(receiverParameter))
            {
                step.ValueStorage.Insert(0, receiverParameter,
                    FieldStorage.FromParameter(receiverParameter, step.Namespace));
            }
        }
    }

    /// <summary>
    /// Determines whether a constructor belongs to the same type as the fluent root.
    /// </summary>
    public bool IsSelfReferencing(IMethodSymbol constructor) =>
        SymbolEqualityComparer.Default.Equals(
            constructor.ContainingType.OriginalDefinition,
            rootType.OriginalDefinition);

    /// <summary>
    /// Checks type compatibility between a factory member type and a target constructor parameter type.
    /// Handles type parameters from different generic types by comparing ordinal positions,
    /// since they will be unified at instantiation time via the open generic type reference.
    /// </summary>
    public static bool AreTypesCompatible(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        if (compilation.HasImplicitConversion(sourceType, targetType))
            return true;

        // Type parameters from different generic types (e.g., T from Container<T> vs T from Widget<T>)
        // are different symbols but will be the same type at instantiation since they share
        // the same ordinal position via the open generic type reference.
        if (sourceType is ITypeParameterSymbol sourceTypeParam &&
            targetType is ITypeParameterSymbol targetTypeParam)
        {
            return sourceTypeParam.Ordinal == targetTypeParam.Ordinal;
        }

        return false;
    }
}
