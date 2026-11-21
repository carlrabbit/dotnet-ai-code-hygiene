using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Finds changed type responsibility clusters and preserves all partial declarations.</summary>
internal sealed class ResponsibilityCohesionReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "architecture.responsibility.cohesion";
    private static readonly Rule RuleDescriptor = new(RuleId, 2, "review-batch", "review-batch", "Review changed type responsibility clusters without using size as a verdict.", true);
    private static readonly ReviewQuestion[] RuleQuestions = [new("Q1", "Does the type have a coherent responsibility, or mix separable owners?")];
    public Rule Descriptor => RuleDescriptor; public int BatchNumber => 22; public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => "Escalate only when a material ownership or design decision cannot be resolved from current authority.";
    public string EscalatedReviewerClass => "planner";
    public RuleModuleResult Evaluate(RuleContext context)
    {
        var groups = new Dictionary<string, List<(Document Document, TypeDeclarationSyntax Node, INamedTypeSymbol Symbol, string Path, string Source)>>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            string path = context.RelativePath(document), source = context.Text(document).ToString();
            SyntaxNode root = context.Root(document); SemanticModel model = context.SemanticModel(document);
            foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>().Where(type => type is not InterfaceDeclarationSyntax))
            {
                if (model.GetDeclaredSymbol(type) is not INamedTypeSymbol symbol) { continue; }
                string identity = context.ProjectIdentity(document) + "\0type:" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!groups.TryGetValue(identity, out var group)) { groups[identity] = group = []; }
                group.Add((document, type, symbol, path, source));
            }
        }

        var subjects = new List<ReviewSubject>(); var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string identity, var group) in groups)
        {
            var entries = group.OrderBy(entry => entry.Path, StringComparer.Ordinal).ThenBy(entry => entry.Node.SpanStart).ToArray();
            bool hasState = entries.Any(entry => entry.Node.Members.OfType<FieldDeclarationSyntax>().Any() || entry.Node.Members.OfType<PropertyDeclarationSyntax>().Any());
            bool hasOperations = entries.Any(entry => entry.Node.Members.OfType<MethodDeclarationSyntax>().Any());
            bool hasNested = entries.Any(entry => entry.Node.Members.OfType<TypeDeclarationSyntax>().Any());
            if (!(hasState && hasOperations) && !(hasNested && (hasState || hasOperations))) { continue; }
            var changed = entries.Where(entry => context.HasChangedLine(entry.Path,
                context.Tree(entry.Document).GetLineSpan(entry.Node.Span).StartLinePosition.Line + 1,
                context.Tree(entry.Document).GetLineSpan(entry.Node.Span).EndLinePosition.Line + 1)).ToArray();
            var primary = (changed.Length > 0 ? changed : entries)[0];
            var position = context.Tree(primary.Document).GetLineSpan(primary.Node.Span).StartLinePosition;
            string symbolName = primary.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string signal = "Compiler-resolved type combines persistent state and operations" + (hasNested ? " with at least one nested type owner" : "") +
                "; declaration count=" + entries.Length + ". Member counts are not scored.";
            const string legitimate = "Type size, member count, and the presence of multiple methods are not defects.";
            var item = new ReviewItem("", primary.Path, position.Line + 1, position.Character + 1, symbolName,
                "Candidate signal: " + signal, primary.Symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            ReviewAnchor[] partials = entries.Where(entry => entry.Document.Id != primary.Document.Id || entry.Node.Span != primary.Node.Span)
                .Select(entry => ReviewRelationshipFacts.Anchor(context, context.Tree(entry.Document), entry.Node.Span, entry.Symbol, "same-symbol-partial-type-declaration")).ToArray();
            subjects.Add(new ReviewSubject(identity, string.Join("\n", entries.Select(entry => entry.Node.WithoutLeadingTrivia().ToFullString())), item,
                DirectChange: changed.Length > 0, RelatedAnchors: partials, ObservedSignal: signal, LegitimateCase: legitimate,
                PrimaryAnchor: new ReviewAnchor(context.ProjectIdentity(primary.Document), primary.Path, position.Line + 1, position.Character + 1, symbolName, "changed-reporting-type"),
                EvidenceLimitation: "Unchanged partial declarations outside loaded project source may not be available."));
            foreach (var entry in entries) { sources.TryAdd(entry.Path, entry.Source); }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }
}
