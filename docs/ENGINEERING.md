# Engineering

## Repository self-hosting policy

The repository is English. `.hygiene/config.json` disables only `docs.summary.language.german.review`; generic summary-quality review, required summaries, and visual control-flow separation remain enabled. The German policy remains fixed and available to consuming repositories that choose to enable it.

## Baseline

```text
Windows 11
.NET 11 SDK line
SDK-default C#
PowerShell
System.CommandLine
TUnit
Roslyn/MSBuild project-aware loading
```

No GitHub Actions/workflows.

## Agent entrypoint

`AGENTS.md` is stable repository-level routing. It must not point at one current/completed milestone or duplicate a milestone execution contract.

Milestone-specific scope, constraints, evidence requirements, and completion instructions belong in the active milestone, its `.execution/` ledger, and the execution prompt.

## Rule source layout

Production rules live below `src/DotNetAiCodeHygiene.Core/Rules/`.

Use family folders:

```text
Rules/
  Documentation/
  SemanticReview/
  Readability/
  Architecture/
  Profile/
```

Shared rule execution/catalog types may live directly under `Rules/`.

Use one production rule module per source file. Shared family facts/helpers may use separate files in that family. Do not create category bucket files containing several unrelated production rule implementations.

When adding or modifying a rule, the normal locality path is:

```text
open the rule file
-> see its descriptor and fixed interface text
-> see or follow its evaluator
-> follow only clearly named shared facts/helpers when needed
```

Rule-specific fixed presentation text belongs to the rule file as plain C# constants/static data where practical. This includes descriptor purpose, finding templates, review questions/rubric text, and rule-specific reviewer/escalation wording.

Do not introduce `.resx`, localization infrastructure, generated metadata, a generic message registry, attributes/reflection, or a rule-definition DSL merely to centralize strings.

Truly generic product/host text remains with the shared mechanism that owns it.

## Architecture test seams

Architecture tests should prove behavior through small internal seams rather than implementation-shape assertions alone.

Useful focused seams include:

- a test rule supplied to the rule runner without editing production orchestration;
- a test rewrite supplied to the rewrite runner without editing a command switch;
- observable session factories/counters proving expensive project/compilation/fact creation is lazy and reused within one command session;
- a test fact provider proving one computation is shared by multiple consumers;
- targeted fixture runs proving reporting scope remains narrower than readable project context.

Do not introduce production plugin discovery or a DI framework solely to obtain these seams.

Physical source-layout requirements are validated by repository/source review plus normal compilation/tests; do not build a runtime architecture framework merely to enforce folders.

## Profile fixtures

Use isolated SDK-style Git repositories covering:

- no profile marker/artifacts;
- clean bootstrap;
- existing root `.editorconfig`;
- existing root `Directory.Build.props`;
- project-level override of profile props;
- nested EditorConfig override of required IDE rule;
- `TreatWarningsAsErrors=true`;
- non-empty `WarningsAsErrors`;
- direct StyleCop package reference;
- central package-management StyleCop reference;
- effective StyleCop analyzer input;
- profile-owned artifact drift;
- malformed/conflicting managed-section/import state;
- bootstrap/update idempotence;
- fault/cancellation before commit.

Tests must prove unrelated existing `.editorconfig`/MSBuild content is preserved.

## Documentation fixtures

Use Roslyn/project-aware C# fixtures for:

- ordinary type/member summaries;
- direct `<inheritdoc/>`;
- inheritdoc plus explicit invalid prose;
- record and record-struct positional properties;
- normal method parameters with zero/some/all `<param>` tags;
- parameter/type-parameter mismatch and duplicates;
- `paramref`/`typeparamref` mismatch;
- optional returns/value/exception structures;
- inline `<see/>` before sentence punctuation;
- excluded elements such as `<example>`;
- semantic review population for ordinary and record-property summary carriers;
- independent quality and German-language review batches when both are enabled.

The documentation-subject/carrier fixture set is the primary shared-fact regression surface.

## Effective configuration

