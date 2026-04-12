using Converj.Attributes;
using Shouldly;

namespace Converj.Tests;

#region Test types

internal interface IAnimal
{
    int Legs { get; }
}

[FluentRoot(ReturnType = typeof(IAnimal))]
internal partial class AnimalBuilder;

[FluentTarget<AnimalBuilder>]
internal record Parrot(int Legs) : IAnimal;

[FluentTarget<AnimalBuilder>]
internal record Spider(int Legs) : IAnimal;

internal abstract record BaseVehicle(int Speed);

[FluentRoot(ReturnType = typeof(BaseVehicle))]
internal partial class VehicleBuilder;

[FluentTarget<VehicleBuilder>]
internal record Bicycle(int Speed) : BaseVehicle(Speed);

#endregion

public class ReturnTypeRuntimeTests
{
    [Fact]
    public void Interface_return_type_should_return_correct_concrete_type()
    {
        IAnimal parrot = AnimalBuilder.WithLegs(2).CreateParrot();

        parrot.ShouldBeOfType<Parrot>();
        parrot.Legs.ShouldBe(2);
    }

    [Fact]
    public void Interface_return_type_should_work_for_multiple_targets()
    {
        IAnimal spider = AnimalBuilder.WithLegs(8).CreateSpider();

        spider.ShouldBeOfType<Spider>();
        spider.Legs.ShouldBe(8);
    }

    [Fact]
    public void Base_class_return_type_should_return_correct_concrete_type()
    {
        BaseVehicle bike = VehicleBuilder.WithSpeed(25).CreateBicycle();

        bike.ShouldBeOfType<Bicycle>();
        bike.Speed.ShouldBe(25);
    }
}
