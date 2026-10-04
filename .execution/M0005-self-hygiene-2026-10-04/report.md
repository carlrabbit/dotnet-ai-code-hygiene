# M0005 Self-Hygiene Detailed Report

Run date: 2026-10-04. Scope: M0005 only. This artifact preserves the complete installed-tool run output and a separately classified view limited to C# lines added or modified by M0005.

## 1. Assessment

**M0005-scoped self-hygiene: FAIL for closure.** The tool ran correctly, but it reported 31 `docs.summary.required` v2 violations on M0005-added/modified public or internal product declarations. These directly concern AC-13, which requires a non-empty summary for every covered subject. The semantic-review workflow also selected the complete eligible population and marked both sampled summaries as failing the German-quality question (Q1). No source files were changed during this review; the deliverable is this report and raw evidence.

The human completion approval previously recorded for `REV-M0005-COMPLETION` is not changed by this dogfooding artifact. This run adds evidence that should be considered before relying on the earlier closure assessment.

## 2. Tool and execution context

- Tool: `DotNetAiCodeHygiene.Tool` 0.5.0, installed from `artifacts/packages/DotNetAiCodeHygiene.Tool.0.5.0.nupkg`.
- Package SHA-256: `BC4F525E858C9CE6513FF4F9DAB5A022C705CCDA0D5C4C95AF4E0134078F329A`; installed launcher SHA-256: `78263AED2C4407DDB199A6AD11C2EFE2D2DFDB674BED39C0F5B0315AEC4FBA4C`.
- Run location: repository root `C:\src\dotnet-ai-code-hygiene`; branch `codex/M0005-supported-dotnet-profile`; current commit `0b48571ab4920ea9ef8eda3c873c7090c5642424`. Product code in the package was built from `83456ff`; the later `0b48571` commit only records human approval in milestone/ledger documentation.
- Platform: Windows 11 Home, build 26200; PowerShell 7.6.5; .NET SDK `11.0.100-rc.1.26425.128`.
- Profile state: `.hygiene/profile.json` = `dotnet-11` version 1. No `.hygiene/config.json` or `.hygiene/decisions.json` exists; no rules were disabled or ignored.
- Diagnostics: all commands exited 0; captured stderr was empty. Current-run state was written under engine-owned `.hygiene/.state`; check/rewrite commands did not mutate C# source.

### Exact command sequence

All commands ran from the repository root with the installed `hygiene.exe`. No positional targets and no `--changed` option were supplied.

```text
hygiene.exe --version
hygiene.exe help --agent
hygiene.exe format --check --output json
hygiene.exe normalize --check --output json
hygiene.exe check --output json
hygiene.exe review expand R-D4535036/B-1 --output json
```

The default repository target mode enumerated these 10 `.cs` files:

- `src/DotNetAiCodeHygiene.Cli/AssemblyInfo.cs`
- `src/DotNetAiCodeHygiene.Cli/Program.cs`
- `src/DotNetAiCodeHygiene.Core/AssemblyInfo.cs`
- `src/DotNetAiCodeHygiene.Core/HygieneEngine.cs`
- `src/DotNetAiCodeHygiene.Core/ProfileManager.cs`
- `src/DotNetAiCodeHygiene.Core/RewriteEngine.cs`
- `src/DotNetAiCodeHygiene.Core/SdkProjectDetection.cs`
- `tests/DotNetAiCodeHygiene.Cli.Tests/CliProcessTests.cs`
- `tests/DotNetAiCodeHygiene.Core.Tests/LifecycleTests.cs`
- `tests/DotNetAiCodeHygiene.Core.Tests/M0005EvidenceTests.cs`

Format and normalize each reported `targetCount=10`, `changedCount=0`, `unchangedCount=10`, and `changedPaths=[]`; both were check-only and left source unchanged. Full JSON results are in `format-check.stdout.txt` and `normalize-check.stdout.txt`.

`check` analyzed default repository C# scope and emitted 565 deterministic findings, `ignoredCount=0`, and one semantic batch. The check JSON does not report target inventory/count; the inventory above was collected independently from the repository file set and engine default-target contract.

## 3. Deterministic findings

Full run: **565 findings; 0 ignored.** Run ID `R-D4535036`. Rule totals: `docs.summary.required` v2: 212; `readability.control-flow.visual-block` v1: 231; `readability.long-line.review` v1: 122. There were no findings for `profile.dotnet.analysis.required`, `profile.stylecop.prohibited`, `docs.xml.consistent`, or `docs.text.sentence`.

