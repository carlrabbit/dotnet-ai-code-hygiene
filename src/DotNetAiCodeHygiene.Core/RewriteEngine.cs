using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Build.Locator;

namespace DotNetAiCodeHygiene.Core;

internal static class RoslynWorkspaceRegistration
{
    private static readonly object Gate = new();
    public static void EnsureRegistered()
    {
        lock (Gate)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
        }
    }
}

internal interface IRewriteModule
{
    public string Command { get; }
    public Document Rewrite(Document document, Compilation compilation);
    public bool ValidatesResult { get; }
    public void ValidateProject(Compilation compilation, string projectPath, string repositoryRoot);
    public void ValidateResult(Compilation compilation, string projectPath, string repositoryRoot);
}

internal sealed class FormatRewriteModule : IRewriteModule
{
    public string Command => "format";
    public Document Rewrite(Document document, Compilation compilation) => Formatter.FormatAsync(document).GetAwaiter().GetResult();
    public bool ValidatesResult => false;
    public void ValidateProject(Compilation compilation, string projectPath, string repositoryRoot) { }
    public void ValidateResult(Compilation compilation, string projectPath, string repositoryRoot) { }
}

internal sealed class NormalizeRewriteModule : IRewriteModule
{
    public string Command => "normalize";
    public bool ValidatesResult => true;
    public void ValidateProject(Compilation compilation, string projectPath, string repositoryRoot)
    {
        if (RewriteCompilerValidation.HasRepositoryErrors(compilation, repositoryRoot))
        {
            throw new ProductException($"Cannot normalize: project '{projectPath}' has compiler errors.");
        }
    }

    public void ValidateResult(Compilation compilation, string projectPath, string repositoryRoot)
    {
        if (RewriteCompilerValidation.HasRepositoryErrors(compilation, repositoryRoot))
        {
            throw new ProductException($"Normalization would introduce compiler errors in '{projectPath}'; no files were changed.");
        }
    }

    public Document Rewrite(Document document, Compilation compilation)
    {
        SyntaxNode root = document.GetSyntaxRootAsync().GetAwaiter().GetResult()!;
        SemanticModel model = compilation.GetSemanticModel(document.GetSyntaxTreeAsync().GetAwaiter().GetResult()!);
        var candidates = root.DescendantNodesAndSelf().Where(node => node is NameSyntax or MemberAccessExpressionSyntax or ThisExpressionSyntax)
            .Where(node => node is not ThisExpressionSyntax || model.GetTypeInfo(node).Type is not null).ToArray();
        SyntaxNode annotated = root.ReplaceNodes(candidates, (oldNode, rewritten) => rewritten.WithAdditionalAnnotations(Simplifier.Annotation));
        Document updated = document.WithSyntaxRoot(annotated);
        updated = Simplifier.ReduceAsync(updated, Simplifier.Annotation).GetAwaiter().GetResult();
        return Formatter.FormatAsync(updated).GetAwaiter().GetResult();
    }
}

internal static class RewriteCompilerValidation
{
    internal static bool HasRepositoryErrors(Compilation compilation, string repositoryRoot) => compilation.GetDiagnostics().Any(d =>
        d.Severity == DiagnosticSeverity.Error && (d.Location.SourceTree is null
            || Path.GetFullPath(d.Location.SourceTree.FilePath).StartsWith(Path.GetFullPath(repositoryRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)));
}

internal sealed class RewriteRunner(IReadOnlyList<IRewriteModule> modules)
{
    internal IRewriteModule Get(string command) => modules.SingleOrDefault(m => m.Command == command)
        ?? throw new ArgumentException("Unknown rewrite command.");
    internal Document Rewrite(string command, Document document, Compilation compilation) => Get(command).Rewrite(document, compilation);
}

internal static class RewriteCatalog
{
    internal static IReadOnlyList<IRewriteModule> Modules { get; } = [new FormatRewriteModule(), new NormalizeRewriteModule()];
    internal static RewriteRunner Runner { get; } = new(Modules);
}

public sealed record RewriteResult(string Command, bool CheckOnly, int TargetCount, int ChangedCount, int UnchangedCount, IReadOnlyList<string> ChangedPaths);

/// <summary>Plans Roslyn rewrites completely before applying an all-target transaction.</summary>
public sealed class RewriteEngine
{
    private readonly string root;
    private readonly Action? beforeCommit;
    private readonly Action<int>? afterFileCommit;
    private readonly Func<string, string>? plannedTextForTesting;
    public RewriteEngine(string? cwd = null) : this(cwd, null, null) { }
    internal RewriteEngine(string? cwd, Action? beforeCommit, Action<int>? afterFileCommit = null, Func<string, string>? plannedTextForTesting = null)
    {
        this.beforeCommit = beforeCommit;
        this.afterFileCommit = afterFileCommit;
        this.plannedTextForTesting = plannedTextForTesting;
        root = RepositorySession.FindRoot(cwd);
    }

