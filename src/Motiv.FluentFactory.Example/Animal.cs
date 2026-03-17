using Motiv.FluentFactory.Attributes;

namespace Motiv.FluentFactory.Example;

[FluentFactory(ReturnType = typeof(Animal))]
internal abstract partial class Animal;

[FluentConstructor(typeof(Animal))]
internal class Dog() : Animal;

[FluentConstructor(typeof(Animal))]
internal class Cat() : Animal;

[FluentConstructor(typeof(Animal))]
internal class Fish() : Animal;