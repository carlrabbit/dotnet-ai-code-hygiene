using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using DotNetAiWorkbench.AgentTrace;

var root = new RootCommand("Local append-only event recorder for agent sessions. It never executes recorded commands. Use caller-selected session and optional actor keys.");
var version = new Option<bool>("--version") { Description = "Show AgentTrace version." };
root.Options.Add(version);
root.SetAction(parse => parse.GetValue(version) ? ShowVersion() : 0);

Option<string> Session() => new("--session") { Required = true, Description = "Caller-selected session key." };
Option<string?> Actor() => new("--actor") { Description = "Optional caller-selected actor key." };
Option<string?> Repo() => new("--repo") { Description = "Repository root; defaults to the current Git worktree." };
Option<string?> Optional(string name, string description) => new(name) { Description = description };
Option<string> Required(string name, string description) => new(name) { Required = true, Description = description };
void Handle(Command command, Func<ParseResult, Task<int>> action) => command.SetAction(parse => Catch(() => action(parse)));
void AddLifecycle(string name, string description)
{
    var command = new Command(name, description);
    var session = Session(); var actor = Actor(); var repo = Repo();
    command.Options.Add(session); command.Options.Add(actor); command.Options.Add(repo);
    if (name == "mark")
    {
        var note = Required("--note", "Marker note."); command.Options.Add(note);
        Handle(command, p => Record(() => new TraceService(p.GetValue(repo)).RecordMarker(p.GetValue(session)!, p.GetValue(actor), name, p.GetValue(note)!)));
    }
    else
    {
        var note = Optional("--note", "Optional lifecycle annotation."); command.Options.Add(note);
        Handle(command, p => Record(() => new TraceService(p.GetValue(repo)).RecordMarker(p.GetValue(session)!, p.GetValue(actor), name, p.GetValue(note))));
    }

    root.Subcommands.Add(command);
}

AddLifecycle("start", "Append a session-start annotation. Does not register or gate a session.");
AddLifecycle("pause", "Append an actor-specific or session-level pause annotation.");
AddLifecycle("resume", "Append an actor-specific or session-level resume annotation.");
AddLifecycle("stop", "Append a session-stop annotation. Recording remains available afterwards.");
AddLifecycle("mark", "Append a timestamped note.");

var commandGroup = new Command("command", "Record commands executed independently by the agent.");
var commandStart = new Command("start", "Append the exact command text and its deterministic content fingerprint.");
{
    var session = Session(); var actor = Actor(); var repo = Repo();
    var text = Required("--command", "Exact full command text; this command is recorded but never executed.");
    var label = Optional("--label", "Optional safe disambiguating label, repeated on the matching end.");
    var sensitive = new Option<bool>("--sensitive") { Description = "Store only the unkeyed SHA-256 fingerprint and sensitivity flag; --label is optional." };
    commandStart.Options.Add(session); commandStart.Options.Add(actor); commandStart.Options.Add(repo); commandStart.Options.Add(text); commandStart.Options.Add(label); commandStart.Options.Add(sensitive);
    Handle(commandStart, p => Record(() => new TraceService(p.GetValue(repo)).RecordCommandStart(p.GetValue(session)!, p.GetValue(actor), p.GetValue(text)!, p.GetValue(label), p.GetValue(sensitive))));
}
var commandEnd = new Command("end", "Append a command result and correlate by exact text fingerprint, actor and optional label.");
{
    var session = Session(); var actor = Actor(); var repo = Repo();
    var text = Required("--command", "Exact full command text used for fingerprint correlation; it is not stored on the end event.");
    var label = Optional("--label", "Optional label supplied at command start.");
    var sensitive = new Option<bool>("--sensitive") { Description = "Flag the fingerprint as sensitive; command text is never stored on end events." };
    var exitCode = new Option<int?>("--exit-code") { Description = "Reported command exit code." };
    var status = Optional("--status", "Reported completion status, such as complete, failed, interrupted, or incomplete.");
    commandEnd.Options.Add(session); commandEnd.Options.Add(actor); commandEnd.Options.Add(repo); commandEnd.Options.Add(text); commandEnd.Options.Add(label); commandEnd.Options.Add(sensitive); commandEnd.Options.Add(exitCode); commandEnd.Options.Add(status);
    Handle(commandEnd, p => Record(() => new TraceService(p.GetValue(repo)).RecordCommandEnd(p.GetValue(session)!, p.GetValue(actor), p.GetValue(text)!, p.GetValue(label), p.GetValue(sensitive), p.GetValue(exitCode), p.GetValue(status))));
}
commandGroup.Subcommands.Add(commandStart); commandGroup.Subcommands.Add(commandEnd); root.Subcommands.Add(commandGroup);

