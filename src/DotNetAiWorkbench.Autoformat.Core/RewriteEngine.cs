using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace DotNetAiWorkbench.Autoformat.Core;

public sealed record RewriteResult(string Command, bool CheckOnly, int TargetCount, int ChangedCount, int UnchangedCount, IReadOnlyList<string> SelectedPaths, IReadOnlyList<string> ChangedPaths, IReadOnlyList<string> SelectedRuleIds, IReadOnlyList<string> Notices);

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

    public RewriteResult Rewrite(string command, string[] paths, bool changed, bool checkOnly)
    {
        if (command is not ("format" or "normalize")) { throw new ArgumentException("Unknown rewrite command."); }
        checkOnly |= changed;
        using var session = new RepositorySession(root);
        string[] targets = session.ResolveTargets(paths, changed);
        Dictionary<string, (Project Project, Document Document)> assigned = session.AssignTargets(targets, paths);
        string[] eligible = assigned.Keys.Order(StringComparer.Ordinal).ToArray();
        string[] diagnostics = command == "normalize" ? ApprovedDiagnostics.ToArray() : [];
        string[] selectedRules = command == "format"
            ? ["format.csharp.sdk.whitespace"]
            : ["canonicalize.csharp", .. CSharpCanonicalizer.Rules, "format.csharp.sdk.whitespace", .. ApprovedDiagnostics.Select(id => "sdk." + id)];

        if (eligible.Length == 0)
        {
            return new(command, checkOnly, 0, 0, 0, [], [], selectedRules, []);
        }

        // Check normalization against the exact mutating pipeline in an isolated source
        // snapshot. SDK style/whitespace edits and the final syntax/comment passes can
        // otherwise disagree with verify-only reports from an intermediate phase.
        if (checkOnly && command == "normalize")
        {
            return PreviewNormalization(session, eligible, assigned, selectedRules);
        }

        var bytes = eligible.ToDictionary(path => path, File.ReadAllBytes, StringComparer.OrdinalIgnoreCase);
        if (!checkOnly && requireCleanCheckpoint)
        {
            string status = Git(root, "status", "--porcelain", "--untracked-files=all");
            if (status.Length > 0) { throw new ProductException("Mutating Autoformat runs require a clean Git worktree and index. Commit or explicitly restore intended changes, then rerun."); }
        }
        string head = checkOnly ? "" : TryGit(root, "rev-parse", "--verify", "HEAD").Trim();
        if (!checkOnly && requireCleanCheckpoint && head.Length == 0) { throw new ProductException("Mutating Autoformat runs require a known Git HEAD checkpoint."); }
        string index = checkOnly || head.Length == 0 ? "" : Git(root, "diff", "--cached", "--binary");
        string reportDirectory = Path.Combine(Path.GetTempPath(), "autoformat-dotnet-format-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(reportDirectory);
        int styleExit = 0, whitespaceExit = 0, batchIndex = 0;
        var reportDirectories = new List<string>();
        string log = "";
        var canonicalChanges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                        canonicalChanges.Add(path);
                        if (!checkOnly)
                        {
                            byte[] body = sourceEncoding.GetBytes(normalized);
                            byte[] encoded = hadBom ? sourceEncoding.GetPreamble().Concat(body).ToArray() : body;
                            File.WriteAllBytes(path, encoded);
                        }
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
                    reportDirectories.Add(phaseReport);
                    (styleExit, string output) = RunPhase(workspace, "style", files, diagnostics, checkOnly, phaseReport);
                    log += output;
                    if (styleExit != 0 && !(checkOnly && styleExit == 2)) { throw Failure("style", styleExit, output, eligible, bytes); }
                }
            }
            foreach ((string workspace, string[] files) in batches)
            {
                string phaseReport = CreatePhaseReportDirectory(reportDirectory, "whitespace", batchIndex++);
                reportDirectories.Add(phaseReport);
                (whitespaceExit, string output) = RunPhase(workspace, "whitespace", files, [], checkOnly, phaseReport);
                log += output;
                if (whitespaceExit != 0 && !(checkOnly && whitespaceExit == 2)) { throw Failure("whitespace", whitespaceExit, output, eligible, bytes); }
            }

            // Reapply only syntax layout after SDK whitespace so the SDK formatter cannot undo
            // opinionated layouts such as a complete vertical parameter list.
            if (command == "normalize" && !checkOnly)
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

            if (!checkOnly)
            {
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
            }

            string[] changedPaths = checkOnly
                ? ReadReportedChanges(reportDirectories, log).Where(candidate => eligible.Any(path => string.Equals(Path.GetFullPath(path), Path.GetFullPath(candidate), StringComparison.OrdinalIgnoreCase))).Concat(canonicalChanges).Select(session.Relative).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()
                : eligible.Where(path => !bytes[path].AsSpan().SequenceEqual(File.ReadAllBytes(path)))
                    .Select(session.Relative).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            string afterHead = checkOnly || head.Length == 0 ? "" : Git(root, "rev-parse", "--verify", "HEAD").Trim();
            string afterIndex = checkOnly || head.Length == 0 ? "" : Git(root, "diff", "--cached", "--binary");
            if (!checkOnly && head.Length > 0 && (afterHead != head || afterIndex != index))
            {
                throw new ProductException("Git HEAD or index changed during formatting. The run is incomplete; inspect the worktree and preserve the current edits before recovery.");
            }
            int changedCount = changedPaths.Length;
            return new(command, checkOnly, eligible.Length, changedCount, Math.Max(0, eligible.Length - changedCount), eligible.Select(session.Relative).ToArray(), changedPaths, selectedRules, notices);
        }
        finally
        {
            try { Directory.Delete(reportDirectory, recursive: true); } catch (IOException) { }
        }
    }

    private RewriteResult PreviewNormalization(RepositorySession session, string[] eligible,
        Dictionary<string, (Project Project, Document Document)> assigned, string[] selectedRules)
    {
        string shadowRoot = Path.Combine(Path.GetTempPath(), "autoformat-preview-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(shadowRoot);
            Directory.CreateDirectory(Path.Combine(shadowRoot, ".git"));
            CopyPreviewWorkspace(shadowRoot, assigned.Values.Select(value => value.Project.FilePath!)
                .Distinct(StringComparer.OrdinalIgnoreCase));

            string[] shadowTargets = eligible
                .Select(path => Path.Combine(shadowRoot, Path.GetRelativePath(root, path)))
                .ToArray();
            var preview = phaseRunner is null
                ? new RewriteEngine(shadowRoot, requireCleanCheckpoint: false)
                : new RewriteEngine(shadowRoot, phaseRunner);
            RewriteResult result = preview.Rewrite("normalize", shadowTargets, changed: false, checkOnly: false);
            return result with
            {
                CheckOnly = true,
                SelectedPaths = eligible.Select(session.Relative).ToArray(),
                ChangedPaths = result.ChangedPaths,
                SelectedRuleIds = selectedRules
            };
        }
        finally
        {
            try { Directory.Delete(shadowRoot, recursive: true); } catch (IOException) { }
        }
    }

    private void CopyPreviewWorkspace(string shadowRoot, IEnumerable<string> projectFiles)
    {
        string[] rootSettings = [".editorconfig", ".globalconfig", "global.json", "NuGet.Config", "nuget.config",
            "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props"];
        var pending = new Stack<string>(projectFiles);
        var copiedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var copiedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (pending.Count > 0)
        {
            string projectFile = Path.GetFullPath(pending.Pop());
            if (!copiedProjects.Add(projectFile) || !File.Exists(projectFile))
            {
                continue;
            }

            string projectDirectory = Path.GetDirectoryName(projectFile)!;
            string relativeProjectDirectory = Path.GetRelativePath(root, projectDirectory);
            string shadowDirectory = Path.GetFullPath(Path.Combine(shadowRoot, relativeProjectDirectory));
            CopyProjectDirectory(projectDirectory, shadowDirectory, copiedDirectories);

            string? ancestor = projectDirectory;
            while (ancestor is not null && (string.Equals(ancestor, root, StringComparison.OrdinalIgnoreCase)
                || ancestor.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (string setting in rootSettings)
                {
                    string settingPath = Path.Combine(ancestor, setting);
                    if (File.Exists(settingPath))
                    {
                        string relative = Path.GetRelativePath(root, settingPath);
                        string destination = Path.Combine(shadowRoot, relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                        File.Copy(settingPath, destination, overwrite: true);
                    }
                }

                if (string.Equals(ancestor, root, StringComparison.OrdinalIgnoreCase)) { break; }
                ancestor = Directory.GetParent(ancestor)?.FullName;
            }

            XDocument document = XDocument.Load(projectFile);
            foreach (XElement reference in document.Descendants().Where(element => element.Name.LocalName == "ProjectReference"))
            {
                string? include = (string?)reference.Attribute("Include");
                if (string.IsNullOrWhiteSpace(include) || include.Contains("$(", StringComparison.Ordinal)) { continue; }
                string referencedProject = Path.GetFullPath(Path.Combine(projectDirectory, include));
                if (referencedProject.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    pending.Push(referencedProject);
                }
            }
        }
    }

    private void CopyProjectDirectory(string source, string destination, HashSet<string> copiedDirectories)
    {
        if (!copiedDirectories.Add(source)) { return; }
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }

        foreach (string directory in Directory.EnumerateDirectories(source))
        {
            string name = Path.GetFileName(directory);
            if (name is ".git" or ".execution" or ".review" or ".vs" or "bin" or "TestResults" or "node_modules") { continue; }
            CopyProjectDirectory(directory, Path.Combine(destination, name), copiedDirectories);
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

    private static string[] ReadReportedChanges(IEnumerable<string> directories, string output)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string directory in directories)
        {
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
        }
        foreach (string line in output.Split(new[] { (char)13, (char)10 }, StringSplitOptions.RemoveEmptyEntries))
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
