using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class BehaviorResourceOwnershipReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "behavior.resource.ownership";
    private const string Question = "Is the resource owner responsible for valid cleanup and propagation across lifecycle paths?";
    private static readonly Rule RuleDescriptor = new(RuleId, 2, "review-batch", "review-batch", "Review changed acquisition, subscription, cancellation and cleanup paths.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", Question)];
    private const string EscalationText = "Escalate only when a missing or conflicting ownership/business boundary contract requires a policy decision. If external lifetime or borrowed ownership cannot be established, report uncertainty.";
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 30;
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
            foreach (SyntaxNode node in root.DescendantNodes().Where(candidate => IsResourceSignal(candidate, model)).OrderBy(candidate => candidate.SpanStart))
            {
                ISymbol? owner = BehaviorReviewSupport.DeclaredOwner(context, document, node);
                if (node is ObjectCreationExpressionSyntax creation)
                {
                    ITypeSymbol? type = model.GetTypeInfo(creation).Type;
                    if (type is not null && !IsDisposable(type) && !creation.Type.ToString().Contains("CancellationTokenSource", StringComparison.Ordinal)) { continue; }
                }
                ReviewSubject subject = BehaviorReviewSupport.Create(context, document, node, owner, "resource lifecycle", "Borrowed resources must not necessarily be disposed by their consumer.", ordinals);
                SyntaxNode? ownerNode = BehaviorReviewSupport.OwnerNode(node);
                SyntaxNode[] lifecycleNodes = ownerNode?.DescendantNodesAndSelf().Where(candidate => IsResourceSignal(candidate, model)).ToArray() ?? [node];
                ReviewAnchor[] lifecycle = lifecycleNodes.Where(candidate => !ReferenceEquals(candidate, node)).Select(candidate =>
                    ReviewRelationshipFacts.Anchor(context, context.Tree(document), candidate.Span,
                        candidate is InvocationExpressionSyntax call ? model.GetSymbolInfo(call).Symbol : candidate is ObjectCreationExpressionSyntax creation ? model.GetTypeInfo(creation).Type : null,
                        "related-acquisition-use-or-release-in-owner")).Distinct().ToArray();
                string observed = node switch
                {
                    AssignmentExpressionSyntax assignment => "Compiler-resolved event " + model.GetSymbolInfo(assignment.Left).Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) +
                        (assignment.IsKind(SyntaxKind.AddAssignmentExpression) ? " subscription (+=)" : " unsubscription (-=)"),
                    ObjectCreationExpressionSyntax allocation => "Compiler-resolved resource allocation type: " + model.GetTypeInfo(allocation).Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    InvocationExpressionSyntax invocation => "Compiler-resolved lifecycle invocation: " + model.GetSymbolInfo(invocation).Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    _ => "Explicit using/await using lifecycle construct"
                };
                subjects.Add(subject with { RelatedAnchors = lifecycle, ObservedSignal = observed + "; related operations in the same owner: " + lifecycle.Length +
                    "; other lifecycle paths, exception propagation and external ownership are not inferred.",
                    EvidenceLimitation = "Only same-owner source operations are included; exceptional, cancellation, and external ownership paths may remain unknown." });
            }
            if (subjects.Any(subject => subject.Item.Path == path)) { sources.TryAdd(path, context.Text(document).ToString()); }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [], ReviewerClass = "implementer" };
    }

    private static bool IsResourceSignal(SyntaxNode node, SemanticModel model) => node switch
    {
        UsingStatementSyntax => true,
        LocalDeclarationStatementSyntax declaration when declaration.UsingKeyword != default => true,
        InvocationExpressionSyntax invocation when IsLifecycleInvocation(invocation, model) => true,
        AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.AddAssignmentExpression) || assignment.IsKind(SyntaxKind.SubtractAssignmentExpression)
            => model.GetSymbolInfo(assignment.Left).Symbol is IEventSymbol,
        ObjectCreationExpressionSyntax creation when model.GetTypeInfo(creation).Type is ITypeSymbol type &&
            (IsDisposable(type) || type.ToDisplayString() == "System.Threading.CancellationTokenSource") => true,
        _ => false
    };

    private static bool IsLifecycleInvocation(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method) { return false; }
        if (method.Name is not ("Dispose" or "DisposeAsync" or "Cancel" or "Register")) { return false; }
        return IsDisposable(method.ContainingType) || IsDisposable(method.ReturnType) || method.ContainingType.ToDisplayString() == "System.Threading.CancellationTokenSource";
    }

    private static bool IsDisposable(ITypeSymbol type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.AllInterfaces.Any(iface => iface.ToDisplayString() is "System.IDisposable" or "System.IAsyncDisposable")) { return true; }
        }
        return false;
    }
}
