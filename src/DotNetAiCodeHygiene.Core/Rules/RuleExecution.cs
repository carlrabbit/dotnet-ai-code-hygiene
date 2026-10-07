using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed record ReviewSubject(string Identity, string Content, ReviewItem Item);

internal sealed record RuleModuleResult(
    IReadOnlyList<Finding> Findings,
    IReadOnlyList<ReviewSubject> ReviewSubjects,
    IReadOnlyDictionary<string, string> ReviewSourceContents)
{
    internal static RuleModuleResult FindingsOnly(IEnumerable<Finding> findings) => new(findings.ToArray(), [], new Dictionary<string, string>());
    internal static RuleModuleResult Empty { get; } = FindingsOnly([]);
}

internal sealed record RuleModuleExecution(Rule Descriptor, RuleModuleResult Result);

/// <summary>Lazy services available to production rule modules for this command.</summary>
internal sealed class RuleContext(RepositorySession session, IReadOnlyList<Document> reportingDocuments)
{
    private readonly Dictionary<(string RuleId, DocumentId DocumentId), Dictionary<string, int>> occurrenceCounts = [];

    internal RepositorySession Session { get; } = session;
    internal IReadOnlyList<Document> ReportingDocuments { get; } = reportingDocuments;
    internal string RelativePath(Document document) => document.FilePath is string path ? Session.Relative(path) : document.Name;
    internal SourceText Text(Document document) => Session.GetFact(("source-text", document.Id), () => document.GetTextAsync().GetAwaiter().GetResult());
    internal SyntaxTree Tree(Document document) => Session.GetFact(("syntax-tree", document.Id), () => document.GetSyntaxTreeAsync().GetAwaiter().GetResult() ?? throw new ProductException($"Roslyn did not provide syntax for '{document.FilePath}'."));
    internal SyntaxNode Root(Document document) => Session.GetFact(("syntax-root", document.Id), () => Tree(document).GetRoot());
    internal Compilation Compilation(Document document) => Session.GetCompilation(document.Project);
    internal SemanticModel SemanticModel(Document document) => Session.GetFact(("semantic-model", document.Id), () => Compilation(document).GetSemanticModel(Tree(document)));
    internal DocumentationSubjectFact DocumentationSubjects(Document document) =>
        Session.GetFact(("documentation-subjects", document.Id), () => DocumentationSubjectFact.Create(Root(document), SemanticModel(document)));
    internal Dictionary<string, int> OccurrenceCounts(string ruleId, Document document) =>
        occurrenceCounts.GetValueOrDefault((ruleId, document.Id)) ?? (occurrenceCounts[(ruleId, document.Id)] = new(StringComparer.Ordinal));
    internal ProfileManager ProfileManager => Session.GetFact("profile-manager", () => new ProfileManager(Session.Root));
}

internal interface IRuleModule
{
    public Rule Descriptor { get; }
    public RuleModuleResult Evaluate(RuleContext context);
}

/// <summary>Executes explicitly registered production rules; the host owns materialization and persistence.</summary>
internal sealed class RuleModuleRunner(IReadOnlyList<IRuleModule> modules)
{
    internal IReadOnlyList<RuleModuleExecution> Run(RuleContext context, IReadOnlySet<string> disabled) =>
        modules.Where(module => !disabled.Contains(module.Descriptor.Id))
            .Select(module => new RuleModuleExecution(module.Descriptor, module.Evaluate(context))).ToArray();
}

internal static class RuleCatalog
{
    internal static IReadOnlyList<IRuleModule> Modules { get; } =
    [
        new ProfileAnalysisRuleModule(), new ProfileStyleCopRuleModule(),
        new DocumentationSummaryRequiredRuleModule(), new DocumentationXmlConsistencyRuleModule(), new DocumentationSentenceRuleModule(),
        new SummaryQualityReviewRuleModule(), new SummaryGermanReviewRuleModule(),
        new LongLineReviewRuleModule(), new ControlFlowVisualBlockRuleModule()
    ];

    internal static Rule[] All { get; } = Modules.Select(module => module.Descriptor).ToArray();
    internal static Rule Get(string id) => All.Single(rule => rule.Id == id);
    internal static RuleModuleRunner Runner { get; } = new(Modules);
    internal static IReadOnlyList<ISemanticReviewRuleModule> SemanticReviewModules { get; } = Modules.OfType<ISemanticReviewRuleModule>().ToArray();
    internal static SemanticReviewRuleRunner SemanticReviewRunner { get; } = new(SemanticReviewModules);
}
