using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace DotNetAiCodeHygiene.Core;

public sealed record RewriteResult(string Command, bool CheckOnly, int TargetCount, int ChangedCount, int UnchangedCount, IReadOnlyList<string> ChangedPaths, IReadOnlyList<string> SelectedRuleIds);

/// <summary>Runs the supported SDK formatter in repository-wide batches and reports its actual source changes.</summary>
public sealed class RewriteEngine
{
    internal static IReadOnlyList<string> ApprovedDiagnostics { get; } = ["IDE0005", "IDE0011", "IDE0036", "IDE0040", "IDE0090", "IDE0034", "IDE0004", "IDE0071", "IDE0037"];
    private readonly string root;
    private readonly Func<string, string, string[], string[], bool, string, (int ExitCode, string Output)>? phaseRunner;

    public RewriteEngine(string? cwd = null)
    {
        root = RepositorySession.FindRoot(cwd);
    }

    internal RewriteEngine(string? cwd, Func<string, string, string[], string[], bool, string, (int ExitCode, string Output)> phaseRunner)
        : this(cwd) => this.phaseRunner = phaseRunner;
    public string Root => root;

    public RewriteResult Rewrite(string command, string[] paths, bool changed, bool checkOnly)
    {
        if (command is not ("format" or "normalize")) { throw new ArgumentException("Unknown rewrite command."); }
        using var session = new RepositorySession(root);
        string[] targets = session.ResolveTargets(paths, changed);
        Dictionary<string, (Project Project, Document Document)> assigned = session.AssignTargets(targets, paths);
        string[] eligible = assigned.Keys.Order(StringComparer.Ordinal).ToArray();
        string[] diagnostics = command == "normalize" ? ApprovedDiagnostics.ToArray() : [];
        string[] selectedRules = command == "format"
            ? ["format.csharp.sdk.whitespace"]
            : ["format.csharp.sdk.whitespace", .. ApprovedDiagnostics.Select(id => "sdk." + id)];

        if (eligible.Length == 0)
        {
            return new(command, checkOnly, 0, 0, 0, [], selectedRules);
        }

        var bytes = eligible.ToDictionary(path => path, File.ReadAllBytes, StringComparer.OrdinalIgnoreCase);
        string head = checkOnly ? "" : TryGit(root, "rev-parse", "--verify", "HEAD").Trim();
        string index = checkOnly || head.Length == 0 ? "" : Git(root, "diff", "--cached", "--binary");
        string reportDirectory = Path.Combine(Path.GetTempPath(), "hygiene-dotnet-format-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(reportDirectory);
        int styleExit = 0, whitespaceExit = 0;
        string log = "";
        try
        {
            IReadOnlyList<(string Workspace, string[] Files)> batches = FindBatches(session, assigned);
            if (command == "normalize")
            {
                foreach ((string workspace, string[] files) in batches)
                {
                    (styleExit, string output) = RunPhase(workspace, "style", files, diagnostics, checkOnly, reportDirectory);
                    log += output;
                    if (styleExit != 0 && !(checkOnly && styleExit == 2)) { throw Failure("style", styleExit, output, eligible, bytes); }
                }
            }
            foreach ((string workspace, string[] files) in batches)
            {
                (whitespaceExit, string output) = RunPhase(workspace, "whitespace", files, [], checkOnly, reportDirectory);
                log += output;
                if (whitespaceExit != 0 && !(checkOnly && whitespaceExit == 2)) { throw Failure("whitespace", whitespaceExit, output, eligible, bytes); }
            }

            string[] changedPaths = checkOnly
                ? ReadReportedChanges(reportDirectory, log).Where(candidate => eligible.Any(path => string.Equals(Path.GetFullPath(path), Path.GetFullPath(candidate), StringComparison.OrdinalIgnoreCase))).Select(session.Relative).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()
                : eligible.Where(path => !bytes[path].AsSpan().SequenceEqual(File.ReadAllBytes(path)))
                    .Select(session.Relative).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            string afterHead = checkOnly || head.Length == 0 ? "" : Git(root, "rev-parse", "--verify", "HEAD").Trim();
            string afterIndex = checkOnly || head.Length == 0 ? "" : Git(root, "diff", "--cached", "--binary");
            if (!checkOnly && head.Length > 0 && (afterHead != head || afterIndex != index))
            {
                throw new ProductException("Git HEAD or index changed during formatting. The run is incomplete; inspect the worktree and preserve the current edits before recovery.");
            }
            int changedCount = changedPaths.Length;
            return new(command, checkOnly, eligible.Length, changedCount, Math.Max(0, eligible.Length - changedCount), changedPaths, selectedRules);
        }
        finally
        {
            try { Directory.Delete(reportDirectory, recursive: true); } catch (IOException) { }
        }
    }

    private ProductException Failure(string phase, int exitCode, string output, string[] eligible, IReadOnlyDictionary<string, byte[]> original)
    {
        string[] partial = eligible.Where(path => File.Exists(path) && !original[path].AsSpan().SequenceEqual(File.ReadAllBytes(path)))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).ToArray();
        return new ProductException($"dotnet format {phase} failed with exit code {exitCode}; formatting is incomplete and edits remain inspectable. Inspect/recover these paths explicitly: {string.Join(", ", partial)}. {output.Trim()}");
    }

