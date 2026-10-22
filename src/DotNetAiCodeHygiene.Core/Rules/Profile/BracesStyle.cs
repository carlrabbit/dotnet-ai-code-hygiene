using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace DotNetAiCodeHygiene.Core;

internal sealed class BracesStyleRuleModule : IRuleModule, IEditorConfigProjectionRule, INormalizeRemediationRule
{
    internal const string RuleId = "style.braces.required";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "finding", "finding", "Require braces through the built-in IDE0011 policy.");
    public Rule Descriptor => RuleDescriptor;
    public IReadOnlyList<KeyValuePair<string, string>> EditorConfigEntries { get; } =
    [new("csharp_prefer_braces", "true"), new("dotnet_diagnostic.IDE0011.severity", "error")];
    public RuleModuleResult Evaluate(RuleContext context) => RuleModuleResult.FindingsOnly(ProfileAnalysisRuleModule.Materialize(
        context.ProfileManager.AnalyzeEditorConfigRule(Descriptor.Id, EditorConfigEntries), Descriptor));

    public Document Normalize(Document document, Compilation compilation)
    {
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        StatementSyntax[] candidates = root.DescendantNodes().OfType<StatementSyntax>()
            .Where(statement => statement is not BlockSyntax and not EmptyStatementSyntax)
            .Where(statement => statement.Parent switch
            {
                IfStatementSyntax owner => ReferenceEquals(owner.Statement, statement),
                ElseClauseSyntax owner => owner.Statement is not IfStatementSyntax && ReferenceEquals(owner.Statement, statement),
                WhileStatementSyntax owner => ReferenceEquals(owner.Statement, statement),
                DoStatementSyntax owner => ReferenceEquals(owner.Statement, statement),
                ForStatementSyntax owner => ReferenceEquals(owner.Statement, statement),
                ForEachStatementSyntax owner => ReferenceEquals(owner.Statement, statement),
                ForEachVariableStatementSyntax owner => ReferenceEquals(owner.Statement, statement),
                UsingStatementSyntax owner => ReferenceEquals(owner.Statement, statement),
                LockStatementSyntax owner => ReferenceEquals(owner.Statement, statement),
                FixedStatementSyntax owner => ReferenceEquals(owner.Statement, statement),
                _ => false
            })
            .Where(statement => !statement.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsDirective))
            .ToArray();

        if (candidates.Length == 0)
        {
            return document;
        }

        SyntaxNode rewritten = root.ReplaceNodes(candidates, (original, current) =>
        {
            StatementSyntax body = (StatementSyntax)current;
            BlockSyntax block = SyntaxFactory.Block(body.WithoutLeadingTrivia().WithoutTrailingTrivia())
                .WithLeadingTrivia(body.GetLeadingTrivia())
                .WithTrailingTrivia(body.GetTrailingTrivia());
            return block.WithAdditionalAnnotations(Formatter.Annotation);
        });
        Document updated = document.WithSyntaxRoot(rewritten);
        return Formatter.FormatAsync(updated, Formatter.Annotation).GetAwaiter().GetResult();
    }
}
