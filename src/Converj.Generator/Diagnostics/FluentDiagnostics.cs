using Microsoft.CodeAnalysis;

namespace Converj.Generator.Diagnostics;

/// <summary>
/// Diagnostic descriptors for the fluent factory source generator.
/// </summary>
public static class FluentDiagnostics
{
    private const string Category = "FluentFactory";

    /// <summary>
    /// Diagnostic for unreachable fluent constructor.
    /// </summary>
    public static readonly DiagnosticDescriptor UnreachableConstructor = new(
        id: "CVJG0001",
        title: "Unreachable fluent constructor",
        messageFormat:
        "Unreachable fluent constructor '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.Unnecessary]);

    /// <summary>
    /// Diagnostic for superseded fluent method template.
    /// </summary>
    public static readonly DiagnosticDescriptor ContainsSupersededFluentMethodTemplate = new(
        id: "CVJG0002",
        title: "Multiple fluent method contains superseded method",
        messageFormat: "Ignoring fluent-method-template '{0}', used by the parameter '{1}' in the constructor '{2}'. Instead, {3}.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.Unnecessary]);

    /// <summary>
    /// Diagnostic for incompatible fluent method template.
    /// </summary>
    public static readonly DiagnosticDescriptor IncompatibleFluentMethodTemplate = new(
        id: "CVJG0003",
        title: "Fluent method template not compatible",
        category: Category,
        messageFormat: "Incompatible return type to the method '{0}'. It is not assignable to the fluent constructor parameter '{1}'.",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.Unnecessary]);

    /// <summary>
    /// Diagnostic for all fluent method templates being incompatible.
    /// </summary>
    public static readonly DiagnosticDescriptor AllFluentMethodTemplatesIncompatible = new(
        id: "CVJG0004",
        title: "All fluent method template incompatible",
        category: Category,
        messageFormat: "None of the fluent-method-templates have return types that are assignable to the fluent constructor parameter '{0}'",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.Unnecessary]);

