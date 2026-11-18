using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class TestChangeContractCoverageReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "tests.change-contract.coverage";
    private static readonly Rule RuleDescriptor = new(RuleId, 3, "review-batch", "review-batch", "Review changed production contracts together with compiler-resolved test relationships.", true);
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 32;
    public IReadOnlyList<ReviewQuestion> Questions { get; } = [new("Q1", "Do available tests protect the materially changed behavior and relevant failure paths?")];
    public string EscalationCondition => "Escalate to frontier when the observable contract or the available test evidence cannot be established from repository context.";
    public string EscalatedReviewerClass => "frontier";

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var contracts = new Dictionary<string, List<(Document Document, MemberDeclarationSyntax Node, ISymbol Symbol, string Path, string Source)>>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments.Where(RuleContext.IsEligibleSourceDocument))
        {
            SyntaxNode root = context.Root(document); SemanticModel model = context.SemanticModel(document);
            string path = context.RelativePath(document), source = context.Text(document).ToString();
            foreach (MemberDeclarationSyntax declaration in root.DescendantNodes().OfType<MemberDeclarationSyntax>().Where(IsProductionContract))
            {
                ISymbol? symbol = GetDeclaredSymbol(model, declaration);
                if (symbol is null) { continue; }
                if (declaration is BaseTypeDeclarationSyntax && HasChangedContractDescendant(context, document, declaration)) { continue; }
                Microsoft.CodeAnalysis.Text.LinePositionSpan lines = context.Tree(document).GetLineSpan(declaration.Span).Span;
                if (!context.HasChangedLine(path, lines.Start.Line + 1, lines.End.Line + 1)) { continue; }
                string identity = context.ProjectIdentity(document) + "\0" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!contracts.TryGetValue(identity, out var group)) { contracts[identity] = group = []; }
                group.Add((document, declaration, symbol, path, source));
            }
        }

        var subjects = new List<ReviewSubject>();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string identity, var group) in contracts)
        {
            var entries = group.OrderBy(entry => entry.Path, StringComparer.Ordinal).ThenBy(entry => entry.Node.SpanStart).ToArray();
            var primary = entries[0];
            var lines = context.Tree(primary.Document).GetLineSpan(primary.Node.Span).Span;
            string name = primary.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string key = identity + "\0contract:" + name;
            var item = new ReviewItem("", primary.Path, lines.Start.Line + 1, lines.Start.Character + 1, name,
                "Changed compiler-resolved production contract; detailed test-reference relationships are resolved only for selected work.",
                primary.Symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            const string legitimate = "No one-to-one test mapping is required; integration or contract tests may protect the behavior. Same-type references to another member are not exact tests of this changed symbol.";
            ISymbol changed = primary.Symbol;
            ReviewAnchor[] partials = entries.Skip(1).Select(entry => ReviewRelationshipFacts.Anchor(context, context.Tree(entry.Document), entry.Node.Span, entry.Symbol, "same-symbol-changed-partial-contract")).ToArray();
            subjects.Add(new ReviewSubject(key, string.Join("\n", entries.Select(entry => entry.Node.WithoutLeadingTrivia().ToFullString())), item,
                DirectChange: true, RelatedAnchors: partials,
                ObservedSignal: "Changed compiler-resolved production declaration; direct and indirect test relationships not yet materialized.",
                LegitimateCase: legitimate,
                PrimaryAnchor: new ReviewAnchor(context.ProjectIdentity(primary.Document), primary.Path, lines.Start.Line + 1, lines.Start.Character + 1, name, "changed-production-contract"),
                Enrich: (_, selected) => EnrichProduction(selected, changed)));
            foreach (var entry in entries) { sources.TryAdd(entry.Path, entry.Source); }
        }

        // Changed tests are inspected only in their reporting scope. Exact source references are
        // retained as evidence; broad type-level association is never described as exact coverage.
        foreach (Document document in context.ReportingDocuments.Where(RuleContext.IsEligibleSourceDocument))
        {
            SyntaxNode root = context.Root(document); SemanticModel model = context.SemanticModel(document);
            string path = context.RelativePath(document), source = context.Text(document).ToString();
            foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(method) is not IMethodSymbol testSymbol || !TestAssertionEffectivenessReviewRuleModule.IsTest(testSymbol)) { continue; }
                Microsoft.CodeAnalysis.Text.LinePositionSpan lines = context.Tree(document).GetLineSpan(method.Span).Span;
                if (!context.HasChangedLine(path, lines.Start.Line + 1, lines.End.Line + 1)) { continue; }
                ISymbol[] referenced = References(method, model);
                ISymbol[] production = referenced.Where(symbol => symbol.ContainingAssembly is not null &&
                    !SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, testSymbol.ContainingAssembly)).ToArray();
                if (production.Length == 0) { continue; }
                string name = testSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                ReviewAnchor[] referencedAnchors = ReviewRelationshipFacts.RelatedSymbols(context, production, "compiler-resolved-production-reference-from-changed-test");
                subjects.Add(new ReviewSubject(context.ProjectIdentity(document) + "\0test-contract:" + name, method.WithoutLeadingTrivia().ToFullString(),
                    new ReviewItem("", path, lines.Start.Line + 1, lines.Start.Character + 1, name,
                        "Changed framework-recognized test with compiler-resolved references to production symbols; reference kind is listed in evidence.",
                        testSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)), DirectChange: true,
                    RelatedAnchors: referencedAnchors, ObservedSignal: "Changed framework-recognized test with compiler-resolved production reference(s); a reference alone does not establish what behavior the test protects.",
                    LegitimateCase: "A matching reference does not imply that the test protects every implementation detail or failure path.",
                    PrimaryAnchor: new ReviewAnchor(context.ProjectIdentity(document), path, lines.Start.Line + 1, lines.Start.Character + 1, name, "changed-test-contract")));
                sources.TryAdd(path, source);
            }
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };

        ReviewSubject EnrichProduction(ReviewSubject subject, ISymbol changedSymbol)
        {
            var direct = new List<ReviewAnchor>();
            var sameTypeOtherMember = new List<ReviewAnchor>();
            var containingTypeOnly = new List<ReviewAnchor>();
            foreach (Project project in context.Session.Projects)
            {
                Compilation compilation = context.Session.GetCompilation(project);
                foreach (SyntaxTree tree in compilation.SyntaxTrees)
                {
                    context.RecordRelationshipAnalysis();
                    SemanticModel model = compilation.GetSemanticModel(tree);
                    foreach (MethodDeclarationSyntax testMethod in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                    {
                        if (model.GetDeclaredSymbol(testMethod) is not IMethodSymbol test || !TestAssertionEffectivenessReviewRuleModule.IsTest(test)) { continue; }
                        ISymbol[] references = References(testMethod, model);
                        bool exact = references.Any(reference => SymbolEqualityComparer.Default.Equals(reference.OriginalDefinition, changedSymbol.OriginalDefinition));
                        bool sibling = !exact && references.Any(reference => reference.ContainingType is not null && changedSymbol.ContainingType is not null &&
                            SymbolEqualityComparer.Default.Equals(reference.ContainingType.OriginalDefinition, changedSymbol.ContainingType.OriginalDefinition));
                        bool typeOnly = !exact && !sibling && changedSymbol.ContainingType is not null && references.Any(reference => reference is INamedTypeSymbol type &&
                            SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, changedSymbol.ContainingType.OriginalDefinition));
                        if (!exact && !sibling && !typeOnly) { continue; }
                        string relation = exact ? "direct-exact-changed-symbol-reference" : sibling ? "same-containing-type-other-member-reference-not-exact" : "containing-type-reference-only-indirect";
                        ReviewAnchor anchor = ReviewRelationshipFacts.Anchor(context, tree, testMethod.Span, test, relation);
                        (exact ? direct : sibling ? sameTypeOtherMember : containingTypeOnly).Add(anchor);
                    }
                }
            }
            ReviewAnchor[] all = direct.Concat(sameTypeOtherMember).Concat(containingTypeOnly).Distinct().ToArray();
            string summary = $"Compiler-resolved test references: direct exact symbol={direct.Count}; same-type other member (not exact)={sameTypeOtherMember.Count}; containing-type only={containingTypeOnly.Count}; other integration/contract coverage remains unverified.";
            return subject with { RelatedAnchors = all, ObservedSignal = summary,
                EvidenceLimitation = "Compiler reference analysis does not prove test intent or enumerate integration/contract coverage with no direct symbol relationship." };
        }
    }

    private static ISymbol[] References(MethodDeclarationSyntax method, SemanticModel model) => method.DescendantNodes().OfType<IdentifierNameSyntax>()
        .Select(identifier => model.GetSymbolInfo(identifier).Symbol?.OriginalDefinition).Where(symbol => symbol is not null).Cast<ISymbol>()
        .Distinct(SymbolEqualityComparer.Default).ToArray();

    private static bool IsProductionContract(MemberDeclarationSyntax declaration) => declaration is MethodDeclarationSyntax or PropertyDeclarationSyntax or EventDeclarationSyntax or ConstructorDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax or InterfaceDeclarationSyntax;
    private static bool HasChangedContractDescendant(RuleContext context, Document document, MemberDeclarationSyntax declaration) => declaration.DescendantNodes().OfType<MemberDeclarationSyntax>().Any(member =>
    {
        var span = context.Tree(document).GetLineSpan(member.Span);
        return context.HasChangedLine(context.RelativePath(document), span.StartLinePosition.Line + 1, span.EndLinePosition.Line + 1);
    });
    private static ISymbol? GetDeclaredSymbol(SemanticModel model, MemberDeclarationSyntax declaration) => declaration switch
    {
        BaseTypeDeclarationSyntax type => model.GetDeclaredSymbol(type),
        MethodDeclarationSyntax method => model.GetDeclaredSymbol(method),
        PropertyDeclarationSyntax property => model.GetDeclaredSymbol(property),
        EventDeclarationSyntax @event => model.GetDeclaredSymbol(@event),
        ConstructorDeclarationSyntax constructor => model.GetDeclaredSymbol(constructor),
        _ => null
    };
}
