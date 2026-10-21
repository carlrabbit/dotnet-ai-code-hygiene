using DotNetAiCodeHygiene.Core;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class M0010CapabilityTests
{
    [Test]
    public async Task CanonicalCatalogueOwnsEveryCapabilityAndProjectionKeyOnce()
    {
        string[] ids = RuleCatalog.All.Select(rule => rule.Id).ToArray();
        string expected = string.Join("|", new[]
        {
            "profile.dotnet.analysis.required", "profile.stylecop.prohibited", "style.braces.required", "style.accessibility.explicit",
            "format.csharp.roslyn", "style.qualification.this.unnecessary", "style.qualification.redundant", "style.object-creation.target-typed", "style.default.literal", "style.cast.redundant", "style.interpolation.redundant", "style.names.inferred", "docs.summary.required",
            "docs.xml.consistent", "docs.text.sentence", "docs.summary.quality.review", "docs.summary.language.german.review",
            "readability.long-line.review", "readability.control-flow.visual-block", "architecture.boringness.review",
            "readability.type-responsibility.review", "tests.quality.review", "comments.intent.review"
        });
        await Assert.That(string.Join("|", ids)).IsEqualTo(expected);
        await Assert.That(ids.Length).IsEqualTo(23);
        await Assert.That(RuleCatalog.Modules.OfType<IEditorConfigProjectionRule>().SelectMany(rule => rule.EditorConfigEntries)
            .Select(entry => entry.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count()).IsEqualTo(4);
        await Assert.That(RuleCatalog.Modules.OfType<IEditorConfigProjectionRule>().SelectMany(rule => rule.EditorConfigEntries)
            .GroupBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase).Any(group => group.Select(entry => entry.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)).IsFalse();
        foreach (IRuleModule module in RuleCatalog.Modules)
        {
            Rule descriptor = RuleCatalog.Get(module.Descriptor.Id);
            await Assert.That(descriptor.FormatRemediate).IsEqualTo(module is IFormatRemediationRule);
            await Assert.That(descriptor.NormalizeRemediate).IsEqualTo(module is INormalizeRemediationRule);
            await Assert.That(descriptor.EditorConfigProject).IsEqualTo(module is IEditorConfigProjectionRule);
        }
    }

    [Test]
    public async Task RuleTogglesControlRewritesProjectionsAndV1MigrationTransactionally()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-m0010-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, ".editorconfig"), "root = true\n[*.cs]\ncsharp_prefer_braces = true\ndotnet_diagnostic.IDE0011.severity = error\n# consumer-owned preexisting content\n");
            string source = Path.Combine(repo, "src", "App.cs");
            string originalSource = "/// <summary>Sample.</summary>\npublic class App{public string Read(){return this.Value;}public string Value=>System.String.Empty; }";
            await File.WriteAllTextAsync(source, originalSource);
            ProfileManager profiles = new(repo);
            _ = profiles.Bootstrap();
            string editorPath = Path.Combine(repo, ".editorconfig");
            await File.AppendAllTextAsync(editorPath, "\n# consumer-owned text\n[*.md]\ntrim_trailing_whitespace = true\n");

            var engine = new HygieneEngine(repo);
            engine.SetRule("style.braces.required", false);
            string disabledEditor = await File.ReadAllTextAsync(editorPath);
            await Assert.That(disabledEditor.Contains("# hygiene rule: style.braces.required", StringComparison.Ordinal)).IsFalse();
            await Assert.That(disabledEditor.Contains("[*.md]\ntrim_trailing_whitespace = true", StringComparison.Ordinal)).IsTrue();
            string userOwnedPrefix = disabledEditor.Split("<!-- hygiene profile:begin -->", StringSplitOptions.None)[0];
            await Assert.That(userOwnedPrefix).Contains("csharp_prefer_braces = true");
            await Assert.That(userOwnedPrefix).Contains("# consumer-owned preexisting content");
            string configBeforeFailure = await File.ReadAllTextAsync(Path.Combine(repo, ".hygiene", "config.json"));
            string editorBeforeFailure = disabledEditor;
            bool failed = false;
            try { new HygieneEngine(repo, () => throw new OperationCanceledException("injected toggle failure")).SetRule("style.accessibility.explicit", false); }
            catch (OperationCanceledException) { failed = true; }
            await Assert.That(failed).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(Path.Combine(repo, ".hygiene", "config.json"))).IsEqualTo(configBeforeFailure);
            await Assert.That(await File.ReadAllTextAsync(editorPath)).IsEqualTo(editorBeforeFailure);
            engine.SetRule("style.braces.required", true);
            IEditorConfigProjectionRule braces = RuleCatalog.Modules.OfType<IEditorConfigProjectionRule>().Single(module => ((IRuleModule)module).Descriptor.Id == "style.braces.required");
            await Assert.That(new ProfileManager(repo).AnalyzeEditorConfigRule("style.braces.required", braces.EditorConfigEntries)).IsEmpty();
            engine.SetRule("style.accessibility.explicit", false);
            string noAccessibility = await File.ReadAllTextAsync(editorPath);
            await Assert.That(noAccessibility.Contains("# hygiene rule: style.accessibility.explicit", StringComparison.Ordinal)).IsFalse();
            await Assert.That(noAccessibility.Contains("dotnet_style_require_accessibility_modifiers = always", StringComparison.Ordinal)).IsFalse();
            engine.SetRule("style.accessibility.explicit", true);

            engine.SetRule("format.csharp.roslyn", false);
            RewriteResult skippedFormat = new RewriteEngine(repo).Rewrite("format", [], false, true);
            await Assert.That(skippedFormat.SelectedRuleIds).IsEmpty();
            await Assert.That(skippedFormat.ChangedCount).IsEqualTo(0);
            engine.SetRule("format.csharp.roslyn", true);
            RewriteResult formatted = new RewriteEngine(repo).Rewrite("format", [], false, true);
            await Assert.That(string.Join("|", formatted.SelectedRuleIds)).IsEqualTo("format.csharp.roslyn");

            engine.SetRule("style.qualification.this.unnecessary", false);
            engine.SetRule("style.qualification.redundant", true);
            RewriteResult thisOff = new RewriteEngine(repo).Rewrite("normalize", [], false, false);
            string thisOffSource = await File.ReadAllTextAsync(source);
            await Assert.That(thisOff.SelectedRuleIds.Contains("style.qualification.this.unnecessary")).IsFalse();
            await Assert.That(thisOffSource).Contains("this.Value");
            await Assert.That(thisOffSource).DoesNotContain("System.String.Empty");
            engine.SetRule("style.qualification.this.unnecessary", true);
            await File.WriteAllTextAsync(source, originalSource);
            _ = new RewriteEngine(repo).Rewrite("normalize", [], false, false);
            string normalized = await File.ReadAllTextAsync(source);
            await Assert.That(normalized).Contains("/// <summary>Sample.</summary>");
            await Assert.That(normalized).DoesNotContain("this.Value");
            await Assert.That(normalized).DoesNotContain("System.String.Empty");

            await File.WriteAllTextAsync(editorPath, "root = true\n\n<!-- hygiene profile:begin -->\n[*.cs]\ncsharp_prefer_braces = true\ndotnet_diagnostic.IDE0011.severity = error\ndotnet_style_require_accessibility_modifiers = always\ndotnet_diagnostic.IDE0040.severity = error\n<!-- hygiene profile:end -->\n\n# consumer-owned text\n[*.md]\ntrim_trailing_whitespace = true\n");
            await File.WriteAllTextAsync(Path.Combine(repo, ".hygiene", "profile.json"), "{\n  \"schemaVersion\": 1,\n  \"profile\": \"dotnet-11\",\n  \"version\": 1\n}\n");
            ProfileResult update = new ProfileManager(repo).Update();
            await Assert.That(update.ChangedPaths.Contains(".hygiene/profile.json")).IsTrue();
            await Assert.That((await File.ReadAllTextAsync(Path.Combine(repo, ".hygiene", "profile.json"))).Contains("\"version\": 2", StringComparison.Ordinal)).IsTrue();
            string migratedEditor = await File.ReadAllTextAsync(editorPath);
            await Assert.That(migratedEditor.Contains("# hygiene rule: style.braces.required", StringComparison.Ordinal)).IsTrue();
            await Assert.That(migratedEditor.Contains("# hygiene rule: style.accessibility.explicit", StringComparison.Ordinal)).IsTrue();
            await Assert.That(migratedEditor.IndexOf("# hygiene rule: style.braces.required", StringComparison.Ordinal) < migratedEditor.IndexOf("# hygiene rule: style.accessibility.explicit", StringComparison.Ordinal)).IsTrue();
            await Assert.That(migratedEditor.Contains("# consumer-owned text\n[*.md]\ntrim_trailing_whitespace = true", StringComparison.Ordinal)).IsTrue();
            await Assert.That(new ProfileManager(repo).Update().ChangedPaths).IsEmpty();
        }
        finally
        {
            DeleteTree(repo);
        }
    }

    [Test]
    public async Task NormalizeDoesNotFormatAlreadyNormalizedPoorlyFormattedDocuments()
    {
        (string repo, string source) = await CreateRewriteFixture("hygiene-normalize-no-structural-change", "public class App{public int Value=>1;}");
        try
        {
            byte[] before = await File.ReadAllBytesAsync(source);
            var engine = new RewriteEngine(repo);
            RewriteResult formatCheck = engine.Rewrite("format", [], false, true);
            await Assert.That(formatCheck.ChangedCount).IsEqualTo(1);

            RewriteResult normalizeCheck = engine.Rewrite("normalize", [], false, true);
            await Assert.That(normalizeCheck.ChangedCount).IsEqualTo(0);
            await Assert.That((await File.ReadAllBytesAsync(source)).AsSpan().SequenceEqual(before)).IsTrue();
            RewriteResult normalize = engine.Rewrite("normalize", [], false, false);
            await Assert.That(normalize.ChangedCount).IsEqualTo(0);
            await Assert.That((await File.ReadAllBytesAsync(source)).AsSpan().SequenceEqual(before)).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task NormalizeDoesNotFormatWhenEveryNormalizeRuleIsDisabled()
    {
        (string repo, string source) = await CreateRewriteFixture("hygiene-normalize-no-enabled-normalizers", "public class App{public int Value=>1;}");
        try
        {
            byte[] before = await File.ReadAllBytesAsync(source);
            var hygiene = new HygieneEngine(repo);
            hygiene.SetRule("style.qualification.this.unnecessary", false);
            hygiene.SetRule("style.qualification.redundant", false);
            var engine = new RewriteEngine(repo);

            RewriteResult normalizeCheck = engine.Rewrite("normalize", [], false, true);
            await Assert.That(normalizeCheck.ChangedCount).IsEqualTo(0);
            await Assert.That((await File.ReadAllBytesAsync(source)).AsSpan().SequenceEqual(before)).IsTrue();
            RewriteResult normalize = engine.Rewrite("normalize", [], false, false);
            await Assert.That(normalize.ChangedCount).IsEqualTo(0);
            await Assert.That((await File.ReadAllBytesAsync(source)).AsSpan().SequenceEqual(before)).IsTrue();
            await Assert.That(engine.Rewrite("format", [], false, true).ChangedCount).IsEqualTo(1);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task RequiredBracesRuleAddsBlocksOnlyWhenEnabledAndIsIdempotent()
    {
        const string initial = "public class App{public void Run(){if(true)System.Console.WriteLine(1);}}";
        (string repo, string source) = await CreateRewriteFixture("hygiene-required-braces", initial);
        try
        {
            var hygiene = new HygieneEngine(repo);
            hygiene.SetRule("style.braces.required", false);
            var rewrite = new RewriteEngine(repo);
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).ChangedCount).IsEqualTo(0);
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);

            hygiene.SetRule("style.braces.required", true);
            RewriteResult plan = rewrite.Rewrite("normalize", [], false, checkOnly: true);
            await Assert.That(plan.ChangedCount).IsEqualTo(1);
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);
            _ = rewrite.Rewrite("normalize", [], false, checkOnly: false);
            string normalized = await File.ReadAllTextAsync(source);
            await Assert.That(normalized).Contains("if (true)");
            await Assert.That(normalized).Contains("if (true) {");
            await Assert.That(normalized).Contains("WriteLine(1); }");
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: false).ChangedCount).IsEqualTo(0);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task ExplicitAccessibilityRuleAddsSafeDefaultsOnlyWhenEnabled()
    {
        const string initial = "/// <summary>Sample.</summary>\n[System.Obsolete] class App{static class Nested{}static int Value;static App(){}public void Run(){}} interface Contract{void Call();} class Implementation:Contract{void Contract.Call(){}}";
        (string repo, string source) = await CreateRewriteFixture("hygiene-explicit-accessibility", initial);
        try
        {
            var hygiene = new HygieneEngine(repo);
            var rewrite = new RewriteEngine(repo);
            hygiene.SetRule("style.accessibility.explicit", false);
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).SelectedRuleIds).DoesNotContain("style.accessibility.explicit");
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);

            hygiene.SetRule("style.accessibility.explicit", true);
            RewriteResult planned = rewrite.Rewrite("normalize", [], false, checkOnly: true);
            await Assert.That(planned.SelectedRuleIds).Contains("style.accessibility.explicit");
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);
            _ = rewrite.Rewrite("normalize", [], false, checkOnly: false);
            string normalized = await File.ReadAllTextAsync(source);
            await Assert.That(normalized).Contains("internal class App");
            await Assert.That(normalized).Contains("private static class Nested");
            await Assert.That(normalized).Contains("private static int Value");
            await Assert.That(normalized).Contains("static App()");
            await Assert.That(normalized).Contains("internal interface Contract");
            await Assert.That(normalized).Contains("public void Call()");
            await Assert.That(normalized).Contains("internal class Implementation");
            await Assert.That(normalized).Contains("void Contract.Call()");
            string afterFirstRun = normalized;
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).ChangedCount).IsEqualTo(0);
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(afterFirstRun);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task DefaultLiteralNormalizationRequiresImmediatelyExplicitMatchingDeclarationType()
    {
        const string initial = "class App{int field=default(int);string Property{get;}=default(string);void Run(){int local=default(int);var inferred=default(int);}string Return(){return default(string);}}";
        (string repo, string source) = await CreateRewriteFixture("hygiene-default-literal", initial);
        try
        {
            var hygiene = new HygieneEngine(repo);
            var rewrite = new RewriteEngine(repo);
            hygiene.SetRule("style.default.literal", false);
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).SelectedRuleIds).DoesNotContain("style.default.literal");
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);

            hygiene.SetRule("style.default.literal", true);
            RewriteResult plan = rewrite.Rewrite("normalize", [], false, checkOnly: true);
            await Assert.That(plan.ChangedCount).IsEqualTo(1);
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);
            _ = rewrite.Rewrite("normalize", [], false, checkOnly: false);
            string normalized = await File.ReadAllTextAsync(source);
            await Assert.That(normalized).Contains("int field = default;");
            await Assert.That(normalized).Contains("string Property { get; } = default;");
            await Assert.That(normalized).Contains("int local = default;");
            await Assert.That(normalized).Contains("var inferred = default(int);");
            await Assert.That(normalized).Contains("return default(string);");
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).ChangedCount).IsEqualTo(0);

            await File.WriteAllTextAsync(source, initial);
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework><LangVersion>7.0</LangVersion></PropertyGroup></Project>");
            hygiene.SetRule("style.accessibility.explicit", false);
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).ChangedCount).IsEqualTo(0);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task RedundantCastNormalizationRemovesOnlyExactIdentityCastsWithoutComments()
    {
        const string initial = "class App{void Run(){int value=(int)1;long widened=(long)1;int commented=(/* keep */int)1;string text=\"\";var inferred=(string)text;}}";
        (string repo, string source) = await CreateRewriteFixture("hygiene-redundant-cast", initial);
        try
        {
            var hygiene = new HygieneEngine(repo);
            var rewrite = new RewriteEngine(repo);
            hygiene.SetRule("style.cast.redundant", false);
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).SelectedRuleIds).DoesNotContain("style.cast.redundant");
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);

            hygiene.SetRule("style.cast.redundant", true);
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).ChangedCount).IsEqualTo(1);
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);
            _ = rewrite.Rewrite("normalize", [], false, checkOnly: false);
            string normalized = await File.ReadAllTextAsync(source);
            await Assert.That(normalized).Contains("int value = 1;");
            await Assert.That(normalized).Contains("long widened = (long)1;");
            await Assert.That(normalized).Contains("int commented = (/* keep */int)1;");
            await Assert.That(normalized).Contains("var inferred = text;");
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).ChangedCount).IsEqualTo(0);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task RedundantInterpolationNormalizationRequiresConstantTextOnlyStrings()
    {
        const string initial = "class App{void Run(){string plain=$\"hello\";string hole=$\"{plain}\";string commented=/* keep */$\"still interpolated\";}}";
        (string repo, string source) = await CreateRewriteFixture("hygiene-redundant-interpolation", initial);
        try
        {
            var hygiene = new HygieneEngine(repo);
            var rewrite = new RewriteEngine(repo);
            hygiene.SetRule("style.interpolation.redundant", false);
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).SelectedRuleIds).DoesNotContain("style.interpolation.redundant");
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);

            hygiene.SetRule("style.interpolation.redundant", true);
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).ChangedCount).IsEqualTo(1);
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);
            _ = rewrite.Rewrite("normalize", [], false, checkOnly: false);
            string normalized = await File.ReadAllTextAsync(source);
            await Assert.That(normalized).Contains("string plain = \"hello\";");
            await Assert.That(normalized).Contains("string hole = $\"{plain}\";");
            await Assert.That(normalized).Contains("/* keep */");
            await Assert.That(normalized).Contains("$\"still interpolated\"");
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).ChangedCount).IsEqualTo(0);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task InferredNameNormalizationRequiresExactIdentifierAndPreservesCommentedNames()
    {
        const string initial = "class App{void Run(){int value=1;string Name=\"\";var tuple=(value:value, other:value, retained:/* keep */value);var inferred=new{Name=Name};var explicitName=new{Other=Name};}}";
        (string repo, string source) = await CreateRewriteFixture("hygiene-inferred-names", initial);
        try
        {
            var hygiene = new HygieneEngine(repo);
            var rewrite = new RewriteEngine(repo);
            hygiene.SetRule("style.names.inferred", false);
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).SelectedRuleIds).DoesNotContain("style.names.inferred");
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);

            hygiene.SetRule("style.names.inferred", true);
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).ChangedCount).IsEqualTo(1);
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);
            _ = rewrite.Rewrite("normalize", [], false, checkOnly: false);
            string normalized = await File.ReadAllTextAsync(source);
            await Assert.That(normalized).Contains("var tuple = (value, other: value, retained:");
            await Assert.That(normalized).Contains("/* keep */value");
            await Assert.That(normalized).Contains("var inferred = new { Name };");
            await Assert.That(normalized).Contains("var explicitName = new { Other = Name };");
            await Assert.That(rewrite.Rewrite("normalize", [], false, checkOnly: true).ChangedCount).IsEqualTo(0);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task NormalizeFormatsDocumentsChangedByStructuralRules()
    {
        const string initial = "public class App{public string Read(){return this.Value;}public string Value=>System.String.Empty;}";
        const string expected = "public class App { public string Read() { return Value; } public string Value => string.Empty; }";
        (string repo, string source) = await CreateRewriteFixture("hygiene-normalize-then-format", initial);
        try
        {
            var engine = new RewriteEngine(repo);
            RewriteResult check = engine.Rewrite("normalize", [], false, true);
            await Assert.That(check.ChangedCount).IsEqualTo(1);
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(initial);

            RewriteResult normalize = engine.Rewrite("normalize", [], false, false);
            await Assert.That(normalize.ChangedCount).IsEqualTo(1);
            string result = await File.ReadAllTextAsync(source);
            await Assert.That(result).IsEqualTo(expected);
            await Assert.That(engine.Rewrite("normalize", [], false, false).ChangedCount).IsEqualTo(0);
        }
        finally { DeleteTree(repo); }
    }

    private static async Task<(string Root, string Source)> CreateRewriteFixture(string name, string sourceText)
    {
        string repo = Path.Combine(Path.GetTempPath(), name + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        await Git(repo, "init", "-q");
        await File.WriteAllTextAsync(Path.Combine(repo, "src", "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        string source = Path.Combine(repo, "src", "App.cs");
        await File.WriteAllTextAsync(source, sourceText);
        return (repo, source);
    }

    private static async Task Git(string dir, params string[] args)
    {
        var start = new System.Diagnostics.ProcessStartInfo("git") { WorkingDirectory = dir, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string arg in args)
        {
            start.ArgumentList.Add(arg);
        }
        using System.Diagnostics.Process process = System.Diagnostics.Process.Start(start)!;
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(await process.StandardError.ReadToEndAsync());
        }
    }

    private static void DeleteTree(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
