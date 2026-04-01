using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot(BuilderMethod = BuilderMethod.DynamicSuffix)]
internal partial class DynamicCreateFactory;

[FluentTarget<DynamicCreateFactory>]
internal record DynamicTarget(int Value);

[FluentRoot(BuilderMethod = BuilderMethod.FixedName)]
internal partial class FixedCreateFactory;

[FluentTarget<FixedCreateFactory>]
internal record FixedTarget(int Value);

[FluentRoot(BuilderMethod = BuilderMethod.FixedName, TerminalVerb = "Build")]
internal partial class CustomVerbFactory;

[FluentTarget<CustomVerbFactory>]
internal record CustomVerbTarget(int Value);

[FluentRoot(BuilderMethod = BuilderMethod.None, MethodPrefix = "")]
internal partial class NoCreateFactory;

[FluentTarget<NoCreateFactory>]
internal partial record NoCreateStep1(int X);

[FluentTarget<NoCreateFactory>]
internal partial record NoCreateStep2(int X, int Y);

#endregion

public class CreateMethodRuntimeTests
{
    [Fact]
    public void Dynamic_create_method_should_include_type_name()
    {
        var result = DynamicCreateFactory.WithValue(42).CreateDynamicTarget();

        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Fixed_create_method_should_use_generic_create()
    {
        var result = FixedCreateFactory.WithValue(42).Create();

        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Custom_verb_should_use_specified_verb()
    {
        var result = CustomVerbFactory.WithValue(42).Build();

        result.Value.ShouldBe(42);
    }

    [Fact]
    public void None_create_method_should_return_target_type_directly()
    {
        NoCreateStep1 step1 = NoCreateFactory.X(5);

        step1.X.ShouldBe(5);
    }

    [Fact]
    public void None_create_method_should_chain_through_partial_types()
    {
        NoCreateStep2 step2 = NoCreateFactory.X(5).Y(10);

        step2.X.ShouldBe(5);
        step2.Y.ShouldBe(10);
    }
}
