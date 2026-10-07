using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

namespace DotNetAiCodeHygiene.Core.Sampling;

internal static class SamplingRandom
{
    internal const int AlgorithmVersion = 1;

    // Canonical encoding: seed bytes, followed by each UTF-8 key as a 32-bit big-endian length and bytes,
    // then generation as signed 64-bit big-endian. The first 53 digest bits use a midpoint transform.
    internal static double Uniform(ReadOnlySpan<byte> seed, IReadOnlyList<string> keys, long generation)
    {
        using var stream = new MemoryStream();
        stream.Write(seed);
        Span<byte> length = stackalloc byte[4];
        foreach (string key in keys)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(key);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            stream.Write(length);
            stream.Write(bytes);
        }
        Span<byte> number = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(number, generation);
        stream.Write(number);
        byte[] digest = SHA256.HashData(stream.ToArray());
        ulong bits = BinaryPrimitives.ReadUInt64BigEndian(digest) >> 11;
        return UniformFromBits(bits);
    }

    internal static double UniformFromBits(ulong bits)
    {
        if (bits >= (1UL << 53)) { throw new ArgumentOutOfRangeException(nameof(bits)); }
        double midpoint = (bits + 0.5) / 9007199254740992d;
        return Math.Min(midpoint, Math.BitDecrement(1d));
    }

    internal static double Threshold(ReadOnlySpan<byte> seed, IReadOnlyList<string> keys, long generation) => -Math.Log(Uniform(seed, keys, generation));
}

internal sealed record SubjectTicket(string RuleId, string SubjectId, int RuleVersion, int ModelVersion, long Generation, string StateEpoch, object SamplerIdentity);
internal sealed record SubjectHazardState(string SubjectId, long Generation, double Hazard, long? LastEvaluationCursorUnixMilliseconds = null, double? LastObservedUnixSeconds = null, bool? LastOutcome = null);
internal sealed record DueSubject(SubjectTicket Ticket, double Hazard, double Threshold, double Urgency);

