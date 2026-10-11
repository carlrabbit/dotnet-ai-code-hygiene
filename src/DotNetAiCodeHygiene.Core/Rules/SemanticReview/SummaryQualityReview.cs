using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class SummaryQualityReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "docs.summary.quality.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 5, "review-batch", "review-batch", "Statistically sample explicit documentation summaries for correctness, value, and clarity.", true);

    public Rule Descriptor => RuleDescriptor;
    internal static readonly ReviewQuestion[] RuleQuestions =
    [
        new("Q1", "Technical correctness: Is it consistent with the declaration and relevant implementation/API context, without inventing behavior?"),
        new("Q2", "Information value: Does it add useful caller-relevant meaning rather than simply repeat/paraphrase the symbol name/type/signature? A concise summary is acceptable only when it still communicates useful purpose/domain meaning."),
        new("Q3", "Clarity and scope: Is it concise, specific, and clear enough to communicate responsibility without irrelevant implementation detail?")
    ];
    internal const string EscalationText = "Expand if any sampled summary materially fails a required question or the implementer cannot confidently answer it.";

    public int BatchNumber => 1;
    public string EscalatedReviewerClass => "frontier";
    public string EscalationCondition => SummaryQualityReviewRuleModule.EscalationText;
    public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public RuleModuleResult Evaluate(RuleContext context)
    {
        RuleModuleResult population = SummaryReviewPopulation.CreatePopulation(context);
        var sampler = context.Sampling.SubjectSampler(RuleId, 5);
        foreach (ReviewSubject subject in population.ReviewSubjects)
        {
            string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(subject.Content)));
            var state = sampler.State(subject.Identity);
            if (state.LastEvaluationFingerprint is null || !StringComparer.Ordinal.Equals(state.LastEvaluationFingerprint, fingerprint)) { sampler.AddHazard(subject.Identity, 1); }
            sampler.SetEvaluationFingerprint(subject.Identity, fingerprint);
        }
        var selected = sampler.SelectDue(population.ReviewSubjects.Select(s => s.Identity), 5)
            .ToDictionary(x => x.Ticket.SubjectId, x => x.Ticket, StringComparer.Ordinal);
        return population with { SelectedReviewSubjects = population.ReviewSubjects.Where(s => selected.ContainsKey(s.Identity)).Select(s => s with { SubjectTicket = selected[s.Identity] }).ToArray() };
    }
    internal static RuleModuleResult BuildPopulation(RuleContext context) => SummaryReviewPopulation.CreatePopulation(context);
}
