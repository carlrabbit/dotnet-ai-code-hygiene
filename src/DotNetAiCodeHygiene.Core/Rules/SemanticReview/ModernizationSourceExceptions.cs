using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed record ModernizationRetention(string Path, string RuleId, string Reason, int CommentLine, TextSpan SubjectSpan, bool Valid);

internal sealed class ModernizationSourceExceptionRuleModule : IRuleModule
{
    private static readonly Rule DescriptorValue = new("modernization.source-exception", 1, "finding", "finding", "Validate narrow source-owned modernization retention annotations.", false);
    public Rule Descriptor => DescriptorValue;
    public RuleModuleResult Evaluate(RuleContext context)
    {
        var findings = new List<Finding>();
        HashSet<(string RuleId, string Path, int Line)> candidates = GetAllCandidateLines(context);
        foreach (Document document in context.ReportingDocuments)
        {
            string path = context.RelativePath(document);
            string source = context.Text(document).ToString();
            foreach (ModernizationRetention retention in ModernizationSourceExceptions.Parse(path, source))
            {
                bool stale = retention.Valid && !candidates.Any(candidate => candidate.RuleId == retention.RuleId
                    && candidate.Path == path && CoversLine(source, retention.SubjectSpan, candidate.Line));
                if (retention.Valid && !stale) { continue; }
                string message = stale ? "Stale modernization retention comment: no matching eligible candidate remains in its attached syntax subject."
                    : "Malformed, unknown-rule, unjustified, or unattached modernization retention comment.";
                findings.Add(new Finding("", "", Descriptor.Id, Descriptor.Version, Descriptor.Classification, path, retention.CommentLine, 1,
                    retention.RuleId, message, "Use // hygiene:retain modernization.<known-rule> -- <concrete local reason> immediately before one declaration or statement.",
                    "Source-local modernization retention annotations must be narrow and justified.", message, "The comment must name one enabled modernization rule, attach to one source syntax subject, and explain a concrete local constraint.",
                    "source-exception:" + retention.CommentLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(source + "\0" + retention.CommentLine)))));
            }
        }
        return RuleModuleResult.FindingsOnly(findings);
    }

    private static HashSet<(string RuleId, string Path, int Line)> GetAllCandidateLines(RuleContext context)
    {
        var result = new HashSet<(string, string, int)>();
        foreach (Project project in context.Session.Projects)
        {
            string projectDirectory = Path.GetDirectoryName(project.FilePath!)!;
            Compilation compilation = context.Session.GetCompilation(project);
            foreach (ModernizationReviewRuleModule module in ModernizationReviewRuleModule.All.OfType<ModernizationReviewRuleModule>())
            {
                foreach (ModernizationCandidateGroup group in module.BuildAllCandidates(project, compilation))
                {
                    foreach (ReviewOccurrence occurrence in group.Occurrences)
                    {
                        string fullPath = Path.GetFullPath(Path.IsPathRooted(occurrence.Path) ? occurrence.Path : Path.Combine(projectDirectory, occurrence.Path));
                        result.Add((module.Descriptor.Id, context.Session.Relative(fullPath), occurrence.Line));
                    }
                }
            }
        }
        return result;
    }

    private static bool CoversLine(string source, TextSpan span, int oneBasedLine)
    {
        SourceText text = SourceText.From(source);
        if (oneBasedLine <= 0 || oneBasedLine > text.Lines.Count || span.Length == 0) { return false; }
        int start = text.Lines.GetLineFromPosition(span.Start).LineNumber + 1;
        int end = text.Lines.GetLineFromPosition(Math.Max(span.Start, span.End - 1)).LineNumber + 1;
        return oneBasedLine >= start && oneBasedLine <= end;
    }
}