    private (int ExitCode, string Output) RunPhase(string workspace, string phase, string[] files, string[] diagnostics, bool checkOnly, string reportDirectory)
    {
        if (phaseRunner is not null) { return phaseRunner(workspace, phase, files, diagnostics, checkOnly, reportDirectory); }
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add("format");
        start.ArgumentList.Add(workspace);
        start.ArgumentList.Add(phase);
        start.ArgumentList.Add("--no-restore");
        start.ArgumentList.Add("--report");
        start.ArgumentList.Add(reportDirectory);
        foreach (string file in files)
        {
            start.ArgumentList.Add("--include");
            start.ArgumentList.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
        }
        if (diagnostics.Length > 0)
        {
            start.ArgumentList.Add("--diagnostics");
            foreach (string diagnostic in diagnostics) { start.ArgumentList.Add(diagnostic); }
        }
        if (checkOnly) { start.ArgumentList.Add("--verify-no-changes"); }
        try
        {
            using Process process = Process.Start(start) ?? throw new EnvironmentException("Could not start the installed .NET SDK dotnet format command.");
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            Task.WaitAll(stdout, stderr);
            return (process.ExitCode, stdout.Result + Environment.NewLine + stderr.Result);
        }
        catch (Win32Exception error) { throw new EnvironmentException("The .NET SDK dotnet format command is unavailable: " + error.Message); }
    }

    private IReadOnlyList<(string Workspace, string[] Files)> FindBatches(RepositorySession session, Dictionary<string, (Project Project, Document Document)> assigned)
    {
        string[] projects = assigned.Values.Select(value => value.Project.FilePath).Where(path => path is not null).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (string solution in Directory.EnumerateFiles(root, "*.sln*", SearchOption.TopDirectoryOnly).Where(path => Path.GetExtension(path) is ".sln" or ".slnx"))
        {
            (int exit, string output) = RunSimple("sln", solution, "list");
            if (exit != 0) { continue; }
            var listed = output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .Select(line => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(solution)!, line.Trim())))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (projects.All(listed.Contains))
            {
                return [(solution, assigned.Keys.Order(StringComparer.Ordinal).ToArray())];
            }
        }
        return assigned.GroupBy(pair => pair.Value.Project.FilePath!, StringComparer.OrdinalIgnoreCase)
            .Select(group => (group.Key, group.Select(pair => pair.Key).Order(StringComparer.Ordinal).ToArray())).ToArray();
    }

    private (int ExitCode, string Output) RunSimple(params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (string argument in arguments) { start.ArgumentList.Add(argument); }
        using Process process = Process.Start(start) ?? throw new EnvironmentException("Could not start dotnet.");
        string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd(); process.WaitForExit();
        return (process.ExitCode, output);
    }

    private static string[] ReadReportedChanges(string directory, string output)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string report in Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                using JsonDocument json = JsonDocument.Parse(File.ReadAllText(report));
                Visit(json.RootElement);
                void Visit(JsonElement value)
                {
                    if (value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (JsonProperty property in value.EnumerateObject())
                        {
                            if (property.Name.Contains("file", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                            {
                                paths.Add(property.Value.GetString()!);
                            }
                            else { Visit(property.Value); }
                        }
                    }
                    else if (value.ValueKind == JsonValueKind.Array) { foreach (JsonElement child in value.EnumerateArray()) { Visit(child); } }
                }
            }
            catch (JsonException) { }
        }
        foreach (string line in output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            Match diagnostic = Regex.Match(line, "^(?<path>.*?\\.cs)\\(\\d+,\\d+\\):", RegexOptions.CultureInvariant);
            if (diagnostic.Success) { paths.Add(diagnostic.Groups["path"].Value); continue; }
            string candidate = line.Trim().Trim('"');
            if (candidate.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) { paths.Add(candidate); }
        }
        return paths.ToArray();
    }

    private static string Git(string workingDirectory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = workingDirectory, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (string argument in arguments) { start.ArgumentList.Add(argument); }
        using Process process = Process.Start(start) ?? throw new EnvironmentException("Could not start Git.");
        string output = process.StandardOutput.ReadToEnd(); string error = process.StandardError.ReadToEnd(); process.WaitForExit();
        if (process.ExitCode != 0) { throw new EnvironmentException("Git checkpoint inspection failed: " + error.Trim()); }
        return output;
    }

    private static string TryGit(string workingDirectory, params string[] arguments)
    {
        try { return Git(workingDirectory, arguments); }
        catch (EnvironmentException) { return ""; }
    }
}
