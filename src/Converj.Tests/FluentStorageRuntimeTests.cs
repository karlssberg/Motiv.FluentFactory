using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

// Scenario 1: [FluentStorage] on a single-constructor step type.
// StorageStepA has ctor (x, y, z) with Z stored via [FluentStorage].
// Target takes (x, y, z) — the generated Create reads all values from the step.
[FluentFactory]
internal partial class StorageFactory;

[FluentConstructor<StorageFactory>(CreateMethod = CreateMethod.None)]
internal partial record StorageStepA(int X, int Y, string Z)
{
    [FluentStorage]
    public string Z { get; init; } = Z;
}

[FluentConstructor<StorageFactory>]
internal class StorageTargetA
{
    public int X { get; }
    public int Y { get; }
    public string Z { get; }

    public StorageTargetA(int x, int y, string z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

// Scenario 2: [FluentStorage] with explicit parameter name mapping.
[FluentFactory]
internal partial class ExplicitStorageFactory;

[FluentConstructor<ExplicitStorageFactory>(CreateMethod = CreateMethod.None)]
internal partial record ExplicitStepA(int Id, string Label)
{
    [FluentStorage("label")]
    public string MappedLabel { get; init; } = Label;
}

[FluentConstructor<ExplicitStorageFactory>]
internal class ExplicitTargetA
{
    public int Id { get; }
    public string Label { get; }

    public ExplicitTargetA(int id, string label)
    {
        Id = id;
        Label = label;
    }
}

// Scenario 3: Step with a nullable parameter threaded to target.
[FluentFactory]
internal partial class NullableStorageFactory;

[FluentConstructor<NullableStorageFactory>(CreateMethod = CreateMethod.None)]
internal partial record NullableStepA(int A, int B, string? Tag);

[FluentConstructor<NullableStorageFactory>]
internal class NullableTargetA
{
    public int A { get; }
    public int B { get; }
    public string? Tag { get; }

    public NullableTargetA(int a, int b, string? tag)
    {
        A = a;
        B = b;
        Tag = tag;
    }
}

// Scenario 4: Single constructor step with CreateMethod.None.
[FluentFactory(CreateMethod = CreateMethod.None)]
internal partial class TwoCtorStepFactory;

internal partial class TwoCtorStep
{
    [FluentConstructor<TwoCtorStepFactory>]
    public TwoCtorStep(int a, int b, double c)
    {
        A = a;
        B = b;
        C = c;
    }

    public int A { get; }
    public int B { get; }
    public double C { get; }
}

#endregion

public class FluentStorageRuntimeTests
{
    [Fact]
    public void FluentStorage_property_should_thread_value_to_final_target()
    {
        var result = StorageFactory
            .WithX(10)
            .WithY(20)
            .WithZ("hello")
            .CreateStorageTargetA();

        result.X.ShouldBe(10);
        result.Y.ShouldBe(20);
        result.Z.ShouldBe("hello");
    }

    [Fact]
    public void Explicit_parameter_name_should_thread_value_via_mapped_member()
    {
        var result = ExplicitStorageFactory
            .WithId(42)
            .WithLabel("test")
            .CreateExplicitTargetA();

        result.Id.ShouldBe(42);
        result.Label.ShouldBe("test");
    }

    [Fact]
    public void Nullable_parameter_with_value_should_thread_through_step()
    {
        var result = NullableStorageFactory
            .WithA(1)
            .WithB(2)
            .WithTag("tagged")
            .CreateNullableTargetA();

        result.A.ShouldBe(1);
        result.B.ShouldBe(2);
        result.Tag.ShouldBe("tagged");
    }

    [Fact]
    public void Nullable_parameter_with_null_should_thread_null_to_target()
    {
        var result = NullableStorageFactory
            .WithA(1)
            .WithB(2)
            .WithTag(null)
            .CreateNullableTargetA();

        result.A.ShouldBe(1);
        result.B.ShouldBe(2);
        result.Tag.ShouldBeNull();
    }

    [Fact]
    public void Single_ctor_step_should_thread_all_params()
    {
        var result = TwoCtorStepFactory
            .WithA(1)
            .WithB(5)
            .WithC(3.14);

        result.A.ShouldBe(1);
        result.B.ShouldBe(5);
        result.C.ShouldBe(3.14);
    }
}
