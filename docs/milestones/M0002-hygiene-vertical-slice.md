# Milestone — M0002 Hygiene Vertical Slice

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | done |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Execution ledger | `.execution/M0002-hygiene-vertical-slice.md` planning-seeded |
| Scope size | large coherent vertical slice |
| Implementation autonomy | high within resolved contracts |
| Documentation sync | deferred |
| Focused validation | Tier 1 Core + CLI process tests |
| Repository validation | Tier 2 `./eng/validate.ps1` |
| Integration validation | Tier 3 isolated local .NET/Git fixture repositories |
| Validation locus/platform | local Windows 11 + .NET 11 SDK + Git + PowerShell |
| Consumer/release validation | Tier 4 installed-tool validation deferred to M0003 |
| Human review | required at milestone completion |

## Goal

Make `hygiene` useful for the first time by implementing one complete deterministic C# hygiene lifecycle from repository/file targeting through Roslyn-backed rule evaluation, agent-oriented results, finding explanation, persisted ignore decisions, stale matching, and unignore.

## Target State

A caller can run a real hygiene check against a Windows-local .NET repository, receive deterministically ordered text or JSON findings from three built-in rules, inspect a finding, persist an ignore through the CLI, verify that the same occurrence is suppressed across later runs, observe stale decisions when relevant code changes, and remove the ignore so reporting resumes.

The implementation remains deterministic and contains no embedded AI/model call.

## Scope

- introduce a Core implementation boundary behind the CLI;
- resolve repository/file/directory/changed C# targets;
- associate target files with SDK-style `.csproj` projects and load Roslyn semantic context;
- implement the canonical three-rule set;
- implement rule enable/disable persistence;
- implement deterministic findings and review candidates;
- implement run/finding handles and latest-run local state;
- implement text and JSON output;
- implement `explain`;
- implement persistent ignore decisions and `unignore`;
- implement active/stale ignore listing;
- implement SHA-256 rule-specific occurrence matching;
- implement atomic/conflict-aware committed state writes;
- update public documentation to match the implemented M0002 surface.

## Non-goals

- no source formatting implementation;
- no normalization implementation;
- no embedded AI/model invocation or model routing;
- no findings-as-failure gating option;
- no historical run database;
- no multi-language analysis;
- no Linux/macOS support claim;
- no IDE extension;
- no MCP server;
- no per-file rule configuration;
- no rule parameters, configurable severity, thresholds, or ordering;
- no inline source suppression comments;
- no installed `.NET tool` consumer validation;
- no NuGet publication/release;
- no dedicated public-doc site/tree.

## Decisions and Constraints

- Use the contracts in `docs/SPECS.md` and `docs/specs/HYGIENE.md` exactly for public behavior.
- Add `DotNetAiCodeHygiene.Core` and `DotNetAiCodeHygiene.Core.Tests`; Core remains internal and is not a supported public library.
- Use Roslyn project/semantic APIs for C# analysis.
- Use TUnit for all new automated tests.
- Use `System.CommandLine` for the CLI.
- Use SHA-256 for occurrence fingerprints.
- All built-in rules are enabled when no config exists.
- Persist only disabled rule IDs in config.
- Canonical rule order is fixed and user-unconfigurable.
- Only the latest run is persisted locally.
- Finding handles are run-local; ignore IDs are repository-persistent.
- Bare `F-*` handles refer only to the latest run.
- An unavailable old qualified run handle must fail rather than resolve ambiguously.
- Ignore creation must revalidate current source before persisting.
- Committed config/decision writes are atomic and conflict-aware.
- Local run state is Git-ignored and may be replaced by a new successful check.
- Findings do not make a successful `check` return non-zero.
- `format` and `normalize` remain non-functional scaffolding.
- Do not add GitHub Actions/workflows.
- No implementation agent needs the external guide repository or planning conversation.

## Baseline Executor Readiness

All material architecture, behavior, persistence, identity, targeting, rule semantics, output semantics, and validation decisions are preserved in project authority.

