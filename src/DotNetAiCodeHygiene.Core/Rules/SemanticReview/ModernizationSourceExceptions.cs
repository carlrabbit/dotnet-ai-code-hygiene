using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed record ModernizationRetention(string Path, string RuleId, string Reason, int CommentLine, TextSpan SubjectSpan, bool Valid, bool IsNative = false);

internal sealed class ModernizationSourceExceptionRuleModule : IRuleModule
{
    private static readonly Rule DescriptorValue = new("modernization.source-exception", 1, "finding", "finding", "Validate narrow source-owned modernization retention annotations.", false);
    public Rule Descriptor => DescriptorValue;
    public RuleModuleResult Evaluate(RuleContext context)
    {
        var findings = new List<Finding>();
        var parsedByDocument = new Dictionary<Document, (string Path, string Source, IReadOnlyList<ModernizationRetention> Exceptions)>();
        foreach (Document document in context.ReportingDocuments)
        {
            string path = context.RelativePath(document);
            string source = context.Text(document).ToString();
            if (!ModernizationSourceExceptions.ContainsAnnotation(source)) { continue; }
            parsedByDocument[document] = (path, source, context.SourceExceptions(path, source));
        }
        if (parsedByDocument.Count == 0) { return RuleModuleResult.Empty; }

        ModernizationRetention[] retentions = parsedByDocument.Values.SelectMany(value => value.Exceptions).ToArray();
        HashSet<(string RuleId, string Path, int Line)> candidates = GetCandidateLinesForExceptions(context,
            retentions.Where(retention => retention.Valid && ModernizationSourceExceptions.IsKnownRule(retention.RuleId)).ToArray());
        HashSet<(string Path, int Line)> ineffectiveNativeSuppressions = GetIneffectiveNativeSuppressions(context,
            parsedByDocument.Keys.ToArray(), retentions.Where(retention => retention.Valid && retention.IsNative).ToArray());

        foreach ((Document _, (string path, string source, IReadOnlyList<ModernizationRetention> exceptions)) in parsedByDocument)
        {
            foreach (ModernizationRetention retention in exceptions)
            {
                bool stale = retention.Valid && (retention.IsNative && retention.RuleId.StartsWith("sdk.", StringComparison.Ordinal)
                    ? ineffectiveNativeSuppressions.Contains((path, retention.CommentLine))
                    : !candidates.Any(candidate => candidate.RuleId == retention.RuleId && candidate.Path == path
                        && CoversLine(source, retention.SubjectSpan, candidate.Line)));
                if (retention.Valid && !stale) { continue; }
                string message = stale
                    ? retention.IsNative ? "Ineffective or stale native IDE suppression: the SDK reports that this warning suppression is unnecessary."
                        : "Stale modernization retention comment: no matching eligible candidate remains in its attached syntax subject."
                    : "Malformed, unknown-rule, unjustified, or unattached modernization retention comment or native pragma.";
                findings.Add(new Finding("", "", Descriptor.Id, Descriptor.Version, Descriptor.Classification, path, retention.CommentLine, 1,
                    retention.RuleId, message, "Use // hygiene:retain modernization.<known-rule> -- <concrete local reason> immediately before one declaration or statement.",
                    "Source-local modernization retention annotations must be narrow and justified.", message, "The comment must name one enabled modernization rule, attach to one source syntax subject, and explain a concrete local constraint.",
                    "source-exception:" + retention.CommentLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(source + "\0" + retention.CommentLine)))));
            }
        }
        return RuleModuleResult.FindingsOnly(findings);
    }

    private static HashSet<(string RuleId, string Path, int Line)> GetCandidateLinesForExceptions(RuleContext context, IReadOnlyList<ModernizationRetention> retentions)
    {
        var result = new HashSet<(string, string, int)>();
        var targets = retentions.GroupBy(retention => (retention.RuleId, retention.Path));
        foreach (IGrouping<(string RuleId, string Path), ModernizationRetention> target in targets)
        {
            ModernizationReviewRuleModule? module = ModernizationReviewRuleModule.All.OfType<ModernizationReviewRuleModule>()
                .FirstOrDefault(candidate => candidate.Descriptor.Id == target.Key.RuleId);
            if (module is null) { continue; }
            foreach (Project project in context.Session.Projects)
            {
                string fullTarget = Path.GetFullPath(Path.Combine(context.Session.Root, target.Key.Path));
                if (!project.Documents.Any(document => string.Equals(document.FilePath, fullTarget, StringComparison.OrdinalIgnoreCase))) { continue; }
                Project analysisProject = ModernizationSourceExceptions.WithoutNativePragmas(project, target.Key.RuleId);
                string cacheKey = "modernization-candidates\0" + project.Id + "\0" + target.Key.RuleId + "\0" + analysisProject.Version + "\0" + bool.TrueString + "\0" + fullTarget;
                IReadOnlyList<ModernizationCandidateGroup> groups = context.Session.GetFact(cacheKey,
                    () => module.BuildCandidatesForPath(analysisProject, analysisProject == project
                        ? context.Session.GetCompilation(project)
                        : analysisProject.GetCompilationAsync().GetAwaiter().GetResult()
                            ?? throw new ProductException($"Could not compile source-exception subject project '{project.Name}'."), fullTarget));
                foreach (ModernizationCandidateGroup group in groups)
                {
                    string projectDirectory = Path.GetDirectoryName(project.FilePath!)!;
                    foreach (ReviewOccurrence occurrence in group.Occurrences)
                    {
                        string fullPath = Path.GetFullPath(Path.IsPathRooted(occurrence.Path) ? occurrence.Path : Path.Combine(projectDirectory, occurrence.Path));
                        string path = context.Session.Relative(fullPath);
                        if (path == target.Key.Path) { result.Add((target.Key.RuleId, path, occurrence.Line)); }
                    }
                }
            }
        }
        return result;
    }

    private static HashSet<(string Path, int Line)> GetIneffectiveNativeSuppressions(RuleContext context,
        IReadOnlyList<Document> documents, IReadOnlyList<ModernizationRetention> retentions)
    {
        var result = new HashSet<(string, int)>();
        ModernizationRetention[] sdkSuppressions = retentions.Where(retention => retention.RuleId.StartsWith("sdk.", StringComparison.Ordinal)).ToArray();
        if (sdkSuppressions.Length == 0) { return result; }
        foreach (IGrouping<Project, Document> projectGroup in documents.GroupBy(document => document.Project))
        {
            string[] projectPaths = projectGroup.Select(context.RelativePath).ToArray();
            ModernizationRetention[] projectSuppressions = sdkSuppressions.Where(retention => projectPaths.Contains(retention.Path, StringComparer.Ordinal)).ToArray();
            if (projectSuppressions.Length == 0) { continue; }
            string cacheKey = "source-exception-sdk-candidates\0" + projectGroup.Key.Id;
            ImmutableArray<Diagnostic> diagnostics = context.Session.GetFact(cacheKey, () =>
            {
                Compilation compilation = context.Session.GetCompilation(projectGroup.Key);
                Compilation unsuppressed = compilation;
                foreach (Document document in projectGroup.Key.Documents)
                {
                    SyntaxTree? originalTree = compilation.SyntaxTrees.FirstOrDefault(tree =>
                        string.Equals(Path.GetFullPath(tree.FilePath), Path.GetFullPath(document.FilePath!), StringComparison.OrdinalIgnoreCase));
                    if (originalTree is null) { continue; }
                    string source = document.GetTextAsync().GetAwaiter().GetResult().ToString();
                    string withoutIde0090Pragmas = Regex.Replace(source,
                        "(?m)^\\s*#pragma\\s+warning\\s+(?:disable|restore)\\s+IDE0090(?:\\s*//[^\\r\\n]*)?\\s*$", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (string.Equals(source, withoutIde0090Pragmas, StringComparison.Ordinal)) { continue; }
                    var replacement = CSharpSyntaxTree.ParseText(SourceText.From(withoutIde0090Pragmas), (CSharpParseOptions)originalTree.Options, originalTree.FilePath);
                    unsuppressed = unsuppressed.ReplaceSyntaxTree(originalTree, replacement);
                }
                return RoslynCodeStyleAnalyzers.Analyze(unsuppressed, projectGroup.Key.AnalyzerOptions, "IDE0090");
            });
            foreach (ModernizationRetention suppression in projectSuppressions)
            {
                string fullPath = Path.GetFullPath(Path.Combine(context.Session.Root, suppression.Path));
                string source = context.Text(projectGroup.First(document => context.RelativePath(document) == suppression.Path)).ToString();
                bool hasCandidate = diagnostics.Any(diagnostic => diagnostic.Id == "IDE0090" && diagnostic.Location.SourceTree is not null
                    && string.Equals(Path.GetFullPath(diagnostic.Location.SourceTree.FilePath), fullPath, StringComparison.OrdinalIgnoreCase)
                    && CoversLine(source, suppression.SubjectSpan, diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1));
                if (!hasCandidate) { result.Add((suppression.Path, suppression.CommentLine)); }
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
                    results.Add(new(path, ruleId.Length > 0 ? ruleId : "sdk." + diagnostic, reason, index + 1, default, false, IsNative: true));
                }
                if (!valid) { results.Add(new(path, ruleId.Length > 0 ? ruleId : "sdk." + diagnostic, reason, index + 1, default, false, IsNative: true)); }
            }
            else if (restore.Success)
            {
                string[] ids = restore.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (!ids.Any(id => id.StartsWith("IDE", StringComparison.OrdinalIgnoreCase))) { continue; }
                string diagnostic = ids.Length == 1 ? ids[0].ToUpperInvariant() : "IDE-multiple";
                if (ids.Length != 1 || !disabled.Remove(diagnostic, out var scope) || index - scope.Line > 12)
                {
                    results.Add(new(path, PragmaRule(diagnostic) is string rule && rule.Length > 0 ? rule : "sdk." + diagnostic, "", index + 1, default, false, IsNative: true));
                }
                else if (scope.Valid)
                {
                    int spanStart = text.Lines[scope.Line].End;
                    int spanEnd = text.Lines[index].Start;
                    results.Add(new(path, scope.RuleId, scope.Reason, scope.Line + 1,
                        TextSpan.FromBounds(spanStart, Math.Max(spanStart, spanEnd)), true, IsNative: true));
                }
            }
        }
        foreach (var remaining in disabled.Values)
        {
            results.Add(new(path, remaining.RuleId.Length > 0 ? remaining.RuleId : "sdk.IDE", "", remaining.Line + 1, default, false, IsNative: true));
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
        RuleContext context, bool exhaustiveAudit, out int suppressedCount)
        => Filter(ruleId, occurrences, sources, context.SourceExceptions, exhaustiveAudit, out suppressedCount);

    // Parser-level fixture entry point; command execution uses the RuleContext cache above.
    internal static ReviewOccurrence[] Filter(string ruleId, ReviewOccurrence[] occurrences, IReadOnlyDictionary<string, string> sources,
        bool exhaustiveAudit, out int suppressedCount)
        => Filter(ruleId, occurrences, sources, Parse, exhaustiveAudit, out suppressedCount);

    private static ReviewOccurrence[] Filter(string ruleId, ReviewOccurrence[] occurrences, IReadOnlyDictionary<string, string> sources,
        Func<string, string, IReadOnlyList<ModernizationRetention>> parse, bool exhaustiveAudit, out int suppressedCount)
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
            if (!ContainsAnnotation(source))
            {
                retained.Add(occurrence);
                continue;
            }
            ModernizationRetention? exception = parse(occurrence.Path, source).Where(item => item.Valid && item.RuleId == ruleId)
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
    internal static bool ContainsAnnotation(string source) => source.Contains("hygiene:retain", StringComparison.Ordinal)
        || source.Contains("#pragma warning", StringComparison.Ordinal);
    internal static bool IsMeaningful(string reason) => reason.Length >= 20
        && !Regex.IsMatch(reason, "^(keep|retain|leave|unchanged|no reason|n/a|todo)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        && reason.Any(char.IsLetter);

    internal static Project WithoutNativePragmas(Project project, string ruleId)
    {
        string[] diagnostics = ruleId switch
        {
            "modernization.var-style" => ["IDE0007", "IDE0008"],
            "modernization.expression-body" => ["IDE0021", "IDE0022", "IDE0023", "IDE0024", "IDE0025", "IDE0026", "IDE0027"],
            "modernization.primary-constructor" => ["IDE0290"],
            "modernization.switch-expression" => ["IDE0066"],
            "modernization.conditional" => ["IDE0045", "IDE0046"],
            "modernization.object-initializer" => ["IDE0017"],
            "modernization.readonly-field" => ["IDE0044"],
            "modernization.inline-declaration" => ["IDE0018"],
            _ => []
        };
        if (diagnostics.Length == 0) { return project; }
        string idPattern = string.Join("|", diagnostics);
        Regex pragma = new($"(?m)^\\s*#pragma\\s+warning\\s+(?:disable|restore)\\s+(?:{idPattern})(?:\\s*//[^\\r\\n]*)?\\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Project result = project;
        foreach (Document document in project.Documents)
        {
            SourceText source = document.GetTextAsync().GetAwaiter().GetResult();
            string text = source.ToString();
            string unsuppressed = pragma.Replace(text, "");
            if (!string.Equals(text, unsuppressed, StringComparison.Ordinal))
            {
                result = result.Solution.WithDocumentText(document.Id, SourceText.From(unsuppressed, source.Encoding))
                    .GetProject(project.Id) ?? throw new ProductException($"Could not create source-exception analysis view for project '{project.Name}'.");
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
