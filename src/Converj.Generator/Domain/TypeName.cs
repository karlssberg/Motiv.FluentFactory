namespace Converj.Generator.Domain;

internal class TypeName
{
    private readonly string _fullName;
    private const string GeneratorAttributesNamespace = "Converj.Attributes.";

    private TypeName(string fullName)
    {
        _fullName = fullName;
    }

    public static readonly TypeName FluentConstructorAttribute = new(GeneratorAttributesNamespace + "FluentConstructorAttribute");
    public static readonly TypeName GenericFluentConstructorAttribute = new(GeneratorAttributesNamespace + "FluentConstructorAttribute`1");
    public static readonly TypeName FluentFactoryAttribute = new(GeneratorAttributesNamespace + "FluentFactoryAttribute");
    public static readonly TypeName FluentMethodAttribute = new(GeneratorAttributesNamespace + "FluentMethodAttribute");
    public static readonly TypeName MultipleFluentMethodsAttribute = new(GeneratorAttributesNamespace + "MultipleFluentMethodsAttribute");
    public static readonly TypeName FluentMethodTemplateAttribute = new(GeneratorAttributesNamespace + "FluentMethodTemplateAttribute");
    public static readonly TypeName AsAttribute = new(GeneratorAttributesNamespace + "AsAttribute");
    public static readonly TypeName FluentParameterAttribute = new(GeneratorAttributesNamespace + "FluentParameterAttribute");
    public static readonly TypeName FluentStorageAttribute = new(GeneratorAttributesNamespace + "FluentStorageAttribute");

    public static readonly TypeName RequiredAttribute = new("System.ComponentModel.DataAnnotations.RequiredAttribute");

    public static implicit operator string(TypeName typeName) => typeName._fullName;
}