Scope methodology: compared run locations against added/modified C# lines in `git diff 61dcf6e..0b48571` (M0004 merge baseline to reviewed M0005 head). This yields **250 in-scope findings**; 315 findings outside those changed lines are retained in raw output but excluded from conclusions.

### In-scope findings by file and rule

| Classification | Rule | Count | File | Finding IDs |
|---|---|---:|---|---|
| legitimate hygiene issue | `readability.control-flow.visual-block` | 9 | `src/DotNetAiCodeHygiene.Cli/Program.cs` | F-335–F-343 |
| questionable/noisy finding | `readability.long-line.review` | 7 | `src/DotNetAiCodeHygiene.Cli/Program.cs` | F-213, F-225–F-227, F-231, F-233–F-234 |
| legitimate hygiene issue | `docs.summary.required` | 12 | `src/DotNetAiCodeHygiene.Core/HygieneEngine.cs` | F-4–F-10, F-109–F-112, F-114 |
| legitimate hygiene issue | `readability.control-flow.visual-block` | 40 | `src/DotNetAiCodeHygiene.Core/HygieneEngine.cs` | F-345–F-346, F-348–F-351, F-354–F-356, F-358–F-360, F-366–F-374, F-379, F-381–F-382, F-385–F-392, F-394–F-401 |
| questionable/noisy finding | `readability.long-line.review` | 17 | `src/DotNetAiCodeHygiene.Core/HygieneEngine.cs` | F-238–F-239, F-243–F-251, F-255, F-262–F-264, F-266–F-267 |
| legitimate hygiene issue | `docs.summary.required` | 16 | `src/DotNetAiCodeHygiene.Core/ProfileManager.cs` | F-123–F-138 |
| legitimate hygiene issue | `readability.control-flow.visual-block` | 41 | `src/DotNetAiCodeHygiene.Core/ProfileManager.cs` | F-403–F-443 |
| questionable/noisy finding | `readability.long-line.review` | 19 | `src/DotNetAiCodeHygiene.Core/ProfileManager.cs` | F-270–F-288 |
| legitimate hygiene issue | `readability.control-flow.visual-block` | 6 | `src/DotNetAiCodeHygiene.Core/RewriteEngine.cs` | F-448, F-450, F-452–F-453, F-457, F-459 |
| legitimate hygiene issue | `docs.summary.required` | 3 | `src/DotNetAiCodeHygiene.Core/SdkProjectDetection.cs` | F-151–F-153 |
| legitimate hygiene issue | `readability.control-flow.visual-block` | 5 | `tests/DotNetAiCodeHygiene.Cli.Tests/CliProcessTests.cs` | F-468, F-472–F-475 |
| questionable/noisy finding | `docs.summary.required` | 3 | `tests/DotNetAiCodeHygiene.Core.Tests/LifecycleTests.cs` | F-168–F-170 |
| legitimate hygiene issue | `readability.control-flow.visual-block` | 9 | `tests/DotNetAiCodeHygiene.Core.Tests/LifecycleTests.cs` | F-476–F-481, F-539–F-541 |
| questionable/noisy finding | `readability.long-line.review` | 4 | `tests/DotNetAiCodeHygiene.Core.Tests/LifecycleTests.cs` | F-298–F-300, F-308 |
| questionable/noisy finding | `docs.summary.required` | 19 | `tests/DotNetAiCodeHygiene.Core.Tests/M0005EvidenceTests.cs` | F-194–F-212 |
| legitimate hygiene issue | `readability.control-flow.visual-block` | 24 | `tests/DotNetAiCodeHygiene.Core.Tests/M0005EvidenceTests.cs` | F-542–F-565 |
| questionable/noisy finding | `readability.long-line.review` | 16 | `tests/DotNetAiCodeHygiene.Core.Tests/M0005EvidenceTests.cs` | F-319–F-334 |

Aggregate classification: **165 legitimate hygiene issues** (31 required product summaries + 134 visual-block findings); **85 questionable/noisy findings** (22 summaries on test fixtures + 63 long-line review candidates). No confirmed false positive or duplicate underlying issue was identified. Record positional type/property summaries at one source line concern distinct subjects; where long-line and visual-block findings share a line, they report separate conditions. One compact nested line (`HygieneEngine.cs:905`) yields two control-flow items at distinct columns for two nested `if` statements.

