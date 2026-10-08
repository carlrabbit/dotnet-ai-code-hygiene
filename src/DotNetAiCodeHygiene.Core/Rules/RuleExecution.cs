using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DotNetAiCodeHygiene.Core.Sampling;

namespace DotNetAiCodeHygiene.Core;

internal sealed record ReviewSubject(string Identity, string Content, ReviewItem Item, SubjectTicket? SubjectTicket = null, PopulationTicket? PopulationTicket = null);

internal sealed record RuleModuleResult(
    IReadOnlyList<Finding> Findings,
    IReadOnlyList<ReviewSubject> ReviewSubjects,
    IReadOnlyDictionary<string, string> ReviewSourceContents,
    IReadOnlyList<ReviewSubject>? SelectedReviewSubjects = null,
    string ReviewerClass = "implementer")
{
    internal static RuleModuleResult FindingsOnly(IEnumerable<Finding> findings) => new(findings.ToArray(), [], new Dictionary<string, string>());
    internal static RuleModuleResult Empty { get; } = FindingsOnly([]);
}

internal sealed record RuleModuleExecution(Rule Descriptor, RuleModuleResult Result);

internal enum SamplingExecutionBoundary
{
    Commit,
    Discard
}

/// <summary>Lazy services available to production rule modules for this command.</summary>
internal sealed class RuleContext(RepositorySession session, IReadOnlyList<Document> reportingDocuments, SamplingExecutionBoundary samplingBoundary = SamplingExecutionBoundary.Commit)
{
    private readonly Dictionary<(string RuleId, DocumentId DocumentId), Dictionary<string, int>> occurrenceCounts = [];
    private readonly Lazy<SamplingSession> sampling = new(() => new SamplingSession(session.Root), LazyThreadSafetyMode.ExecutionAndPublication);

    internal RepositorySession Session { get; } = session;
    internal IReadOnlyList<Document> ReportingDocuments { get; } = reportingDocuments;
    internal string RelativePath(Document document) => document.FilePath is string path ? Session.Relative(path) : document.Name;
    internal string ProjectIdentity(Document document) => document.Project.FilePath is string path ? Session.Relative(path) : document.Project.Name;
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
    internal SamplingSession Sampling => sampling.Value;
    internal bool HasSampling => sampling.IsValueCreated;
    internal void CompleteSampling()
    {
        if (samplingBoundary == SamplingExecutionBoundary.Commit && sampling.IsValueCreated) { sampling.Value.Commit(); }
    }
}

internal interface IRuleModule
{
    public Rule Descriptor { get; }
    public RuleModuleResult Evaluate(RuleContext context);
}

internal interface IFormatRemediationRule
{
    public Document Format(Document document, Compilation compilation);
}

internal interface INormalizeRemediationRule
{
    public Document Normalize(Document document, Compilation compilation);
}

internal interface IEditorConfigProjectionRule
{
    public IReadOnlyList<KeyValuePair<string, string>> EditorConfigEntries { get; }
}

/// <summary>Executes explicitly registered production rules; the host owns materialization and persistence.</summary>
internal sealed class RuleModuleRunner(IReadOnlyList<IRuleModule> modules)
{
    internal IReadOnlyList<RuleModuleExecution> Run(RuleContext context, IReadOnlySet<string> disabled) =>
        modules.Where(module => module.Descriptor.Diagnose && !disabled.Contains(module.Descriptor.Id))
            .Select(module => new RuleModuleExecution(module.Descriptor, module.Evaluate(context))).ToArray();
}

internal static class RuleCatalog
{
    internal static IReadOnlyList<IRuleModule> Modules { get; } =
    [
        new ProfileAnalysisRuleModule(), new ProfileStyleCopRuleModule(), new BracesStyleRuleModule(), new AccessibilityStyleRuleModule(),
        new RoslynFormatRuleModule(), new ThisQualificationRuleModule(), new RedundantQualificationRuleModule(),
        new DocumentationSummaryRequiredRuleModule(), new DocumentationXmlConsistencyRuleModule(), new DocumentationSentenceRuleModule(),
        new SummaryQualityReviewRuleModule(), new SummaryGermanReviewRuleModule(),
        new LongLineReviewRuleModule(), new ControlFlowVisualBlockRuleModule(), new BoringnessReviewRuleModule()
    ];

    internal static Rule[] All { get; } = Modules.Select(module => module.Descriptor with
    {
        FormatRemediate = module is IFormatRemediationRule,
        NormalizeRemediate = module is INormalizeRemediationRule,
        EditorConfigProject = module is IEditorConfigProjectionRule
    }).ToArray();
    internal static Rule Get(string id) => All.Single(rule => rule.Id == id);
    internal static RuleModuleRunner Runner { get; } = new(Modules);
    internal static IReadOnlyList<ISemanticReviewRuleModule> SemanticReviewModules { get; } = Modules.OfType<ISemanticReviewRuleModule>().ToArray();
    internal static SemanticReviewRuleRunner SemanticReviewRunner { get; } = new(SemanticReviewModules);
}
