using System.Collections.Immutable;
using Converj.Generator.Extensions;
using Converj.Generator.Models.Methods;
using Converj.Generator.Models.Steps;
using Converj.Generator.SyntaxGeneration.Helpers;
using Converj.Generator.TargetAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converj.Generator.SyntaxGeneration;

/// <summary>
/// Emits the <c>readonly struct</c> declaration for an <see cref="AccumulatorFluentStep"/>.
/// Produces two constructors (entry + private copy), one <c>ImmutableArray&lt;T&gt;</c> field per
/// collection parameter initialised to <c>.Empty</c> (GEN-04), <c>AddX</c> self-returning methods
/// with <c>[MethodImpl(AggressiveInlining)]</c> (GEN-06), and a terminal method that converts
/// accumulated fields to declared collection types (GEN-02).
/// </summary>
internal static class AccumulatorStepDeclaration
{
    private const string ImmutableArrayGlobal =
        "global::System.Collections.Immutable.ImmutableArray";

    /// <summary>
    /// Creates the <see cref="StructDeclarationSyntax"/> for the given accumulator step.
    /// The emitted struct is unconditionally <c>readonly</c> (GEN-06 / RESEARCH.md Pitfall 5).
    /// Handles both parameter-backed and property-backed collection accumulators.
    /// Property-backed accumulators use <c>__property</c> field naming and emit object-initializer
    /// assignment in the terminal method.
    /// </summary>
    /// <param name="step">The accumulator step model to emit.</param>
    /// <returns>A <see cref="StructDeclarationSyntax"/> representing the complete accumulator step struct.</returns>
    public static StructDeclarationSyntax Create(AccumulatorFluentStep step)
    {
        var forwardedFields = CreateForwardedFieldDeclarations(step);
        var accumulatorFields = CreateAccumulatorFieldDeclarations(step);
        var propertyAccumulatorFields = CreatePropertyAccumulatorFieldDeclarations(step);
        var entryCtor = CreateEntryConstructor(step);
        var copyCtor = CreateCopyConstructor(step);
        var addMethods = CreateAddMethods(step);
        var addPropertyMethods = CreatePropertyAddMethods(step);
        var terminalMethod = CreateTerminalMethod(step);

        var modifiers = GetAccessibilityTokens(step.Accessibility);

        var typeParameterList = CreateTypeParameterList(step);
        var constraintClauses = CreateConstraintClauses(step);

        return StructDeclaration(step.Name)
            .WithModifiers(modifiers)
            .WithTypeParameterList(typeParameterList)
            .WithConstraintClauses(constraintClauses)
            .WithAttributeLists(SingletonList(Helpers.GeneratedCodeAttributeSyntax.Create()))
            .WithMembers(List<MemberDeclarationSyntax>([
                ..forwardedFields,
                ..accumulatorFields,
                ..propertyAccumulatorFields,
                entryCtor,
                copyCtor,
                ..addMethods,
                ..addPropertyMethods,
                terminalMethod
            ]));
    }

    /// <summary>
    /// Builds the type parameter list for the accumulator struct from the effective generic
    /// parameters carried by forwarded / threaded parameters (e.g., <c>&lt;TEngine&gt;</c>).
    /// Returns <see langword="null"/> when no generic parameters are present.
    /// </summary>
    private static TypeParameterListSyntax? CreateTypeParameterList(AccumulatorFluentStep step)
    {
        var typeArguments = step.GetDistinctEffectiveTypeArguments();
        if (typeArguments.Length == 0)
            return null;

        return TypeParameterList(SeparatedList(
            typeArguments.Select(arg => TypeParameter(arg.GetEffectiveName()))));
    }

