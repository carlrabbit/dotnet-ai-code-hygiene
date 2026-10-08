using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace DotNetAiCodeHygiene.Core;

internal sealed class ThisQualificationRuleModule : IRuleModule, INormalizeRemediationRule
{
    private static readonly Rule RuleDescriptor = new("style.qualification.this.unnecessary", 1, "none", "none", "Remove explicit this qualification only when Roslyn proves it redundant.", true, Diagnose: false);
    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;
    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        SemanticModel model = compilation.GetSemanticModel(document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!);
        SyntaxNode[] candidates = root.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>()
            .Where(node => node.Expression is ThisExpressionSyntax && model.GetSymbolInfo(node).Symbol is not null).ToArray();
        if (candidates.Length == 0)
        {
            return document;
        }
        SyntaxNode annotated = root.ReplaceNodes(candidates, (oldNode, rewritten) => rewritten.WithAdditionalAnnotations(Simplifier.Annotation));
        Document updated = Simplifier.ReduceAsync(document.WithSyntaxRoot(annotated), Simplifier.Annotation).GetAwaiter().GetResult();
        return updated;
    }
}
