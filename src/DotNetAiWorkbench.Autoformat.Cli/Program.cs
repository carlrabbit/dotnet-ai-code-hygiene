using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using DotNetAiWorkbench.Autoformat.Core;

var root = new RootCommand("Independent .NET source formatting tool.");
var output = new Option<string>("--output") { DefaultValueFactory = _ => "text" };
var version = new Option<bool>("--version") { Description = "Show the Autoformat package version." };
root.Options.Add(version);
var paths = new Argument<string[]>("paths") { Arity = ArgumentArity.ZeroOrMore };
foreach (string mode in new[] { "format", "normalize" })
{
    var command = mode == "format" ? new Command("format", "Apply SDK whitespace formatting only.") : new Command("normalize", "Canonicalize C# source, apply SDK style fixes, then format whitespace.");
    var check = new Option<bool>("--check") { Description = "Verify without writing." };
    var changed = new Option<bool>("--changed") { Description = "Select staged, unstaged, and untracked C# files; implies --check." };
    command.Arguments.Add(paths);
    command.Options.Add(check);
    command.Options.Add(changed);
    command.Options.Add(output);
    command.SetAction(parse => Run(() => Render(mode, parse.GetValue(paths) ?? [], parse.GetValue(check), parse.GetValue(changed), parse.GetValue(output) ?? "text")));
    root.Subcommands.Add(command);
}
var init = new Command("init", "Establish or validate the fixed SDK mechanical formatting policy.");
init.Options.Add(output);
init.SetAction(parse => Run(() => Initialize(parse.GetValue(output) ?? "text")));
root.Subcommands.Add(init);
var defaultPaths = new Argument<string[]>("paths") { Arity = ArgumentArity.ZeroOrMore };
var defaultCheck = new Option<bool>("--check") { Description = "Verify without writing." };
var defaultChanged = new Option<bool>("--changed") { Description = "Select staged, unstaged, and untracked C# files; implies --check." };
root.Arguments.Add(defaultPaths);
root.Options.Add(defaultCheck);
root.Options.Add(defaultChanged);
root.Options.Add(output);
root.SetAction(parse => Run(() => parse.GetValue(version) ? ShowVersion() : Render("normalize", parse.GetValue(defaultPaths) ?? [], parse.GetValue(defaultCheck), parse.GetValue(defaultChanged), parse.GetValue(output) ?? "text")));
return await root.Parse(args).InvokeAsync();

static int ShowVersion() { Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0"); return 0; }
static int Run(Func<int> action) { try { return action(); } catch (Exception e) { Console.Error.WriteLine(e.Message); return 2; } }
static int Render(string mode, string[] paths, bool check, bool changed, string output)
{
    ValidateOutput(output);
    string repo = FindRoot();
    EnsurePolicy(repo);
    RewriteResult result = new RewriteEngine(repo).Rewrite(mode, paths, changed, check);
    var report = new { status = "complete", mode, checkOnly = result.CheckOnly, targetCount = result.TargetCount, changedCount = result.ChangedCount, selectedPaths = result.SelectedPaths, changedPaths = result.ChangedPaths, phases = mode == "format" ? new[] { "sdk-whitespace" } : new[] { "csharp-canonicalization", "sdk-style", "sdk-whitespace" }, selectedRules = result.SelectedRuleIds, notices = result.Notices };
    if (output == "json")
    {
        Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }
    else
    {
        Console.WriteLine($"{mode}: {result.ChangedCount} changed of {result.TargetCount} selected; check-only={result.CheckOnly}.");
        Console.WriteLine("phases: " + string.Join(", ", mode == "format" ? ["sdk-whitespace"] : ["csharp-canonicalization", "sdk-style", "sdk-whitespace"]));
        foreach (string notice in result.Notices)
        {
            Console.WriteLine("notice: " + notice);
        }
    }

    return 0;
}
static int Initialize(string output)
{
    ValidateOutput(output);
    string repo = FindRoot(), path = Path.Combine(repo, ".editorconfig");
    string before = File.Exists(path) ? File.ReadAllText(path) : "";
    bool hasAutoStart = before.Contains(AutoformatPolicy.StartMarker, StringComparison.Ordinal);
    bool changed = AutoformatPolicy.EnsureInitialized(repo);
    EnsurePolicy(repo);
    if (hasAutoStart)
    {
        EnsurePolicy(repo);
        if (output == "json")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { status = "ready", changedPaths = changed ? new[] { ".editorconfig" } : Array.Empty<string>() }));
        }
        else
        {
            Console.WriteLine("The existing fixed SDK mechanical policy is ready for Autoformat.");
        }

        return 0;
    }

    if (output == "json")
    {
        Console.WriteLine(JsonSerializer.Serialize(new { status = "initialized", changedPaths = new[] { ".editorconfig" } }));
    }
    else
    {
        Console.WriteLine("Initialized fixed SDK mechanical policy in .editorconfig.");
    }

    return 0;
}
static void EnsurePolicy(string repo)
{
    string path = Path.Combine(repo, ".editorconfig");
    string text = File.Exists(path) ? File.ReadAllText(path) : "";
    string start = AutoformatPolicy.StartMarker;
    string end = AutoformatPolicy.EndMarker;
    int a = text.IndexOf(start, StringComparison.Ordinal), b = text.IndexOf(end, StringComparison.Ordinal);
    if (a < 0 || b < a)
    {
        throw new ProductException("No supported SDK formatting policy is initialized. Run 'dotnet autoformat init' first.");
    }

    string block = text[a..(b + end.Length)];
    foreach ((string key, string value) in AutoformatPolicy.MechanicalOptions)
    {
        bool present = block.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries).Any(line =>
        {
            int equals = line.IndexOf('=');
            return equals > 0 && line[..equals].Trim().Equals(key, StringComparison.OrdinalIgnoreCase) && line[(equals + 1)..].Trim().Equals(value, StringComparison.OrdinalIgnoreCase);
        });
        if (!present)
        {
            throw new ProductException($"The managed Autoformat policy is missing or has drifted at '{key}'. Repair .editorconfig or rerun 'dotnet autoformat init' before formatting.");
        }
    }
    AutoformatPolicy.ValidateEffectiveMechanicalSettings(repo);
}
static void ValidateOutput(string output)
{
    if (output is not ("text" or "json"))
    {
        throw new ArgumentException("--output must be 'text' or 'json'.");
    }
}
static string FindRoot()
{
    var directory = new DirectoryInfo(Environment.CurrentDirectory);
    while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")) && !File.Exists(Path.Combine(directory.FullName, ".git")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new ProductException("Current directory is not inside a Git repository.");
}