Profile validation must exercise effective MSBuild/analyzer configuration, not merely string-match generated files.

The mandatory profile uses .NET 11 `AnalysisLevel=11`, explicit built-in analyzer enablement, and build-time code-style enforcement. Enabled `style.braces.required` and `style.accessibility.explicit` rules project IDE0011/IDE0040 settings and diagnose their effective values.

Do not introduce `latest` AnalysisLevel in the supported profile.

## Severity testing

Prove:

```text
required IDE rules -> effective error
profile emits no unrelated severity overrides
TreatWarningsAsErrors=true -> finding
WarningsAsErrors non-empty -> finding
```

User per-rule severity configuration for unrelated diagnostics is not a profile violation unless it weakens a required profile diagnostic.

## Mutation validation

Bootstrap/update and source rewrites retain all-or-nothing mutation behavior.

A failed profile run must not leave a partial marker/props/import/editorconfig update. A failed source rewrite must not leave a mixed selected-target state.

Profile mutation and source rewrite semantics remain independently regression-covered.

## Validation topology

| Depth | Target | Locus |
|---|---|---|
| Tier 1 | rule runner/catalog/session/facts/result materialization + Core behavior | Windows 11 + .NET 11 |
| Tier 1 | built CLI process | Windows 11 + .NET 11 |
| Tier 3 | isolated SDK-style Git fixture repositories, including full/explicit/changed scope and rewrite commands | Windows 11 + .NET 11 + Git |
| Tier 4 | exact current locally packed/installed tool | isolated Windows consumer repository |
| Tier 2 | complete repository | `./eng/validate.ps1` |
| Documentation | architecture/spec consistency | repository review |
| Human | modularity/BORING architecture, locality, and behavior preservation | project owner/delegate |

Tier 4 must invoke the installed `hygiene` command and exercise representative bootstrap/update/check/rules/review and format/normalize behavior from the current build artifact. M0010 validated the exact 0.6.0 package and dotnet-11 v1-to-v2 migration. M0011 validated the exact 0.7.0 package, explicit review acceptance, summary rotation, and BORINGness planner handoff. M0016 validated the exact 0.8.0 package, PR-base and explicit full-review gates, zero-work bare checks, and gate-local acceptance. M0017 validates the exact 0.9.0 package and six-rule review surface.

## Validation workflow

Validation has four distinct uses; the required evidence comes from the active milestone, not from a single default depth:

- **Development feedback** is a quick, affected check used to guide the current change. A focused test filter or shard proves only the cases it ran and does not create a complete-suite receipt.
- **Investigative execution** includes historical simulations, profiling and experiments. Run these only when the investigation calls for them; they are not ordinary regression tests or milestone gates unless the milestone explicitly declares them.
- **Milestone acceptance** runs each gate declared by the ready milestone and records current, criterion-specific evidence. Completion requires those declared gates, not every historical suite or consumer scenario. If a milestone explicitly requires a full suite or installed-consumer validation, that gate remains mandatory. Reconcile every obligation and evidence case in its ledger; do not infer full-suite validation from focused checks.
- **Full release qualification** remains the comprehensive M0020 path at `./eng/validate.ps1`. Use it when release qualification or the active milestone requires it. Its long duration is acceptable; the implementation reuses only evidence whose recorded provenance still proves it current.

For the canonical M0020 release qualification, discover stable stage IDs and commands with:

```powershell
./eng/validate.ps1 -PlanJson
```

The seven stages are solution build, Core tests, CLI tests, README policy, current package, installed consumers and self-host validation. Run a bounded stage by ID. A shard requires current receipts for its prerequisites and does not run unrelated stages:

```powershell
./eng/validate.ps1 -Shard solution
./eng/validate.ps1 -Shard core-tests
./eng/validate.ps1 -Shard cli-tests
./eng/validate.ps1 -Shard installed-consumers
```

Core and CLI stages call the actual TUnit/Microsoft.Testing.Platform project and print their slowest tests. Select a focused group with the MTP tree filter, for example:

