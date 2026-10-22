using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DotNetAiCodeHygiene.Core;

internal sealed record ModernizationDecisionObservation(string RuleId, int RuleVersion, string OccurrenceId,
    string Fingerprint, string GateIdentity, string Verdict, DateTimeOffset RecordedAtUtc);
internal sealed record ModernizationDecisionState(int SchemaVersion, ModernizationDecisionObservation[] Observations);

internal static class ModernizationDecisionMechanics
{
    private const int MaximumObservations = 10_000;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = false, WriteIndented = true };

    internal static IReadOnlyList<RuleModuleExecution> Apply(IReadOnlyList<RuleModuleExecution> executions, string gateIdentity, string path)
    {
        if (!File.Exists(path)) { return executions; }
        ModernizationDecisionState state = Read(path);
        var decisions = state.Observations.Where(observation => observation.GateIdentity == gateIdentity)
            .GroupBy(observation => (observation.RuleId, observation.RuleVersion, observation.OccurrenceId, observation.Fingerprint))
            .ToDictionary(group => group.Key, group => group.Last().Verdict);
        if (decisions.Count == 0) { return executions; }

        return executions.Select(execution =>
        {
            if (execution.Descriptor.Id is not { } ruleId || !ruleId.StartsWith("modernization.", StringComparison.Ordinal)) { return execution; }
            RuleModuleResult result = execution.Result;
            var updated = result.ReviewSubjects.Select(subject => WithDecisions(subject, ruleId, execution.Descriptor.Version, decisions))
                .ToDictionary(subject => subject.Identity, StringComparer.Ordinal);
            ReviewSubject[] selected = (result.SelectedReviewSubjects ?? []).Select(subject => updated.GetValueOrDefault(subject.Identity, subject)).ToArray();
            return execution with
            {
                Result = result with
                {
                    ReviewSubjects = result.ReviewSubjects.Select(subject => updated.GetValueOrDefault(subject.Identity, subject)).ToArray(),
                    SelectedReviewSubjects = selected
                }
            };
        }).ToArray();
    }

    internal static string Fingerprint(string ruleId, int ruleVersion, ReviewOccurrence occurrence) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join("\0", ruleId, ruleVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), occurrence.Id,
            occurrence.DiagnosticId, occurrence.Path, occurrence.Owner, occurrence.OriginalSyntax, occurrence.Context,
            occurrence.PreviewTitle, occurrence.Preview))));

    internal static void Record(string path, string gateIdentity, string ruleId, int ruleVersion, string occurrenceId,
        string fingerprint, string verdict, Action<string, string, string?> writeAtomic)
    {
        string? original = File.Exists(path) ? File.ReadAllText(path) : null;
        ModernizationDecisionObservation[] current = original is null ? [] : Read(path).Observations;
        ModernizationDecisionObservation[] sameGate = current.Where(observation => observation.GateIdentity == gateIdentity
            && !(observation.RuleId == ruleId && observation.OccurrenceId == occurrenceId)).ToArray();
        if (current.Any(observation => observation.GateIdentity == gateIdentity && observation.RuleId == ruleId
            && observation.OccurrenceId == occurrenceId && observation.Fingerprint == fingerprint))
        {
            throw new ProductException("A decision for this modernization occurrence is already recorded for the current gate.");
        }
        if (sameGate.Length >= MaximumObservations) { throw new ProductException("Modernization decision state reached its bounded capacity; preserve the file and request a state-maintenance decision."); }
        ModernizationDecisionObservation[] next = sameGate.Append(new ModernizationDecisionObservation(ruleId, ruleVersion,
            occurrenceId, fingerprint, gateIdentity, verdict, DateTimeOffset.UtcNow))
            .OrderBy(observation => observation.RuleId, StringComparer.Ordinal)
            .ThenBy(observation => observation.OccurrenceId, StringComparer.Ordinal).ToArray();
        writeAtomic(path, JsonSerializer.Serialize(new ModernizationDecisionState(1, next), Json), original);
    }

    private static ModernizationDecisionState Read(string path)
    {
        try
        {
            ModernizationDecisionState state = JsonSerializer.Deserialize<ModernizationDecisionState>(File.ReadAllText(path), Json)
                ?? throw new JsonException();
            if (state.SchemaVersion != 1 || state.Observations is null || state.Observations.Length > MaximumObservations
                || state.Observations.Any(observation => observation is null || !observation.RuleId.StartsWith("modernization.", StringComparison.Ordinal)
                    || observation.RuleVersion < 1 || string.IsNullOrWhiteSpace(observation.OccurrenceId) || observation.Fingerprint.Length != 64
                    || observation.GateIdentity.Length != 64 || observation.Verdict is not ("desirable" or "retain" or "uncertain")))
            {
                throw new JsonException();
            }
            return state;
        }
        catch (Exception exception) when (exception is JsonException or IOException or NullReferenceException)
        {
            throw new ProductException("Invalid or unsupported .hygiene/.state/modernization-decisions.json; previous decisions were preserved.");
        }
    }

    private static ReviewSubject WithDecisions(ReviewSubject subject, string ruleId, int ruleVersion,
        IReadOnlyDictionary<(string RuleId, int RuleVersion, string OccurrenceId, string Fingerprint), string> decisions)
    {
        if (subject.Item.Occurrences is null) { return subject; }
        ReviewOccurrence[] occurrences = subject.Item.Occurrences.Select(occurrence =>
        {
            string fingerprint = Fingerprint(ruleId, ruleVersion, occurrence);
            return decisions.TryGetValue((ruleId, ruleVersion, occurrence.Id, fingerprint), out string? verdict)
                ? occurrence with { Decision = verdict }
                : occurrence;
        }).ToArray();
        return subject with { Item = subject.Item with { Occurrences = occurrences } };
    }
}
