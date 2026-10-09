using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DotNetAiCodeHygiene.Core.Sampling;

internal sealed record SamplingStateFile(int SchemaVersion, string Seed, SamplingRuleState[] Rules);
internal sealed record SamplingRuleState(string RuleId, int RuleVersion, int ModelVersion, string Model, string StateEpoch, SubjectHazardState[]? Subjects = null, PopulationHazardState[]? Populations = null);

/// <summary>Explicit durable sampler state, created only when a rule asks for it.</summary>
internal sealed class SamplingSession
{
    private const int Schema = 2;
    private readonly string path;
    private readonly Action? beforeCommit;
    private readonly string? original;
    private readonly SamplingStateFile state;
    private readonly Dictionary<string, SubjectHazardSampler> subjects = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PopulationHazardSampler> populations = new(StringComparer.Ordinal);
    private bool dirty;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    internal SamplingSession(string repositoryRoot, Action? beforeCommit = null)
    {
        path = Path.Combine(repositoryRoot, ".hygiene", ".state", "sampling.json");
        this.beforeCommit = beforeCommit;
        original = File.Exists(path) ? File.ReadAllText(path) : null;
        if (original is null) { state = new(Schema, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), []); }
        else
        {
            try { state = JsonSerializer.Deserialize<SamplingStateFile>(original) ?? throw new JsonException(); }
            catch (JsonException e) { throw new ProductException($"Invalid or unsupported .hygiene/.state/sampling.json: {e.Message}"); }
            if (state.SchemaVersion != Schema || state.Seed is null || !IsSeed(state.Seed) || state.Rules is null || state.Rules.Any(x => !ValidRuleState(x)) || state.Rules.Select(x => (x.RuleId, x.Model)).Distinct().Count() != state.Rules.Length)
                { throw new ProductException("Invalid or unsupported .hygiene/.state/sampling.json."); }
        }
    }

    internal SubjectHazardSampler SubjectSampler(string ruleId, int ruleVersion, int modelVersion = SamplingRandom.AlgorithmVersion)
    {
        ValidateRequest(ruleId, ruleVersion, modelVersion);
        EnsureSingleActiveVersion(ruleId, "subject", ruleVersion, modelVersion);
        string key = Key(ruleId, ruleVersion, modelVersion, "subject");
        if (!subjects.TryGetValue(key, out var sampler))
        {
            string epoch = Guid.NewGuid().ToString("N");
            var old = state.Rules.FirstOrDefault(r => r.RuleId == ruleId && r.Model == "subject");
            if (old is not null && old.RuleVersion == ruleVersion && old.ModelVersion == modelVersion)
            {
                epoch = old.StateEpoch;
                sampler = new(Convert.FromHexString(state.Seed), ruleId, ruleVersion, modelVersion, epoch);
                sampler.Restore(old.Subjects ?? []);
            }
            else { sampler = new(Convert.FromHexString(state.Seed), ruleId, ruleVersion, modelVersion, epoch); }
            subjects.Add(key, sampler);
        }
        dirty = true;
        return sampler;
    }

    internal PopulationHazardSampler PopulationSampler(string ruleId, int ruleVersion, int modelVersion = SamplingRandom.AlgorithmVersion)
    {
        ValidateRequest(ruleId, ruleVersion, modelVersion);
        EnsureSingleActiveVersion(ruleId, "population", ruleVersion, modelVersion);
        string key = Key(ruleId, ruleVersion, modelVersion, "population");
        if (!populations.TryGetValue(key, out var sampler))
        {
            string epoch = Guid.NewGuid().ToString("N");
            var old = state.Rules.FirstOrDefault(r => r.RuleId == ruleId && r.Model == "population");
            if (old is not null && old.RuleVersion == ruleVersion && old.ModelVersion == modelVersion)
            {
                epoch = old.StateEpoch;
                sampler = new(Convert.FromHexString(state.Seed), ruleId, ruleVersion, modelVersion, epoch);
                sampler.Restore(old.Populations ?? []);
            }
            else { sampler = new(Convert.FromHexString(state.Seed), ruleId, ruleVersion, modelVersion, epoch); }
            populations.Add(key, sampler);
        }
        dirty = true;
        return sampler;
    }

    internal bool IsLoaded => true;
    internal void Commit()
    {
        using PreparedSamplingCommit? prepared = PrepareCommit();
        if (prepared is null) { return; }
        prepared.Publish();
        prepared.Complete();
    }

    internal PreparedSamplingCommit? PrepareCommit()
    {
        if (!dirty) { return null; }
        var active = subjects.Keys.Concat(populations.Keys).Select(ParseKey).ToArray();
        var rules = state.Rules.Where(old => !active.Any(current => current.Rule == old.RuleId && current.Model == old.Model)).ToList();
        rules.AddRange(subjects.Select(x => new SamplingRuleState(ParseKey(x.Key).Rule, ParseKey(x.Key).RuleVersion, ParseKey(x.Key).ModelVersion, "subject", x.Value.StateEpoch, x.Value.States.ToArray())));
        rules.AddRange(populations.Select(x => new SamplingRuleState(ParseKey(x.Key).Rule, ParseKey(x.Key).RuleVersion, ParseKey(x.Key).ModelVersion, "population", x.Value.StateEpoch, Populations: x.Value.States.ToArray())));
        string content = JsonSerializer.Serialize(state with { Rules = rules.ToArray() }, Json);
        VerifyUnchanged();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, content, new UTF8Encoding(false));
            beforeCommit?.Invoke();
            VerifyUnchanged();
            return new PreparedSamplingCommit(path, temp, original, content, VerifyUnchanged);
        }
        catch
        {
            if (File.Exists(temp)) { File.Delete(temp); }
            throw;
        }
    }

    private void VerifyUnchanged()
    {
        if (File.Exists(path) ? File.ReadAllText(path) != original : original is not null)
        { throw new ProductException("Sampling state changed concurrently; retry the command."); }
    }

    private static bool IsSeed(string seed) { try { return Convert.FromHexString(seed).Length == 32 && seed == seed.ToUpperInvariant(); } catch (FormatException) { return false; } }
    private static bool ValidRuleState(SamplingRuleState value)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.RuleId) || value.RuleVersion < 1 || value.ModelVersion < 1 || !Guid.TryParseExact(value.StateEpoch, "N", out _)) { return false; }
        if (value.Model == "subject")
        {
            SubjectHazardState[] subjects = value.Subjects ?? [];
            return value.Populations is null && subjects.All(x => x is not null) && subjects.Select(x => x.SubjectId).Distinct(StringComparer.Ordinal).Count() == subjects.Length
                && subjects.All(x => x is not null && !string.IsNullOrEmpty(x.SubjectId) && x.Generation >= 0 && double.IsFinite(x.Hazard) && x.Hazard >= 0 && (x.LastEvaluationCursorUnixMilliseconds is null || x.LastEvaluationCursorUnixMilliseconds >= 0) && (x.LastObservedUnixSeconds is null || double.IsFinite(x.LastObservedUnixSeconds.Value)) && (x.LastEvaluationFingerprint is null || IsFingerprint(x.LastEvaluationFingerprint)));
        }
        if (value.Model == "population")
        {
            PopulationHazardState[] populations = value.Populations ?? [];
            return value.Subjects is null && populations.All(x => x is not null) && populations.Select(x => x.UnitId).Distinct(StringComparer.Ordinal).Count() == populations.Length
                && populations.All(x => x is not null && !string.IsNullOrEmpty(x.UnitId) && x.Generation >= 0 && double.IsFinite(x.ResidualHazard) && x.ResidualHazard >= 0 && double.IsFinite(x.PassEvidence) && x.PassEvidence >= 0 && double.IsFinite(x.FailEvidence) && x.FailEvidence >= 0 && (x.LastEvaluationCursorUnixMilliseconds is null || x.LastEvaluationCursorUnixMilliseconds >= 0) && (x.LastEvaluationFingerprint is null || IsFingerprint(x.LastEvaluationFingerprint)) && (x.LastCandidateCount is null || x.LastCandidateCount >= 0));
        }
        return false;
    }
    private static bool IsFingerprint(string fingerprint) => fingerprint.Length == 64 && fingerprint.All(Uri.IsHexDigit) && fingerprint == fingerprint.ToUpperInvariant();
    private static string Key(string id, int ruleVersion, int modelVersion, string model) => $"{model}\0{id}\0{ruleVersion}\0{modelVersion}";
    private void EnsureSingleActiveVersion(string ruleId, string model, int ruleVersion, int modelVersion)
    {
        bool conflict = subjects.Keys.Concat(populations.Keys).Select(ParseKey).Any(active => active.Rule == ruleId && active.Model == model && (active.RuleVersion != ruleVersion || active.ModelVersion != modelVersion));
        if (conflict) { throw new ProductException($"Conflicting sampling versions requested for '{ruleId}' in one command session."); }
    }
    private static void ValidateRequest(string ruleId, int ruleVersion, int modelVersion)
    {
        if (string.IsNullOrWhiteSpace(ruleId) || ruleVersion < 1 || modelVersion < 1) { throw new ArgumentException("Sampling rule identity and versions must be valid."); }
    }
    private static (string Model, string Rule, int RuleVersion, int ModelVersion) ParseKey(string key) { var p = key.Split('\0'); return (p[0], p[1], int.Parse(p[2]), int.Parse(p[3])); }
}

