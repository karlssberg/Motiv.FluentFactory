using System.Collections.Immutable;
using System.Reflection;
using Converj.Generator.Models.Methods;
using Converj.Generator.Models.Parameters;
using Converj.Generator.Models.Steps;
using Converj.Generator.Models.Storage;
using Converj.Generator.TargetAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Converj.Generator.Tests.DomainModel;

/// <summary>
/// Unit tests for the <see cref="AccumulatorBulkMethod"/> and <see cref="AccumulatorBulkTransitionMethod"/>
/// domain types introduced in Phase 23 Plan 02.
/// These are pure domain tests using a minimal Roslyn compilation — no source generation is invoked.
/// </summary>
public class AccumulatorBulkMethodDomainTests
{
    // ── Test infrastructure ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a minimal Roslyn compilation sufficient for resolving IEnumerable&lt;T&gt;
    /// and a user-defined element type symbol.
    /// </summary>
    private static (Compilation Compilation, ITypeSymbol StringType) CreateMinimalCompilation()
    {
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
            ]);

        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        return (compilation, stringType);
    }

    private static AccumulatorFluentStep CreateDummyAccumulatorStep()
    {
        // Build a minimal named type symbol by getting System.Object from compilation
        var compilation = CSharpCompilation.Create(
            "Dummy",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var objectType = (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_Object);
        return new AccumulatorFluentStep(objectType);
    }

    private static CollectionParameterInfo CreateDummyCollectionParameterInfo(
        Compilation compilation,
        ITypeSymbol elementType)
    {
        // Create a dummy parameter symbol by parsing a minimal method
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            public class Holder
            {
                public void M(System.Collections.Generic.IList<string> tags) { }
            }
            """);

        var comp = CSharpCompilation.Create(
            "Dummy2",
            [syntaxTree],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
            ]);

        var holder = comp.GetTypeByMetadataName("Holder")!;
        var method = holder.GetMembers("M").OfType<IMethodSymbol>().Single();
        var parameter = method.Parameters[0];

        var listType = comp.GetTypeByMetadataName("System.Collections.Generic.IList`1")!
            .Construct(comp.GetSpecialType(SpecialType.System_String));

        return new CollectionParameterInfo(
            parameter: parameter,
            elementType: comp.GetSpecialType(SpecialType.System_String),
            declaredCollectionType: listType,
            methodName: "AddTag",
            minItems: 0);
    }

    // ── ISelfReturningAccumulatorMethod marker interface ───────────────────────

    [Fact]
    public void AccumulatorMethod_implements_ISelfReturningAccumulatorMethod_marker()
    {
        // AccumulatorMethod must implement the marker to be excluded from traversals.
        Assert.True(
            typeof(AccumulatorMethod).IsAssignableTo(typeof(ISelfReturningAccumulatorMethod)),
            $"{nameof(AccumulatorMethod)} must implement {nameof(ISelfReturningAccumulatorMethod)}");
    }

    [Fact]
    public void AccumulatorBulkMethod_implements_ISelfReturningAccumulatorMethod_marker()
    {
        Assert.True(
            typeof(AccumulatorBulkMethod).IsAssignableTo(typeof(ISelfReturningAccumulatorMethod)),
            $"{nameof(AccumulatorBulkMethod)} must implement {nameof(ISelfReturningAccumulatorMethod)}");
    }

    [Fact]
    public void AccumulatorBulkTransitionMethod_does_NOT_implement_ISelfReturningAccumulatorMethod_marker()
    {
        // Transition method lives on the preceding regular step — it is NOT self-returning.
        Assert.False(
            typeof(AccumulatorBulkTransitionMethod).IsAssignableTo(typeof(ISelfReturningAccumulatorMethod)),
            $"{nameof(AccumulatorBulkTransitionMethod)} must NOT implement {nameof(ISelfReturningAccumulatorMethod)}");
    }

    // ── AccumulatorBulkMethod construction ────────────────────────────────────

    [Fact]
    public void AccumulatorBulkMethod_MethodParameters_single_IEnumerable_of_element_type()
    {
        var (compilation, _) = CreateMinimalCompilation();
        var collectionInfo = CreateDummyCollectionParameterInfo(compilation, compilation.GetSpecialType(SpecialType.System_String));
        var step = CreateDummyAccumulatorStep();

        var bulkMethod = new AccumulatorBulkMethod(
            collectionParameter: collectionInfo,
            returnStep: step,
            rootNamespace: step.RootType.ContainingNamespace,
            availableParameterFields: ImmutableArray<FluentMethodParameter>.Empty,
            valueSources: new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(),
            bulkMethodName: "WithTags",
            compilation: compilation);

        var singleParam = Assert.Single(bulkMethod.MethodParameters);

        // The source type must be IEnumerable<ElementType>
        var expectedIEnumerable = compilation
            .GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
            .Construct(collectionInfo.ElementType);

        Assert.Equal(
            expectedIEnumerable.ToDisplayString(),
            singleParam.SourceType.ToDisplayString(),
            StringComparer.Ordinal);
    }

    [Fact]
    public void AccumulatorBulkMethod_Return_is_reference_equal_to_injected_accumulator_step()
    {
        var (compilation, _) = CreateMinimalCompilation();
        var collectionInfo = CreateDummyCollectionParameterInfo(compilation, compilation.GetSpecialType(SpecialType.System_String));
        var step = CreateDummyAccumulatorStep();

        var bulkMethod = new AccumulatorBulkMethod(
            collectionParameter: collectionInfo,
            returnStep: step,
            rootNamespace: step.RootType.ContainingNamespace,
            availableParameterFields: ImmutableArray<FluentMethodParameter>.Empty,
            valueSources: new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(),
            bulkMethodName: "WithTags",
            compilation: compilation);

        Assert.Same(step, bulkMethod.Return);
    }

    [Fact]
    public void AccumulatorBulkMethod_Name_returns_caller_provided_bulk_method_name()
    {
        var (compilation, _) = CreateMinimalCompilation();
        var collectionInfo = CreateDummyCollectionParameterInfo(compilation, compilation.GetSpecialType(SpecialType.System_String));
        var step = CreateDummyAccumulatorStep();

        var bulkMethod = new AccumulatorBulkMethod(
            collectionParameter: collectionInfo,
            returnStep: step,
            rootNamespace: step.RootType.ContainingNamespace,
            availableParameterFields: ImmutableArray<FluentMethodParameter>.Empty,
            valueSources: new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(),
            bulkMethodName: "WithTags",
            compilation: compilation);

        Assert.Equal("WithTags", bulkMethod.Name);
    }

    // ── AccumulatorBulkTransitionMethod construction ──────────────────────────

    [Fact]
    public void AccumulatorBulkTransitionMethod_MethodParameters_single_IEnumerable_of_element_type()
    {
        var (compilation, _) = CreateMinimalCompilation();
        var collectionInfo = CreateDummyCollectionParameterInfo(compilation, compilation.GetSpecialType(SpecialType.System_String));
        var step = CreateDummyAccumulatorStep();

        var transitionMethod = new AccumulatorBulkTransitionMethod(
            name: "WithTags",
            returnStep: step,
            rootNamespace: step.RootType.ContainingNamespace,
            availableParameterFields: ImmutableArray<FluentMethodParameter>.Empty,
            valueSources: new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(),
            collectionParameter: collectionInfo,
            compilation: compilation);

        var singleParam = Assert.Single(transitionMethod.MethodParameters);

        var expectedIEnumerable = compilation
            .GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
            .Construct(collectionInfo.ElementType);

        Assert.Equal(
            expectedIEnumerable.ToDisplayString(),
            singleParam.SourceType.ToDisplayString(),
            StringComparer.Ordinal);
    }

    [Fact]
    public void AccumulatorBulkTransitionMethod_Return_is_reference_equal_to_injected_accumulator_step()
    {
        var (compilation, _) = CreateMinimalCompilation();
        var collectionInfo = CreateDummyCollectionParameterInfo(compilation, compilation.GetSpecialType(SpecialType.System_String));
        var step = CreateDummyAccumulatorStep();

        var transitionMethod = new AccumulatorBulkTransitionMethod(
            name: "WithTags",
            returnStep: step,
            rootNamespace: step.RootType.ContainingNamespace,
            availableParameterFields: ImmutableArray<FluentMethodParameter>.Empty,
            valueSources: new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(),
            collectionParameter: collectionInfo,
            compilation: compilation);

        Assert.Same(step, transitionMethod.Return);
    }

    [Fact]
    public void AccumulatorBulkTransitionMethod_exposes_CollectionParameter_property()
    {
        var (compilation, _) = CreateMinimalCompilation();
        var collectionInfo = CreateDummyCollectionParameterInfo(compilation, compilation.GetSpecialType(SpecialType.System_String));
        var step = CreateDummyAccumulatorStep();

        var transitionMethod = new AccumulatorBulkTransitionMethod(
            name: "WithTags",
            returnStep: step,
            rootNamespace: step.RootType.ContainingNamespace,
            availableParameterFields: ImmutableArray<FluentMethodParameter>.Empty,
            valueSources: new OrderedDictionary<IParameterSymbol, IFluentValueStorage>(),
            collectionParameter: collectionInfo,
            compilation: compilation);

        Assert.Same(collectionInfo, transitionMethod.CollectionParameter);
    }
}
