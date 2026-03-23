using Converj.Attributes;

namespace Converj.Example;

internal interface IDependency;

[FluentFactory(CreateVerb = "Build", CreateMethod = CreateMethod.Fixed)]
internal partial record ServiceFactory(IDependency Dependency);

[FluentConstructor<ServiceFactory>]
internal record CustomizableService(IDependency Dependency, string Name);

internal class ConsumerService(ServiceFactory factory)
{
    public void UseDependency()
    {
        var targetDependency = factory.WithName("TargetDependency").Build();
    }
}