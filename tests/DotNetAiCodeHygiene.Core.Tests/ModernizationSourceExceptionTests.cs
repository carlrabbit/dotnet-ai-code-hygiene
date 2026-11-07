using DotNetAiCodeHygiene.Core;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class ModernizationSourceExceptionTests
{
    [Test]
    public async Task HygieneRetentionCommentFiltersOnlyTheNamedAttachedOccurrence()
    {
        const string path = "Sample.cs";
        const string source = "public class Sample\n{\n    public void Run()\n    {\n        // hygiene:retain modernization.var-style -- This explicit type documents an interop boundary.\n        int first = 1;\n        var second = 2;\n    }\n}\n";
        ReviewOccurrence[] occurrences =
        [
            Occurrence("modernization.var-style", path, 6),
            Occurrence("modernization.var-style", path, 7),
            Occurrence("modernization.expression-body", path, 3)
        ];
        IReadOnlyDictionary<string, string> sources = new Dictionary<string, string> { [path] = source };

        ReviewOccurrence[] normal = ModernizationSourceExceptions.Filter("modernization.var-style", occurrences[..2], sources, exhaustiveAudit: false, out int suppressed);
        ReviewOccurrence[] audit = ModernizationSourceExceptions.Filter("modernization.var-style", occurrences[..2], sources, exhaustiveAudit: true, out _);

        await Assert.That(suppressed).IsEqualTo(1);
        await Assert.That(normal.Select(item => item.Line).ToArray()).IsEquivalentTo([7]);
        await Assert.That(audit.Single(item => item.Line == 6).SourceExceptionReason).IsEqualTo("This explicit type documents an interop boundary.");
        await Assert.That(ModernizationSourceExceptions.Filter("modernization.expression-body", occurrences[2..], sources, false, out _).Length).IsEqualTo(1);
    }

    [Test]
    public async Task UnknownUnjustifiedAndOrphanRetentionCommentsAreInvalid()
    {
        string[] sources =
        [
            "// hygiene:retain modernization.unknown-rule -- A concrete reason that is long enough.\npublic class Sample { }",
            "// hygiene:retain modernization.var-style -- keep it\npublic class Sample { }",
            "// hygiene:retain modernization.var-style -- This is a concrete interop boundary constraint."
        ];
        foreach (string source in sources)
        {
            await Assert.That(ModernizationSourceExceptions.Parse("Sample.cs", source).Any(item => !item.Valid)).IsTrue();
        }
    }

    [Test]
    public async Task SingleDiagnosticPragmaRequiresReasonAndMatchingRestore()
    {
        const string valid = "#pragma warning disable IDE0090 // An explicit type is needed by the generated serializer contract.\nclass A { object value = new object(); }\n#pragma warning restore IDE0090\n";
        const string invalid = "#pragma warning disable IDE0090\nclass A { object value = new object(); }\n";
        await Assert.That(ModernizationSourceExceptions.Parse("Sample.cs", valid).Any(item => !item.Valid)).IsFalse();
        await Assert.That(ModernizationSourceExceptions.Parse("Sample.cs", invalid).Any(item => !item.Valid)).IsTrue();
    }

    [Test]
    public async Task NativePragmaRetainsOnlyTheMatchingModernizationDiagnostic()
    {
        const string path = "Sample.cs";
        const string source = "#pragma warning disable IDE0007 // An explicit declaration is needed at this interop boundary.\nclass A { void Run() { var value = 1; } }\n#pragma warning restore IDE0007\n";
        ReviewOccurrence occurrence = Occurrence("modernization.var-style", path, 2);
        ReviewOccurrence[] retained = ModernizationSourceExceptions.Filter("modernization.var-style", [occurrence],
            new Dictionary<string, string> { [path] = source }, exhaustiveAudit: true, out int suppressed);
        await Assert.That(suppressed).IsEqualTo(1);
        await Assert.That(retained.Single().SourceExceptionReason).IsEqualTo("An explicit declaration is needed at this interop boundary.");
    }

    private static ReviewOccurrence Occurrence(string rule, string path, int line) =>
        new("id-" + line, "IDE0008", path, line, 1, "Sample.Run", "var value = 1", "context", "preview", "var value = 1", "question");
}
