using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

[FluentRoot]
internal partial class CustomMethodFactory;

[FluentTarget<CustomMethodFactory>]
internal record EngineTarget([FluentMethod("WithCarEngine")] string Engine, int Horsepower);

[FluentTarget<CustomMethodFactory>]
internal record MotorTarget([FluentMethod("WithBoatMotor")] string Engine);

#endregion

public class FluentMethodRuntimeTests
{
    [Fact]
    public void Custom_method_name_should_thread_value_correctly()
    {
        var result = CustomMethodFactory.WithCarEngine("V8").WithHorsepower(450).CreateEngineTarget();

        result.Engine.ShouldBe("V8");
        result.Horsepower.ShouldBe(450);
    }

    [Fact]
    public void Different_custom_method_names_should_create_separate_paths()
    {
        var result = CustomMethodFactory.WithBoatMotor("Outboard").CreateMotorTarget();

        result.Engine.ShouldBe("Outboard");
    }
}
