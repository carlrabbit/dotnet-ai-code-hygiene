using Microsoft.CodeAnalysis;

namespace DotNetAiCodeHygiene.Core;

/// <inheritdoc/>
internal sealed class RuleContext(RepositorySession session, IReadOnlyList<Document> reportingDocuments)
{
/// <inheritdoc/>
    internal RepositorySession Session { get; } = session;
/// <inheritdoc/>
    internal IReadOnlyList<Document> ReportingDocuments { get; } = reportingDocuments;
}

/// <inheritdoc/>
internal interface IRuleModule
{
/// <inheritdoc/>
    public Rule Descriptor { get; }
/// <inheritdoc/>
    public IReadOnlyList<Finding> Evaluate(RuleContext context);
}

/// <summary>Executes explicitly supplied rule modules; the host remains responsible for result materialization.</summary>
internal sealed class RuleModuleRunner
{
    private readonly IReadOnlyList<IRuleModule> modules;

/// <inheritdoc/>
    internal RuleModuleRunner(IReadOnlyList<IRuleModule> modules) => this.modules = modules;

/// <inheritdoc/>
    internal IReadOnlyList<Finding> Run(RuleContext context) => modules.SelectMany(module => module.Evaluate(context)).ToArray();
}

/// <summary>Collects rule execution metadata and explicit module registrations.</summary>
internal static class RuleCatalog
{
    /// <summary>Canonical fixed rule identity, version, output, and order metadata.</summary>
    internal static readonly Rule[] All =
    [
        new("profile.dotnet.analysis.required", 1, "finding", "finding", "Require the supported .NET analysis profile.", false),
        new("profile.stylecop.prohibited", 1, "finding", "finding", "Prohibit StyleCop analyzers.", false),
        new("docs.summary.required", 2, "finding", "finding", "Require documentation summaries on covered API symbols."),
        new("docs.xml.consistent", 1, "finding", "finding", "Check present XML documentation structure and references."),
        new("docs.text.sentence", 1, "finding", "finding", "Require sentence punctuation in selected documentation prose."),
        new("docs.summary.quality.review", 3, "review-batch", "review-batch", "Review a deterministic sample of explicit documentation summaries for correctness, value, and clarity."),
        new("docs.summary.language.german.review", 1, "review-batch", "review-batch", "Review a deterministic sample of explicit documentation summaries for natural, comprehensible German."),
        new("readability.long-line.review", 1, "finding", "review-candidate", "Review unusually long physical source lines."),
        new("readability.control-flow.visual-block", 1, "finding", "finding", "Separate control-flow blocks visually from preceding statements.")
    ];

/// <inheritdoc/>
    /// <summary>Explicit document evaluators registered for fixed rules.</summary>
    internal static IReadOnlyList<IDocumentRuleModule> DocumentModules { get; } =
    [
        new DocumentationXmlConsistencyRuleModule(),
        new DocumentationSentenceRuleModule(),
        new LongLineReviewRuleModule(),
        new ControlFlowVisualBlockRuleModule()
    ];

    /// <summary>Runner for the explicit document evaluator registration.</summary>
    internal static DocumentRuleRunner DocumentRunner { get; } = new(DocumentModules);

/// <inheritdoc/>
    /// <summary>Explicit mandatory profile diagnosis modules.</summary>
    internal static IReadOnlyList<IProfileRuleModule> ProfileModules { get; } =
        [new ProfileAnalysisRuleModule(), new ProfileStyleCopRuleModule()];

    /// <summary>Runner for mandatory profile diagnosis modules.</summary>
    internal static ProfileRuleRunner ProfileRunner { get; } = new(ProfileModules);

/// <inheritdoc/>
    /// <summary>Explicit semantic review rules with independent policy metadata.</summary>
    internal static IReadOnlyList<ISemanticReviewRuleModule> SemanticReviewModules { get; } =
        [new SummaryQualityReviewRuleModule(), new SummaryGermanReviewRuleModule()];

    /// <summary>Runner for the registered semantic review rules.</summary>
    internal static SemanticReviewRuleRunner SemanticReviewRunner { get; } = new(SemanticReviewModules);
}
