using System.ComponentModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace DotNetAiWorkbench.Autoformat.Core;

public sealed record RewriteResult(string Command, int TargetCount, int ChangedCount, int UnchangedCount, IReadOnlyList<string> SelectedPaths, IReadOnlyList<string> ChangedPaths, IReadOnlyList<string> SelectedRuleIds, IReadOnlyList<string> Notices);

/// <summary>Runs the supported SDK formatter in repository-wide batches and reports its actual source changes.</summary>
public sealed class RewriteEngine
{
    public static IReadOnlyList<string> ApprovedDiagnostics => AutoformatPolicy.ApprovedDiagnostics;
    private readonly string root;
    private readonly bool requireCleanCheckpoint;
    private readonly Func<string, string, string[], string[], bool, string, (int ExitCode, string Output)>? phaseRunner;

    public RewriteEngine(string? cwd = null, bool requireCleanCheckpoint = true)
    {
        root = RepositorySession.FindRoot(cwd);
        this.requireCleanCheckpoint = requireCleanCheckpoint;
    }
    public RewriteEngine(string? cwd, Func<string, string, string[], string[], bool, string, (int ExitCode, string Output)> phaseRunner)
        : this(cwd, requireCleanCheckpoint: false) => this.phaseRunner = phaseRunner;
    public string Root => root;