var operationGroup = new Command("operation", "Record agent-declared external or MCP operations.");
foreach (string edge in new[] { "start", "end" })
{
    var command = new Command(edge, edge == "start" ? "Append a declared operation start." : "Append a declared operation end; no operation ID is generated.");
    var session = Session(); var actor = Actor(); var repo = Repo();
    var kind = Required("--kind", "external or mcp."); var name = Required("--name", "Caller-supplied operation name."); var label = Optional("--label", "Optional safe FIFO disambiguating label.");
    command.Options.Add(session); command.Options.Add(actor); command.Options.Add(repo); command.Options.Add(kind); command.Options.Add(name); command.Options.Add(label);
    Option<string?> status = Optional("--status", "Agent-reported operation outcome.");
    if (edge == "end")
    {
        command.Options.Add(status);
    }

    Handle(command, p => Record(() => new TraceService(p.GetValue(repo)).RecordOperation(p.GetValue(session)!, p.GetValue(actor), edge, p.GetValue(kind)!, p.GetValue(name)!, p.GetValue(label), edge == "end" ? p.GetValue(status) : null)));
    operationGroup.Subcommands.Add(command);
}
root.Subcommands.Add(operationGroup);

var sessions = new Command("sessions", "Discover session keys in the selected store.");
var sessionsRepo = Repo(); var sessionsFormat = new Option<string>("--format") { DefaultValueFactory = _ => "text", Description = "text or json." };
sessions.Options.Add(sessionsRepo); sessions.Options.Add(sessionsFormat);
Handle(sessions, p => Task.FromResult(WriteSessions(new TraceService(p.GetValue(sessionsRepo)).Sessions(), p.GetValue(sessionsFormat) ?? "text")));
root.Subcommands.Add(sessions);

foreach (string mode in new[] { "report", "export" })
{
    var read = new Command(mode, mode == "report" ? "Render a human chronological report." : "Export raw versioned events and labeled derived interpretations.");
    var session = Session(); var repo = Repo();
    var format = new Option<string>("--format") { DefaultValueFactory = _ => mode == "report" ? "text" : "json", Description = mode == "report" ? "text or markdown." : "json or jsonl." };
    var output = Optional("--out", "Write to a file instead of stdout.");
    read.Options.Add(session); read.Options.Add(repo); read.Options.Add(format); read.Options.Add(output);
    Handle(read, p => Task.FromResult(Render(mode, new TraceService(p.GetValue(repo)), p.GetValue(session)!, p.GetValue(format) ?? (mode == "report" ? "text" : "json"), p.GetValue(output))));
    root.Subcommands.Add(read);
}

return await root.Parse(args).InvokeAsync();

static int ShowVersion() { Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0"); return 0; }
static Task<int> Record(Action action) { action(); return Task.FromResult(0); }
static Task<int> Catch(Func<Task<int>> action) => CatchAsync(action);
static async Task<int> CatchAsync(Func<Task<int>> action)
{
    try { return await action().ConfigureAwait(false); }
    catch (Exception ex) { Console.Error.WriteLine(ex is TraceException ? ex.Message : $"AgentTrace error: {ex.Message}"); return 2; }
}
static int WriteSessions(IReadOnlyList<TraceSessionSummary> sessions, string format)
{
    if (format == "json")
    {
        Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, sessions }, new JsonSerializerOptions(TraceJson.Options) { WriteIndented = true }));
    }
    else if (format == "text")
    {
        foreach (TraceSessionSummary item in sessions)
        {
            Console.WriteLine($"{item.Session}\tactors={string.Join(',', item.Actors)}\tevents={item.EventCount}\twindow={item.FirstEventUtc:O}..{item.LastEventUtc:O}");
        }
    }
    else
    {
        throw new TraceException("sessions --format must be text or json.");
    }

    return 0;
}
static int Render(string mode, TraceService service, string session, string format, string? output)
{
    string content = mode == "report" ? format switch { "text" => service.Report(session, false), "markdown" => service.Report(session, true), _ => throw new TraceException("report --format must be text or markdown.") } : format switch { "json" => service.ExportJson(session), "jsonl" => service.ExportJsonl(session), _ => throw new TraceException("export --format must be json or jsonl.") };
    if (string.IsNullOrWhiteSpace(output))
    {
        Console.Write(content);
    }
    else
    {
        File.WriteAllText(Path.GetFullPath(output), content, new System.Text.UTF8Encoding(false));
    }

    return 0;
}
