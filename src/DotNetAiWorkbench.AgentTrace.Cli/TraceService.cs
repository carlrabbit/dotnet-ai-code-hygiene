using System.Text;
using System.Text.Json;

namespace DotNetAiWorkbench.AgentTrace;

internal sealed class TraceService(string? repo)
{
    private readonly (string Root, ITraceStore Store) _repository = TraceRepository.Open(repo);
    private static DateTimeOffset Now() => DateTimeOffset.UtcNow;

    public void RecordMarker(string session, string? actor, string type, string? note = null)
    {
        if (type is not ("start" or "stop" or "pause" or "resume" or "mark"))
        {
            throw new TraceException($"Unknown marker type '{type}'.");
        }

        Append(NewEvent(session, actor, type) with { Note = note });
    }

    public void RecordCommandStart(string session, string? actor, string command, string? label, bool sensitive)
    {
        Append(NewEvent(session, actor, "command-start") with
        {
            CommandText = sensitive ? null : command,
            CommandFingerprint = CommandFingerprint.Compute(command),
            Sensitive = sensitive,
            Label = label
        });
    }

    public void RecordCommandEnd(string session, string? actor, string command, string? label, bool sensitive, int? exitCode, string? status)
    {
        if (exitCode is null && string.IsNullOrWhiteSpace(status))
        {
            throw new TraceException("Command ends require --exit-code and/or --status.");
        }

        Append(NewEvent(session, actor, "command-end") with
        {
            CommandFingerprint = CommandFingerprint.Compute(command),
            Sensitive = sensitive,
            Label = label,
            ExitCode = exitCode,
            Status = status
        });
    }

    public void RecordOperation(string session, string? actor, string edge, string kind, string name, string? label, string? status)
    {
        if (kind is not ("external" or "mcp"))
        {
            throw new TraceException("--kind must be 'external' or 'mcp'.");
        }

        if (edge == "end" && string.IsNullOrWhiteSpace(status))
        {
            throw new TraceException("Operation ends require --status.");
        }

        Append(NewEvent(session, actor, "operation-" + edge) with { Kind = kind, Name = name, Label = label, Status = status });
    }

    public IReadOnlyList<StoredEvent> Events() => _repository.Store.ReadAll();

    private TraceEvent NewEvent(string session, string? actor, string type)
    {
        ValidateCallerKey(session, "--session");
        if (actor is not null)
        {
            ValidateCallerKey(actor, "--actor");
        }

        return new TraceEvent { Session = session, Actor = actor, Type = type, TimestampUtc = Now() };
    }

    private void Append(TraceEvent item)
    {
        using var sessionLock = new SessionLock(_repository.Store.LockDirectory, item.Session);
        if (_repository.Store.Append(item))
        {
            Console.Error.WriteLine("AgentTrace warning: discarded an incomplete malformed JSONL tail before appending; prior complete records were preserved.");
        }
    }

