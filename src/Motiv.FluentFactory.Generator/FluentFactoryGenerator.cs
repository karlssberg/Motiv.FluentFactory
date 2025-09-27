using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Motiv.FluentFactory.Generator.Analysis;
using Motiv.FluentFactory.Generator.Generation.SyntaxElements;
using Motiv.FluentFactory.Generator.Model;

namespace Motiv.FluentFactory.Generator;

/// <summary>
/// Source generator for creating fluent factories based on constructors marked with the FluentConstructor attribute.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class FluentFactoryGenerator : IIncrementalGenerator
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
    /// Diagnostic for invalid create method name.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidCreateMethodName = new(
        id: "MFFG0007",
        title: "Invalid CreateMethodName",
        category: Category,
        messageFormat: "CreateMethodName must be a valid identifier",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic for duplicate create method name.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateCreateMethodName = new(
        id: "MFFG0008",
        title: "Duplicate CreateMethodName",
        category: Category,
        messageFormat: "CreateMethodName must be unique",
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
    /// Diagnostic for CreateMethodName specified with NoCreateMethod option.
    /// </summary>
    public static readonly DiagnosticDescriptor CreateMethodNameWithNoCreateMethod = new(
        "MFFG0010",
        title: "CreateMethodName specified with NoCreateMethod option",
        category: Category,
        messageFormat: "CreateMethodName cannot be used with NoCreateMethod option",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Initializes the source generator by setting up the incremental generation pipeline.
    /// </summary>
    /// <param name="context"></param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationProvider = context.CompilationProvider;

        // Step 1: Match FluentConstructorAttribute usages
        var typeOrConstructorDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(TypeName.FluentConstructorAttribute,
                (node, _) => node switch
                {
                    // Capture type declarations with FluentConstructor attributes (for applying to all constructors)
                    TypeDeclarationSyntax { AttributeLists.Count: > 0 } => true,
                    // Capture explicit constructors
                    ConstructorDeclarationSyntax { AttributeLists.Count: > 0 } => true,
                    _ => false
                },
                (ctx, _) =>
                {
                    var syntax = ctx.TargetNode;
                    var filePath = syntax.SyntaxTree.FilePath;
                    return (syntax, filePath);
                }
            );

        // Step 2: Gather all discovered candidate constructors and capture metadata
        var constructorModels = typeOrConstructorDeclarations
            .Combine(compilationProvider)
            .SelectMany((data, ct) =>
            {
                var compilation = data.Right;
                var syntax = data.Left.syntax;
                return CreateConstructorContexts(compilation, syntax, ct);
            })
            .WithTrackingName("ConstructorModelCreation");

        // Step 3: Transform constructor contexts into compiled file contents
        var consolidated = constructorModels
            .Collect()
            .Combine(compilationProvider)
            .WithTrackingName("ConstructorModelsConsolidation")
            .SelectMany((tuple, _) =>
            {
                var (builderContextsCollection, compilation) = tuple;
                return builderContextsCollection
                    .SelectMany(builderContexts => builderContexts)
                    .GroupBy(builderContext => builderContext.RootType, SymbolEqualityComparer.Default)
                    .Select(fluentApiConstructors =>
                        new FluentModelFactory(compilation)
                            .CreateFluentFactoryCompilationUnit(
                                (INamedTypeSymbol)fluentApiConstructors.Key!,
                                [..DeDuplicateFluentConstructors(fluentApiConstructors)]));
            })
            .WithTrackingName("ConstructorModelsToFluentBuilderFiles");

        // Step 4: Write the generated files.
        context.RegisterSourceOutput(consolidated, Execute);
    }

    private static IEnumerable<FluentConstructorContext> DeDuplicateFluentConstructors(
        IEnumerable<FluentConstructorContext> fluentApiConstructors) =>
        fluentApiConstructors
            .GroupBy(constructorContext => constructorContext.Constructor,
                SymbolEqualityComparer.Default)
            .SelectMany(ChooseOverridingConstructors);

    private static ImmutableList<FluentConstructorContext> ChooseOverridingConstructors(IEnumerable<FluentConstructorContext> duplicateConstructors)
    {
        var emptyList = ImmutableList<FluentConstructorContext>.Empty;
        var (usedOnType, usedOnConstructor) = duplicateConstructors
            .Aggregate(
                (OnType: emptyList, OnConstructor: emptyList),
                (whenAttributes, ctor) => ctor.IsAttributedUsedOnContainingType switch
                {
                    true => (whenAttributes.OnType.Add(ctor),
                        whenAttributes.OnConstructor),
                    false => (whenAttributes.OnType,
                        whenAttributes.OnConstructor.Add(ctor)),
                });

        return usedOnConstructor.Any()
            ? usedOnConstructor
            : usedOnType;
    }

    private static void Execute(
        SourceProductionContext context,
        FluentFactoryCompilationUnit builder)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        foreach (var diagnostic in builder.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
        if (builder.IsEmpty)
            return;

        var source = CompilationUnit.CreateCompilationUnit(builder).NormalizeWhitespace().ToString();
        context.AddSource($"{builder.RootType.ToFileName()}.g.cs", source);
    }

    private static ImmutableArray<IEnumerable<FluentConstructorContext>> CreateConstructorContexts(
        Compilation compilation,
        SyntaxNode syntaxTree,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var semanticModel = compilation.GetSemanticModel(syntaxTree.SyntaxTree);

        var symbol = semanticModel.GetDeclaredSymbol(syntaxTree);
        if (symbol is null)
            return [];

        return
        [
            ..GetFluentFactoryMetadata(symbol)
                .Select(metadata =>
                {
                    var attributePresent = metadata.AttributePresent;
                    var rootTypeFullName = metadata.RootTypeFullName;
                    if (!attributePresent || string.IsNullOrWhiteSpace(rootTypeFullName))
                        return [];

                    return symbol switch
                    {
                        IMethodSymbol constructor =>
                        [
                            new FluentConstructorContext(
                                constructor,
                                metadata.AttributeData!,
                                metadata.RootTypeSymbol,
                                metadata,
                                false,
                                semanticModel)
                        ],
                        INamedTypeSymbol type => CreateContainingTypeFluentConstructorContexts(
                            type,
                            metadata.RootTypeSymbol,
                            metadata),
                        _ => []
                    };
                })
        ];

        ImmutableArray<FluentConstructorContext> CreateContainingTypeFluentConstructorContexts(
            INamedTypeSymbol type,
            INamedTypeSymbol alreadyDeclaredRootType,
            FluentFactoryMetadata metadata)
        {
            return
            [
                ..type.Constructors
                    .Where(ctor => !ctor.IsImplicitlyDeclared)
                    .Select(ctor =>
                        new FluentConstructorContext(
                            ctor,
                            metadata.AttributeData!,
                            alreadyDeclaredRootType,
                            metadata,
                            true,
                            semanticModel))
            ];
        }
    }

    private static IEnumerable<FluentFactoryMetadata> GetFluentFactoryMetadata(ISymbol symbol)
    {
        return symbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == TypeName.FluentConstructorAttribute)
            .Select(attribute =>
            {
                // ensure an attribute is present and has an argument
                if (attribute is null || attribute.ConstructorArguments.Length == 0)
                    return FluentFactoryMetadata.Invalid;

                var typeArg = attribute.ConstructorArguments.FirstOrDefault();
                if (typeArg.IsNull || typeArg.Value is not INamedTypeSymbol typeSymbol)
                    return FluentFactoryMetadata.Invalid;

                // Grab the options flags symbol
                var optionsArgument = attribute.NamedArguments
                    .FirstOrDefault(namedArg => namedArg.Key == "Options")
                    .Value;
                var options = ConvertToFluentFactoryGeneratorOptions(optionsArgument);

                // Grab the create method name
                var createMethodNameArgument = attribute.NamedArguments
                    .FirstOrDefault(namedArg => namedArg.Key == "CreateMethodName")
                    .Value;
                var createMethodName = createMethodNameArgument.Value as string;

                return new FluentFactoryMetadata(typeSymbol)
                {
                    Options = options,
                    RootTypeFullName = typeSymbol.ToDisplayString(),
                    CreateMethodName = createMethodName,
                    AttributeData = attribute,
                };
            });
    }

    private static FluentFactoryGeneratorOptions ConvertToFluentFactoryGeneratorOptions(
        TypedConstant namedAttributeArgument)
    {
        if (namedAttributeArgument.Kind != TypedConstantKind.Enum)
            return FluentFactoryGeneratorOptions.None;

        // Get the underlying int value
        var value = (int?)namedAttributeArgument.Value ?? 0;

        // Get the type symbol for the enum
        if (namedAttributeArgument.Type is not INamedTypeSymbol enumType)
            return FluentFactoryGeneratorOptions.None;

        // Get all the declared members of the enum
        var flagMembers = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue && f.ConstantValue is int)
            .ToList();

        // Check which flags are set
        var setFlags = flagMembers
            .Where(member =>
            {
                var memberValue = (int?)member.ConstantValue ?? 0;
                return memberValue != 0 && (value & memberValue) == memberValue;
            })
            .ToList();

        if (setFlags.Count == 0)
            return FluentFactoryGeneratorOptions.None;

        return setFlags
            .Select(flag => Enum.TryParse<FluentFactoryGeneratorOptions>(flag.Name, true, out var option)
                ? option
                : FluentFactoryGeneratorOptions.None)
            .Aggregate((prev, next) => prev | next);
    }
}
