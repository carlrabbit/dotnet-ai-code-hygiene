using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed record ReviewSubject(string Identity, string Content, ReviewItem Item, GateTicket? GateTicket = null,
    bool DirectChange = false, bool RelatedChange = false, IReadOnlyList<ReviewAnchor>? RelatedAnchors = null,
    string? ObservedSignal = null, string? LegitimateCase = null, ReviewAnchor? PrimaryAnchor = null,
    string? RelatedEvidenceFingerprint = null, Func<RuleContext, ReviewSubject, ReviewSubject>? Enrich = null,
    string? EvidenceLimitation = null);

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

/// <summary>Lazy services available to production rule modules for this command.</summary>
internal sealed record GateSelectionContext(string GateId, string Mode, IReadOnlySet<string> ChangedPaths, IReadOnlyDictionary<string, HashSet<int>> ChangedLines, string? ExpandRuleId = null, int ExpandPageOffset = 0);

internal sealed class RuleContext(RepositorySession session, IReadOnlyList<Document> reportingDocuments, GateSelectionContext? gate = null, bool exhaustiveAudit = false)
{
    private readonly Dictionary<(string RuleId, DocumentId DocumentId), Dictionary<string, int>> occurrenceCounts = [];
    private readonly Lazy<GateLocalReviewStore> gateReviewStore = new(() => new GateLocalReviewStore(session.Root), LazyThreadSafetyMode.ExecutionAndPublication);
    private int relationshipAnalysisCount;

    internal RepositorySession Session { get; } = session;
    internal bool ExhaustiveAudit { get; } = exhaustiveAudit;
    internal IReadOnlyList<Document> ReportingDocuments { get; } = reportingDocuments;
    internal GateSelectionContext Gate { get; } = gate ?? new GateSelectionContext("", "closed", new HashSet<string>(StringComparer.Ordinal), new Dictionary<string, HashSet<int>>(StringComparer.Ordinal));
    internal GateLocalReviewStore GateReviewStore => gateReviewStore.Value;
    internal int RelationshipAnalysisCount => relationshipAnalysisCount;
    internal void RecordRelationshipAnalysis() => Interlocked.Increment(ref relationshipAnalysisCount);
    internal bool HasChangedLine(string path, int first, int last) => Gate.ChangedLines.TryGetValue(path, out HashSet<int>? lines) && lines.Any(line => line >= first && line <= last);
    internal IReadOnlyList<ReviewSubject> SelectGate(Rule rule, IReadOnlyList<ReviewSubject> population) =>
        ExhaustiveAudit ? population : Gate.ExpandRuleId == rule.Id
            ? population.Skip(Gate.ExpandPageOffset).Take(50).ToArray()
            : GateReviewStore.Select(Gate.GateId, rule, population, Gate.ChangedPaths, Gate.Mode);
    internal IReadOnlyDictionary<string, IReadOnlyList<ReviewSubject>> SelectGeneral(
        IReadOnlyList<(Rule Rule, IReadOnlyList<ReviewSubject> Population)> populations)
    {
        if (ExhaustiveAudit) { return populations.ToDictionary(p => p.Rule.Id, p => p.Population, StringComparer.Ordinal); }
        if (Gate.ExpandRuleId is string expandRule)
        {
            const int pageSize = 50;
            return populations.ToDictionary(p => p.Rule.Id, p => (IReadOnlyList<ReviewSubject>)(p.Rule.Id == expandRule
                ? p.Population.Skip(Gate.ExpandPageOffset).Take(pageSize).ToArray()
                : Array.Empty<ReviewSubject>()), StringComparer.Ordinal);
        }
        if (Gate.Mode == "closed") { return populations.ToDictionary(p => p.Rule.Id, _ => (IReadOnlyList<ReviewSubject>)Array.Empty<ReviewSubject>(), StringComparer.Ordinal); }
        return GateReviewStore.SelectShared(Gate.GateId, populations, Gate.ChangedPaths, Gate.Mode);
    }
    internal string RelationshipFingerprint(IReadOnlyList<ReviewAnchor>? anchors)
    {
        if (anchors is null || anchors.Count == 0) { return ""; }
        var parts = new List<string>();
        foreach (ReviewAnchor anchor in anchors.Distinct().OrderBy(value => value.Project, StringComparer.Ordinal).ThenBy(value => value.Path, StringComparer.Ordinal).ThenBy(value => value.Line))
        {
            RecordRelationshipAnalysis();
            Document? document = Session.Projects.SelectMany(project => project.Documents).FirstOrDefault(candidate =>
                string.Equals(RelativePath(candidate), anchor.Path.Replace('\\', '/'), StringComparison.Ordinal));
            if (document is null) { parts.Add(anchor.Project + "\0" + anchor.Path + "\0unavailable"); continue; }
            SyntaxNode root = Root(document);
            SourceText text = Text(document);
            int line = Math.Clamp(anchor.Line - 1, 0, Math.Max(0, text.Lines.Count - 1));
            SyntaxNode? tokenNode = root.FindToken(text.Lines[line].Start).Parent;
            SyntaxNode? declaration = tokenNode?.AncestorsAndSelf().FirstOrDefault(node => node is MemberDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax);
            if (declaration is null)
            {
                declaration = tokenNode?.AncestorsAndSelf().FirstOrDefault(node => node is BaseTypeDeclarationSyntax or EnumMemberDeclarationSyntax);
            }
            string evidence = declaration is null ? "unavailable" : declaration.ToFullString();
            parts.Add(anchor.Project + "\0" + anchor.Path + "\0" + anchor.Symbol + "\0" + GateLocalReviewStore.Fingerprint(evidence));
        }
        return GateLocalReviewStore.Fingerprint(string.Join("\n", parts));
    }
    internal string? SourceContent(string relativePath)
    {
        Document? document = Session.Projects.SelectMany(project => project.Documents).FirstOrDefault(candidate =>
            string.Equals(RelativePath(candidate), relativePath.Replace('\\', '/'), StringComparison.Ordinal));
        if (document is not null) { return Text(document).ToString(); }
        string path = Path.GetFullPath(Path.Combine(Session.Root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return Session.WithinRoot(path) && File.Exists(path) ? File.ReadAllText(path) : null;
    }
    internal string RelativePath(Document document) => document.FilePath is string path ? Session.Relative(path) : document.Name;
    internal string ProjectIdentity(Document document) => ProjectIdentity(document.Project);
    internal string ProjectIdentity(Project project) => project.FilePath is string path ? Session.Relative(path) : project.Name;
    internal static bool IsEligibleSourceDocument(Document document)
    {
        if (document.FilePath is not string path || !IsSourcePathEligible(path)) { return false; }
        return !HasGeneratedHeader(document.GetTextAsync().GetAwaiter().GetResult().ToString());
    }
    private static bool IsSourcePathEligible(string path)
    {
        if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) { return false; }
        string[] parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (parts.Any(x => x is "bin" or "obj" or ".git" or ".hygiene")) { return false; }
        string file = Path.GetFileName(path);
        if (file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)) { return false; }
        return true;
    }
    private static bool HasGeneratedHeader(string text)
    {
        string header = string.Join("\n", text.Split('\n').Take(5));
        return header.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase);
    }
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
    internal IReadOnlyList<ModernizationRetention> SourceExceptions(Document document)
    {
        string path = RelativePath(document);
        string source = Text(document).ToString();
        var options = Tree(document).Options as Microsoft.CodeAnalysis.CSharp.CSharpParseOptions;
        return Session.GetFact(("modernization-source-exceptions", document.Id), () => ModernizationSourceExceptions.Parse(path, source, options));
    }
    internal IReadOnlyList<ModernizationRetention> SourceExceptions(string path, string source)
    {
        Document? document = ReportingDocuments.FirstOrDefault(candidate => string.Equals(RelativePath(candidate), path, StringComparison.Ordinal));
        return document is null
            ? Session.GetFact(("modernization-source-exceptions", path), () => ModernizationSourceExceptions.Parse(path, source))
            : SourceExceptions(document);
    }
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
        modules.Where(module => module.Descriptor.Diagnose && !disabled.Contains(module.Descriptor.Id))
            .Select(module => new RuleModuleExecution(module.Descriptor, module.Evaluate(context))).ToArray();
}

