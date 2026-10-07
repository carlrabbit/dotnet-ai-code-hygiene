using System.Security.Cryptography;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Build.Locator;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace DotNetAiCodeHygiene.Core;

public sealed record Rule(string Id, int Version, string OutputKind, string Classification, string Purpose, bool Configurable = true);
public sealed record Finding(string Id, string Handle, string RuleId, int RuleVersion, string Classification, string Path, int Line, int Column, string? Symbol, string Message, string Suggestion, string Observation, string Reason, string Constraint, string Anchor, string Fingerprint, string? Discriminator = null);
public sealed record ReviewQuestion(string Id, string Text);
public sealed record ReviewItem(string Id, string Path, int Line, int Column, string Symbol, string Summary, string Declaration);
public sealed record ReviewEscalation(string Condition, string Command, string ReviewerClass);
public sealed record ReviewSource(string Path, string Content);
public sealed record ReviewBatch(string Id, string Handle, string RuleId, int RuleVersion, string Mode, string ReviewerClass, int PopulationCount, int SampleCount, IReadOnlyList<ReviewQuestion> Questions, ReviewEscalation Escalation, IReadOnlyList<ReviewItem> Items, string PopulationFingerprint, IReadOnlyList<ReviewItem>? PopulationItems = null, IReadOnlyList<ReviewSource>? SourceContents = null);
public sealed record ReviewRequestSource(string RunId, string BatchHandle);
public sealed record ReviewRequestRule(string Id, int Version);
public sealed record SemanticReviewRequest(int SchemaVersion, string Kind, string HandoffId, string CreatedAtUtc, ReviewRequestSource Source, ReviewRequestRule Rule, string Mode, string ReviewerClass, int PopulationCount, IReadOnlyList<ReviewQuestion> Questions, IReadOnlyList<ReviewItem> Items, IReadOnlyList<ReviewSource> Sources);
public sealed record CheckResult(int SchemaVersion, string RunId, IReadOnlyList<Finding> Findings, int IgnoredCount, IReadOnlyList<ReviewBatch> ReviewBatches);
public sealed record IgnoreDecision(string Id, string RuleId, int RuleVersion, string Path, string Anchor, string Fingerprint, string Reason, DateTimeOffset CreatedAt, string? Discriminator = null);
public sealed record IgnoreView(string Id, string RuleId, string Path, string State, string Reason);
public sealed class ProductException(string message) : Exception(message);
public sealed class EnvironmentException(string message) : Exception(message);

