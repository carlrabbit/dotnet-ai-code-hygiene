using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetAiCodeHygiene.Core;

public sealed record AuditRuleFact(string Id, int Version, bool Enabled, string Participation, string CheckUnit, int ApplicableChecks, int Eligible, int Acceptable, int Problem, int Uncertain, int Unassessed, int DeterministicFindings);
public sealed record AuditJudgmentInput(string ObligationId, string Outcome, string? Rationale = null);
public sealed record AuditJudgmentFile(string AuditId, IReadOnlyList<AuditJudgmentInput> Judgments);
public sealed record AuditNextWork(string AuditId, bool Complete, int Remaining, IReadOnlyList<AuditWorkItem> Items);
public sealed record AuditWorkItem(string ObligationId, string RuleId, int RuleVersion, string SubjectId, IReadOnlyList<ReviewQuestion> Questions, ReviewItem Subject, string SourcePath, string SourceContent);
public sealed record AuditSemanticObligation(string ObligationId, string RuleId, int RuleVersion, string SubjectId, IReadOnlyList<ReviewQuestion> Questions, ReviewItem Subject, string SourcePath);
public sealed record AuditFinding(string RuleId, int RuleVersion, Finding Finding, bool WasIgnored);
public sealed record AuditRewriteProposal(string Command, IReadOnlyList<string> RuleIds, IReadOnlyList<string> ChangedPaths, int TargetCount, int UnchangedCount);

internal sealed record AuditManifest(int SchemaVersion, string AuditId, string Status, DateTimeOffset StartedAt, string SourceHead, string SourceDirtyState, string ToolVersion, string Authorization, string InventorySha256);
internal sealed record AuditSource(string Path, string Content);
internal sealed record AuditSourceExclusions(int GeneratedFiles, int UnsupportedOrUnprojectedFiles);
internal sealed record AuditInventory(int SchemaVersion, string AuditId, IReadOnlyList<AuditRuleFact> Rules, IReadOnlyList<AuditSemanticObligation> Obligations, IReadOnlyList<AuditFinding> Findings, IReadOnlyList<AuditRewriteProposal> RewriteProposals, IReadOnlyList<AuditSource> Sources, AuditSourceExclusions SourceExclusions, string SnapshotLimitation);
internal sealed record StoredAuditJudgment(string ObligationId, string RuleId, string Outcome, string? Rationale, DateTimeOffset RecordedAt);
internal sealed record AuditJudgmentBatch(int SchemaVersion, string AuditId, IReadOnlyList<StoredAuditJudgment> Judgments);

/// <summary>Creates report-local exhaustive audit artifacts. It never publishes or updates ordinary hygiene state.</summary>
public sealed class ReadOnlyAuditEngine
{
    internal const int WorkBatchSize = 20;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    private readonly HygieneEngine hygiene;
    private readonly string root;

    public ReadOnlyAuditEngine(string? cwd = null)
    {
        hygiene = new HygieneEngine(cwd);
        root = hygiene.Root;
    }

