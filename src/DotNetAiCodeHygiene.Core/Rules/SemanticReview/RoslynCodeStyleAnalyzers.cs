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
    private static readonly ImmutableHashSet<string> SupportedFixDiagnostics = ["IDE0005", "IDE0007", "IDE0008"];
    private static readonly Lazy<ImmutableArray<DiagnosticAnalyzer>> AllAnalyzers = new(LoadAnalyzers, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<ImmutableDictionary<string, CodeFixProvider>> CodeFixProviders = new(LoadCodeFixProviders, LazyThreadSafetyMode.ExecutionAndPublication);

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

    internal static ImmutableArray<Diagnostic> Analyze(
        Compilation compilation,
        AnalyzerOptions analyzerOptions,
        params string[] diagnosticIds) => compilation.WithAnalyzers(ForDiagnostics(diagnosticIds), analyzerOptions)
            .GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();

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

    internal static async Task<RoslynCodeFixPreview?> PreviewCodeFixAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), cancellationToken);
        if (!CodeFixProviders.Value.TryGetValue(diagnostic.Id, out CodeFixProvider? provider))
        {
            throw new ProductException($"The pinned Roslyn code-style package does not expose a code fix for diagnostic '{diagnostic.Id}'.");
        }
        await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        foreach (CodeAction action in actions.OrderBy(action => action.Title, StringComparer.Ordinal))
        {
            ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            ApplyChangesOperation? change = operations.OfType<ApplyChangesOperation>().FirstOrDefault();
            if (change?.ChangedSolution.GetDocument(document.Id) is Document changed)
            {
                string text = changed.GetTextAsync().GetAwaiter().GetResult().ToString();
                return new RoslynCodeFixPreview(action.Title, changed, text);
            }
        }
        return null;
    }

    private static ImmutableDictionary<string, CodeFixProvider> LoadCodeFixProviders()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "RoslynAnalyzers");
        var loader = new RuntimeAnalyzerAssemblyLoader(folder);
        var providers = new Dictionary<string, CodeFixProvider>(StringComparer.Ordinal);
        foreach (string assemblyName in new[] { "Microsoft.CodeAnalysis.CodeStyle.Fixes.dll", "Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes.dll" })
        {
            string path = Path.Combine(folder, assemblyName);
            if (!File.Exists(path)) { throw new ProductException($"The Roslyn code-style fixer assembly is missing at '{path}'. Reinstall the hygiene tool package."); }
            foreach (Type type in loader.LoadFromPath(path).GetTypes()
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters && typeof(CodeFixProvider).IsAssignableFrom(type)))
            {
                CodeFixProvider? provider;
                try { provider = Activator.CreateInstance(type) as CodeFixProvider; }
                catch (Exception exception) when (exception is ArgumentException or TargetInvocationException or MissingMethodException) { continue; }
                if (provider is null) { continue; }
                foreach (string diagnosticId in provider.FixableDiagnosticIds.Where(SupportedFixDiagnostics.Contains))
                {
                    providers.TryAdd(diagnosticId, provider);
                }
            }
        }
        return providers.ToImmutableDictionary(StringComparer.Ordinal);
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

internal sealed record RoslynCodeFixPreview(string Title, Document ChangedDocument, string Text);