    public RewriteResult Rewrite(string command, string[] paths, bool noRestore = false)
    {
        if (command is not ("format" or "normalize")) { throw new ArgumentException("Unknown rewrite command."); }
        using var session = new RepositorySession(root);
        string[] targets = session.ResolveTargets(paths);
        Dictionary<string, (Project Project, Document Document)> assigned = session.AssignTargets(targets, paths);
        string[] eligible = assigned.Keys.Order(StringComparer.Ordinal).ToArray();
        string[] diagnostics = command == "normalize" ? ApprovedDiagnostics.ToArray() : [];
        string[] selectedRules = command == "format"
            ? ["format.csharp.sdk.whitespace"]
            : ["canonicalize.csharp", .. CSharpCanonicalizer.Rules, "format.csharp.sdk.whitespace", .. ApprovedDiagnostics.Select(id => "sdk." + id)];

        if (eligible.Length == 0)
        {
            return new(command, 0, 0, 0, [], [], selectedRules, []);
        }

        var bytes = eligible.ToDictionary(path => path, File.ReadAllBytes, StringComparer.OrdinalIgnoreCase);
        if (requireCleanCheckpoint)
        {
            string status = Git(root, "status", "--porcelain", "--untracked-files=all");
            if (status.Length > 0) { throw new ProductException("Mutating Autoformat runs require a clean Git worktree and index. Commit or explicitly restore intended changes, then rerun."); }
        }
        string head = TryGit(root, "rev-parse", "--verify", "HEAD").Trim();
        if (requireCleanCheckpoint && head.Length == 0) { throw new ProductException("Mutating Autoformat runs require a known Git HEAD checkpoint."); }
        string index = head.Length == 0 ? "" : Git(root, "diff", "--cached", "--binary");
        string reportDirectory = Path.Combine(Path.GetTempPath(), "autoformat-dotnet-format-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(reportDirectory);
        int batchIndex = 0;
        var notices = new List<string>();
        var retainedCommentIndentation = new Dictionary<string, IReadOnlyList<CSharpCanonicalizer.RetainedCommentIndentation>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (command == "normalize")
            {
                foreach (string path in eligible)
                {
                    byte[] sourceBytes = File.ReadAllBytes(path);
                    string source;
                    System.Text.Encoding sourceEncoding;
                    using (var reader = new StreamReader(new MemoryStream(sourceBytes), detectEncodingFromByteOrderMarks: true))
                    {
                        source = reader.ReadToEnd();
                        sourceEncoding = reader.CurrentEncoding;
                    }
                    retainedCommentIndentation[path] = CSharpCanonicalizer.CaptureRetainedCommentIndentation(source);

                    bool hadBom = sourceEncoding.GetPreamble().Length > 0 && sourceBytes.AsSpan().StartsWith(sourceEncoding.GetPreamble());
                    string normalized = CSharpCanonicalizer.Normalize(source, assigned[path].Project.ParseOptions as Microsoft.CodeAnalysis.CSharp.CSharpParseOptions, out IReadOnlyList<string> fileNotices);
                    notices.AddRange(fileNotices.Select(message => session.Relative(path) + ": " + message));
                    if (!string.Equals(source, normalized, StringComparison.Ordinal))
                    {
                        byte[] body = sourceEncoding.GetBytes(normalized);
                        byte[] encoded = hadBom ? sourceEncoding.GetPreamble().Concat(body).ToArray() : body;
                        File.WriteAllBytes(path, encoded);
                    }
                }
            }
            else
            {
                foreach (string path in eligible)
                {
                    byte[] sourceBytes = File.ReadAllBytes(path);
                    using var reader = new StreamReader(new MemoryStream(sourceBytes), detectEncodingFromByteOrderMarks: true);
                    retainedCommentIndentation[path] = CSharpCanonicalizer.CaptureRetainedCommentIndentation(reader.ReadToEnd());
                }
            }
            IReadOnlyList<(string Workspace, string[] Files)> batches = FindBatches(session, assigned);
            if (command == "normalize")
            {
                foreach ((string workspace, string[] files) in batches)
                {
                    string phaseReport = CreatePhaseReportDirectory(reportDirectory, "style", batchIndex++);
                    (int styleExit, string output) = RunPhase(workspace, "style", files, diagnostics, noRestore, phaseReport);
                    if (styleExit != 0) { throw Failure("style", styleExit, output, eligible, bytes); }
                }
            }
            foreach ((string workspace, string[] files) in batches)
            {
                string phaseReport = CreatePhaseReportDirectory(reportDirectory, "whitespace", batchIndex++);
                (int whitespaceExit, string output) = RunPhase(workspace, "whitespace", files, [], noRestore, phaseReport);
                if (whitespaceExit != 0) { throw Failure("whitespace", whitespaceExit, output, eligible, bytes); }
            }

            // Reapply only syntax layout after SDK whitespace so the SDK formatter cannot undo
            // opinionated layouts such as a complete vertical parameter list.
            if (command == "normalize")
            {
                foreach (string path in eligible)
                {
                    byte[] sourceBytes = File.ReadAllBytes(path);
                    string source;
                    System.Text.Encoding sourceEncoding;
                    using (var reader = new StreamReader(new MemoryStream(sourceBytes), detectEncodingFromByteOrderMarks: true))
                    {
                        source = reader.ReadToEnd();
                        sourceEncoding = reader.CurrentEncoding;
                    }

                    bool hadBom = sourceEncoding.GetPreamble().Length > 0 && sourceBytes.AsSpan().StartsWith(sourceEncoding.GetPreamble());
                    string normalized = CSharpCanonicalizer.NormalizeLayout(source, out IReadOnlyList<string> fileNotices);
                    notices.AddRange(fileNotices.Select(message => session.Relative(path) + ": " + message));
                    if (!string.Equals(source, normalized, StringComparison.Ordinal))
                    {
                        byte[] body = sourceEncoding.GetBytes(normalized);
                        byte[] encoded = hadBom ? sourceEncoding.GetPreamble().Concat(body).ToArray() : body;
                        File.WriteAllBytes(path, encoded);
                    }
                }
            }

            foreach (string path in eligible)
            {
                if (retainedCommentIndentation[path].Count == 0)
                {
                    continue;
                }

                byte[] sourceBytes = File.ReadAllBytes(path);
                string source;
                System.Text.Encoding sourceEncoding;
                using (var reader = new StreamReader(new MemoryStream(sourceBytes), detectEncodingFromByteOrderMarks: true))
                {
                    source = reader.ReadToEnd();
                    sourceEncoding = reader.CurrentEncoding;
                }

                string restored = CSharpCanonicalizer.RestoreRetainedCommentIndentation(source, retainedCommentIndentation[path]);
                if (!string.Equals(source, restored, StringComparison.Ordinal))
                {
                    bool hadBom = sourceEncoding.GetPreamble().Length > 0 && sourceBytes.AsSpan().StartsWith(sourceEncoding.GetPreamble());
                    byte[] body = sourceEncoding.GetBytes(restored);
                    File.WriteAllBytes(path, hadBom ? sourceEncoding.GetPreamble().Concat(body).ToArray() : body);
                }
            }

            string[] changedPaths = eligible.Where(path => !bytes[path].AsSpan().SequenceEqual(File.ReadAllBytes(path)))
                    .Select(session.Relative).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            string afterHead = head.Length == 0 ? "" : Git(root, "rev-parse", "--verify", "HEAD").Trim();
            string afterIndex = head.Length == 0 ? "" : Git(root, "diff", "--cached", "--binary");
            if (head.Length > 0 && (afterHead != head || afterIndex != index))
            {
                throw new ProductException("Git HEAD or index changed during formatting. The run is incomplete; inspect the worktree and preserve the current edits before recovery.");
            }
            int changedCount = changedPaths.Length;
            return new(command, eligible.Length, changedCount, Math.Max(0, eligible.Length - changedCount), eligible.Select(session.Relative).ToArray(), changedPaths, selectedRules, notices);
        }
        finally
        {
            try { Directory.Delete(reportDirectory, recursive: true); } catch (IOException) { }
        }
    }

    private static string CreatePhaseReportDirectory(string root, string phase, int index)
    {
        string directory = Path.Combine(root, $"{index:D3}-{phase}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private ProductException Failure(string phase, int exitCode, string output, string[] eligible, IReadOnlyDictionary<string, byte[]> original)
    {
        string[] partial = eligible.Where(path => File.Exists(path) && !original[path].AsSpan().SequenceEqual(File.ReadAllBytes(path)))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).ToArray();
        return new ProductException($"dotnet format {phase} failed with exit code {exitCode}; formatting is incomplete and edits remain inspectable. Inspect/recover these paths explicitly: {string.Join(", ", partial)}. {output.Trim()}");
    }

    private (int ExitCode, string Output) RunPhase(string workspace, string phase, string[] files, string[] diagnostics, bool noRestore, string reportDirectory)
    {
        if (phaseRunner is not null) { return phaseRunner(workspace, phase, files, diagnostics, noRestore, reportDirectory); }
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add("format");
        start.ArgumentList.Add(workspace);
        start.ArgumentList.Add(phase);
        if (noRestore) { start.ArgumentList.Add("--no-restore"); }
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
            var listed = output.Split(new[] { (char)13, (char)10 }, StringSplitOptions.RemoveEmptyEntries)
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