```powershell
./eng/validate.ps1 -Shard cli-tests -TestFilter '/*/*/CliProcessTests/VersionUsesProductVersionOnStandardOutput'
```

A filtered run is development feedback only and never creates a complete-suite receipt. The no-argument command runs missing or invalidated stages, reuses only current independent stage receipts, and reports aggregate success only after the full receipt verifier accepts every stage. `-Verify` checks the complete evidence set without executing tests. `-Fresh` invalidates all stage receipts, reruns the full qualification and reconstructs the pinned prior-version package fixture.

Receipts fingerprint the active .NET SDK and Git versions, PowerShell/OS identity, each stage's relevant inputs and command, current prerequisite receipt identities, and declared output hashes. The solution receipt includes production assemblies and every test project's compiled runtime artifacts; Core and CLI stages run with `--no-build`, so changing or removing those artifacts invalidates build evidence and dependent receipts. Source, project, test, build configuration, validation-script, toolchain and package changes invalidate stages that depend on them. Documentation changes invalidate self-host evidence through its merge-base change-set fingerprint, including tracked changes and non-ignored untracked inputs, without needlessly invalidating unrelated test stages. Current package output and installed-consumer evidence are coupled by the pack-stage output hash: a stale current binary or package cannot validate the consumer boundary.

The pinned 0.9.0 previous-version package is immutable test input. Its optional cache is outside the repository and is keyed by pinned commit/tree, exact SDK version, package ID and version; a sidecar records and rechecks its SHA-256. Reuse requires this identity and content verification. Missing, malformed or changed cache evidence triggers clean worktree reconstruction. The current release-candidate package is freshly produced when relevant inputs change; its dependent installed-consumer evidence must be refreshed. `-Fresh` bypasses fixture-cache reads, reconstructs the fixture and refreshes the cache. Consumer repositories, feeds, package installs and caches remain isolated per consumer stage.

The M0013 and M0015 research entrypoints under `eng/` are investigative tools, not part of the ordinary Core/CLI test projects or the canonical M0020 stage list. Do not run historical simulations as default regression or release validation; preserve the independent research records and execute a simulation only when an active task asks for it.

Existing Windows 11/.NET 11, Git and PowerShell tools, dependency restores and disposable isolated project/tool paths may be used for authorized repository work. This execution authority does not grant permission to provision persistent host software, change CI/workflows, or modify remote infrastructure.

Each receipt reports actual stage wall time and whether evidence was freshly executed or reused. Cached elapsed time is historical execution cost, not time spent in the current command. Test stages report slowest cases where supported; concurrently executed case durations can overlap and must not be summed as suite wall time.

After a cold or warm baseline has produced current receipts, the M0020 final-boundary and test-output provenance regressions can be exercised with:

```powershell
./eng/test-validation-final-verification.ps1
./eng/test-validation-test-outputs.ps1
```

These scripts require a complete current receipt set, temporarily corrupt and restore evidence under the validation lock, and verify rejection and recovery without leaving altered evidence behind. They are M0020 validator checks, not automatically required for every milestone. M0020's full release qualification remains available and authoritative whenever release readiness or a milestone requires that evidence; milestone-scoped acceptance does not silently replace, weaken, or universally rerun it.
## Regression expectations

Architecture/refactoring work must preserve:

- target/finding/ignore/explain behavior;
- semantic-review population, independent batches, expansion, and handoff behavior;
- format/normalize/check-only and transactional mutation behavior;
- profile/bootstrap/update/documentation behavior;
- fixed-rule/toggle-only policy and repository German-rule disablement;
- exact public rule IDs, versions, order, descriptor metadata, and established interface text unless a milestone explicitly changes them.

## Deferred

No external specialist analyzer is currently approved. No StyleCop compatibility, model client, MCP, IDE integration, other language, hosted CI, public plugin architecture, statistical sampling subsystem, persistent analysis cache, speculative repository index, localization framework, or rule metadata DSL is introduced merely for rule locality.