public sealed class HygieneEngine
{
    public static readonly Rule[] Rules = RuleCatalog.All.ToArray();
    private readonly string root;
    private readonly string hygiene;
    private readonly Action? beforeAtomicReplace;
    private readonly JsonSerializerOptions json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = false };

    public HygieneEngine(string? cwd = null) : this(cwd, null) { }

    internal HygieneEngine(string? cwd, Action? beforeAtomicReplace)
    {
        this.beforeAtomicReplace = beforeAtomicReplace;
        string dir = RepositorySession.FindRoot(cwd);
        root = dir; hygiene = Path.Combine(root, ".hygiene");
    }
    public string Root => root;
    public ProfileResult Bootstrap(string output = "text") => new ProfileManager(root).Bootstrap(output);
    public ProfileResult UpdateProfile(string output = "text") => new ProfileManager(root).Update(output);
    public void RequireProfile() => new ProfileManager(root).RequireCurrent();
    public IReadOnlyList<ProfileFinding> AnalyzeProfile() => new ProfileManager(root).Analyze();

    private T Read<T>(string path, T fallback)
    {
        if (!File.Exists(path))
        {
            return fallback;
        }

        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(path), json) ?? throw new JsonException(); }
        catch (Exception e) when (e is JsonException or IOException) { throw new ProductException($"Invalid state file '{Path.GetRelativePath(root, path)}'."); }
    }
    private static string? Snapshot(string path) => File.Exists(path) ? File.ReadAllText(path) : null;
    private void WriteAtomic(string path, string content, string? expected = null, bool verifyExpected = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string? original = verifyExpected ? expected : Snapshot(path);
        if (Snapshot(path) != original)
        {
            throw new ProductException("State changed concurrently; retry the command.");
        }

        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, content, new UTF8Encoding(false));
            beforeAtomicReplace?.Invoke();
            if (Snapshot(path) != original)
            {
                throw new ProductException("State changed concurrently; retry the command.");
            }

            if (File.Exists(path))
            {
                File.Replace(temp, path, null);
            }
            else
            {
                File.Move(temp, path);
            }
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }
    private string ConfigPath => Path.Combine(hygiene, "config.json");
    private string DecisionsPath => Path.Combine(hygiene, "decisions.json");
    private string LatestPath => Path.Combine(hygiene, ".state", "latest-run.json");
    private string[] Disabled
    {
        get
        {
            Config value = Read(ConfigPath, new Config(1, []));
            if (value.SchemaVersion != 1 || value.DisabledRules is null || value.DisabledRules.Any(id => !Rules.Any(r => r.Id == id && r.Configurable)) || value.DisabledRules.Distinct(StringComparer.Ordinal).Count() != value.DisabledRules.Length)
            {
                throw new ProductException("Invalid or unsupported .hygiene/config.json.");
            }

            return value.DisabledRules;
        }
    }
    private DecisionFile ReadDecisions()
    {
        DecisionFile value = Read(DecisionsPath, new DecisionFile(1, []));
        if (value.SchemaVersion != 1 || value.Decisions is null || value.Decisions.Any(d => d is null || string.IsNullOrEmpty(d.Id) || d.Id.Length < 3 || !d.Id.StartsWith("I-", StringComparison.Ordinal) || !Rules.Any(r => r.Id == d.RuleId && r.Configurable) || d.RuleVersion < 1 || string.IsNullOrEmpty(d.Fingerprint) || d.Fingerprint.Length != 64) || value.Decisions.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != value.Decisions.Length)
        {
            throw new ProductException("Invalid or unsupported .hygiene/decisions.json.");
        }

        return value;
    }
    private sealed record Config(int SchemaVersion, string[] DisabledRules);
    private sealed record RunSnapshot(int SchemaVersion, string RunId, Finding[] Findings, int IgnoredCount, ReviewBatch[]? ReviewBatches = null, string[]? TargetPaths = null, bool ChangedScope = false, string[]? InputPaths = null);
    private sealed record DecisionFile(int SchemaVersion, IgnoreDecision[] Decisions);

    public void SetRule(string id, bool enabled)
    {
        Rule rule = Rules.SingleOrDefault(r => r.Id == id) ?? throw new ProductException($"Unknown rule ID '{id}'.");
        if (!rule.Configurable)
        {
            throw new ProductException($"Rule '{id}' is mandatory and cannot be enabled or disabled.");
        }

        string? original = Snapshot(ConfigPath);
        string[] current = Disabled;
        string[] next = enabled ? current.Where(x => x != id).ToArray() : current.Append(id).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        WriteAtomic(ConfigPath, JsonSerializer.Serialize(new Config(1, next), json), original, true);
    }
    public IReadOnlyList<(Rule Rule, bool Enabled)> ListRules() => Rules.Select(r => (r, !r.Configurable || !Disabled.Contains(r.Id, StringComparer.Ordinal))).ToArray();

    internal string[] ResolveTargets(string[] paths, bool changed)
    {
        using var session = new RepositorySession(root);
        return session.ResolveTargets(paths, changed);
    }

    public CheckResult Check(string[] paths, bool changed, bool applyIgnores = true, bool includeDisabled = false, bool publishLatest = true)
    {
        RequireProfile();
        using var session = new RepositorySession(root);
        string[] targets = session.ResolveTargets(paths, changed);
        Dictionary<string, (Project Project, Document Document)> projectsByTarget = session.AssignTargets(targets, paths);
        string[] disabled = includeDisabled ? [] : Disabled;
        IgnoreDecision[] decisions = ReadDecisions().Decisions;
        Document[] reportingDocuments = projectsByTarget.Values.Select(value => value.Document).DistinctBy(document => document.Id).ToArray();
        var ruleContext = new RuleContext(session, reportingDocuments);
        IReadOnlyList<RuleModuleExecution> executions = RuleCatalog.Runner.Run(ruleContext, disabled.ToHashSet(StringComparer.Ordinal));
        var candidates = executions.SelectMany(execution => execution.Result.Findings).ToList();
        var active = candidates.Where(c => !applyIgnores || !decisions.Any(d => Same(d, c))).OrderBy(c => Array.FindIndex(Rules, r => r.Id == c.RuleId)).ThenBy(c => c.Path, StringComparer.Ordinal).ThenBy(c => c.Line).ThenBy(c => c.Column).ThenBy(c => c.Fingerprint, StringComparer.Ordinal).ToArray();
        string run = "R-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(5))[..8];
        for (int i = 0; i < active.Length; i++) { string id = "F-" + (i + 1); active[i] = active[i] with { Id = id, Handle = run + "/" + id }; }
        int ignored = candidates.Count - active.Length;
        ReviewBatch[] batches = RuleCatalog.SemanticReviewRunner.BuildBatches(run, executions);
        var result = new CheckResult(1, run, active, ignored, batches);
        if (publishLatest)
        {
            string[] storedTargets = targets.Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).ToArray();
            string[] storedInputs = changed ? storedTargets : paths.Select(p => Path.GetFullPath(Path.IsPathRooted(p) ? p : Path.Combine(root, p)))
                .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).ToArray();
            WriteAtomic(LatestPath, JsonSerializer.Serialize(new RunSnapshot(1, run, active, ignored, batches.Select(b => b with { SourceContents = null }).ToArray(), storedTargets, changed, storedInputs), json));
        }
        return result;
    }

    internal static readonly ReviewQuestion[] QualityQuestions =
    [
        new("Q1", "Technical correctness: Is it consistent with the declaration and relevant implementation/API context, without inventing behavior?"),
        new("Q2", "Information value: Does it add useful caller-relevant meaning rather than simply repeat/paraphrase the symbol name/type/signature? A concise summary is acceptable only when it still communicates useful purpose/domain meaning."),
        new("Q3", "Clarity and scope: Is it concise, specific, and clear enough to communicate responsibility without irrelevant implementation detail?")
    ];

    internal static readonly ReviewQuestion[] GermanQuestions =
    [ new("Q1", "German language: Is the summary natural, comprehensible German rather than awkward literal translation or merely German-looking text?") ];

    internal static ReviewBatch BuildReviewBatch(string run, Rule rule, int batchNumber, IReadOnlyList<ReviewSubject> subjects, Dictionary<string, string> sourceContents, IReadOnlyList<ReviewQuestion> questions)
    {
        var ordered = subjects.GroupBy(s => s.Identity, StringComparer.Ordinal).Select(g => g.First())
            .OrderBy(s => s.Item.Path, StringComparer.Ordinal).ThenBy(s => s.Item.Line).ThenBy(s => s.Identity, StringComparer.Ordinal)
            .Select(s => (s.Identity, s.Content, s.Item, ContentFingerprint: Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s.Content)))))
            .ToArray();
        string populationData = string.Join("\n", ordered.Select(s => s.Identity + "\0" + s.ContentFingerprint));
        string population = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rule.Id + "\0" + rule.Version + "\0" + populationData)));
        var ranked = ordered.Select(s => (Subject: s, Rank: Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rule.Id + "\0" + rule.Version + "\0" + population + "\0" + s.Identity + "\0" + s.ContentFingerprint)))))
            .OrderBy(x => x.Rank, StringComparer.Ordinal).ThenBy(x => x.Subject.Identity, StringComparer.Ordinal).Take(5).ToArray();
        ReviewItem[] all = ordered.Select((s, i) => s.Item with { Id = "RI-" + (i + 1) }).ToArray();
        var ids = all.ToDictionary(x => x.Path + "\0" + x.Line + "\0" + x.Symbol, x => x.Id, StringComparer.Ordinal);
        ReviewItem[] sample = ranked.Select(x => x.Subject.Item with { Id = ids[x.Subject.Item.Path + "\0" + x.Subject.Item.Line + "\0" + x.Subject.Item.Symbol] }).ToArray();
        ReviewSource[] sources = ordered.Select(s => s.Item.Path).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .Select(path => sourceContents.TryGetValue(path, out string? content) && content is not null ? new ReviewSource(path, content) : throw new ProductException($"Source context for '{path}' is unavailable."))
            .ToArray();
        string batchId = "B-" + batchNumber;
        return new ReviewBatch(batchId, run + "/" + batchId, rule.Id, rule.Version, "sample", "implementer", ordered.Length, sample.Length, questions,
            new ReviewEscalation("Expand if any sampled summary materially fails a required question or the implementer cannot confidently answer it.", "hygiene review expand " + run + "/" + batchId, "frontier"), sample, population, all, sources);
    }

    public ReviewBatch ExpandReview(string handle)
    {
        RunSnapshot snapshot = Read(LatestPath, new RunSnapshot(0, "", [], 0));
        if (snapshot.SchemaVersion != 1 || string.IsNullOrWhiteSpace(snapshot.RunId) || snapshot.ReviewBatches is null || snapshot.TargetPaths is null)
        {
            throw new ProductException("Review batch is unavailable; run check again.");
        }

        string batchId = handle;
        if (handle.Contains('/'))
        {
            string[] parts = handle.Split('/');
            if (parts.Length != 2 || parts[0] != snapshot.RunId)
            {
                throw new ProductException("Review batch handle does not refer to the latest available run.");
            }

            batchId = parts[1];
        }
        ReviewBatch stored = snapshot.ReviewBatches.SingleOrDefault(b => b.Id == batchId) ?? throw new ProductException($"Review batch '{handle}' was not found in the latest run.");
        CheckResult fresh;
        string[] revalidationPaths = snapshot.ChangedScope ? snapshot.TargetPaths : snapshot.InputPaths ?? snapshot.TargetPaths;
        try { fresh = Check(revalidationPaths, false, true, false, false); }
        catch (ProductException) { throw new ProductException("Review population cannot be revalidated; rerun hygiene check."); }
        ReviewBatch current = fresh.ReviewBatches.SingleOrDefault(b => b.RuleId == stored.RuleId) ?? throw new ProductException("Review population changed; rerun hygiene check.");
        if (!StringComparer.Ordinal.Equals(stored.PopulationFingerprint, current.PopulationFingerprint))
        {
            throw new ProductException("Review population changed; rerun hygiene check.");
        }

        return stored with
        {
            Mode = "expanded",
            ReviewerClass = "frontier",
            PopulationCount = current.PopulationCount,
            SampleCount = current.PopulationCount,
            Items = current.PopulationItems ?? [],
            PopulationItems = current.PopulationItems,
            SourceContents = current.SourceContents
        };
    }

    public string CreateReviewHandoff(string handle, string? filePath = null)
    {
        ReviewBatch expanded = ExpandReview(handle);
        string[] sourceHandle = expanded.Handle.Split('/');
        string runId = sourceHandle[0];
        string runToken = runId.StartsWith("R-", StringComparison.Ordinal) ? runId[2..] : runId;
        string batchToken = expanded.Id.Replace("-", "", StringComparison.Ordinal);
        string handoffId = "HR-" + Regex.Replace(runToken, "[^A-Za-z0-9]", "", RegexOptions.CultureInvariant).ToUpperInvariant() + "-" + Regex.Replace(batchToken, "[^A-Za-z0-9]", "", RegexOptions.CultureInvariant).ToUpperInvariant();
        var request = new SemanticReviewRequest(1, "semantic-review-request", handoffId, DateTimeOffset.UtcNow.ToString("O"),
            new ReviewRequestSource(runId, expanded.Handle), new ReviewRequestRule(expanded.RuleId, expanded.RuleVersion), "expanded", "frontier",
            expanded.PopulationCount, expanded.Questions, expanded.Items, expanded.SourceContents ?? []);
        string destination = Path.GetFullPath(string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(root, ".hygiene", "reviews", handoffId, "request.json")
            : Path.IsPathRooted(filePath) ? filePath : Path.Combine(root, filePath));
        WriteNewAtomic(destination, JsonSerializer.Serialize(request, json));
        return destination;
    }

    private void WriteNewAtomic(string path, string content)
    {
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        if (File.Exists(path))
        {
            throw new ProductException($"Review handoff destination already exists: '{path}'. Choose a new --file path or remove the existing request explicitly.");
        }

        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
                writer.Flush();
                stream.Flush(true);
            }
            beforeAtomicReplace?.Invoke();
            try { File.Move(temp, path, false); }
            catch (IOException) when (File.Exists(path))
            {
                throw new ProductException($"Review handoff destination already exists: '{path}'. Choose a new --file path or remove the existing request explicitly.");
            }
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }
    /// <inheritdoc/>
    internal static bool IsControl(StatementSyntax s) => s is IfStatementSyntax or SwitchStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax or WhileStatementSyntax or DoStatementSyntax or TryStatementSyntax or UsingStatementSyntax or LockStatementSyntax;
    private static bool Same(IgnoreDecision d, Finding f) => d.RuleId == f.RuleId && d.RuleVersion == f.RuleVersion && d.Path == f.Path && d.Anchor == f.Anchor && d.Fingerprint == f.Fingerprint && d.Discriminator == f.Discriminator;
    /// <inheritdoc/>
    internal static string NextDiscriminator(Dictionary<string, int> counts, Rule rule, string anchor, string evidence)
    {
        string key = rule.Id + "\0" + anchor + "\0" + evidence;
        counts.TryGetValue(key, out int occurrence);
        counts[key] = occurrence + 1;
        return "occurrence:" + occurrence;
    }
    /// <inheritdoc/>
    internal static void SourceTextLines(string s, Action<string, int, int> action)
    {
        int start = 0, number = 1;
        for (int i = 0; i <= s.Length; i++)
        {
            if (i < s.Length && s[i] is not ('\r' or '\n'))
            {
                continue;
            }

            action(s[start..i], number++, start);
            if (i == s.Length)
            {
                break;
            }

            if (s[i] == '\r' && i + 1 < s.Length && s[i + 1] == '\n')
            {
                i++;
            }

            start = i + 1;
        }
    }
    /// <inheritdoc/>
    internal static Finding Make(Rule r, string path, SyntaxTree tree, int offset, string? symbol, string msg, string suggestion, string observation, string constraint, string anchor, string evidence, string? discriminator = null)
    {
        var pos = tree.GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(offset, 0)).StartLinePosition;
        string fp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(evidence)));
        return new Finding("", "", r.Id, r.Version, r.Classification, path, pos.Line + 1, pos.Character + 1, symbol, msg, suggestion, observation, msg, constraint, anchor, fp, discriminator);
    }

    public Finding Explain(string handle)
    {
        RunSnapshot snapshot = Read(LatestPath, new RunSnapshot(0, "", [], 0));
        if (snapshot.SchemaVersion != 1 || string.IsNullOrWhiteSpace(snapshot.RunId) || snapshot.Findings is null || snapshot.IgnoredCount < 0 || snapshot.Findings.Any(f => f is null || string.IsNullOrEmpty(f.Id) || string.IsNullOrEmpty(f.RuleId)) || snapshot.Findings.Select(f => f.Id).Distinct(StringComparer.Ordinal).Count() != snapshot.Findings.Length)
        {
            throw new ProductException("Latest run state is unavailable or malformed; run check again.");
        }

        string id = handle;
        if (handle.Contains('/')) { string[] parts = handle.Split('/'); if (parts.Length != 2 || parts[0] != snapshot.RunId) { throw new ProductException("Finding handle does not refer to the latest available run."); } id = parts[1]; }
        return snapshot.Findings.SingleOrDefault(f => f.Id == id) ?? throw new ProductException($"Finding '{handle}' was not found in the latest run.");
    }
    public IgnoreDecision Ignore(string handle, string reason)
    {
        Finding reference = Explain(handle);
        if (!Rules.Single(r => r.Id == reference.RuleId).Configurable)
        {
            throw new ProductException($"Mandatory profile finding '{reference.RuleId}' cannot be ignored.");
        }

        CheckResult fresh = Check([reference.Path], false, false, true, false);
        Finding? current = fresh.Findings.FirstOrDefault(f => Same(new IgnoreDecision("", reference.RuleId, reference.RuleVersion, reference.Path, reference.Anchor, reference.Fingerprint, "", default, reference.Discriminator), f));
        if (current is null)
        {
            throw new ProductException("Finding is stale; run check again before ignoring it.");
        }

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
        if (next.Length == file.Decisions.Length)
        {
            throw new ProductException($"Ignore decision '{id}' was not found.");
        }

        WriteAtomic(DecisionsPath, JsonSerializer.Serialize(new DecisionFile(1, next), json), original, true);
    }
    public IReadOnlyList<IgnoreView> ListIgnores(string[] paths)
    {
        DecisionFile file = ReadDecisions();
        var filters = paths.Select(p =>
        {
            string full = Path.GetFullPath(Path.IsPathRooted(p) ? p : Path.Combine(root, p));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && full != root)
            {
                throw new ProductException("Ignore filter is outside the repository.");
            }

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