    private static void ValidateCallerKey(string value, string option)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new TraceException($"{option} must be a nonempty caller-selected key.");
        }
    }

    public IReadOnlyList<TraceSessionSummary> Sessions()
    {
        return Events().GroupBy(item => item.Event.Session, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new TraceSessionSummary(group.Key,
                group.Select(item => item.Event.Actor).Where(actor => actor is not null).Cast<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
                group.Count(), group.Min(item => item.Event.TimestampUtc), group.Max(item => item.Event.TimestampUtc))).ToArray();
    }

    private EventProjection Projection(string session)
    {
        EventProjection projection = EventProjectionBuilder.Build(Events(), session);
        if (projection.Events.Count == 0)
        {
            throw new TraceException($"No recorded events for session '{session}' in the currently configured {_repository.Store.Provider} store at '{_repository.Store.Location}'.");
        }

        return projection;
    }

    public string ExportJson(string session)
    {
        EventProjection projection = Projection(session);
        return JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            session,
            store = _repository.Store.Provider,
            events = projection.Events.Select(item => item.Event).ToArray(),
            derived = Derived(projection)
        }, new JsonSerializerOptions(TraceJson.Options) { WriteIndented = true });
    }

    public string ExportJsonl(string session)
    {
        EventProjection projection = Projection(session);
        var lines = new List<string>
        {
            JsonSerializer.Serialize(new { schemaVersion = 1, recordType = "metadata", session, store = _repository.Store.Provider }, TraceJson.Options)
        };
        foreach (StoredEvent item in projection.Events)
        {
            lines.Add(JsonSerializer.Serialize(new { schemaVersion = 1, recordType = "event", @event = item.Event }, TraceJson.Options));
        }

        lines.Add(JsonSerializer.Serialize(new { schemaVersion = 1, recordType = "derived", value = Derived(projection) }, TraceJson.Options));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static object Derived(EventProjection projection)
    {
        IReadOnlyList<Interval> commands = MatchedIntervals(projection, projection.Commands);
        IReadOnlyList<Interval> operations = MatchedIntervals(projection, projection.Operations);
        IReadOnlyList<Interval> pauses = MatchedIntervals(projection, projection.Pauses);
        return new
        {
            recordType = "derivedInterpretation",
            commands = projection.Commands.Select(pair => new { startIndex = pair.StartIndex, endIndex = pair.EndIndex, durationMilliseconds = pair.DurationMilliseconds, pair.Ambiguous, pair.ClockInvalid, sensitive = projection.Events[pair.StartIndex].Event.Sensitive || projection.Events[pair.EndIndex].Event.Sensitive }),
            unmatchedCommandStarts = projection.UnmatchedCommandStarts,
            unmatchedCommandEnds = projection.UnmatchedCommandEnds,
            repeatedCommands = RepeatedCommands(projection),
            operations = projection.Operations.Select(pair => new { startIndex = pair.StartIndex, endIndex = pair.EndIndex, durationMilliseconds = pair.DurationMilliseconds, pair.Ambiguous, pair.ClockInvalid }),
            unmatchedOperationStarts = projection.UnmatchedOperationStarts,
            unmatchedOperationEnds = projection.UnmatchedOperationEnds,
            pauses = projection.Pauses.Select(pair => new { startIndex = pair.StartIndex, endIndex = pair.EndIndex, durationMilliseconds = pair.DurationMilliseconds, pair.Ambiguous, pair.ClockInvalid }),
            unmatchedPauses = projection.UnmatchedPauses,
            unmatchedResumes = projection.UnmatchedResumes,
            actorCoverageMilliseconds = ActorCoverage(projection, commands, operations, pauses),
            commandUnionMilliseconds = Union(commands.Select(interval => interval with { Pauses = PausesForActor(pauses, interval.Actor) })),
            declaredOperationUnionMilliseconds = Union(operations.Select(interval => interval with { Pauses = PausesForActor(pauses, interval.Actor) })),
            crossActorCommandOverlaps = CrossActorOverlaps(projection, commands)
        };
    }

    public string Report(string session, bool markdown)
    {
        EventProjection p = Projection(session);
        DateTimeOffset first = p.Events.Min(item => item.Event.TimestampUtc);
        DateTimeOffset last = p.Events.Max(item => item.Event.TimestampUtc);
        IReadOnlyList<Interval> commandIntervals = MatchedIntervals(p, p.Commands);
        IReadOnlyList<Interval> operationIntervals = MatchedIntervals(p, p.Operations);
        IReadOnlyList<Interval> pauseIntervals = MatchedIntervals(p, p.Pauses);
        double commandUnion = Union(commandIntervals.Select(interval => interval with { Pauses = PausesForActor(pauseIntervals, interval.Actor) }));
        double operationUnion = Union(operationIntervals.Select(interval => interval with { Pauses = PausesForActor(pauseIntervals, interval.Actor) }));
        IReadOnlyList<Interval> observedIntervals = commandIntervals.Concat(operationIntervals).ToArray();
        double observedCoverage = Union(observedIntervals.Select(interval => interval with { Pauses = PausesForActor(pauseIntervals, interval.Actor) }));
        double window = Math.Max(0, (last - first).TotalMilliseconds);
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> gaps = Gaps(first, last, observedIntervals, pauseIntervals);

        var b = new StringBuilder();
        b.AppendLine(markdown ? $"# AgentTrace — `{session}`" : $"AgentTrace session {session}");
        b.AppendLine($"Observed event window: {first:O} to {last:O} ({Fmt(window)}). Events describe only this observed window, not necessarily the complete session lifetime.");
        b.AppendLine($"Recorded command coverage (union, actor pauses excluded): {Fmt(commandUnion)} | declared operation coverage: {Fmt(operationUnion)} | combined observed coverage: {Fmt(observedCoverage)} | unattributed observed gaps: {Fmt(Math.Max(0, window - observedCoverage))}.");
        b.AppendLine("Unattributed gaps are not model thinking time; they may contain latency, human waiting, unwrapped commands, uninstrumented MCP activity, or explicitly paused intervals.");
        b.AppendLine(markdown ? "\n## Actors\n" : "Actors:");
        foreach (string? actor in p.Events.Select(item => item.Event.Actor).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
        {
            string actorName = actor ?? "(unspecified actor)";
            Interval[] actorCommands = commandIntervals.Where(interval => interval.Actor == actor).ToArray();
            Interval[] actorOperations = operationIntervals.Where(interval => interval.Actor == actor).ToArray();
            Interval[] actorPauses = pauseIntervals.Where(interval => interval.Actor == actor).ToArray();
            b.AppendLine($"- {actorName}: {actorCommands.Length} paired commands; command coverage {Fmt(Union(actorCommands.Select(interval => interval with { Pauses = actorPauses })))}; {actorPauses.Length} paired pause interval(s).");
        }

        b.AppendLine(markdown ? "\n## Timeline\n" : "Timeline:");
        foreach ((StoredEvent stored, int index) in p.Events.Select((item, index) => (item, index)).OrderBy(entry => entry.item.Event.TimestampUtc).ThenBy(entry => entry.item.Sequence))
        {
            b.AppendLine($"- {stored.Event.TimestampUtc:O}  {Describe(stored.Event)}");
        }

        b.AppendLine(markdown ? "\n## Command matching\n" : "Command matching:");
        foreach (MatchedSpan pair in p.Commands)
        {
            TraceEvent start = p.Events[pair.StartIndex].Event;
            TraceEvent end = p.Events[pair.EndIndex].Event;
            string identity = DisplayCommand(start);
            string status = end.Status ?? (end.ExitCode is int code ? code == 0 ? "reported success" : "reported failure" : "outcome unspecified");
            b.AppendLine($"- {identity}: {FmtDuration(pair.DurationMilliseconds, pair.ClockInvalid)}; {status}; actor={ActorLabel(start.Actor)}{LabelText(start.Label)}{(pair.Ambiguous ? "; AMBIGUOUS FIFO pairing" : "")}");
        }

        foreach (int index in p.UnmatchedCommandStarts)
        {
            TraceEvent item = p.Events[index].Event;
            b.AppendLine($"- unmatched start: {DisplayCommand(item)}; actor={ActorLabel(item.Actor)}{LabelText(item.Label)}{(item.Sensitive ? "; sensitive hash" : "")}");
        }

        foreach (int index in p.UnmatchedCommandEnds)
        {
            TraceEvent item = p.Events[index].Event;
            b.AppendLine($"- unmatched end: {FingerprintLabel(item)}; actor={ActorLabel(item.Actor)}{LabelText(item.Label)}; reported {item.Status ?? (item.ExitCode is int code ? $"exit {code}" : "outcome unspecified")}");
        }

        b.AppendLine(markdown ? "\n## Repeated command identities\n" : "Repeated command identities:");
        foreach (var repeated in RepeatedCommands(p))
        {
            b.AppendLine($"- {repeated.Identity}: {repeated.Count} recorded executions; known duration sum {Fmt(repeated.DurationMilliseconds)}; actor={ActorLabel(repeated.Actor)}{LabelText(repeated.Label)}. All activity is agent-reported.");
        }

        b.AppendLine(markdown ? "\n## Declared operations and markers\n" : "Declared operations and markers:");
        foreach (MatchedSpan pair in p.Operations)
        {
            TraceEvent start = p.Events[pair.StartIndex].Event;
            TraceEvent end = p.Events[pair.EndIndex].Event;
            b.AppendLine($"- declared {start.Kind}: {start.Name}; {FmtDuration(pair.DurationMilliseconds, pair.ClockInvalid)}; {end.Status}; actor={ActorLabel(start.Actor)}{LabelText(start.Label)}{(pair.Ambiguous ? "; AMBIGUOUS FIFO pairing" : "")}");
        }

        foreach (int index in p.UnmatchedOperationStarts)
        {
            TraceEvent item = p.Events[index].Event;
            b.AppendLine($"- unmatched declared {item.Kind} start: {item.Name}; actor={ActorLabel(item.Actor)}{LabelText(item.Label)}");
        }

        foreach (int index in p.UnmatchedOperationEnds)
        {
            TraceEvent item = p.Events[index].Event;
            b.AppendLine($"- unmatched declared {item.Kind} end: {item.Name}; status={item.Status}; actor={ActorLabel(item.Actor)}{LabelText(item.Label)}");
        }

        foreach (int index in p.UnmatchedPauses)
        {
            b.AppendLine($"- unpaired pause annotation: actor={ActorLabel(p.Events[index].Event.Actor)}");
        }

        foreach (int index in p.UnmatchedResumes)
        {
            b.AppendLine($"- unpaired resume annotation: actor={ActorLabel(p.Events[index].Event.Actor)}");
        }

        foreach (MatchedSpan pause in p.Pauses.Where(span => span.Ambiguous))
        {
            b.AppendLine($"- AMBIGUOUS pause/resume FIFO interpretation: actor={ActorLabel(p.Events[pause.StartIndex].Event.Actor)}; elapsed time is only an estimate.");
        }

        int failed = p.Commands.Count(pair =>
        {
            TraceEvent end = p.Events[pair.EndIndex].Event;
            return end.Status is "failed" or "interrupted" or "incomplete" || end.ExitCode is not null and not 0;
        });
        b.AppendLine($"\nSummary: {p.Events.Count} events; {p.Commands.Count} paired commands ({failed} reported failed/interrupted); {p.UnmatchedCommandStarts.Count} command start(s) missing an end; {p.UnmatchedCommandEnds.Count} unmatched command end(s); {p.Commands.Count(pair => pair.Ambiguous)} ambiguous FIFO pair(s); {CrossActorOverlaps(p, commandIntervals).Count} cross-actor command overlap(s).");
        b.AppendLine("Largest unattributed observed gaps:");
        foreach (var gap in gaps.OrderByDescending(interval => interval.End - interval.Start).Take(3))
        {
            b.AppendLine($"- {gap.Start:O} to {gap.End:O}: {Fmt((gap.End - gap.Start).TotalMilliseconds)}");
        }

        if (p.Events.Any(item => item.Event.Actor is null))
        {
            b.AppendLine("Actor attribution is incomplete where callers omitted --actor; independent writers may share an undifferentiated stream.");
        }

        b.AppendLine("Command durations are UTC timestamp differences across separate CLI calls, not continuously measured process runtimes.");
        b.AppendLine("All command activity and outcomes are agent-reported; the recorder does not execute or independently verify commands.");
        b.AppendLine("Sensitive command hashes are unkeyed SHA-256 correlation values, not encryption or protection against guessing.");
        return b.ToString();
    }

    private static string Describe(TraceEvent item)
    {
        string actor = ActorLabel(item.Actor);
        string label = LabelText(item.Label);
        return item.Type switch
        {
            "command-start" => $"command start: {DisplayCommand(item)}{(item.Sensitive ? "" : $"; fingerprint={item.CommandFingerprint}")}; actor={actor}{label}{(item.Sensitive ? "; sensitive" : "")}",
            "command-end" => $"command end: {FingerprintLabel(item)}; actor={actor}{label}; {item.Status ?? (item.ExitCode is int code ? $"reported exit {code}" : "outcome unspecified")}{(item.ExitCode is int exitCode ? $" (exit {exitCode})" : "")}{(item.Sensitive ? "; sensitive" : "")}",
            "operation-start" => $"declared {item.Kind} start: {item.Name}; actor={actor}{label} (agent-reported)",
            "operation-end" => $"declared {item.Kind} end: {item.Name}; actor={actor}{label}; {item.Status} (agent-reported)",
            "mark" => $"mark: {item.Note}; actor={actor}",
            _ => $"{item.Type} annotation; actor={actor}{(item.Note is null ? "" : "; " + item.Note)}"
        };
    }

    private static IReadOnlyList<RepeatedCommand> RepeatedCommands(EventProjection projection)
    {
        return projection.Commands
            .GroupBy(pair =>
            {
                TraceEvent start = projection.Events[pair.StartIndex].Event;
                return (start.CommandFingerprint, start.Actor, start.Label);
            })
            .Where(group => group.Count() > 1)
            .Select(group =>
            {
                TraceEvent start = projection.Events[group.First().StartIndex].Event;
                double knownDuration = group.Where(pair => !pair.ClockInvalid && pair.DurationMilliseconds is not null).Sum(pair => pair.DurationMilliseconds!.Value);
                return new RepeatedCommand(DisplayCommand(start), group.Count(), knownDuration, start.Actor, start.Label);
            })
            .ToArray();
    }

    private sealed record RepeatedCommand(string Identity, int Count, double DurationMilliseconds, string? Actor, string? Label);

    private static string DisplayCommand(TraceEvent item) => item.Sensitive ? FingerprintLabel(item) : item.CommandText ?? FingerprintLabel(item);
    private static string FingerprintLabel(TraceEvent item) => $"sha256:{item.CommandFingerprint}{(item.Sensitive ? " (sensitive)" : "")}";
    private static string ActorLabel(string? actor) => actor ?? "(unspecified)";
    private static string LabelText(string? label) => label is null ? "" : $" label={label}";
    private static string Fmt(double milliseconds)
    {
        TimeSpan duration = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return $"{(long)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}.{duration.Milliseconds:000}";
    }
    private static string FmtDuration(double? milliseconds, bool invalid) => invalid ? "invalid/backwards UTC interval" : milliseconds is null ? "duration unknown" : Fmt(milliseconds.Value) + " (UTC-derived)";

    private sealed record Interval(int StartIndex, int EndIndex, DateTimeOffset Start, DateTimeOffset End, string? Actor, IReadOnlyList<Interval> Pauses);

    private static IReadOnlyList<Interval> MatchedIntervals(EventProjection projection, IReadOnlyList<MatchedSpan> spans)
        => spans.Where(span => !span.ClockInvalid && span.DurationMilliseconds is not null)
            .Select(span => new Interval(span.StartIndex, span.EndIndex, projection.Events[span.StartIndex].Event.TimestampUtc, projection.Events[span.EndIndex].Event.TimestampUtc, projection.Events[span.StartIndex].Event.Actor, []))
            .ToArray();

    private static IReadOnlyList<Interval> PausesForActor(IReadOnlyList<Interval> pauses, string? actor) => pauses.Where(pause => pause.Actor == actor).ToArray();

    private static double Union(IEnumerable<Interval> intervals)
    {
        var slices = new List<(DateTimeOffset Start, DateTimeOffset End)>();
        foreach (Interval interval in intervals)
        {
            var remaining = new List<(DateTimeOffset Start, DateTimeOffset End)> { (interval.Start, interval.End) };
            foreach (Interval pause in interval.Pauses)
            {
                remaining = remaining.SelectMany(slice => pause.End <= slice.Start || pause.Start >= slice.End
                    ? [slice]
                    : new[] { (slice.Start, pause.Start > slice.Start ? pause.Start : slice.Start), (pause.End < slice.End ? pause.End : slice.End, slice.End) }.Where(part => part.Item2 > part.Item1)).ToList();
            }

            slices.AddRange(remaining);
        }

        double total = 0;
        DateTimeOffset? currentStart = null, currentEnd = null;
        foreach (var slice in slices.OrderBy(slice => slice.Start))
        {
            if (currentStart is null)
            {
                currentStart = slice.Start;
                currentEnd = slice.End;
            }
            else if (slice.Start <= currentEnd)
            {
                currentEnd = slice.End > currentEnd ? slice.End : currentEnd;
            }
            else
            {
                total += (currentEnd!.Value - currentStart.Value).TotalMilliseconds;
                currentStart = slice.Start;
                currentEnd = slice.End;
            }
        }

        if (currentStart is not null)
        {
            total += (currentEnd!.Value - currentStart.Value).TotalMilliseconds;
        }

        return Math.Max(0, total);
    }

    private static object ActorCoverage(EventProjection projection, IReadOnlyList<Interval> commands, IReadOnlyList<Interval> operations, IReadOnlyList<Interval> pauses)
    {
        return projection.Events.Select(item => item.Event.Actor).Distinct(StringComparer.Ordinal).OrderBy(actor => actor, StringComparer.Ordinal).Select(actor => new
        {
            actor = actor ?? "(unspecified)",
            commandCoverageMilliseconds = Union(commands.Where(interval => interval.Actor == actor).Select(interval => interval with { Pauses = PausesForActor(pauses, actor) })),
            operationCoverageMilliseconds = Union(operations.Where(interval => interval.Actor == actor).Select(interval => interval with { Pauses = PausesForActor(pauses, actor) })),
            pausedMilliseconds = Union(PausesForActor(pauses, actor).Select(interval => interval with { Pauses = [] }))
        }).ToArray();
    }

    private static IReadOnlyList<object> CrossActorOverlaps(EventProjection projection, IReadOnlyList<Interval> commands)
    {
        var overlaps = new List<object>();
        for (int i = 0; i < commands.Count; i++)
        {
            for (int j = i + 1; j < commands.Count; j++)
            {
                Interval left = commands[i], right = commands[j];
                if (left.Actor == right.Actor || left.Start >= right.End || right.Start >= left.End)
                {
                    continue;
                }

                overlaps.Add(new { firstStartIndex = left.StartIndex, secondStartIndex = right.StartIndex, firstActor = left.Actor, secondActor = right.Actor, overlapMilliseconds = (Math.Min(left.End.UtcTicks, right.End.UtcTicks) - Math.Max(left.Start.UtcTicks, right.Start.UtcTicks)) / TimeSpan.TicksPerMillisecond });
            }
        }

        return overlaps;
    }

    private static IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> Gaps(DateTimeOffset first, DateTimeOffset last, IReadOnlyList<Interval> activity, IReadOnlyList<Interval> pauses)
    {
        var active = activity.SelectMany(interval =>
        {
            var intervals = new List<(DateTimeOffset Start, DateTimeOffset End)> { (interval.Start, interval.End) };
            foreach (Interval pause in PausesForActor(pauses, interval.Actor))
            {
                intervals = intervals.SelectMany(slice => pause.End <= slice.Start || pause.Start >= slice.End
                    ? [slice]
                    : new[] { (slice.Start, pause.Start > slice.Start ? pause.Start : slice.Start), (pause.End < slice.End ? pause.End : slice.End, slice.End) }.Where(part => part.Item2 > part.Item1)).ToList();
            }

            return intervals;
        }).OrderBy(interval => interval.Start).ToArray();
        var merged = new List<(DateTimeOffset Start, DateTimeOffset End)>();
        foreach (var interval in active)
        {
            if (merged.Count == 0 || interval.Start > merged[^1].End)
            {
                merged.Add(interval);
            }
            else if (interval.End > merged[^1].End)
            {
                merged[^1] = (merged[^1].Start, interval.End);
            }
        }

        var gaps = new List<(DateTimeOffset Start, DateTimeOffset End)>();
        DateTimeOffset cursor = first;
        foreach (var interval in merged)
        {
            if (interval.Start > cursor)
            {
                gaps.Add((cursor, interval.Start));
            }

            if (interval.End > cursor)
            {
                cursor = interval.End;
            }
        }

        if (cursor < last)
        {
            gaps.Add((cursor, last));
        }

        return gaps;
    }
}
