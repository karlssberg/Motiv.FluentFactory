using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Converj.Generator.Tests.CSharpSourceGeneratorVerifier<Converj.Generator.FluentRootGenerator>;

namespace Converj.Generator.Tests;

// Scenario: a collection element type carries a type parameter that is not yet resolved
// by the forwarded/threaded parameters leading into the accumulator. Example:
//   class Train<TEngine, T>(TEngine engine, [FluentCollectionMethod] IEnumerable<Wheel<T>> wheels)
// At the point the accumulator is entered via WithTrainEngine, TEngine is bound but T is not.
// T enters the system only when the first AddWheel argument supplies it.
//
// Design:
//   - Pre-resolution step: same index, fewer generics. Forwarded fields only (no collection
//     field yet). Exposes a generic AddWheel<T>(in Wheel<T>) that transitions to the
//     resolved step, and a generic terminal CreateVehicles_Train<T>() for the empty-chain case.
//   - Resolved step: same index, extra generic for the newly bound parameter. Holds the
//     ImmutableArray<Wheel<T>> field, a non-generic AddWheel(in Wheel<T>), and the
//     non-generic terminal.
// The two structs share a base name and differ only in arity so both can coexist.
public class AccumulatorUnresolvedGenericTests
{
    [Fact(Skip = "Pending split-accumulator emission. Test bodies pinned; implementation lands in follow-up.")]
    internal async Task Should_split_accumulator_when_collection_element_carries_unresolved_generic()
    {
        const string code = """
            using System.Collections.Generic;
            namespace Test;

            public interface IEngine;
            public record Wheel<T>;

            [FluentRoot]
            public partial class Builder;

            [FluentTarget<Builder>]
            public partial record Train<TEngine, T>(
                [FluentMethod("WithEngine")] TEngine Engine,
                [FluentCollectionMethod] IEnumerable<Wheel<T>> Wheels)
                where TEngine : IEngine;
            """;

        // TO-BE-CAPTURED: run the test, paste actual output, replace version token.
        const string expected = "TO-BE-CAPTURED";

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                GeneratedSources =
                {
                    (typeof(FluentRootGenerator), "Test.Builder.g.cs", expected)
                }
            }
        };
        await test.RunAsync();
    }

    [Fact(Skip = "Pending split-accumulator emission. Generic terminal on pre-step lands in follow-up.")]
    internal async Task Should_support_empty_chain_via_generic_terminal_on_pre_step()
    {
        const string code = """
            using System.Collections.Generic;
            namespace Test;

            public interface IEngine;
            public class TrainEngine : IEngine;
            public record Wheel<T>;

            [FluentRoot]
            public partial class Builder;

            [FluentTarget<Builder>]
            public partial record Train<TEngine, T>(
                [FluentMethod("WithEngine")] TEngine Engine,
                [FluentCollectionMethod] IEnumerable<Wheel<T>> Wheels)
                where TEngine : IEngine;

            // Consumer proof the generic terminal is callable with explicit type arg:
            //   Train<TrainEngine, int> train = Builder.WithEngine(new TrainEngine()).CreateTrain<int>();
            """;

        const string expected = "TO-BE-CAPTURED";

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                GeneratedSources =
                {
                    (typeof(FluentRootGenerator), "Test.Builder.g.cs", expected)
                }
            }
        };
        await test.RunAsync();
    }

    [Fact]
    internal async Task Should_diagnose_when_multiple_collections_carry_disjoint_unresolved_generics()
    {
        const string code = """
            using System.Collections.Generic;
            namespace Test;

            public interface IEngine;
            public record Wheel<T>;
            public record Brake<U>;

            [FluentRoot]
            public partial class Builder;

            [FluentTarget<Builder>]
            public partial record Train<TEngine, T, U>(
                [FluentMethod("WithEngine")] TEngine Engine,
                [FluentCollectionMethod] IEnumerable<Wheel<T>> Wheels,
                [FluentCollectionMethod] IEnumerable<Brake<U>> Brakes)
                where TEngine : IEngine;
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("CVJG0054")
                        .WithSpan("/0/Test1.cs", 12, 23, 12, 28)
                        .WithArguments("Test.Train<TEngine, T, U>", "T, U")
                }
            }
        };
        await test.RunAsync();
    }
}
