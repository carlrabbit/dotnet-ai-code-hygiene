using System.Collections.Concurrent;
using System.ComponentModel;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Command-scoped owner for repository targets and shared Roslyn project context.</summary>
internal sealed class RepositorySession : IDisposable
{
    private readonly Lazy<MSBuildWorkspace> workspace;
    private readonly Lazy<Project[]> projects;
    private readonly ConcurrentDictionary<ProjectId, Lazy<Compilation>> compilations = new();
    private readonly ConcurrentDictionary<object, Lazy<object>> facts = new();

/// <inheritdoc/>
    internal RepositorySession(string root)
    {
        Root = Path.GetFullPath(root);
        workspace = new Lazy<MSBuildWorkspace>(CreateWorkspace, LazyThreadSafetyMode.ExecutionAndPublication);
        projects = new Lazy<Project[]>(LoadProjects, LazyThreadSafetyMode.ExecutionAndPublication);
    }

/// <inheritdoc/>
    internal static string FindRoot(string? cwd)
    {
        string directory = Path.GetFullPath(cwd ?? Environment.CurrentDirectory);

        while (!Directory.Exists(Path.Combine(directory, ".git")) && !File.Exists(Path.Combine(directory, ".git")))
        {
            directory = Directory.GetParent(directory)?.FullName ?? throw new ProductException("Current directory is not inside a Git repository.");
        }

        return directory;
    }

/// <inheritdoc/>
    internal string Root { get; }
/// <inheritdoc/>
    internal MSBuildWorkspace Workspace => workspace.Value;
/// <inheritdoc/>
    internal IReadOnlyList<Project> Projects => projects.Value;
/// <inheritdoc/>
    internal bool HasWorkspace => workspace.IsValueCreated;
/// <inheritdoc/>
    internal bool HasProjects => projects.IsValueCreated;
/// <inheritdoc/>
    internal int CompilationCount => compilations.Count;
/// <inheritdoc/>
    internal int FactCount => facts.Count;

/// <inheritdoc/>
    internal string[] ResolveTargets(string[] paths, bool changed)
    {
        if (changed && paths.Length > 0)
        {
            throw new ArgumentException("Explicit paths and --changed are mutually exclusive.");
        }

        IEnumerable<string> files;

        if (changed)
        {
            try { files = GitChanged().ToArray(); }
            catch (Exception e) when (e is Win32Exception or IOException) { throw new EnvironmentException("Git is unavailable."); }
        }
        else if (paths.Length == 0)
        {
            files = Directory.EnumerateFiles(Root, "*.cs", SearchOption.AllDirectories);
        }
        else
        {
            var chosen = new List<string>();

            foreach (string raw in paths)
            {
                string full = Path.GetFullPath(Path.IsPathRooted(raw) ? raw : Path.Combine(Root, raw));

                if (!WithinRoot(full))
                {
                    throw new ProductException("Target is outside the repository.");
                }

                if (File.Exists(full))
                {
                    if (!full.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ProductException($"Unsupported explicit file: {raw}");
                    }

                    chosen.Add(full);
                }
                else if (Directory.Exists(full))
                {
                    chosen.AddRange(Directory.EnumerateFiles(full, "*.cs", SearchOption.AllDirectories));
                }
                else
                {
                    throw new ProductException($"Target does not exist: {raw}");
                }
            }
            files = chosen;
        }

        return files.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(File.Exists).Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !Excluded(p))
            .Order(StringComparer.Ordinal).ToArray();
    }

