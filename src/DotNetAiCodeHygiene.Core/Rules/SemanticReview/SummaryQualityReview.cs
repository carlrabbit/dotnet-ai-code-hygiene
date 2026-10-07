using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class SummaryQualityReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "docs.summary.quality.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 3, "review-batch", "review-batch", "Review a deterministic sample of explicit documentation summaries for correctness, value, and clarity.", true);

    public Rule Descriptor => RuleDescriptor;
    internal static readonly ReviewQuestion[] RuleQuestions =
    [
        new("Q1", "Technical correctness: Is it consistent with the declaration and relevant implementation/API context, without inventing behavior?"),
        new("Q2", "Information value: Does it add useful caller-relevant meaning rather than simply repeat/paraphrase the symbol name/type/signature? A concise summary is acceptable only when it still communicates useful purpose/domain meaning."),
        new("Q3", "Clarity and scope: Is it concise, specific, and clear enough to communicate responsibility without irrelevant implementation detail?")
    ];
    internal const string EscalationText = "Expand if any sampled summary materially fails a required question or the implementer cannot confidently answer it.";

    public int BatchNumber => 1;
    public string EscalationCondition => SummaryQualityReviewRuleModule.EscalationText;
    public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public RuleModuleResult Evaluate(RuleContext context) => BuildPopulation(context);
    internal static RuleModuleResult BuildPopulation(RuleContext context) => SummaryReviewPopulation.CreatePopulation(context);
}
