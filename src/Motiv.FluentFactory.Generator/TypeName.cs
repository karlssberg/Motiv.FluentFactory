namespace Motiv.FluentFactory.Generator;

internal class TypeName
{
    private readonly string _fullName;
    private const string GeneratorAttributesNamespace = "Motiv.FluentFactory.Attributes.";

    private TypeName(string fullName)
    {
        _fullName = fullName;
    }

    public static readonly TypeName FluentConstructorAttribute = new(GeneratorAttributesNamespace + "FluentConstructorAttribute");
    public static readonly TypeName FluentFactoryAttribute = new(GeneratorAttributesNamespace + "FluentFactoryAttribute");
    public static readonly TypeName FluentMethodAttribute = new(GeneratorAttributesNamespace + "FluentMethodAttribute");
    public static readonly TypeName MultipleFluentMethodsAttribute = new(GeneratorAttributesNamespace + "MultipleFluentMethodsAttribute");
    public static readonly TypeName FluentMethodTemplateAttribute = new(GeneratorAttributesNamespace + "FluentMethodTemplateAttribute");

    public static implicit operator string(TypeName typeName) => typeName._fullName;
}
