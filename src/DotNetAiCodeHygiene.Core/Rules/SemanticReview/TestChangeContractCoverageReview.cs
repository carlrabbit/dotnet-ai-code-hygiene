using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class TestChangeContractCoverageReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "tests.change-contract.coverage";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review changed production contracts together with compiler-resolved test relationships.", true);
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 32;
    public IReadOnlyList<ReviewQuestion> Questions { get; } = [new("Q1", "Do available tests protect the materially changed behavior and relevant failure paths?")];
    public string EscalationCondition => "Escalate to frontier when the observable contract or the available test evidence cannot be established from repository context.";
    public string EscalatedReviewerClass => "frontier";

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var documents = context.Session.Projects.SelectMany(project => project.Documents)
            .Where(RuleContext.IsEligibleSourceDocument).ToArray();
        var tests = new List<(Document Document, MethodDeclarationSyntax Method, IMethodSymbol Symbol, HashSet<ISymbol> References)>();
        foreach (Document document in documents)
        {
            SyntaxNode root = context.Root(document);
            SemanticModel model = context.SemanticModel(document);
            foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(method) is not IMethodSymbol symbol || !TestAssertionEffectivenessReviewRuleModule.IsTest(symbol)) { continue; }
                var refs = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                foreach (IdentifierNameSyntax identifier in method.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    if (model.GetSymbolInfo(identifier).Symbol is ISymbol target) { refs.Add(target.OriginalDefinition); }
                }
                tests.Add((document, method, symbol, refs));
            }
        }

        var subjects = new List<ReviewSubject>();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            SyntaxNode root = context.Root(document);
            SemanticModel model = context.SemanticModel(document);
            string path = context.RelativePath(document), source = context.Text(document).ToString();
            foreach (MemberDeclarationSyntax declaration in root.DescendantNodes().OfType<MemberDeclarationSyntax>().Where(IsProductionContract))
            {
                if (declaration is BaseTypeDeclarationSyntax && HasChangedContractDescendant(context, document, declaration)) { continue; }
                ISymbol? changedSymbol = GetDeclaredSymbol(model, declaration);
                if (changedSymbol is null) { continue; }
                var spanLines = context.Tree(document).GetLineSpan(declaration.Span);
                if (!context.HasChangedLine(path, spanLines.StartLinePosition.Line + 1, spanLines.EndLinePosition.Line + 1)) { continue; }
                var related = tests.Where(test => test.References.Any(reference => SymbolEqualityComparer.Default.Equals(reference, changedSymbol.OriginalDefinition)
                    || SymbolEqualityComparer.Default.Equals(reference.ContainingType, changedSymbol.ContainingType))).ToArray();
                string relatedEvidence = related.Length == 0 ? "Related test discovery: no compiler-resolved test reference found in readable eligible sources." :
                    "Related test declarations: " + string.Join("; ", related.Select(test => context.ProjectIdentity(test.Document) + "/" + context.RelativePath(test.Document) + "::" + test.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Order(StringComparer.Ordinal));
                string name = changedSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var item = new ReviewItem("", path, spanLines.StartLinePosition.Line + 1, spanLines.StartLinePosition.Character + 1, name, relatedEvidence, changedSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                string identity = context.ProjectIdentity(document) + "\0" + path + "\0contract:" + name;
                ReviewAnchor[] relatedAnchors = related.Select(test =>
                {
                    string testPath = context.RelativePath(test.Document);
                    var testPosition = context.Tree(test.Document).GetLineSpan(test.Method.Span).StartLinePosition;
                    return new ReviewAnchor(context.ProjectIdentity(test.Document), testPath, testPosition.Line + 1, testPosition.Character + 1,
                        test.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "compiler-resolved-related-test");
                }).Distinct().ToArray();
                subjects.Add(new ReviewSubject(identity, declaration.WithoutLeadingTrivia().ToFullString() + "\0" + relatedEvidence, item,
                    DirectChange: true, RelatedAnchors: relatedAnchors,
                    ObservedSignal: related.Length == 0 ? "Changed compiler-resolved production contract; no related compiler-resolved test method was discovered in readable eligible projects." : "Changed compiler-resolved production contract with related compiler-resolved test method(s).",
                    LegitimateCase: "No one-to-one test mapping is required; integration, contract, or indirect tests may cover the behavior even when direct symbol references are not discovered.",
                    PrimaryAnchor: new ReviewAnchor(context.ProjectIdentity(document), path, spanLines.StartLinePosition.Line + 1, spanLines.StartLinePosition.Character + 1, name, "changed-production-contract")));
                sources.TryAdd(path, source);
                foreach (var test in related)
                {
                    string testPath = context.RelativePath(test.Document);
                    sources.TryAdd(testPath, context.Text(test.Document).ToString());
                }
            }
        }
        foreach (var test in tests)
        {
            string path = context.RelativePath(test.Document);
            if (!context.ReportingDocuments.Contains(test.Document)) { continue; }
            var spanLines = context.Tree(test.Document).GetLineSpan(test.Method.Span);
            if (!context.HasChangedLine(path, spanLines.StartLinePosition.Line + 1, spanLines.EndLinePosition.Line + 1)) { continue; }
            bool referencesProduction = test.References.Any(symbol => symbol.ContainingAssembly is not null
                && !SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, test.Symbol.ContainingAssembly));
            if (!referencesProduction) { continue; }
            string name = test.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string source = context.Text(test.Document).ToString();
            var item = new ReviewItem("", path, spanLines.StartLinePosition.Line + 1, spanLines.StartLinePosition.Character + 1, name, "Changed framework-recognized test with compiler-resolved production references.", test.Symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            subjects.Add(new ReviewSubject(context.ProjectIdentity(test.Document) + "\0" + path + "\0test-contract:" + name,
                test.Method.WithoutLeadingTrivia().ToFullString(), item, DirectChange: true,
                RelatedAnchors: GetReferencedSourceAnchors(context, test),
                ObservedSignal: "Changed framework-recognized test with compiler-resolved references to production assembly symbols.",
                LegitimateCase: "A matching reference does not imply that the test is intended to cover every implementation detail or failure path.",
                PrimaryAnchor: new ReviewAnchor(context.ProjectIdentity(test.Document), path, spanLines.StartLinePosition.Line + 1, spanLines.StartLinePosition.Character + 1, name, "changed-test-contract")));
            sources.TryAdd(path, source);
        }
        return new RuleModuleResult([], subjects, sources) with { SelectedReviewSubjects = [] };
    }

    private static bool IsProductionContract(MemberDeclarationSyntax declaration) => declaration is MethodDeclarationSyntax or PropertyDeclarationSyntax or EventDeclarationSyntax or ConstructorDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax or InterfaceDeclarationSyntax;
    private static bool HasChangedContractDescendant(RuleContext context, Document document, MemberDeclarationSyntax declaration) =>
        declaration.DescendantNodes().OfType<MemberDeclarationSyntax>().Any(member =>
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
    private static ReviewAnchor[] GetReferencedSourceAnchors(RuleContext context, (Document Document, MethodDeclarationSyntax Method, IMethodSymbol Symbol, HashSet<ISymbol> References) test) =>
        test.References.SelectMany(symbol => symbol.Locations.Where(location => location.IsInSource && location.SourceTree?.FilePath is not null).Select(location =>
        {
            string fullPath = location.SourceTree!.FilePath;
            string path = context.Session.Relative(fullPath);
            var position = location.SourceTree.GetLineSpan(location.SourceSpan).StartLinePosition;
            string project = context.Session.Projects.FirstOrDefault(candidate => candidate.Documents.Any(document => string.Equals(document.FilePath, fullPath, StringComparison.OrdinalIgnoreCase)))?.FilePath is string projectPath
                ? context.Session.Relative(projectPath)
                : Path.GetDirectoryName(path) ?? "";
            return new ReviewAnchor(project, path, position.Line + 1, position.Character + 1, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "compiler-resolved-production-reference");
        })).Distinct().ToArray();
}
