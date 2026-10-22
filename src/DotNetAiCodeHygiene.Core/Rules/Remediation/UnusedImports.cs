using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Removes only using directives reported unnecessary by Roslyn IDE0005.</summary>
internal sealed class UnusedImportsRuleModule : IRuleModule, INormalizeRemediationRule
{
    internal const string RuleId = "style.usings.unused";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "none", "none",
        "Remove using directives only when Roslyn IDE0005 reports them unnecessary; preserve directives and comment-bearing imports.", true, Diagnose: false);

    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        if (!root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any()) { return document; }
        if (root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(HasCommentOrDirectiveTrivia)) { return document; }
        SyntaxTree tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!;
        ImmutableArray<Diagnostic> diagnostics = RoslynCodeStyleAnalyzers.Analyze(compilation, document.Project.AnalyzerOptions, "IDE0005");
        Diagnostic? fixable = diagnostics.FirstOrDefault(diagnostic => diagnostic.Id == "RemoveUnnecessaryImportsFixable"
            && diagnostic.Location.SourceTree == tree);
        return fixable is null ? document
            : RoslynCodeStyleAnalyzers.PreviewCodeFixAsync(document, fixable, CancellationToken.None).GetAwaiter().GetResult()?.ChangedDocument ?? document;
    }

    private static bool HasCommentOrDirectiveTrivia(UsingDirectiveSyntax directive) => directive.GetLeadingTrivia()
        .Concat(directive.GetTrailingTrivia()).Concat(directive.DescendantTrivia(descendIntoTrivia: true))
        .Any(trivia => trivia.IsDirective || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));
}
