using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class BehaviorResourceOwnershipReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "behavior.resource.ownership";
    private const string Question = "Is the resource owner responsible for valid cleanup and propagation across lifecycle paths?";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review changed acquisition, subscription, cancellation and cleanup paths.", true);
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
            foreach (SyntaxNode node in root.DescendantNodes().Where(IsResourceSignal).OrderBy(candidate => candidate.SpanStart))
            {
                ISymbol? owner = BehaviorReviewSupport.DeclaredOwner(context, document, node);
                if (node is ObjectCreationExpressionSyntax creation)
                {
                    ITypeSymbol? type = model.GetTypeInfo(creation).Type;
                    if (type is not null && !IsDisposable(type) && !creation.Type.ToString().Contains("CancellationTokenSource", StringComparison.Ordinal)) { continue; }
                }
                subjects.Add(BehaviorReviewSupport.Create(context, document, node, owner, "resource lifecycle", "Borrowed resources must not necessarily be disposed by their consumer.", ordinals));
            }
            if (subjects.Any(subject => subject.Item.Path == path)) { sources.TryAdd(path, context.Text(document).ToString()); }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [], ReviewerClass = "implementer" };
    }

    private static bool IsResourceSignal(SyntaxNode node) => node switch
    {
        UsingStatementSyntax => true,
        LocalDeclarationStatementSyntax declaration when declaration.UsingKeyword != default => true,
        InvocationExpressionSyntax invocation when invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText is "Dispose" or "DisposeAsync" or "Cancel" or "Register" => true,
        AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.AddAssignmentExpression) || assignment.IsKind(SyntaxKind.SubtractAssignmentExpression) => true,
        ObjectCreationExpressionSyntax creation when creation.Type.ToString().Contains("Stream", StringComparison.Ordinal) || creation.Type.ToString().Contains("CancellationTokenSource", StringComparison.Ordinal) => true,
        _ => false
    };

    private static bool IsDisposable(ITypeSymbol type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.AllInterfaces.Any(iface => iface.ToDisplayString() is "System.IDisposable" or "System.IAsyncDisposable")) { return true; }
        }
        return false;
    }
}
