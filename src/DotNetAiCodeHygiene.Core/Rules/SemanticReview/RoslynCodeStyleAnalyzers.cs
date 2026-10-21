using System.Reflection;
using System.Runtime.Loader;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNetAiCodeHygiene.Core;

/// <summary>Loads the pinned public Roslyn code-style analyzers shipped with the hygiene tool.</summary>
internal static class RoslynCodeStyleAnalyzers
{
    private const string AssemblyName = "Microsoft.CodeAnalysis.CSharp.CodeStyle.dll";
    private const string FixAssemblyName = "Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes.dll";
    private static readonly Lazy<ImmutableArray<DiagnosticAnalyzer>> AllAnalyzers = new(LoadAnalyzers, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<CodeFixProvider> UnusedImportsFixProvider = new(LoadUnusedImportsFixProvider, LazyThreadSafetyMode.ExecutionAndPublication);

    internal static ImmutableArray<DiagnosticAnalyzer> ForDiagnostics(params string[] diagnosticIds)
    {
        HashSet<string> requested = diagnosticIds.ToHashSet(StringComparer.Ordinal);
        ImmutableArray<DiagnosticAnalyzer> analyzers = AllAnalyzers.Value
            .Where(analyzer => analyzer.SupportedDiagnostics.Any(descriptor => requested.Contains(descriptor.Id)))
            .ToImmutableArray();
        if (analyzers.IsEmpty)
        {
            throw new ProductException($"The pinned Roslyn code-style analyzer package does not expose diagnostics {string.Join(", ", requested)}.");
        }
        return analyzers;
    }

    private static ImmutableArray<DiagnosticAnalyzer> LoadAnalyzers()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "RoslynAnalyzers", AssemblyName);
        if (!File.Exists(path)) { throw new ProductException($"The Roslyn code-style analyzer assembly is missing at '{path}'. Reinstall the hygiene tool package."); }
        var reference = new AnalyzerFileReference(path, new RuntimeAnalyzerAssemblyLoader(Path.GetDirectoryName(path)!));
        List<string> loadFailures = [];
        reference.AnalyzerLoadFailed += (_, args) => loadFailures.Add(args.Message);
        ImmutableArray<DiagnosticAnalyzer> analyzers = reference.GetAnalyzers(LanguageNames.CSharp).ToImmutableArray();
        if (analyzers.IsEmpty)
        {
            string detail = loadFailures.Count == 0 ? "No load failure was reported." : string.Join(" ", loadFailures);
            throw new ProductException($"The Roslyn code-style analyzer assembly at '{path}' exposed no C# analyzers. {detail}");
        }
        return analyzers;
    }

    internal static async Task<Document> ApplyUnusedImportsFixAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), cancellationToken);
        await UnusedImportsFixProvider.Value.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        foreach (CodeAction action in actions)
        {
            ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            ApplyChangesOperation? change = operations.OfType<ApplyChangesOperation>().FirstOrDefault();
            if (change?.ChangedSolution.GetDocument(document.Id) is Document changed) { return changed; }
        }
        return document;
    }

    private static CodeFixProvider LoadUnusedImportsFixProvider()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "RoslynAnalyzers");
        string path = Path.Combine(folder, FixAssemblyName);
        if (!File.Exists(path)) { throw new ProductException($"The Roslyn code-style fixer assembly is missing at '{path}'. Reinstall the hygiene tool package."); }
        var loader = new RuntimeAnalyzerAssemblyLoader(folder);
        Type providerType = loader.LoadFromPath(path).GetTypes()
            .SingleOrDefault(type => !type.IsAbstract && !type.ContainsGenericParameters && typeof(CodeFixProvider).IsAssignableFrom(type)
                && type.Name == "CSharpRemoveUnnecessaryImportsCodeFixProvider")
            ?? throw new ProductException($"Roslyn's pinned IDE0005 code fixer is not available in '{path}'.");
        return (CodeFixProvider)(Activator.CreateInstance(providerType)
            ?? throw new ProductException("Roslyn's IDE0005 code fixer could not be created."));
    }

    private sealed class RuntimeAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        private readonly AssemblyLoadContext context = AssemblyLoadContext.Default;
        private readonly string directory;

        internal RuntimeAnalyzerAssemblyLoader(string directory)
        {
            this.directory = directory;
            context.Resolving += Resolve;
        }

        public void AddDependencyLocation(string fullPath) { }
        public Assembly LoadFromPath(string fullPath) => context.LoadFromAssemblyPath(Path.GetFullPath(fullPath));

        private Assembly? Resolve(AssemblyLoadContext _, AssemblyName assemblyName)
        {
            string candidate = Path.Combine(directory, assemblyName.Name + ".dll");
            return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
        }
    }
}