    public string Start(string? reportDirectory = null, string? toolVersion = null)
    {
        string auditId = "A-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
        string destination = Path.GetFullPath(reportDirectory is null
            ? Path.Combine(root, ".hygiene", "audits", auditId)
            : Path.IsPathRooted(reportDirectory) ? reportDirectory : Path.Combine(root, reportDirectory));
        if (IsRemotePath(destination))
        {
            throw new ProductException("Audit reports must be written to a local filesystem path.");
        }

        if (Directory.Exists(destination) || File.Exists(destination))
        {
            throw new ProductException($"Audit destination already exists: {destination}");
        }

        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        string head = ReadHead(root);
        string dirtyState = ReadDirtyState(root);
        (CheckResult check, int sourceDocuments, int projects) = hygiene.CheckExhaustiveAuditSnapshot();
        (IReadOnlyList<AuditRuleFact> ruleFacts, IReadOnlyList<AuditSemanticObligation> obligations, IReadOnlyList<AuditFinding> findings, IReadOnlyList<AuditSource> sources) = BuildInventory(auditId, check, sourceDocuments, projects);
        AuditRewriteProposal[] rewriteProposals = [ObserveRewrite("format"), ObserveRewrite("normalize")];
        AuditSourceExclusions exclusions = MeasureSourceExclusions(sourceDocuments);
        var inventory = new AuditInventory(1, auditId, ruleFacts, obligations, findings, rewriteProposals, sources, exclusions,
            "The audit captures one finite observed inventory. Source changes during collection are not tracked; findings may span a mixed time interval and no atomic repository snapshot is claimed.");
        string inventoryJson = JsonSerializer.Serialize(inventory, Json);
        string inventoryHash = Hash(inventoryJson);
        var informationalVersion = typeof(ReadOnlyAuditEngine).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().Select(attribute => attribute.InformationalVersion.Split('+')[0]).FirstOrDefault();
        var manifest = new AuditManifest(1, auditId, "collecting", startedAt, head, dirtyState, toolVersion ?? informationalVersion ?? typeof(ReadOnlyAuditEngine).Assembly.GetName().Version?.ToString() ?? "unknown", "affirmative caller assertion: --attest-explicit-task (identity is not authenticated)", inventoryHash);
        if (Directory.Exists(destination) || File.Exists(destination))
        {
            throw new ProductException($"Audit destination already exists: {destination}");
        }

        Directory.CreateDirectory(destination);
        try
        {
            WriteNew(Path.Combine(destination, "inventory.json"), inventoryJson);
            Directory.CreateDirectory(Path.Combine(destination, "judgments"));
            WriteNew(Path.Combine(destination, "manifest.json"), JsonSerializer.Serialize(manifest, Json));
        }
        catch
        {
            // Keep any partial audit-local output for truthful recovery; never recursively remove a path
            // that could have acquired unrelated files after the initial collision check.
            throw;
        }
        return destination;
    }

    public AuditNextWork Next(string auditDirectory)
    {
        using var locked = Lock(auditDirectory);
        (AuditManifest manifest, AuditInventory inventory, IReadOnlyList<StoredAuditJudgment> judgments) = Load(auditDirectory);
        HashSet<string> recorded = judgments.Select(x => x.ObligationId).ToHashSet(StringComparer.Ordinal);
        AuditWorkItem[] items = SelectNextObligations(inventory.Obligations, recorded).Select(obligation =>
        {
            AuditSource source = inventory.Sources.SingleOrDefault(x => x.Path == obligation.SourcePath) ?? throw new ProductException("Audit source context is missing; the audit cannot continue safely.");
            return new AuditWorkItem(obligation.ObligationId, obligation.RuleId, obligation.RuleVersion, obligation.SubjectId, obligation.Questions, obligation.Subject, source.Path, source.Content);
        }).ToArray();
        return new(manifest.AuditId, items.Length == 0, inventory.Obligations.Count - recorded.Count, items);
    }

    internal static IReadOnlyList<AuditSemanticObligation> SelectNextObligations(IReadOnlyList<AuditSemanticObligation> obligations, IReadOnlySet<string> recorded)
    {
        var selected = new List<AuditSemanticObligation>(WorkBatchSize);
        foreach (AuditSemanticObligation obligation in obligations)
        {
            if (recorded.Contains(obligation.ObligationId)) { continue; }
            selected.Add(obligation);
            if (selected.Count == WorkBatchSize) { break; }
        }

        return selected;
    }

