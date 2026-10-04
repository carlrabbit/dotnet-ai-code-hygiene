# Milestone — M0004 Deterministic Rewrites & Installed Agent Tool

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | awaiting human review |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Execution ledger | `.execution/M0004-installed-agent-tool.md` planning-seeded |
| Scope size | large coherent developer-tool milestone |
| Validation locus/platform | Windows 11 + .NET 11 SDK + Git + PowerShell |
| Installed consumer validation | required Tier 4 |
| Human review | required |

## Goal

Turn `hygiene` from a checker/reviewer into a complete installed coding-agent developer tool by adding deterministic rewrite surfaces, a safe small normalization policy, agent-oriented usage guidance, and validation of the exact locally packed/installed `.NET tool` artifact.

## Target State

```text
implement/change code
-> run relevant tests
-> hygiene normalize
-> hygiene check
-> resolve deterministic findings and semantic review work
-> rerun/recheck
```

Functional production surfaces:

```text
hygiene format
hygiene normalize
hygiene help --agent
```

Rewrite operations support repository/default, explicit file/directory, and `--changed` targets plus non-mutating `--check`. Mutation is all-or-nothing over the selected target set. The exact packed version `0.4.0` is installed and validated from an isolated consumer repository.

## Scope

- common rewrite targeting aligned with `check`;
- deterministic `format`;
- semantics-preserving `normalize`;
- fixed small Roslyn-backed normalization catalogue;
- automatic formatting of normalized changed documents;
- non-mutating `--check`;
- text and JSON rewrite results;
- transactional multi-file mutation;
- external-modification/cancellation/fault safety;
- pre/post compilation validation for normalize;
- idempotence;
- `hygiene help --agent` and accurate CLI help;
- package/product version `0.4.0`;
- exact locally installed `.NET tool` Tier-4 consumer validation;
- public install/agent-workflow docs;
- M0002/M0003 regression preservation.

## Non-goals

No broad modernization catalogue, configurable transformation rules, automatic `check` from rewrite commands, resolve/remediation loop, model integration, MCP, IDE integration, portable Agent Skill, GitHub workflows, Linux/macOS support claim, or public-feed publication requirement.

## Decisions and Constraints

- `format` is presentation-only.
- `normalize` is semantics-preserving, project-aware, and formats changed documents.
- both commands support `--check` and `--output text|json`.
- rewrite-needed status is successful exit `0`.
- target concepts align with `check`; explicit paths and `--changed` are mutually exclusive.
- mutation is all-or-nothing across the complete selected target set.
- complete plan/validation happens before source commit.
- external source modification between read/plan and commit is rejected.
- failure/cancellation must not leave mixed old/new selected target content.
- `format` may operate with ordinary compilation errors when Roslyn can load/parse sufficiently.
- `normalize` requires zero relevant compiler errors before and after transformation.
- format and normalize are idempotent.
- normalization policy is exactly `docs/specs/REWRITES.md`.
- analysis-rule enable/disable state does not configure normalization transformations.
- package/product version is exactly `0.4.0`.
- Tier 4 installs the exact locally packed package and invokes the installed command.
- Windows 11 remains the supported/claimed platform.
- existing check/review/handoff behavior remains compatible.

## Required Authority

- `docs/SPECS.md`
- `docs/specs/HYGIENE.md`
- `docs/specs/SEMANTIC-REVIEWS.md`
- `docs/specs/REVIEW-HANDOFFS.md`
- `docs/specs/REWRITES.md`
- `docs/specs/AGENT-HELP.md`
- `docs/ARCHITECTURE.md`
- `docs/ENGINEERING.md`
- `docs/TERMINOLOGY.md`
- `docs/PUBLIC-DOCS.md`

## Acceptance Criteria

