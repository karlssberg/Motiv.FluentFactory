using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

// Scenario 1: [FluentStorage] bridges the gap for a non-primary-constructor parameter.
// StorageStepA has ctors (x, y) and (x, y, z). Z is stored via [FluentStorage].
// Target takes (x, y, z) — matching the long constructor — so the generated Create
// on a struct step reads X, Y from the record and Z from [FluentStorage].
[FluentFactory]
internal partial class StorageFactory;

[FluentConstructor<StorageFactory>(CreateMethod = CreateMethod.None)]
internal partial record StorageStepA(int X, int Y);

internal partial record StorageStepA
{
    [FluentStorage]
    public string Z { get; init; } = "";

    [FluentConstructor<StorageFactory>(CreateMethod = CreateMethod.None)]
    public StorageStepA(int X, int Y, string Z) : this(X, Y) { this.Z = Z; }
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
internal partial record ExplicitStepA(int Id);

internal partial record ExplicitStepA
{
    [FluentStorage("label")]
    public string MappedLabel { get; init; } = "";

    [FluentConstructor<ExplicitStorageFactory>(CreateMethod = CreateMethod.None)]
    public ExplicitStepA(int Id, string Label) : this(Id) { MappedLabel = Label; }
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

// Scenario 3: Multi-constructor step with a nullable parameter (no explicit storage).
// The nullable parameter should default to null when skipped, and thread through when set.
[FluentFactory]
internal partial class NullableStorageFactory;

[FluentConstructor<NullableStorageFactory>(CreateMethod = CreateMethod.None)]
internal partial record NullableStepA(int A, int B);

internal partial record NullableStepA
{
    [FluentConstructor<NullableStorageFactory>(CreateMethod = CreateMethod.None)]
    public NullableStepA(int A, int B, string? Tag) : this(A, B) { this.Tag = Tag; }

    public string? Tag { get; init; }
}

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
    public void Nullable_parameter_without_value_should_default_to_null()
    {
        var result = NullableStorageFactory
            .WithA(1)
            .WithB(2)
            .CreateNullableTargetA();

        result.A.ShouldBe(1);
        result.B.ShouldBe(2);
        result.Tag.ShouldBeNull();
    }
}