The baseline executor may choose local implementation mechanics such as internal type names, concrete Roslyn workspace composition, deterministic serialization helpers, temporary-file naming, and test fixture organization provided the observable contracts are preserved.

A newly discovered fact that would require changing rule semantics, public JSON, persistent matching semantics, supported target classes, or validation topology returns to planning.

## Required Authority

Implementation must read:

- `docs/SPECS.md`
- `docs/specs/HYGIENE.md`
- `docs/ARCHITECTURE.md`
- `docs/ENGINEERING.md`
- `docs/TERMINOLOGY.md`
- `docs/PUBLIC-DOCS.md`

Implementation does not need `.guide-profile.json`, `.guide-sync/`, the external guide repository, or planning history.

## Acceptance Criteria

- **AC-01** — The repository adds `DotNetAiCodeHygiene.Core` and `DotNetAiCodeHygiene.Core.Tests`; Core owns product semantics behind thin CLI handlers and remains an internal implementation assembly rather than a supported reusable API.
- **AC-02** — `hygiene check` supports repository-default, explicit-file, explicit-directory, and `--changed` target modes with repository-root-relative semantics, no duplicate analysis for overlapping targets, and findings restricted to selected files.
- **AC-03** — Explicit target paths and `--changed` are mutually exclusive; missing repository context, outside-repository paths, unsupported explicit files, and C# files not associated with a discoverable SDK-style project fail as product input errors rather than producing partial/ambiguous results.
- **AC-04** — `--changed` includes staged, unstaged, and untracked current C# paths, ignores deleted source paths, uses no remote/base-branch inference, and maps unavailable Git execution to dependency/environment failure.
- **AC-05** — Rule selection is deterministic: the three built-in rules exist in the specified canonical order, default enabled state is all enabled, `rules enable/disable` is idempotent and persisted through `.hygiene/config.json`, and unknown rule IDs fail without rewriting valid config.
- **AC-06** — `docs.summary.required` version 1 implements exactly the symbol scope and non-empty XML-summary semantics defined in `docs/specs/HYGIENE.md`.
- **AC-07** — `readability.long-line.review` version 1 emits one review candidate per physical C# line over 200 UTF-16 code units and uses the specified non-mechanical cleanup guidance.
- **AC-08** — `readability.control-flow.visual-block` version 1 implements the exact control-flow/linear-sibling/blank-line/comment grouping semantics defined in `docs/specs/HYGIENE.md`.
- **AC-09** — Visible findings are deterministically ordered by rule, ordinal repository-relative path, source span, and local discriminator, then assigned run-local `F-*` ordinals; ignored occurrences are omitted and counted separately.
- **AC-10** — Every successful check produces one `R-*` run ID and atomically replaces `.hygiene/.state/latest-run.json`; `.hygiene/.state/` is Git-ignored and cancelled/failed checks do not publish partial latest-run state.
- **AC-11** — `hygiene explain` accepts a full latest-run handle or bare latest-run `F-*`, returns the required rule/location/observation/reason/suggestion/constraint information in text or JSON, and does not rerun the complete check.
- **AC-12** — A qualified finding handle whose run ID is not the locally available latest run fails safely; it is never reinterpreted as a finding from the current run.
- **AC-13** — `check --output text` provides compact run/summary/finding output conforming to the documented text semantics and keeps diagnostics on stderr.
- **AC-14** — `check --output json` conforms to JSON schema version 1 fields defined in `docs/specs/HYGIENE.md`; JSON stdout contains no diagnostics/progress or internal fingerprint/persistence fields.
- **AC-15** — `hygiene ignore <finding>` revalidates the current source occurrence and, only when it still matches, creates a stable `I-*` decision in `.hygiene/decisions.json` with rule/version, path metadata, semantic anchor, SHA-256 fingerprint, discriminator when required, optional reason, and UTC timestamp.
- **AC-16** — A persisted active ignore suppresses the same occurrence on later checks while preserving the ignored count; it is matched by stable occurrence identity rather than line number.
- **AC-17** — `hygiene unignore <I-*>` removes exactly the named decision; a later check reports the still-present occurrence again.
- **AC-18** — Rule-specific fingerprinting has the required stability/invalidation behavior: symbol movement does not break the summary occurrence, irrelevant whitespace does not break a still-long structural line occurrence, relevant code change invalidates the old occurrence, and adding the required visual blank line resolves the control-flow occurrence.
- **AC-19** — `hygiene ignores` lists persisted decisions with stable IDs and `active`/`stale` state; optional file/directory filters work repository-relatively, stale decisions are retained until explicitly removed, and disabled rules do not automatically make their decisions stale.
- **AC-20** — `.hygiene/config.json` and `.hygiene/decisions.json` are schema-validated before use; malformed/unsupported committed state fails with exit code `3` and is not silently rewritten.
- **AC-21** — Committed config/decision writes are atomic and detect conflicting external modification instead of silently overwriting it; cancellation before commit leaves the prior valid state intact.
- **AC-22** — Public exit/stream behavior remains stable: successful checks return `0` even with findings; invalid invocation returns `2`; invalid product input/state returns `3`; unavailable required environment/dependency returns `4`; unexpected internal failure returns `1`.
- **AC-23** — `format` and `normalize` remain clearly non-functional scaffolding, no embedded AI/model dependency is added, and no M0003 installed-tool validation or cross-platform claim is introduced.
- **AC-24** — `./eng/validate.ps1` remains a thin complete Windows-local validation entry point and successfully runs the M0002 restore/build/test/pack validation set.
- **DOC-01** — README/public documentation is updated at completion with representative M0002 source-project usage, JSON/exit semantics, CLI-owned state guidance, Windows-first status, and explicit deferral of formatting/normalization and installed-tool guidance.
- **DOC-02** — Project authority remains internally consistent with the implemented M0002 public/persisted behavior; material deviations are returned to planning rather than silently changing contracts.
- **REV-01** — A human completion review confirms that the first hygiene loop is useful and deterministic, evidence is criterion-specific, persisted ignores behave safely, and M0003 scope was not pulled forward.

