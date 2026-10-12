using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class TypeResponsibilityReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "readability.type-responsibility.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review source-backed type declarations for coherent responsibility and change locality.", true);
    internal static readonly ReviewQuestion[] RuleQuestions =
    [
        new("Q1", "Ownership: Does this type mix responsibilities without one coherent ownership/purpose?"),
        new("Q2", "Coupling: Does understanding or changing ordinary behavior require reconstructing unrelated state or workflows within this type?"),
        new("Q3", "Separation: Is there a concrete responsibility that should be owned elsewhere without creating needless abstraction or fragmentation?")
    ];
    internal const string EscalationText = "Escalate material concerns or insufficient context to the planner. Do not infer a verdict from type length or member count.";
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 4;
    public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => EscalationText;
    public string EscalatedReviewerClass => "planner";

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>();
        var sourceContents = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            string path = context.RelativePath(document);
            string source = context.Text(document).ToString();
            SyntaxNode root = context.Root(document);
            SemanticModel model = context.SemanticModel(document);
            var declarations = root.DescendantNodes().OfType<MemberDeclarationSyntax>()
                .Where(node => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax or InterfaceDeclarationSyntax)
                .Select(node => (Node: node, Symbol: model.GetDeclaredSymbol(node)))
                .Where(pair => pair.Symbol is not null)
                .OrderBy(pair => pair.Node.SpanStart).ToArray();
            var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var declaration in declarations)
            {
                ISymbol symbol = declaration.Symbol!;
                string semanticName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string owner = context.ProjectIdentity(document) + "\0" + path + "\0" + semanticName;
                int ordinal = ordinals.GetValueOrDefault(owner);
                ordinals[owner] = ordinal + 1;
                var span = declaration.Node.Span;
                var position = context.Tree(document).GetLineSpan(span).StartLinePosition;
                string text = declaration.Node.WithoutLeadingTrivia().ToFullString();
                var item = new ReviewItem("", path, position.Line + 1, position.Character + 1, semanticName, "", symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                int endLine = context.Tree(document).GetLineSpan(span).EndLinePosition.Line + 1;
                subjects.Add(new ReviewSubject(owner + "\0part:" + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture), text, item,
                    DirectChange: context.HasChangedLine(path, position.Line + 1, endLine)));
            }
            if (declarations.Length > 0) { sourceContents[path] = source; }
        }
        var result = new RuleModuleResult([], subjects, sourceContents);
        return result with { SelectedReviewSubjects = context.SelectGate(Descriptor, subjects), ReviewerClass = "implementer" };
    }
}
