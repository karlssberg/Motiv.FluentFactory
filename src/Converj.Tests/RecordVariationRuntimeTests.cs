using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal partial class RecordBuilder;

[FluentTarget<RecordBuilder>]
internal record PositionalRecord(int X, int Y);

[FluentTarget<RecordBuilder>]
internal record struct PositionalRecordStruct(int X, int Y);

[FluentRoot]
internal partial class ClassBuilder;

[FluentTarget<ClassBuilder>]
internal class ClassTarget
{
    public int Value { get; }
    public string Name { get; }

    public ClassTarget(int value, string name)
    {
        Value = value;
        Name = name;
    }
}

#endregion

public class RecordVariationRuntimeTests
{
    [Fact]
    public void Record_target_should_receive_threaded_values()
    {
        var result = RecordBuilder.WithX(3).WithY(4).CreatePositionalRecord();

        result.X.ShouldBe(3);
        result.Y.ShouldBe(4);
    }

    [Fact]
    public void Record_struct_target_should_receive_threaded_values()
    {
        var result = RecordBuilder.WithX(5).WithY(6).CreatePositionalRecordStruct();

        result.X.ShouldBe(5);
        result.Y.ShouldBe(6);
    }

    [Fact]
    public void Class_target_should_receive_threaded_values()
    {
        var result = ClassBuilder.WithValue(10).WithName("test").CreateClassTarget();

        result.Value.ShouldBe(10);
        result.Name.ShouldBe("test");
    }
}