    public int Record(string auditDirectory, string judgmentFile)
    {
        using var locked = Lock(auditDirectory);
        (AuditManifest manifest, AuditInventory inventory, IReadOnlyList<StoredAuditJudgment> existing) = Load(auditDirectory);
        AuditJudgmentFile input;
        try { input = JsonSerializer.Deserialize<AuditJudgmentFile>(File.ReadAllText(judgmentFile), Json) ?? throw new JsonException(); }
        catch (Exception e) when (e is IOException or JsonException) { throw new ProductException("Judgment file is unreadable or malformed."); }
        if (input.AuditId != manifest.AuditId || input.Judgments is null || input.Judgments.Count == 0)
        {
            throw new ProductException("Judgment file must identify this audit and contain at least one judgment.");
        }

        if (input.Judgments.Count > WorkBatchSize)
        {
            throw new ProductException($"A judgment submission may contain at most {WorkBatchSize} obligations; split it into bounded batches.");
        }

        if (input.Judgments.Any(j => j is null || string.IsNullOrWhiteSpace(j.ObligationId) || string.IsNullOrWhiteSpace(j.Outcome)) || input.Judgments.Select(j => j.ObligationId).Distinct(StringComparer.Ordinal).Count() != input.Judgments.Count)
        {
            throw new ProductException("Judgment file contains missing fields or duplicate obligation IDs; no judgments were recorded.");
        }

        var byId = inventory.Obligations.ToDictionary(x => x.ObligationId, StringComparer.Ordinal);
        var prior = existing.Select(x => x.ObligationId).ToHashSet(StringComparer.Ordinal);
        var next = new List<StoredAuditJudgment>();
        foreach (AuditJudgmentInput judgment in input.Judgments)
        {
            if (!byId.TryGetValue(judgment.ObligationId, out AuditSemanticObligation? obligation))
            {
                throw new ProductException($"Unknown or cross-audit obligation '{judgment.ObligationId}'; no judgments were recorded.");
            }

            if (prior.Contains(judgment.ObligationId))
            {
                throw new ProductException($"Obligation '{judgment.ObligationId}' already has a recorded judgment; no judgments were recorded.");
            }

            if (judgment.Outcome is not ("acceptable" or "problem" or "uncertain"))
            {
                throw new ProductException($"Invalid outcome for '{judgment.ObligationId}'; use acceptable, problem, or uncertain.");
            }

            if ((judgment.Outcome is "problem" or "uncertain") && string.IsNullOrWhiteSpace(judgment.Rationale))
            {
                throw new ProductException($"Outcome '{judgment.Outcome}' requires concrete rationale for '{judgment.ObligationId}'.");
            }

            prior.Add(judgment.ObligationId);
            next.Add(new StoredAuditJudgment(judgment.ObligationId, obligation.RuleId, judgment.Outcome, judgment.Rationale, DateTimeOffset.UtcNow));
        }
        var batch = new AuditJudgmentBatch(1, manifest.AuditId, next);
        WriteAtomicNew(Path.Combine(auditDirectory, "judgments", "J-" + Guid.NewGuid().ToString("N") + ".json"), JsonSerializer.Serialize(batch, Json));

        return input.Judgments.Count;
    }

    public string Finish(string auditDirectory)
    {
        using var locked = Lock(auditDirectory);
        (AuditManifest manifest, AuditInventory inventory, IReadOnlyList<StoredAuditJudgment> storedJudgments) = Load(auditDirectory);
        var judgments = storedJudgments.ToDictionary(x => x.ObligationId, StringComparer.Ordinal);
        AuditRuleFact[] rules = inventory.Rules.Select(rule =>
        {
            StoredAuditJudgment[] forRule = inventory.Obligations.Where(x => x.RuleId == rule.Id).Select(x => judgments.GetValueOrDefault(x.ObligationId)).Where(x => x is not null).Cast<StoredAuditJudgment>().ToArray();
            int eligible = inventory.Obligations.Count(x => x.RuleId == rule.Id);
            bool semantic = rule.CheckUnit == "semantic rule/subject obligations";
            return rule with { ApplicableChecks = semantic ? eligible : rule.ApplicableChecks, Eligible = eligible, Acceptable = forRule.Count(x => x.Outcome == "acceptable"), Problem = forRule.Count(x => x.Outcome == "problem"), Uncertain = forRule.Count(x => x.Outcome == "uncertain"), Unassessed = eligible - forRule.Length };
        }).ToArray();
        int unassessed = rules.Sum(x => x.Unassessed);
        string status = unassessed == 0 ? "report-complete" : "incomplete";
        var report = new
        {
            schemaVersion = 1,
            auditId = manifest.AuditId,
            status,
            startedAt = manifest.StartedAt,
            finishedAt = DateTimeOffset.UtcNow,
            source = new { root, head = manifest.SourceHead, dirtyAtStart = manifest.SourceDirtyState, snapshotLimitation = inventory.SnapshotLimitation },
            sourceExclusions = inventory.SourceExclusions,
            tool = manifest.ToolVersion,
            authorization = manifest.Authorization,
            coverage = new { eligible = inventory.Obligations.Count, assessed = judgments.Count, acceptable = judgments.Values.Count(x => x.Outcome == "acceptable"), problem = judgments.Values.Count(x => x.Outcome == "problem"), uncertain = judgments.Values.Count(x => x.Outcome == "uncertain"), unassessed },
            rules,
            findings = inventory.Findings,
            rewriteProposals = inventory.RewriteProposals,
            semanticProblems = inventory.Obligations.Where(x => judgments.TryGetValue(x.ObligationId, out StoredAuditJudgment? j) && j.Outcome == "problem").Select(x => new { obligation = x, judgment = judgments[x.ObligationId] }),
            semanticUncertainties = inventory.Obligations.Where(x => judgments.TryGetValue(x.ObligationId, out StoredAuditJudgment? j) && j.Outcome == "uncertain").Select(x => new { obligation = x, judgment = judgments[x.ObligationId] }),
            continuation = unassessed == 0 ? null : $"dotnet hygiene audit next --run \"{auditDirectory}\"; then record validated judgments and run audit finish again."
        };
        WriteAtomic(Path.Combine(auditDirectory, "report.json"), JsonSerializer.Serialize(report, Json));
        WriteAtomic(Path.Combine(auditDirectory, "report.md"), Markdown(auditDirectory, manifest, inventory, rules, judgments, status, unassessed));
        return status;
    }