    /// <summary>
    /// Builds the constraint clauses for the accumulator struct type parameters.
    /// Collects type parameters from the candidate target containing type (and receiver when present),
    /// matches them to the accumulator's effective type arguments by name, and emits the constraints
    /// (e.g., <c>where TEngine : global::Ns.IEngine</c>).
    /// </summary>
    private static SyntaxList<TypeParameterConstraintClauseSyntax> CreateConstraintClauses(AccumulatorFluentStep step)
    {
        var typeArguments = step.GetDistinctEffectiveTypeArguments();
        if (typeArguments.Length == 0)
            return List<TypeParameterConstraintClauseSyntax>();

        var candidateTypeParameters = new List<ITypeParameterSymbol>();

        foreach (var target in step.CandidateTargets)
        {
            if (target.ContainingType is { IsGenericType: true } targetType)
                candidateTypeParameters.AddRange(targetType.OriginalDefinition.TypeParameters);
        }

        if (step.ReceiverParameter?.Type is INamedTypeSymbol { IsGenericType: true } receiverType)
            candidateTypeParameters.AddRange(receiverType.OriginalDefinition.TypeParameters);

        var effectiveNames = new HashSet<string>(typeArguments.Select(tp => tp.GetEffectiveName()));
        var matchedParameters = candidateTypeParameters
            .Where(tp => effectiveNames.Contains(tp.GetEffectiveName()))
            .DistinctBy(tp => tp.GetEffectiveName())
            .ToImmutableArray();

        if (matchedParameters.Length == 0)
            return List<TypeParameterConstraintClauseSyntax>();

        return List(Helpers.TypeParameterConstraintBuilder.Create(matchedParameters));
    }