/// <summary>A prepared sampling-state replacement that can be rolled back while a check publishes its latest-run file.</summary>
internal sealed class PreparedSamplingCommit(string path, string tempPath, string? original, string content, Action verifyUnchanged) : IDisposable
{
    private readonly string backupPath = path + "." + Guid.NewGuid().ToString("N") + ".rollback";
    private bool published;
    private bool completed;

    internal void Publish()
    {
        verifyUnchanged();
        if (File.Exists(path)) { File.Replace(tempPath, path, backupPath); }
        else { File.Move(tempPath, path); }
        published = true;
    }

    internal void Rollback()
    {
        if (!published || completed) { return; }
        if (original is null)
        {
            if (File.Exists(path) && File.ReadAllText(path) == content) { File.Delete(path); }
        }
        else
        {
            if (!File.Exists(backupPath)) { throw new IOException("Sampling rollback copy is unavailable."); }
            if (!File.Exists(path) || File.ReadAllText(path) != content)
            { throw new ProductException("Sampling state changed concurrently; retry the command."); }
            File.Replace(backupPath, path, null);
        }
        published = false;
    }

    internal void Complete()
    {
        completed = true;
        Cleanup();
    }

    public void Dispose()
    {
        try { if (!completed && published) { Rollback(); } }
        finally { Cleanup(); }
    }

    private void Cleanup()
    {
        if (File.Exists(tempPath)) { File.Delete(tempPath); }
        if (File.Exists(backupPath)) { File.Delete(backupPath); }
    }
}