    private (IReadOnlyList<AuditRuleFact>, IReadOnlyList<AuditSemanticObligation>, IReadOnlyList<AuditFinding>, IReadOnlyList<AuditSource>) BuildInventory(string auditId, CheckResult check, int sourceDocuments, int projects)
    {
        IReadOnlyList<(Rule Rule, bool Enabled)> states = hygiene.ListRules();
        var facts = states.Select(state => new AuditRuleFact(state.Rule.Id, state.Rule.Version, state.Enabled, state.Enabled ? "enabled" : "disabled",
            state.Rule.OutputKind == "review-batch" ? "semantic rule/subject obligations" : state.Rule.Id.StartsWith("profile.", StringComparison.Ordinal) ? "supported SDK project evaluations" : "supported source documents",
            state.Rule.OutputKind == "review-batch" ? 0 : state.Rule.Id.StartsWith("profile.", StringComparison.Ordinal) ? projects : sourceDocuments, 0, 0, 0, 0, 0,
            check.Findings.Count(x => x.RuleId == state.Rule.Id))).ToArray();
        var obligations = new List<AuditSemanticObligation>();
        var sources = new Dictionary<string, AuditSource>(StringComparer.Ordinal);
        foreach (ReviewBatch batch in check.ReviewBatches)
        {
            foreach (ReviewItem item in batch.PopulationItems ?? batch.Items)
            {
                string obligationId = batch.RuleId + "/" + item.Id;
                obligations.Add(new AuditSemanticObligation(obligationId, batch.RuleId, batch.RuleVersion, item.Id, batch.Questions, item, item.Path));
            }
            foreach (ReviewSource source in batch.SourceContents ?? [])
            {
                sources.TryAdd(source.Path, new AuditSource(source.Path, source.Content));
            }
        }
        IgnoreDecision[] decisions = ReadIgnoreDecisions();
        var findings = check.Findings.Select(f => new AuditFinding(f.RuleId, f.RuleVersion, f, decisions.Any(d => Same(d, f)))).ToArray();
        return (facts, obligations, findings, sources.Values.OrderBy(x => x.Path, StringComparer.Ordinal).ToArray());
    }

    private AuditRewriteProposal ObserveRewrite(string command)
    {
        RewriteResult result = new RewriteEngine(root).Rewrite(command, [], false, checkOnly: true);
        return new AuditRewriteProposal(command, result.SelectedRuleIds, result.ChangedPaths, result.TargetCount, result.UnchangedCount);
    }

