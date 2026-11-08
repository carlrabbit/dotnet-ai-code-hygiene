using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

internal sealed record ModernizationRetention(string Path, string RuleId, string Reason, int CommentLine, TextSpan SubjectSpan, bool Valid,
    bool IsNative = false, string? NativeDiagnosticId = null);

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
            parsedByDocument[document] = (path, source, context.SourceExceptions(document));
        }
        if (parsedByDocument.Count == 0) { return RuleModuleResult.Empty; }

        ModernizationRetention[] retentions = parsedByDocument.Values.SelectMany(value => value.Exceptions).ToArray();
        HashSet<ExceptionCandidate> candidates = GetCandidatesForExceptions(context,
            retentions.Where(retention => retention.Valid && ModernizationSourceExceptions.IsKnownRule(retention.RuleId)).ToArray());
        HashSet<(string Path, int CommentLine)> ineffectiveNativeSuppressions = GetIneffectiveNativeSuppressions(context,
            parsedByDocument.Keys.ToArray(), retentions.Where(retention => retention.Valid && retention.IsNative).ToArray());

        foreach ((Document _, (string path, string source, IReadOnlyList<ModernizationRetention> exceptions)) in parsedByDocument)
        {
            foreach (ModernizationRetention retention in exceptions)
            {
                bool stale = retention.Valid && (retention.IsNative && retention.RuleId.StartsWith("sdk.", StringComparison.Ordinal)
                    ? ineffectiveNativeSuppressions.Contains((path, retention.CommentLine))
                    : !candidates.Any(candidate => candidate.RuleId == retention.RuleId && candidate.Path == path
                        && (retention.IsNative ? candidate.DiagnosticId == retention.NativeDiagnosticId : true)
                        && retention.SubjectSpan.Contains(candidate.Span)));
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

    private sealed record ExceptionCandidate(string RuleId, string Path, string DiagnosticId, TextSpan Span);

    private static HashSet<ExceptionCandidate> GetCandidatesForExceptions(RuleContext context, IReadOnlyList<ModernizationRetention> retentions)
    {
        var result = new HashSet<ExceptionCandidate>();
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
                        if (path == target.Key.Path && occurrence.CandidateSpan is TextSpan span)
                        {
                            result.Add(new ExceptionCandidate(target.Key.RuleId, path, occurrence.DiagnosticId, span));
                        }
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
                    SourceText source = document.GetTextAsync().GetAwaiter().GetResult();
                    SourceText withoutPragmas = ModernizationSourceExceptions.RemoveNativePragmaDirectives(originalTree, source, ["IDE0090"]);
                    if (source.ContentEquals(withoutPragmas)) { continue; }
                    var replacement = CSharpSyntaxTree.ParseText(withoutPragmas, (CSharpParseOptions)originalTree.Options, originalTree.FilePath);
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
                    && suppression.SubjectSpan.Contains(diagnostic.Location.SourceSpan));
                if (!hasCandidate) { result.Add((suppression.Path, suppression.CommentLine)); }
            }
        }
        return result;
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

    internal static IReadOnlyList<ModernizationRetention> Parse(string path, string source, CSharpParseOptions? parseOptions = null)
    {
        SourceText text = SourceText.From(source);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(text, parseOptions, path: path);
        SyntaxNode root = tree.GetRoot();
        var results = new List<ModernizationRetention>();
        SyntaxTrivia[] comments = root.DescendantTrivia(descendIntoTrivia: false)
            .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)).ToArray();
        foreach (SyntaxTrivia trivia in comments.Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)))
        {
            string comment = trivia.ToString();
            if (!comment.Contains("hygiene:retain", StringComparison.Ordinal)) { continue; }
            Match match = Syntax.Match(comment);
            LinePositionSpan commentLine = text.Lines.GetLinePositionSpan(trivia.Span);
            TextLine physicalLine = text.Lines[commentLine.Start.Line];
            bool standalone = string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(physicalLine.Start, trivia.SpanStart)));
            if (!match.Success || !standalone)
            {
                results.Add(new(path, "", "", commentLine.Start.Line + 1, default, false));
                continue;
            }

            string ruleId = match.Groups[1].Value;
            string reason = match.Groups[2].Value.Trim();
            SyntaxToken nextToken = root.DescendantTokens(descendIntoTrivia: false)
                .FirstOrDefault(token => token.SpanStart >= trivia.Span.End);
            if (nextToken.RawKind == 0 || !OnlyWhitespaceOrComments(text, comments, trivia.Span.End, nextToken.SpanStart))
            {
                results.Add(new(path, ruleId, reason, commentLine.Start.Line + 1, default, false));
                continue;
            }
            SyntaxNode? subject = nextToken.Parent?.AncestorsAndSelf()
                .FirstOrDefault(node => node is StatementSyntax or MemberDeclarationSyntax);
            bool valid = Rules.Contains(ruleId) && IsMeaningful(reason) && subject is not null;
            results.Add(new(path, ruleId, reason, commentLine.Start.Line + 1, subject?.Span ?? default, valid));
        }
        results.AddRange(ParsePragmas(tree, text, path));
        return results;
    }

    private sealed record PendingPragma(int Line, string Reason, bool Valid, string RuleId, string DiagnosticId, TextSpan DirectiveSpan);

    private static IEnumerable<ModernizationRetention> ParsePragmas(SyntaxTree tree, SourceText text, string path)
    {
        var results = new List<ModernizationRetention>();
        var disabled = new Dictionary<string, PendingPragma>(StringComparer.OrdinalIgnoreCase);
        SyntaxNode root = tree.GetRoot();
        SyntaxTrivia[] comments = root.DescendantTrivia(descendIntoTrivia: true)
            .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)).ToArray();
        foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true)
            .Where(trivia => trivia.HasStructure && trivia.GetStructure() is PragmaWarningDirectiveTriviaSyntax))
        {
            var directive = (PragmaWarningDirectiveTriviaSyntax)trivia.GetStructure()!;
            if (!directive.IsActive) { continue; }
            bool isDisable = directive.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword);
            bool isRestore = directive.DisableOrRestoreKeyword.IsKind(SyntaxKind.RestoreKeyword);
            if (!isDisable && !isRestore) { continue; }
            string[] ids = directive.ErrorCodes.Select(code => code.ToString().ToUpperInvariant()).ToArray();
            bool hasIdeId = ids.Any(id => id.StartsWith("IDE", StringComparison.Ordinal));
            bool disableAll = isDisable && ids.Length == 0;
            if (!hasIdeId && !disableAll) { continue; }

            string key = disableAll ? "*" : ids.Length == 1 ? ids[0] : string.Join(",", ids.Order(StringComparer.Ordinal));
            string diagnosticId = disableAll ? "IDE-all" : ids.Length == 1 ? ids[0] : "IDE-multiple";
            string ruleId = ids.Length == 1 ? PragmaRule(diagnosticId) : "";
            int line = text.Lines.GetLineFromPosition(directive.SpanStart).LineNumber;
            string reason = isDisable ? GetPragmaJustification(root, text, comments, directive) : "";
            bool valid = ids.Length == 1 && ruleId.Length > 0 && IsMeaningful(reason);
            if (isDisable)
            {
                if (!disabled.TryAdd(key, new PendingPragma(line, reason, valid, ruleId, diagnosticId, directive.Span)))
                {
                    results.Add(new(path, ruleId.Length > 0 ? ruleId : "sdk." + diagnosticId, reason, line + 1, default, false, IsNative: true, NativeDiagnosticId: diagnosticId));
                }
                if (!valid) { results.Add(new(path, ruleId.Length > 0 ? ruleId : "sdk." + diagnosticId, reason, line + 1, default, false, IsNative: true, NativeDiagnosticId: diagnosticId)); }
            }
            else
            {
                if (!disabled.Remove(key, out PendingPragma? scope) || line - scope.Line > 12)
                {
                    results.Add(new(path, ruleId.Length > 0 ? ruleId : "sdk." + diagnosticId, "", line + 1, default, false, IsNative: true, NativeDiagnosticId: diagnosticId));
                }
                else if (scope.Valid)
                {
                    results.Add(new(path, scope.RuleId, scope.Reason, scope.Line + 1,
                        TextSpan.FromBounds(scope.DirectiveSpan.End, Math.Max(scope.DirectiveSpan.End, directive.SpanStart)), true,
                        IsNative: true, NativeDiagnosticId: scope.DiagnosticId));
                }
            }
        }
        foreach (PendingPragma remaining in disabled.Values)
        {
            results.Add(new(path, remaining.RuleId.Length > 0 ? remaining.RuleId : "sdk." + remaining.DiagnosticId,
                remaining.Reason, remaining.Line + 1, default, false, IsNative: true, NativeDiagnosticId: remaining.DiagnosticId));
        }
        return results;
    }

    private static string GetPragmaJustification(SyntaxNode root, SourceText text, SyntaxTrivia[] comments,
        PragmaWarningDirectiveTriviaSyntax directive)
    {
        TextLine directiveLine = text.Lines.GetLineFromPosition(directive.SpanStart);
        SyntaxTrivia? sameLine = directive.DescendantTrivia(descendIntoTrivia: true)
            .Where(comment => comment.IsKind(SyntaxKind.SingleLineCommentTrivia) || comment.IsKind(SyntaxKind.MultiLineCommentTrivia))
            .OrderBy(comment => comment.SpanStart).Cast<SyntaxTrivia?>().FirstOrDefault();
        if (sameLine is SyntaxTrivia sameLineComment) { return CommentText(sameLineComment); }

        SyntaxTrivia? preceding = comments.Where(comment => text.Lines.GetLinePositionSpan(comment.Span).End.Line == directiveLine.LineNumber - 1
                && string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(
                    text.Lines[text.Lines.GetLinePositionSpan(comment.Span).Start.Line].Start, comment.SpanStart)))
                && string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(comment.Span.End,
                    text.Lines[text.Lines.GetLinePositionSpan(comment.Span).End.Line].End)))
                && string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(
                    text.Lines[text.Lines.GetLinePositionSpan(comment.Span).End.Line].End, directiveLine.Start)))
                && root.FullSpan.Contains(comment.Span))
            .OrderByDescending(comment => comment.SpanStart).Cast<SyntaxTrivia?>().FirstOrDefault();
        return preceding is SyntaxTrivia precedingComment ? CommentText(precedingComment) : "";
    }

    private static string CommentText(SyntaxTrivia comment)
    {
        string text = comment.ToString().Trim();
        if (text.StartsWith("//", StringComparison.Ordinal)) { return text[2..].Trim(); }
        if (text.StartsWith("/*", StringComparison.Ordinal) && text.EndsWith("*/", StringComparison.Ordinal)) { return text[2..^2].Trim(); }
        return "";
    }

    private static bool OnlyWhitespaceOrComments(SourceText text, SyntaxTrivia[] comments, int start, int end)
    {
        if (end < start) { return false; }
        var commentSpans = comments.Where(comment => comment.SpanStart >= start && comment.Span.End <= end)
            .Select(comment => comment.Span).OrderBy(span => span.Start).ToArray();
        int cursor = start;
        foreach (TextSpan span in commentSpans)
        {
            if (!string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(cursor, span.Start)))) { return false; }
            cursor = span.End;
        }
        return string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(cursor, end)));
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
        => Filter(ruleId, occurrences, sources, static (path, source) => Parse(path, source), exhaustiveAudit, out suppressedCount);

    private static ReviewOccurrence[] Filter(string ruleId, ReviewOccurrence[] occurrences, IReadOnlyDictionary<string, string> sources,
        Func<string, string, IReadOnlyList<ModernizationRetention>> parse, bool exhaustiveAudit, out int suppressedCount)
    {
        suppressedCount = 0;
        var retained = new List<ReviewOccurrence>(occurrences.Length);
        var parsedByPath = new Dictionary<string, IReadOnlyList<ModernizationRetention>>(StringComparer.Ordinal);
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
            if (!parsedByPath.TryGetValue(occurrence.Path, out IReadOnlyList<ModernizationRetention>? parsed))
            {
                parsed = parse(occurrence.Path, source);
                parsedByPath.Add(occurrence.Path, parsed);
            }
            ModernizationRetention? exception = parsed.Where(item => item.Valid
                    && (item.IsNative ? item.RuleId == ruleId && item.NativeDiagnosticId == occurrence.DiagnosticId : item.RuleId == ruleId))
                .FirstOrDefault(item => occurrence.CandidateSpan is TextSpan candidateSpan && item.SubjectSpan.Contains(candidateSpan));
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
            "sdk.IDE0090" => ["IDE0090"],
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
        Project result = project;
        foreach (Document document in project.Documents)
        {
            SourceText source = document.GetTextAsync().GetAwaiter().GetResult();
            SyntaxTree? tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult();
            if (tree is null) { continue; }
            SourceText unsuppressed = RemoveNativePragmaDirectives(tree, source, diagnostics);
            if (!source.ContentEquals(unsuppressed))
            {
                result = result.Solution.WithDocumentText(document.Id, unsuppressed)
                    .GetProject(project.Id) ?? throw new ProductException($"Could not create source-exception analysis view for project '{project.Name}'.");
            }
        }
        return result;
    }

    internal static SourceText RemoveNativePragmaDirectives(SyntaxTree tree, SourceText source, IReadOnlyCollection<string> diagnosticIds)
    {
        SyntaxNode root = tree.GetRoot();
        var changes = new List<TextChange>();
        foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            if (trivia.GetStructure() is not PragmaWarningDirectiveTriviaSyntax directive || !directive.IsActive) { continue; }
            if (!directive.ErrorCodes.Any(code => diagnosticIds.Contains(code.ToString(), StringComparer.OrdinalIgnoreCase))) { continue; }
            TextLine line = source.Lines.GetLineFromPosition(directive.SpanStart);
            changes.Add(new TextChange(line.Span, new string(' ', line.Span.Length)));
        }
        return changes.Count == 0 ? source : source.WithChanges(changes);
    }
}