    public RewriteResult Rewrite(string command, string[] paths, bool changed, bool checkOnly)
    {
        IRewriteModule module = RewriteCatalog.Runner.Get(command);
        using var session = new RepositorySession(root);
        string[] targets = session.ResolveTargets(paths, changed);
        var original = targets.ToDictionary(p => p, File.ReadAllBytes, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, (Project Project, Document Document)> assigned = session.AssignTargets(targets, paths);
        var plan = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var changedByProject = new Dictionary<ProjectId, Dictionary<string, string>>();
        foreach (var group in assigned.GroupBy(x => x.Value.Project.Id))
        {
            Project project = group.First().Value.Project;
            Compilation compilation = session.GetCompilation(project);
            module.ValidateProject(compilation, Rel(project.FilePath!), root);

            foreach (var pair in group)
            {
                Document document = pair.Value.Document;
                SourceText source = document.GetTextAsync().GetAwaiter().GetResult();
                Document changedDocument = RewriteCatalog.Runner.Rewrite(command, document, compilation);
                SourceText rewritten = changedDocument.GetTextAsync().GetAwaiter().GetResult();
                string content = plannedTextForTesting?.Invoke(rewritten.ToString()) ?? rewritten.ToString();
                if (!StringComparer.Ordinal.Equals(source.ToString(), content))
                {
                    Encoding encoding = source.Encoding ?? new UTF8Encoding(false);
                    byte[] preamble = encoding.GetPreamble();
                    bool hasPreamble = preamble.Length > 0 && original[pair.Key].AsSpan().StartsWith(preamble);
                    byte[] encoded = encoding.GetBytes(content);
                    plan[pair.Key] = hasPreamble ? preamble.Concat(encoded).ToArray() : encoded;
                    if (!changedByProject.TryGetValue(group.Key, out var files))
                    {
                        changedByProject[group.Key] = files = new(StringComparer.OrdinalIgnoreCase);
                    }

                    files[pair.Key] = content;
                }
            }
        }
        if (module.ValidatesResult && plan.Count > 0)
        {
            foreach (var group in assigned.GroupBy(x => x.Value.Project.Id))
            {
                if (!changedByProject.TryGetValue(group.Key, out var files))
                {
                    continue;
                }

                Project project = group.First().Value.Project;
                Solution solution = project.Solution;
                foreach (var entry in group)
                {
                    if (!files.TryGetValue(entry.Key, out string? text))
                    {
                        continue;
                    }

                    solution = solution.WithDocumentText(entry.Value.Document.Id, SourceText.From(text, Encoding.UTF8));
                }
                Compilation after = solution.GetProject(group.Key)!.GetCompilationAsync().GetAwaiter().GetResult() ?? throw new ProductException("Roslyn could not validate the rewrite plan.");
                module.ValidateResult(after, Rel(project.FilePath!), root);
            }
        }
        string[] changedPaths = plan.Keys.Select(Rel).Order(StringComparer.Ordinal).ToArray();
        if (!checkOnly && plan.Count > 0)
        {
            Commit(plan, original);
        }

        return new(command, checkOnly, targets.Length, plan.Count, targets.Length - plan.Count, changedPaths);
    }

    private void Commit(Dictionary<string, byte[]> plan, Dictionary<string, byte[]> original)
    {
        foreach (var item in original)
        {
            if (!File.ReadAllBytes(item.Key).AsSpan().SequenceEqual(item.Value))
            {
                throw new ProductException($"Source changed during rewrite planning: {Rel(item.Key)}; retry the command.");
            }
        }

        var staged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var committed = new List<string>();
        try
        {
            foreach (var entry in plan)
            {
                string temp = entry.Key + "." + Guid.NewGuid().ToString("N") + ".rewrite.tmp";
                File.WriteAllBytes(temp, entry.Value);
                staged.Add(entry.Key, temp);
            }
            beforeCommit?.Invoke();
            foreach (var item in original)
            {
                if (!File.ReadAllBytes(item.Key).AsSpan().SequenceEqual(item.Value))
                {
                    throw new ProductException($"Source changed during rewrite planning: {Rel(item.Key)}; retry the command.");
                }
            }

            int committedCount = 0;
            foreach (var entry in staged.ToArray())
            {
                string backup = entry.Key + "." + Guid.NewGuid().ToString("N") + ".rewrite.bak";
                File.Replace(entry.Value, entry.Key, backup);
                staged[entry.Key] = backup;
                committed.Add(entry.Key);
                afterFileCommit?.Invoke(++committedCount);
            }
            foreach (string backup in staged.Values)
            {
                if (File.Exists(backup))
                {
                    File.Delete(backup);
                }
            }
        }
        catch
        {
            foreach (string path in committed.AsEnumerable().Reverse())
            {
                string backup = staged[path];
                if (File.Exists(backup))
                {
                    File.Replace(backup, path, null);
                }
            }
            throw;
        }
        finally
        {
            foreach (string temp in staged.Values)
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
        }
    }

    private string Rel(string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
}