## Acceptance Evidence Topology

| ID | Parent obligation | Required evidence case | Why separate evidence is required |
|---|---|---|---|
| EC-02a | AC-02 | repository-default targeting | repository enumeration path differs from explicit targets |
| EC-02b | AC-02 | explicit single/multiple file targeting | direct file resolution path |
| EC-02c | AC-02 | recursive directory targeting including overlap de-duplication | directory enumeration/de-duplication path |
| EC-02d | AC-02 | `--changed` targeting | Git-derived target path |
| EC-03a | AC-03 | explicit path + `--changed` invalid invocation | parser/contract failure path |
| EC-03b | AC-03 | no repository / outside repository / unsupported explicit file | target/repository validation path |
| EC-03c | AC-03 | C# file not associated with supported project | project-context validation path |
| EC-04a | AC-04 | staged/unstaged changed C# | tracked Git state paths |
| EC-04b | AC-04 | untracked C# | untracked Git path |
| EC-04c | AC-04 | deleted C# excluded | deletion path |
| EC-05a | AC-05 | no config means all rules enabled in canonical order | default state path |
| EC-05b | AC-05 | disable then persisted disabled state | config write/read path |
| EC-05c | AC-05 | enable/idempotent mutation and unknown ID rejection | opposite/no-op/error paths |
| EC-06a | AC-06 | covered type symbol | named-type semantic path |
| EC-06b | AC-06 | covered constructor/method/property symbol | member semantic paths |
| EC-06c | AC-06 | excluded symbol categories | exclusion semantics |
| EC-07a | AC-07 | >200 line produces one review candidate | trigger path |
| EC-07b | AC-07 | <=200 and wrapped lines do not produce candidate | non-trigger/resolution path |
| EC-08a | AC-08 | linear statement followed by listed control flow without blank line | trigger path |
| EC-08b | AC-08 | blank-line separation / first statement / previous control flow | non-trigger paths |
| EC-08c | AC-08 | leading comments belong to control-flow visual group | trivia/comment path |
| EC-11a | AC-11 | bare latest-run finding handle | shorthand resolution |
| EC-11b | AC-11 | fully qualified latest-run handle | qualified resolution |
| EC-14a | AC-14 | findings present JSON | populated result schema |
| EC-14b | AC-14 | zero findings JSON | empty-result schema |
| EC-15a | AC-15 | current finding successfully ignored | persistence success path |
| EC-15b | AC-15 | source changed after check before ignore | stale-reference rejection path |
| EC-18a | AC-18 | symbol movement retains summary ignore identity | semantic-anchor stability |
| EC-18b | AC-18 | irrelevant whitespace retains still-long occurrence identity | structural-token stability |
| EC-18c | AC-18 | relevant token change reopens old ignored occurrence | structural invalidation |
| EC-18d | AC-18 | adding control-flow blank line resolves ignored occurrence | trivia-sensitive resolution |
| EC-19a | AC-19 | active decision listed | live-match path |
| EC-19b | AC-19 | stale decision retained/listed | stale-match path |
| EC-21a | AC-21 | successful atomic first/replace write | normal write path |
| EC-21b | AC-21 | concurrent/external modification rejected | conflict path |
| EC-21c | AC-21 | cancellation/failure before commit preserves old state | interruption path |
| EC-22a | AC-22 | successful check with findings returns 0 | product-result success semantics |
| EC-22b | AC-22 | invalid invocation returns 2 | parser failure semantics |
| EC-22c | AC-22 | invalid target/state returns 3 | product input failure semantics |
| EC-22d | AC-22 | Git/dependency unavailable returns 4 | environment failure semantics |

