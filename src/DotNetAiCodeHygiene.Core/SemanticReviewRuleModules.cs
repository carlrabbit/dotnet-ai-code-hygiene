namespace DotNetAiCodeHygiene.Core;

/// <inheritdoc/>
internal interface ISemanticReviewRuleModule
{
/// <inheritdoc/>
    public Rule Descriptor { get; }
/// <inheritdoc/>
    public int BatchNumber { get; }
/// <inheritdoc/>
    public IReadOnlyList<ReviewQuestion> Questions { get; }
}

/// <summary>Builds independent semantic review batches from shared subject populations.</summary>
internal sealed class SemanticReviewRuleRunner(IReadOnlyList<ISemanticReviewRuleModule> modules)
{
    /// <summary>Builds enabled batches in canonical catalog order.</summary>
    internal ReviewBatch[] BuildBatches(string run, IReadOnlySet<string> disabled,
        List<(string Identity, string Content, ReviewItem Item)> subjects, Dictionary<string, string> sourceContents) =>
        modules.Where(module => !disabled.Contains(module.Descriptor.Id))
            .Select(module => HygieneEngine.BuildReviewBatch(run, module.Descriptor, module.BatchNumber,
                subjects, sourceContents, module.Questions)).ToArray();
}

/// <inheritdoc/>
internal sealed class SummaryQualityReviewRuleModule : ISemanticReviewRuleModule
{
/// <inheritdoc/>
    public Rule Descriptor => RuleCatalog.All.Single(rule => rule.Id == "docs.summary.quality.review");
/// <inheritdoc/>
    public int BatchNumber => 1;
/// <inheritdoc/>
    public IReadOnlyList<ReviewQuestion> Questions => HygieneEngine.QualityQuestions;
}

/// <inheritdoc/>
internal sealed class SummaryGermanReviewRuleModule : ISemanticReviewRuleModule
{
/// <inheritdoc/>
    public Rule Descriptor => RuleCatalog.All.Single(rule => rule.Id == "docs.summary.language.german.review");
/// <inheritdoc/>
    public int BatchNumber => 2;
/// <inheritdoc/>
    public IReadOnlyList<ReviewQuestion> Questions => HygieneEngine.GermanQuestions;
}
