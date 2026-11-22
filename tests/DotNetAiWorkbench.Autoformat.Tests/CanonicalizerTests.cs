using DotNetAiWorkbench.Autoformat.Core;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace DotNetAiWorkbench.Autoformat.Tests;

public class CanonicalizerTests
{
    [Test]
    public async Task EngineNormalizesDisposableSdkProjectAndSecondPassIsStable()
    {
        string root = Path.Combine(Path.GetTempPath(), "autoformat-normalize-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        string project = Path.Combine(root, "Fixture.csproj");
        string sourcePath = Path.Combine(root, "Sample.cs");
        await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        string fixtureSource = "using System.Collections.Generic;\nclass Sample { int _value; public Sample(int value) { _value = value; } int Value() { return 1; } IReadOnlyList<int> Items() { return new List<int> { 1, 2, 3 }; } void Process(int first, string second, bool third) { }\n // autoformat:retain readability.comments -- keep the annotated member's layout\n void KeepComments() {\n // keep this comment's indentation\n Call();\n CallAgain();\n }\n}";
        await File.WriteAllTextAsync(sourcePath, fixtureSource);
        using (Process restore = Process.Start(new ProcessStartInfo("dotnet", ["restore", project, "--ignore-failed-sources"]) { WorkingDirectory = root, UseShellExecute = false })!)
        {
            await restore.WaitForExitAsync();
            await Assert.That(restore.ExitCode).IsEqualTo(0);
        }

        var engine = new RewriteEngine(root, requireCleanCheckpoint: false);
        RewriteResult first = engine.Rewrite("normalize", [sourcePath], changed: false, checkOnly: false);
        string normalized = await File.ReadAllTextAsync(sourcePath);
        await Assert.That(first.ChangedCount).IsEqualTo(1);
        await Assert.That(normalized).Contains("class Sample(int value)");
        await Assert.That(normalized).Contains("IReadOnlyList<int> Items() => [1,");
        await Assert.That(normalized).Contains("readonly int _value");
        string normalizedLines = normalized.Replace("\r\n", "\n", StringComparison.Ordinal);
        await Assert.That(normalizedLines).Contains("void Process(\n        int first,\n        string second,\n        bool third\n    )");
        await Assert.That(normalizedLines).Contains("\n // autoformat:retain readability.comments -- keep the annotated member's layout\n");
        await Assert.That(normalizedLines).Contains("void KeepComments()");
        await Assert.That(normalizedLines).Contains("\n // keep this comment's indentation\n");
        RewriteResult second = engine.Rewrite("normalize", [sourcePath], changed: false, checkOnly: false);
        string normalizedSecond = await File.ReadAllTextAsync(sourcePath);
        await Assert.That(normalizedSecond).IsEqualTo(normalized);
        await Assert.That(second.ChangedCount).IsEqualTo(0);

        RewriteResult cleanCheck = engine.Rewrite("normalize", [sourcePath], changed: false, checkOnly: true);
        await Assert.That(cleanCheck.ChangedCount).IsEqualTo(0);
        string violation = normalized.Replace("int Value", "int  Value", StringComparison.Ordinal);
        await File.WriteAllTextAsync(sourcePath, violation);
        RewriteResult dirtyCheck = engine.Rewrite("normalize", [sourcePath], changed: false, checkOnly: true);
        await Assert.That(dirtyCheck.ChangedCount).IsEqualTo(1);
        await Assert.That(await File.ReadAllTextAsync(sourcePath)).IsEqualTo(violation);
    }

    [Test]
    public async Task EngineCanonicalizesCompactFlowBoundariesInOnePass()
    {
        string root = Path.Combine(Path.GetTempPath(), "autoformat-flow-convergence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        string project = Path.Combine(root, "Fixture.csproj");
        string source = Path.Combine(root, "Flow.cs");
        await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        const string fixture = "class Sample { static void Run(string[] args) { var count = 0; for (int i = 0; i < args.Length; i++) { if (args[i].Length == 0) { continue; } count++; } if (count > 1) { count--; } } }\n";
        await File.WriteAllTextAsync(source, fixture);
        using (Process restore = Process.Start(new ProcessStartInfo("dotnet", ["restore", project, "--ignore-failed-sources"]) { WorkingDirectory = root, UseShellExecute = false })!)
        {
            await restore.WaitForExitAsync();
            await Assert.That(restore.ExitCode).IsEqualTo(0);
        }

        try
        {
            var engine = new RewriteEngine(root, requireCleanCheckpoint: false);
            RewriteResult first = engine.Rewrite("normalize", [source], changed: false, checkOnly: false);
            string canonical = await File.ReadAllTextAsync(source);
            RewriteResult second = engine.Rewrite("normalize", [source], changed: false, checkOnly: false);
            RewriteResult normalizeCheck = engine.Rewrite("normalize", [source], changed: false, checkOnly: true);
            RewriteResult formatCheck = engine.Rewrite("format", [source], changed: false, checkOnly: true);

            await Assert.That(first.ChangedCount).IsEqualTo(1);
            await Assert.That(second.ChangedCount).IsEqualTo(0);
            await Assert.That(normalizeCheck.ChangedCount).IsEqualTo(0);
            await Assert.That(formatCheck.ChangedCount).IsEqualTo(0);
            await Assert.That(await File.ReadAllTextAsync(source)).IsEqualTo(canonical);
        }
        finally
        {
            if (Directory.Exists(root)) { Directory.Delete(root, recursive: true); }
        }
    }

    [Test]
    public async Task AppliesAllNineModernizationFamiliesToRecognizedShapes()
    {
        string source = """
            using System.Collections.Generic;
            class Sample {
              int _value;
              public Sample(int value) { _value = value; }
              int Get(int x) { return x + 1; }
              int Pick(int x) {
                switch (x) { case 0: return 10; default: return 20; }
              }
              IReadOnlyList<int> Items() { return new List<int> { 1, 2, 3 }; }
              void Run() {
                var answer = 42;
                IReadOnlyList<int> values = new List<int> { 4, 5, 6 };
                var concrete = (List<int>)values;
                int found;
                Try(out found);
                var item = new Sample(1);
                item._value = 3;
                if (answer > 0) { answer = 2; } else { answer = 3; }
              }
              bool Try(out int value) { value = 0; return true; }
            }
            """;
        string result = CSharpCanonicalizer.Normalize(source, null, out _);
        await Assert.That(result.Replace(" ", "", StringComparison.Ordinal)).Contains("classSample(intvalue)");
        await Assert.That(result.Replace(" ", "", StringComparison.Ordinal)).Contains("readonlyint_value");
        await Assert.That(result.Replace(" ", "", StringComparison.Ordinal)).Contains("intGet(intx)=>x+1;");
        await Assert.That(result).Contains("x switch");
        await Assert.That(result.Replace(" ", "", StringComparison.Ordinal)).Contains("IReadOnlyList<int>Items()=>[1,");
        await Assert.That(result.Replace(" ", "", StringComparison.Ordinal)).Contains("IReadOnlyList<int>values=[4,5,6];");
        await Assert.That(result).Contains("int answer = 42;");
        await Assert.That(result).Contains("out int found");
        await Assert.That(result.Replace(" ", "", StringComparison.Ordinal)).Contains("answer=answer>0?2:3;");
        await Assert.That(CSharpSyntaxTree.ParseText(result).GetDiagnostics().Any(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)).IsFalse();
    }

    [Test]
    public async Task RetainsOnlyNamedRuleAndConverges()
    {
        string source = "class Sample {\n// autoformat:retain modernization.expression-body -- public event accessor\nint Value() { return 1; } int Other() { return 2; } }";
        string first = CSharpCanonicalizer.Normalize(source, null, out _);
        string second = CSharpCanonicalizer.Normalize(first, null, out _);
        await Assert.That(first).Contains("int Value() { return 1; }");
        await Assert.That(first.Replace(" ", "", StringComparison.Ordinal)).Contains("intOther()=>2;");
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task PreservesCommentsOnBlockBodiesInsteadOfDroppingThemIntoExpressionBodies()
    {
        const string source = "class Sample { int Value() { // keep method rationale\nreturn 1; } int Property { get { /* keep getter rationale */ return 2; } } }";
        string result = CSharpCanonicalizer.Normalize(source, null, out _);
        await Assert.That(result).Contains("// keep method rationale");
        await Assert.That(result).Contains("/* keep getter rationale */");
        await Assert.That(result).Contains("int Value() {");
        await Assert.That(result).Contains("get {");
    }

    [Test]
    public async Task KeepsTargetTypedObjectInitializersAndConvertsExplicitListInitializers()
    {
        const string source = "class Customer { public string Name { get; set; } = \"\"; public int Age { get; set; } } class Sample { Customer customer = new() { Name = \"Alice\", Age = 35 }; IReadOnlyList<int> values = new List<int> { 1, 2 }; List<int> concrete = new() { 3, 4 }; }";
        string result = CSharpCanonicalizer.Normalize(source, null, out _);
        await Assert.That(result).Contains("Customer customer = new() { Name = \"Alice\", Age = 35 }");
        await Assert.That(result).Contains("IReadOnlyList<int> values = [1, 2]");
        await Assert.That(result).Contains("List<int> concrete = [3, 4]");
        await Assert.That(CSharpSyntaxTree.ParseText(result).GetDiagnostics().Any(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)).IsFalse();
    }

    [Test]
    public async Task RestrictsCollectionExpressionsToSupportedInitializerShapes()
    {
        const string source = "class Sample { List<int> values = new List<int> { Capacity = 10 }; int[,] grid = new int[,] { { 1, 2 }, { 3, 4 } }; IReadOnlyList<int> items = new List<int> { 5, 6 }; int[] vector = new int[] { 7, 8 }; }";
        string first = CSharpCanonicalizer.Normalize(source, null, out _);
        string second = CSharpCanonicalizer.Normalize(first, null, out _);
        await Assert.That(first).Contains("List<int> values = new List<int> { Capacity = 10 }");
        await Assert.That(first).Contains("int[,] grid = new int[,] { { 1, 2 }, { 3, 4 } }");
        await Assert.That(first).Contains("IReadOnlyList<int> items = [5, 6]");
        await Assert.That(first).Contains("int[] vector = [7, 8]");
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task CollectionExpressionsRequireARecognizedTargetTypeContext()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            class Sample {
              IReadOnlyList<int> Property { get; } = new List<int> { 1, 2 };
              int[] ArrayProperty => new int[] { 3, 4 };
              IReadOnlyList<int> Items() { return new List<int> { 5, 6 }; }
              void Consume(int[] values) { }
              void Run() {
                foreach (var name in new[] { "added", "modified", "deleted" }) { }
                foreach (var values in new[] { new[] { 1 }, new[] { 2 } }) { }
                foreach (var key in new[] { "alias", "packageId", "version" }) { }
                Consume(new[] { 7, 8 });
                var inferred = new[] { 9, 10 };
                List<int> concrete = new List<int> { 11, 12 };
                IReadOnlyList<int> collectionInterface = new List<int> { 13, 14 };
              }
              IEnumerable<int> Outer() {
                object Local() { return new[] { 1, 2 }; }
                IEnumerable<int> LocalItems() { return new[] { 3, 4 }; }
                Func<object> callback = () => { return new[] { 5, 6 }; };
                _ = Local();
                _ = LocalItems();
                _ = callback();
                return new[] { 7, 8 };
              }
            }
            """;

        string result = CSharpCanonicalizer.Normalize(source, null, out _);

        await Assert.That(result).Contains("foreach (var name in new[] { \"added\", \"modified\", \"deleted\" })");
        await Assert.That(result).Contains("foreach (var values in new[] { new[] { 1 }, new[] { 2 } })");
        await Assert.That(result).Contains("foreach (var key in new[] { \"alias\", \"packageId\", \"version\" })");
        await Assert.That(result).Contains("Consume(new[] { 7, 8 })");
        await Assert.That(result).Contains("int[] inferred = [9, 10]");
        await Assert.That(result).Contains("IReadOnlyList<int> Property { get; } = [1, 2]");
        await Assert.That(result).Contains("int[] ArrayProperty => [3, 4]");
        await Assert.That(result).Contains("IReadOnlyList<int> Items() =>[5, 6]");
        await Assert.That(result).Contains("List<int> concrete = [11, 12]");
        await Assert.That(result).Contains("IReadOnlyList<int> collectionInterface = [13, 14]");
        await Assert.That(result).Contains("object Local() { return new[] { 1, 2 }; }");
        await Assert.That(result).Contains("IEnumerable<int> LocalItems() { return [3, 4]; }");
        await Assert.That(result).Contains("Func<object> callback = () => { return new[] { 5, 6 }; }");
        await Assert.That(result).Contains("return [7, 8];");
    }

    [Test]
    public async Task KeepsConstructorAttributesAndTriviaWhenPrimaryConstructorCannotRepresentThem()
    {
        const string source = "class Service { private readonly int _value; [Obsolete(\"Use the factory\")] public Service(int value) { _value = value; } }";
        string result = CSharpCanonicalizer.Normalize(source, null, out _);
        await Assert.That(result).Contains("[Obsolete(\"Use the factory\")]");
        await Assert.That(result).Contains("public Service(int value)");
        await Assert.That(result).DoesNotContain("class Service(int value)");
    }

    [Test]
    public async Task KeepsRestrictedConstructorsExplicitButModernizesPublicConstructors()
    {
        const string source = "public class Restricted { private readonly int _value; private Restricted(int value) { _value = value; } } public class Eligible { private readonly int _value; public Eligible(int value) { _value = value; } }";
        string result = CSharpCanonicalizer.Normalize(source, null, out _);
        await Assert.That(result).Contains("private Restricted(int value)");
        await Assert.That(result).DoesNotContain("class Restricted(int value)");
        await Assert.That(result).Contains("class Eligible(int value)");
        await Assert.That(result).DoesNotContain("public Eligible(int value)");
    }

    [Test]
    public async Task KeepsDirectiveBearingConstructorIntact()
    {
        const string source = "class Service { private readonly int _value; public Service(int value) {\n#if DEBUG\n_value = value;\n#endif\n} }";
        string result = CSharpCanonicalizer.Normalize(source, null, out _);
        await Assert.That(result).Contains("#if DEBUG");
        await Assert.That(result).Contains("#endif");
        await Assert.That(result).Contains("public Service(int value)");
        await Assert.That(result).DoesNotContain("class Service(int value)");
    }

    [Test]
    public async Task TypeLevelPrimaryConstructorRetainSuppressesOnlyThatModernization()
    {
        const string source = "// autoformat:retain modernization.primary-constructor -- Preserve explicit construction contract.\nclass Service { private readonly int _value; public Service(int value) { _value = value; } int Value() { return 1; } IReadOnlyList<int> Items = new List<int> { 2, 3 }; }";
        string first = CSharpCanonicalizer.Normalize(source, null, out _);
        string second = CSharpCanonicalizer.Normalize(first, null, out _);
        await Assert.That(first).Contains("// autoformat:retain modernization.primary-constructor -- Preserve explicit construction contract.");
        await Assert.That(first).Contains("public Service(int value)");
        await Assert.That(first).DoesNotContain("class Service(int value)");
        await Assert.That(first).Contains("int Value() =>1;");
        await Assert.That(first).Contains("IReadOnlyList<int> Items = [2, 3]");
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task FormatsTheWholeParameterListVerticallyAndConverges()
    {
        const string source = "class Sample {\n    public void Process(Customer customer, ProcessingOptions options, CancellationToken cancellationToken) { }\n}";
        string first = CSharpCanonicalizer.Normalize(source, null, out _);
        string second = CSharpCanonicalizer.Normalize(first, null, out _);
        await Assert.That(first).Contains("Process(\n        Customer customer,\n        ProcessingOptions options,\n        CancellationToken cancellationToken\n    )");
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task InlinesOutDeclarationAndAllowsFollowingUse()
    {
        const string source = "class Sample { void Run(string input) { int result; TryParse(input, out result); Console.WriteLine(result); } }";
        string result = CSharpCanonicalizer.Normalize(source, null, out _);
        await Assert.That(result).Contains("TryParse(input, out int result);");
        await Assert.That(result).Contains("Console.WriteLine(result);");
    }

    [Test]
    public async Task ResolvesRetainsFromOwningConstructAcrossModernizationAndLayoutRules()
    {
        const string source = "class Sample {\n// autoformat:retain modernization.collection-expression -- keep chosen list construction\nIReadOnlyList<int> Values = new List<int> { 1, 2, 3 };\n// autoformat:retain readability.parameters -- preserve compact declaration\nvoid Keep(int one, int two, int three) { }\nvoid Wrap(int one, int two, int three) { }\nvoid Run() { int answer = 1;\n// autoformat:retain readability.visual-block -- keep adjacent statements\nif (answer > 0) { answer++; }\n// autoformat:retain modernization.inline-declaration -- retain explicit local\nint result;\nTry(out result);\n} }";
        string first = CSharpCanonicalizer.Normalize(source, null, out _);
        string second = CSharpCanonicalizer.Normalize(first, null, out _);
        await Assert.That(first).Contains("Values = new List<int> { 1, 2, 3 }");
        await Assert.That(first).Contains("Keep(int one, int two, int three)");
        await Assert.That(first).Contains("Wrap(\n");
        await Assert.That(first).Contains("answer = 1;\n// autoformat:retain readability.visual-block -- keep adjacent statements\nif");
        await Assert.That(first).Contains("int result;");
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task RetainedLongLineAndBlankSpacingStayLocalToTheirRule()
    {
        string longArgument = new string('x', 130);
        string source = "class Sample {\nvoid Run() {\n// autoformat:retain readability.long-line -- keep this diagnostic invocation intact\nCall(\"" + longArgument + "\");\n}\n// autoformat:retain readability.comments -- preserve authored spacing\nvoid Keep() {\n\n\nCall();\nCallAgain();\n}\n}";
        string result = CSharpCanonicalizer.Normalize(source, null, out _);
        await Assert.That(result).Contains("Call(\"" + longArgument + "\")");
        await Assert.That(result).Contains("void Keep() {\n\n\nCall();");
        await Assert.That(result).Contains("Call();");
    }

    [Test]
    public async Task RestoresOnlyRetainedMemberCommentIndentationAfterSdkWhitespace()
    {
        const string source = "class Sample {\n // autoformat:retain readability.comments -- preserve comment indentation\n void Keep() {\n // preserve this comment\n Call();\n CallAgain();\n }\n}";
        IReadOnlyList<CSharpCanonicalizer.RetainedCommentIndentation> retained = CSharpCanonicalizer.CaptureRetainedCommentIndentation(source);
        await Assert.That(retained.Count).IsEqualTo(2);
        string sdkFormatted = source.Replace("\n // autoformat", "\n    // autoformat", StringComparison.Ordinal)
            .Replace("\n // preserve this comment", "\n        // preserve this comment", StringComparison.Ordinal);
        await Assert.That(CSharpCanonicalizer.RestoreRetainedCommentIndentation(sdkFormatted, retained)).IsEqualTo(source);
    }

    [Test]
    public async Task ConvertsSwitchAssignmentsToSwitchExpressions()
    {
        const string source = "class Sample { int Pick(int value) { int result; switch (value) { case 0: result = 10; break; default: result = 20; break; } return result; } }";
        string result = CSharpCanonicalizer.Normalize(source, null, out _);
        await Assert.That(result).Contains("result = value switch");
        await Assert.That(result).Contains("0 => 10");
        await Assert.That(result).Contains("_ => 20");
    }

    [Test]
    public async Task ReportsMalformedRetainAndKeepsCommentsWhileWrappingParameters()
    {
        string source = "class Sample { // keep explanatory comment\nvoid Call(int first, int second, int third) { Work(first, second, third); } }";
        string result = CSharpCanonicalizer.Normalize(source, null, out IReadOnlyList<string> notices);
        await Assert.That(result).Contains("// keep explanatory comment");
        await Assert.That(result).Contains("int first,\n");
        await Assert.That(notices.Count).IsEqualTo(0);
        _ = CSharpCanonicalizer.Normalize("// autoformat:retain modernization.nope -- reason\nclass C {}", null, out notices);
        await Assert.That(notices.Count).IsEqualTo(1);
    }

    [Test]
    public async Task WrapsLongCollectionAtElementBoundariesAndPreservesLiteralContents()
    {
        string a = new string('a', 46), b = new string('b', 46), c = new string('c', 46);
        string source = $"class Sample {{ string[] Values = new string[] {{ \"{a}\", \"{b}\", \"{c}\" }}; }}";
        string first = CSharpCanonicalizer.Normalize(source, null, out _);
        string second = CSharpCanonicalizer.Normalize(first, null, out _);
        await Assert.That(first).Contains("\n");
        await Assert.That(first).Contains(a);
        await Assert.That(first).Contains(b);
        await Assert.That(first).Contains(c);
        await Assert.That(CSharpSyntaxTree.ParseText(first).GetDiagnostics().Any(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)).IsFalse();
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task SkipsCollectionAndPrimaryConstructorGrammarWhenProjectLanguageIsCSharp11()
    {
        const string source = "class Sample { int _value; Sample(int value) { _value = value; } int[] Values = new[] { 1, 2 }; }";
        string result = CSharpCanonicalizer.Normalize(source, new CSharpParseOptions(LanguageVersion.CSharp11), out _);
        await Assert.That(result).Contains("Sample(int value)");
        await Assert.That(result).Contains("new[]");
        await Assert.That(result).DoesNotContain("[1, 2]");
    }
}
