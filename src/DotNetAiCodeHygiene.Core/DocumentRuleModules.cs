using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed class DocumentationSummaryRequiredRuleModule : IRuleModule
{
    public Rule Descriptor => RuleCatalog.Get("docs.summary.required");

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var findings = new List<Finding>();
        foreach (Document document in context.ReportingDocuments)
        {
            SyntaxTree tree = context.Tree(document);
            string path = context.RelativePath(document);
            foreach (DocumentationSummarySubject subject in context.DocumentationSubjects(document).SummarySubjects)
            {
                if (subject.IsPositionalRecordProperty)
                {
                    if (!subject.HasSummary && !subject.HasDirectInheritdoc)
                    {
                        findings.Add(HygieneEngine.Make(Descriptor, path, tree, subject.SourceOffset, subject.Anchor,
                            "Public or internal positional record property has no non-empty documentation summary.",
                            $"Add a non-empty <param name=\"{subject.ParameterName}\"> summary to the record documentation.",
                            "The matching record parameter has no summary prose.",
                            "Ordinary parameters remain optional documentation subjects.", subject.Anchor, "missing-record-property-summary:" + subject.Anchor));
                    }
                }
                else if (!subject.HasSummary)
                {
                    findings.Add(HygieneEngine.Make(Descriptor, path, tree, subject.SourceOffset, subject.Symbol.ToDisplayString(),
                        "Public or internal symbol has no non-empty documentation summary.",
                        "Add a concise documentation summary for this API subject.", "The declaration has no summary text.",
                        "This rule requires documentation, not a particular wording or language.", subject.Anchor, "missing-summary:" + subject.Anchor));
                }
            }
        }
        return RuleModuleResult.FindingsOnly(findings);
    }
}

internal sealed class DocumentationXmlConsistencyRuleModule : IRuleModule
{
    public Rule Descriptor => RuleCatalog.Get("docs.xml.consistent");

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var findings = new List<Finding>();
        foreach (Document document in context.ReportingDocuments)
        {
            DocumentationSubjectFact subjects = context.DocumentationSubjects(document);
            SemanticModel model = context.SemanticModel(document);
            Compilation compilation = context.Compilation(document);
            foreach (MemberDeclarationSyntax declaration in subjects.CoveredDeclarations)
            {
                ISymbol symbol = model.GetDeclaredSymbol(declaration)!;
                findings.AddRange(DocumentationRuleEvaluation.EvaluateXml(declaration, symbol, model, compilation,
                    context.RelativePath(document), context.Tree(document), Descriptor));
            }
        }
        return RuleModuleResult.FindingsOnly(findings);
    }
}

internal sealed class DocumentationSentenceRuleModule : IRuleModule
{
    public Rule Descriptor => RuleCatalog.Get("docs.text.sentence");

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var findings = new List<Finding>();
        foreach (Document document in context.ReportingDocuments)
        {
            DocumentationSubjectFact subjects = context.DocumentationSubjects(document);
            SemanticModel model = context.SemanticModel(document);
            foreach (MemberDeclarationSyntax declaration in subjects.CoveredDeclarations)
            {
                ISymbol symbol = model.GetDeclaredSymbol(declaration)!;
                findings.AddRange(DocumentationRuleEvaluation.EvaluateSentences(declaration, symbol,
                    context.RelativePath(document), context.Tree(document), Descriptor));
            }
        }
        return RuleModuleResult.FindingsOnly(findings);
    }
}

