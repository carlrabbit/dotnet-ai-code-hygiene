namespace DotNetAiCodeHygiene.Core;

internal sealed class DocumentationSummaryGermanReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "docs.summary.language.german.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 5, "review-batch", "review-batch", "Review explicit API summary carriers for natural, understandable German.", true);
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 35;
    public IReadOnlyList<ReviewQuestion> Questions { get; } = [new("Q1", "Is this summary written in natural, understandable German rather than superficial translation, while preserving the documented contract?")];
    public string EscalationCondition => "Escalate to frontier when language quality or faithful meaning cannot be established with confidence.";
    public string EscalatedReviewerClass => "frontier";
    public RuleModuleResult Evaluate(RuleContext context)
    {
        RuleModuleResult raw = SummaryReviewPopulation.CreatePopulation(context);
        ReviewSubject[] subjects = raw.ReviewSubjects.Select(subject => subject with
        {
            ObservedSignal = "Compiler-resolved API subject has explicit, non-empty summary-carrier prose and the German review policy is enabled.",
            LegitimateCase = "The hygiene repository may disable this independently configured rule; there is no universal requirement for German summaries.",
            PrimaryAnchor = new ReviewAnchor(subject.Identity.Split('\0')[0], subject.Item.Path, subject.Item.Line, subject.Item.Column, subject.Item.Symbol, "german-summary-carrier")
        }).ToArray();
        RuleModuleResult population = raw with { ReviewSubjects = subjects };
        return population with { SelectedReviewSubjects = [] };
    }
}