/// <summary>Recognizes narrow source-owned retention comments for one following C# syntax subject.</summary>
internal static class ModernizationSourceExceptions
{
    private static readonly HashSet<string> Rules =
    [
        "modernization.var-style", "modernization.expression-body", "modernization.collection-expression",
        "modernization.primary-constructor", "modernization.switch-expression", "modernization.conditional",
        "modernization.object-initializer", "modernization.readonly-field", "modernization.inline-declaration"
    ];
    private static readonly Regex Syntax = new("^//\\s*hygiene:retain\\s+(modernization\\.[a-z0-9.-]+)\\s+--\\s*(.*)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static IReadOnlyList<ModernizationRetention> Parse(string path, string source)
    {
        SourceText text = SourceText.From(source);
        SyntaxNode root = CSharpSyntaxTree.ParseText(text, path: path).GetRoot();
        var results = new List<ModernizationRetention>();
        foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true).Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)))
        {
            string comment = trivia.ToString();
            if (!comment.Contains("hygiene:retain", StringComparison.Ordinal)) { continue; }
            Match match = Syntax.Match(comment);
            LinePositionSpan commentLine = text.Lines.GetLinePositionSpan(trivia.Span);
            if (!match.Success)
            {
                results.Add(new(path, "", "", commentLine.Start.Line + 1, default, false));
                continue;
            }

            string ruleId = match.Groups[1].Value;
            string reason = match.Groups[2].Value.Trim();
            int nextLine = commentLine.End.Line + 1;
            while (nextLine < text.Lines.Count && (string.IsNullOrWhiteSpace(text.Lines[nextLine].ToString())
                || text.Lines[nextLine].ToString().TrimStart().StartsWith("//", StringComparison.Ordinal))) { nextLine++; }
            if (nextLine >= text.Lines.Count)
            {
                results.Add(new(path, ruleId, reason, commentLine.Start.Line + 1, default, false));
                continue;
            }

            int nextPosition = text.Lines[nextLine].Start;
            SyntaxNode? subject = root.DescendantNodes().Where(node => node is StatementSyntax or MemberDeclarationSyntax)
                .Where(node => text.Lines.GetLineFromPosition(node.SpanStart).LineNumber == nextLine)
                .OrderBy(node => node.Span.Length).FirstOrDefault();
            bool valid = Rules.Contains(ruleId) && IsMeaningful(reason) && subject is not null;
            results.Add(new(path, ruleId, reason, commentLine.Start.Line + 1, subject?.Span ?? new TextSpan(nextPosition, 0), valid));
        }
        results.AddRange(ParsePragmas(text, path));
        return results;
    }

    private static IEnumerable<ModernizationRetention> ParsePragmas(SourceText text, string path)
    {
        var results = new List<ModernizationRetention>();
        var disabled = new Dictionary<string, (int Line, string Reason, bool Valid, string RuleId)>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < text.Lines.Count; index++)
        {
            string line = text.Lines[index].ToString();
            Match disable = Regex.Match(line, "^\\s*#pragma\\s+warning\\s+disable\\s+([^/]+?)(?:\\s*//\\s*(.*))?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            Match restore = Regex.Match(line, "^\\s*#pragma\\s+warning\\s+restore\\s+([^/]+?)\\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (disable.Success)
            {
                string[] ids = disable.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (!ids.Any(id => id.StartsWith("IDE", StringComparison.OrdinalIgnoreCase))) { continue; }
                string diagnostic = ids.Length == 1 ? ids[0].ToUpperInvariant() : "IDE-multiple";
                string reason = disable.Groups[2].Success ? disable.Groups[2].Value.Trim() : index > 0 ? text.Lines[index - 1].ToString().Trim().TrimStart('/', ' ', '\t') : "";
                string ruleId = PragmaRule(diagnostic);
                bool valid = ids.Length == 1 && ruleId.Length > 0 && IsMeaningful(reason) && !disabled.ContainsKey(diagnostic);
                if (!disabled.TryAdd(diagnostic, (index, reason, valid, ruleId)))
                {
                    results.Add(new(path, ruleId.Length > 0 ? ruleId : "sdk." + diagnostic, reason, index + 1, default, false));
                }
                if (!valid) { results.Add(new(path, ruleId.Length > 0 ? ruleId : "sdk." + diagnostic, reason, index + 1, default, false)); }
            }
            else if (restore.Success)
            {
                string[] ids = restore.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (!ids.Any(id => id.StartsWith("IDE", StringComparison.OrdinalIgnoreCase))) { continue; }
                string diagnostic = ids.Length == 1 ? ids[0].ToUpperInvariant() : "IDE-multiple";
                if (ids.Length != 1 || !disabled.Remove(diagnostic, out var scope) || index - scope.Line > 12)
                {
                    results.Add(new(path, PragmaRule(diagnostic) is string rule && rule.Length > 0 ? rule : "sdk." + diagnostic, "", index + 1, default, false));
                }
                else if (scope.Valid)
                {
                    int spanStart = text.Lines[scope.Line].End;
                    int spanEnd = text.Lines[index].Start;
                    results.Add(new(path, scope.RuleId, scope.Reason, scope.Line + 1,
                        TextSpan.FromBounds(spanStart, Math.Max(spanStart, spanEnd)), true));
                }
            }
        }
        foreach (var remaining in disabled.Values)
        {
            results.Add(new(path, remaining.RuleId.Length > 0 ? remaining.RuleId : "sdk.IDE", "", remaining.Line + 1, default, false));
        }
        return results;
    }

    private static string PragmaRule(string diagnostic) => diagnostic switch
    {
        "IDE0090" => "sdk.IDE0090",
        "IDE0007" or "IDE0008" => "modernization.var-style",
        "IDE0021" or "IDE0022" or "IDE0023" or "IDE0024" or "IDE0025" or "IDE0026" or "IDE0027" => "modernization.expression-body",
        "IDE0290" => "modernization.primary-constructor",
        "IDE0066" => "modernization.switch-expression",
        "IDE0045" or "IDE0046" => "modernization.conditional",
        "IDE0017" => "modernization.object-initializer",
        "IDE0044" => "modernization.readonly-field",
        "IDE0018" => "modernization.inline-declaration",
        _ => ""
    };

    internal static ReviewOccurrence[] Filter(string ruleId, ReviewOccurrence[] occurrences, IReadOnlyDictionary<string, string> sources,
        bool exhaustiveAudit, out int suppressedCount)
    {
        suppressedCount = 0;
        var retained = new List<ReviewOccurrence>(occurrences.Length);
        foreach (ReviewOccurrence occurrence in occurrences)
        {
            if (!sources.TryGetValue(occurrence.Path, out string? source))
            {
                retained.Add(occurrence);
                continue;
            }
            ModernizationRetention? exception = Parse(occurrence.Path, source).Where(item => item.Valid && item.RuleId == ruleId)
                .FirstOrDefault(item => CoversLine(source, item.SubjectSpan, occurrence.Line));
            if (exception is null)
            {
                retained.Add(occurrence);
                continue;
            }
            suppressedCount++;
            if (exhaustiveAudit) { retained.Add(occurrence with { SourceExceptionReason = exception.Reason }); }
        }
        return retained.ToArray();
    }

    internal static bool IsKnownRule(string ruleId) => Rules.Contains(ruleId);
    internal static bool IsMeaningful(string reason) => reason.Length >= 20
        && !Regex.IsMatch(reason, "^(keep|retain|leave|unchanged|no reason|n/a|todo)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        && reason.Any(char.IsLetter);

    private static bool CoversLine(string source, TextSpan span, int oneBasedLine)
    {
        SourceText text = SourceText.From(source);
        if (oneBasedLine <= 0 || oneBasedLine > text.Lines.Count || span.Length == 0) { return false; }
        int start = text.Lines.GetLineFromPosition(span.Start).LineNumber + 1;
        int end = text.Lines.GetLineFromPosition(Math.Max(span.Start, span.End - 1)).LineNumber + 1;
        return oneBasedLine >= start && oneBasedLine <= end;
    }
}
