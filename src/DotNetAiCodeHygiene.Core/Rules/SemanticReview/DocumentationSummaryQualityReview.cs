namespace DotNetAiCodeHygiene.Core;

internal sealed class DocumentationSummaryQualityReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "docs.summary.quality.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 7, "review-batch", "review-batch", "Review explicit API summary carriers for technical accuracy, information value and clarity.", true);
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 34;
    public IReadOnlyList<ReviewQuestion> Questions { get; } = [new("Q1", "Is the summary technically faithful, informative and clear for this contract?")];
    public string EscalationCondition => "Escalate to frontier if the documented contract cannot be established or material uncertainty remains; do not score grammar, length, or style mechanically.";
    public string EscalatedReviewerClass => "frontier";
    public RuleModuleResult Evaluate(RuleContext context)
    {
        RuleModuleResult raw = SummaryReviewPopulation.CreatePopulation(context);
        ReviewSubject[] subjects = raw.ReviewSubjects.Select(subject => subject with
        {
            ObservedSignal = "Compiler-resolved API subject has explicit, non-empty summary-carrier prose.",
            LegitimateCase = "Concise or technical wording can be correct and useful; grammar, length, and style are not semantic verdicts.",
            PrimaryAnchor = new ReviewAnchor(subject.Identity.Split('\0')[0], subject.Item.Path, subject.Item.Line, subject.Item.Column, subject.Item.Symbol, "explicit-summary-carrier")
        }).ToArray();
        RuleModuleResult population = raw with { ReviewSubjects = subjects };
        return population with { SelectedReviewSubjects = [] };
    }
}
