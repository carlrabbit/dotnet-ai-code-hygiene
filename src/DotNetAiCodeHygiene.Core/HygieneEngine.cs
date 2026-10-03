using System.Security.Cryptography;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiCodeHygiene.Core;

public sealed record Rule(string Id, int Version, string Classification, string Purpose);
public sealed record Finding(string Id, string Handle, string RuleId, int RuleVersion, string Classification, string Path, int Line, int Column, string? Symbol, string Message, string Suggestion, string Observation, string Reason, string Constraint, string Anchor, string Fingerprint, string? Discriminator = null);
public sealed record CheckResult(int SchemaVersion, string RunId, IReadOnlyList<Finding> Findings, int IgnoredCount);
public sealed record IgnoreDecision(string Id, string RuleId, int RuleVersion, string Path, string Anchor, string Fingerprint, string Reason, DateTimeOffset CreatedAt, string? Discriminator = null);
public sealed record IgnoreView(string Id, string RuleId, string Path, string State, string Reason);
public sealed class ProductException(string message) : Exception(message);
public sealed class EnvironmentException(string message) : Exception(message);

public sealed class HygieneEngine
{
    public static readonly Rule[] Rules =
    [
        new("docs.summary.required", 1, "finding", "Require XML summaries on public and internal API symbols."),
        new("readability.long-line.review", 1, "review-candidate", "Review unusually long physical source lines."),
        new("readability.control-flow.visual-block", 1, "finding", "Separate control-flow blocks visually from preceding statements.")
    ];
    private readonly string root;
    private readonly string hygiene;
    private readonly Action? beforeAtomicReplace;
    private readonly JsonSerializerOptions json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = false };

    public HygieneEngine(string? cwd = null) : this(cwd, null) { }

    internal HygieneEngine(string? cwd, Action? beforeAtomicReplace)
    {
        this.beforeAtomicReplace = beforeAtomicReplace;
        string dir = Path.GetFullPath(cwd ?? Environment.CurrentDirectory);
        while (!Directory.Exists(Path.Combine(dir, ".git")) && !File.Exists(Path.Combine(dir, ".git")))
        {
            string? parent = Directory.GetParent(dir)?.FullName;
            if (parent is null) throw new ProductException("Current directory is not inside a Git repository.");
            dir = parent;
        }
        root = dir; hygiene = Path.Combine(root, ".hygiene");
    }
    public string Root => root;

    private T Read<T>(string path, T fallback)
    {
        if (!File.Exists(path)) return fallback;
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(path), json) ?? throw new JsonException(); }
        catch (Exception e) when (e is JsonException or IOException) { throw new ProductException($"Invalid state file '{Path.GetRelativePath(root, path)}'."); }
    }
    private static string? Snapshot(string path) => File.Exists(path) ? File.ReadAllText(path) : null;
    private void WriteAtomic(string path, string content, string? expected = null, bool verifyExpected = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string? original = verifyExpected ? expected : Snapshot(path);
        if (Snapshot(path) != original) throw new ProductException("State changed concurrently; retry the command.");
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, content, new UTF8Encoding(false));
            beforeAtomicReplace?.Invoke();
            if (Snapshot(path) != original) throw new ProductException("State changed concurrently; retry the command.");
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private string ConfigPath => Path.Combine(hygiene, "config.json");
    private string DecisionsPath => Path.Combine(hygiene, "decisions.json");
    private string LatestPath => Path.Combine(hygiene, ".state", "latest-run.json");
    private string[] Disabled
    {
        get
        {
            Config value = Read(ConfigPath, new Config(1, []));
            if (value.SchemaVersion != 1 || value.DisabledRules is null || value.DisabledRules.Any(id => !Rules.Any(r => r.Id == id)) || value.DisabledRules.Distinct(StringComparer.Ordinal).Count() != value.DisabledRules.Length)
                throw new ProductException("Invalid or unsupported .hygiene/config.json.");
            return value.DisabledRules;
        }
    }
    private DecisionFile ReadDecisions()
    {
        DecisionFile value = Read(DecisionsPath, new DecisionFile(1, []));
        if (value.SchemaVersion != 1 || value.Decisions is null || value.Decisions.Any(d => d is null || string.IsNullOrEmpty(d.Id) || d.Id.Length < 3 || !d.Id.StartsWith("I-", StringComparison.Ordinal) || !Rules.Any(r => r.Id == d.RuleId) || d.RuleVersion < 1 || string.IsNullOrEmpty(d.Fingerprint) || d.Fingerprint.Length != 64) || value.Decisions.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != value.Decisions.Length)
            throw new ProductException("Invalid or unsupported .hygiene/decisions.json.");
        return value;
    }
    private sealed record Config(int SchemaVersion, string[] DisabledRules);
    private sealed record RunSnapshot(int SchemaVersion, string RunId, Finding[] Findings, int IgnoredCount);
    private sealed record DecisionFile(int SchemaVersion, IgnoreDecision[] Decisions);

    public void SetRule(string id, bool enabled)
    {
        if (!Rules.Any(r => r.Id == id)) throw new ProductException($"Unknown rule ID '{id}'.");
        string? original = Snapshot(ConfigPath);
        string[] current = Disabled;
        string[] next = enabled ? current.Where(x => x != id).ToArray() : current.Append(id).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        WriteAtomic(ConfigPath, JsonSerializer.Serialize(new Config(1, next), json), original, true);
    }
    public IReadOnlyList<(Rule Rule, bool Enabled)> ListRules() => Rules.Select(r => (r, !Disabled.Contains(r.Id, StringComparer.Ordinal))).ToArray();

    private string[] ResolveTargets(string[] paths, bool changed)
    {
        if (changed && paths.Length > 0) throw new ArgumentException("Explicit paths and --changed are mutually exclusive.");
        IEnumerable<string> files;
        if (changed)
        {
            try { files = GitChanged().ToArray(); }
            catch (Exception e) when (e is Win32Exception or IOException) { throw new EnvironmentException("Git is unavailable."); }
        }
        else if (paths.Length == 0) files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories);
        else
        {
            var chosen = new List<string>();
            foreach (string raw in paths)
            {
                string full = Path.GetFullPath(Path.IsPathRooted(raw) ? raw : Path.Combine(root, raw));
                if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && full != root) throw new ProductException("Target is outside the repository.");
                if (File.Exists(full))
                {
                    if (!full.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) throw new ProductException($"Unsupported explicit file: {raw}");
                    chosen.Add(full);
                }
                else if (Directory.Exists(full)) chosen.AddRange(Directory.EnumerateFiles(full, "*.cs", SearchOption.AllDirectories));
                else throw new ProductException($"Target does not exist: {raw}");
            }
            files = chosen;
        }
        return files.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).Where(File.Exists).Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !Excluded(p)).Order(StringComparer.Ordinal).ToArray();
    }
    private bool Excluded(string p) => Path.GetRelativePath(root, p).Split(Path.DirectorySeparatorChar).Any(x => x is ".git" or ".hygiene" or "bin" or "obj");
    private IEnumerable<string> GitChanged()
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        psi.ArgumentList.Add("status"); psi.ArgumentList.Add("--porcelain"); psi.ArgumentList.Add("-z"); psi.ArgumentList.Add("--untracked-files=all");
        using var p = System.Diagnostics.Process.Start(psi) ?? throw new IOException(); string output = p.StandardOutput.ReadToEnd(); p.WaitForExit(); if (p.ExitCode != 0) throw new IOException();
        foreach (string entry in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            if (entry.Length < 4 || entry.StartsWith(" D", StringComparison.Ordinal) || entry.StartsWith("D ", StringComparison.Ordinal)) continue;
            string rel = entry[3..]; if (rel.Contains(" -> ")) rel = rel[(rel.LastIndexOf(" -> ", StringComparison.Ordinal) + 4)..]; yield return Path.Combine(root, rel);
        }
    }
    private static string? FindSdkProject(string file)
    {
        for (string? dir = Path.GetDirectoryName(file); dir is not null; dir = Directory.GetParent(dir)?.FullName)
        foreach (string project in Directory.EnumerateFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly))
            if (File.ReadAllText(project).Contains("Sdk=\"", StringComparison.OrdinalIgnoreCase) || File.ReadAllText(project).Contains("<Sdk>", StringComparison.OrdinalIgnoreCase)) return project;
        return null;
    }

    public CheckResult Check(string[] paths, bool changed, bool applyIgnores = true, bool includeDisabled = false, bool publishLatest = true)
    {
        string[] targets = ResolveTargets(paths, changed);
        var projectsByTarget = targets.ToDictionary(path => path, path => FindSdkProject(path), StringComparer.OrdinalIgnoreCase);
        foreach ((string path, string? project) in projectsByTarget) if (project is null) throw new ProductException($"C# file is not associated with a discoverable SDK-style project: {Path.GetRelativePath(root, path)}");
        string[] disabled = includeDisabled ? [] : Disabled;
        IgnoreDecision[] decisions = ReadDecisions().Decisions;
        var candidates = new List<Finding>();
        string[] references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "").Split(Path.PathSeparator);
        foreach (var projectGroup in projectsByTarget.GroupBy(item => item.Value!, StringComparer.OrdinalIgnoreCase))
        {
            string projectPath = projectGroup.Key;
            string projectRoot = Path.GetDirectoryName(projectPath)!;
            string[] contextPaths = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories).Where(p => !Excluded(p)).Order(StringComparer.Ordinal).ToArray();
            var trees = contextPaths.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.Preview), path)).ToArray();
            var targetSet = projectGroup.Select(item => item.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var targetTrees = trees.Where(t => targetSet.Contains(Path.GetFullPath(t.FilePath))).ToArray();
            var refs = references.Select(p => MetadataReference.CreateFromFile(p));
            var compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(projectPath), trees, refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            foreach (SyntaxTree tree in targetTrees)
            {
            string path = tree.FilePath;
            string text = File.ReadAllText(path); string rel = Path.GetRelativePath(root, path).Replace('\\', '/');
            var model = compilation.GetSemanticModel(tree);
            var occurrenceCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            SyntaxNode syntaxRoot = tree.GetRoot();
            foreach (SyntaxNode node in syntaxRoot.DescendantNodes())
            {
                if (!disabled.Contains(Rules[0].Id) && node is MemberDeclarationSyntax member && model.GetDeclaredSymbol(member) is ISymbol symbol && (symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal) && !symbol.IsImplicitlyDeclared && SummaryEligible(symbol) && !HasSummary(member))
                {
                    string anchor = symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString();
                    candidates.Add(Make(Rules[0], rel, tree, member.GetLocation().SourceSpan.Start, symbol.ToDisplayString(), "Public or internal symbol has no non-empty XML summary.", "Add a concise XML <summary> that describes the symbol's purpose.", "The declaration has no summary text.", "This rule requires documentation, not a particular wording or language.", anchor, "missing-summary:" + anchor));
                }
            }
            if (!disabled.Contains(Rules[1].Id))
            {
                SourceTextLines(text, (line, number, start) =>
                {
                    if (line.Length > 200)
                    {
                        SyntaxNode? containing = syntaxRoot.FindNode(new Microsoft.CodeAnalysis.Text.TextSpan(start, line.Length), getInnermostNodeForTie: false);
                        SyntaxNode? anchorNode = containing?.AncestorsAndSelf().FirstOrDefault(n => n is MemberDeclarationSyntax or BaseTypeDeclarationSyntax);
                        string anchor = anchorNode is not null && model.GetDeclaredSymbol(anchorNode) is ISymbol s ? s.GetDocumentationCommentId() ?? s.ToDisplayString() : rel;
                        string canonical = string.Join(" ", syntaxRoot.DescendantTokens(new Microsoft.CodeAnalysis.Text.TextSpan(start, line.Length)).Select(t => t.ToString()));
                        string discriminator = NextDiscriminator(occurrenceCounts, Rules[1], anchor, canonical);
                        candidates.Add(Make(Rules[1], rel, tree, start, anchorNode is not null && model.GetDeclaredSymbol(anchorNode) is ISymbol symbol ? symbol.ToDisplayString() : null, "Physical line exceeds 200 characters.", "Review whether the line hides multiple concepts or structures that should be made visible or named.", "The physical line exceeds 200 UTF-16 code units.", "Do not split mechanically merely to satisfy a line-length limit.", anchor, canonical + "\0" + discriminator, discriminator));
                    }
                });
            }
            if (!disabled.Contains(Rules[2].Id))
            {
                foreach (BlockSyntax block in syntaxRoot.DescendantNodes().OfType<BlockSyntax>())
                {
                    StatementSyntax[] statements = block.Statements.ToArray();
                    for (int i = 1; i < statements.Length; i++)
                    {
                        StatementSyntax current = statements[i], previous = statements[i - 1];
                        if (!IsControl(current) || IsControl(previous) || previous is LocalFunctionStatementSyntax) continue;
                        int prevEnd = previous.Span.End;
                        SyntaxTriviaList leading = current.GetLeadingTrivia();
                        SyntaxTrivia[] comments = leading.Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia) || t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).ToArray();
                        int boundary = comments.Length > 0 ? comments[0].SpanStart : current.SpanStart;
                        string between = text[Math.Min(prevEnd, text.Length)..Math.Clamp(boundary, prevEnd, text.Length)];
                        string[] boundaryLines = between.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
                        if (boundaryLines.Length > 2 && boundaryLines.Skip(1).Take(boundaryLines.Length - 2).Any(string.IsNullOrWhiteSpace)) continue;
                        ISymbol? container = model.GetEnclosingSymbol(current.SpanStart);
                        string anchor = container?.GetDocumentationCommentId() ?? container?.ToDisplayString() ?? rel;
                        string evidence = previous.Kind().ToString() + ":" + string.Join(" ", previous.DescendantTokens().Select(t => t.ToString())) + "|" + current.Kind() + ":" + string.Join(" ", current.DescendantTokens().Select(t => t.ToString()));
                        string discriminator = NextDiscriminator(occurrenceCounts, Rules[2], anchor, evidence);
                        candidates.Add(Make(Rules[2], rel, tree, boundary, container?.ToDisplayString(), "Control-flow statement needs a blank line after the preceding linear statement.", "Insert one completely blank line before this control-flow group.", "A control-flow statement immediately follows a linear statement without a blank line.", "Keep comments documenting the control-flow statement with that statement.", anchor, evidence + "\0" + discriminator, discriminator));
                    }
                }
            }
            }
        }
        var active = candidates.Where(c => !applyIgnores || !decisions.Any(d => Same(d, c))).OrderBy(c => Array.FindIndex(Rules, r => r.Id == c.RuleId)).ThenBy(c => c.Path, StringComparer.Ordinal).ThenBy(c => c.Line).ThenBy(c => c.Column).ThenBy(c => c.Fingerprint, StringComparer.Ordinal).ToArray();
        string run = "R-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(5))[..8];
        for (int i = 0; i < active.Length; i++) { string id = "F-" + (i + 1); active[i] = active[i] with { Id = id, Handle = run + "/" + id }; }
        int ignored = candidates.Count - active.Length;
        var result = new CheckResult(1, run, active, ignored);
        if (publishLatest) WriteAtomic(LatestPath, JsonSerializer.Serialize(new RunSnapshot(1, run, active, ignored), json));
        return result;
    }
    private static bool IsControl(StatementSyntax s) => s is IfStatementSyntax or SwitchStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax or WhileStatementSyntax or DoStatementSyntax or TryStatementSyntax or UsingStatementSyntax or LockStatementSyntax;
    private static bool Same(IgnoreDecision d, Finding f) => d.RuleId == f.RuleId && d.RuleVersion == f.RuleVersion && d.Path == f.Path && d.Anchor == f.Anchor && d.Fingerprint == f.Fingerprint && d.Discriminator == f.Discriminator;
    private static string NextDiscriminator(Dictionary<string, int> counts, Rule rule, string anchor, string evidence)
    {
        string key = rule.Id + "\0" + anchor + "\0" + evidence;
        counts.TryGetValue(key, out int occurrence);
        counts[key] = occurrence + 1;
        return "occurrence:" + occurrence;
    }
    private static bool SummaryEligible(ISymbol s) => s switch { INamedTypeSymbol => true, IPropertySymbol => true, IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.Ordinary } => true, _ => false };
    private static bool HasSummary(MemberDeclarationSyntax m)
    {
        string docs = m.GetLeadingTrivia().ToFullString();
        Match summary = Regex.Match(docs, "<summary(?:\\s[^>]*)?>(.*?)</summary>", RegexOptions.Singleline | RegexOptions.CultureInvariant);
        if (!summary.Success) return false;
        string content = Regex.Replace(summary.Groups[1].Value, "<[^>]*>", "", RegexOptions.CultureInvariant);
        content = Regex.Replace(content, @"(?m)^\s*///?\s?", "", RegexOptions.CultureInvariant);
        return content.Any(c => !char.IsWhiteSpace(c));
    }
    private static void SourceTextLines(string s, Action<string, int, int> action)
    {
        int start = 0, number = 1;
        for (int i = 0; i <= s.Length; i++)
        {
            if (i < s.Length && s[i] is not ('\r' or '\n')) continue;
            action(s[start..i], number++, start);
            if (i == s.Length) break;
            if (s[i] == '\r' && i + 1 < s.Length && s[i + 1] == '\n') i++;
            start = i + 1;
        }
    }
    private static Finding Make(Rule r, string path, SyntaxTree tree, int offset, string? symbol, string msg, string suggestion, string observation, string constraint, string anchor, string evidence, string? discriminator = null)
    {
        var pos = tree.GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(offset, 0)).StartLinePosition;
        string fp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(evidence)));
        return new Finding("", "", r.Id, r.Version, r.Classification, path, pos.Line + 1, pos.Character + 1, symbol, msg, suggestion, observation, msg, constraint, anchor, fp, discriminator);
    }

    public Finding Explain(string handle)
    {
        RunSnapshot snapshot = Read(LatestPath, new RunSnapshot(0, "", [] , 0));
        if (snapshot.SchemaVersion != 1 || string.IsNullOrWhiteSpace(snapshot.RunId) || snapshot.Findings is null || snapshot.IgnoredCount < 0 || snapshot.Findings.Any(f => f is null || string.IsNullOrEmpty(f.Id) || string.IsNullOrEmpty(f.RuleId)) || snapshot.Findings.Select(f => f.Id).Distinct(StringComparer.Ordinal).Count() != snapshot.Findings.Length) throw new ProductException("Latest run state is unavailable or malformed; run check again.");
        string id = handle;
        if (handle.Contains('/')) { string[] parts = handle.Split('/'); if (parts.Length != 2 || parts[0] != snapshot.RunId) throw new ProductException("Finding handle does not refer to the latest available run."); id = parts[1]; }
        return snapshot.Findings.SingleOrDefault(f => f.Id == id) ?? throw new ProductException($"Finding '{handle}' was not found in the latest run.");
    }
    public IgnoreDecision Ignore(string handle, string reason)
    {
        Finding reference = Explain(handle);
        CheckResult fresh = Check([reference.Path], false, false, true, false);
        Finding? current = fresh.Findings.FirstOrDefault(f => Same(new IgnoreDecision("", reference.RuleId, reference.RuleVersion, reference.Path, reference.Anchor, reference.Fingerprint, "", default, reference.Discriminator), f));
        if (current is null) throw new ProductException("Finding is stale; run check again before ignoring it.");
        string? original = Snapshot(DecisionsPath);
        DecisionFile file = ReadDecisions();
        IgnoreDecision decision = new("I-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(4)), current.RuleId, current.RuleVersion, current.Path, current.Anchor, current.Fingerprint, reason, DateTimeOffset.UtcNow, current.Discriminator);
        WriteAtomic(DecisionsPath, JsonSerializer.Serialize(new DecisionFile(1, file.Decisions.Append(decision).ToArray()), json), original, true);
        return decision;
    }
    public void Unignore(string id)
    {
        string? original = Snapshot(DecisionsPath);
        DecisionFile file = ReadDecisions();
        IgnoreDecision[] next = file.Decisions.Where(d => d.Id != id).ToArray();
        if (next.Length == file.Decisions.Length) throw new ProductException($"Ignore decision '{id}' was not found.");
        WriteAtomic(DecisionsPath, JsonSerializer.Serialize(new DecisionFile(1, next), json), original, true);
    }
    public IReadOnlyList<IgnoreView> ListIgnores(string[] paths)
    {
        DecisionFile file = ReadDecisions();
        var filters = paths.Select(p =>
        {
            string full = Path.GetFullPath(Path.IsPathRooted(p) ? p : Path.Combine(root, p));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && full != root) throw new ProductException("Ignore filter is outside the repository.");
            return (Path: Path.GetRelativePath(root, full).Replace('\\', '/').TrimEnd('/'), Directory: Directory.Exists(full));
        }).ToArray();
        var scoped = filters.Length == 0 ? file.Decisions : file.Decisions.Where(d => filters.Any(f => d.Path == f.Path || (f.Directory && d.Path.StartsWith(f.Path + "/", StringComparison.Ordinal))));
        return scoped.Select(d =>
        {
            bool active = false;
            try { active = Check([d.Path], false, false, true, false).Findings.Any(f => Same(d, f)); } catch (ProductException) { }
            return new IgnoreView(d.Id, d.RuleId, d.Path, active ? "active" : "stale", d.Reason);
        }).ToArray();
    }
}
