using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed class ControlFlowVisualBlockRuleModule : IRuleModule
{
    internal const string RuleId = "readability.control-flow.visual-block";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "finding", "finding", "Separate control-flow blocks visually from preceding statements.", true);

    public Rule Descriptor => RuleDescriptor;

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