internal static class RuleCatalog
{
    internal static IReadOnlyList<IRuleModule> Modules { get; } =
    [
        new ProfileAnalysisRuleModule(), new ProfileStyleCopRuleModule(),
        new DocumentationSummaryRequiredRuleModule(), new DocumentationXmlConsistencyRuleModule(), new DocumentationSentenceRuleModule(),
        new ModernizationSourceExceptionRuleModule(),
        .. ModernizationReviewRuleModule.All,
        new AbstractionNecessityReviewRuleModule(), new IndirectionValueReviewRuleModule(), new ResponsibilityCohesionReviewRuleModule(),
        new BoundaryOwnershipReviewRuleModule(), new AuthorityUniquenessReviewRuleModule(), new LifecycleBindingReviewRuleModule(), new MechanismExplicitnessReviewRuleModule(),
        new BehaviorErrorContractReviewRuleModule(), new BehaviorValidationBoundaryReviewRuleModule(), new BehaviorStateConsistencyReviewRuleModule(), new BehaviorResourceOwnershipReviewRuleModule(),
        new TestAssertionEffectivenessReviewRuleModule(), new TestChangeContractCoverageReviewRuleModule(), new TestIsolationCouplingReviewRuleModule(),
        new DocumentationSummaryQualityReviewRuleModule(), new DocumentationSummaryGermanReviewRuleModule(), new CommentIntentQualityReviewRuleModule()
    ];

    internal static Rule[] All { get; } = Modules.Select(module => module.Descriptor with
    {
        FormatRemediate = false,
        NormalizeRemediate = false,
        EditorConfigProject = false
    }).ToArray();
    internal static Rule Get(string id) => All.Single(rule => rule.Id == id);
    internal static RuleModuleRunner Runner { get; } = new(Modules);
    internal static IReadOnlyList<ISemanticReviewRuleModule> SemanticReviewModules { get; } = Modules.OfType<ISemanticReviewRuleModule>().ToArray();
    internal static SemanticReviewRuleRunner SemanticReviewRunner { get; } = new(SemanticReviewModules);
}
