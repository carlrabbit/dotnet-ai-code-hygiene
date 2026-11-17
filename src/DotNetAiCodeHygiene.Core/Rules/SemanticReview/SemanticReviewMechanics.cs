using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace DotNetAiCodeHygiene.Core;

internal interface ISemanticReviewRuleModule : IRuleModule
{
    public int BatchNumber { get; }
    public IReadOnlyList<ReviewQuestion> Questions { get; }
    public string EscalationCondition { get; }
    public string EscalatedReviewerClass { get; }
}

internal interface ISharedModernizationReviewRuleModule : ISemanticReviewRuleModule { }

internal sealed record ReviewTicketAssociation(string BatchId, string ItemId, GateTicket GateTicket);
internal sealed record ReviewBatchMaterialization(ReviewBatch Batch, IReadOnlyList<ReviewTicketAssociation> Tickets);
internal sealed record SemanticReviewMaterialization(ReviewBatch[] Batches, ReviewTicketAssociation[] Tickets);

internal sealed class SemanticReviewRuleRunner(IReadOnlyList<ISemanticReviewRuleModule> modules)
{
    internal SemanticReviewMaterialization BuildBatches(string run, IReadOnlyList<RuleModuleExecution> executions, bool createTickets = true)
    {
        var byId = executions.ToDictionary(execution => execution.Descriptor.Id, StringComparer.Ordinal);
        ReviewBatchMaterialization[] built = modules.Where(module => byId.ContainsKey(module.Descriptor.Id))
            .Select(module =>
            {
                RuleModuleResult result = byId[module.Descriptor.Id].Result;
                bool issueItemAcceptanceTickets = createTickets && module is not ISharedModernizationReviewRuleModule;
                ReviewBatchMaterialization materialized = HygieneEngine.BuildReviewBatch(run, module.Descriptor, module.BatchNumber,
                    result.ReviewSubjects, result.SelectedReviewSubjects ?? [],
                    result.ReviewSourceContents.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
                    module.Questions, module.EscalationCondition, module.EscalatedReviewerClass, result.ReviewerClass, issueItemAcceptanceTickets);
                ReviewBatch batch = materialized.Batch;
                if (module is ISharedModernizationReviewRuleModule)
                {
                    batch = batch with { Escalation = batch.Escalation with
                    {
                        Command = "Prefer modern syntax; retain only with a valid source-local suppression, otherwise leave the candidate outstanding."
                    } };
                }
                return materialized with { Batch = batch };
            }).ToArray();
        return new(built.Select(x => x.Batch).ToArray(), built.SelectMany(x => x.Tickets).ToArray());
    }
}

internal static class GeneralSemanticBudget
{
    internal const int MaximumObligations = 10;
    internal const int MaximumContextAffected = 2;
    private static readonly HashSet<string> PortfolioRuleIds = new(StringComparer.Ordinal)
    {
        "architecture.abstraction.necessity", "architecture.indirection.value", "architecture.responsibility.cohesion",
        "architecture.boundary.ownership", "architecture.authority.uniqueness", "architecture.lifecycle.binding", "architecture.mechanism.explicitness",
        "behavior.error.contract", "behavior.validation.boundary", "behavior.state.consistency", "behavior.resource.ownership",
        "tests.assertion.effectiveness", "tests.change-contract.coverage", "tests.isolation.coupling",
        "docs.summary.quality.review", "docs.summary.language.german.review", "comments.intent.review"
    };

    internal static IReadOnlyList<RuleModuleExecution> SelectExecutions(IReadOnlyList<RuleModuleExecution> executions, RuleContext context)
    {
        var general = executions.Where(execution => PortfolioRuleIds.Contains(execution.Descriptor.Id))
            .ToArray();
        if (general.Length == 0) { return executions; }
        var populations = general.Select(execution => (execution.Descriptor,
            (IReadOnlyList<ReviewSubject>)execution.Result.ReviewSubjects.Select(subject => subject with
            { RelatedEvidenceFingerprint = context.RelationshipFingerprint(subject.RelatedAnchors) }).ToArray())).ToArray();
        IReadOnlyDictionary<string, IReadOnlyList<ReviewSubject>> selected = context.SelectGeneral(populations);
        var populationById = populations.ToDictionary(population => population.Descriptor.Id, population => population.Item2, StringComparer.Ordinal);
        return executions.Select(execution =>
        {
            if (!selected.TryGetValue(execution.Descriptor.Id, out IReadOnlyList<ReviewSubject>? subjects)) { return execution; }
            RuleModuleResult result = execution.Result with
            {
                ReviewSubjects = populationById[execution.Descriptor.Id],
                SelectedReviewSubjects = subjects,
                ReviewSourceContents = SelectedSources(execution.Result, subjects, context)
            };
            return execution with { Result = result };
        }).ToArray();
    }

    private static IReadOnlyDictionary<string, string> SelectedSources(RuleModuleResult result, IReadOnlyList<ReviewSubject> selected, RuleContext context)
    {
        var sources = result.ReviewSourceContents.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        foreach (ReviewAnchor anchor in selected.SelectMany(subject => subject.RelatedAnchors ?? []).DistinctBy(value => value.Path))
        {
            if (!sources.ContainsKey(anchor.Path) && context.SourceContent(anchor.Path) is string content) { sources[anchor.Path] = content; }
        }
        return sources;
    }
}

internal static class ReviewEvidenceBuilder
{
    internal const int MaximumExcerpts = 8;
    internal const int MaximumUtf8Bytes = 24_000;