/// <inheritdoc/>
    internal Dictionary<string, (Project Project, Document Document)> AssignTargets(string[] targets, string[] inputPaths)
    {
        var directlyTargeted = inputPaths.Select(raw => Path.GetFullPath(Path.IsPathRooted(raw) ? raw : Path.Combine(Root, raw)))
            .Where(File.Exists).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assigned = new Dictionary<string, (Project Project, Document Document)>(StringComparer.OrdinalIgnoreCase);

        foreach (string path in targets)
        {
            var match = Projects.SelectMany(p => p.Documents.Select(d => (Project: p, Document: d)))
                .Where(x => x.Document.FilePath is not null && Path.GetFullPath(x.Document.FilePath).Equals(path, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => Path.GetFullPath(x.Project.FilePath ?? "").Length).FirstOrDefault();

            if (match.Document is null)
            {
                if (directlyTargeted.Contains(path))
                {
                    throw new ProductException($"C# file is not included by a discoverable SDK-style project: {Relative(path)}");
                }

                continue;
            }
            assigned[path] = match;
        }
        return assigned;
    }

/// <inheritdoc/>
    internal Compilation GetCompilation(Project project) => compilations.GetOrAdd(project.Id,
        _ => new Lazy<Compilation>(() => project.GetCompilationAsync().GetAwaiter().GetResult()
            ?? throw new ProductException($"Roslyn did not produce a compilation for '{Relative(project.FilePath ?? Root)}'."),
            LazyThreadSafetyMode.ExecutionAndPublication)).Value;

/// <inheritdoc/>
    internal T GetFact<T>(object key, Func<T> factory) where T : notnull =>
        (T)facts.GetOrAdd(key, _ => new Lazy<object>(() => factory(), LazyThreadSafetyMode.ExecutionAndPublication)).Value;

/// <inheritdoc/>
    internal string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');
/// <inheritdoc/>
    internal bool WithinRoot(string path) => Path.GetFullPath(path).Equals(Root, StringComparison.OrdinalIgnoreCase)
        || Path.GetFullPath(path).StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private bool Excluded(string path) => Path.GetRelativePath(Root, path).Split(Path.DirectorySeparatorChar).Any(x => x is ".git" or ".hygiene" or "bin" or "obj");

    private IEnumerable<string> GitChanged()
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git") { WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        psi.ArgumentList.Add("status"); psi.ArgumentList.Add("--porcelain"); psi.ArgumentList.Add("-z"); psi.ArgumentList.Add("--untracked-files=all");
        using var process = System.Diagnostics.Process.Start(psi) ?? throw new IOException();
        string output = process.StandardOutput.ReadToEnd(); process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new IOException();
        }

        foreach (string entry in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            if (entry.Length < 4 || entry.StartsWith(" D", StringComparison.Ordinal) || entry.StartsWith("D ", StringComparison.Ordinal))
            {
                continue;
            }

            string relative = entry[3..];

            if (relative.Contains(" -> "))
            {
                relative = relative[(relative.LastIndexOf(" -> ", StringComparison.Ordinal) + 4)..];
            }

            yield return Path.Combine(Root, relative);
        }
    }

    private MSBuildWorkspace CreateWorkspace()
    {
        RoslynWorkspaceRegistration.EnsureRegistered();
        return MSBuildWorkspace.Create(new Dictionary<string, string> { ["DesignTimeBuild"] = "true" });
    }

    private Project[] LoadProjects()
    {
        var loadedProjects = new List<Project>();
        var pending = new Stack<string>(); pending.Push(Root);

        while (pending.Count > 0)
        {
            string directory = pending.Pop();

            foreach (string path in Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly).Order(StringComparer.Ordinal))
            {
                try
                {
                    XDocument document = XDocument.Load(path);

                    if (!SdkProjectDetection.IsSdkStyle(document))
                    {
                        continue;
                    }

                    Project? existing = Workspace.CurrentSolution.Projects.FirstOrDefault(p => Path.GetFullPath(p.FilePath ?? "").Equals(Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase));
                    Project loaded = existing ?? Workspace.OpenProjectAsync(path).GetAwaiter().GetResult();
                    loadedProjects.Add(Workspace.CurrentSolution.GetProject(loaded.Id)!);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    throw new ProductException($"Unable to evaluate SDK-style project '{Relative(path)}': {e.Message}");
                }
            }
            foreach (string child in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
            {
                FileAttributes attributes = File.GetAttributes(child);

                if ((attributes & FileAttributes.ReparsePoint) == 0 && !Excluded(child) && WithinRoot(child))
                {
                    pending.Push(child);
                }
            }
        }
        return loadedProjects.OrderBy(p => p.FilePath, StringComparer.Ordinal).ToArray();
    }

/// <inheritdoc/>
    public void Dispose()
    {
        if (workspace.IsValueCreated)
        {
            workspace.Value.Dispose();
        }
    }
}