The six semantic-review rules use bounded, deterministic gate-local selection as specified in `docs/specs/SEMANTIC-REVIEWS.md` and `docs/specs/SAMPLING.md`. PR-base and working-tree source changes authorize ordinary review; explicit paths request manual bounded inspection; bare full checks schedule zero semantic review; `--review-all` explicitly requests bounded full-repository inspection. Selection is risk-stratified and potentially biased, not defect-rate calibration or population certification. Only explicit acceptance writes versioned observations to gate-review state. Mechanical rules remain exhaustive.

## M0018 command and lifecycle authority

The only supported public command namespace is `dotnet hygiene`, packaged with `ToolCommandName=dotnet-hygiene` at version `0.11.0`. The no-argument entrypoint owns the bootstrap/profile/gate lifecycle. A profile marker never substitutes for report/snapshot-bound baseline acceptance; an accepted baseline permits ordinary source edits but material policy changes require reapproval. Feature branches with an unknown/ambiguous committed comparison fail closed even when the worktree is dirty. Update scope resolves from the invoking apphost or ancestor local manifest and never from a different PATH shim. There is no pre-1.0 compatibility obligation for the old bare `hygiene` command, CLI shapes, output schemas, or persisted experimental representations. Source and user configuration data still must not be silently destroyed. Lifecycle authority and approval boundaries are in `docs/specs/AUTONOMOUS-WORKFLOW.md`.

## M0019 exhaustive-audit validation

M0019 adds an independent read-only audit path. Run the focused Core populations with `dotnet test .\tests\DotNetAiCodeHygiene.Core.Tests\DotNetAiCodeHygiene.Core.Tests.csproj --configuration Release --no-build --treenode-filter '/*/*/GateLocalReviewTests/ExhaustiveAuditBypassesNormalSampleAndAcceptedObservationWithoutPublishingNormalState'` and `dotnet test .\tests\DotNetAiCodeHygiene.Core.Tests\DotNetAiCodeHygiene.Core.Tests.csproj --configuration Release --no-build --treenode-filter '/*/*/GateLocalReviewTests/ExhaustiveReviewMaterializationKeepsMoreThanFiftyThousandObligations'`. Run CLI authorization/report/read-only/resume coverage with `dotnet test .\tests\DotNetAiCodeHygiene.Cli.Tests\DotNetAiCodeHygiene.Cli.Tests.csproj --configuration Release --no-build --treenode-filter '/*/*/CliProcessTests/ExhaustiveAuditRequiresAttestationResumesJudgmentsAndPreservesNormalState'` and prerequisite coverage with `dotnet test .\tests\DotNetAiCodeHygiene.Cli.Tests\DotNetAiCodeHygiene.Cli.Tests.csproj --configuration Release --no-build --treenode-filter '/*/*/CliProcessTests/ExhaustiveAuditStopsWithoutAcceptedBaselineAndLeavesDestinationAbsent'`. These focused commands prove only the selected tests and do not create complete-suite receipts. At closure, inspect M0020's current stage plan using `./eng/validate.ps1 -PlanJson`, run affected declared stages, and use `./eng/validate.ps1 -Verify` only for receipts whose fingerprints remain current and relevant. Do not run the unrelated historical full release matrix solely for M0019.

For the mandatory consumer gate, run `./eng/validate.ps1 -Shard pack-current`, then install that exact 0.11.0 nupkg from a disposable local feed: `dotnet tool install DotNetAiCodeHygiene.Tool --tool-path $toolPath --add-source $feed --version 0.11.0`. Put `$toolPath` on `PATH` and run the generated `dotnet-hygiene.exe` shim from a disposable initialized repository with a valid accepted baseline. Exercise the unattested and denied handshakes, affirmative scan, byte hashes for source/Git index/profile/normal state, bounded `audit next`, acceptable/problem/uncertain judgment recording, invalid/duplicate/cross-run rejection, an incomplete report, interruption/resumption, and the final report. Record the nupkg and generated shim SHA-256 values. Internal DLL or project tests do not replace this public-tool evidence. M0019 leaves `.review/REV-M0019-COMPLETION.md` pending independent approval.