Every in-scope finding record, including finding ID/handle, rule version, relative path, line/column, message, suggestion, symbol, and fingerprint, is in `m0005-changed-line-findings.json`. The unfiltered complete 565-finding result is `check.stdout.txt`; `in-scope-groups.json` is a compact index.

### Assessment and examples

- **31 `docs.summary.required` product findings — legitimate hygiene issues; AC-13 unmet on these subjects.** The tool found missing summaries on M0005-added/modified `HygieneEngine` rule/result/profile APIs (12 findings, F-4–F-10 and F-109–F-112/F-114), `ProfileManager` profile result records and public operations (16 findings, F-123–F-138), and the new shared SDK detector internal type/methods (3 findings, F-151–F-153). The message and direction, “Add a concise documentation summary for this API subject,” are accurate, though generic.

  ```csharp
  // HygieneEngine.cs:15
  public sealed record Rule(string Id, int Version, string OutputKind, string Classification, string Purpose, bool Configurable = true);
  // ProfileManager.cs:11-12
  public sealed record ProfileFinding(string RuleId, string Path, string Message, string Suggestion);
  public sealed record ProfileResult(string Command, int FindingCount, IReadOnlyList<ProfileFinding> Findings, IReadOnlyList<string> ChangedPaths);
  // SdkProjectDetection.cs:5,7,13
  internal static class SdkProjectDetection
  internal static bool IsSdkStyle(string path)
  internal static bool IsSdkStyle(XDocument document)
  ```

- **22 test-summary findings — questionable/low-value scope signal.** F-168–F-170 and F-194–F-212 target public TUnit test fixture types/methods in `LifecycleTests.cs` and `M0005EvidenceTests.cs`. They technically match the implemented public/internal method rule, but are not shipped caller APIs. The suggestion is formally accurate but creates noise for test entry points; consider a narrowly scoped test-project policy follow-up rather than treating these as product API defects.

- **134 `readability.control-flow.visual-block` findings — legitimate hygiene issues on changed lines.** The source location is the control-flow statement, and “Insert one completely blank line before this control-flow group” matches the contract. Example: `ProfileManager.cs:44` places `if (!File.Exists(path))` directly after the linear assignment at line 43. The findings remain to be resolved or explicitly dispositioned under the normal workflow; no formatting mutation was run.

- **63 `readability.long-line.review` findings — review candidates, not confirmed defects.** Locations point to physical lines longer than 200 characters and the suggestion appropriately asks for review. Example F-270 at `ProfileManager.cs:26` is a single generated XML string for the managed props block; F-319 at `M0005EvidenceTests.cs:51` is an inline SDK project fixture. Length alone does not prove that extraction improves these cases, so these are questionable/noisy until individually reviewed.

## 4. Semantic review

`check` created sample batch `R-D4535036/B-1` for `docs.summary.quality.review` v2 with `reviewerClass=implementer`, `populationCount=2`, `sampleCount=2`. Both summaries were reviewed; both materially fail Q1, so the normal escalation rule required `review expand`. Expansion succeeded with `mode=expanded`, `reviewerClass=frontier`, and full population 2/2. The complete sample is in `check.stdout.txt`; the full expanded batch is in `review-expand.stdout.txt`; per-item rubric judgments are in `semantic-assessments.json`.

| Item | Location / summary | Q1 German | Q2 Technical | Q3 Value | Q4 Clarity | Overall |
|---|---|---|---|---|---|---|
| RI-1 | `ProfileManager.cs:15` — “Owns the fixed dotnet-11 profile and its explicitly delimited repository artifacts.” | Fail: English, not German. | Pass. | Pass. | Pass; “explicitly delimited” is slightly abstract. | Expected semantic item; quality issue under rubric. |
| RI-2 | `RewriteEngine.cs:32` — “Plans Roslyn rewrites completely before applying an all-target transaction.” | Fail: English, not German. | Pass. | Pass. | Pass. | Expected semantic item; quality issue under rubric. |

This is not a sampling defect: the sample equals the entire eligible population. The rule accurately exposed that the only two non-empty local summary carriers fail its explicit German-language Q1. Repository docs and code are otherwise English, so the rubric creates a language-policy tension worth recording; the result is not a false positive against the published German rubric.

## 5. Expected problems observed and not observed

