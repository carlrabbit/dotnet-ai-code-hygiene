using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

/// <inheritdoc/>
internal sealed record RuleDocumentContext(
    Rule Rule,
    string RelativePath,
    SyntaxTree Tree,
    SyntaxNode Root,
    SemanticModel SemanticModel,
    string Text,
    Dictionary<string, int> OccurrenceCounts,
    Compilation Compilation,
    DocumentationSubjectFact DocumentationSubjects);

/// <inheritdoc/>
internal interface IDocumentRuleModule
{
/// <inheritdoc/>
    public string RuleId { get; }
/// <inheritdoc/>
    public IReadOnlyList<Finding> Evaluate(RuleDocumentContext context);
}

/// <summary>Runs selected document evaluators and returns their domain occurrences.</summary>
internal sealed class DocumentRuleRunner(IReadOnlyList<IDocumentRuleModule> modules)
{
    /// <summary>Evaluates enabled modules against one document context.</summary>
    internal IReadOnlyList<Finding> Run(RuleDocumentContext context, IReadOnlySet<string> disabled) =>
        modules.Where(module => !disabled.Contains(module.RuleId))
            .SelectMany(module => module.Evaluate(context with
            {
                Rule = RuleCatalog.All.Single(rule => rule.Id == module.RuleId)
            })).ToArray();
}

/// <inheritdoc/>
internal sealed class DocumentationSummaryRequiredRuleModule
{
/// <inheritdoc/>
    internal string RuleId => "docs.summary.required";

/// <inheritdoc/>
    internal Finding? Evaluate(DocumentationSummarySubject subject, Rule rule, string relativePath, SyntaxTree tree)
    {
        if (subject.IsPositionalRecordProperty || subject.HasSummary)
        {
            return null;
        }

        return HygieneEngine.Make(rule, relativePath, tree, subject.SourceOffset, subject.Symbol.ToDisplayString(),
            "Public or internal symbol has no non-empty documentation summary.",
            "Add a concise documentation summary for this API subject.", "The declaration has no summary text.",
            "This rule requires documentation, not a particular wording or language.", subject.Anchor, "missing-summary:" + subject.Anchor);
    }
}

/// <inheritdoc/>
internal sealed class PositionalRecordSummaryRequiredRuleModule
{
/// <inheritdoc/>
    internal Finding? Evaluate(DocumentationSummarySubject subject, Rule rule, string relativePath, SyntaxTree tree)
    {
        if (!subject.IsPositionalRecordProperty || subject.HasSummary || subject.HasDirectInheritdoc)
        {
            return null;
        }

        return HygieneEngine.Make(rule, relativePath, tree, subject.SourceOffset, subject.Anchor,
            "Public or internal positional record property has no non-empty documentation summary.",
            $"Add a non-empty <param name=\"{subject.ParameterName}\"> summary to the record documentation.",
            "The matching record parameter has no summary prose.",
            "Ordinary parameters remain optional documentation subjects.", subject.Anchor, "missing-record-property-summary:" + subject.Anchor);
    }
}

/// <inheritdoc/>
internal sealed class LongLineReviewRuleModule : IDocumentRuleModule
{
/// <inheritdoc/>
    public string RuleId => "readability.long-line.review";

/// <inheritdoc/>
    public IReadOnlyList<Finding> Evaluate(RuleDocumentContext context)
    {
        var findings = new List<Finding>();
        HygieneEngine.SourceTextLines(context.Text, (line, _, start) =>
        {
            if (line.Length <= 200)
            {
                return;
            }

            SyntaxNode? containing = context.Root.FindNode(new TextSpan(start, line.Length), getInnermostNodeForTie: false);
            SyntaxNode? anchorNode = containing?.AncestorsAndSelf().FirstOrDefault(n => n is MemberDeclarationSyntax or BaseTypeDeclarationSyntax);
            string anchor = anchorNode is not null && context.SemanticModel.GetDeclaredSymbol(anchorNode) is ISymbol symbol
                ? symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString()
                : context.RelativePath;
            string canonical = string.Join(" ", context.Root.DescendantTokens(new TextSpan(start, line.Length)).Select(token => token.ToString()));
            string discriminator = HygieneEngine.NextDiscriminator(context.OccurrenceCounts, context.Rule, anchor, canonical);
            findings.Add(HygieneEngine.Make(context.Rule, context.RelativePath, context.Tree, start,
                anchorNode is not null && context.SemanticModel.GetDeclaredSymbol(anchorNode) is ISymbol declared ? declared.ToDisplayString() : null,
                "Physical line exceeds 200 characters.",
                "Review whether the line hides multiple concepts or structures that should be made visible or named.",
                "The physical line exceeds 200 UTF-16 code units.",
                "Do not split mechanically merely to satisfy a line-length limit.", anchor, canonical + "\0" + discriminator, discriminator));
        });
        return findings;
    }
}

