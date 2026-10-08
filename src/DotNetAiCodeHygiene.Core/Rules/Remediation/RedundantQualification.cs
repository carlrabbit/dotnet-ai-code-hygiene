using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace DotNetAiCodeHygiene.Core;

internal sealed class RedundantQualificationRuleModule : IRuleModule, INormalizeRemediationRule
{
    private static readonly Rule RuleDescriptor = new("style.qualification.redundant", 1, "none", "none", "Remove redundant qualification where Roslyn proves semantic equivalence.", true, Diagnose: false);
    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;
    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        SemanticModel model = compilation.GetSemanticModel(document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!);
        SyntaxNode[] candidates = root.DescendantNodesAndSelf().Where(node => node is NameSyntax or MemberAccessExpressionSyntax)
            .Where(node => node is not MemberAccessExpressionSyntax member || member.Expression is not ThisExpressionSyntax)
            .Where(node => model.GetSymbolInfo(node).Symbol is not null).ToArray();
        if (candidates.Length == 0)
        {
            return document;
        }
        SyntaxNode annotated = root.ReplaceNodes(candidates, (oldNode, rewritten) => rewritten.WithAdditionalAnnotations(Simplifier.Annotation));
        Document updated = Simplifier.ReduceAsync(document.WithSyntaxRoot(annotated), Simplifier.Annotation).GetAwaiter().GetResult();
        return updated;
    }
}
