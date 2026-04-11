using Converj.Attributes;

namespace Converj.Example;

internal class MultipleFluentMethods
{
    public void Run()
    {
        // FluentMethod on SecondA.value2 wins the Value2(int) signature over SecondB's
        // MultipleFluentMethods template, so this chain constructs SecondA.
        SecondA secondA = Factory
            .Build(2)
            .Value2(42)
            .Value3(100);
    }
}

[FluentRoot(TerminalMethod = TerminalMethod.None)]
internal partial class Factory;

[FluentTarget<Factory>]
internal partial record First(
    [FluentMethod("Build")]int value1);

[FluentTarget<Factory>]
#pragma warning disable CVJG0001
internal partial record SecondB(
#pragma warning restore CVJG0001
    [FluentMethod("Build")]int value1,
#pragma warning disable CVJG0002
    [MultipleFluentMethods(typeof(MultipleMethods))]int value2,
#pragma warning restore CVJG0002
    [FluentMethod("Value3")]int value3);

[FluentTarget<Factory>]
internal partial record SecondA(
    [FluentMethod("Build")]int value1,
    [FluentMethod("Value2")]int value2,
    [FluentMethod("Value3")]int value3);

internal static class MultipleMethods
{
    [FluentMethodTemplate]
    public static int Value2(int value2) => value2;
    [FluentMethodTemplate]
    public static int Value2(Func<int> value2) => value2();
}