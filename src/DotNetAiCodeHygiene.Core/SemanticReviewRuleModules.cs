using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal interface ISemanticReviewRuleModule : IRuleModule
{
    public int BatchNumber { get; }
    public IReadOnlyList<ReviewQuestion> Questions { get; }
}

internal sealed class SemanticReviewRuleRunner(IReadOnlyList<ISemanticReviewRuleModule> modules)
{
    internal ReviewBatch[] BuildBatches(string run, IReadOnlyList<RuleModuleExecution> executions)
    {
        var byId = executions.ToDictionary(execution => execution.Descriptor.Id, StringComparer.Ordinal);
        return modules.Where(module => byId.ContainsKey(module.Descriptor.Id))
            .Select(module =>
            {
                RuleModuleResult result = byId[module.Descriptor.Id].Result;
                return HygieneEngine.BuildReviewBatch(run, module.Descriptor, module.BatchNumber,
                    result.ReviewSubjects, result.ReviewSourceContents.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal), module.Questions);
            }).ToArray();
    }
}

internal sealed class SummaryQualityReviewRuleModule : ISemanticReviewRuleModule
{
    public Rule Descriptor => RuleCatalog.Get("docs.summary.quality.review");
    public int BatchNumber => 1;
    public IReadOnlyList<ReviewQuestion> Questions => HygieneEngine.QualityQuestions;
    public RuleModuleResult Evaluate(RuleContext context) => BuildPopulation(context);
    internal static RuleModuleResult BuildPopulation(RuleContext context) => SummaryReviewPopulation.CreatePopulation(context);
}

internal sealed class SummaryGermanReviewRuleModule : ISemanticReviewRuleModule
{
    public Rule Descriptor => RuleCatalog.Get("docs.summary.language.german.review");
    public int BatchNumber => 2;
    public IReadOnlyList<ReviewQuestion> Questions => HygieneEngine.GermanQuestions;
    public RuleModuleResult Evaluate(RuleContext context) => SummaryQualityReviewRuleModule.BuildPopulation(context);
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
                subjects.Add(new ReviewSubject(path + "\0" + subject.Anchor, subject.CarrierContent, item));
                sourceContents.TryAdd(path, context.Text(document).ToString());
            }
        }
        return new RuleModuleResult([], subjects, sourceContents);
    }
}
