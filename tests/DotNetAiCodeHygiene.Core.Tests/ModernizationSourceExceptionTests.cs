using DotNetAiCodeHygiene.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotNetAiCodeHygiene.Core.Tests;

public sealed class ModernizationSourceExceptionTests
{
    [Test]
    public async Task HygieneRetentionAttachesToOneSyntaxSubjectEvenWhenStatementsShareALine()
    {
        const string path = "Sample.cs";
        const string source = "// hygiene:retain modernization.var-style -- This explicit type documents an interop boundary.\nvar first = 1; int second = 2; var third = 3;\n";
        LocalDeclarationStatementSyntax[] statements = CSharpSyntaxTree.ParseText(source).GetRoot()
            .DescendantNodes().OfType<LocalDeclarationStatementSyntax>().ToArray();
        ReviewOccurrence[] occurrences =
        [
            Occurrence("first", "IDE0008", path, 2, statements[0].Span),
            Occurrence("same-subject", "IDE0008", path, 2, new TextSpan(statements[0].SpanStart + 1, statements[0].Span.Length - 2)),
            Occurrence("second", "IDE0008", path, 2, statements[1].Span),
            Occurrence("third", "IDE0008", path, 2, statements[2].Span)
        ];
        IReadOnlyDictionary<string, string> sources = new Dictionary<string, string> { [path] = source };

        ReviewOccurrence[] normal = ModernizationSourceExceptions.Filter("modernization.var-style", occurrences, sources,
            exhaustiveAudit: false, out int suppressed);
        ReviewOccurrence[] audit = ModernizationSourceExceptions.Filter("modernization.var-style", occurrences, sources,
            exhaustiveAudit: true, out int auditSuppressed);

        await Assert.That(suppressed).IsEqualTo(2);
        await Assert.That(normal.Select(item => item.Id).ToArray()).IsEquivalentTo(["second", "third"]);
        await Assert.That(auditSuppressed).IsEqualTo(2);
        await Assert.That(audit.Where(item => item.SourceExceptionReason is not null).Select(item => item.Id).ToArray())
            .IsEquivalentTo(["first", "same-subject"]);
        await Assert.That(audit.Single(item => item.Id == "second").SourceExceptionReason).IsNull();
    }

    [Test]
    public async Task HygieneRetentionDoesNotFilterAnUnrelatedSameLineCandidate()
    {
        const string source = "// hygiene:retain modernization.var-style -- This explicit type documents an interop boundary.\nvar first = 1; int second = 2;\n";
        IReadOnlyList<ModernizationRetention> exceptions = ModernizationSourceExceptions.Parse("Sample.cs", source);
        LocalDeclarationStatementSyntax candidate = CSharpSyntaxTree.ParseText(source).GetRoot()
            .DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Last();
        ReviewOccurrence[] remaining = [Occurrence("second", "IDE0008", "Sample.cs", 2, candidate.Span)];
        ReviewOccurrence[] filtered = ModernizationSourceExceptions.Filter("modernization.var-style", remaining,
            new Dictionary<string, string> { ["Sample.cs"] = source },
            exhaustiveAudit: false, out int suppressed);
        await Assert.That(exceptions.Single().Valid).IsTrue();
        await Assert.That(suppressed).IsEqualTo(0);
        await Assert.That(filtered.Select(item => item.Id).ToArray()).IsEquivalentTo(["second"]);
    }

