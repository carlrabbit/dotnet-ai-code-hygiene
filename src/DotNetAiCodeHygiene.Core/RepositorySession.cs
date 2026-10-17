using System.Collections.Concurrent;
using System.ComponentModel;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Command-scoped owner for repository targets and shared Roslyn project context.</summary>
internal sealed class RepositorySession : IDisposable
{
    private readonly Lazy<MSBuildWorkspace> workspace;
    private readonly Lazy<Project[]> projects;
    private readonly ConcurrentDictionary<ProjectId, Lazy<Compilation>> compilations = new();
    private readonly ConcurrentDictionary<object, Lazy<object>> facts = new();
    private readonly ConcurrentDictionary<object, int> factComputations = new();

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
    internal int FactComputations(object key) => factComputations.GetValueOrDefault(key);

/// <inheritdoc/>
    internal string[] ResolveTargets(string[] paths, bool changed, string? baseRef = null)
    {
        if (baseRef is not null && (changed || paths.Length > 0)) { throw new ArgumentException("--base is mutually exclusive with explicit paths and --changed."); }
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
        else if (baseRef is not null)
        {
            try { files = GitChanged(baseRef).ToArray(); }
            catch (ProductException) { throw; }
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
            .Where(File.Exists).Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !Excluded(p) && !GeneratedSource(p))
            .Order(StringComparer.Ordinal).ToArray();
    }

    private static bool GeneratedSource(string path)
    {
        string file = Path.GetFileName(path);
        if (file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)) { return true; }
        using var reader = new StreamReader(path);
        for (int line = 0; line < 5 && reader.ReadLine() is string text; line++)
        {
            if (text.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase)) { return true; }
        }
        return false;
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
        (T)facts.GetOrAdd(key, _ => new Lazy<object>(() =>
        {
            factComputations.AddOrUpdate(key, 1, static (_, count) => count + 1);
            return factory();
        }, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

/// <inheritdoc/>
    internal string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');
/// <inheritdoc/>
    internal bool WithinRoot(string path) => Path.GetFullPath(path).Equals(Root, StringComparison.OrdinalIgnoreCase)
        || Path.GetFullPath(path).StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private bool Excluded(string path) => Path.GetRelativePath(Root, path).Split(Path.DirectorySeparatorChar).Any(x => x is ".git" or ".hygiene" or "bin" or "obj");

    internal string[] ChangedPaths(bool changed, string? baseRef)
    {
        try
        {
            IEnumerable<string> values = baseRef is not null ? GitChanged(baseRef) : changed ? GitChanged() : [];
            return values.Select(Path.GetFullPath).Where(WithinRoot).Where(p => File.Exists(p) && p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !Excluded(p))
                .Select(Relative).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        }
        catch (ProductException) { throw; }
        catch (Exception e) when (e is Win32Exception or IOException) { throw new EnvironmentException("Git is unavailable."); }
    }

    internal Dictionary<string, HashSet<int>> ChangedLineNumbers(string[] paths, bool changed, string? baseRef)
    {
        var result = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        string reference = baseRef is not null ? MergeBase(baseRef) : HeadId();
        foreach (string relative in paths)
        {
            string full = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
            var lines = new HashSet<int>();
            bool untracked = Git("status", "--porcelain", "--", relative).Split('\n', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith("??", StringComparison.Ordinal));
            if (untracked || reference == "UNBORN")
            {
                lines.UnionWith(Enumerable.Range(1, File.ReadAllLines(full).Length));
            }
            else
            {
                string diff = Git("diff", "--no-ext-diff", "--unified=0", reference, "--", relative);
                SourceText? sourceText = null;
                SyntaxNode? syntaxRoot = null;
                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(diff, "(?m)^@@ -(\\d+)(?:,(\\d+))? \\+(\\d+)(?:,(\\d+))? @@"))
                {
                    int start = int.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    int count = match.Groups[4].Success ? int.Parse(match.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture) : 1;
                    for (int line = start; line < start + count; line++) { lines.Add(line); }
                    if (count != 0) { continue; }

                    // A deletion has no added line to overlap a surviving declaration.
                    // Attribute its insertion point only when that point is strictly
                    // inside a surviving type; a top-level declaration removed between
                    // types or at EOF must not make its neighbor look changed.
                    sourceText ??= SourceText.From(File.ReadAllText(full));
                    syntaxRoot ??= CSharpSyntaxTree.ParseText(sourceText).GetRoot();
                    int newStart = int.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    int position = newStart == 0
                        ? 0
                        : sourceText.Lines[Math.Min(newStart - 1, sourceText.Lines.Count - 1)].EndIncludingLineBreak;
                    if (position > 0 && position < sourceText.Length && syntaxRoot.DescendantNodes().OfType<TypeDeclarationSyntax>()
                        .Any(declaration => declaration.SpanStart < position && position < declaration.Span.End))
                    {
                        lines.Add(sourceText.Lines.GetLineFromPosition(position).LineNumber + 1);
                    }
                }
            }
            result[relative] = lines;
        }
        return result;
    }

    internal string HeadId()
    {
        try { return Git("rev-parse", "--verify", "HEAD").Trim(); }
        catch (IOException e) when (e.Message.Contains("Needed a single revision", StringComparison.Ordinal) || e.Message.Contains("unknown revision", StringComparison.Ordinal)) { return "UNBORN"; }
        catch (Exception e) when (e is Win32Exception or IOException) { throw new EnvironmentException("Git is unavailable."); }
    }
    internal string MergeBase(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) { throw new ArgumentException("--base requires a Git ref."); }
        try { return Git("merge-base", reference, "HEAD").Trim(); }
        catch (IOException) { throw new ProductException($"Git ref '{reference}' is invalid or has no merge base with HEAD."); }
    }

    private IEnumerable<string> GitChanged(string? baseRef = null)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (baseRef is null)
        {
            string[] entries = Git("status", "--porcelain", "-z", "--untracked-files=all").Split('\0', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i];
                if (entry.Length < 4 || entry.StartsWith(" D", StringComparison.Ordinal) || entry.StartsWith("D ", StringComparison.Ordinal)) { continue; }
                string relative = entry[3..];
                if (entry.StartsWith("R", StringComparison.Ordinal) || entry.StartsWith("C", StringComparison.Ordinal)) { i++; }
                paths.Add(Path.GetFullPath(Path.Combine(Root, relative)));
            }
        }
        else
        {
            string merge = MergeBase(baseRef);
            foreach (string path in Git("diff", "--name-only", "--no-renames", "--diff-filter=ACMRTUXB", "-z", merge, "--").Split('\0', StringSplitOptions.RemoveEmptyEntries))
            { paths.Add(Path.GetFullPath(Path.Combine(Root, path))); }
            foreach (string path in GitChanged()) { paths.Add(path); }
        }
        return paths.Where(File.Exists).Order(StringComparer.Ordinal);
    }

    private string Git(params string[] arguments)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git") { WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string argument in arguments) { psi.ArgumentList.Add(argument); }
        using var process = System.Diagnostics.Process.Start(psi) ?? throw new IOException();
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(); Task<string> errorTask = process.StandardError.ReadToEndAsync(); process.WaitForExit();
        Task.WaitAll(outputTask, errorTask); string output = outputTask.Result; string error = errorTask.Result;
        if (process.ExitCode != 0) { throw new IOException(error); }
        return output;
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
