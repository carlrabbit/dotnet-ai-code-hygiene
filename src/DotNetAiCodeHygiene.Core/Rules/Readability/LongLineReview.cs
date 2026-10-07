using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed class LongLineReviewRuleModule : IRuleModule
{
    internal const string RuleId = "readability.long-line.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "finding", "review-candidate", "Review unusually long physical source lines.", true);

    public Rule Descriptor => RuleDescriptor;

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
