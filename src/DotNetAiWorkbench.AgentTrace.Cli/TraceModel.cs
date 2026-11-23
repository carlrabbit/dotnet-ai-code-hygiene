using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetAiWorkbench.AgentTrace;

internal sealed record TraceEvent
{
    public int SchemaVersion { get; init; } = 1;
    public required string Session { get; init; }
    public string? Actor { get; init; }
    public required string Type { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
    public string? CommandText { get; init; }
    public string? CommandFingerprint { get; init; }
    public string? CommandName { get; init; }
    public bool Sensitive { get; init; }
    public string? Label { get; init; }
    public string? Kind { get; init; }
    public string? Name { get; init; }
    public int? ExitCode { get; init; }
    public string? Status { get; init; }
    public string? Note { get; init; }
}

internal sealed record TraceConfig(int SchemaVersion, StorageConfig Storage);
internal sealed record StorageConfig(string Provider, string Path);
internal sealed record StoredEvent(long Sequence, TraceEvent Event);
internal sealed record TraceGroup(string Session, IReadOnlyList<StoredEvent> Events);
internal sealed record TraceSessionSummary(string Session, IReadOnlyList<string> Actors, int EventCount, DateTimeOffset FirstEventUtc, DateTimeOffset LastEventUtc);
internal sealed record MatchedSpan(int StartIndex, int EndIndex, bool Ambiguous, double? DurationMilliseconds, bool ClockInvalid);
internal sealed record EventProjection(IReadOnlyList<StoredEvent> Events, IReadOnlyList<MatchedSpan> Commands, IReadOnlyList<int> UnmatchedCommandStarts, IReadOnlyList<int> UnmatchedCommandEnds, IReadOnlyList<MatchedSpan> Operations, IReadOnlyList<int> UnmatchedOperationStarts, IReadOnlyList<int> UnmatchedOperationEnds, IReadOnlyList<MatchedSpan> Pauses, IReadOnlyList<int> UnmatchedPauses, IReadOnlyList<int> UnmatchedResumes);

internal static class TraceJson
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
}

internal static class CommandFingerprint
{
    public static string Compute(string exactCommandText)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(exactCommandText))).ToLowerInvariant();
}

internal static class EventProjectionBuilder
{
    public static EventProjection Build(IReadOnlyList<StoredEvent> source, string session)
    {
        StoredEvent[] events = source.Where(item => item.Event.Session == session).OrderBy(item => item.Sequence).ToArray();
        List<MatchedSpan> commands = [];
        List<int> unmatchedCommandStarts = [], unmatchedCommandEnds = [];
        Match(
            events,
            e => e.Type == "command-start",
            e => e.Type == "command-end",
            start => (start.CommandFingerprint ?? "", start.Actor ?? "", start.Label ?? ""),
            end => (end.CommandFingerprint ?? "", end.Actor ?? "", end.Label ?? ""),
            commands,
            unmatchedCommandStarts,
            unmatchedCommandEnds);

        List<MatchedSpan> operations = [];
        List<int> unmatchedOperationStarts = [], unmatchedOperationEnds = [];
        Match(
            events,
            e => e.Type == "operation-start",
            e => e.Type == "operation-end",
            start => (start.Kind ?? "", start.Name ?? "", start.Actor ?? "", start.Label ?? ""),
            end => (end.Kind ?? "", end.Name ?? "", end.Actor ?? "", end.Label ?? ""),
            operations,
            unmatchedOperationStarts,
            unmatchedOperationEnds);

        List<MatchedSpan> pauses = [];
        List<int> unmatchedPauses = [], unmatchedResumes = [];
        Match(
            events,
            e => e.Type == "pause",
            e => e.Type == "resume",
            start => start.Actor ?? "",
            end => end.Actor ?? "",
            pauses,
            unmatchedPauses,
            unmatchedResumes);

        return new EventProjection(events, commands, unmatchedCommandStarts, unmatchedCommandEnds, operations, unmatchedOperationStarts, unmatchedOperationEnds, pauses, unmatchedPauses, unmatchedResumes);
    }

    private static void Match<TKey>(
        StoredEvent[] events,
        Func<TraceEvent, bool> isStart,
        Func<TraceEvent, bool> isEnd,
        Func<TraceEvent, TKey> startKey,
        Func<TraceEvent, TKey> endKey,
        List<MatchedSpan> matches,
        List<int> unmatchedStarts,
        List<int> unmatchedEnds) where TKey : notnull
    {
        var queues = new Dictionary<TKey, Queue<int>>();
        var ambiguousStarts = new HashSet<int>();
        var pairedStarts = new HashSet<int>();
        var indexBySequence = events.Select((item, index) => (item.Sequence, index)).ToDictionary(item => item.Sequence, item => item.index);

        foreach (StoredEvent item in events)
        {
            TraceEvent current = item.Event;
            int currentIndex = indexBySequence[item.Sequence];
            if (isStart(current))
            {
                TKey key = startKey(current);
                if (!queues.TryGetValue(key, out Queue<int>? queue))
                {
                    queue = new Queue<int>();
                    queues.Add(key, queue);
                }

                if (queue.Count > 0)
                {
                    foreach (int previous in queue)
                    {
                        ambiguousStarts.Add(previous);
                    }

                    ambiguousStarts.Add(currentIndex);
                }

                queue.Enqueue(currentIndex);
                continue;
            }

            if (!isEnd(current))
            {
                continue;
            }

            TKey endIdentity = endKey(current);
            if (!queues.TryGetValue(endIdentity, out Queue<int>? pending) || pending.Count == 0)
            {
                unmatchedEnds.Add(currentIndex);
                continue;
            }

            int startIndex = pending.Dequeue();
            TraceEvent startEvent = events[startIndex].Event;
            double duration = (current.TimestampUtc - startEvent.TimestampUtc).TotalMilliseconds;
            bool invalidClock = duration < 0;
            bool ambiguous = ambiguousStarts.Contains(startIndex);
            matches.Add(new MatchedSpan(startIndex, currentIndex, ambiguous, invalidClock ? null : duration, invalidClock));
            pairedStarts.Add(startIndex);
        }

        for (int i = 0; i < events.Length; i++)
        {
            if (isStart(events[i].Event) && !pairedStarts.Contains(i))
            {
                unmatchedStarts.Add(i);
            }
        }

        matches.Sort((left, right) => left.StartIndex.CompareTo(right.StartIndex));
    }
}

internal sealed class TraceException(string message, Exception? inner = null) : Exception(message, inner);