## Validation

| ID | Depth | Target | Locus/platform | Command/check | Proves | Expected evidence |
|---|---|---|---|---|---|---|
| VAL-01 | Tier 1 | Core deterministic semantics | local Windows 11 + .NET 11 SDK | focused TUnit Core tests | AC-05, AC-06, AC-07, AC-08, AC-09, AC-18, AC-20, AC-21 and their EC cases | focused passing tests against deterministic rule/persistence semantics |
| VAL-02 | Tier 1 | built CLI process | local Windows 11 + .NET 11 SDK | TUnit CLI process tests | AC-11, AC-12, AC-13, AC-14, AC-22, AC-23 and relevant EC cases | real process exit/stdout/stderr/text/JSON evidence |
| VAL-03 | Tier 3 | isolated temporary SDK-style .NET/Git repositories | local Windows 11 + .NET 11 SDK + Git | TUnit integration scenarios invoking built CLI/Core as appropriate | AC-02, AC-03, AC-04, AC-10, AC-15, AC-16, AC-17, AC-19 and their EC cases | real repo/project/Git/state lifecycle evidence |
| VAL-04 | Tier 2 | complete repository | local Windows 11 + .NET 11 SDK + Git + PowerShell | `./eng/validate.ps1` | AC-01, AC-24, DOC-02 plus aggregate regression evidence | restore/build/test/pack success; required projects and no workflows/model dependency |
| VAL-05 | Tier 2 | public documentation | local repository | compare README/public docs to live M0002 process behavior | DOC-01 | documented examples/claims match current behavior |
| VAL-06 | human review | milestone completion evidence | human reviewer | inspect durable reconciliation and representative hygiene lifecycle | REV-01 | explicit accepted review decision |

No Tier 4 installed-tool validation is required in M0002.

## Direct Documentation Impact

Implementation must update README/public usage documentation when the behavior exists.

If local implementation requires a non-material clarification to architecture/engineering authority, update the relevant authority document consistently. A material contract change returns to planning.

## Deferred Documentation Synchronization

No guide-document copies are synchronized into the repository.

No `.guide-sync/pending/` item is required by the planned M0002 change. Future guide-version synchronization remains a separate workflow.

