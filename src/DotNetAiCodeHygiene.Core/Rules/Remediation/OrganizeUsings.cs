using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Deterministically orders simple namespace imports without moving aliases or trivia-bearing directives.</summary>
internal sealed class OrganizeUsingsRuleModule : IRuleModule, INormalizeRemediationRule
{
    internal const string RuleId = "style.usings.organize";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "none", "none",
        "Order uncomplicated namespace imports while preserving aliases, global/static imports, comments, and conditional regions.", true, Diagnose: false);

    public Rule Descriptor => RuleDescriptor;
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.Empty;

    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        if (root is not CompilationUnitSyntax compilationUnit) { return document; }
        SyntaxTree tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!;
        bool sortSystemFirst = SortSystemFirst(document, tree);

        CompilationUnitSyntax updated = compilationUnit.WithUsings(Organize(compilationUnit.Usings, sortSystemFirst));
        BaseNamespaceDeclarationSyntax[] namespaces = compilationUnit.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().ToArray();
        if (namespaces.Length > 0)
        {
            updated = (CompilationUnitSyntax)updated.ReplaceNodes(namespaces, (original, rewritten) =>
                rewritten.WithUsings(Organize(original.Usings, sortSystemFirst)));
        }

        return updated.IsEquivalentTo(compilationUnit) && updated.ToFullString() == compilationUnit.ToFullString()
            ? document
            : document.WithSyntaxRoot(updated);
    }

    private static bool SortSystemFirst(Document document, SyntaxTree tree)
    {
        AnalyzerConfigOptions options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);
        return !options.TryGetValue("dotnet_sort_system_directives_first", out string? value)
            || !bool.TryParse(value, out bool enabled)
            || enabled;
    }

    private static SyntaxList<UsingDirectiveSyntax> Organize(SyntaxList<UsingDirectiveSyntax> usings, bool sortSystemFirst)
    {
        if (usings.Count < 2) { return usings; }
        var result = usings.ToArray();
        int start = 0;
        while (start < result.Length)
        {
            if (!IsSafe(result[start])) { start++; continue; }
            int end = start + 1;
            while (end < result.Length && IsSafe(result[end])) { end++; }
            if (end - start > 1)
            {
                UsingDirectiveSyntax[] sorted = result[start..end]
                    .OrderBy(item => sortSystemFirst ? SystemGroup(item) : 0)
                    .ThenBy(item => item.Name!.ToString(), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.Name!.ToString(), StringComparer.Ordinal)
                    .ToArray();
                Array.Copy(sorted, 0, result, start, sorted.Length);
            }
            start = end;
        }
        return SyntaxFactory.List(result);
    }

    private static bool IsSafe(UsingDirectiveSyntax directive)
    {
        if (directive.Alias is not null || directive.StaticKeyword.RawKind != 0 || directive.GlobalKeyword.RawKind != 0
            || directive.Name is null || directive.Name.DescendantNodesAndSelf().Any(node => node is AliasQualifiedNameSyntax))
        {
            return false;
        }

        return !directive.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsDirective
            || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
            || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
            || trivia.IsKind(SyntaxKind.DisabledTextTrivia));
    }

    private static int SystemGroup(UsingDirectiveSyntax directive) =>
        directive.Name!.ToString().Equals("System", StringComparison.Ordinal)
        || directive.Name.ToString().StartsWith("System.", StringComparison.Ordinal) ? 0 : 1;
}
