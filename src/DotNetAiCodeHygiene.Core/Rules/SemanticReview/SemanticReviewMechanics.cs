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
    internal SemanticReviewMaterialization BuildBatches(string run, IReadOnlyList<RuleModuleExecution> executions, bool createTickets = true, bool includeSourceContents = false)
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
                    module.Questions, module.EscalationCondition, module.EscalatedReviewerClass, result.ReviewerClass, issueItemAcceptanceTickets, includeSourceContents);
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
        var modulesById = RuleCatalog.SemanticReviewModules.ToDictionary(module => module.Descriptor.Id, StringComparer.Ordinal);
        var populations = general.Select(execution => (execution.Descriptor,
            (IReadOnlyList<ReviewSubject>)execution.Result.ReviewSubjects.Select(subject => subject with
            { Content = subject.Content + "\0rule-version:" + execution.Descriptor.Version + "\0question:" +
                string.Join("\n", modulesById[execution.Descriptor.Id].Questions.Select(question => question.Id + "=" + question.Text)) }).ToArray())).ToArray();
        IReadOnlyDictionary<string, IReadOnlyList<ReviewSubject>> selected = context.SelectGeneral(populations);
        var enrichedSelected = selected.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<ReviewSubject>)pair.Value.Select(subject =>
        {
            ReviewSubject enriched = subject.Enrich?.Invoke(context, subject) ?? subject;
            return enriched with { Enrich = null, RelatedEvidenceFingerprint = context.RelationshipFingerprint(enriched.RelatedAnchors) };
        }).ToArray(), StringComparer.Ordinal);
        IReadOnlyDictionary<string, IReadOnlyList<ReviewSubject>> finalized = context.ExhaustiveAudit || context.Gate.ExpandRuleId is not null
            ? enrichedSelected
            : context.GateReviewStore.FinalizeSharedSelected(context.Gate.GateId, populations, enrichedSelected);
        return executions.Select(execution =>
        {
            if (!finalized.TryGetValue(execution.Descriptor.Id, out IReadOnlyList<ReviewSubject>? subjects)) { return execution; }
            RuleModuleResult result = execution.Result with
            {
                // Keep the discovery population stable for gate revalidation and population
                // fingerprints. SelectedReviewSubjects carries the freshly enriched copies.
                ReviewSubjects = populations.Single(population => population.Descriptor.Id == execution.Descriptor.Id).Item2,
                SelectedReviewSubjects = subjects,
                ReviewSourceContents = SelectedSources(execution.Result, enrichedSelected[execution.Descriptor.Id], context)
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
        string? question, string? legitimateCase, string? signal, string? evidenceLimitation = null)
    {
        primary ??= new ReviewAnchor("", item.Path, item.Line, item.Column, item.Symbol, "primary");
        ReviewAnchor[] relatedAnchors = (related ?? []).DistinctBy(a => (a.Project, a.Path, a.Line, a.Symbol, a.Relation)).ToArray();
        var anchors = new[] { primary }.Concat(relatedAnchors).DistinctBy(a => (a.Project, a.Path, a.Line, a.Symbol, a.Relation)).ToArray();
        var excerpts = new List<ReviewEvidenceExcerpt>();
        var missing = new List<string>();
        int missingCount = string.IsNullOrWhiteSpace(evidenceLimitation) ? 0 : 1;
        if (!string.IsNullOrWhiteSpace(evidenceLimitation)) { missing.Add(Bound(evidenceLimitation, 800)); }
        ReviewAnchor[] representedAnchors = relatedAnchors.Take(12).Select(BoundAnchor).ToArray();
        if (relatedAnchors.Length > representedAnchors.Length)
        {
            int omittedAnchors = relatedAnchors.Length - representedAnchors.Length;
            missingCount += omittedAnchors;
            missing.Add($"{omittedAnchors} additional related anchors omitted by payload bound");
        }
        ReviewItem bounded = item with
        {
            Path = Bound(item.Path, 1_024), Symbol = Bound(item.Symbol, 512), Summary = Bound(item.Summary, 1_000), Declaration = Bound(item.Declaration, 1_000),
            Question = Bound(question, 1_000), LegitimateCase = Bound(legitimateCase, 700), ObservedSignal = Bound(signal, 1_000),
            PrimaryAnchor = BoundAnchor(primary), RelatedAnchors = representedAnchors
        };
        foreach (ReviewAnchor anchor in anchors)
        {
            string label = $"{anchor.Project}:{anchor.Path}:{anchor.Line}:{anchor.Symbol}";
            if (!sourceContents.TryGetValue(anchor.Path, out string? source))
            {
                missingCount++; AddMissing($"{label} (source unavailable)");
                continue;
            }
            if (excerpts.Count >= MaximumExcerpts)
            {
                missingCount++; AddMissing($"{label} (excerpt limit {MaximumExcerpts} reached)");
                continue;
            }
            (string text, int first, int last) = RequiredSourceExcerpt(source, anchor.Line);
            string sourceHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(source)));
            var excerpt = new ReviewEvidenceExcerpt(Bound(anchor.Project, 1_024), Bound(anchor.Path, 1_024), first + 1, last + 1, Bound(anchor.Symbol, 512), text, sourceHash);
            ReviewEvidenceExcerpt candidate = excerpt;
            ReviewItem probe = bounded with { Evidence = excerpts.Append(candidate).ToArray(), ContextStatus = "available", MissingEvidence = [] };
            if (System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(probe).Length > MaximumUtf8Bytes)
            {
                string shortened = text;
                while (shortened.Length > 0)
                {
                    int target = Math.Max(0, (int)(Encoding.UTF8.GetByteCount(shortened) * 0.75));
                    shortened = TruncateUtf8(shortened, target);
                    candidate = excerpt with { Text = shortened };
                    probe = bounded with { Evidence = excerpts.Append(candidate).ToArray(), ContextStatus = "partial", MissingEvidence = ["Required source context was truncated to fit the 24,000 UTF-8 byte work-item bound."] };
                    if (System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(probe).Length <= MaximumUtf8Bytes) { break; }
                    if (target == 0) { break; }
                }
                if (System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(probe).Length > MaximumUtf8Bytes)
                {
                    missingCount++; AddMissing($"{label} (omitted because required context does not fit the 24,000 UTF-8 byte work-item bound)");
                    continue;
                }
                missingCount++; AddMissing($"{label} (excerpt truncated to fit the 24,000 UTF-8 byte work-item bound)");
            }
            excerpts.Add(candidate);
        }
        if (missingCount > missing.Count)
        {
            string summary = $"{missingCount - missing.Count} additional omissions omitted from the bounded detail list.";
            if (missing.Count == 8) { missing[^1] = summary; } else { missing.Add(summary); }
        }
        string status = excerpts.Count == 0 ? "unavailable" : missing.Count == 0 ? "available" : "partial";
        ReviewItem result = bounded with
        {
            Question = bounded.Question,
            LegitimateCase = bounded.LegitimateCase,
            ObservedSignal = bounded.ObservedSignal,
            ContextStatus = status,
            PrimaryAnchor = bounded.PrimaryAnchor,
            RelatedAnchors = representedAnchors,
            Evidence = excerpts,
            MissingEvidence = missing.Take(8).Select(value => Bound(value, 800)).ToArray()
        };
        if (System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(result).Length > MaximumUtf8Bytes)
        {
            result = result with { Summary = Bound(result.Summary, 500), Declaration = Bound(result.Declaration, 500), RelatedAnchors = [], MissingEvidence = ["Context metadata was bounded to keep the complete work item within 24,000 UTF-8 bytes."] };
            while (result.Evidence is { Count: > 0 } && System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(result).Length > MaximumUtf8Bytes)
            {
                result = result with { Evidence = result.Evidence.Take(result.Evidence.Count - 1).ToArray(), MissingEvidence = ["One or more required excerpts were omitted to satisfy the 24,000 UTF-8 byte work-item bound."] };
                missingCount++;
            }
            result = result with { ContextStatus = result.Evidence is { Count: > 0 } ? "partial" : "unavailable" };
        }
        return (result, []);

        void AddMissing(string value)
        {
            if (missing.Count < 8) { missing.Add(Bound(value, 800)); }
        }
    }

    private static (string Text, int FirstLine, int LastLine) RequiredSourceExcerpt(string source, int anchorLine)
    {
        Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
        Microsoft.CodeAnalysis.SyntaxNode root = tree.GetRoot();
        Microsoft.CodeAnalysis.Text.SourceText sourceText = tree.GetText();
        int line = Math.Clamp(anchorLine - 1, 0, Math.Max(0, sourceText.Lines.Count - 1));
        Microsoft.CodeAnalysis.SyntaxNode? node = root.FindToken(sourceText.Lines[line].Start).Parent?.AncestorsAndSelf()
            .FirstOrDefault(candidate => candidate is MemberDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax)
            ?? root.FindToken(sourceText.Lines[line].Start).Parent?.AncestorsAndSelf().FirstOrDefault(candidate => candidate is BaseTypeDeclarationSyntax);
        if (node is null) { return (sourceText.Lines[line].ToString(), line, line); }
        Microsoft.CodeAnalysis.Text.LinePositionSpan span = tree.GetLineSpan(node.Span).Span;
        return (node.ToFullString(), span.Start.Line, span.End.Line);
    }

    private static string Bound(string? value, int maximumChars) => string.IsNullOrEmpty(value) || value.Length <= maximumChars ? value ?? "" : value[..maximumChars] + "…";
    private static ReviewAnchor BoundAnchor(ReviewAnchor anchor) => anchor with
    {
        Project = Bound(anchor.Project, 256), Path = Bound(anchor.Path, 256), Symbol = Bound(anchor.Symbol, 128), Relation = Bound(anchor.Relation, 128)
    };

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