internal sealed class LongLineReviewRuleModule : IRuleModule
{
    public Rule Descriptor => RuleCatalog.Get("readability.long-line.review");

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var findings = new List<Finding>();
        foreach (Document document in context.ReportingDocuments)
        {
            string text = context.Text(document).ToString();
            string path = context.RelativePath(document);
            SyntaxTree? tree = null;
            SyntaxNode? root = null;
            SemanticModel? model = null;
            Dictionary<string, int> counts = context.OccurrenceCounts(Descriptor.Id, document);
            HygieneEngine.SourceTextLines(text, (line, _, start) =>
            {
                if (line.Length <= 200)
                {
                    return;
                }

                tree ??= context.Tree(document);
                root ??= context.Root(document);
                SyntaxNode? containing = root.FindNode(new TextSpan(start, line.Length), getInnermostNodeForTie: false);
                SyntaxNode? anchorNode = containing?.AncestorsAndSelf().FirstOrDefault(n => n is MemberDeclarationSyntax or BaseTypeDeclarationSyntax);
                ISymbol? symbol = null;
                if (anchorNode is not null)
                {
                    model ??= context.SemanticModel(document);
                    symbol = model.GetDeclaredSymbol(anchorNode);
                }
                string anchor = symbol?.GetDocumentationCommentId() ?? symbol?.ToDisplayString() ?? path;
                string canonical = string.Join(" ", root.DescendantTokens(new TextSpan(start, line.Length)).Select(token => token.ToString()));
                string discriminator = HygieneEngine.NextDiscriminator(counts, Descriptor, anchor, canonical);
                findings.Add(HygieneEngine.Make(Descriptor, path, tree, start, symbol?.ToDisplayString(),
                    "Physical line exceeds 200 characters.",
                    "Review whether the line hides multiple concepts or structures that should be made visible or named.",
                    "The physical line exceeds 200 UTF-16 code units.",
                    "Do not split mechanically merely to satisfy a line-length limit.", anchor, canonical + "\0" + discriminator, discriminator));
            });
        }
        return RuleModuleResult.FindingsOnly(findings);
    }
}

internal sealed class ControlFlowVisualBlockRuleModule : IRuleModule
{
    public Rule Descriptor => RuleCatalog.Get("readability.control-flow.visual-block");

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var findings = new List<Finding>();
        foreach (Document document in context.ReportingDocuments)
        {
            SyntaxNode root = context.Root(document);
            string text = context.Text(document).ToString();
            SyntaxTree tree = context.Tree(document);
            SemanticModel model = context.SemanticModel(document);
            string path = context.RelativePath(document);
            Dictionary<string, int> counts = context.OccurrenceCounts(Descriptor.Id, document);
            foreach (BlockSyntax block in root.DescendantNodes().OfType<BlockSyntax>())
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
                    SyntaxTrivia[] comments = current.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia)
                        || t.IsKind(SyntaxKind.MultiLineCommentTrivia) || t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).ToArray();
                    int boundary = comments.Length > 0 ? comments[0].SpanStart : current.SpanStart;
                    string between = text[Math.Min(previousEnd, text.Length)..Math.Clamp(boundary, previousEnd, text.Length)];
                    string[] lines = between.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
                    if (lines.Length > 2 && lines.Skip(1).Take(lines.Length - 2).Any(string.IsNullOrWhiteSpace))
                    {
                        continue;
                    }

                    ISymbol? container = model.GetEnclosingSymbol(current.SpanStart);
                    string anchor = container?.GetDocumentationCommentId() ?? container?.ToDisplayString() ?? path;
                    string evidence = previous.Kind() + ":" + string.Join(" ", previous.DescendantTokens().Select(token => token.ToString()))
                        + "|" + current.Kind() + ":" + string.Join(" ", current.DescendantTokens().Select(token => token.ToString()));
                    string discriminator = HygieneEngine.NextDiscriminator(counts, Descriptor, anchor, evidence);
                    findings.Add(HygieneEngine.Make(Descriptor, path, tree, boundary, container?.ToDisplayString(),
                        "Control-flow statement needs a blank line after the preceding linear statement.",
                        "Insert one completely blank line before this control-flow group.",
                        "A control-flow statement immediately follows a linear statement without a blank line.",
                        "Keep comments documenting the control-flow statement with that statement.", anchor, evidence + "\0" + discriminator, discriminator));
                }
            }
        }
        return RuleModuleResult.FindingsOnly(findings);
    }
}
