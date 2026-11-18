using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class BehaviorStateConsistencyReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "behavior.state.consistency";
    private const string Question = "Could stale results or state updates apply to the wrong active context?";
    private static readonly Rule RuleDescriptor = new(RuleId, 2, "review-batch", "review-batch", "Review changed asynchronous and mutable state transitions for context-sensitive updates.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", Question)];
    private const string EscalationText = "Escalate a missing or conflicting business, security, or data-boundary state contract to the planner. Unsupported runtime scheduling or callback context remains uncertain.";
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 29;
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
            foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>().OrderBy(candidate => candidate.SpanStart))
            {
                bool async = method.Modifiers.Any(token => token.ValueText == "async");
                bool asyncState = async && method.DescendantNodes().OfType<AwaitExpressionSyntax>().Any()
                    && method.DescendantNodes().Any(IsStateMutation);
                bool contextualName = method.Identifier.ValueText.Contains("Cache", StringComparison.OrdinalIgnoreCase)
                    || method.Identifier.ValueText.Contains("Select", StringComparison.OrdinalIgnoreCase)
                    || method.Identifier.ValueText.Contains("Context", StringComparison.OrdinalIgnoreCase)
                    || method.Identifier.ValueText.Contains("Request", StringComparison.OrdinalIgnoreCase);
                bool eventCallback = method.ParameterList.Parameters.Any(parameter => parameter.Type?.ToString().Contains("EventArgs", StringComparison.Ordinal) == true)
                    && method.DescendantNodes().Any(IsStateMutation);
                if (!asyncState && !eventCallback && !(contextualName && method.DescendantNodes().Any(IsStateMutation))) { continue; }
                ISymbol? owner = model.GetDeclaredSymbol(method);
                ReviewSubject subject = BehaviorReviewSupport.Create(context, document, method, owner, "asynchronous/state transition", "Sequential code or isolated concurrent operations need no extra cancellation machinery.", ordinals);
                AssignmentExpressionSyntax[] writes = method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(IsStateMutation).ToArray();
                ISymbol[] stateTargets = writes.Select(write => model.GetSymbolInfo(write.Left).Symbol).Where(symbol => symbol is not null).Cast<ISymbol>()
                    .Distinct(SymbolEqualityComparer.Default).ToArray();
                ReviewAnchor[] transitions = stateTargets.SelectMany(target => ReviewRelationshipFacts.Declarations(context, target, "mutated-state-owner"))
                    .Concat(writes.Select(write => ReviewRelationshipFacts.Anchor(context, context.Tree(document), write.Span,
                        model.GetSymbolInfo(write.Left).Symbol, "state-transition-write"))).Distinct().ToArray();
                string stateSignal = "Asynchronous/context-sensitive method; compiler-resolved state target(s): " +
                    (stateTargets.Length == 0 ? "none resolved" : string.Join(", ", stateTargets.Select(target => target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Order(StringComparer.Ordinal))) +
                    "; mechanically observed trigger: " + (asyncState ? "async+await+state mutation" : eventCallback ? "EventArgs callback+state mutation" : "context-like method name+state mutation") +
                    "; await count=" + method.DescendantNodes().OfType<AwaitExpressionSyntax>().Count() + ". Scheduler behavior is not inferred.";
                subjects.Add(subject with { RelatedAnchors = transitions, ObservedSignal = stateSignal,
                    Content = subject.Content + "\0transition:" + string.Join("\0", writes.Select(write => write.WithoutTrivia().ToFullString())),
                    EvidenceLimitation = stateTargets.Length == 0 ? "No compiler-resolved mutable state target was found in the selected transition; runtime context/state remains unknown." : null });
            }
            if (subjects.Any(subject => subject.Item.Path == path)) { sources.TryAdd(path, context.Text(document).ToString()); }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [], ReviewerClass = "implementer" };
    }

    private static bool IsStateMutation(SyntaxNode node) => node is AssignmentExpressionSyntax assignment
        && assignment.Kind() is Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleAssignmentExpression or Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddAssignmentExpression or Microsoft.CodeAnalysis.CSharp.SyntaxKind.SubtractAssignmentExpression;
}
