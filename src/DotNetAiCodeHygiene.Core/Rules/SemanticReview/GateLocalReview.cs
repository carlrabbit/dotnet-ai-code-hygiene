using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DotNetAiCodeHygiene.Core;

internal sealed record GateTicket(string GateId, string RuleId, int RuleVersion, string SubjectId, string Fingerprint, string TicketId);
internal sealed record GateReviewObservation(string RuleId, int RuleVersion, string SubjectId, string Fingerprint, string GateId, DateTimeOffset AcceptedAtUtc);
internal sealed record GateReviewState(int SchemaVersion, GateReviewObservation[] Observations);

/// <summary>Deterministic, gate-local selection and explicit accepted-review evidence.</summary>
internal sealed class GateLocalReviewStore
{
    internal const int AlgorithmVersion = 2;
    private const int MaximumObservations = 50_000;
    private static readonly JsonSerializerOptions StateJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    private readonly string path;
    private readonly string? initial;
    private readonly Action? beforeCommit;
    private readonly List<GateReviewObservation> observations;

    internal GateLocalReviewStore(string repositoryRoot, Action? beforeCommit = null)
    {
        this.beforeCommit = beforeCommit;
        path = Path.Combine(repositoryRoot, ".hygiene", ".state", "gate-reviews.json");
        initial = File.Exists(path) ? File.ReadAllText(path) : null;
        if (initial is null) { observations = []; return; }
        try
        {
            GateReviewState state = JsonSerializer.Deserialize<GateReviewState>(initial, StateJson) ?? throw new JsonException();
            if (state.SchemaVersion != 1 || state.Observations is null || state.Observations.Any(x => x is null || string.IsNullOrWhiteSpace(x.RuleId) || x.RuleVersion < 1 || string.IsNullOrWhiteSpace(x.SubjectId) || x.Fingerprint.Length != 64 || x.GateId.Length != 64)) { throw new JsonException(); }
            observations = state.Observations.ToList();
        }
        catch (Exception e) when (e is JsonException or NullReferenceException)
        { throw new ProductException("Invalid or unsupported .hygiene/.state/gate-reviews.json; accepted review evidence was not changed."); }
    }

    internal IReadOnlyList<ReviewSubject> Select(string gateId, Rule rule, IReadOnlyList<ReviewSubject> population,
        IReadOnlySet<string> changedPaths, string mode, int limit = 5)
    {
        var eligible = population.Select(subject => (Subject: subject, Fingerprint: Fingerprint(subject.Content))).ToArray();
        string populationIdentity = Hash(string.Join("\n", eligible.OrderBy(x => x.Subject.Identity, StringComparer.Ordinal).Select(x => x.Subject.Identity + "\0" + x.Fingerprint)));
        string ruleGateId = Hash(gateId + "\0" + rule.Id + "\0" + rule.Version + "\0" + AlgorithmVersion + "\0" + populationIdentity);
        if (mode == "closed" || limit == 0) { return []; }
        bool broad = mode is "all" or "paths";
        bool Accepted((ReviewSubject Subject, string Fingerprint) candidate) => observations.Any(o => o.RuleId == rule.Id && o.RuleVersion == rule.Version && o.SubjectId == candidate.Subject.Identity && o.Fingerprint == candidate.Fingerprint);
        bool AcceptedBeforeThisGate((ReviewSubject Subject, string Fingerprint) candidate) => observations.Any(o => o.RuleId == rule.Id && o.RuleVersion == rule.Version && o.SubjectId == candidate.Subject.Identity && o.Fingerprint == candidate.Fingerprint && o.GateId != ruleGateId);
        // Earlier gates must not spend this gate's budget. Accepted items from this exact
        // gate stay in the ranked allocation until after the ceiling is applied, so a
        // partial accept cannot cause the same gate to backfill with new identities.
        var eligibleForAllocation = eligible.Where(x => !AcceptedBeforeThisGate(x)).ToArray();
        var direct = eligibleForAllocation.Where(x => changedPaths.Contains(x.Subject.Item.Path) && x.Subject.DirectChange).ToArray();
        var touched = eligibleForAllocation.Where(x => changedPaths.Contains(x.Subject.Item.Path) && !direct.Any(d => d.Subject.Identity == x.Subject.Identity)).ToArray();
        IEnumerable<(ReviewSubject Subject, string Fingerprint)> Rank(IEnumerable<(ReviewSubject Subject, string Fingerprint)> values) => values
            .OrderBy(x => RankKey(ruleGateId, rule, x.Subject.Identity, x.Fingerprint), StringComparer.Ordinal)
            .ThenBy(x => x.Subject.Identity, StringComparer.Ordinal);

        IEnumerable<(ReviewSubject Subject, string Fingerprint)> chosen = broad
            ? Rank(eligibleForAllocation).Take(limit)
            : Rank(direct).Take(Math.Max(0, limit - (touched.Length > 0 ? 1 : 0))).Concat(Rank(touched).Take(touched.Length > 0 && limit > 0 ? 1 : 0));
        return chosen.Where(x => !Accepted(x))
            .Select(x => x.Subject with { GateTicket = Ticket(ruleGateId, rule, x.Subject.Identity, x.Fingerprint) }).ToArray();
    }