    [Test]
    public async Task RetentionBeforeClosingBracesIsUnattachedAndCannotSuppressEarlierCandidates()
    {
        const string annotation = "// hygiene:retain modernization.var-style -- Explicit typing is required by an interoperability constraint.";
        string[] sources =
        [
            $"class MethodClose {{ void Run() {{ var earlier = 42;\n{annotation}\n}} }}\n",
            $"class TypeClose {{ void Run() {{ var earlier = 42; }}\n{annotation}\n}}\n",
            $"class NestedClose {{ void Run() {{ {{ var earlier = 42;\n{annotation}\n}} }} }}\n"
        ];

        for (int index = 0; index < sources.Length; index++)
        {
            string path = $"Closing{index}.cs";
            SyntaxNode root = CSharpSyntaxTree.ParseText(sources[index]).GetRoot();
            LocalDeclarationStatementSyntax earlier = root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
            IReadOnlyList<ModernizationRetention> parsed = ModernizationSourceExceptions.Parse(path, sources[index]);
            ReviewOccurrence[] candidates = [Occurrence("earlier", "IDE0008", path, 1, earlier.Span)];
            IReadOnlyDictionary<string, string> sourceMap = new Dictionary<string, string> { [path] = sources[index] };

            ReviewOccurrence[] normal = ModernizationSourceExceptions.Filter("modernization.var-style", candidates, sourceMap,
                exhaustiveAudit: false, out int normalSuppressed);
            ReviewOccurrence[] audit = ModernizationSourceExceptions.Filter("modernization.var-style", candidates, sourceMap,
                exhaustiveAudit: true, out int auditSuppressed);

            await Assert.That(parsed.Count).IsEqualTo(1);
            await Assert.That(parsed[0].Valid).IsFalse();
            await Assert.That(parsed[0].SubjectSpan.Length).IsEqualTo(0);
            await Assert.That(normalSuppressed).IsEqualTo(0);
            await Assert.That(normal.Select(item => item.Id).ToArray()).IsEquivalentTo(["earlier"]);
            await Assert.That(auditSuppressed).IsEqualTo(0);
            await Assert.That(audit.Single().SourceExceptionReason).IsNull();
        }
    }

    [Test]
    public async Task RetentionAttachesToTheNextStatementOrMemberDeclaration()
    {
        const string annotation = "// hygiene:retain modernization.var-style -- Explicit typing is required by an interoperability constraint.";
        const string statementSource = $"class StatementTarget {{ void Run() {{\n{annotation}\nvar value = 42;\n}} }}\n";
        const string memberSource = $"class MemberTarget {{\n{annotation}\nvoid Run() {{ }}\n}}\n";
        SyntaxNode statementRoot = CSharpSyntaxTree.ParseText(statementSource).GetRoot();
        SyntaxNode memberRoot = CSharpSyntaxTree.ParseText(memberSource).GetRoot();
        StatementSyntax statement = statementRoot.DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
        MethodDeclarationSyntax member = memberRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        ModernizationRetention statementRetention = ModernizationSourceExceptions.Parse("Statement.cs", statementSource).Single();
        ModernizationRetention memberRetention = ModernizationSourceExceptions.Parse("Member.cs", memberSource).Single();

        await Assert.That(statementRetention.Valid).IsTrue();
        await Assert.That(statementRetention.SubjectSpan).IsEqualTo(statement.Span);
        await Assert.That(memberRetention.Valid).IsTrue();
        await Assert.That(memberRetention.SubjectSpan).IsEqualTo(member.Span);
    }