    // ── Modifier list ─────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the struct modifier token list.
    /// <c>ReadOnlyKeyword</c> is included unconditionally (GEN-06 / RESEARCH.md Pitfall 5).
    /// </summary>
    private static SyntaxTokenList GetAccessibilityTokens(Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public =>
                TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
            Accessibility.Private =>
                TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
            Accessibility.Protected =>
                TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
            Accessibility.Internal =>
                TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
            Accessibility.ProtectedOrInternal =>
                TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
            Accessibility.ProtectedAndInternal =>
                TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
            _ =>
                TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ReadOnlyKeyword))
        };

    // ── Field declarations ────────────────────────────────────────────────────

    /// <summary>
    /// Emits one <c>private readonly {Type} _{name}__parameter</c> field per forwarded (non-collection)
    /// constructor parameter (RESEARCH.md Pitfall 8).
    /// </summary>
    private static ImmutableArray<FieldDeclarationSyntax> CreateForwardedFieldDeclarations(
        AccumulatorFluentStep step) =>
        FieldAndPropertySyntax.CreateDeclarations(step.ValueStorage)
            .OfType<FieldDeclarationSyntax>()
            .ToImmutableArray();

    /// <summary>
    /// Emits one <c>private readonly global::System.Collections.Immutable.ImmutableArray&lt;ElementType&gt;
    /// _{paramName}__parameter</c> field per collection parameter (GEN-04).
    /// </summary>
    private static ImmutableArray<FieldDeclarationSyntax> CreateAccumulatorFieldDeclarations(
        AccumulatorFluentStep step) =>
        step.CollectionParameters
            .Select(cp => FieldDeclaration(
                    VariableDeclaration(
                        ParseTypeName($"{ImmutableArrayGlobal}<{cp.ElementType.ToGlobalDisplayString()}>"))
                        .AddVariables(VariableDeclarator(
                            Identifier(cp.Parameter.Name.ToParameterFieldName()))))
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PrivateKeyword),
                    Token(SyntaxKind.ReadOnlyKeyword))))
            .ToImmutableArray();

    /// <summary>
    /// Emits one <c>private readonly global::System.Collections.Immutable.ImmutableArray&lt;ElementType&gt;
    /// _{propertyName}__property</c> field per property-backed collection accumulator.
    /// Uses the <c>__property</c> suffix to distinguish from parameter-backed fields.
    /// </summary>
    private static ImmutableArray<FieldDeclarationSyntax> CreatePropertyAccumulatorFieldDeclarations(
        AccumulatorFluentStep step) =>
        step.CollectionProperties
            .Select(cp => FieldDeclaration(
                    VariableDeclaration(
                        ParseTypeName($"{ImmutableArrayGlobal}<{cp.ElementType.ToGlobalDisplayString()}>"))
                        .AddVariables(VariableDeclarator(
                            Identifier(cp.Property.Name.ToPropertyFieldName()))))
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PrivateKeyword),
                    Token(SyntaxKind.ReadOnlyKeyword))))
            .ToImmutableArray();

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <summary>
    /// Emits the entry constructor: parameters = forwarded fields only; initialises every accumulator
    /// field to <c>ImmutableArray&lt;T&gt;.Empty</c> (GEN-04 / RESEARCH.md Pitfall 1).
    /// </summary>
    private static ConstructorDeclarationSyntax CreateEntryConstructor(AccumulatorFluentStep step)
    {
        var forwardedParams = BuildForwardedConstructorParameters(step);
        var forwardedAssignments = BuildForwardedFieldAssignments(step);
        var emptyInitializations = BuildEmptyInitializations(step);
        var propertyEmptyInitializations = BuildPropertyEmptyInitializations(step);

        var allStatements = forwardedAssignments
            .Concat(emptyInitializations)
            .Concat(propertyEmptyInitializations)
            .ToArray();

        var accessModifier = forwardedParams.Any()
            ? step.Accessibility.AccessibilityToSyntaxKind().Select(Token)
            : [Token(SyntaxKind.PublicKeyword)]; // parameterless structs must be public per C# spec

        return ConstructorDeclaration(Identifier(step.Name))
            .WithModifiers(TokenList(accessModifier))
            .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(
                forwardedParams.InterleaveWith(Token(SyntaxKind.CommaToken)))))
            .WithBody(Block(allStatements));
    }

    /// <summary>
    /// Emits the private copy constructor: parameters = forwarded fields + accumulator fields;
    /// body assigns each field directly. Used by <c>AddX</c> return paths (RESEARCH.md Pattern 4).
    /// </summary>
    private static ConstructorDeclarationSyntax CreateCopyConstructor(AccumulatorFluentStep step)
    {
        var forwardedParams = BuildForwardedConstructorParameters(step);
        var accumulatorParams = BuildAccumulatorConstructorParameters(step);
        var propertyAccumulatorParams = BuildPropertyAccumulatorConstructorParameters(step);

        var allParams = forwardedParams
            .Concat(accumulatorParams)
            .Concat(propertyAccumulatorParams)
            .InterleaveWith(Token(SyntaxKind.CommaToken));

        var forwardedAssignments = BuildForwardedFieldAssignments(step);
        var accumulatorAssignments = BuildAccumulatorFieldAssignments(step);
        var propertyAccumulatorAssignments = BuildPropertyAccumulatorFieldAssignments(step);

        var allStatements = forwardedAssignments
            .Concat(accumulatorAssignments)
            .Concat(propertyAccumulatorAssignments)
            .ToArray();

        return ConstructorDeclaration(Identifier(step.Name))
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(allParams)))
            .WithBody(Block(allStatements));
    }

    // ── AddX methods ──────────────────────────────────────────────────────────

    private const string IEnumerableGlobal =
        "global::System.Collections.Generic.IEnumerable";

    /// <summary>
    /// Emits one <c>public {StepType} AddX(in ElementType item)</c> method per
    /// <see cref="AccumulatorMethod"/> in <c>step.FluentMethods</c>, plus one
    /// <c>public {StepType} WithXs(IEnumerable&lt;ElementType&gt; items)</c> per
    /// <see cref="AccumulatorBulkMethod"/> (Phase 23 Plan 02 composability feature).
    /// Each method carries <c>[MethodImpl(AggressiveInlining)]</c> (GEN-06) and returns a new
    /// struct instance via the private copy constructor, forwarding all non-target fields unchanged
    /// and updating the target accumulator field via <c>.Add(item)</c> or <c>.AddRange(items)</c>.
    /// </summary>
    private static ImmutableArray<MethodDeclarationSyntax> CreateAddMethods(AccumulatorFluentStep step)
    {
        var stepGlobalName = step.IdentifierDisplayString();

        var singleAdds = step.FluentMethods
            .OfType<AccumulatorMethod>()
            .Select(method => CreateAddMethod(step, method, stepGlobalName));

        var bulkAdds = step.FluentMethods
            .OfType<AccumulatorBulkMethod>()
            .Select(method => CreateBulkMethod(step, method, stepGlobalName));

        return [..singleAdds, ..bulkAdds];
    }

    private static MethodDeclarationSyntax CreateAddMethod(
        AccumulatorFluentStep step,
        AccumulatorMethod method,
        string stepGlobalName)
    {
        var cp = method.CollectionParameter;
        var elementTypeName = cp.ElementType.ToGlobalDisplayString();
        var fieldName = cp.Parameter.Name.ToParameterFieldName();

        var targetFieldExpression = BuildAddExpression(fieldName);
        var ctorArgs = BuildCopyConstructorArguments(step, targetFieldName: fieldName, targetFieldExpression);

        return MethodDeclaration(ParseTypeName(stepGlobalName), Identifier(method.Name))
            .WithAttributeLists(SingletonList(
                AttributeList(SingletonSeparatedList(AggressiveInliningAttributeSyntax.Create()))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(SingletonSeparatedList(
                Parameter(Identifier("item"))
                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                    .WithType(ParseTypeName(elementTypeName)))))
            .WithBody(Block(
                ReturnStatement(
                    ObjectCreationExpression(ParseTypeName(stepGlobalName))
                        .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(
                            ctorArgs.InterleaveWith(Token(SyntaxKind.CommaToken))))))));
    }

    /// <summary>
    /// Emits a <c>WithXs(IEnumerable&lt;ElementType&gt; items)</c> bulk-append method on the accumulator step.
    /// The method body calls <c>this._{fieldName}__parameter.AddRange(items)</c> via the private
    /// copy constructor, forwarding all other accumulator fields unchanged.
    /// </summary>
    private static MethodDeclarationSyntax CreateBulkMethod(
        AccumulatorFluentStep step,
        AccumulatorBulkMethod method,
        string stepGlobalName)
    {
        var cp = method.CollectionParameter;
        var elementTypeName = cp.ElementType.ToGlobalDisplayString();
        var fieldName = cp.Parameter.Name.ToParameterFieldName();

        var bulkParamType = $"{IEnumerableGlobal}<{elementTypeName}>";
        var targetFieldExpression = BuildAddRangeExpression(fieldName);
        var ctorArgs = BuildCopyConstructorArguments(step, targetFieldName: fieldName, targetFieldExpression);

        return MethodDeclaration(ParseTypeName(stepGlobalName), Identifier(method.Name))
            .WithAttributeLists(SingletonList(
                AttributeList(SingletonSeparatedList(AggressiveInliningAttributeSyntax.Create()))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(SingletonSeparatedList(
                Parameter(Identifier("items"))
                    .WithType(ParseTypeName(bulkParamType)))))
            .WithBody(Block(
                ReturnStatement(
                    ObjectCreationExpression(ParseTypeName(stepGlobalName))
                        .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(
                            ctorArgs.InterleaveWith(Token(SyntaxKind.CommaToken))))))));
    }

    /// <summary>
    /// Builds <c>this._{fieldName}__parameter.Add(item)</c> for use in the single-item AddX copy-ctor argument.
    /// </summary>
    private static ExpressionSyntax BuildAddExpression(string fieldName)
    {
        var thisField = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ThisExpression(),
            IdentifierName(fieldName));

        return InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                thisField,
                IdentifierName("Add")))
            .WithArgumentList(ArgumentList(
                SingletonSeparatedList(Argument(IdentifierName("item")))));
    }

    /// <summary>
    /// Builds <c>this._{fieldName}__parameter.AddRange(items)</c> for use in the bulk WithXs copy-ctor argument.
    /// </summary>
    private static ExpressionSyntax BuildAddRangeExpression(string fieldName)
    {
        var thisField = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ThisExpression(),
            IdentifierName(fieldName));

        return InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                thisField,
                IdentifierName("AddRange")))
            .WithArgumentList(ArgumentList(
                SingletonSeparatedList(Argument(IdentifierName("items")))));
    }

    // ── Property-backed AddX methods ─────────────────────────────────────────

    /// <summary>
    /// Emits one <c>AddX(in ElementType item)</c> method per property-backed collection accumulator.
    /// Parallel to <see cref="CreateAddMethods"/> but drives field names from
    /// <see cref="AccumulatorFluentStep.CollectionProperties"/> using <c>__property</c> suffix.
    /// </summary>
    private static ImmutableArray<MethodDeclarationSyntax> CreatePropertyAddMethods(AccumulatorFluentStep step)
    {
        var stepGlobalName = step.IdentifierDisplayString();
        return step.CollectionProperties
            .Select(cp => CreatePropertyAddMethod(step, cp, stepGlobalName))
            .ToImmutableArray();
    }

    private static MethodDeclarationSyntax CreatePropertyAddMethod(
        AccumulatorFluentStep step,
        CollectionPropertyInfo cp,
        string stepGlobalName)
    {
        var elementTypeName = cp.ElementType.ToGlobalDisplayString();
        var fieldName = cp.Property.Name.ToPropertyFieldName();

        var targetFieldExpression = BuildAddExpressionForField(fieldName);
        var ctorArgs = BuildCopyConstructorArgumentsWithProperties(step, targetParamFieldName: null, targetPropertyFieldName: fieldName, targetFieldExpression);

        return MethodDeclaration(ParseTypeName(stepGlobalName), Identifier(cp.MethodName))
            .WithAttributeLists(SingletonList(
                AttributeList(SingletonSeparatedList(AggressiveInliningAttributeSyntax.Create()))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(SingletonSeparatedList(
                Parameter(Identifier("item"))
                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                    .WithType(ParseTypeName(elementTypeName)))))
            .WithBody(Block(
                ReturnStatement(
                    ObjectCreationExpression(ParseTypeName(stepGlobalName))
                        .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(
                            ctorArgs.InterleaveWith(Token(SyntaxKind.CommaToken))))))));
    }

    /// <summary>
    /// Builds <c>this._{fieldName}.Add(item)</c> for use in a copy-constructor argument.
    /// </summary>
    private static ExpressionSyntax BuildAddExpressionForField(string fieldName)
    {
        var thisField = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ThisExpression(),
            IdentifierName(fieldName));

        return InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                thisField,
                IdentifierName("Add")))
            .WithArgumentList(ArgumentList(
                SingletonSeparatedList(Argument(IdentifierName("item")))));
    }

    // ── Terminal method ───────────────────────────────────────────────────────

    /// <summary>
    /// Emits the terminal method on the accumulator step.
    /// The single <see cref="TerminalMethod"/> in <c>step.FluentMethods</c> is located and its
    /// body calls <c>new TargetType(...)</c> in constructor-parameter order, using
    /// <see cref="AccumulatorCollectionConversionExpression.ConvertToDeclaredType"/> for each
    /// collection-parameter argument and <c>this._field__parameter</c> field access for each
    /// forwarded non-collection argument.
    /// </summary>
    private static MethodDeclarationSyntax CreateTerminalMethod(AccumulatorFluentStep step)
    {
        var terminal = step.FluentMethods.OfType<TerminalMethod>().Single();
        return BuildTerminalMethodDeclaration(step, terminal);
    }

    private static InvocationExpressionSyntax BuildStaticMethodInvocation(
        TerminalMethod terminal,
        ArgumentListSyntax argList)
    {
        var containingType = terminal.Return.CandidateTargets[0].ContainingType.ToGlobalDisplayString();
        var methodName = terminal.Return.CandidateTargets[0].Name;

        return InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ParseExpression(containingType),
                IdentifierName(methodName)))
            .WithArgumentList(argList);
    }

    private static MethodDeclarationSyntax BuildTerminalMethodDeclaration(
        AccumulatorFluentStep step,
        TerminalMethod terminal)
    {
        // Build a fast lookup: original constructor parameter name → CollectionParameterInfo
        var collectionLookup = step.CollectionParameters
            .ToDictionary(
                cp => cp.Parameter.Name,
                cp => cp,
                StringComparer.Ordinal);

        var targetTypeReturn = (TargetTypeReturn)terminal.Return;
        var returnTypeName = targetTypeReturn.ReturnTypeDisplayString();

        // Emit arguments in the order they appear in AvailableParameterFields,
        // which matches the target constructor's parameter order
        var args = terminal.AvailableParameterFields
            .Select(field => BuildTerminalArgument(field, collectionLookup))
            .ToArray();

        var argList = ArgumentList(SeparatedList<ArgumentSyntax>(
            args.InterleaveWith(Token(SyntaxKind.CommaToken))));

        ExpressionSyntax returnExpression = terminal.IsStaticMethodTarget
            ? BuildStaticMethodInvocation(terminal, argList)
            : EmitTerminalForParameterBackedAccumulator(step, returnTypeName, argList);

        return MethodDeclaration(ParseTypeName(returnTypeName), Identifier(terminal.Name))
            .WithAttributeLists(SingletonList(
                AttributeList(SingletonSeparatedList(AggressiveInliningAttributeSyntax.Create()))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithBody(Block(ReturnStatement(returnExpression)));
    }

    /// <summary>
    /// Emits the terminal object creation for parameter-backed (constructor-arg) accumulation,
    /// optionally followed by a property object-initializer block for property-backed accumulators.
    /// When no property-backed accumulators exist, emits plain <c>new Target(...)</c>.
    /// When property-backed accumulators exist, emits <c>new Target(...) { Prop = fieldExpr, ... }</c>.
    /// </summary>
    private static ExpressionSyntax EmitTerminalForParameterBackedAccumulator(
        AccumulatorFluentStep step,
        string returnTypeName,
        ArgumentListSyntax argList)
    {
        var objectCreation = ObjectCreationExpression(ParseTypeName(returnTypeName))
            .WithArgumentList(argList);

        if (step.CollectionProperties.IsEmpty)
            return objectCreation;

        return EmitTerminalWithPropertyInitializers(objectCreation, step);
    }

    /// <summary>
    /// Adds object-initializer assignments for property-backed collection fields to the given
    /// object creation expression. Each initializer converts the <c>ImmutableArray&lt;T&gt;</c>
    /// backing field to the property's declared collection type.
    /// </summary>
    private static ObjectCreationExpressionSyntax EmitTerminalWithPropertyInitializers(
        ObjectCreationExpressionSyntax objectCreation,
        AccumulatorFluentStep step)
    {
        var initializerExpressions = step.CollectionProperties
            .Select(cp =>
            {
                var fieldName = cp.Property.Name.ToPropertyFieldName();
                var fieldAccess = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(fieldName));

                var converted = AccumulatorCollectionConversionExpression.ConvertToDeclaredType(
                    fieldAccess,
                    cp.DeclaredCollectionType,
                    cp.ElementType);

                return (ExpressionSyntax)AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(cp.Property.Name),
                    converted);
            })
            .ToArray();

        return objectCreation.WithInitializer(
            InitializerExpression(
                SyntaxKind.ObjectInitializerExpression,
                SeparatedList(initializerExpressions)));
    }

    private static ArgumentSyntax BuildTerminalArgument(
        FluentMethodParameter field,
        Dictionary<string, CollectionParameterInfo> collectionLookup)
    {
        var fieldName = field.SourceName.ToParameterFieldName();
        var fieldAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ThisExpression(),
            IdentifierName(fieldName));

        if (!collectionLookup.TryGetValue(field.SourceName, out var cp))
            return Argument(fieldAccess);

        var converted = AccumulatorCollectionConversionExpression.ConvertToDeclaredType(
            fieldAccess,
            cp.DeclaredCollectionType,
            cp.ElementType);

        return Argument(converted);
    }

    // ── Constructor parameter / assignment helpers ────────────────────────────

    private static IEnumerable<ParameterSyntax> BuildForwardedConstructorParameters(
        AccumulatorFluentStep step) =>
        step.ForwardedTargetParameters
            .Select(p =>
                Parameter(Identifier(p.Name.ToCamelCase()))
                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                    .WithType(ParseTypeName(p.Type.ToGlobalDisplayString())));

    private static IEnumerable<ParameterSyntax> BuildAccumulatorConstructorParameters(
        AccumulatorFluentStep step) =>
        step.CollectionParameters
            .Select(cp =>
                Parameter(Identifier(cp.Parameter.Name.ToCamelCase()))
                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                    .WithType(ParseTypeName($"{ImmutableArrayGlobal}<{cp.ElementType.ToGlobalDisplayString()}>")));

    private static IEnumerable<ParameterSyntax> BuildPropertyAccumulatorConstructorParameters(
        AccumulatorFluentStep step) =>
        step.CollectionProperties
            .Select(cp =>
                Parameter(Identifier(cp.Property.Name.ToCamelCase()))
                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                    .WithType(ParseTypeName($"{ImmutableArrayGlobal}<{cp.ElementType.ToGlobalDisplayString()}>")));

    private static IEnumerable<StatementSyntax> BuildForwardedFieldAssignments(
        AccumulatorFluentStep step) =>
        step.ForwardedTargetParameters
            .Select(p =>
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(p.Name.ToParameterFieldName())),
                        IdentifierName(p.Name.ToCamelCase()))));

    private static IEnumerable<StatementSyntax> BuildEmptyInitializations(
        AccumulatorFluentStep step) =>
        step.CollectionParameters
            .Select(cp =>
            {
                var emptyExpr = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ParseExpression($"{ImmutableArrayGlobal}<{cp.ElementType.ToGlobalDisplayString()}>"),
                    IdentifierName("Empty"));

                return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(cp.Parameter.Name.ToParameterFieldName())),
                        emptyExpr));
            });

    private static IEnumerable<StatementSyntax> BuildPropertyEmptyInitializations(
        AccumulatorFluentStep step) =>
        step.CollectionProperties
            .Select(cp =>
            {
                var emptyExpr = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ParseExpression($"{ImmutableArrayGlobal}<{cp.ElementType.ToGlobalDisplayString()}>"),
                    IdentifierName("Empty"));

                return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(cp.Property.Name.ToPropertyFieldName())),
                        emptyExpr));
            });

    private static IEnumerable<StatementSyntax> BuildAccumulatorFieldAssignments(
        AccumulatorFluentStep step) =>
        step.CollectionParameters
            .Select(cp =>
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(cp.Parameter.Name.ToParameterFieldName())),
                        IdentifierName(cp.Parameter.Name.ToCamelCase()))));

    private static IEnumerable<StatementSyntax> BuildPropertyAccumulatorFieldAssignments(
        AccumulatorFluentStep step) =>
        step.CollectionProperties
            .Select(cp =>
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(cp.Property.Name.ToPropertyFieldName())),
                        IdentifierName(cp.Property.Name.ToCamelCase()))));

    /// <summary>
    /// Builds the argument list for the private copy constructor call inside a parameter-backed
    /// <c>AddX</c> or <c>WithXs</c> method. Passes all forwarded fields, all parameter accumulator
    /// fields (updating the target), and all property accumulator fields unchanged.
    /// </summary>
    /// <param name="step">The accumulator step whose fields drive the argument order.</param>
    /// <param name="targetFieldName">The field name of the collection being mutated.</param>
    /// <param name="targetFieldExpression">
    /// The expression to substitute for the target field (e.g., <c>Add(item)</c> invocation).
    /// </param>
    private static IEnumerable<ArgumentSyntax> BuildCopyConstructorArguments(
        AccumulatorFluentStep step,
        string targetFieldName,
        ExpressionSyntax targetFieldExpression)
    {
        // Forwarded non-collection fields — pass through unchanged
        foreach (var p in step.ForwardedTargetParameters)
        {
            yield return Argument(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(p.Name.ToParameterFieldName())));
        }

        // Parameter accumulator fields — update target via caller-provided expression, pass others through
        foreach (var cp in step.CollectionParameters)
        {
            var fieldName = cp.Parameter.Name.ToParameterFieldName();
            var thisField = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                IdentifierName(fieldName));

            yield return Argument(fieldName == targetFieldName ? targetFieldExpression : thisField);
        }

        // Property accumulator fields — always pass through unchanged in parameter-backed AddX
        foreach (var cp in step.CollectionProperties)
        {
            yield return Argument(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(cp.Property.Name.ToPropertyFieldName())));
        }
    }

    /// <summary>
    /// Builds the argument list for the private copy constructor call inside a property-backed
    /// <c>AddX</c> method. Passes all forwarded fields, all parameter accumulator fields unchanged,
    /// and all property accumulator fields (updating the target).
    /// </summary>
    /// <param name="step">The accumulator step whose fields drive the argument order.</param>
    /// <param name="targetParamFieldName">Must be <see langword="null"/> — no parameter field is being mutated.</param>
    /// <param name="targetPropertyFieldName">The property field name being mutated.</param>
    /// <param name="targetFieldExpression">The expression to substitute for the target property field.</param>
    private static IEnumerable<ArgumentSyntax> BuildCopyConstructorArgumentsWithProperties(
        AccumulatorFluentStep step,
        string? targetParamFieldName,
        string targetPropertyFieldName,
        ExpressionSyntax targetFieldExpression)
    {
        // Forwarded non-collection fields — pass through unchanged
        foreach (var p in step.ForwardedTargetParameters)
        {
            yield return Argument(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(p.Name.ToParameterFieldName())));
        }

        // Parameter accumulator fields — pass through unchanged in property-backed AddX
        foreach (var cp in step.CollectionParameters)
        {
            yield return Argument(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(cp.Parameter.Name.ToParameterFieldName())));
        }

        // Property accumulator fields — update target, pass others through
        foreach (var cp in step.CollectionProperties)
        {
            var fieldName = cp.Property.Name.ToPropertyFieldName();
            var thisField = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                IdentifierName(fieldName));

            yield return Argument(fieldName == targetPropertyFieldName ? targetFieldExpression : thisField);
        }
    }
}
