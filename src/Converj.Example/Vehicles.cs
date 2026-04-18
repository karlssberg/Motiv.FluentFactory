using Converj.Attributes;

namespace Converj.Example;

internal partial class Vehicles
{
    public void Test()
    {
        Car<CarEngine> car = Vehicle.WithCarEngine(new CarEngine()).WithAge(5);
        Vehicle
            .WithCar<int>(new Car<ICarEngine>(new CarEngine(), 5))
            .DispatchVehicle();
        Vehicle
            .WithTrain(new Train<ITrainEngine, int>(new TrainEngine(), []))
            .DispatchVehicle();

        Train<TrainEngine, int> train = Vehicle
            .WithTrainEngine(new TrainEngine())
            .AddWheel(new Wheel<int>())
            .AddWheel(new Wheel<int>())
            .CreateVehicles_Train();
    }
    
    [FluentRoot]
    internal partial class Vehicle;

    internal interface ICarEngine;
    internal class CarEngine : ICarEngine;
    internal interface ITrainEngine;
    internal class TrainEngine : ITrainEngine;
    internal record Wheel<T>;

    [FluentTarget<Vehicle>(TerminalMethod = TerminalMethod.None)]
    internal partial record Car<[As("TEngine")]TCarEngine>(
        [FluentMethod("WithCarEngine")] TCarEngine Engine,
        int Age) 
        where TCarEngine : ICarEngine;

    [FluentTarget<Vehicle>]
    internal partial record Train<TEngine, T>(
        [FluentMethod("WithTrainEngine")] TEngine Engine,
        [FluentCollectionMethod] IEnumerable<Wheel<T>> Wheels)
        where TEngine : ITrainEngine;
    
    [FluentTarget<Vehicle>]
    public static string DispatchVehicle<T>(
        [FluentMethod("WithCar")]Car<ICarEngine>? car = null,
        [FluentMethod("WithTrain")]Train<ITrainEngine, T>? train = null)
    {
        return (car, train) switch
        {
            (not null, not null) => 
                $"Both car ({car}) and train ({train}) are dispatched",
            
            (not null, null) =>
                $"Dispatched car: {car}",
            
            (null, not null) =>
                $"Dispatched train: {train}",
            
            _ =>
                "No vehicle to dispatch"
        };
    }
}