- **AC-01** — `hygiene format` is functional and uses established repository/default, explicit file/directory, and `--changed` targeting with de-duplication, exclusions, repository containment, and invalid-mode/input behavior aligned with `check`.
- **AC-02** — `format` performs Roslyn-backed presentation-only formatting and does not intentionally apply M0004 semantic normalization transformations.
- **AC-03** — `format` is idempotent: after one successful mutation run, an identical second run reports zero changes and leaves source bytes unchanged.
- **AC-04** — `format --check` computes the same would-change file set as mutation mode, changes no source bytes, and returns exit `0` when formatting is needed.
- **AC-05** — `format` supports text/JSON schema-v1 output with check-only flag, target/changed/unchanged counts, deterministic repository-relative changed paths, and stderr diagnostics separation.
- **AC-06** — `format` can operate on parseable selected source when the relevant project has ordinary compiler errors, provided required Roslyn loading succeeds.
- **AC-07** — `hygiene normalize` is functional and uses the same target concepts/boundaries as `format`/`check`.
- **AC-08** — `normalize` refuses mutation when any relevant loaded project has compiler errors before transformation and returns exit `3` without changing selected source.
- **AC-09** — M0004 normalization implements only the fixed Roslyn-backed semantic simplification catalogue in `docs/specs/REWRITES.md`, leaving unsafe/ambiguous cases unchanged.
- **AC-10** — `normalize` formats every changed document as part of the normalization result.
- **AC-11** — Before commit, `normalize` recompiles/revalidates rewritten relevant project context and refuses mutation with exit `3` if compiler errors would result.
- **AC-12** — `normalize` is idempotent: a second identical run reports zero changes and leaves source bytes unchanged.
- **AC-13** — `normalize --check` computes the same would-change set as mutation mode, performs required semantic validation, changes no bytes, and returns exit `0` when changes are needed.
- **AC-14** — `normalize` supports the same text/JSON rewrite-result contract as `format`.
- **AC-15** — Rewrite mutation for both commands is all-or-nothing across the complete selected target set; no selected source is committed before complete plan construction and required validation succeed.
- **AC-16** — Rewrite commit detects external modification between source read/plan and commit and fails rather than silently overwriting it.
- **AC-17** — Cancellation/fault during mutation does not leave the selected target set in a mixed partially rewritten state.
- **AC-18** — Successful multi-file rewrite commits exactly the validated plan and leaves unselected/unaffected source unchanged.
- **AC-19** — Rewrite operations do not create/modify ignore decisions, semantic review state, rule configuration, or unrelated committed `.hygiene` state.
- **AC-20** — Analysis rules and rewrite transformations remain separate; M0004 does not make transformations configurable through existing rule enable/disable state.
- **AC-21** — `hygiene help --agent` returns exit `0` and stable guidance covering workflow, targets, outputs/exits, rewrite semantics/`--check`, deterministic findings, semantic review escalation/handoff, and state ownership.
- **AC-22** — Root and relevant subcommand help accurately expose functional format/normalize/review/help surfaces and contain no stale scaffolding wording.
- **AC-23** — Product/package version is exactly `0.4.0`, package metadata is consistent, and installed `hygiene --version` reports `0.4.0`.
- **AC-24** — Tier-4 validation packs the current package, installs that exact package via `dotnet tool install` from an isolated local package source/tool path, and invokes the installed `hygiene` command rather than development build outputs.
- **AC-25** — The isolated installed-tool consumer scenario exercises `--version`, `help --agent`, `format --check`, format mutation, `normalize --check`, normalize mutation, and `check`, proving expected source/output/exit behavior.
- **AC-26** — `./eng/validate.ps1` remains the canonical thin Windows-local repository validation entry point and includes/passes Core, CLI process, fixture, pack, and Tier-4 installed-tool validation.
- **AC-27** — Existing M0002 deterministic finding/ignore/target/exit behavior and completed M0003 semantic review/expansion/handoff behavior remain regression-covered and compatible.
- **AC-28** — No model/provider SDK/call, automatic remediation/resolve loop, MCP, IDE integration, portable Agent Skill, GitHub workflow, or cross-platform support claim is introduced.
- **DOC-01** — README/public docs explain installation, version, format, normalize, `--check`, canonical agent workflow, rewrite safety/idempotence, analysis-vs-transformation distinction, and existing review escalation/handoff.
- **DOC-02** — Public docs accurately describe Windows 11 support and do not claim public package publication, Linux/macOS validation, broad modernization, automatic remediation, MCP, or IDE integration.
- **REV-01** — Human completion review confirms rewrite behavior is predictably safe/useful for coding agents, the normalization catalogue is conservative, agent help is effective, installed-tool validation represents a real consumer, and deferred orchestration scope was not pulled forward.

