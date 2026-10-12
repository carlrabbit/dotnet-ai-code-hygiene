using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class SummaryGermanReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "docs.summary.language.german.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 4, "review-batch", "review-batch", "Statistically sample explicit documentation summaries for natural, comprehensible German.", true);

    public Rule Descriptor => RuleDescriptor;
    internal static readonly ReviewQuestion[] RuleQuestions = [ new("Q1", "German language: Is the summary natural, comprehensible German rather than awkward literal translation or merely German-looking text?") ];
    internal const string EscalationText = "Expand if any sampled summary materially fails a required question or the implementer cannot confidently answer it.";

    public int BatchNumber => 2;
    public string EscalatedReviewerClass => "frontier";
    public string EscalationCondition => SummaryGermanReviewRuleModule.EscalationText;
    public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public RuleModuleResult Evaluate(RuleContext context)
    {
        RuleModuleResult population = SummaryQualityReviewRuleModule.BuildPopulation(context);
        return population with { SelectedReviewSubjects = context.SelectGate(Descriptor, population.ReviewSubjects) };
    }
}
