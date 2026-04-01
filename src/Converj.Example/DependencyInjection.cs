using Converj.Attributes;

namespace Converj.Example;

internal interface IDependency;

[FluentRoot(TerminalVerb = "Build", BuilderMethod = BuilderMethod.FixedName)]
internal partial record ServiceFactory(IDependency Dependency);

[FluentTarget<ServiceFactory>]
internal record CustomizableService(IDependency Dependency, string Name);

internal class ConsumerService(ServiceFactory factory)
{
    public void UseDependency()
    {
        var targetDependency = factory.WithName("TargetDependency").Build();
    }
}