using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class BehaviorValidationBoundaryReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "behavior.validation.boundary";
    private const string Question = "Is the actual invariant enforced where untrusted data enters without redundant interior checks or unjustified assumptions?";
    private static readonly Rule RuleDescriptor = new(RuleId, 2, "review-batch", "review-batch", "Review changed input and validation boundaries with explicit binding evidence.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", Question)];
    private const string EscalationText = "Escalate a missing or conflicting business, security, or data-boundary contract to the planner. Runtime binder or serializer behavior that cannot be established from loaded source remains uncertain.";
    private static readonly HashSet<string> BoundaryAttributes = new(StringComparer.Ordinal)
    {
        "Microsoft.AspNetCore.Mvc.FromBodyAttribute", "Microsoft.AspNetCore.Mvc.FromQueryAttribute", "Microsoft.AspNetCore.Mvc.FromRouteAttribute",
        "Microsoft.AspNetCore.Mvc.FromFormAttribute", "Microsoft.AspNetCore.Mvc.FromHeaderAttribute", "System.ComponentModel.DataAnnotations.RequiredAttribute",
        "System.ComponentModel.DataAnnotations.RangeAttribute", "System.ComponentModel.DataAnnotations.StringLengthAttribute",
        "System.ComponentModel.DataAnnotations.MinLengthAttribute", "System.ComponentModel.DataAnnotations.MaxLengthAttribute",
        "System.ComponentModel.DataAnnotations.RegularExpressionAttribute", "System.ComponentModel.DataAnnotations.CustomValidationAttribute"
    };
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 28;
    public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => EscalationText;
    public string EscalatedReviewerClass => "planner";

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            SyntaxNode root = context.Root(document);
            SemanticModel model = context.SemanticModel(document);
            string path = context.RelativePath(document);
            var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (SyntaxNode node in root.DescendantNodes().Where(candidate => IsInputSignal(candidate, model)).OrderBy(candidate => candidate.SpanStart))
            {
                ISymbol? owner = BehaviorReviewSupport.DeclaredOwner(context, document, node);
                ReviewSubject subject = BehaviorReviewSupport.Create(context, document, node, owner, "input/validation boundary", "Compiler nullability alone is not runtime JSON validation; actual configured binders matter.", ordinals);
                ISymbol?[] relatedSymbols = node switch
                {
                    ParameterSyntax parameter => [owner, model.GetTypeInfo(parameter.Type!).Type],
                    MethodDeclarationSyntax method => [owner, .. method.ParameterList.Parameters.Select(parameter => model.GetTypeInfo(parameter.Type!).Type)],
                    InvocationExpressionSyntax invocation => new ISymbol?[] { model.GetSymbolInfo(invocation).Symbol, owner }
                        .Concat((model.GetSymbolInfo(invocation).Symbol as IMethodSymbol)?.TypeArguments.Cast<ISymbol?>() ?? []).ToArray(),
                    _ => [owner]
                };
                string exactSignal = node switch
                {
                    ParameterSyntax parameter => "Compiler-resolved boundary attribute(s): " + string.Join(", ", model.GetDeclaredSymbol(parameter)?.GetAttributes().Select(attribute => attribute.AttributeClass?.ToDisplayString()).Where(name => name is not null) ?? []),
                    MethodDeclarationSyntax method => "Compiler-resolved endpoint attribute(s): " + string.Join(", ", model.GetDeclaredSymbol(method)?.GetAttributes().Select(attribute => attribute.AttributeClass?.ToDisplayString()).Where(name => name is not null) ?? []),
                    InvocationExpressionSyntax invocation => "Compiler-resolved input/binder/serializer method: " + model.GetSymbolInfo(invocation).Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    _ => "Compiler-resolved input boundary signal"
                };
                subjects.Add(subject with { ObservedSignal = exactSignal + "; effective runtime binder/serializer configuration is not inferred from this source signal.",
                    Enrich = (_, selected) => selected with
                    {
                        RelatedAnchors = ReviewRelationshipFacts.RelatedSymbols(context, relatedSymbols, "boundary-owner-dto-or-serializer-source-declaration"),
                        EvidenceLimitation = "Effective runtime binder, serializer options, and validation registration were not established from the source boundary signal."
                    } });
            }
            if (subjects.Any(subject => subject.Item.Path == path)) { sources.TryAdd(path, context.Text(document).ToString()); }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [], ReviewerClass = "implementer" };
    }

    private static bool IsInputSignal(SyntaxNode node, SemanticModel model)
    {
        if (node is ParameterSyntax parameter && parameter.AttributeLists.Count > 0)
        {
            ISymbol? symbol = model.GetDeclaredSymbol(parameter);
            if (symbol?.GetAttributes().Any(attribute => IsBoundaryAttribute(attribute.AttributeClass)) == true) { return true; }
        }
        if (node is MethodDeclarationSyntax method && method.AttributeLists.Count > 0)
        {
            ISymbol? symbol = model.GetDeclaredSymbol(method);
            if (symbol?.GetAttributes().Any(attribute => IsHttpBoundaryAttribute(attribute.AttributeClass)) == true) { return true; }
        }
        if (node is InvocationExpressionSyntax invocation && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol called)
        {
            string containingType = called.ContainingType.ToDisplayString();
            return called.Name switch
            {
                "Deserialize" => IsFrameworkType(called.ContainingType, "System.Text.Json.JsonSerializer", "System.Text.Json") ||
                    IsFrameworkType(called.ContainingType, "Newtonsoft.Json.JsonConvert", "Newtonsoft.Json") ||
                    IsFrameworkType(called.ContainingType, "System.Xml.Serialization.XmlSerializer", "System.Xml.XmlSerializer"),
                "Validate" => IsFrameworkType(called.ContainingType, "System.ComponentModel.DataAnnotations.Validator", "System.ComponentModel.Annotations"),
                "TryValidateModel" or "TryUpdateModelAsync" => IsFrameworkType(called.ContainingType, "Microsoft.AspNetCore.Mvc.ControllerBase", "Microsoft.AspNetCore.Mvc.Core"),
                "Bind" => containingType.StartsWith("Microsoft.AspNetCore.Mvc.ModelBinding.", StringComparison.Ordinal) &&
                    called.ContainingAssembly.Identity.Name == "Microsoft.AspNetCore.Mvc.Core",
                _ => false
            };
        }
        return false;
    }

    private static bool IsBoundaryAttribute(INamedTypeSymbol? type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (!BoundaryAttributes.Contains(current.ToDisplayString())) { continue; }
            string expectedAssembly = current.ToDisplayString().StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal)
                ? "Microsoft.AspNetCore.Mvc.Core" : "System.ComponentModel.Annotations";
            return current.ContainingAssembly.Identity.Name == expectedAssembly;
        }
        return false;
    }

    private static bool IsFrameworkType(INamedTypeSymbol type, string fullName, string assembly) =>
        type.ToDisplayString() == fullName && type.ContainingAssembly.Identity.Name == assembly;

    private static bool IsHttpBoundaryAttribute(INamedTypeSymbol? type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() is "Microsoft.AspNetCore.Mvc.HttpGetAttribute" or "Microsoft.AspNetCore.Mvc.HttpPostAttribute" or
                "Microsoft.AspNetCore.Mvc.HttpPutAttribute" or "Microsoft.AspNetCore.Mvc.HttpPatchAttribute" or "Microsoft.AspNetCore.Mvc.HttpDeleteAttribute")
            {
                return current.ContainingAssembly.Identity.Name == "Microsoft.AspNetCore.Mvc.Core";
            }
        }
        return false;
    }
}