## Acceptance Evidence Topology

| ID | Parent | Required evidence case | Why separate |
|---|---|---|---|
| EC-01a | AC-01 | repository-default format targeting | target mode |
| EC-01b | AC-01 | explicit file/multiple/directory overlap de-duplication | target mode |
| EC-01c | AC-01 | `--changed` format targeting | target mode |
| EC-01d | AC-01 | invalid outside/unsupported/mutually-exclusive target invocation | error path |
| EC-02a | AC-02 | whitespace/presentation changes | positive formatting |
| EC-02b | AC-02 | formatting does not perform semantic simplification | boundary |
| EC-04a | AC-04 | format check detects pending changes with unchanged bytes | check mode |
| EC-04b | AC-04 | format check on clean source reports zero changes | clean path |
| EC-05a | AC-05 | populated format JSON | schema path |
| EC-05b | AC-05 | clean format JSON/text | zero-change path |
| EC-06a | AC-06 | format succeeds with compiler error | syntax-vs-semantic boundary |
| EC-08a | AC-08 | pre-existing compiler error rejects normalize | semantic precondition |
| EC-08b | AC-08 | rejection leaves all selected bytes unchanged | mutation safety |
| EC-09a | AC-09 | redundant qualification simplified | transformation |
| EC-09b | AC-09 | type/name simplification safely applied | transformation |
| EC-09c | AC-09 | unnecessary `this.` safely removed | transformation |
| EC-09d | AC-09 | unsafe/ambiguous simplification unchanged | negative safety |
| EC-10a | AC-10 | structural rewrite output is automatically formatted | composition |
| EC-11a | AC-11 | valid rewritten compilation accepted | post-validation |
| EC-11b | AC-11 | post-rewrite compiler failure blocks commit | failure path |
| EC-13a | AC-13 | normalize check detects same paths as mutation | check equivalence |
| EC-13b | AC-13 | normalize check leaves bytes unchanged | non-mutating |
| EC-15a | AC-15 | validation failure in one selected target causes zero commits | all-or-nothing |
| EC-16a | AC-16 | external source modification conflict rejected | concurrency |
| EC-17a | AC-17 | fault/cancellation before/during commit preserves/restores complete selected set | rollback |
| EC-18a | AC-18 | successful multi-file commit changes all-and-only planned targets | positive transaction |
| EC-19a | AC-19 | rewrite leaves decisions/config/review state unchanged | persistence boundary |
| EC-20a | AC-20 | disabling analysis rule does not disable normalization transformation | abstraction separation |
| EC-21a | AC-21 | agent help contains all required workflow sections | usability |
| EC-22a | AC-22 | root/format/normalize/help command help reflects current functionality | discoverability |
| EC-23a | AC-23 | package metadata version 0.4.0 | package identity |
| EC-23b | AC-23 | installed `hygiene --version` is 0.4.0 | runtime identity |
| EC-24a | AC-24 | local package install uses newly packed nupkg/source | consumer artifact |
| EC-24b | AC-24 | installed command path is invoked, not dev DLL | process boundary |
| EC-25a | AC-25 | installed format lifecycle | consumer behavior |
| EC-25b | AC-25 | installed normalize lifecycle | consumer behavior |
| EC-25c | AC-25 | installed check + agent-help lifecycle | consumer behavior |
| EC-27a | AC-27 | M0002 finding/ignore regression | compatibility |
| EC-27b | AC-27 | M0003 semantic review/handoff regression | compatibility |

