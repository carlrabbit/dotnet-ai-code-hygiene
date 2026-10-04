using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using DotNetAiCodeHygiene.Core;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class LifecycleTests
{
    [Test]
    public async Task RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-rewrite-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string file = Path.Combine(repo, "src", "Fixture.cs");
            const string initial = "public class Fixture{private int value;public int Value(){return this.value;}}";
            await File.WriteAllTextAsync(file, initial);
            var engine = new RewriteEngine(repo);
            RewriteResult formatCheck = engine.Rewrite("format", [], false, true);
            await Assert.That(formatCheck.ChangedCount).IsEqualTo(1);
            await Assert.That(await File.ReadAllTextAsync(file)).IsEqualTo(initial);
            RewriteResult format = engine.Rewrite("format", [], false, false);
            await Assert.That(format.ChangedPaths).IsEquivalentTo(new[] { "src/Fixture.cs" });
            string formatted = await File.ReadAllTextAsync(file);
            await Assert.That(engine.Rewrite("format", [], false, false).ChangedCount).IsEqualTo(0);
            await Assert.That(await File.ReadAllTextAsync(file)).IsEqualTo(formatted);
            RewriteResult normalizeCheck = engine.Rewrite("normalize", [], false, true);
            await Assert.That(normalizeCheck.ChangedCount).IsEqualTo(1);
            await Assert.That(await File.ReadAllTextAsync(file)).IsEqualTo(formatted);
            RewriteResult normalize = engine.Rewrite("normalize", [], false, false);
            await Assert.That(normalize.ChangedCount).IsEqualTo(1);
            string normalized = await File.ReadAllTextAsync(file);
            await Assert.That(engine.Rewrite("normalize", [], false, false).ChangedCount).IsEqualTo(0);
            await Assert.That(await File.ReadAllTextAsync(file)).IsEqualTo(normalized);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task NormalizeRejectsCompilerErrorsAndRewriteConflictNeverOverwritesSource()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-rewrite-safety-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string first = Path.Combine(repo, "src", "A.cs");
            string second = Path.Combine(repo, "src", "B.cs");
            string unselected = Path.Combine(repo, "src", "Unselected.cs");
            const string a = "public class A{public void Run(){System.Console.WriteLine(1);}}";
            const string invalid = "public class B { public void Run() { MissingType value = null; } }";
            const string untouched = "public class Unselected { }\n";
            await File.WriteAllTextAsync(first, a);
            await File.WriteAllTextAsync(second, invalid);
            await File.WriteAllTextAsync(unselected, untouched);
            var engine = new RewriteEngine(repo);
            bool rejected = false;
            try { engine.Rewrite("normalize", [first, second], false, false); } catch (ProductException) { rejected = true; }
            await Assert.That(rejected).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(first)).IsEqualTo(a);
            await Assert.That(await File.ReadAllTextAsync(second)).IsEqualTo(invalid);

            const string validSecond = "public class B{public void Run(){System.Console.WriteLine(2);}}";
            await File.WriteAllTextAsync(second, validSecond);
            var conflicting = new RewriteEngine(repo, () => File.WriteAllText(second, "external edit"));
            rejected = false;
            try { conflicting.Rewrite("format", [first, second], false, false); } catch (ProductException) { rejected = true; }
            await Assert.That(rejected).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(first)).IsEqualTo(a);
            await Assert.That(await File.ReadAllTextAsync(second)).IsEqualTo("external edit");

            await File.WriteAllTextAsync(second, validSecond);
            var faulted = new RewriteEngine(repo, null, count => { if (count == 1) throw new OperationCanceledException("injected cancellation"); });
            bool cancelled = false;
            try { faulted.Rewrite("format", [first, second], false, false); } catch (OperationCanceledException) { cancelled = true; }
            await Assert.That(cancelled).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(first)).IsEqualTo(a);
            await Assert.That(await File.ReadAllTextAsync(second)).IsEqualTo(validSecond);
            RewriteResult committed = new RewriteEngine(repo).Rewrite("format", [first, second], false, false);
            await Assert.That(committed.ChangedCount).IsEqualTo(2);
            await Assert.That(await File.ReadAllTextAsync(first)).IsNotEqualTo(a);
            await Assert.That(await File.ReadAllTextAsync(second)).IsNotEqualTo(validSecond);
            await Assert.That(await File.ReadAllTextAsync(unselected)).IsEqualTo(untouched);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task RewriteTargetsMatchCheckForOverlapChangedAndUnprojectedFiles()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-rewrite-targets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await Git(repo, "config", "user.email", "hygiene@example.invalid");
            await Git(repo, "config", "user.name", "Hygiene Tests");
            string a = Path.Combine(repo, "src", "A.cs");
            string b = Path.Combine(repo, "src", "B.cs");
            string loose = Path.Combine(repo, "Loose.cs");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            const string rawA = "public class A{ }";
            const string rawB = "public class B{ }";
            const string looseSource = "public class Loose{ }";
            await File.WriteAllTextAsync(a, rawA);
            await File.WriteAllTextAsync(b, rawB);
            await File.WriteAllTextAsync(loose, looseSource);
            await Git(repo, "add", ".");
            await Git(repo, "commit", "-qm", "baseline");

            var engine = new RewriteEngine(repo);
            RewriteResult defaultCheck = engine.Rewrite("format", [], false, true);
            await Assert.That(defaultCheck.TargetCount).IsEqualTo(3);
            await Assert.That(defaultCheck.ChangedCount).IsEqualTo(2);
            await Assert.That(await File.ReadAllTextAsync(loose)).IsEqualTo(looseSource);
            RewriteResult overlap = engine.Rewrite("format", ["src", a], false, true);
            await Assert.That(overlap.TargetCount).IsEqualTo(2);
            await Assert.That(overlap.ChangedPaths).IsEquivalentTo(new[] { "src/A.cs", "src/B.cs" });

            bool explicitLooseRejected = false;
            try { engine.Rewrite("format", [loose], false, true); } catch (ProductException) { explicitLooseRejected = true; }
            await Assert.That(explicitLooseRejected).IsTrue();
            bool mutuallyExclusiveRejected = false;
            try { engine.Rewrite("format", [a], true, true); } catch (ArgumentException) { mutuallyExclusiveRejected = true; }
            await Assert.That(mutuallyExclusiveRejected).IsTrue();
            bool outsideRejected = false;
            try { engine.Rewrite("format", [Path.GetTempPath()], false, true); } catch (ProductException) { outsideRejected = true; }
            await Assert.That(outsideRejected).IsTrue();
            bool unsupportedRejected = false;
            try { engine.Rewrite("format", [Path.Combine(repo, "src", "Fixture.csproj")], false, true); } catch (ProductException) { unsupportedRejected = true; }
            await Assert.That(unsupportedRejected).IsTrue();

            await File.WriteAllTextAsync(a, "public class A{ public int F(){return 1;} }");
            string c = Path.Combine(repo, "src", "C.cs");
            await File.WriteAllTextAsync(c, "public class C{ }");
            RewriteResult changed = engine.Rewrite("format", [], true, true);
            await Assert.That(changed.TargetCount).IsEqualTo(2);
            await Assert.That(changed.ChangedPaths).IsEquivalentTo(new[] { "src/A.cs", "src/C.cs" });
            RewriteResult changedMutation = engine.Rewrite("format", [], true, false);
            await Assert.That(changedMutation.ChangedPaths).IsEquivalentTo(changed.ChangedPaths);
            await Assert.That(await File.ReadAllTextAsync(loose)).IsEqualTo(looseSource);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task NormalizationCatalogueIsConservativeAndPostValidationBlocksEveryWrite()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-rewrite-catalogue-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string first = Path.Combine(repo, "src", "A.cs");
            string second = Path.Combine(repo, "src", "B.cs");
            const string source = "using System; public class Fixture{private int value; public String Safe(){return System.String.Empty;} public int Read(){return this.value;} public int Shadow(int value){return this.value + value;}}";
            await File.WriteAllTextAsync(first, source);
            await File.WriteAllTextAsync(second, "public class Other { }\n");
            var engine = new RewriteEngine(repo);
            RewriteResult plan = engine.Rewrite("normalize", [first], false, true);
            await Assert.That(plan.ChangedCount).IsEqualTo(1);
            await Assert.That(await File.ReadAllTextAsync(first)).IsEqualTo(source);
            RewriteResult normalized = engine.Rewrite("normalize", [first], false, false);
            string result = await File.ReadAllTextAsync(first);
            await Assert.That(normalized.ChangedCount).IsEqualTo(1);
            await Assert.That(result).Contains("using System;");
            await Assert.That(result).Contains("string Safe()");
            await Assert.That(result).Contains("return value;");
            await Assert.That(result).Contains("return this.value + value;");
            await Assert.That(result).DoesNotContain("System.String.Empty");

            byte[] firstBefore = await File.ReadAllBytesAsync(first);
            byte[] secondBefore = await File.ReadAllBytesAsync(second);
            var invalidPlan = new RewriteEngine(repo, null, null, text => text + "\npublic MissingType Broken;\n");
            bool postValidationRejected = false;
            try { invalidPlan.Rewrite("normalize", [first, second], false, false); } catch (ProductException e) when (e.Message.Contains("would introduce compiler errors", StringComparison.Ordinal)) { postValidationRejected = true; }
            await Assert.That(postValidationRejected).IsTrue();
            await Assert.That((await File.ReadAllBytesAsync(first)).AsSpan().SequenceEqual(firstBefore)).IsTrue();
            await Assert.That((await File.ReadAllBytesAsync(second)).AsSpan().SequenceEqual(secondBefore)).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

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
            await Assert.That(engine.ListRules().Select(x => x.Rule.Id).ToArray()).IsEquivalentTo(new[] { "docs.summary.required", "docs.summary.quality.review", "readability.long-line.review", "readability.control-flow.visual-block" });
            await Assert.That(engine.ListRules().All(x => x.Enabled)).IsTrue();
            CheckResult result = engine.Check([], false);
            await Assert.That(result.Findings.Select(x => x.RuleId).ToArray()).IsEquivalentTo(new[] { "docs.summary.required", "docs.summary.required", "readability.control-flow.visual-block" });
            await Assert.That(result.Findings.Select(x => x.Id).SequenceEqual(new[] { "F-1", "F-2", "F-3" })).IsTrue();
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
    public async Task SemanticReviewSamplingAndExpansionAreDeterministicAndRunScoped()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-review-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string file = Path.Combine(repo, "src", "Fixture.cs");
            string source = string.Join("\n", Enumerable.Range(0, 8).Select(i => $"/// <summary>Diese Beschreibung erklärt den fachlichen Zweck {i}.</summary>\npublic class Subject{i} {{ }}")) + "\n/// <summary>   </summary>\npublic class EmptySummary { }\n/// <summary><bad>Ungültig.</summary>\npublic class InvalidSummary { }\n// ordinary comment\npublic class UndocumentedText { }\npublic class MissingSummary { }";
            await File.WriteAllTextAsync(file, source);
            var engine = new HygieneEngine(repo);
            CheckResult first = engine.Check([], false);
            ReviewBatch batch = first.ReviewBatches.Single();
            await Assert.That(batch.PopulationCount).IsEqualTo(8);
            await Assert.That(batch.SampleCount).IsEqualTo(5);
            await Assert.That(batch.Mode).IsEqualTo("sample");
            await Assert.That(batch.ReviewerClass).IsEqualTo("implementer");
            await Assert.That(batch.Items.All(item => item.Id.StartsWith("RI-", StringComparison.Ordinal))).IsTrue();
            await Assert.That(batch.Questions.Select(q => q.Id).ToArray()).IsEquivalentTo(new[] { "Q1", "Q2", "Q3", "Q4" });
            CheckResult repeat = engine.Check([], false);
            await Assert.That(repeat.ReviewBatches.Single().Items.Select(i => i.Symbol).ToArray()).IsEquivalentTo(batch.Items.Select(i => i.Symbol).ToArray());
            await Assert.That(repeat.ReviewBatches.Single().PopulationFingerprint).IsEqualTo(batch.PopulationFingerprint);
            batch = repeat.ReviewBatches.Single();

            string latestPath = Path.Combine(repo, ".hygiene", ".state", "latest-run.json");
            string beforeExpand = await File.ReadAllTextAsync(latestPath);
            ReviewBatch expanded = engine.ExpandReview(batch.Id);
            await Assert.That(expanded.Mode).IsEqualTo("expanded");
            await Assert.That(expanded.ReviewerClass).IsEqualTo("frontier");
            await Assert.That(expanded.Items.Count).IsEqualTo(8);
            await Assert.That(batch.Items.All(sample => expanded.Items.Any(item => item.Symbol == sample.Symbol))).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(latestPath)).IsEqualTo(beforeExpand);
            bool oldRejected = false;
            try { _ = engine.ExpandReview("R-OLD/B-1"); } catch (ProductException) { oldRejected = true; }
            await Assert.That(oldRejected).IsTrue();
            bool ignoreRejected = false;
            try { _ = engine.Ignore("B-1", "not a finding"); } catch (ProductException) { ignoreRejected = true; }
            await Assert.That(ignoreRejected).IsTrue();

            await File.WriteAllTextAsync(file, source.Replace("fachlichen Zweck 0", "geänderten fachlichen Zweck 0", StringComparison.Ordinal));
            ReviewBatch changedPopulation = engine.Check([], false, true, false, false).ReviewBatches.Single();
            await Assert.That(changedPopulation.PopulationFingerprint).IsNotEqualTo(batch.PopulationFingerprint);
            bool changedRejected = false;
            try { _ = engine.ExpandReview(batch.Handle); } catch (ProductException e) when (e.Message.Contains("rerun hygiene check", StringComparison.Ordinal)) { changedRejected = true; }
            await Assert.That(changedRejected).IsTrue();

            string five = string.Join("\n", Enumerable.Range(0, 5).Select(i => $"/// <summary>Eine brauchbare Zusammenfassung {i}.</summary>\npublic class Small{i} {{ }}"));
            await File.WriteAllTextAsync(file, five);
            ReviewBatch small = engine.Check([], false).ReviewBatches.Single();
            await Assert.That(small.PopulationCount).IsEqualTo(5);
            await Assert.That(small.SampleCount).IsEqualTo(5);
            await Assert.That(small.Items.Count).IsEqualTo(5);

            await File.WriteAllTextAsync(file, "public class NoSummaries { }");
            ReviewBatch empty = engine.Check([], false).ReviewBatches.Single();
            await Assert.That(empty.PopulationCount).IsEqualTo(0);
            await Assert.That(empty.SampleCount).IsEqualTo(0);
            await Assert.That(empty.Items.Count).IsEqualTo(0);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task DurableReviewHandoffEmbedsOnlyCurrentRelevantSourcesAndIsAtomic()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-handoff-" + Guid.NewGuid().ToString("N"));
        string external = Path.Combine(Path.GetTempPath(), "hygiene-handoff-external-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, ".gitignore"), "**/bin/\n**/obj/\n**/.hygiene/.state/\n");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string aPath = Path.Combine(repo, "src", "A.cs");
            string bPath = Path.Combine(repo, "src", "B.cs");
            string unrelatedPath = Path.Combine(repo, "src", "Unrelated.cs");
            string aSource = "/// <summary>Erste relevante Beschreibung.</summary>\npublic class A { }\n/// <summary>Zweite relevante Beschreibung.</summary>\npublic class A2 { }\n";
            string bSource = "/// <summary>Dritte relevante Beschreibung.</summary>\npublic class B { }\n";
            string unrelatedSource = "public class Unrelated { }\n";
            await File.WriteAllTextAsync(aPath, aSource);
            await File.WriteAllTextAsync(bPath, bSource);
            await File.WriteAllTextAsync(unrelatedPath, unrelatedSource);

            var engine = new HygieneEngine(repo);
            ReviewBatch batch = engine.Check([], false).ReviewBatches.Single();
            string statePath = Path.Combine(repo, ".hygiene", ".state", "latest-run.json");
            string stateBefore = await File.ReadAllTextAsync(statePath);
            string aBefore = await File.ReadAllTextAsync(aPath);
            string bBefore = await File.ReadAllTextAsync(bPath);
            string unrelatedBefore = await File.ReadAllTextAsync(unrelatedPath);
            string defaultPath = engine.CreateReviewHandoff("B-1");
            string expectedId = "HR-" + batch.Handle.Split('/')[0][2..] + "-B1";
            await Assert.That(Path.GetRelativePath(repo, defaultPath).Replace('\\', '/')).IsEqualTo(".hygiene/reviews/" + expectedId + "/request.json");
            await Assert.That(Regex.IsMatch(expectedId, "^HR-[A-Z0-9-]+$", RegexOptions.CultureInvariant)).IsTrue();
            await Assert.That(await RunGitExitCode(repo, "check-ignore", "--quiet", ".hygiene/reviews/" + expectedId + "/request.json")).IsEqualTo(1);

            using (JsonDocument request = JsonDocument.Parse(await File.ReadAllTextAsync(defaultPath)))
            {
                JsonElement root = request.RootElement;
                await Assert.That(root.GetProperty("schemaVersion").GetInt32()).IsEqualTo(1);
                await Assert.That(root.GetProperty("kind").GetString()).IsEqualTo("semantic-review-request");
                await Assert.That(root.GetProperty("handoffId").GetString()).IsEqualTo(expectedId);
                await Assert.That(DateTimeOffset.TryParse(root.GetProperty("createdAtUtc").GetString(), out _)).IsTrue();
                await Assert.That(root.GetProperty("source").GetProperty("runId").GetString()).IsEqualTo(batch.Handle.Split('/')[0]);
                await Assert.That(root.GetProperty("source").GetProperty("batchHandle").GetString()).IsEqualTo(batch.Handle);
                await Assert.That(root.GetProperty("rule").GetProperty("id").GetString()).IsEqualTo("docs.summary.quality.review");
                await Assert.That(root.GetProperty("rule").GetProperty("version").GetInt32()).IsEqualTo(1);
                await Assert.That(root.GetProperty("mode").GetString()).IsEqualTo("expanded");
                await Assert.That(root.GetProperty("reviewerClass").GetString()).IsEqualTo("frontier");
                await Assert.That(root.GetProperty("populationCount").GetInt32()).IsEqualTo(3);
                await Assert.That(root.GetProperty("items").GetArrayLength()).IsEqualTo(3);
                await Assert.That(root.GetProperty("questions").GetArrayLength()).IsEqualTo(4);
                await Assert.That(root.GetProperty("items")[0].GetProperty("id").GetString()!.StartsWith("RI-", StringComparison.Ordinal)).IsTrue();
                await Assert.That(root.GetProperty("sources").GetArrayLength()).IsEqualTo(2);
                string[] sourcePaths = root.GetProperty("sources").EnumerateArray().Select(x => x.GetProperty("path").GetString()!).ToArray();
                await Assert.That(sourcePaths).IsEquivalentTo(new[] { "src/A.cs", "src/B.cs" });
                string embeddedA = root.GetProperty("sources").EnumerateArray().Single(x => x.GetProperty("path").GetString() == "src/A.cs").GetProperty("content").GetString()!;
                await Assert.That(embeddedA).IsEqualTo(aSource);
                string embeddedB = root.GetProperty("sources").EnumerateArray().Single(x => x.GetProperty("path").GetString() == "src/B.cs").GetProperty("content").GetString()!;
                await Assert.That(embeddedB).IsEqualTo(bSource);
                await Assert.That(root.GetProperty("sources").EnumerateArray().Any(x => x.GetProperty("path").GetString() == "src/Unrelated.cs")).IsFalse();
            }
            string jsonText = await File.ReadAllTextAsync(defaultPath);
            await Assert.That(jsonText.Contains("PopulationFingerprint", StringComparison.OrdinalIgnoreCase)).IsFalse();
            await Assert.That(jsonText.Contains("rankingHash", StringComparison.OrdinalIgnoreCase)).IsFalse();

            string externalPath = Path.Combine(external, "nested", "deeper", "request.json");
            await Assert.That(engine.CreateReviewHandoff(batch.Handle, externalPath)).IsEqualTo(Path.GetFullPath(externalPath));
            await Assert.That(File.Exists(externalPath)).IsTrue();
            using (JsonDocument externalRequest = JsonDocument.Parse(await File.ReadAllTextAsync(externalPath)))
                await Assert.That(externalRequest.RootElement.GetProperty("handoffId").GetString()).IsEqualTo(expectedId);
            string internalPath = Path.Combine(repo, ".hygiene", "custom", "request.json");
            await Assert.That(engine.CreateReviewHandoff(batch.Handle, internalPath)).IsEqualTo(Path.GetFullPath(internalPath));
            await Assert.That(File.Exists(internalPath)).IsTrue();

            string conflictPath = Path.Combine(external, "existing", "request.json");
            Directory.CreateDirectory(Path.GetDirectoryName(conflictPath)!);
            const string existing = "keep these bytes";
            await File.WriteAllTextAsync(conflictPath, existing);
            bool conflict = false;
            try { _ = engine.CreateReviewHandoff(batch.Handle, conflictPath); } catch (ProductException) { conflict = true; }
            await Assert.That(conflict).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(conflictPath)).IsEqualTo(existing);

            string failedPath = Path.Combine(external, "atomic", "request.json");
            var interrupted = new HygieneEngine(repo, () => throw new OperationCanceledException("simulated interrupted handoff"));
            bool interruptedWrite = false;
            try { _ = interrupted.CreateReviewHandoff(batch.Handle, failedPath); } catch (OperationCanceledException) { interruptedWrite = true; }
            await Assert.That(interruptedWrite).IsTrue();
            await Assert.That(File.Exists(failedPath)).IsFalse();
            await Assert.That(Directory.EnumerateFiles(Path.GetDirectoryName(failedPath)!, "*.tmp").Any()).IsFalse();

            await Assert.That(await File.ReadAllTextAsync(statePath)).IsEqualTo(stateBefore);
            await Assert.That(await File.ReadAllTextAsync(aPath)).IsEqualTo(aBefore);
            await Assert.That(await File.ReadAllTextAsync(bPath)).IsEqualTo(bBefore);
            await Assert.That(await File.ReadAllTextAsync(unrelatedPath)).IsEqualTo(unrelatedBefore);

            await File.WriteAllTextAsync(aPath, aSource.Replace("Erste relevante", "Geänderte relevante", StringComparison.Ordinal));
            bool stale = false;
            try { _ = engine.CreateReviewHandoff(batch.Handle, Path.Combine(external, "stale", "request.json")); }
            catch (ProductException e) when (e.Message.Contains("rerun hygiene check", StringComparison.Ordinal)) { stale = true; }
            await Assert.That(stale).IsTrue();
            await Assert.That(File.Exists(Path.Combine(external, "stale", "request.json"))).IsFalse();
            bool old = false;
            try { _ = engine.CreateReviewHandoff("R-OLD/B-1", Path.Combine(external, "old", "request.json")); } catch (ProductException) { old = true; }
            await Assert.That(old).IsTrue();
            await Assert.That(File.Exists(Path.Combine(external, "old", "request.json"))).IsFalse();

            ReviewBatch beforeAddedFile = engine.Check([], false).ReviewBatches.Single();
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "NewEligible.cs"), "/// <summary>Neue relevante Datei.</summary>\npublic class NewEligible { }\n");
            bool addedSubjectStale = false;
            try { _ = engine.CreateReviewHandoff(beforeAddedFile.Handle, Path.Combine(external, "added", "request.json")); }
            catch (ProductException e) when (e.Message.Contains("rerun hygiene check", StringComparison.Ordinal)) { addedSubjectStale = true; }
            await Assert.That(addedSubjectStale).IsTrue();
            await Assert.That(File.Exists(Path.Combine(external, "added", "request.json"))).IsFalse();
        }
        finally
        {
            DeleteTree(repo);
            DeleteTree(external);
        }
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
    public async Task LatestRunPublishesOnlySuccessfulChecksAndStateIsGitIgnored()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, ".gitignore"), ".hygiene/.state/\n");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.cs"), "public class Sample { }");
            var engine = new HygieneEngine(repo);
            CheckResult first = engine.Check([], false);
            string statePath = Path.Combine(repo, ".hygiene", ".state", "latest-run.json");
            string firstState = await File.ReadAllTextAsync(statePath);
            CheckResult second = engine.Check([], false);
            string secondState = await File.ReadAllTextAsync(statePath);
            await Assert.That(first.RunId).IsNotEqualTo(second.RunId);
            await Assert.That(firstState).IsNotEqualTo(secondState);

            bool failed = false;
            try { _ = engine.Check(["missing.cs"], false); } catch (ProductException) { failed = true; }
            await Assert.That(failed).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(statePath)).IsEqualTo(secondState);
            await Git(repo, "check-ignore", ".hygiene/.state/latest-run.json");
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task DecisionWritesRejectConflictsAndCancellationWithoutLosingPreviousState()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.cs"), "public class Sample { }");
            var engine = new HygieneEngine(repo);
            Finding finding = engine.Check([], false).Findings.Single(f => f.RuleId == "docs.summary.required");
            string decisionsPath = Path.Combine(repo, ".hygiene", "decisions.json");
            const string external = "{\"schemaVersion\":1,\"decisions\":[]}";
            var conflicting = new HygieneEngine(repo, () => File.WriteAllText(decisionsPath, external));
            bool conflict = false;
            try { conflicting.Ignore(finding.Id, "conflict"); } catch (ProductException) { conflict = true; }
            await Assert.That(conflict).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(decisionsPath)).IsEqualTo(external);
            await Assert.That(Directory.EnumerateFiles(Path.Combine(repo, ".hygiene"), "*.tmp").Any()).IsFalse();

            IgnoreDecision decision = engine.Ignore(finding.Id, "persisted");
            string before = await File.ReadAllTextAsync(decisionsPath);
            var cancelled = new HygieneEngine(repo, () => throw new OperationCanceledException());
            bool cancellation = false;
            try { cancelled.Unignore(decision.Id); } catch (OperationCanceledException) { cancellation = true; }
            await Assert.That(cancellation).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(decisionsPath)).IsEqualTo(before);
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
            CheckResult explicitFiles = engine.Check(["src/A.cs", "src/nested/B.cs"], false);
            await Assert.That(explicitFiles.Findings.Select(f => f.Path).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).SequenceEqual(new[] { "src/A.cs", "src/nested/B.cs" })).IsTrue();
            CheckResult explicitTargets = engine.Check(["src", "src/nested", "src/A.cs"], false);
            string[] explicitTargetPaths = explicitTargets.Findings.Select(f => f.Path).ToArray();
            await Assert.That(explicitTargetPaths.Distinct(StringComparer.Ordinal).Count()).IsEqualTo(3);
            await Assert.That(explicitTargetPaths.Length).IsEqualTo(3);
            string[] findingOrder = explicitTargets.Findings.Select(f => f.RuleId + "|" + f.Path + "|" + f.Line + "|" + f.Column + "|" + f.Fingerprint).ToArray();
            CheckResult repeatedTargets = engine.Check(["src", "src/nested", "src/A.cs"], false);
            await Assert.That(repeatedTargets.Findings.Select(f => f.RuleId + "|" + f.Path + "|" + f.Line + "|" + f.Column + "|" + f.Fingerprint).SequenceEqual(findingOrder)).IsTrue();
            await Assert.That(explicitTargets.Findings.Select(f => f.Path).SequenceEqual(explicitTargets.Findings.Select(f => f.Path).Order(StringComparer.Ordinal))).IsTrue();
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
    public async Task ProjectModelExcludesCompileRemovedFilesAndDoesNotDiscoverProjectsAboveRepositoryRoot()
    {
        string outer = Path.Combine(Path.GetTempPath(), "hygiene-outer-" + Guid.NewGuid().ToString("N"));
        string repo = Path.Combine(outer, "repo");
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(outer, "Outside.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await Git(repo, "init", "-q");
            string project = Path.Combine(repo, "src", "Sample.csproj");
            await File.WriteAllTextAsync(project, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup>
                  <ItemGroup><Compile Remove="Excluded.cs" /></ItemGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Included.cs"), "public class Included { }");
            string excluded = Path.Combine(repo, "src", "Excluded.cs");
            await File.WriteAllTextAsync(excluded, "public class Excluded { }");
            var engine = new HygieneEngine(repo);

            CheckResult discovered = engine.Check([], false);
            await Assert.That(discovered.Findings.Any(f => f.Path == "src/Included.cs")).IsTrue();
            await Assert.That(discovered.Findings.Any(f => f.Path == "src/Excluded.cs")).IsFalse();
            bool excludedExplicitlyRejected = false;
            try { _ = engine.Check([excluded], false); } catch (ProductException) { excludedExplicitlyRejected = true; }
            await Assert.That(excludedExplicitlyRejected).IsTrue();

            Directory.Delete(Path.Combine(repo, "src"), true);
            Directory.CreateDirectory(Path.Combine(repo, "src"));
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Loose.cs"), "public class Loose { }");
            bool outerProjectRejected = false;
            try { _ = engine.Check(["src/Loose.cs"], false); } catch (ProductException) { outerProjectRejected = true; }
            await Assert.That(outerProjectRejected).IsTrue();
        }
        finally { DeleteTree(outer); }
    }

    [Test]
    public async Task ProjectCompilationUsesProjectReferencesAndConditionalSymbols()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "App"));
        Directory.CreateDirectory(Path.Combine(repo, "Shared"));
        Directory.CreateDirectory(Path.Combine(repo, "Generated"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "Shared", "Shared.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>
                """);
            await File.WriteAllTextAsync(Path.Combine(repo, "Shared", "Dependency.cs"), "namespace Shared; public class Dependency { }");
            await File.WriteAllTextAsync(Path.Combine(repo, "Generated", "ProjectGenerated.cs"), "namespace Generated; public interface IProjectGenerated { }");
            await File.WriteAllTextAsync(Path.Combine(repo, "App", "App.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net11.0</TargetFramework><LangVersion>10.0</LangVersion><DefineConstants>$(DefineConstants);PROJECT_CONTEXT</DefineConstants></PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="..\Shared\Shared.csproj" />
                    <Compile Include="..\Generated\ProjectGenerated.cs" Link="Generated\ProjectGenerated.cs" />
                  </ItemGroup>
                </Project>
                """);
            string source = Path.Combine(repo, "App", "Consumer.cs");
            await File.WriteAllTextAsync(source, """
                using Shared;
                using Generated;
                #if PROJECT_CONTEXT
                public class Consumer : Dependency, IProjectGenerated { }
                #endif
                """);

            CheckResult result = new HygieneEngine(repo).Check([source], false);
            await Assert.That(result.Findings.Count(f => f.RuleId == "docs.summary.required" && f.Path == "App/Consumer.cs")).IsEqualTo(1);
            await Assert.That(result.Findings.Single(f => f.Path == "App/Consumer.cs").Symbol).IsEqualTo("Consumer");

            using var workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();
            Microsoft.CodeAnalysis.Project project = await workspace.OpenProjectAsync(Path.Combine(repo, "App", "App.csproj"));
            Microsoft.CodeAnalysis.Compilation compilation = (await project.GetCompilationAsync())!;
            Microsoft.CodeAnalysis.SyntaxTree targetTree = compilation.SyntaxTrees.Single(t => Path.GetFullPath(t.FilePath).Equals(source, StringComparison.OrdinalIgnoreCase));
            await Assert.That(((CSharpParseOptions)targetTree.Options).LanguageVersion).IsEqualTo(LanguageVersion.CSharp10);
            Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax consumer = targetTree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().Single();
            Microsoft.CodeAnalysis.ITypeSymbol?[] baseTypes = consumer.BaseList!.Types.Select(t => compilation.GetSemanticModel(targetTree).GetTypeInfo(t.Type).Type).ToArray();
            await Assert.That(baseTypes.Select(t => t?.ToDisplayString()).Contains("Shared.Dependency")).IsTrue();
            await Assert.That(baseTypes.Select(t => t?.ToDisplayString()).Contains("Generated.IProjectGenerated")).IsTrue();
            await Assert.That(baseTypes.All(t => t?.TypeKind != Microsoft.CodeAnalysis.TypeKind.Error)).IsTrue();
        }
        finally { DeleteTree(repo); }
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
                    public int WithAccessor { get; set; }
                    public record PositionalRecord(int Value);
                    public interface IContract { void Explicit(); }
                    public sealed class ExplicitImplementation : IContract { void IContract.Explicit() { } }
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
            await Assert.That(names.Any(n => n.Contains("WithAccessor.get", StringComparison.Ordinal) || n.Contains("WithAccessor.set", StringComparison.Ordinal))).IsFalse();
            await Assert.That(names.Any(n => n.Contains("ExplicitImplementation.Explicit", StringComparison.Ordinal))).IsFalse();
            await Assert.That(names.Any(n => n.Contains("PositionalRecord.Value", StringComparison.Ordinal))).IsFalse();
            await Assert.That(names.Any(n => n.Contains("Documented", StringComparison.Ordinal))).IsFalse();
            await Assert.That(names.Any(n => n.Contains("Local", StringComparison.Ordinal))).IsFalse();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task SummaryRuleRequiresNonEmptyRoslynXmlSummaryDocumentation()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        try
        {
            await Git(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Sample.cs"), """
                // <summary>This is an ordinary comment, not XML documentation.</summary>
                public class OrdinaryComment { }
                /// <summary></summary>
                public class EmptySummary { }
                /// <summary>   </summary>
                public class WhitespaceSummary { }
                /// <summary>
                /// Useful description.
                /// </summary>
                public class DocumentedSummary { }
                """);

            Finding[] summaries = new HygieneEngine(repo).Check([], false).Findings.Where(f => f.RuleId == "docs.summary.required").ToArray();
            await Assert.That(summaries.Any(f => f.Symbol == "OrdinaryComment")).IsTrue();
            await Assert.That(summaries.Any(f => f.Symbol == "EmptySummary")).IsTrue();
            await Assert.That(summaries.Any(f => f.Symbol == "WhitespaceSummary")).IsTrue();
            await Assert.That(summaries.Any(f => f.Symbol == "DocumentedSummary")).IsFalse();
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

            string[] controls =
            [
                "if (x > 0) { }", "switch (x) { default: break; }", "for (int i = 0; i < 1; i++) { }",
                "foreach (var item in new int[0]) { }", "while (x > 0) { break; }", "do { break; } while (x > 0);",
                "try { } catch { }", "using (var stream = new System.IO.MemoryStream()) { }", "lock (this) { }"
            ];
            foreach (string control in controls)
            {
                await File.WriteAllTextAsync(file, "class C { void M() { var x = 1; " + control + " } }");
                await Assert.That(engine.Check([], false).Findings.Count(f => f.RuleId == "readability.control-flow.visual-block")).IsEqualTo(1);
            }
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

    private static async Task<int> RunGitExitCode(string dir, params string[] args)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = dir, RedirectStandardError = true, UseShellExecute = false };
        foreach (string arg in args) start.ArgumentList.Add(arg);
        using Process process = Process.Start(start)!;
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static void DeleteTree(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(path, true);
    }
}
