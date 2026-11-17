using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class BehaviorValidationBoundaryReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "behavior.validation.boundary";
    private const string Question = "Is the actual invariant enforced where untrusted data enters without redundant interior checks or unjustified assumptions?";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review changed input and validation boundaries with explicit binding evidence.", true);
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
                subjects.Add(BehaviorReviewSupport.Create(context, document, node, owner, "input/validation boundary", "Compiler nullability alone is not runtime JSON validation; actual configured binders matter.", ordinals));
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
            if (symbol?.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() is "Microsoft.AspNetCore.Mvc.HttpGetAttribute" or "Microsoft.AspNetCore.Mvc.HttpPostAttribute" or "Microsoft.AspNetCore.Mvc.HttpPutAttribute" or "Microsoft.AspNetCore.Mvc.HttpPatchAttribute" or "Microsoft.AspNetCore.Mvc.HttpDeleteAttribute") == true) { return true; }
        }
        if (node is InvocationExpressionSyntax invocation)
        {
            string name = invocation.Expression switch { MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText, IdentifierNameSyntax identifier => identifier.Identifier.ValueText, _ => "" };
            return name is "Deserialize" or "TryValidateModel" or "Validate" or "Bind";
        }
        return false;
    }

    private static bool IsBoundaryAttribute(INamedTypeSymbol? type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (BoundaryAttributes.Contains(current.ToDisplayString())) { return true; }
        }
        return false;
    }
}
