using Microsoft.CodeAnalysis;

namespace Motiv.FluentFactory.Generator.Diagnostics;

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
        id: "MFFG0001",
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
        id: "MFFG0002",
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
        id: "MFFG0003",
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
        id: "MFFG0004",
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
        id: "MFFG0005",
        title: "Fluent method template not static",
        category: Category,
        messageFormat: "Static method required '{0}'",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for fluent method template being superseded by a higher precedence parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentMethodTemplateSuperseded = new(
        id: "MFFG0006",
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
        id: "MFFG0007",
        title: "Invalid CreateVerb",
        category: Category,
        messageFormat: "CreateVerb must be a valid identifier",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for duplicate create method name.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateCreateMethodName = new(
        id: "MFFG0008",
        title: "Duplicate create method name",
        category: Category,
        messageFormat: "Create method name must be unique",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for FluentConstructor target type missing FluentFactory attribute.
    /// </summary>
    public static readonly DiagnosticDescriptor FluentConstructorTargetTypeMissingFluentFactory = new(
        id: "MFFG0009",
        title: "FluentConstructor target type missing FluentFactory attribute",
        category: Category,
        messageFormat: "FluentConstructor references type '{0}' which does not have the FluentFactory attribute",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for CreateVerb specified with CreateMethod.None.
    /// </summary>
    public static readonly DiagnosticDescriptor CreateVerbWithNone = new(
        "MFFG0010",
        title: "CreateVerb specified with CreateMethod.None",
        category: Category,
        messageFormat: "CreateVerb cannot be used with CreateMethod.None",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for constructors with unsupported parameter modifiers (ref, out, in, ref readonly).
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedParameterModifier = new(
        id: "MFFG0011",
        title: "Unsupported parameter modifier",
        messageFormat: "Constructor '{0}' has parameter '{1}' with unsupported modifier '{2}'. Fluent factory generation requires value-type parameters. This constructor will be skipped.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for constructors with inaccessible accessibility (private, protected).
    /// </summary>
    public static readonly DiagnosticDescriptor InaccessibleConstructor = new(
        id: "MFFG0012",
        title: "Inaccessible constructor",
        messageFormat: "Constructor '{0}' has '{1}' accessibility and cannot be used by the fluent factory. Only public and internal constructors are supported.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for factory root types missing the partial modifier.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingPartialModifier = new(
        id: "MFFG0013",
        title: "Factory type missing partial modifier",
        messageFormat: "Factory type '{0}' must be declared as partial to receive generated fluent methods",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for constructor parameter types less accessible than the factory type.
    /// </summary>
    public static readonly DiagnosticDescriptor InaccessibleParameterType = new(
        id: "MFFG0014",
        title: "Inaccessible parameter type in fluent factory",
        messageFormat: "Parameter '{0}' of type '{1}' in constructor '{2}' is less accessible than the factory type '{3}' and the generated fluent method will expose an inaccessible type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for factory accessibility exceeding target type accessibility.
    /// </summary>
    public static readonly DiagnosticDescriptor AccessibilityMismatch = new(
        id: "MFFG0015",
        title: "Factory accessibility exceeds target type",
        messageFormat: "Factory '{0}' is {1} but target type '{2}' is {3} and the generated factory may expose an inaccessible type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for ambiguous fluent method chains across different target types.
    /// </summary>
    public static readonly DiagnosticDescriptor AmbiguousFluentMethodChain = new(
        id: "MFFG0016",
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
        id: "MFFG0017",
        title: "Empty CreateVerb with CreateMethod.None",
        category: Category,
        messageFormat: "CreateVerb has no effect with CreateMethod.None",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}