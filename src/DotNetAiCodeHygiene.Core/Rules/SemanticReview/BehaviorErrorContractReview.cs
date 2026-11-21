using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class BehaviorErrorContractReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "behavior.error.contract";
    private const string Question = "Can a failure be misrepresented as success, denial or a different business outcome?";
    private static readonly Rule RuleDescriptor = new(RuleId, 2, "review-batch", "review-batch", "Review changed failure-to-outcome paths using compiler-backed source signals.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", Question)];
    private const string EscalationText = "Escalate only when a missing or conflicting business, security, or data-boundary contract requires a policy decision. If runtime context is unavailable, report uncertainty rather than inventing an outcome.";
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 27;
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
            foreach (SyntaxNode node in root.DescendantNodes().Where(candidate => IsFailureOutcomeSignal(candidate, model)).OrderBy(node => node.SpanStart))
            {
                // A catch/filter, fallback arm, conversion or retry is a review trigger,
                // not evidence that its behavior is wrong.
                ISymbol? owner = BehaviorReviewSupport.DeclaredOwner(context, document, node);
                subjects.Add(BehaviorReviewSupport.Create(context, document, node, owner, "failure/outcome path", "Intentional domain fallbacks and handled expected errors may be correct.", ordinals));
            }
            if (subjects.Any(subject => subject.Item.Path == path)) { sources.TryAdd(path, context.Text(document).ToString()); }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [], ReviewerClass = "implementer" };
    }

    private static bool IsFailureOutcomeSignal(SyntaxNode node, SemanticModel model) => node switch
    {
        CatchClauseSyntax => true,
        ThrowExpressionSyntax => true,
        CatchFilterClauseSyntax => true,
        ConditionalExpressionSyntax conditional => conditional.WhenTrue is ThrowExpressionSyntax || conditional.WhenFalse is ThrowExpressionSyntax,
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.CoalesceExpression) && IsObservableFallback(binary, model) => true,
        InvocationExpressionSyntax invocation => IsOutcomeConversionOrRetry(invocation, model),
        _ => false
    };

    private static bool IsObservableFallback(BinaryExpressionSyntax binary, SemanticModel model) => binary.Right is ThrowExpressionSyntax ||
        binary.Right.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(invocation =>
            model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && LooksLikeFailureOutcome(method.ReturnType));

    private static bool IsOutcomeConversionOrRetry(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method) { return false; }
        bool named = method.Name is "Retry" or "RetryAsync" or "Fallback" or "FallbackAsync" or "ToResult" or "ToError" or "ToFailure" or "FromException" or "MapError" or "MapFailure" or "HandleException";
        if (!named) { return false; }
        bool exceptionContext = invocation.Ancestors().Any(node => node is CatchClauseSyntax or ThrowStatementSyntax or ThrowExpressionSyntax);
        bool failureInput = method.Parameters.Any(parameter => LooksLikeFailureOutcome(parameter.Type)) ||
            invocation.ArgumentList.Arguments.Any(argument => LooksLikeFailureOutcome(model.GetTypeInfo(argument.Expression).Type));
        return exceptionContext || failureInput || LooksLikeFailureOutcome(method.ReturnType) || LooksLikeFailureOutcome(method.ContainingType);
    }

    private static bool LooksLikeFailureOutcome(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.Name.Contains("Exception", StringComparison.OrdinalIgnoreCase) || current.Name.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                current.Name.Contains("Failure", StringComparison.OrdinalIgnoreCase) || current.Name.Contains("Result", StringComparison.OrdinalIgnoreCase) ||
                current.Name.Contains("Problem", StringComparison.OrdinalIgnoreCase)) { return true; }
        }
        return false;
    }
}
