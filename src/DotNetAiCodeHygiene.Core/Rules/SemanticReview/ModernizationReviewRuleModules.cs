using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

namespace DotNetAiCodeHygiene.Core;

internal sealed record ModernizationCandidateGroup(string Identity, string Path, int Line, string Owner,
    int TotalOccurrences, int RemainingOccurrences, string Summary, string Declaration, IReadOnlyList<ReviewOccurrence> Occurrences);

/// <summary>Adapts the explicitly registered M0023 candidate families to occurrence-bearing review packages.</summary>
internal sealed class ModernizationReviewRuleModule(
    Rule descriptor,
    int batchNumber,
    string question,
    Func<Project, Compilation, CancellationToken, bool, string?, IReadOnlyList<ModernizationCandidateGroup>> buildPopulation)
    : ISharedModernizationReviewRuleModule
{
    public Rule Descriptor { get; } = descriptor;
    public int BatchNumber { get; } = batchNumber;
    public IReadOnlyList<ReviewQuestion> Questions { get; } = [new("occurrence-judgment", question)];
    public string EscalationCondition => "If the implementer cannot judge an occurrence from its bounded context, record uncertain and request planner input outside the CLI.";
    public string EscalatedReviewerClass => "planner";
    internal IReadOnlyList<ModernizationCandidateGroup> BuildAllCandidates(Project project, Compilation compilation) =>
        buildPopulation(project, compilation, CancellationToken.None, true, null);
    internal IReadOnlyList<ModernizationCandidateGroup> BuildCandidatesForPath(Project project, Compilation compilation, string fullPath) =>
        buildPopulation(project, compilation, CancellationToken.None, true, Path.GetFullPath(fullPath));

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>();
        var sourceContents = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Project project in context.Session.Projects)
        {
            bool hasNativeException = project.Documents.Any(document =>
            {
                string path = context.RelativePath(document);
                string source = context.Text(document).ToString();
                return ModernizationSourceExceptions.ContainsAnnotation(source)
                    && context.SourceExceptions(path, source).Any(exception => exception.Valid && exception.IsNative && exception.RuleId == Descriptor.Id);
            });
            Project analysisProject = hasNativeException ? ModernizationSourceExceptions.WithoutNativePragmas(project, Descriptor.Id) : project;
            string candidateKey = "modernization-candidates\0" + project.Id + "\0" + Descriptor.Id + "\0" + analysisProject.Version + "\0" + context.ExhaustiveAudit;
            IReadOnlyList<ModernizationCandidateGroup> groups = context.Session.GetFact(candidateKey, () =>
            {
                Compilation compilation = analysisProject == project
                    ? context.Session.GetCompilation(project)
                    : analysisProject.GetCompilationAsync().GetAwaiter().GetResult()
                        ?? throw new ProductException($"Could not compile modernization project '{project.Name}'.");
                return buildPopulation(analysisProject, compilation, CancellationToken.None, context.ExhaustiveAudit, null);
            });
            foreach (ModernizationCandidateGroup group in groups)
            {
                string projectDirectory = Path.GetDirectoryName(project.FilePath!)!;
                string fullPath = Path.GetFullPath(Path.Combine(projectDirectory, group.Path));
                string normalizedPath = context.Session.Relative(fullPath);
                if (!File.Exists(fullPath)) { continue; }
                string source = File.ReadAllText(fullPath);
                sourceContents.TryAdd(normalizedPath, source);
                foreach (string occurrencePath in group.Occurrences.Select(occurrence => occurrence.Path).Distinct(StringComparer.Ordinal))
                {
                    string occurrenceFullPath = Path.GetFullPath(Path.Combine(projectDirectory, occurrencePath));
                    if (File.Exists(occurrenceFullPath))
                    {
                        sourceContents.TryAdd(context.Session.Relative(occurrenceFullPath), File.ReadAllText(occurrenceFullPath));
                    }
                }
                string content = string.Join("\n", group.Occurrences.Select(occurrence => string.Join("\0",
                    occurrence.Id, occurrence.DiagnosticId, occurrence.OriginalSyntax, occurrence.Context, occurrence.Preview)));
                bool directChange = group.Occurrences.Any(occurrence => context.HasChangedLine(
                    context.Session.Relative(Path.GetFullPath(Path.Combine(projectDirectory, occurrence.Path))), occurrence.Line, occurrence.Line));
                string projectIdentity = context.ProjectIdentity(project);
                ReviewOccurrence[] occurrences = group.Occurrences.Select(occurrence => occurrence with
                {
                    Id = ScopedOccurrenceId(projectIdentity, Descriptor.Id, occurrence.Id),
                    Path = context.Session.Relative(Path.GetFullPath(Path.Combine(projectDirectory, occurrence.Path)))
                }).ToArray();
                occurrences = ModernizationSourceExceptions.Filter(Descriptor.Id, occurrences, sourceContents, context, context.ExhaustiveAudit, out int suppressed);
                if (occurrences.Length == 0) { continue; }
                string summary = suppressed == 0 ? group.Summary : $"{group.Summary} {suppressed} valid source-owned retention exception(s) are excluded from this normal sample.";
                var item = new ReviewItem("", normalizedPath, group.Line, 1, group.Owner, summary, group.Declaration,
                    occurrences, Math.Max(0, group.TotalOccurrences - (context.ExhaustiveAudit ? 0 : suppressed)), group.RemainingOccurrences);
                subjects.Add(new ReviewSubject(Descriptor.Id + "\0" + projectIdentity + "\0" + group.Identity,
                    content, item, DirectChange: directChange));
            }
        }
        return new RuleModuleResult([], subjects, sourceContents, ReviewerClass: "implementer");
    }

    internal static IReadOnlyList<IRuleModule> All { get; } =
    [
        Create("modernization.var-style", 10, "Review whether var or an explicit local type improves readability in each listed occurrence.", BuildVarStyle),
        Create("modernization.expression-body", 11, "Review each expression-bodied member independently in its containing type.", BuildExpressionBodies),
        Create("modernization.collection-expression", 12, "Review each collection-expression conversion independently in its file context.", BuildCollectionExpressions),
        Create("modernization.primary-constructor", 13, "Review each primary-constructor proposal independently with related type declarations and state.", BuildPrimaryConstructors),
        Create("modernization.switch-expression", 14, "Review each switch-expression proposal independently with enclosing member behavior.", BuildSwitchExpressions),
        Create("modernization.conditional", 15, "Review each conditional-expression conversion independently with its control-flow context.", BuildConditionals),
        Create("modernization.object-initializer", 16, "Review each object-initializer proposal independently with construction and mutation context.", BuildObjectInitializers),
        Create("modernization.readonly-field", 17, "Review each readonly-field proposal independently with all type assignments and uses.", BuildReadonlyFields),
        Create("modernization.inline-declaration", 18, "Review only the concrete IDE0018 inline-declaration proposal; do not infer general variable merging.", BuildInlineDeclarations)
    ];

    private static ModernizationReviewRuleModule Create(string id, int batch, string question,
        Func<Project, Compilation, CancellationToken, bool, string?, IReadOnlyList<ModernizationCandidateGroup>> build) =>
        new(new Rule(id, 1, "review-batch", "review-batch", "Offer compiler-backed, non-mutating modernization candidates for implementer judgment.", true),
            batch, question, build);

    private static IReadOnlyList<ModernizationCandidateGroup> BuildVarStyle(Project project, Compilation compilation, CancellationToken token, bool exhaustive, string? onlyPath) =>
        VarStyleCandidatePopulation.Build(project, compilation, token, exhaustive, onlyPath).Select(group => Group(group.Path, group.Path, group.TotalEligibleOccurrences, group.RemainingOccurrences,
            group.Occurrences.Select(occurrence => Convert(occurrence.Id, occurrence.DiagnosticId, occurrence.Path, occurrence.Line, occurrence.Column,
                occurrence.Owner, occurrence.OriginalSyntax, occurrence.Context, occurrence.PreviewTitle, occurrence.PreviewDocument, occurrence.Question)))).ToArray();

    private static IReadOnlyList<ModernizationCandidateGroup> BuildExpressionBodies(Project project, Compilation compilation, CancellationToken token, bool exhaustive, string? onlyPath) =>
        ExpressionBodyCandidatePopulation.Build(project, compilation, token, exhaustive, onlyPath).Select(group => Group(group.TypeId, group.Occurrences[0].Path,
            group.TotalEligibleOccurrences, group.RemainingOccurrences, group.Occurrences.Select(occurrence => Convert(occurrence.Id, occurrence.DiagnosticId, occurrence.Path,
                occurrence.Line, 1, occurrence.Owner, occurrence.OriginalSyntax, occurrence.Context, occurrence.PreviewTitle, occurrence.PreviewDocument, occurrence.Question)))).ToArray();

    private static IReadOnlyList<ModernizationCandidateGroup> BuildCollectionExpressions(Project project, Compilation compilation, CancellationToken token, bool exhaustive, string? onlyPath) =>
        CollectionExpressionCandidatePopulation.Build(project, compilation, token, exhaustive, onlyPath).Select(group => Group(group.Path, group.Path, group.TotalEligibleOccurrences, group.RemainingOccurrences,
            group.Occurrences.Select(occurrence => Convert(occurrence.Id, occurrence.DiagnosticId, occurrence.Path, occurrence.Line, 1,
                "collection expression", occurrence.OriginalSyntax, occurrence.Context, occurrence.PreviewTitle, occurrence.PreviewDocument, occurrence.Question)))).ToArray();

    private static IReadOnlyList<ModernizationCandidateGroup> BuildPrimaryConstructors(Project project, Compilation compilation, CancellationToken token, bool exhaustive, string? onlyPath) =>
        PrimaryConstructorCandidatePopulation.Build(project, compilation, token, exhaustive, onlyPath).Select(group => Group(group.TypeId, group.Occurrences[0].Path,
            group.TotalEligibleOccurrences, group.RemainingOccurrences, group.Occurrences.Select(occurrence => Convert(occurrence.Id, "IDE0290", occurrence.Path, occurrence.Line, 1,
                occurrence.Owner, occurrence.OriginalSyntax, occurrence.Context, occurrence.PreviewTitle, occurrence.PreviewDocument, occurrence.Question)))).ToArray();

    private static IReadOnlyList<ModernizationCandidateGroup> BuildSwitchExpressions(Project project, Compilation compilation, CancellationToken token, bool exhaustive, string? onlyPath) =>
        SwitchExpressionCandidatePopulation.Build(project, compilation, token, exhaustive, onlyPath).Select(group => Group(group.MemberId, group.Path, group.TotalEligibleOccurrences, group.RemainingOccurrences,
            group.Occurrences.Select(occurrence => Convert(occurrence.Id, "IDE0066", occurrence.Path, occurrence.Line, 1, occurrence.Owner,
                occurrence.OriginalSyntax, occurrence.Context, occurrence.PreviewTitle, occurrence.PreviewDocument, occurrence.Question)))).ToArray();

    private static IReadOnlyList<ModernizationCandidateGroup> BuildConditionals(Project project, Compilation compilation, CancellationToken token, bool exhaustive, string? onlyPath) =>
        ConditionalCandidatePopulation.Build(project, compilation, token, exhaustive, onlyPath).Select(group => Group(group.MemberId, group.Path, group.TotalEligibleOccurrences, group.RemainingOccurrences,
            group.Occurrences.Select(occurrence => Convert(occurrence.Id, occurrence.DiagnosticId, occurrence.Path, occurrence.Line, 1,
                occurrence.Owner, occurrence.OriginalSyntax, occurrence.Context, occurrence.PreviewTitle, occurrence.PreviewDocument, occurrence.Question)))).ToArray();

    private static IReadOnlyList<ModernizationCandidateGroup> BuildObjectInitializers(Project project, Compilation compilation, CancellationToken token, bool exhaustive, string? onlyPath) =>
        ObjectInitializerCandidatePopulation.Build(project, compilation, token, exhaustive, onlyPath).Select(group => Group(group.MemberId, group.Path, group.TotalEligibleOccurrences, group.RemainingOccurrences,
            group.Occurrences.Select(occurrence => Convert(occurrence.Id, "IDE0017", occurrence.Path, occurrence.Line, 1, occurrence.Owner,
                occurrence.OriginalSyntax, occurrence.Context, occurrence.PreviewTitle, occurrence.PreviewDocument, occurrence.Question)))).ToArray();

    private static IReadOnlyList<ModernizationCandidateGroup> BuildReadonlyFields(Project project, Compilation compilation, CancellationToken token, bool exhaustive, string? onlyPath) =>
        ReadonlyFieldCandidatePopulation.Build(project, compilation, token, exhaustive, onlyPath).Select(group => Group(group.TypeId, group.Occurrences[0].Path,
            group.TotalEligibleOccurrences, group.RemainingOccurrences, group.Occurrences.Select(occurrence => Convert(occurrence.Id, "IDE0044", occurrence.Path, occurrence.Line, 1,
                occurrence.Owner, occurrence.OriginalSyntax, occurrence.Context, occurrence.PreviewTitle, occurrence.PreviewDocument, occurrence.Question)))).ToArray();

    private static IReadOnlyList<ModernizationCandidateGroup> BuildInlineDeclarations(Project project, Compilation compilation, CancellationToken token, bool exhaustive, string? onlyPath) =>
        InlineDeclarationCandidatePopulation.Build(project, compilation, token, exhaustive, onlyPath).Select(group => Group(group.MemberId, group.Path, group.TotalEligibleOccurrences, group.RemainingOccurrences,
            group.Occurrences.Select(occurrence => Convert(occurrence.Id, "IDE0018", occurrence.Path, occurrence.Line, 1, occurrence.Owner,
                occurrence.OriginalSyntax, occurrence.Context, occurrence.PreviewTitle, occurrence.PreviewDocument, occurrence.Question)))).ToArray();

    private static ModernizationCandidateGroup Group(string identity, string path, int total, int remaining, IEnumerable<ReviewOccurrence> occurrences)
    {
        ReviewOccurrence[] values = occurrences.ToArray();
        ReviewOccurrence first = values[0];
        return new ModernizationCandidateGroup(identity, path, first.Line, first.Owner, total, remaining,
            $"Selected {values.Length} of {total} eligible modernization occurrence(s); {remaining} remain outside this bounded package.", first.OriginalSyntax, values);
    }

    private static ReviewOccurrence Convert(string id, string diagnosticId, string path, int line, int column,
        string owner, string original, string context, string previewTitle, string preview, string question) =>
        new(id, diagnosticId, path, line, column, owner, original, context, previewTitle, preview, question);

    private static string ScopedOccurrenceId(string project, string rule, string candidate)
    {
        string value = rule + "\0" + project + "\0" + candidate;
        return "MO-" + System.Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
    }
}