internal sealed class SubjectHazardSampler(byte[] seed, string ruleId, int ruleVersion, int modelVersion, string stateEpoch = "default")
{
    private readonly Dictionary<string, SubjectHazardState> states = new(StringComparer.Ordinal);
    private readonly object samplerIdentity = new();
    internal string StateEpoch { get; } = stateEpoch;
    private readonly string ticketEpoch = stateEpoch + ":" + Convert.ToHexString(seed);
    internal IReadOnlyCollection<SubjectHazardState> States => states.Values;
    internal SubjectHazardState State(string id) => states.TryGetValue(id, out var value) ? value : new(id, 0, 0);
    internal void AddHazard(string id, double increment)
    {
        ValidateHazard(increment);
        var old = State(id);
        double hazard = old.Hazard + increment;
        ValidateHazard(hazard);
        states[id] = old with { Hazard = hazard };
    }
    internal void AccrueElapsed(string id, long evaluationCursorUnixMilliseconds, double hazardPerSecond)
    {
        ValidateCursor(evaluationCursorUnixMilliseconds);
        ValidateHazard(hazardPerSecond);
        var old = State(id);
        if (old.LastEvaluationCursorUnixMilliseconds is long previous && evaluationCursorUnixMilliseconds < previous)
        {
            throw new ArgumentOutOfRangeException(nameof(evaluationCursorUnixMilliseconds), "Evaluation cursor cannot move backwards.");
        }
        long elapsedMilliseconds = old.LastEvaluationCursorUnixMilliseconds is long last ? evaluationCursorUnixMilliseconds - last : 0;
        double increment = (elapsedMilliseconds / 1000d) * hazardPerSecond;
        ValidateHazard(increment);
        double hazard = old.Hazard + increment;
        ValidateHazard(hazard);
        states[id] = old with { Hazard = hazard, LastEvaluationCursorUnixMilliseconds = evaluationCursorUnixMilliseconds };
    }
    internal DueSubject? Due(string id)
    {
        var s = State(id); double threshold = SamplingRandom.Threshold(seed, [ruleId, ruleVersion.ToString(CultureInfo.InvariantCulture), modelVersion.ToString(CultureInfo.InvariantCulture), "subject", id], s.Generation);
        if (s.Hazard < threshold) { return null; }
        return new(new(ruleId, id, ruleVersion, modelVersion, s.Generation, ticketEpoch, samplerIdentity), s.Hazard, threshold, s.Hazard - threshold);
    }
    internal IReadOnlyList<DueSubject> SelectDue(IEnumerable<string> ids, int budget)
    {
        if (budget < 0) { throw new ArgumentOutOfRangeException(nameof(budget)); }
        return ids.Distinct(StringComparer.Ordinal).Select(Due).Where(x => x is not null).Select(x => x!)
            .OrderByDescending(x => x.Urgency).ThenBy(x => x.Ticket.SubjectId, StringComparer.Ordinal).Take(budget).ToArray();
    }
    internal bool Observe(SubjectTicket ticket, bool? outcome = null, DateTimeOffset? at = null)
    {
        if (!TicketMatches(ticket) || Due(ticket.SubjectId) is null) { return false; }
        var current = State(ticket.SubjectId);
        if (current.Generation == long.MaxValue) { throw new InvalidOperationException("Sampling subject generation is exhausted; reset its state."); }
        states[ticket.SubjectId] = current with { Generation = current.Generation + 1, Hazard = 0,
            LastObservedUnixSeconds = (at ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds(), LastOutcome = outcome };
        return true;
    }
    internal void Restore(IEnumerable<SubjectHazardState> values)
    {
        states.Clear(); foreach (var value in values) { states.Add(value.SubjectId, value); }
    }
    private bool TicketMatches(SubjectTicket t) => ReferenceEquals(t.SamplerIdentity, samplerIdentity) && t.RuleId == ruleId && t.RuleVersion == ruleVersion && t.ModelVersion == modelVersion && t.StateEpoch == ticketEpoch && State(t.SubjectId).Generation == t.Generation;
    internal static void ValidateHazard(double h) { if (!double.IsFinite(h) || h < 0) { throw new ArgumentOutOfRangeException(nameof(h), "Hazard must be finite and non-negative."); } }
    internal static void ValidateCursor(long cursor) { if (cursor < 0) { throw new ArgumentOutOfRangeException(nameof(cursor), "Evaluation cursor must be a non-negative Unix millisecond value."); } }
}

internal sealed record PopulationTicket(string RuleId, string UnitId, int RuleVersion, int ModelVersion, long Generation, string StateEpoch, object SamplerIdentity);
internal sealed record PopulationHazardState(string UnitId, long Generation, double ResidualHazard, double PassEvidence = 0, double FailEvidence = 0, long? LastEvaluationCursorUnixMilliseconds = null);
internal sealed record DuePopulationEvent(PopulationTicket Ticket, double Threshold, double ResidualAfterThreshold);

internal sealed class PopulationHazardSampler(byte[] seed, string ruleId, int ruleVersion, int modelVersion, string stateEpoch = "default")
{
    private readonly Dictionary<string, PopulationHazardState> states = new(StringComparer.Ordinal);
    private readonly object samplerIdentity = new();
    internal string StateEpoch { get; } = stateEpoch;
    private readonly string ticketEpoch = stateEpoch + ":" + Convert.ToHexString(seed);
    internal IReadOnlyCollection<PopulationHazardState> States => states.Values;
    internal PopulationHazardState State(string id) => states.TryGetValue(id, out var value) ? value : new(id, 0, 0);
    internal void AddHazard(string unit, double increment)
    {
        SubjectHazardSampler.ValidateHazard(increment); var s = State(unit); double residual = s.ResidualHazard + increment;
        SubjectHazardSampler.ValidateHazard(residual); states[unit] = s with { ResidualHazard = residual };
    }
    internal void AccrueElapsed(string unit, long evaluationCursorUnixMilliseconds, double hazardPerSecond)
    {
        SubjectHazardSampler.ValidateCursor(evaluationCursorUnixMilliseconds);
        SubjectHazardSampler.ValidateHazard(hazardPerSecond);
        var old = State(unit);
        if (old.LastEvaluationCursorUnixMilliseconds is long previous && evaluationCursorUnixMilliseconds < previous)
        {
            throw new ArgumentOutOfRangeException(nameof(evaluationCursorUnixMilliseconds), "Evaluation cursor cannot move backwards.");
        }
        long elapsedMilliseconds = old.LastEvaluationCursorUnixMilliseconds is long last ? evaluationCursorUnixMilliseconds - last : 0;
        double increment = (elapsedMilliseconds / 1000d) * hazardPerSecond;
        SubjectHazardSampler.ValidateHazard(increment);
        double residual = old.ResidualHazard + increment;
        SubjectHazardSampler.ValidateHazard(residual);
        states[unit] = old with { ResidualHazard = residual, LastEvaluationCursorUnixMilliseconds = evaluationCursorUnixMilliseconds };
    }
    internal IReadOnlyList<DuePopulationEvent> DueEvents(string unit, int maximum = 10000)
    {
        if (maximum < 0) { throw new ArgumentOutOfRangeException(nameof(maximum)); }
        var state = State(unit); var due = new List<DuePopulationEvent>(); double residual = state.ResidualHazard;
        for (long generation = state.Generation; due.Count < maximum; generation++)
        {
            double threshold = Threshold(unit, generation);
            if (residual < threshold) { break; }
            due.Add(new(new(ruleId, unit, ruleVersion, modelVersion, generation, ticketEpoch, samplerIdentity), threshold, residual - threshold));
            residual -= threshold;
        }
        return due;
    }
    internal bool Observe(PopulationTicket ticket, bool? passed = null, double retention = 1)
    {
        if (!double.IsFinite(retention) || retention < 0 || retention > 1) { throw new ArgumentOutOfRangeException(nameof(retention)); }
        var s = State(ticket.UnitId);
        if (!ReferenceEquals(ticket.SamplerIdentity, samplerIdentity) || ticket.RuleId != ruleId || ticket.RuleVersion != ruleVersion || ticket.ModelVersion != modelVersion || ticket.StateEpoch != ticketEpoch || ticket.Generation != s.Generation) { return false; }
        var due = DueEvents(ticket.UnitId, 1);
        if (due.Count == 0 || due[0].Ticket.Generation != ticket.Generation) { return false; }
        if (s.Generation == long.MaxValue) { throw new InvalidOperationException("Sampling population generation is exhausted; reset its state."); }
        states[ticket.UnitId] = s with { Generation = s.Generation + 1, ResidualHazard = s.ResidualHazard - due[0].Threshold,
            PassEvidence = s.PassEvidence * retention + (passed == true ? 1 : 0), FailEvidence = s.FailEvidence * retention + (passed == false ? 1 : 0) };
        return true;
    }
    internal IReadOnlyList<(string Candidate, PopulationTicket Ticket)> SelectSubjects(string unit, IReadOnlyList<string> candidates, int eventCount)
    {
        if (eventCount < 0) { throw new ArgumentOutOfRangeException(nameof(eventCount)); }
        var events = DueEvents(unit, eventCount); var chosen = new List<(string, PopulationTicket)>(); var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in events)
        {
            if (candidates.Count == 0) { break; }
            var ranked = candidates.Select(c => (Candidate: c, Rank: SamplingRandom.Uniform(seed, [ruleId, ruleVersion.ToString(CultureInfo.InvariantCulture), modelVersion.ToString(CultureInfo.InvariantCulture), "population", unit, c], e.Ticket.Generation)))
                .OrderBy(x => x.Rank).ThenBy(x => x.Candidate, StringComparer.Ordinal);
            var selected = ranked.FirstOrDefault(x => !used.Contains(x.Candidate));
            if (selected.Candidate is null) { selected = ranked.First(); } else { used.Add(selected.Candidate); }
            chosen.Add((selected.Candidate, e.Ticket));
        }
        return chosen;
    }
    internal void Restore(IEnumerable<PopulationHazardState> values) { states.Clear(); foreach (var value in values) { states.Add(value.UnitId, value); } }
    private double Threshold(string unit, long generation) => SamplingRandom.Threshold(seed, [ruleId, ruleVersion.ToString(CultureInfo.InvariantCulture), modelVersion.ToString(CultureInfo.InvariantCulture), "population", unit], generation);
}

internal sealed record DiscountedEvidence
{
    internal DiscountedEvidence(double pass, double fail)
    {
        ValidateCount(pass, nameof(pass));
        ValidateCount(fail, nameof(fail));
        Pass = pass;
        Fail = fail;
    }

    internal double Pass { get; }
    internal double Fail { get; }

    internal DiscountedEvidence Observe(bool passed, double retention)
    {
        if (!double.IsFinite(retention) || retention < 0 || retention > 1) { throw new ArgumentOutOfRangeException(nameof(retention)); }
        return new(Pass * retention + (passed ? 1 : 0), Fail * retention + (passed ? 0 : 1));
    }
    internal double PosteriorMean(double alpha0, double beta0)
    {
        if (!double.IsFinite(alpha0) || !double.IsFinite(beta0) || alpha0 <= 0 || beta0 <= 0) { throw new ArgumentOutOfRangeException(nameof(alpha0)); }
        return (alpha0 + Fail) / (alpha0 + beta0 + Fail + Pass);
    }

    private static void ValidateCount(double value, string parameter)
    {
        if (!double.IsFinite(value) || value < 0) { throw new ArgumentOutOfRangeException(parameter, "Evidence must be finite and non-negative."); }
    }
}
