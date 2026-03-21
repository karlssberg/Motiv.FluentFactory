using Converg.Attributes;

namespace Converg.Example;

[FluentFactory(ReturnType = typeof(Animal))]
internal abstract partial record Animal;

[FluentConstructor(typeof(Animal))]
internal record Dog(int Legs = 4) : Animal;

[FluentConstructor(typeof(Animal))]
internal record Cat(int Legs = 4) : Animal;

[FluentConstructor(typeof(Animal))]
internal record Fish(int FinCount = 1, int EyeCount = 2) : Animal;