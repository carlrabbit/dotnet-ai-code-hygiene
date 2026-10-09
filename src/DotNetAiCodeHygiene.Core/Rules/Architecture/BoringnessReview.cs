using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

internal sealed class BoringnessReviewRuleModule : ISemanticReviewRuleModule
{
    internal const string RuleId = "architecture.boringness.review";
    private static readonly Rule RuleDescriptor = new(RuleId, 1, "review-batch", "review-batch", "Sample source-backed types for observable architectural pressure that may warrant planner attention.", true);
    internal static readonly ReviewQuestion[] RuleQuestions =
    [
        new("Q1", "Speculative abstraction: Is there an interface, provider, factory, strategy, generic mechanism, extension point, or similar abstraction with only one meaningful production use/implementation and no current requirement for variability?"),
        new("Q2", "Indirection: To understand what this code actually does, must you follow several forwarding/delegation layers before reaching the behavior?"),
        new("Q3", "Change locality: Would a small behavioral change require coordinated edits across multiple plumbing types, registrations, adapters, or mappings rather than mostly changing the code that owns the behavior?"),
        new("Q4", "Hidden machinery: Does the implementation rely on reflection, dynamic discovery, convention-based registration, a custom DSL/framework, or other implicit machinery where ordinary explicit C# could satisfy the current requirement?"),
        new("Q5", "Unclear ownership: After reading the relevant types, is it difficult to identify the single place that owns the behavior or state being reviewed?")
    ];
    internal const string EscalationText = "Escalate to the planner when Q4 is yes or at least two of Q1, Q2, Q3, and Q5 are yes. Legitimate external-service, persistence, platform, and interoperability boundaries may justify abstraction.";
    private const double YearlyHazardRate = 1d / (365d * 24d * 60d * 60d);

    public Rule Descriptor => RuleDescriptor;
    public int BatchNumber => 3;
    public IReadOnlyList<ReviewQuestion> Questions => RuleQuestions;
    public string EscalationCondition => EscalationText;
    public string EscalatedReviewerClass => "planner";

    internal static bool RequiresPlannerEscalation(IReadOnlyList<bool> answers)
    {
        if (answers.Count != 5) { throw new ArgumentException("BORINGness review requires exactly five answers.", nameof(answers)); }
        return answers[3] || answers.Where((_, index) => index != 3).Count(answer => answer) >= 2;
    }

    internal static string PopulationFingerprint(IEnumerable<string> orderedCandidates) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\0", orderedCandidates))));

    internal static void RecordPopulationEvaluation(DotNetAiCodeHygiene.Core.Sampling.PopulationHazardSampler sampler,
        string unit, string fingerprint, int candidateCount, long cursor)
    {
        var old = sampler.State(unit);
        if (old.LastEvaluationFingerprint is null || !StringComparer.Ordinal.Equals(old.LastEvaluationFingerprint, fingerprint)) { sampler.AddHazard(unit, 1); }
        double previousCount = old.LastCandidateCount ?? 0;
        sampler.AccrueElapsed(unit, cursor, previousCount * YearlyHazardRate);
        sampler.SetEvaluationMetadata(unit, fingerprint, candidateCount);
    }

    public RuleModuleResult Evaluate(RuleContext context)
    {
        var populationSampler = context.Sampling.PopulationSampler(RuleId, 1);
        long cursor = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var subjects = new List<ReviewSubject>();
        var sourceContents = new Dictionary<string, string>(StringComparer.Ordinal);
        var units = new List<(string Path, string Fingerprint, List<ReviewSubject> Subjects)>();

        foreach (Document document in context.ReportingDocuments)
        {
            string path = context.RelativePath(document);
            string source = context.Text(document).ToString();
            SyntaxNode root = context.Root(document);
            var declarations = root.DescendantNodes().OfType<MemberDeclarationSyntax>()
                .Where(node => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax or InterfaceDeclarationSyntax)
                .Select(node => (Node: node, Symbol: context.SemanticModel(document).GetDeclaredSymbol(node)))
                .Where(pair => pair.Symbol is not null)
                .Select(pair => (pair.Node, Symbol: pair.Symbol!, Identity: pair.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
                .OrderBy(pair => pair.Node.SpanStart).ThenBy(pair => pair.Identity, StringComparer.Ordinal).ToArray();
            var current = new List<ReviewSubject>(declarations.Length);
            foreach (var declaration in declarations)
            {
                var position = context.Tree(document).GetLineSpan(declaration.Node.Span).StartLinePosition;
                string itemIdentity = path + "\0" + declaration.Identity + "\0" + declaration.Node.SpanStart;
                string declarationText = declaration.Node.ToFullString();
                var item = new ReviewItem("", path, position.Line + 1, position.Character + 1,
                    declaration.Identity, "", declaration.Symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                current.Add(new ReviewSubject(itemIdentity, declarationText, item));
            }
            if (current.Count == 0 && !populationSampler.States.Any(state => StringComparer.Ordinal.Equals(state.UnitId, path))) { continue; }
            string fingerprint = PopulationFingerprint(current.Select(x => x.Content));
            RecordPopulationEvaluation(populationSampler, path, fingerprint, current.Count, cursor);
            if (current.Count == 0) { continue; }
            subjects.AddRange(current);
            sourceContents[path] = source;
            units.Add((path, fingerprint, current));
        }

        var dueUnits = units.Select(unit => (Unit: unit, Event: populationSampler.DueEvents(unit.Path, 1).FirstOrDefault()))
            .Where(x => x.Event is not null)
            .OrderByDescending(x => x.Event!.ResidualAfterThreshold)
            .ThenBy(x => x.Unit.Path, StringComparer.Ordinal)
            .Take(5).ToArray();
        var selected = new List<ReviewSubject>();
        foreach (var due in dueUnits)
        {
            var candidate = populationSampler.SelectSubjects(due.Unit.Path, due.Unit.Subjects.Select(x => x.Identity).ToArray(), 1).SingleOrDefault();
            if (candidate.Candidate is null) { continue; }
            ReviewSubject subject = due.Unit.Subjects.Single(x => x.Identity == candidate.Candidate);
            selected.Add(subject with { PopulationTicket = candidate.Ticket });
        }
        return new RuleModuleResult([], subjects, sourceContents, selected, "implementer");
    }
}