    private AuditSourceExclusions MeasureSourceExclusions(int supportedSourceDocuments)
    {
        int generated = 0;
        int candidates = 0;
        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part is ".git" or ".hygiene" or "bin" or "obj")) { continue; }
            candidates++;
            string file = Path.GetFileName(path);
            bool generatedPath = file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase);
            string header = string.Join("\n", File.ReadLines(path).Take(5));
            if (generatedPath || header.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase)) { generated++; }
        }

        return new AuditSourceExclusions(generated, Math.Max(0, candidates - generated - supportedSourceDocuments));
    }

    private IgnoreDecision[] ReadIgnoreDecisions()
    {
        string path = Path.Combine(root, ".hygiene", "decisions.json");
        if (!File.Exists(path)) { return []; }
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.GetProperty("decisions").Deserialize<IgnoreDecision[]>(Json) ?? [];
        }
        catch (Exception e) when (e is IOException or JsonException or KeyNotFoundException) { throw new ProductException("Existing ignore decisions could not be read for audit annotations."); }
    }

    private static bool Same(IgnoreDecision decision, Finding finding) => decision.RuleId == finding.RuleId && decision.RuleVersion == finding.RuleVersion
        && decision.Path == finding.Path && decision.Anchor == finding.Anchor && decision.Fingerprint == finding.Fingerprint && decision.Discriminator == finding.Discriminator;

    private static string Markdown(string auditDirectory, AuditManifest manifest, AuditInventory inventory, IReadOnlyList<AuditRuleFact> rules, IReadOnlyDictionary<string, StoredAuditJudgment> judgments, string status, int unassessed)
    {
        var b = new StringBuilder();
        b.AppendLine("# Read-only hygiene audit").AppendLine().AppendLine($"- Status: **{status}**").AppendLine($"- Audit: `{manifest.AuditId}`").AppendLine($"- Source HEAD: `{manifest.SourceHead}`").AppendLine($"- Source working tree at start: `{manifest.SourceDirtyState}`").AppendLine($"- Authorization: {manifest.Authorization}").AppendLine($"- Semantic obligations remaining: {unassessed}").AppendLine().AppendLine("## Rule coverage").AppendLine().AppendLine("| Rule | Participation | Check unit | Applicable checks | Eligible semantic subjects | Acceptable | Problem | Uncertain | Unassessed | Findings |").AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|---:|");
        foreach (AuditRuleFact rule in rules)
        {
            b.AppendLine($"| `{rule.Id}` v{rule.Version} | {rule.Participation} | {rule.CheckUnit} | {rule.ApplicableChecks} | {rule.Eligible} | {rule.Acceptable} | {rule.Problem} | {rule.Uncertain} | {rule.Unassessed} | {rule.DeterministicFindings} |");
        }

        b.AppendLine().AppendLine("## Deterministic findings").AppendLine();
        if (inventory.Findings.Count == 0)
        {
            b.AppendLine("No deterministic findings were emitted.");
        }

        foreach (AuditFinding finding in inventory.Findings)
        {
            b.AppendLine($"- `{finding.RuleId}` `{finding.Finding.Path}:{finding.Finding.Line}:{finding.Finding.Column}`{(finding.WasIgnored ? " (normal ignore recorded)" : "")} — {finding.Finding.Message}");
        }

        b.AppendLine().AppendLine("## Check-only rewrite proposals").AppendLine();
        foreach (AuditRewriteProposal proposal in inventory.RewriteProposals)
        {
            b.AppendLine($"- `{proposal.Command}`: {proposal.ChangedPaths.Count} would-change path(s) of {proposal.TargetCount} target(s); enabled rules: {string.Join(", ", proposal.RuleIds)}.");
            foreach (string path in proposal.ChangedPaths) { b.AppendLine($"  - `{path}`"); }
        }

        b.AppendLine().AppendLine("## Semantic problems and uncertainties").AppendLine();
        foreach (AuditSemanticObligation obligation in inventory.Obligations)
        {
            if (judgments.TryGetValue(obligation.ObligationId, out StoredAuditJudgment? j) && (j.Outcome is "problem" or "uncertain"))
            {
                b.AppendLine($"- **{j.Outcome}** `{obligation.RuleId}` `{obligation.Subject.Path}:{obligation.Subject.Line}` — {j.Rationale}");
            }
        }

        b.AppendLine().AppendLine("## Limitations").AppendLine().AppendLine(inventory.SnapshotLimitation).AppendLine("A complete report records coverage; it does not certify quality, defect freedom, statistical confidence, or user approval.");
        if (unassessed > 0)
        {
            b.AppendLine().AppendLine($"Continue report collection with `dotnet hygiene audit next --run \"{auditDirectory}\"`, then submit judgments and run `dotnet hygiene audit finish --run \"{auditDirectory}\"`.");
        }

        return b.ToString();
    }

    private (AuditManifest, AuditInventory, IReadOnlyList<StoredAuditJudgment>) Load(string directory)
    {
        string path = Path.GetFullPath(directory);
        AuditManifest manifest = Read<AuditManifest>(Path.Combine(path, "manifest.json"));
        AuditInventory inventory = Read<AuditInventory>(Path.Combine(path, "inventory.json"));
        string inventoryBytes = File.ReadAllText(Path.Combine(path, "inventory.json"));
        if (manifest.SchemaVersion != 1 || inventory.SchemaVersion != 1 || manifest.AuditId != inventory.AuditId || Hash(inventoryBytes) != manifest.InventorySha256)
        {
            throw new ProductException("Audit artifacts are inconsistent or corrupted; no report can be certified.");
        }

        var valid = inventory.Obligations.ToDictionary(x => x.ObligationId, StringComparer.Ordinal);
        string judgmentDirectory = Path.Combine(path, "judgments");
        if (!Directory.Exists(judgmentDirectory))
        {
            throw new ProductException("Audit judgment records are missing; no report can be certified.");
        }

        AuditJudgmentBatch[] batches;
        try
        {
            batches = Directory.EnumerateFiles(judgmentDirectory, "*.json", SearchOption.TopDirectoryOnly)
                .Order(StringComparer.Ordinal)
                .Select(file => Read<AuditJudgmentBatch>(file)).ToArray();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new ProductException("Audit judgment records are unavailable; no report can be certified.");
        }

        if (batches.Any(batch => batch.SchemaVersion != 1 || batch.AuditId != manifest.AuditId || batch.Judgments is null || batch.Judgments.Count > WorkBatchSize))
        {
            throw new ProductException("Audit judgment records are inconsistent or corrupted; no report can be certified.");
        }

        StoredAuditJudgment[] judgments = batches.SelectMany(batch => batch.Judgments).ToArray();
        if (judgments.Select(x => x.ObligationId).Distinct(StringComparer.Ordinal).Count() != judgments.Length
            || judgments.Any(x => !valid.TryGetValue(x.ObligationId, out AuditSemanticObligation? o) || o.RuleId != x.RuleId || x.Outcome is not ("acceptable" or "problem" or "uncertain") || ((x.Outcome is "problem" or "uncertain") && string.IsNullOrWhiteSpace(x.Rationale))))
        {
            throw new ProductException("Audit judgment records are inconsistent or corrupted; no report can be certified.");
        }

        return (manifest, inventory, judgments);
    }

    private static T Read<T>(string path)
    {
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) ?? throw new JsonException(); }
        catch (Exception e) when (e is IOException or JsonException) { throw new ProductException($"Audit artifact '{Path.GetFileName(path)}' is missing, malformed, or truncated."); }
    }
    private static FileStream Lock(string directory)
    {
        try { return new FileStream(Path.Combine(Path.GetFullPath(directory), ".audit.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { throw new ProductException("Audit is already in use or its report directory is unavailable; retry after the active operation finishes."); }
    }
    private static void WriteNew(string path, string content)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false)); writer.Write(content);
    }
    private static void WriteAtomicNew(string path, string content)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            WriteNew(temp, content);
            File.Move(temp, path);
        }
        finally
        {
            if (File.Exists(temp)) { File.Delete(temp); }
        }
    }
    private static void WriteAtomic(string path, string content)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { WriteNew(temp, content); if (File.Exists(path))
            {
                File.Replace(temp, path, null);
            }
            else
            {
                File.Move(temp, path);
            }
        }
        finally { if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string ReadHead(string root)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", "rev-parse --verify HEAD") { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true });
            if (process is null)
            {
                return "unavailable";
            }

            string output = process.StandardOutput.ReadToEnd().Trim(); process.WaitForExit(5000); return process.ExitCode == 0 ? output : "unavailable";
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or System.ComponentModel.Win32Exception) { return "unavailable"; }
    }
    private static string ReadDirtyState(string root)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", "status --porcelain=v1 --untracked-files=all") { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true });
            if (process is null) { return "unavailable"; }
            string output = process.StandardOutput.ReadToEnd(); process.WaitForExit(5000);
            return process.ExitCode == 0 ? output.Length == 0 ? "clean" : "dirty" : "unavailable";
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or System.ComponentModel.Win32Exception) { return "unavailable"; }
    }
    private static bool IsRemotePath(string path)
    {
        if (path.StartsWith("\\\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal)) { return true; }
        string? driveRoot = Path.GetPathRoot(path);
        return driveRoot is not null && new DriveInfo(driveRoot).DriveType == DriveType.Network;
    }
}