## Human Review

Review ID: `REV-M0002-COMPLETION`

Class: milestone completion review  
Applicability: required  
Reviewer: project owner or delegated human reviewer

Review subject:

- complete check → explain → ignore → re-check → stale/unignore lifecycle;
- rule behavior and agent usefulness;
- stability/safety of occurrence matching;
- text/JSON process surface;
- Windows-local target/Git/project behavior;
- evidence that M0003 formatting/normalization/installed-tool scope was not pulled forward.

Acceptance:

The reviewer confirms the implemented vertical slice is coherent, useful for an agent loop, deterministic where specified, and safely persists reviewed exceptions.

Waiver: none unless project authority changes through planning.

## Completion Evidence

Implementation fills this section before `COMPLETE`.

| Obligation/evidence case | Concrete evidence | Validation gate/target | Result |
|---|---|---|---|
| AC-01–AC-04; EC-02*, EC-03*, EC-04* | `TargetResolutionDeduplicatesAndChangedIncludesCurrentGitPathsOnly`, `InvalidRepositoryTargetsFailAsProductInput`, `ProjectModelExcludesCompileRemovedFilesAndDoesNotDiscoverProjectsAboveRepositoryRoot`, `ProjectCompilationUsesProjectReferencesAndConditionalSymbols` (linked compile input, project reference symbol binding, define and language version); solution projects | VAL-03 isolated Windows SDK/Git Core/CLI fixtures; VAL-04 | passed |
| AC-05–AC-09; EC-05*, EC-06*, EC-07*, EC-08* | `RuleOrderAndRepositoryLifecycleAreDeterministic` (canonical order/all-enabled default/F ordinals), `SummaryRuleCoversRequiredSymbolsAndExcludesOtherCategories`, `SummaryRuleRequiresNonEmptyRoslynXmlSummaryDocumentation`, `LongLineUsesPhysicalUtf16Length`, `VisualBlockRuleHonorsBlankLinesAndLeadingComments` (all listed control-flow kinds), target fixture ordering assertion | VAL-01 Windows .NET 11 TUnit Core suite | passed |
| AC-10–AC-14; EC-11*, EC-14* | `LatestRunPublishesOnlySuccessfulChecksAndStateIsGitIgnored`, `IsolatedRepositorySupportsJsonExplainAndIgnoreLifecycle` (text/populated/empty JSON, bare/qualified text+JSON explain fields, latest-run bytes unchanged after explain) | VAL-01, VAL-02, VAL-03; isolated fixtures and built CLI process | passed |
| AC-15–AC-19; EC-15*, EC-18*, EC-19* | `IgnoreRejectsSourceChangedSinceTheFindingRun`, `FingerprintsKeepRelevantIdentityAndMarkChangedOccurrencesStale`, `BlankLineResolvesAnIgnoredControlFlowOccurrence`, `SummaryIgnoreSurvivesSymbolMovement`; CLI ignore/recheck/unignore and filtered active/stale listings | VAL-01 and VAL-03; isolated SDK/Git fixtures | passed |
| AC-20–AC-22; EC-21*, EC-22* | malformed config/decision exit-3/no-rewrite assertions; `DecisionWritesRejectConflictsAndCancellationWithoutLosingPreviousState`; config conflict/cancellation fixture; CLI exit 0/2/3/4 plus `UnexpectedHandlerExceptionUsesInternalFailureExitCode` | VAL-01 Core and VAL-02 CLI tests | passed |
| AC-23–AC-24; DOC-01–DOC-02 | `FormatAndNormalizeRemainNonFunctionalScaffolding`, project/package/workflow inspection, README compared with command behavior and project authority | VAL-02, VAL-04 and VAL-05; Windows repository | passed |
| REV-01 | Project owner approved `REV-M0002-COMPLETION` with “Ok, I approve” on 2026-10-04 after reviewing the durable reconciliation, project-aware context and XML-doc proof. | VAL-06; project owner | accepted |
