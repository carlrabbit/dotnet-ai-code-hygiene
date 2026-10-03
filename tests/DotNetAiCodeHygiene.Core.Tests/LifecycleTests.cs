using System.Diagnostics;
using DotNetAiCodeHygiene.Core;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class LifecycleTests
{
    [Test]
    public async Task RuleOrderAndRepositoryLifecycleAreDeterministic()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        try
        {
            await Git(repo, "init");
            Directory.CreateDirectory(Path.Combine(repo, "src"));
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.cs"), "public class Sample { public void Run() { var x = 1; if (x > 0) { x++; } } }\n");
            var engine = new HygieneEngine(repo);
            await Assert.That(engine.ListRules().Select(x => x.Rule.Id).ToArray()).IsEquivalentTo(new[] { "docs.summary.required", "readability.long-line.review", "readability.control-flow.visual-block" });
            CheckResult result = engine.Check([], false);
            await Assert.That(result.Findings.Select(x => x.RuleId).ToArray()).IsEquivalentTo(new[] { "docs.summary.required", "docs.summary.required", "readability.control-flow.visual-block" });
            Finding target = result.Findings.First(f => f.RuleId == "readability.control-flow.visual-block");
            IgnoreDecision decision = engine.Ignore(target.Id, "Reviewed");
            CheckResult after = engine.Check([], false);
            await Assert.That(after.IgnoredCount).IsEqualTo(1);
            await Assert.That(engine.Explain(after.Findings[0].Handle).RuleId).IsEqualTo(after.Findings[0].RuleId);
            engine.Unignore(decision.Id);
            CheckResult restored = engine.Check([], false);
            await Assert.That(restored.IgnoredCount).IsEqualTo(0);
            await Assert.That(restored.Findings.Any(f => f.Fingerprint == target.Fingerprint)).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task LongLineUsesPhysicalUtf16Length()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        try
        {
            await Git(repo, "init"); Directory.CreateDirectory(Path.Combine(repo, "src"));
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            string line = "// " + new string('x', 198);
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.cs"), line);
            var result = new HygieneEngine(repo).Check([], false);
            await Assert.That(result.Findings.Count(x => x.RuleId == "readability.long-line.review")).IsEqualTo(1);
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.cs"), "// " + new string('x', 197));
            CheckResult atLimit = new HygieneEngine(repo).Check([], false);
            await Assert.That(atLimit.Findings.Count(x => x.RuleId == "readability.long-line.review")).IsEqualTo(0);
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.cs"), "// " + new string('x', 147) + "\n// " + new string('y', 147));
            CheckResult wrapped = new HygieneEngine(repo).Check([], false);
            await Assert.That(wrapped.Findings.Count(x => x.RuleId == "readability.long-line.review")).IsEqualTo(0);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task RuleStateIsCamelCasePersistedAndMalformedStateIsRejected()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        try
        {
            await Git(repo, "init");
            var engine = new HygieneEngine(repo);
            engine.SetRule("readability.long-line.review", false);
            string config = await File.ReadAllTextAsync(Path.Combine(repo, ".hygiene", "config.json"));
            await Assert.That(config).Contains("\"schemaVersion\"");
            await Assert.That(config).Contains("\"disabledRules\"");
            await Assert.That(engine.ListRules().Single(x => x.Rule.Id == "readability.long-line.review").Enabled).IsFalse();
            string before = config;
            try { engine.SetRule("unknown.rule", false); } catch (ProductException) { }
            await Assert.That(await File.ReadAllTextAsync(Path.Combine(repo, ".hygiene", "config.json"))).IsEqualTo(before);
            await File.WriteAllTextAsync(Path.Combine(repo, ".hygiene", "config.json"), "{\"schemaVersion\":99,\"disabledRules\":[]}");
            bool rejected = false;
            try { _ = engine.ListRules(); } catch (ProductException) { rejected = true; }
            await Assert.That(rejected).IsTrue();
            await File.WriteAllTextAsync(Path.Combine(repo, ".hygiene", "config.json"), "{\"schemaVersion\":1,\"disabledRules\":[]}");
            await File.WriteAllTextAsync(Path.Combine(repo, ".hygiene", "decisions.json"), "{\"schemaVersion\":2,\"decisions\":[]}");
            bool decisionsRejected = false;
            try { _ = engine.Check([], false); } catch (ProductException) { decisionsRejected = true; }
            await Assert.That(decisionsRejected).IsTrue();
            string externalState = "{\"schemaVersion\":1,\"disabledRules\":[\"readability.long-line.review\"]}";
            string configPath = Path.Combine(repo, ".hygiene", "config.json");
            var conflicting = new HygieneEngine(repo, () => File.WriteAllText(configPath, externalState));
            bool conflictRejected = false;
            try { conflicting.SetRule("docs.summary.required", false); } catch (ProductException) { conflictRejected = true; }
            await Assert.That(conflictRejected).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(configPath)).IsEqualTo(externalState);
            await Assert.That(Directory.EnumerateFiles(Path.Combine(repo, ".hygiene"), "*.tmp").Any()).IsFalse();
            var cancelled = new HygieneEngine(repo, () => throw new OperationCanceledException());
            bool cancellationObserved = false;
            try { cancelled.SetRule("readability.long-line.review", true); } catch (OperationCanceledException) { cancellationObserved = true; }
            await Assert.That(cancellationObserved).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(configPath)).IsEqualTo(externalState);
            await Assert.That(Directory.EnumerateFiles(Path.Combine(repo, ".hygiene"), "*.tmp").Any()).IsFalse();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task TargetResolutionDeduplicatesAndChangedIncludesCurrentGitPathsOnly()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src", "nested"));
        try
        {
            await Git(repo, "init", "-q"); await Git(repo, "config", "user.email", "fixture@example.invalid"); await Git(repo, "config", "user.name", "Fixture");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string a = Path.Combine(repo, "src", "A.cs"), b = Path.Combine(repo, "src", "nested", "B.cs"), deleted = Path.Combine(repo, "src", "Deleted.cs");
            await File.WriteAllTextAsync(a, "public class A { }"); await File.WriteAllTextAsync(b, "public class B { }"); await File.WriteAllTextAsync(deleted, "public class Deleted { }");
            await Git(repo, "add", "."); await Git(repo, "commit", "-m", "base");
            var engine = new HygieneEngine(repo);
            CheckResult explicitTargets = engine.Check(["src", "src/nested", "src/A.cs"], false);
            await Assert.That(explicitTargets.Findings.Select(f => f.Path).Distinct().Count()).IsEqualTo(3);
            await File.WriteAllTextAsync(a, "public class A { public void Changed() { } }");
            await File.WriteAllTextAsync(b, "public class B { public void Staged() { } }"); await Git(repo, "add", b);
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "nested", "Untracked.cs"), "public class Untracked { }");
            File.Delete(deleted);
            CheckResult changed = engine.Check([], true);
            string[] changedPaths = changed.Findings.Select(f => f.Path).Distinct(StringComparer.Ordinal).ToArray();
            await Assert.That(changedPaths).IsEquivalentTo(new[] { "src/A.cs", "src/nested/B.cs", "src/nested/Untracked.cs" });
            bool invalidCombination = false;
            try { _ = engine.Check(["src/A.cs"], true); } catch (ArgumentException) { invalidCombination = true; }
            await Assert.That(invalidCombination).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task InvalidRepositoryTargetsFailAsProductInput()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        string outside = Path.Combine(Path.GetTempPath(), "outside-" + Guid.NewGuid().ToString("N") + ".cs");
        Directory.CreateDirectory(repo);
        try
        {
            bool noRepository = false;
            try { _ = new HygieneEngine(repo); } catch (ProductException) { noRepository = true; }
            await Assert.That(noRepository).IsTrue();
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(outside, "class Outside { }");
            await File.WriteAllTextAsync(Path.Combine(repo, "README.md"), "text");
            var engine = new HygieneEngine(repo);
            bool outsideRejected = false;
            try { _ = engine.Check([outside], false); } catch (ProductException) { outsideRejected = true; }
            await Assert.That(outsideRejected).IsTrue();
            bool unsupportedRejected = false;
            try { _ = engine.Check(["README.md"], false); } catch (ProductException) { unsupportedRejected = true; }
            await Assert.That(unsupportedRejected).IsTrue();
            await File.WriteAllTextAsync(Path.Combine(repo, "Loose.cs"), "class Loose { }");
            bool projectRejected = false;
            try { _ = engine.Check(["Loose.cs"], false); } catch (ProductException) { projectRejected = true; }
            await Assert.That(projectRejected).IsTrue();
        }
        finally { if (File.Exists(outside)) File.Delete(outside); DeleteTree(repo); }
    }

    [Test]
    public async Task SummaryRuleCoversRequiredSymbolsAndExcludesOtherCategories()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.cs"), """
                using System;
                public class Sample
                {
                    public Sample() { }
                    public void Missing() { void Local() { } }
                    public int MissingProperty { get; }
                    public int field;
                    public event Action? Event;
                    public static Sample operator +(Sample a, Sample b) => a;
                    /// <summary>A documented member.</summary>
                    public void Documented() { }
                }
                """);
            var result = new HygieneEngine(repo).Check([], false);
            string[] names = result.Findings.Where(f => f.RuleId == "docs.summary.required").Select(f => f.Symbol!).ToArray();
            await Assert.That(names.Any(n => n.Contains("Sample.Sample", StringComparison.Ordinal))).IsTrue();
            await Assert.That(names.Any(n => n.Contains("Missing(", StringComparison.Ordinal))).IsTrue();
            await Assert.That(names.Any(n => n.Contains("MissingProperty", StringComparison.Ordinal))).IsTrue();
            await Assert.That(names.Any(n => n.Contains("field", StringComparison.Ordinal))).IsFalse();
            await Assert.That(names.Any(n => n.Contains("Event", StringComparison.Ordinal))).IsFalse();
            await Assert.That(names.Any(n => n.Contains("Documented", StringComparison.Ordinal))).IsFalse();
            await Assert.That(names.Any(n => n.Contains("Local", StringComparison.Ordinal))).IsFalse();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task VisualBlockRuleHonorsBlankLinesAndLeadingComments()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            string file = Path.Combine(repo, "src", "Sample.cs");
            var engine = new HygieneEngine(repo);
            await File.WriteAllTextAsync(file, "class C { void M() { var x = 1; if (x > 0) { x++; } } }");
            await Assert.That(engine.Check([], false).Findings.Count(f => f.RuleId == "readability.control-flow.visual-block")).IsEqualTo(1);
            await File.WriteAllTextAsync(file, "class C { void M() { if (true) { } if (false) { } } }");
            await Assert.That(engine.Check([], false).Findings.Count(f => f.RuleId == "readability.control-flow.visual-block")).IsEqualTo(0);
            await File.WriteAllTextAsync(file, "class C { void M() { if (true) { } } }");
            await Assert.That(engine.Check([], false).Findings.Count(f => f.RuleId == "readability.control-flow.visual-block")).IsEqualTo(0);
            await File.WriteAllTextAsync(file, "class C { void M() { var x = 1;\n\nif (x > 0) { x++; } } }");
            await Assert.That(engine.Check([], false).Findings.Count(f => f.RuleId == "readability.control-flow.visual-block")).IsEqualTo(0);
            await File.WriteAllTextAsync(file, "class C { void M() { if (true) { } var x = 1; if (x > 0) { x++; } } }");
            await Assert.That(engine.Check([], false).Findings.Count(f => f.RuleId == "readability.control-flow.visual-block")).IsEqualTo(1);
            await File.WriteAllTextAsync(file, "class C { void M() { var x = 1;\n// explain this branch\nif (x > 0) { x++; } } }");
            await Assert.That(engine.Check([], false).Findings.Count(f => f.RuleId == "readability.control-flow.visual-block")).IsEqualTo(1);
            await File.WriteAllTextAsync(file, "class C { void M() { var x = 1;\n\n// explain this branch\nif (x > 0) { x++; } } }");
            await Assert.That(engine.Check([], false).Findings.Count(f => f.RuleId == "readability.control-flow.visual-block")).IsEqualTo(0);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task FingerprintsKeepRelevantIdentityAndMarkChangedOccurrencesStale()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            string file = Path.Combine(repo, "src", "Sample.cs");
            string payload = new string('x', 210);
            await File.WriteAllTextAsync(file, "public class Sample { public string Value = \"" + payload + "\"; }");
            var engine = new HygieneEngine(repo);
            Finding lineFinding = engine.Check([], false).Findings.Single(f => f.RuleId == "readability.long-line.review");
            IgnoreDecision decision = engine.Ignore(lineFinding.Id, "Reviewed line");
            await File.WriteAllTextAsync(file, "public class Sample { public string Value      = \"" + payload + "\"; }");
            await Assert.That(engine.Check([], false).IgnoredCount).IsEqualTo(1);
            await File.WriteAllTextAsync(file, "public class Sample { public string Value      = \"" + new string('y', 210) + "\"; }");
            CheckResult changed = engine.Check([], false);
            await Assert.That(changed.IgnoredCount).IsEqualTo(0);
            await Assert.That(changed.Findings.Any(f => f.RuleId == "readability.long-line.review")).IsTrue();
            await Assert.That(engine.ListIgnores([]).Single(x => x.Id == decision.Id).State).IsEqualTo("stale");
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task SummaryIgnoreSurvivesSymbolMovement()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            string file = Path.Combine(repo, "src", "Sample.cs");
            await File.WriteAllTextAsync(file, "public class Sample { public void Run() { } }");
            var engine = new HygieneEngine(repo);
            Finding method = engine.Check([], false).Findings.Single(f => f.RuleId == "docs.summary.required" && f.Symbol!.Contains("Run", StringComparison.Ordinal));
            engine.Ignore(method.Id, "Reviewed missing summary");
            engine.SetRule("docs.summary.required", false);
            await Assert.That(engine.ListIgnores([]).Single().State).IsEqualTo("active");
            engine.SetRule("docs.summary.required", true);
            await File.WriteAllTextAsync(file, "\n\npublic class Sample { public void Run() { } }");
            await Assert.That(engine.Check([], false).IgnoredCount).IsEqualTo(1);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task IgnoreRejectsSourceChangedSinceTheFindingRun()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            string file = Path.Combine(repo, "src", "Sample.cs");
            await File.WriteAllTextAsync(file, "public class Sample { }");
            var engine = new HygieneEngine(repo);
            Finding finding = engine.Check([], false).Findings.Single(f => f.RuleId == "docs.summary.required");
            await File.WriteAllTextAsync(file, "public class Renamed { }");
            bool rejected = false;
            try { engine.Ignore(finding.Handle, "stale"); } catch (ProductException) { rejected = true; }
            await Assert.That(rejected).IsTrue();
            await Assert.That(File.Exists(Path.Combine(repo, ".hygiene", "decisions.json"))).IsFalse();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task IdenticalOccurrencesWithinOneAnchorRemainDistinct()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            string line = "// " + new string('x', 205);
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.cs"), "public class Sample { public void Run() {\n" + line + "\n" + line + "\n} }");
            var engine = new HygieneEngine(repo);
            Finding[] findings = engine.Check([], false).Findings.Where(f => f.RuleId == "readability.long-line.review").ToArray();
            await Assert.That(findings.Length).IsEqualTo(2);
            await Assert.That(findings[0].Fingerprint).IsNotEqualTo(findings[1].Fingerprint);
            engine.Ignore(findings[0].Id, "One occurrence reviewed");
            CheckResult next = engine.Check([], false);
            await Assert.That(next.IgnoredCount).IsEqualTo(1);
            await Assert.That(next.Findings.Count(f => f.RuleId == "readability.long-line.review")).IsEqualTo(1);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task BlankLineResolvesAnIgnoredControlFlowOccurrence()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            string file = Path.Combine(repo, "src", "Sample.cs");
            await File.WriteAllTextAsync(file, "public class Sample { public void Run() { var x = 1; if (x > 0) { x++; } } }");
            var engine = new HygieneEngine(repo);
            Finding finding = engine.Check([], false).Findings.Single(f => f.RuleId == "readability.control-flow.visual-block");
            IgnoreDecision decision = engine.Ignore(finding.Id, "Reviewed grouping");
            await File.WriteAllTextAsync(file, "public class Sample { public void Run() { var x = 1;\n\nif (x > 0) { x++; } } }");
            CheckResult resolved = engine.Check([], false);
            await Assert.That(resolved.IgnoredCount).IsEqualTo(0);
            await Assert.That(resolved.Findings.Any(f => f.RuleId == "readability.control-flow.visual-block")).IsFalse();
            await Assert.That(engine.ListIgnores([]).Single(x => x.Id == decision.Id).State).IsEqualTo("stale");
        }
        finally { DeleteTree(repo); }
    }

    private static async Task Git(string dir, params string[] args)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = dir, RedirectStandardError = true, UseShellExecute = false };
        foreach (string arg in args) start.ArgumentList.Add(arg);
        using Process p = Process.Start(start)!; await p.WaitForExitAsync();
        if (p.ExitCode != 0) throw new InvalidOperationException(await p.StandardError.ReadToEndAsync());
    }

    private static void DeleteTree(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(path, true);
    }
}