    /// <summary>
    /// Diagnostic for fluent method template not being static.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentMethodTemplateAttributeNotStatic = new(
        id: "CVJG0005",
        title: "Fluent method template not static",
        category: Category,
        messageFormat: "Static method required '{0}'",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for fluent method template being superseded by a higher precedence parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentMethodTemplateSuperseded = new(
        id: "CVJG0006",
        title: "Fluent method template superseded",
        category: Category,
        messageFormat: "Fluent method template '{0}' is not being applied for the fluent constructor parameter '{1}' in constructor '{2}'. " +
            "This is because of the higher precedence afforded to fluent constructor parameter '{3}' in constructor '{4}'.",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for invalid create verb.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidCreateVerb = new(
        id: "CVJG0007",
        title: "Invalid CreateVerb",
        category: Category,
        messageFormat: "CreateVerb must be a valid identifier",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for duplicate create method name.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateCreateMethodName = new(
        id: "CVJG0008",
        title: "Duplicate create method name",
        category: Category,
        messageFormat: "Create method name must be unique",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for FluentConstructor target type missing FluentFactory attribute.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentConstructorTargetTypeMissingFluentFactory = new(
        id: "CVJG0009",
        title: "FluentConstructor target type missing FluentFactory attribute",
        category: Category,
        messageFormat: "FluentConstructor references type '{0}' which does not have the FluentFactory attribute",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for CreateVerb specified with CreateMethod.None.
    /// </summary>
    public static readonly DiagnosticDescriptor CreateVerbWithNone = new(
        "CVJG0010",
        title: "CreateVerb specified with CreateMethod.None",
        category: Category,
        messageFormat: "CreateVerb cannot be used with CreateMethod.None",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for constructors with unsupported parameter modifiers (ref, out, in, ref readonly).
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedParameterModifier = new(
        id: "CVJG0011",
        title: "Unsupported parameter modifier",
        messageFormat: "Constructor '{0}' has parameter '{1}' with unsupported modifier '{2}'. Fluent factory generation requires value-type parameters. This constructor will be skipped.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for constructors with inaccessible accessibility (private, protected).
    /// </summary>
    public static readonly DiagnosticDescriptor InaccessibleConstructor = new(
        id: "CVJG0012",
        title: "Inaccessible constructor",
        messageFormat: "Constructor '{0}' has '{1}' accessibility and cannot be used by the fluent factory. Only public and internal constructors are supported.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for factory root types missing the partial modifier.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingPartialModifier = new(
        id: "CVJG0013",
        title: "Factory type missing partial modifier",
        messageFormat: "Factory type '{0}' must be declared as partial to receive generated fluent methods",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for constructor parameter types less accessible than the factory type.
    /// </summary>
    public static readonly DiagnosticDescriptor InaccessibleParameterType = new(
        id: "CVJG0014",
        title: "Inaccessible parameter type in fluent factory",
        messageFormat: "Parameter '{0}' of type '{1}' in constructor '{2}' is less accessible than the factory type '{3}' and the generated fluent method will expose an inaccessible type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for factory accessibility exceeding target type accessibility.
    /// </summary>
    public static readonly DiagnosticDescriptor AccessibilityMismatch = new(
        id: "CVJG0015",
        title: "Factory accessibility exceeds target type",
        messageFormat: "Factory '{0}' is {1} but target type '{2}' is {3} and the generated factory may expose an inaccessible type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for ambiguous fluent method chains across different target types.
    /// </summary>
    public static readonly DiagnosticDescriptor AmbiguousFluentMethodChain = new(
        id: "CVJG0016",
        title: "Ambiguous fluent method chain",
        messageFormat:
        "Parameter '{0}' in constructor '{1}' produces fluent method '{2}' that creates an ambiguous fluent method chain across types {3}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for empty CreateVerb used with CreateMethod.None.
    /// </summary>
    public static readonly DiagnosticDescriptor EmptyCreateVerbWithNone = new(
        id: "CVJG0017",
        title: "Empty CreateVerb with CreateMethod.None",
        category: Category,
        messageFormat: "CreateVerb has no effect with CreateMethod.None",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for invalid method prefix.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidMethodPrefix = new(
        id: "CVJG0018",
        title: "Invalid MethodPrefix",
        category: Category,
        messageFormat: "MethodPrefix must be a valid identifier",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for target type not assignable to the specified ReturnType.
    /// </summary>
    public static readonly DiagnosticDescriptor ReturnTypeNotAssignable = new(
        id: "CVJG0019",
        title: "Target type not assignable to ReturnType",
        category: Category,
        messageFormat: "Target type '{0}' is not assignable to ReturnType '{1}'",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for ReturnType specified with CreateMethod.None.
    /// </summary>
    public static readonly DiagnosticDescriptor ReturnTypeWithNone = new(
        id: "CVJG0021",
        title: "ReturnType specified with CreateMethod.None",
        category: Category,
        messageFormat: "ReturnType cannot be used with CreateMethod.None because no creation method is generated",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for optional parameters causing ambiguous fluent method chains across different target types.
    /// </summary>
    public static readonly DiagnosticDescriptor OptionalParameterAmbiguousFluentMethodChain = new(
        id: "CVJG0022",
        title: "Optional parameters cause ambiguous fluent method chain",
        messageFormat:
        "Optional parameter '{0}' in constructor '{1}' causes fluent method chain to become ambiguous with '{2}' across types {3}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for ReturnType that equals the concrete target type (pointless usage).
    /// </summary>
    public static readonly DiagnosticDescriptor PointlessReturnType = new(
        id: "CVJG0020",
        title: "ReturnType equals concrete target type",
        category: Category,
        messageFormat: "ReturnType '{0}' is the same as the concrete target type and has no effect",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for conflicting generic type constraints producing duplicate method signatures.
    /// </summary>
    public static readonly DiagnosticDescriptor ConflictingTypeConstraints = new(
        id: "CVJG0023",
        title: "Conflicting type constraints produce duplicate method signatures",
        messageFormat:
        "Parameter '{0}' in constructor '{1}' produces fluent method '{2}' that conflicts with another constructor due to differing generic constraints across types {3}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for custom intermediate step types that have no accessible property or field
    /// for a constructor parameter, preventing value threading to subsequent steps.
    /// </summary>
    public static readonly DiagnosticDescriptor UnresolvableCustomStepStorage = new(
        id: "CVJG0024",
        title: "Custom step has no storage for constructor parameter",
        messageFormat:
        "Custom step type '{0}' has no accessible property or field for constructor parameter '{1}'. The parameter value cannot be threaded to subsequent steps.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for [FluentParameter] applied to a member of a type without [FluentFactory].
    /// </summary>
    public static readonly DiagnosticDescriptor FluentParameterWithoutFluentFactory = new(
        id: "CVJG0025",
        title: "FluentParameter on type without FluentFactory",
        messageFormat:
        "Member '{0}' is marked with [FluentParameter] but containing type '{1}' does not have the [FluentFactory] attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for [FluentParameter] applied to a member of a static factory type.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentParameterOnStaticFactory = new(
        id: "CVJG0026",
        title: "FluentParameter on static factory type",
        messageFormat:
        "Member '{0}' is marked with [FluentParameter] but containing type '{1}' is static. Fluent parameter threading requires a non-static factory type.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for [FluentParameter] applied to a property without a getter.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentParameterPropertyWithoutGetter = new(
        id: "CVJG0027",
        title: "FluentParameter property has no getter",
        messageFormat:
        "Property '{0}' is marked with [FluentParameter] but has no getter. The generator must be able to read the value to thread it to target constructors.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for duplicate [FluentParameter] mappings to the same target parameter name.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateFluentParameterMapping = new(
        id: "CVJG0028",
        title: "Duplicate FluentParameter mapping",
        messageFormat:
        "Multiple members map to target parameter '{0}': '{1}' and '{2}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for [FluentParameter] where the name matches a target constructor parameter
    /// but the type is not assignable.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentParameterTypeMismatch = new(
        id: "CVJG0029",
        title: "FluentParameter type mismatch",
        messageFormat:
        "Member '{0}' of type '{1}' matches target parameter name '{2}' but the type is not assignable to the target parameter type '{3}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for [FluentParameter] that does not match any target constructor parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentParameterNoMatch = new(
        id: "CVJG0030",
        title: "FluentParameter has no matching target parameter",
        messageFormat:
        "Member '{0}' with [FluentParameter(\"{1}\")] does not match any target constructor parameter",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for [FluentParameter] that matches some but not all target constructors.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentParameterPartialOverlap = new(
        id: "CVJG0031",
        title: "FluentParameter partial overlap",
        messageFormat:
        "Member '{0}' with [FluentParameter(\"{1}\")] matches only some target constructors. Set AllowPartialParameterOverlap = true on [FluentFactory] to allow this.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for target constructor parameter that has [FluentMethod] or [MultipleFluentMethods]
    /// but is pre-satisfied by a [FluentParameter] binding.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentParameterOverridesFluentMethod = new(
        id: "CVJG0032",
        title: "FluentParameter overrides FluentMethod",
        messageFormat:
        "Target parameter '{0}' has [FluentMethod] or [MultipleFluentMethods] but is pre-satisfied by [FluentParameter] on member '{1}'. The method attribute has no effect.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for static and instance entry methods with the same name and signature.
    /// </summary>
    public static readonly DiagnosticDescriptor StaticInstanceMethodNameCollision = new(
        id: "CVJG0033",
        title: "Static/instance method name collision",
        messageFormat:
        "Method '{0}' on '{1}' conflicts between static (self-referencing) and instance (external target) chains. Rename the parameter using [FluentMethod] on one of the target constructors.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for [FluentStorage] applied to a property without a getter.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentStoragePropertyWithoutGetter = new(
        id: "CVJG0035",
        title: "FluentStorage property has no getter",
        messageFormat:
        "Property '{0}' is marked with [FluentStorage] but has no getter. The generator must be able to read the value to use it as parameter storage.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for duplicate [FluentStorage] mappings to the same parameter name.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateFluentStorageMapping = new(
        id: "CVJG0036",
        title: "Duplicate FluentStorage mapping",
        messageFormat:
        "Multiple members map to storage parameter '{0}': '{1}' and '{2}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for multiple [FluentConstructor] attributes on the same type with CreateMethod.None.
    /// When CreateMethod.None is in effect, the target type itself becomes the fluent step and all
    /// generated methods live on the same type, preventing step ordering enforcement.
    /// </summary>
    public static readonly DiagnosticDescriptor MultipleConstructorsWithCreateMethodNone = new(
        id: "CVJG0037",
        title: "Multiple FluentConstructors with CreateMethod.None",
        messageFormat:
        "Type '{0}' has multiple [FluentConstructor] attributes for factory '{1}' with CreateMethod.None — only one constructor per type is allowed when CreateMethod.None is in effect",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for [FluentMethod] applied to a property without a set or init accessor.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentMethodOnPropertyWithoutSetter = new(
        id: "CVJG0038",
        title: "FluentMethod on property without setter",
        messageFormat:
        "Property '{0}' is marked with [FluentMethod] but has no set or init accessor. The generator cannot assign a value to this property.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for property support being used with CreateMethod.None.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentMethodPropertyWithCreateMethodNone = new(
        id: "CVJG0039",
        title: "Property support excluded from CreateMethod.None",
        messageFormat:
        "Required property '{0}' on type '{1}' cannot be used with CreateMethod.None. Property initialization via object initializer requires a creation method.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for an unresolvable name clash between a property and a constructor parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor PropertyNameClash = new(
        id: "CVJG0040",
        title: "Property name clashes with constructor parameter",
        messageFormat:
        "Property '{0}' on type '{1}' produces fluent method '{2}' that clashes with constructor parameter '{3}'. Use [FluentMethod(\"AlternateName\")] to rename.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for duplicate fluent method names produced by properties.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateFluentPropertyMethodName = new(
        id: "CVJG0041",
        title: "Duplicate fluent property method name",
        messageFormat:
        "Property '{0}' produces fluent method name '{1}' that conflicts with another property or parameter",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for [FluentMethod] without a method name on a constructor parameter, which has no effect.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentMethodNoEffectOnParameter = new(
        id: "CVJG0042",
        title: "FluentMethod without name has no effect on parameter",
        messageFormat:
        "[FluentMethod] without an explicit method name has no effect on constructor parameter '{0}'. Provide a method name or remove the attribute.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for ambiguous type-first entry method names across different target types.
    /// </summary>
    public static readonly DiagnosticDescriptor AmbiguousTypeFirstEntryMethod = new(
        id: "CVJG0043",
        title: "Ambiguous type-first entry method",
        messageFormat:
        "Type-first entry method '{0}' is ambiguous between types {1}. Disambiguate by setting TypeFirstVerb on [FluentConstructor] to change the entry method, or use CreateVerb with CreateMethod.Fixed to change the terminal method.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}