**Observed:** missing summaries on M0005 product API subjects (AC-13); changed-line control-flow and long-line signals; both available semantic summaries failing Q1. The current profile state produced no weakened effective property/EditorConfig finding, and no prohibited StyleCop input was found.

**Not observed:** no profile, StyleCop, XML-structure, or documentation-sentence findings. No known bad case of those kinds was present in this repository state, so these absences are expected clean results, not evidence that the rules missed a defect. The repository projects use the root-attribute `<Project Sdk="Microsoft.NET.Sdk">` form; this dogfood run does not independently exercise the alternate top-level `<Sdk Name="Microsoft.NET.Sdk" />` form, which remains covered by the focused regression test.

No additional missed M0005 issue was identified from the exercised state. This is bounded evidence, not a claim that all possible analyzer/project forms were re-tested by dogfooding.

## 6. Tool UX and interpretation limits

- The JSON check output includes stable IDs/handles, rule IDs/versions, source location, symbol, message, and suggestion, but omits the selected target inventory/count. This report records the default target scope and exact command separately.
- `readability.long-line.review` is a candidate rule and appropriately avoids asserting a defect, but its generic suggestion supplies no excerpt or reason tied to that line. The raw source location plus repository revision make it inspectable; the suggestion is not enough for an agent to decide automatically.
- `review expand` returns the complete summary/declaration items and rubric but no surrounding source excerpt. This report supplies short descriptions and source locations; source remains available at the recorded commit.
- No command diagnostics or execution failures affected interpretation. `check` exit 0 means analysis completed, not that the 565 findings were clean or resolved.

## 7. Final assessment

1. **M0005 self-hygiene review:** Fail for closure because 31 changed product declarations violate AC-13 summary requirements; the normal run and review expansion themselves completed successfully.
2. **In-scope volume/classification:** 250 deterministic findings: 165 legitimate hygiene issues (31 documentation summaries, 134 visual-block findings) and 85 questionable/noisy findings (22 test-fixture summaries, 63 long-line candidates). Two semantic-review items were expanded and assessed separately; both have Q1 quality failures.
3. **Expected problems successfully observed:** AC-13 omissions in newly added product APIs; deterministic readability issues on changed lines; both available summaries failing the published German rubric. Effective profile and analyzer checks returned clean for current repository state.
4. **Expected problems missed:** None identified within the exercised repository state. The self-run does not exercise top-level SDK-element projects; that representation is covered only by the focused regression test.
5. **Suspected false positives or low-value results:** 22 test-method/type summary findings are low-value for non-shipped test APIs; 63 long-line results are review candidates, with generated XML and fixture strings prominent. No confirmed false positive was established.
6. **Semantic-review quality:** Correctly sampled and expanded all 2 eligible summaries. Both pass Q2-Q4; both fail Q1 because they are English under a German-only rubric. The repository-language mismatch is a policy/usability tension, not a sampler miss.
7. **Tool UX/reporting:** Check JSON lacks target inventory metadata; long-line suggestions and expanded review items lack source excerpts. IDs, versions, locations, messages, and directions are otherwise sufficient to map findings to source.
8. **Required M0005 remediation:** Add summaries to the 31 M0005-added/modified covered product API subjects (AC-13) and rerun check. Review/disposition all 134 changed-line visual-block findings through the normal workflow. Resolve or explicitly accept the two Q1 review results according to the published language policy. No source changes were made in this report-only run.
9. **Deferred follow-up beyond M0005:** Decide whether test fixture methods should be excluded from the summary rule; align the German semantic rubric with repository language policy if English is intended; consider target metadata and contextual excerpts for JSON/review UX; manually triage the 63 long-line candidates. No unrelated repository cleanup is proposed.

## Evidence files

- `context.json`, `installed-artifact.json`, `scope.json`: version, platform, artifact hashes, and target scope.
- `commands.json` and `*.stdout.txt` / `*.stderr.txt`: exact commands and complete raw outputs; `check.stdout.txt` contains the full deterministic finding set and normal semantic sample.
- `review-expand.stdout.txt` and `review-expand-command.json`: complete expanded semantic population and execution record.
- `m0005-changed-line-findings.json`: all 250 in-scope deterministic findings, each with full tool fields plus an assessment and rationale.
- `in-scope-groups.json`, `analysis-index.json`, `semantic-assessments.json`: compact finding index, classifications, and per-item rubric evaluations.