    internal static (ReviewItem Item, ReviewSource[] Sources) Build(ReviewItem item, ReviewAnchor? primary,
        IReadOnlyList<ReviewAnchor>? related, IReadOnlyDictionary<string, string> sourceContents,
        string? question, string? legitimateCase, string? signal)
    {
        primary ??= new ReviewAnchor("", item.Path, item.Line, item.Column, item.Symbol, "primary");
        var anchors = new[] { primary }.Concat(related ?? []).DistinctBy(a => (a.Project, a.Path, a.Line, a.Symbol, a.Relation)).ToArray();
        var excerpts = new List<ReviewEvidenceExcerpt>();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        var missing = new List<string>();
        int bytes = Encoding.UTF8.GetByteCount(question ?? "") + Encoding.UTF8.GetByteCount(legitimateCase ?? "") +
            Encoding.UTF8.GetByteCount(signal ?? "") + Encoding.UTF8.GetByteCount(item.Summary) +
            Encoding.UTF8.GetByteCount(item.Declaration) + Encoding.UTF8.GetByteCount(item.Symbol) + Encoding.UTF8.GetByteCount(item.Path);
        foreach (ReviewAnchor anchor in anchors)
        {
            string label = $"{anchor.Project}:{anchor.Path}:{anchor.Line}:{anchor.Symbol}";
            if (!sourceContents.TryGetValue(anchor.Path, out string? source))
            {
                missing.Add($"{label} (source unavailable)");
                continue;
            }
            if (excerpts.Count >= MaximumExcerpts)
            {
                missing.Add($"{label} (excerpt limit {MaximumExcerpts} reached)");
                continue;
            }
            string[] lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            int center = Math.Clamp(anchor.Line - 1, 0, Math.Max(0, lines.Length - 1));
            int first = Math.Max(0, center - 3), last = Math.Min(lines.Length - 1, center + 3);
            string text = string.Join("\n", lines[first..(last + 1)]);
            int metadataBytes = Encoding.UTF8.GetByteCount(anchor.Project + anchor.Path + anchor.Symbol + anchor.Relation);
            int remaining = MaximumUtf8Bytes - bytes - metadataBytes;
            if (remaining <= 0)
            {
                missing.Add($"{label} (24,000 UTF-8 byte limit reached)");
                continue;
            }
            string bounded = TruncateUtf8(text, remaining);
            if (bounded.Length < text.Length) { missing.Add($"{label} (excerpt truncated at 24,000 UTF-8 bytes)"); }
            int excerptBytes = Encoding.UTF8.GetByteCount(bounded) + metadataBytes;
            bytes += excerptBytes;
            string sourceHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(source)));
            var excerpt = new ReviewEvidenceExcerpt(anchor.Project, anchor.Path, first + 1, last + 1, anchor.Symbol, bounded, sourceHash);
            excerpts.Add(excerpt);
            sources[anchor.Path] = bounded;
        }
        string status = excerpts.Count == 0 ? "unavailable" : missing.Count == 0 ? "available" : "partial";
        return (item with
        {
            Question = question,
            LegitimateCase = legitimateCase,
            ObservedSignal = signal,
            ContextStatus = status,
            PrimaryAnchor = primary,
            RelatedAnchors = related ?? [],
            Evidence = excerpts,
            MissingEvidence = missing
        }, sources.Select(pair => new ReviewSource(pair.Key, pair.Value)).ToArray());
    }

    private static string TruncateUtf8(string value, int maximumBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes) { return value; }
        int low = 0, high = value.Length;
        while (low < high)
        {
            int middle = low + (high - low + 1) / 2;
            if (Encoding.UTF8.GetByteCount(value.AsSpan(0, middle)) <= maximumBytes) { low = middle; }
            else { high = middle - 1; }
        }
        return value[..low];
    }
}





internal static class SummaryReviewPopulation
{
    internal static RuleModuleResult CreatePopulation(RuleContext context)
    {
        var subjects = new List<ReviewSubject>();
        var sourceContents = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Microsoft.CodeAnalysis.Document document in context.ReportingDocuments)
        {
            string path = context.RelativePath(document);
            var tree = context.Tree(document);
            foreach (DocumentationSummarySubject subject in context.DocumentationSubjects(document).SummarySubjects)
            {
                if (string.IsNullOrWhiteSpace(subject.Summary))
                {
                    continue;
                }

                var position = tree.GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(subject.SourceOffset, 0)).StartLinePosition;
                string reviewSymbol;
                string declaration;
                string project = context.ProjectIdentity(document);
                if (subject.IsPositionalRecordProperty)
                {
                    ISymbol recordSymbol = context.SemanticModel(document).GetDeclaredSymbol(subject.Declaration)!;
                    reviewSymbol = subject.Anchor;
                    declaration = recordSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                }
                else
                {
                    reviewSymbol = subject.Symbol.ToDisplayString();
                    declaration = subject.Symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                }
                var item = new ReviewItem("", path, position.Line + 1, position.Character + 1, reviewSymbol, subject.Summary, declaration);
                int carrierFirstLine = tree.GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(subject.Declaration.GetLeadingTrivia().FullSpan.Start, 0)).StartLinePosition.Line + 1;
                int carrierLastLine = tree.GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(subject.Declaration.SpanStart, 0)).StartLinePosition.Line + 1;
                subjects.Add(new ReviewSubject(project + "\0" + path + "\0" + subject.Anchor,
                    subject.CarrierContent + "\0" + subject.Declaration.ToFullString(), item,
                    DirectChange: context.HasChangedLine(path, carrierFirstLine, carrierLastLine)));
                sourceContents.TryAdd(path, context.Text(document).ToString());
            }
        }
        return new RuleModuleResult([], subjects, sourceContents);
    }
}