    internal bool Accept(string gateId, Rule rule, ReviewSubject subject)
    {
        string fingerprint = Fingerprint(subject.Content);
        if (subject.GateTicket is not GateTicket ticket || ticket.GateId != gateId || ticket.RuleId != rule.Id || ticket.RuleVersion != rule.Version || ticket.SubjectId != subject.Identity || ticket.Fingerprint != fingerprint || ticket.TicketId != Ticket(gateId, rule, subject.Identity, fingerprint).TicketId) { return false; }
        if (observations.Any(o => o.RuleId == rule.Id && o.RuleVersion == rule.Version && o.SubjectId == subject.Identity && o.Fingerprint == fingerprint)) { return false; }
        observations.Add(new(rule.Id, rule.Version, subject.Identity, fingerprint, gateId, DateTimeOffset.UtcNow));
        return true;
    }

    internal bool AcceptTicket(GateTicket ticket)
    {
        if (ticket.TicketId != Ticket(ticket.GateId, new Rule(ticket.RuleId, ticket.RuleVersion, "review-batch", "review-batch", "", true), ticket.SubjectId, ticket.Fingerprint).TicketId) { return false; }
        if (observations.Any(o => o.RuleId == ticket.RuleId && o.RuleVersion == ticket.RuleVersion && o.SubjectId == ticket.SubjectId && o.Fingerprint == ticket.Fingerprint)) { return false; }
        observations.RemoveAll(o => o.RuleId == ticket.RuleId && o.SubjectId == ticket.SubjectId);
        if (observations.Count >= MaximumObservations) { throw new ProductException("Gate-review state reached its bounded observation limit; preserve the file and request a state-maintenance decision."); }
        observations.Add(new(ticket.RuleId, ticket.RuleVersion, ticket.SubjectId, ticket.Fingerprint, ticket.GateId, DateTimeOffset.UtcNow));
        return true;
    }

    internal void Commit()
    {
        string? current = File.Exists(path) ? File.ReadAllText(path) : null;
        if (!StringComparer.Ordinal.Equals(current, initial)) { throw new ProductException("Gate-review state changed concurrently; retry the command."); }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(new GateReviewState(1, observations.ToArray()), StateJson), new UTF8Encoding(false));
            beforeCommit?.Invoke();
            current = File.Exists(path) ? File.ReadAllText(path) : null;
            if (!StringComparer.Ordinal.Equals(current, initial)) { throw new ProductException("Gate-review state changed concurrently; retry the command."); }
            if (File.Exists(path)) { File.Replace(temp, path, null); } else { File.Move(temp, path); }
        }
        finally { if (File.Exists(temp)) { File.Delete(temp); } }
    }

    internal static string Fingerprint(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    internal static GateTicket Ticket(string gate, Rule rule, string subject, string fingerprint)
    {
        string id = Hash(gate + "\0" + rule.Id + "\0" + rule.Version + "\0" + AlgorithmVersion + "\0" + subject + "\0" + fingerprint);
        return new(gate, rule.Id, rule.Version, subject, fingerprint, id);
    }
    internal static string GateIdentity(string selectionGateId, string mode, string? baseRef, string head, IEnumerable<string> paths, IEnumerable<(string Rule, int Version, IEnumerable<ReviewSubject> Subjects)> populations)
    {
        string data = string.Join("\n", new[] { selectionGateId, mode, baseRef ?? "", head, string.Join("\n", paths.Order(StringComparer.Ordinal)) }
            .Concat(populations.OrderBy(x => x.Rule, StringComparer.Ordinal).Select(x => x.Rule + "/" + x.Version + "/" + string.Join("\n", x.Subjects.OrderBy(s => s.Identity, StringComparer.Ordinal).Select(s => s.Identity + "\0" + Fingerprint(s.Content))))));
        return Hash(data);
    }
    private static string RankKey(string gate, Rule rule, string identity, string fingerprint) => Hash(gate + "\0" + rule.Id + "\0" + rule.Version + "\0" + AlgorithmVersion + "\0" + identity + "\0" + fingerprint);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