## Validation

| ID | Depth | Target/locus | Proves |
|---|---|---|---|
| VAL-01 | Tier 1 | Core rewrite/planning/transaction/semantic tests / Windows 11 + .NET 11 | AC-01..AC-20 and mapped ECs |
| VAL-02 | Tier 1 | built CLI process tests / Windows 11 + .NET 11 | public format/normalize/help/output/exit contracts |
| VAL-03 | Tier 3 | isolated SDK-style Git fixture repos / Windows 11 + .NET 11 + Git | real target/project/rewrite/conflict/regression behavior |
| VAL-04 | Tier 4 | exact locally packed+installed .NET tool / isolated consumer repo / Windows 11 | AC-23..AC-25 and mapped ECs |
| VAL-05 | Tier 2 | complete repository via `./eng/validate.ps1` | AC-26..AC-28 and aggregate regression/scope evidence |
| VAL-06 | Tier 2 | README/public docs vs installed behavior | DOC-01, DOC-02 |
| VAL-07 | Human | rewrite/agent-tool usability | REV-01 |

## Human Review

Review ID: `REV-M0004-COMPLETION`.

Confirm: formatting is presentation-only; normalization is conservative/useful; all-or-nothing mutation is credible; `--check` is useful; agent help is sufficient; installed-tool validation represents a real consumer; M0002/M0003 remain coherent; no resolve/model/MCP/IDE scope was pulled forward.

## Completion Evidence

Implementation and automated validation evidence collected on 2026-10-04, Windows 11, .NET SDK `11.0.100-rc.1.26425.128`:

- `./eng/validate.ps1` is the canonical repository gate. Its last completed run passed restore, Release build, Core 21/21, CLI 19/19, package creation, local installation of `DotNetAiCodeHygiene.Tool` version `0.4.0`, and installed consumer invocations through the generated `hygiene.exe` in an isolated Git/SDK-style repository.
- The installed consumer exercised `--version`, `help --agent`, `format --check`, format mutation, `normalize --check`, normalize mutation, and `check`. Tier-4 asserts check-only source preservation, expected mutation, and second-run idempotence for both rewrites.
- Core rewrite lifecycle evidence is in `RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent` and `NormalizeRejectsCompilerErrorsAndRewriteConflictNeverOverwritesSource`; the latter covers pre-existing compiler errors, a concurrent source edit detected before commit, injected cancellation after the first replacement, rollback, and successful two-file mutation without changing an unselected file.
- The CLI test suite retains the M0002/M0003 lifecycle coverage and now checks agent help. README/public docs describe the install/workflow boundary, rewrite semantics, and Windows 11 support without claiming public-feed publication or deferred integration scope.
- Review follow-up evidence is recorded in `.execution/M0004-installed-agent-tool.md`: target selection now delegates to the shared `HygieneEngine` resolver; focused tests cover all four normalization catalogue cases and post-rewrite compiler rejection with selected-byte preservation; CLI fixtures cover output/help/state contracts. The final suite passed Core 23/23 and CLI 21/21.
- Tier-4 provenance uses a fresh `NUGET_PACKAGES` directory, an isolated `NuGet.config` with package source mapping that pins `DotNetAiCodeHygiene.Tool` to the current-run local feed, a SHA-256 equality check between packed and copied `.nupkg`, and direct invocation of the installed tool-path command.
- `git diff --check` passed. Repository inspection found no GitHub workflow, model/provider dependency or invocation, MCP/IDE implementation, or cross-platform support claim added.

`REV-M0004-COMPLETION` remains required and has not been self-approved. The milestone lifecycle remains pending human review.
