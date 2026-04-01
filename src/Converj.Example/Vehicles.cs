using Converj.Attributes;

namespace Converj.Example;

internal partial class Vehicles
{
    public void Test()
    {
        Car<CarEngine> car = Vehicle.WithCarEngine(new CarEngine()).WithAge(5);
        Vehicle
            .WithCar(new Car<ICarEngine>(new CarEngine(), 5))
            .DispatchVehicle();
    }
    
    [FluentRoot]
    internal partial class Vehicle;

    internal interface ICarEngine;
    internal class CarEngine : ICarEngine;
    internal interface ITrainEngine;
    internal class TrainEngine : ITrainEngine;

    [FluentTarget<Vehicle>(BuilderMethod = BuilderMethod.None)]
    internal partial record Car<[As("TEngine")]TCarEngine>(
        [FluentMethod("WithCarEngine")] TCarEngine Engine,
        int Age) 
        where TCarEngine : ICarEngine;

    [FluentTarget<Vehicle>]
    internal partial record Train<TEngine>(
        [FluentMethod("WithTrainEngine")] TEngine Engine)
        where TEngine : ITrainEngine;
    
    [FluentTarget<Vehicle>]
    public static string DispatchVehicle(
        [FluentMethod("WithCar")]Car<ICarEngine>? car = null,
        [FluentMethod("WithTrain")]Train<ITrainEngine>? train = null)
    {
        if (car is not null)
            return $"Dispatched car: {car}";
        if (train is not null)
            return $"Dispatched train: {train}";
        return "No vehicle to dispatch";
    }
}
