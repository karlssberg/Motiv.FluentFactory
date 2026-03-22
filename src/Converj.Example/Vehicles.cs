using Converj.Attributes;

namespace Converj.Example;

internal partial class Vehicles
{
    public void Test()
    {
        Car<CarEngine> car = Vehicle.WithCarEngine(new CarEngine()).WithAge(5);
    }
    
    [FluentFactory]
    internal partial class Vehicle;

    internal interface ICarEngine;
    internal class CarEngine : ICarEngine;
    internal interface ITrainEngine;
    internal class TrainEngine : ITrainEngine;

    [FluentConstructor<Vehicle>(CreateMethod = CreateMethod.None)]
    internal partial record Car<[As("TEngine")]TCarEngine>([FluentMethod("WithCarEngine")] TCarEngine Engine, int Age) where TCarEngine : ICarEngine;

    [FluentConstructor<Vehicle>]
    internal partial record Train<TEngine>([FluentMethod("WithTrainEngine")] TEngine Engine) where TEngine : ITrainEngine;
}
