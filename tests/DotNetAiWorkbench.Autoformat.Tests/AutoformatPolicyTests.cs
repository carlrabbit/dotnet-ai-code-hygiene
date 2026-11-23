using DotNetAiWorkbench.Autoformat.Core;

namespace DotNetAiWorkbench.Autoformat.Tests;

public sealed class AutoformatPolicyTests
{
    [Test]
    public async Task InitializationPreservesUnrelatedEditorConfigContent()
    {
        string root = await CreateRepositoryAsync();
        try
        {
            const string unrelated = "root = true\n# user-owned policy\n[*.cs]\nend_of_line = lf\n";
            string config = Path.Combine(root, ".editorconfig");
            await File.WriteAllTextAsync(config, unrelated);

            await Assert.That(AutoformatPolicy.EnsureInitialized(root)).IsTrue();
            string initialized = await File.ReadAllTextAsync(config);
            await Assert.That(initialized.StartsWith(unrelated, StringComparison.Ordinal)).IsTrue();
            await Assert.That(initialized).Contains(AutoformatPolicy.StartMarker);
            await Assert.That(initialized).Contains(AutoformatPolicy.EndMarker);
        }
        finally { DeleteTree(root); }
    }

    [Test]
    public async Task InitializationRejectsEffectiveNestedMechanicalConflict()
    {
        string root = await CreateRepositoryAsync();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, ".editorconfig"), "root = true\n[*.cs]\nindent_size = 4\n");
            string nested = Path.Combine(root, "src");
            Directory.CreateDirectory(nested);
            await File.WriteAllTextAsync(Path.Combine(nested, ".editorconfig"), "[*.cs]\nindent_size = 2\n");
            await File.WriteAllTextAsync(Path.Combine(nested, "Nested.cs"), "class Nested {}\n");

            ProductException? conflict = null;
            try { _ = AutoformatPolicy.EnsureInitialized(root); }
            catch (ProductException error) { conflict = error; }

            await Assert.That(conflict).IsNotNull();
            await Assert.That(conflict!.Message).Contains("Effective mechanical setting conflict");
            await Assert.That(conflict.Message).Contains("indent_size");
            await Assert.That(await File.ReadAllTextAsync(Path.Combine(root, ".editorconfig"))).DoesNotContain(AutoformatPolicy.StartMarker);
        }
        finally { DeleteTree(root); }
    }

    [Test]
    public async Task InitializationPreservesConcurrentEditorConfigChange()
    {
        string root = await CreateRepositoryAsync();
        try
        {
            string config = Path.Combine(root, ".editorconfig");
            const string original = "root = true\n# user-owned policy\n";
            await File.WriteAllTextAsync(config, original);
            const string concurrent = original + "# concurrent editor change\n";

            ProductException? conflict = null;
            try
            {
                _ = AutoformatPolicy.EnsureInitialized(root, () => File.WriteAllText(config, concurrent));
            }
            catch (ProductException error) { conflict = error; }

            await Assert.That(conflict).IsNotNull();
            await Assert.That(conflict!.Message).Contains("changed concurrently");
            await Assert.That(await File.ReadAllTextAsync(config)).IsEqualTo(concurrent);
        }
        finally { DeleteTree(root); }
    }

    private static async Task<string> CreateRepositoryAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "autoformat-policy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        await File.WriteAllTextAsync(Path.Combine(root, "Fixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        await File.WriteAllTextAsync(Path.Combine(root, "Fixture.cs"), "class Fixture {}\n");
        return root;
    }

    private static void DeleteTree(string root)
    {
        if (!Directory.Exists(root)) { return; }
        foreach (string path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) { File.SetAttributes(path, FileAttributes.Normal); }
        Directory.Delete(root, recursive: true);
    }
}
