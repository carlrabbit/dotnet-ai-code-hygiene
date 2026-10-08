using DotNetAiCodeHygiene.Core;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class M0005EvidenceTests
{
    [Test]
    public async Task EC01MissingAndFutureProfileStatesGiveSafeGuidanceWithoutMutation()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-m0005-state-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        try
        {
            bool missingRejected = false;
            try { new ProfileManager(repo).RequireCurrent(); }
            catch (ProductException e) { missingRejected = e.Message.Contains("bootstrap", StringComparison.OrdinalIgnoreCase); }
            await Assert.That(missingRejected).IsTrue();
            _ = new ProfileManager(repo).Bootstrap();
            string marker = Path.Combine(repo, ".hygiene", "profile.json");
            string future = "{\n  \"schemaVersion\": 1,\n  \"profile\": \"dotnet-11\",\n  \"version\": 3\n}\n";
            await File.WriteAllTextAsync(marker, future);
            bool futureRejected = false;
            try { new ProfileManager(repo).Update(); }
            catch (ProductException e) { futureRejected = e.Message.Contains("Unsupported", StringComparison.OrdinalIgnoreCase); }
            await Assert.That(futureRejected).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(marker)).IsEqualTo(future);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC05CanonicalEffectiveProfilePassesAfterBootstrap()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            IReadOnlyList<ProfileFinding> findings = new ProfileManager(repo).Analyze();
            await Assert.That(findings.Any(f => f.RuleId == "profile.dotnet.analysis.required")).IsFalse();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC05TopLevelSdkElementProjectParticipatesInProfileAnalysis()
    {
        string repo = await NewProfileRepo();
        try
        {
            string source = Path.Combine(repo, "src");
            Directory.CreateDirectory(source);
            await File.WriteAllTextAsync(Path.Combine(source, "App.csproj"), "<Project><Sdk Name=\"Microsoft.NET.Sdk\" /><PropertyGroup><TargetFramework>net11.0</TargetFramework><AnalysisLevel>10</AnalysisLevel><EnableNETAnalyzers>false</EnableNETAnalyzers><EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include=\"Api.cs\" /><PackageReference Include=\"StyleCop.Analyzers\" /></ItemGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(source, "Api.cs"), "public class Api { }\n");
            await File.WriteAllTextAsync(Path.Combine(source, ".editorconfig"), "[*.cs]\ndotnet_diagnostic.IDE0011.severity = none\n");

            IReadOnlyList<ProfileFinding> findings = new ProfileManager(repo).Analyze();

            foreach (string setting in new[] { "AnalysisLevel", "EnableNETAnalyzers", "EnforceCodeStyleInBuild" })
            {
                await Assert.That(findings.Any(f => f.RuleId == "profile.dotnet.analysis.required" && f.Path == "src/App.csproj" && f.Message.Contains(setting, StringComparison.Ordinal))).IsTrue();
            }
            await Assert.That(findings.Any(f => f.RuleId == "style.braces.required" && f.Path == "src/Api.cs" && f.Message.Contains("IDE0011", StringComparison.Ordinal))).IsTrue();
            await Assert.That(findings.Any(f => f.RuleId == "profile.stylecop.prohibited" && f.Path == "src/App.csproj")).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC07BootstrapPreservesPreexistingEditorConfigAndBuildProperties()
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-m0005-preserve-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repo, ".editorconfig"), "root = false\n[*.cs]\nindent_size = 2\n");
            await File.WriteAllTextAsync(Path.Combine(repo, "Directory.Build.props"), "<Project><PropertyGroup><UserSetting>keep</UserSetting></PropertyGroup></Project>\n");
            _ = new ProfileManager(repo).Bootstrap();
            string editor = await File.ReadAllTextAsync(Path.Combine(repo, ".editorconfig"));
            string props = await File.ReadAllTextAsync(Path.Combine(repo, "Directory.Build.props"));
            await Assert.That(editor.Contains("indent_size = 2", StringComparison.Ordinal)).IsTrue();
            await Assert.That(editor.Contains("root = true", StringComparison.Ordinal)).IsTrue();
            await Assert.That(props.Contains("<UserSetting>keep</UserSetting>", StringComparison.Ordinal)).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC05EffectiveImportedMsbuildPropertiesAndWarningPromotionAreReported()
    {
        string repo = await NewProfileRepo();
        try
        {
            Directory.CreateDirectory(Path.Combine(repo, "src"));
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Directory.Build.targets"), "<Project><PropertyGroup><AnalysisLevel>10</AnalysisLevel><EnableNETAnalyzers>false</EnableNETAnalyzers><EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild><TreatWarningsAsErrors>true</TreatWarningsAsErrors><WarningsAsErrors>CS0168</WarningsAsErrors></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), "public class Api { }\n");
            IReadOnlyList<ProfileFinding> findings = new ProfileManager(repo).Analyze();
            await Assert.That(findings.Any(f => f.RuleId == "profile.dotnet.analysis.required" && f.Path == "src/App.csproj" && f.Message.Contains("AnalysisLevel", StringComparison.Ordinal))).IsTrue();
            foreach (string setting in new[] { "TreatWarningsAsErrors", "WarningsAsErrors" })
            {
                await Assert.That(findings.Any(f => f.RuleId == "profile.dotnet.analysis.required" && f.Path == "src/Directory.Build.targets" && f.Message.Contains(setting, StringComparison.Ordinal))).IsTrue();
            }
            await Assert.That(findings.Any(f => f.Path == "src/App.csproj" && f.Message.Contains("EnableNETAnalyzers", StringComparison.Ordinal))).IsTrue();
            await Assert.That(findings.Any(f => f.Path == "src/App.csproj" && f.Message.Contains("EnforceCodeStyleInBuild", StringComparison.Ordinal))).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC05NestedEditorConfigOverridesAndRootCutoffsAreEffective()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            string nested = Path.Combine(repo, "src", "nested");
            Directory.CreateDirectory(nested);
            string source = Path.Combine(nested, "Api.cs");
            await File.WriteAllTextAsync(source, "public class Api { }\n");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include=\"nested/Api.cs\" /></ItemGroup></Project>");
            string config = Path.Combine(nested, ".editorconfig");
            await File.WriteAllTextAsync(config, "[*.cs]\ndotnet_diagnostic.IDE0011.severity = none\n");
            IReadOnlyList<ProfileFinding> inherited = new ProfileManager(repo).Analyze();
            await Assert.That(inherited.Any(f => f.RuleId == "style.braces.required" && f.Path == "src/nested/Api.cs" && f.Message.Contains("IDE0011", StringComparison.Ordinal))).IsTrue();
            await File.WriteAllTextAsync(config, "root = true\n[*.cs]\ndotnet_diagnostic.IDE0040.severity = none\n");
            IReadOnlyList<ProfileFinding> cutoff = new ProfileManager(repo).Analyze();
            await Assert.That(cutoff.Any(f => f.RuleId == "style.accessibility.explicit" && f.Path == "src/nested/Api.cs" && f.Message.Contains("IDE0040", StringComparison.Ordinal))).IsTrue();
            await Assert.That(cutoff.Any(f => f.RuleId == "style.braces.required" && f.Path == "src/nested/Api.cs" && f.Message.Contains("IDE0011", StringComparison.Ordinal))).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC05EffectiveEditorConfigAppliesOnlyToEvaluatedCompileSources()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            string nested = Path.Combine(repo, "src", "nested");
            Directory.CreateDirectory(nested);
            await File.WriteAllTextAsync(Path.Combine(nested, "Loose.cs"), "public class Loose { }\n");
            await File.WriteAllTextAsync(Path.Combine(nested, "Projected.cs"), "public class Projected { }\n");
            await File.WriteAllTextAsync(Path.Combine(nested, ".editorconfig"), "[*.cs]\ndotnet_diagnostic.IDE0011.severity = none\n");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include=\"nested/Projected.cs\" /></ItemGroup></Project>");
            IReadOnlyList<ProfileFinding> findings = new ProfileManager(repo).Analyze();
            await Assert.That(findings.Any(f => f.RuleId == "style.braces.required" && f.Path == "src/nested/Loose.cs" && f.Message.Contains("IDE0011", StringComparison.Ordinal))).IsFalse();
            await Assert.That(findings.Any(f => f.RuleId == "style.braces.required" && f.Path == "src/nested/Projected.cs" && f.Message.Contains("IDE0011", StringComparison.Ordinal))).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC05MissingRootCutoffInheritsParentEditorConfig()
    {
        string container = Path.Combine(Path.GetTempPath(), "hygiene-m0005-parent-editorconfig-" + Guid.NewGuid().ToString("N"));
        string repo = Path.Combine(container, "repo");
        Directory.CreateDirectory(repo);
        try
        {
            await RunGit(repo, "init", "-q");
            await File.WriteAllTextAsync(Path.Combine(container, ".editorconfig"), "root = true\n[*.cs]\ndotnet_diagnostic.IDE0011.severity = none\n");
            Directory.CreateDirectory(Path.Combine(repo, "src"));
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), "public class Api { }\n");
            _ = new ProfileManager(repo).Bootstrap();
            await File.WriteAllTextAsync(Path.Combine(repo, ".editorconfig"), "root = false\n[*.cs]\nindent_size = 2\n");
            IReadOnlyList<ProfileFinding> findings = new ProfileManager(repo).Analyze();
            await Assert.That(findings.Any(f => f.Path == "src/Api.cs" && f.Message.Contains("IDE0011", StringComparison.Ordinal))).IsTrue();
        }
        finally { DeleteTree(container); }
    }

    [Test]
    public async Task EC09PackageCentralAndPathAnalyzerInputsAreDetectedWithoutDuplicateSourceFindings()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            Directory.CreateDirectory(Path.Combine(repo, "lib"));
            await File.WriteAllTextAsync(Path.Combine(repo, "Directory.Packages.props"), "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup><ItemGroup><PackageVersion Include=\"StyleCop.Analyzers\" Version=\"1.2.0\" /></ItemGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" /><Analyzer Include=\"../lib/StyleCop.Analyzers.Custom.dll\" /></ItemGroup></Project>");
            IReadOnlyList<ProfileFinding> findings = new ProfileManager(repo).Analyze().Where(f => f.RuleId == "profile.stylecop.prohibited").ToArray();
            await Assert.That(findings.Count(f => f.Path == "src/App.csproj")).IsEqualTo(1);
            await Assert.That(findings.Count(f => f.Path == "Directory.Packages.props")).IsEqualTo(1);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC09ResolvedNugetAssetsAndPathBasedAnalyzerInputsAreRecognized()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            string project = Path.Combine(repo, "src", "App.csproj");
            await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup><ItemGroup><Analyzer Include=\"..\\lib\\StyleCop.Custom.dll\" /></ItemGroup></Project>");
            IReadOnlyList<ProfileFinding> findings = new ProfileManager(repo).Analyze().Where(f => f.RuleId == "profile.stylecop.prohibited").ToArray();
            await Assert.That(findings.Count(f => f.Path == "src/App.csproj")).IsEqualTo(1);
            await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            string assets = Path.Combine(repo, "src", "obj", "project.assets.json");
            Directory.CreateDirectory(Path.GetDirectoryName(assets)!);
            await File.WriteAllTextAsync(assets, "{\"targets\":{\"net11.0\":{\"StyleCop.Analyzers/1.2.0\":{\"analyzers\":{\"analyzers/dotnet/cs/StyleCop.Analyzers.dll\":{\"codeLanguage\":\"cs\"}}}}}}");
            IReadOnlyList<ProfileFinding> resolved = new ProfileManager(repo).Analyze().Where(f => f.RuleId == "profile.stylecop.prohibited").ToArray();
            await Assert.That(resolved.Count(f => f.Path == "src/App.csproj")).IsEqualTo(1);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC18DelegateIndexerAndTypeParameterScopeAreCorrect()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            string code = """
                /// <summary>Callback type.</summary>
                /// <typeparam name="T">Callback state.</typeparam>
                /// <param name="state">The callback state.</param>
                public delegate void Callback<T>(T state);
                /// <summary>Generic container.</summary>
                /// <typeparam name="T">The container value type.</typeparam>
                public class Container<T>
                {
                    /// <summary>Gets an item.</summary>
                    /// <param name="index">The item index.</param>
                    /// <value>The selected item.</value>
                    public T this[int index] => default!;
                    /// <summary>Maps a value.</summary>
                    /// <typeparam name="U">The output type.</typeparam>
                    /// <param name="value">The source value.</param>
                    /// <returns>The mapped value.</returns>
                    public U Map<U>(int value) => default!;
                    /// <summary>Wrong type parameter ownership.</summary>
                    /// <typeparam name="T">This belongs to the containing type.</typeparam>
                    public void Wrong() { }
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), code);
            Finding[] structural = new HygieneEngine(repo).Check([], false).Findings.Where(f => f.RuleId == "docs.xml.consistent").ToArray();
            await Assert.That(structural.Length).IsEqualTo(1);
            await Assert.That(structural[0].Message.Contains("declaration type parameter", StringComparison.Ordinal)).IsTrue();

            string refs = """
                /// <summary>Generic container.</summary>
                /// <typeparam name="T">The container value type.</typeparam>
                public class Container<T>
                {
                    /// <summary>Maps <typeparamref name="T"/> to <typeparamref name="U"/>.</summary>
                    /// <typeparam name="U">The output type.</typeparam>
                    /// <param name="value">The source value.</param>
                    /// <returns>The mapped value.</returns>
                    public U Map<U>(int value) => default!;
                    /// <summary>Gets <paramref name="index"/> from the container.</summary>
                    /// <param name="index">The item index.</param>
                    /// <value>The selected item.</value>
                    public T this[int index] => default!;
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), refs);
            Finding[] validReferences = new HygieneEngine(repo).Check([], false).Findings.Where(f => f.RuleId == "docs.xml.consistent").ToArray();
            await Assert.That(validReferences.Length).IsEqualTo(0);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC16AndEC18OptionalDocumentationAndDeclarationElementsArePresenceBased()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), """
                /// <summary>Container summary.</summary>
                /// <typeparam name="T">The containing type argument.</typeparam>
                public class Container<T>
                {
                    /// <summary>Maps a value.</summary>
                    /// <typeparam name="U">The method type argument.</typeparam>
                    /// <typeparam name="U">Duplicate.</typeparam>
                    /// <typeparam name="T">Not declared on this method.</typeparam>
                    /// <param name="value">The value.</param>
                    /// <param name="orphan">Orphan.</param>
                    /// <param name="value">Duplicate.</param>
                    public U Map<U>(int value, int other) => default!;

                    /// <summary>Has empty documentation tags.</summary>
                    /// <typeparam name="V"></typeparam>
                    /// <param name="target"></param>
                    public T Empty<V>(int target) => default!;
                }
                """);
            CheckResult result = new HygieneEngine(repo).Check([], false);
            await Assert.That(result.Findings.Any(f => f.RuleId == "docs.summary.required" && f.Symbol!.Contains("other", StringComparison.Ordinal))).IsFalse();
            Finding[] structural = result.Findings.Where(f => f.RuleId == "docs.xml.consistent").ToArray();
            await Assert.That(structural.Length >= 4).IsTrue();
            await Assert.That(structural.Any(f => f.Message.Contains("Duplicate <param>", StringComparison.Ordinal))).IsTrue();
            await Assert.That(structural.Any(f => f.Message.Contains("Duplicate <typeparam>", StringComparison.Ordinal))).IsTrue();
            await Assert.That(structural.Any(f => f.Message.Contains("declaration parameter", StringComparison.Ordinal))).IsTrue();
            await Assert.That(structural.Any(f => f.Message.Contains("declaration type parameter", StringComparison.Ordinal))).IsTrue();
            await Assert.That(structural.Any(f => f.Message == "<param name=\"target\"> must contain prose.")).IsTrue();
            await Assert.That(structural.Any(f => f.Message == "<typeparam name=\"V\"> must contain prose.")).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC18bInvalidInlineReferencesAreRejectedWhileEnclosingTypeParametersAreInScope()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), """
                /// <summary>Container summary.</summary>
                /// <typeparam name="T">The containing type argument.</typeparam>
                public class Container<T>
                {
                    /// <summary>Maps <typeparamref name="T"/> using <paramref name="missing"/>.</summary>
                    /// <typeparam name="U">The method type argument.</typeparam>
                    /// <param name="value">The value.</param>
                    /// <returns>The mapped value.</returns>
                    public U Map<U>(int value) => default!;
                    /// <summary>Missing <paramref/> name.</summary>
                    public void MissingName() { }
                    /// <summary>Missing <typeparamref name="Lost"/>.</summary>
                    public void Invalid() { }
                }
                """);
            Finding[] references = new HygieneEngine(repo).Check([], false).Findings.Where(f => f.RuleId == "docs.xml.consistent" && f.Message.Contains("must reference", StringComparison.Ordinal)).ToArray();
            await Assert.That(references.Length).IsEqualTo(3);
            await Assert.That(references.Single(f => f.Observation == "missing name").Observation).IsEqualTo("missing name");
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC19PresentReturnsValuesAndExceptionsAreValidatedWithoutCompletenessRules()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), """
                /// <summary>A valid container.</summary>
                public class Api
                {
                    /// <summary>A void operation.</summary>
                    /// <returns>Invalid return documentation.</returns>
                    public void Run() { }
                    /// <summary>A non-empty result.</summary>
                    /// <returns></returns>
                    public int Get() => 1;
                    /// <summary>Gets a value.</summary>
                    /// <value>Invalid on a method.</value>
                    public int MethodValue() => 1;
                    /// <summary>A valid property.</summary>
                    /// <value>The value.</value>
                    public int Value => 1;
                    /// <summary>Throws a documented exception.</summary>
                    /// <exception cref="System.Exception">A failure.</exception>
                    public void Throws() { }
                    /// <summary>Invalid exception target.</summary>
                    /// <exception cref="System.String">Not an exception.</exception>
                    public void BadThrows() { }
                    /// <summary>No optional tags needed.</summary>
                    public int UndocumentedReturn() => 0;
                }
                """);
            Finding[] structural = new HygieneEngine(repo).Check([], false).Findings.Where(f => f.RuleId == "docs.xml.consistent").ToArray();
            await Assert.That(structural.Length).IsEqualTo(4);
            await Assert.That(structural.Any(f => f.Message.Contains("value-returning callable", StringComparison.Ordinal))).IsTrue();
            await Assert.That(structural.Any(f => f.Message.Contains("must contain prose", StringComparison.Ordinal))).IsTrue();
            await Assert.That(structural.Any(f => f.Message.Contains("property or indexer", StringComparison.Ordinal))).IsTrue();
            await Assert.That(structural.Any(f => f.Message.Contains("exception type", StringComparison.Ordinal))).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC20InlineSentenceBoundariesAndPermissiveElementsAreEnforcedPrecisely()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), """
                /// <summary>Returns a <see cref="System.String"/>.</summary>
                public class Punctuated { }
                /// <summary>Returns a <see cref="System.String"/></summary>
                public class Unpunctuated { }
                /// <summary>A valid summary.</summary>
                /// <example>an example without a period</example>
                /// <custom>custom prose</custom>
                public class Permissive { }
                """);
            Finding[] sentences = new HygieneEngine(repo).Check([], false).Findings.Where(f => f.RuleId == "docs.text.sentence").ToArray();
            await Assert.That(sentences.Length).IsEqualTo(1);
            await Assert.That(sentences[0].Symbol!.Contains("Unpunctuated", StringComparison.Ordinal)).IsTrue();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC21SemanticSummaryPopulationUsesOnlyLocalSummaryCarriers()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), """
                /// <summary>Concrete container purpose.</summary>
                /// <typeparam name="T">The element type.</typeparam>
                /// <param name="Value">The stored domain value.</param>
                public record Container<T>(T Value);
                /// <inheritdoc/>
                public class Inherited { }
                /// <summary>A callable operation.</summary>
                /// <param name="optional">This is optional parameter prose.</param>
                public class Api { public void Run(int optional) { } }
                """);
            ReviewBatch batch = new HygieneEngine(repo).Check([], false).ReviewBatches.Single(b => b.RuleId == "docs.summary.quality.review");
            string[] subjects = batch.PopulationItems!.Select(item => item.Symbol!).ToArray();
            await Assert.That(subjects.Any(name => name.Contains("Container", StringComparison.Ordinal))).IsTrue();
            await Assert.That(subjects.Any(name => name.Contains("Value", StringComparison.Ordinal))).IsTrue();
            await Assert.That(subjects.Any(name => name.Contains("Inherited", StringComparison.Ordinal))).IsFalse();
            await Assert.That(subjects.Any(name => name.Contains("optional", StringComparison.Ordinal))).IsFalse();
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC17NestedInheritdocDoesNotExemptMissingDeclarationSummary()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), "/// <remarks><inheritdoc/></remarks>\npublic class NestedInheritdoc { }\n");
            Finding[] findings = new HygieneEngine(repo).Check([], false).Findings.Where(f => f.RuleId == "docs.summary.required").ToArray();
            await Assert.That(findings.Length).IsEqualTo(1);
        }
        finally { DeleteTree(repo); }
    }

    [Test]
    public async Task EC04UpdateRepairsProfileOwnedDriftWithoutOverwritingUserContent()
    {
        string repo = await NewProfileRepo(withProject: true);
        try
        {
            string props = Path.Combine(repo, ".hygiene", "profile", "Hygiene.props");
            await File.WriteAllTextAsync(props, "<Project />\n");
            string rootProps = Path.Combine(repo, "Directory.Build.props");
            await File.AppendAllTextAsync(rootProps, "<!-- user-owned -->\n");
            ProfileResult update = new ProfileManager(repo).Update();
            await Assert.That(update.ChangedPaths.Contains(".hygiene/profile/Hygiene.props")).IsTrue();
            await Assert.That((await File.ReadAllTextAsync(props)).Contains("<AnalysisLevel>11</AnalysisLevel>", StringComparison.Ordinal)).IsTrue();
            await Assert.That((await File.ReadAllTextAsync(rootProps)).Contains("user-owned", StringComparison.Ordinal)).IsTrue();
            await Assert.That(new ProfileManager(repo).Update().ChangedPaths.Count).IsEqualTo(0);
        }
        finally { DeleteTree(repo); }
    }

    private static async Task<string> NewProfileRepo(bool withProject = false)
    {
        string repo = Path.Combine(Path.GetTempPath(), "hygiene-m0005-evidence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        await RunGit(repo, "init", "-q");
        if (withProject)
        {
            Directory.CreateDirectory(Path.Combine(repo, "src"));
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(repo, "src", "Api.cs"), "public class Api { }\n");
        }
        _ = new ProfileManager(repo).Bootstrap();
        return repo;
    }

    private static async Task RunGit(string cwd, params string[] args)
    {
        var start = new System.Diagnostics.ProcessStartInfo("git") { WorkingDirectory = cwd, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
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
