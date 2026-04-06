using Converj.Attributes;

namespace Converj.Example;

[FluentRoot(ReturnType = typeof(Animal))]
internal abstract partial record Animal;

[FluentTarget(typeof(Animal))]
internal record Dog(int Legs = 4) : Animal;

[FluentTarget(typeof(Animal))]
internal record Cat(int Legs = 4) : Animal;

[FluentTarget(typeof(Animal))]
internal record Monster(int ArmCount = 1, int EyeCount = 2) : Animal;

[FluentTarget(typeof(Animal))]
internal record Bird(int Legs, (int Wings, string name) BirdThings) : Animal;