/// <inheritdoc/>
internal sealed class ControlFlowVisualBlockRuleModule : IDocumentRuleModule
{
/// <inheritdoc/>
    public string RuleId => "readability.control-flow.visual-block";

/// <inheritdoc/>
    public IReadOnlyList<Finding> Evaluate(RuleDocumentContext context)
    {
        var findings = new List<Finding>();

        foreach (BlockSyntax block in context.Root.DescendantNodes().OfType<BlockSyntax>())
        {
            StatementSyntax[] statements = block.Statements.ToArray();

            for (int i = 1; i < statements.Length; i++)
            {
                StatementSyntax current = statements[i], previous = statements[i - 1];

                if (!HygieneEngine.IsControl(current) || HygieneEngine.IsControl(previous) || previous is LocalFunctionStatementSyntax)
                {
                    continue;
                }

                int previousEnd = previous.Span.End;
                SyntaxTrivia[] comments = current.GetLeadingTrivia().Where(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineCommentTrivia)
                    || t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.MultiLineCommentTrivia)
                    || t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineDocumentationCommentTrivia)).ToArray();
                int boundary = comments.Length > 0 ? comments[0].SpanStart : current.SpanStart;
                string between = context.Text[Math.Min(previousEnd, context.Text.Length)..Math.Clamp(boundary, previousEnd, context.Text.Length)];
                string[] lines = between.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');

                if (lines.Length > 2 && lines.Skip(1).Take(lines.Length - 2).Any(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                ISymbol? container = context.SemanticModel.GetEnclosingSymbol(current.SpanStart);
                string anchor = container?.GetDocumentationCommentId() ?? container?.ToDisplayString() ?? context.RelativePath;
                string evidence = previous.Kind() + ":" + string.Join(" ", previous.DescendantTokens().Select(token => token.ToString()))
                    + "|" + current.Kind() + ":" + string.Join(" ", current.DescendantTokens().Select(token => token.ToString()));
                string discriminator = HygieneEngine.NextDiscriminator(context.OccurrenceCounts, context.Rule, anchor, evidence);
                findings.Add(HygieneEngine.Make(context.Rule, context.RelativePath, context.Tree, boundary, container?.ToDisplayString(),
                    "Control-flow statement needs a blank line after the preceding linear statement.",
                    "Insert one completely blank line before this control-flow group.",
                    "A control-flow statement immediately follows a linear statement without a blank line.",
                    "Keep comments documenting the control-flow statement with that statement.", anchor, evidence + "\0" + discriminator, discriminator));
            }
        }
        return findings;
    }
}

/// <inheritdoc/>
internal sealed class DocumentationXmlConsistencyRuleModule : IDocumentRuleModule
{
/// <inheritdoc/>
    public string RuleId => "docs.xml.consistent";

/// <inheritdoc/>
    public IReadOnlyList<Finding> Evaluate(RuleDocumentContext context) => EvaluateCovered(context)
        .Where(finding => finding.RuleId == RuleId).ToArray();

    private static IEnumerable<Finding> EvaluateCovered(RuleDocumentContext context)
    {
        foreach (MemberDeclarationSyntax member in context.DocumentationSubjects.CoveredDeclarations)
        {
            ISymbol symbol = context.SemanticModel.GetDeclaredSymbol(member)!;

            foreach (Finding finding in DocumentationRuleEvaluation.Evaluate(member, symbol, context.SemanticModel,
                context.Compilation, context.RelativePath, context.Tree))
            {
                yield return finding;
            }
        }
    }
}

/// <inheritdoc/>
internal sealed class DocumentationSentenceRuleModule : IDocumentRuleModule
{
/// <inheritdoc/>
    public string RuleId => "docs.text.sentence";

/// <inheritdoc/>
    public IReadOnlyList<Finding> Evaluate(RuleDocumentContext context)
    {
        var findings = new List<Finding>();

        foreach (MemberDeclarationSyntax member in context.DocumentationSubjects.CoveredDeclarations)
        {
            ISymbol symbol = context.SemanticModel.GetDeclaredSymbol(member)!;
            findings.AddRange(DocumentationRuleEvaluation.Evaluate(member, symbol, context.SemanticModel,
                context.Compilation, context.RelativePath, context.Tree).Where(finding => finding.RuleId == RuleId));
        }
        return findings;
    }
}
