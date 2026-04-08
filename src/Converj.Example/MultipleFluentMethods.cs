using Converj.Attributes;

namespace Converj.Example;

public class MultipleFluentMethods
{
    
}

[FluentRoot(TerminalMethod = TerminalMethod.None)]
public partial class Factory;

[FluentTarget<Factory>]
public partial record First(
    [FluentMethod("Build")]int value1);


[FluentTarget<Factory>]
public partial record SecondB(
    [FluentMethod("Build")]int value1,
    [MultipleFluentMethods(typeof(MultipleMethods))]int value2,
    [FluentMethod("Value3")]int value3);

[FluentTarget<Factory>]
public partial record SecondA(
    [FluentMethod("Build")]int value1,
    [FluentMethod("Value2")]int value2,
    [FluentMethod("Value3")]int value3);

public static class MultipleMethods
{
    [FluentMethodTemplate]
    public static int Value2(int value2) => value2;
    [FluentMethodTemplate]
    public static int Value2(Func<int> value2) => value2();
}
