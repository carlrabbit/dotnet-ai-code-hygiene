using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class SummaryGermanReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "docs.summary.language.german.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 2, "review-batch", "review-batch", "Statistically sample explicit documentation summaries for natural, comprehensible German.", true);

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
        var sampler = context.Sampling.SubjectSampler(RuleId, 2);
        long cursor = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        const double yearlyRate = 1d / (365d * 24d * 60d * 60d);
        foreach (ReviewSubject subject in population.ReviewSubjects)
        {
            string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(subject.Content)));
            var state = sampler.State(subject.Identity);
            if (state.LastEvaluationFingerprint is null || !StringComparer.Ordinal.Equals(state.LastEvaluationFingerprint, fingerprint)) { sampler.AddHazard(subject.Identity, 1); }
            sampler.AccrueElapsed(subject.Identity, cursor, yearlyRate);
            sampler.SetEvaluationFingerprint(subject.Identity, fingerprint);
        }
        var selected = sampler.SelectDue(population.ReviewSubjects.Select(s => s.Identity), 5)
            .ToDictionary(x => x.Ticket.SubjectId, x => x.Ticket, StringComparer.Ordinal);
        return population with { SelectedReviewSubjects = population.ReviewSubjects.Where(s => selected.ContainsKey(s.Identity)).Select(s => s with { SubjectTicket = selected[s.Identity] }).ToArray() };
    }
}
