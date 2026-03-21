using Motiv.FluentFactory.Attributes;

namespace Motiv.FluentFactory.Example;

internal partial class Playground
{
    public void Test()
    {
        Entity.WithCarEngine(new CarEngine()).WithAge(5);
    }
    
    [FluentFactory]
    internal partial class Entity;

    internal interface ICarEngine;
    internal class CarEngine : ICarEngine;
    internal interface ITrainEngine;
    internal class TrainEngine : ITrainEngine;

    [FluentConstructor<Entity>(CreateMethod = CreateMethod.None)]
    internal partial record Car<[As("TEngine")]TCarEngine>([FluentMethod("WithCarEngine")] TCarEngine Engine, int Age) where TCarEngine : ICarEngine;

    [FluentConstructor<Entity>]
    internal partial record Train<TEngine>([FluentMethod("WithTrainEngine")] TEngine Engine) where TEngine : ITrainEngine;
}
