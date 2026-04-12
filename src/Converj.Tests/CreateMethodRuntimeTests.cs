using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot(TerminalMethod = TerminalMethod.DynamicSuffix)]
internal partial class DynamicCreateBuilder;

[FluentTarget<DynamicCreateBuilder>]
internal record DynamicTarget(int Value);

[FluentRoot(TerminalMethod = TerminalMethod.FixedName)]
internal partial class FixedCreateBuilder;

[FluentTarget<FixedCreateBuilder>]
internal record FixedTarget(int Value);

[FluentRoot(TerminalMethod = TerminalMethod.FixedName, TerminalVerb = "Build")]
internal partial class CustomVerbBuilder;

[FluentTarget<CustomVerbBuilder>]
internal record CustomVerbTarget(int Value);

[FluentRoot(TerminalMethod = TerminalMethod.None, MethodPrefix = "")]
internal partial class NoCreateBuilder;

[FluentTarget<NoCreateBuilder>]
internal partial record NoCreateStep1(int X);

[FluentTarget<NoCreateBuilder>]
internal partial record NoCreateStep2(int X, int Y);

#endregion

public class CreateMethodRuntimeTests
{
    [Fact]
    public void Dynamic_create_method_should_include_type_name()
    {
        var result = DynamicCreateBuilder.WithValue(42).CreateDynamicTarget();

        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Fixed_create_method_should_use_generic_create()
    {
        var result = FixedCreateBuilder.WithValue(42).Create();

        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Custom_verb_should_use_specified_verb()
    {
        var result = CustomVerbBuilder.WithValue(42).Build();

        result.Value.ShouldBe(42);
    }

    [Fact]
    public void None_create_method_should_return_target_type_directly()
    {
        NoCreateStep1 step1 = NoCreateBuilder.X(5);

        step1.X.ShouldBe(5);
    }

    [Fact]
    public void None_create_method_should_chain_through_partial_types()
    {
        NoCreateStep2 step2 = NoCreateBuilder.X(5).Y(10);

        step2.X.ShouldBe(5);
        step2.Y.ShouldBe(10);
    }
}
