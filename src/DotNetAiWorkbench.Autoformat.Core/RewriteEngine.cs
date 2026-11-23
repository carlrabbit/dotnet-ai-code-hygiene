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
    private readonly Func<string, string, string[], string[], bool, bool, string, (int ExitCode, string Output)>? phaseRunner;

    public RewriteEngine(string? cwd = null, bool requireCleanCheckpoint = true)
    {
        root = RepositorySession.FindRoot(cwd);
        this.requireCleanCheckpoint = requireCleanCheckpoint;
    }
    public RewriteEngine(string? cwd, Func<string, string, string[], string[], bool, bool, string, (int ExitCode, string Output)> phaseRunner)
        : this(cwd, requireCleanCheckpoint: false) => this.phaseRunner = phaseRunner;
    public string Root => root;

    public RewriteResult Rewrite(string command, string[] paths, bool changed, bool checkOnly, bool noRestore = false)
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
            return PreviewNormalization(session, eligible, assigned, selectedRules, noRestore);
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
                    (styleExit, string output) = RunPhase(workspace, "style", files, diagnostics, checkOnly, noRestore, phaseReport);
                    log += output;
                    if (styleExit != 0 && !(checkOnly && styleExit == 2)) { throw Failure("style", styleExit, output, eligible, bytes); }
                }
            }
            foreach ((string workspace, string[] files) in batches)
            {
                string phaseReport = CreatePhaseReportDirectory(reportDirectory, "whitespace", batchIndex++);
                reportDirectories.Add(phaseReport);
                (whitespaceExit, string output) = RunPhase(workspace, "whitespace", files, [], checkOnly, noRestore, phaseReport);
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
        Dictionary<string, (Project Project, Document Document)> assigned, string[] selectedRules, bool noRestore)
    {
        string tempRoot = Path.GetFullPath(Path.GetTempPath());
        EnsureNoReparsePoint(Path.GetPathRoot(tempRoot)!, tempRoot);
        string shadowRoot = Path.Combine(tempRoot, "autoformat-preview-" + Guid.NewGuid().ToString("N"));
        EnsureOwnedDestination(tempRoot, shadowRoot);
        try
        {
            Directory.CreateDirectory(shadowRoot);
            EnsureNoReparsePoint(Path.GetPathRoot(shadowRoot)!, shadowRoot);
            Directory.CreateDirectory(Path.Combine(shadowRoot, ".git"));
            string[] selectedProjects = assigned.Values.Select(value => value.Project.FilePath!)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            bool useSolution = selectedProjects.Length > 1;
            CopyPreviewWorkspace(shadowRoot, ReferencedProjectFiles(session, assigned),
                copyRestoreArtifacts: noRestore, includeSolution: useSolution);

            string[] shadowTargets = eligible
                .Select(path => SafeDestination(shadowRoot, Path.GetRelativePath(root, path)))
                .ToArray();
            var preview = phaseRunner is null
                ? new RewriteEngine(shadowRoot, requireCleanCheckpoint: false)
                : new RewriteEngine(shadowRoot, phaseRunner);
            RewriteResult result = preview.Rewrite("normalize", shadowTargets, changed: false, checkOnly: false, noRestore: noRestore);
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

    private static IEnumerable<string> ReferencedProjectFiles(RepositorySession session,
        Dictionary<string, (Project Project, Document Document)> assigned)
    {
        var pending = new Stack<Project>(assigned.Values.Select(value => value.Project));
        var visited = new HashSet<ProjectId>();
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (pending.Count > 0)
        {
            Project project = pending.Pop();
            if (!visited.Add(project.Id)) { continue; }
            if (project.FilePath is null)
            {
                throw new ProductException("Check-only normalization cannot safely preserve a project without a filesystem path.");
            }
            files.Add(project.FilePath);
            foreach (Document document in project.Documents.Concat(project.AdditionalDocuments))
            {
                if (document.FilePath is not null && !session.WithinRoot(document.FilePath))
                {
                    throw new ProductException($"Check-only normalization cannot safely preserve external project input '{document.FilePath}'. Move it under the repository or run normalize without --check.");
                }
            }
            foreach (ProjectReference reference in project.ProjectReferences)
            {
                Project? referenced = session.Workspace.CurrentSolution.GetProject(reference.ProjectId);
                if (referenced is null)
                {
                    throw new ProductException($"Check-only normalization cannot safely resolve project reference '{reference.ProjectId}'. Run normalize without --check or make the project reference resolvable.");
                }
                pending.Push(referenced);
            }
        }
        return files;
    }

    private void CopyPreviewWorkspace(string shadowRoot, IEnumerable<string> projectFiles, bool copyRestoreArtifacts, bool includeSolution)
    {
        string[] rootSettings = [".editorconfig", ".globalconfig", "global.json", "NuGet.Config", "nuget.config",
            "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props"];
        EnsureNoReparsePoint(Path.GetPathRoot(root)!, root);
        EnsureNoReparsePoint(Path.GetPathRoot(shadowRoot)!, shadowRoot);
        EnsureOwnedDestination(shadowRoot, shadowRoot);
        string[] solutions = includeSolution
            ? Directory.EnumerateFiles(root, "*.sln*", SearchOption.TopDirectoryOnly)
                .Where(path => Path.GetExtension(path) is ".sln" or ".slnx").ToArray()
            : [];
        foreach (string solution in solutions)
        {
            EnsureNoReparsePoint(root, solution);
            File.Copy(solution, SafeDestination(shadowRoot, Path.GetFileName(solution)), overwrite: true);
        }

        var pending = new Stack<string>(projectFiles);
        foreach (string solution in solutions)
        {
            (int exit, string output) = RunSimple("sln", solution, "list");
            if (exit != 0) { throw new ProductException($"Unable to inspect solution project references for isolated check-only preview: {output.Trim()}"); }
            foreach (string project in output.Split(new[] { (char)13, (char)10 }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
            {
                string projectPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(solution)!, project.Trim()));
                RequireProjectInsideRepository(projectPath, solution);
                pending.Push(projectPath);
            }
        }
        var copiedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var projectsToCopy = new List<string>();
        while (pending.Count > 0)
        {
            string projectFile = Path.GetFullPath(pending.Pop());
            RequireProjectInsideRepository(projectFile, root);
            if (!copiedProjects.Add(projectFile)) { continue; }
            if (!File.Exists(projectFile))
            {
                throw new ProductException($"A required project for the isolated check-only preview is missing: {Path.GetRelativePath(root, projectFile)}. Restore the project or remove the stale reference, then rerun.");
            }
            EnsureNoReparsePoint(root, projectFile);
            projectsToCopy.Add(projectFile);
            string projectDirectory = Path.GetDirectoryName(projectFile)!;
            XDocument document = XDocument.Load(projectFile);
            foreach (XElement reference in document.Descendants().Where(element => element.Name.LocalName == "ProjectReference"))
            {
                string? include = (string?)reference.Attribute("Include");
                if (string.IsNullOrWhiteSpace(include)) { continue; }
                if (include.Contains("$(", StringComparison.Ordinal) || include.Contains('*') || include.Contains(';'))
                {
                    throw new ProductException($"The isolated check-only preview cannot safely resolve ProjectReference '{include}' in '{Path.GetRelativePath(root, projectFile)}'. Use a literal in-repository project path or run normalize without --check.");
                }
                string referencedProject = Path.GetFullPath(Path.Combine(projectDirectory, include));
                RequireProjectInsideRepository(referencedProject, projectFile);
                if (!File.Exists(referencedProject))
                {
                    throw new ProductException($"The isolated check-only preview cannot find referenced project '{include}' from '{Path.GetRelativePath(root, projectFile)}'. Restore the project or correct the reference, then rerun.");
                }
                pending.Push(referencedProject);
            }
        }

        var copiedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string projectFile in projectsToCopy)
        {
            string projectDirectory = Path.GetDirectoryName(projectFile)!;
            string relativeProjectDirectory = Path.GetRelativePath(root, projectDirectory);
            string shadowDirectory = SafeDestination(shadowRoot, relativeProjectDirectory);
            CopyProjectDirectory(projectDirectory, shadowDirectory, copiedDirectories, copiedProjects, copyRestoreArtifacts);

            string? ancestor = projectDirectory;
            while (ancestor is not null && IsWithin(root, ancestor))
            {
                foreach (string setting in rootSettings)
                {
                    string settingPath = Path.Combine(ancestor, setting);
                    if (File.Exists(settingPath))
                    {
                        EnsureNoReparsePoint(root, settingPath);
                        string relative = Path.GetRelativePath(root, settingPath);
                        string destination = SafeDestination(shadowRoot, relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                        File.Copy(settingPath, destination, overwrite: true);
                    }
                }

                if (string.Equals(ancestor, root, StringComparison.OrdinalIgnoreCase)) { break; }
                ancestor = Directory.GetParent(ancestor)?.FullName;
            }
        }
    }

    private void CopyProjectDirectory(string source, string destination, HashSet<string> copiedDirectories,
        HashSet<string> requiredProjects, bool copyRestoreArtifacts)
    {
        EnsureNoReparsePoint(root, source);
        EnsureOwnedDestination(Path.GetTempPath(), destination);
        if (!copiedDirectories.Add(source)) { return; }
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.EnumerateFiles(source))
        {
            if (Path.GetExtension(file).Equals(".csproj", StringComparison.OrdinalIgnoreCase)
                && !requiredProjects.Contains(Path.GetFullPath(file))) { continue; }
            EnsureNoReparsePoint(root, file);
            File.Copy(file, SafeDestination(Path.GetTempPath(), Path.Combine(destination, Path.GetFileName(file))), overwrite: true);
        }

        foreach (string directory in Directory.EnumerateDirectories(source))
        {
            string name = Path.GetFileName(directory);
            if (name is ".git" or ".execution" or ".review" or ".vs" or "bin" or "TestResults" or "node_modules"
                || name == "obj" && !copyRestoreArtifacts) { continue; }
            EnsureNoReparsePoint(root, directory);
            CopyProjectDirectory(directory, SafeDestination(Path.GetTempPath(), Path.Combine(destination, name)),
                copiedDirectories, requiredProjects, copyRestoreArtifacts);
        }
    }

    private void RequireProjectInsideRepository(string projectPath, string referringPath)
    {
        string fullPath = Path.GetFullPath(projectPath);
        if (!IsWithin(root, fullPath))
        {
            throw new ProductException($"Check-only normalization cannot safely preview external project '{projectPath}' referenced by '{referringPath}'. Move the project under the repository or run normalize without --check.");
        }
        EnsureNoReparsePoint(root, fullPath);
    }

    private static string SafeDestination(string ownedRoot, string relativeOrFullPath)
    {
        string destination = Path.GetFullPath(Path.IsPathRooted(relativeOrFullPath)
            ? relativeOrFullPath
            : Path.Combine(ownedRoot, relativeOrFullPath));
        EnsureOwnedDestination(ownedRoot, destination);
        return destination;
    }

    private static void EnsureOwnedDestination(string ownedRoot, string destination)
    {
        string rootPath = NormalizeBoundary(ownedRoot);
        string destinationPath = Path.GetFullPath(destination);
        if (!IsWithin(rootPath, destinationPath))
        {
            throw new ProductException($"The isolated check-only preview refused a destination outside its disposable workspace: '{destinationPath}'.");
        }

        string current = rootPath;
        string relative = Path.GetRelativePath(rootPath, destinationPath);
        foreach (string segment in relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (!File.Exists(current) && !Directory.Exists(current)) { continue; }
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                throw new ProductException($"The isolated check-only preview refused a reparse point inside its disposable workspace: '{current}'.");
            }
        }
    }

    private static void EnsureNoReparsePoint(string boundary, string path)
    {
        string fullBoundary = NormalizeBoundary(boundary);
        string fullPath = Path.GetFullPath(path);
        if (!IsWithin(fullBoundary, fullPath))
        {
            throw new ProductException($"Check-only normalization cannot safely inspect a path outside the repository: '{fullPath}'.");
        }

        string current = fullBoundary;
        if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
        {
            throw new ProductException($"Check-only normalization does not support filesystem roots that are reparse points: '{current}'.");
        }
        foreach (string segment in Path.GetRelativePath(fullBoundary, fullPath).Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (!File.Exists(current) && !Directory.Exists(current)) { continue; }
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                throw new ProductException($"Check-only normalization does not support reparse points in copied project context: '{current}'. Remove the link or move the project inside the repository, then rerun.");
            }
        }
    }

    private static bool IsWithin(string boundary, string path)
    {
        string fullBoundary = NormalizeBoundary(boundary);
        string fullPath = Path.GetFullPath(path);
        return fullPath.Equals(fullBoundary, StringComparison.OrdinalIgnoreCase)
            || (fullBoundary.EndsWith(Path.DirectorySeparatorChar) || fullBoundary.EndsWith(Path.AltDirectorySeparatorChar)
                ? fullPath.StartsWith(fullBoundary, StringComparison.OrdinalIgnoreCase)
                : fullPath.StartsWith(fullBoundary + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeBoundary(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string? volumeRoot = Path.GetPathRoot(fullPath);
        return string.Equals(fullPath, volumeRoot, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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

    private (int ExitCode, string Output) RunPhase(string workspace, string phase, string[] files, string[] diagnostics, bool checkOnly, bool noRestore, string reportDirectory)
    {
        if (phaseRunner is not null) { return phaseRunner(workspace, phase, files, diagnostics, checkOnly, noRestore, reportDirectory); }
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