    [Test]
    public async Task NativePragmaMatchesExactDiagnosticWithinItsModernizationFamily()
    {
        const string path = "Sample.cs";
        const string source = "#pragma warning disable IDE0007 // Explicit typing is required at this interoperability boundary.\nclass Sample { void Run() { var inferred = new Widget(); int explicitValue = 1; } }\n#pragma warning restore IDE0007\nclass Widget { }\n";
        LocalDeclarationStatementSyntax[] statements = CSharpSyntaxTree.ParseText(source).GetRoot()
            .DescendantNodes().OfType<LocalDeclarationStatementSyntax>().ToArray();
        ReviewOccurrence[] occurrences =
        [
            Occurrence("ide0008", "IDE0008", path, 2, statements[0].Span),
            Occurrence("ide0007", "IDE0007", path, 2, statements[1].Span)
        ];
        IReadOnlyDictionary<string, string> sources = new Dictionary<string, string> { [path] = source };

        ReviewOccurrence[] normal = ModernizationSourceExceptions.Filter("modernization.var-style", occurrences, sources,
            exhaustiveAudit: false, out int suppressed);
        ReviewOccurrence[] audit = ModernizationSourceExceptions.Filter("modernization.var-style", occurrences, sources,
            exhaustiveAudit: true, out int auditSuppressed);

        await Assert.That(suppressed).IsEqualTo(1);
        await Assert.That(normal.Select(item => item.Id).ToArray()).IsEquivalentTo(["ide0008"]);
        await Assert.That(auditSuppressed).IsEqualTo(1);
        await Assert.That(audit.Single(item => item.Id == "ide0007").SourceExceptionReason)
            .IsEqualTo("Explicit typing is required at this interoperability boundary.");
        await Assert.That(audit.Single(item => item.Id == "ide0008").SourceExceptionReason).IsNull();
    }

    [Test]
    public async Task NativePragmaRequiresActiveMatchedScopedDirectiveAndRealCommentRationale()
    {
        const string reason = "An explicit type is required by the serializer boundary.";
        string[] valid =
        [
            $"#pragma warning disable IDE0090 // {reason}\nclass A {{ object Value = new object(); }}\n#pragma warning restore IDE0090\n",
            $"// {reason}\n#pragma warning disable IDE0090\nclass A {{ object Value = new object(); }}\n#pragma warning restore IDE0090\n"
        ];
        foreach (string source in valid)
        {
            ModernizationRetention retention = ModernizationSourceExceptions.Parse("Sample.cs", source).Single();
            await Assert.That(retention.NativeDiagnosticId).IsEqualTo("IDE0090");
            await Assert.That(retention.RuleId).IsEqualTo("sdk.IDE0090");
            await Assert.That(retention.Reason).IsEqualTo(reason);
            await Assert.That(retention.Valid).IsTrue();
        }

        string[] invalid =
        [
            "public class Before { }\n#pragma warning disable IDE0090\nclass A { object Value = new object(); }\n#pragma warning restore IDE0090\n",
            "#pragma warning disable IDE0090\nclass A { object Value = new object(); }\n#pragma warning restore IDE0090\n",
            "#pragma warning disable IDE0090 // keep\nclass A { object Value = new object(); }\n#pragma warning restore IDE0090\n",
            $"#pragma warning disable IDE0090, IDE0034 // {reason}\nclass A {{ object Value = new object(); }}\n#pragma warning restore IDE0090, IDE0034\n",
            $"#pragma warning disable IDE0090 // {reason}\nclass A {{ object Value = new object(); }}\n",
            $"#pragma warning disable IDE0090 // {reason}\n{string.Join("\n", Enumerable.Repeat("// bounded context", 13))}\n#pragma warning restore IDE0090\n"
        ];
        foreach (string source in invalid)
        {
            await Assert.That(ModernizationSourceExceptions.Parse("Sample.cs", source).Any(item => !item.Valid)).IsTrue();
        }

        string literalAndInactive = "class A { string Normal = \"#pragma warning disable IDE0090\"; string Verbatim = @\"#pragma warning restore IDE0090\"; string Raw = \"\"\"#pragma warning disable IDE0090\"\"\"; }\n#if NEVER_DEFINED\n#pragma warning disable IDE0090 // This explicit type is required by the serializer boundary.\n#endif\n";
        await Assert.That(ModernizationSourceExceptions.Parse("Sample.cs", literalAndInactive)).IsEmpty();
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

    private static ReviewOccurrence Occurrence(string id, string diagnostic, string path, int line, TextSpan span) =>
        new ReviewOccurrence(id, diagnostic, path, line, span.Start + 1, "Sample.Run", "var value = 1", "context", "preview", "var value = 1", "question")
        {
            CandidateSpan = span
        };
}
