using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class TestQualityReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "tests.quality.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Review framework-recognized test methods for useful, maintainable regression evidence.", true);
    internal static readonly ReviewQuestion[] RuleQuestions =
    [
        new("Q1", "Regression evidence: Could the assertions plausibly detect a meaningful violation of the tested behavior rather than merely successful execution or a non-null result?"),
        new("Q2", "Contract focus: Does the test primarily examine observable behavior rather than unnecessarily binding itself to implementation details?"),
        new("Q3", "Maintainability: Is the setup/assertion structure clear enough to diagnose failure without unjustified fragility, coupling or mock choreography?")
    ];
    internal const string EscalationText = "Escalate a material concern or uncertainty to frontier. Assertion and mock counts are not quality verdicts.";
    private static readonly HashSet<string> TestAttributes = new(StringComparer.Ordinal)
    {
        "TUnit.Core.TestAttribute", "Xunit.FactAttribute", "Xunit.TheoryAttribute",
        "NUnit.Framework.TestAttribute", "NUnit.Framework.TestCaseAttribute",
        "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute", "Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute"
    };
    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 5;
    public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => EscalationText;
    public string EscalatedReviewerClass => "frontier";

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var subjects = new List<ReviewSubject>();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Document document in context.ReportingDocuments)
        {
            if (!RuleContext.IsEligibleSourceDocument(document)) { continue; }
            SyntaxNode root = context.Root(document);
            SemanticModel model = context.SemanticModel(document);
            string path = context.RelativePath(document), source = context.Text(document).ToString();
            foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>().OrderBy(x => x.SpanStart))
            {
                if (model.GetDeclaredSymbol(method) is not IMethodSymbol symbol || !IsTest(symbol)) { continue; }
                string owner = context.ProjectIdentity(document) + "\0" + path + "\0" + symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string methodName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var span = method.Span;
                var position = context.Tree(document).GetLineSpan(span).StartLinePosition;
                int endLine = context.Tree(document).GetLineSpan(span).EndLinePosition.Line + 1;
                var item = new ReviewItem("", path, position.Line + 1, position.Character + 1, methodName, "", symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                subjects.Add(new ReviewSubject(owner + "\0method:" + methodName, method.WithoutLeadingTrivia().ToFullString(), item,
                    DirectChange: context.HasChangedLine(path, position.Line + 1, endLine)));
                sources.TryAdd(path, source);
            }
        }
        var result = new RuleModuleResult([], subjects, sources);
        return result with { SelectedReviewSubjects = context.SelectGate(Descriptor, subjects), ReviewerClass = "implementer" };
    }

    private static bool IsTest(IMethodSymbol method) => method.GetAttributes().Any(attribute => IsKnownTestAttribute(attribute.AttributeClass));
    private static bool IsKnownTestAttribute(INamedTypeSymbol? type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (TestAttributes.Contains(current.ToDisplayString())) { return true; }
        }
        return false;
    }
}
