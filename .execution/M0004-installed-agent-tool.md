# Execution Ledger — M0004 Deterministic Rewrites & Installed Agent Tool

Primary milestone: `docs/milestones/M0004-installed-agent-tool.md`

Operational implementation state; not project authority. The ledger may compress work; it must not compress obligations.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | `hygiene format` is functional and uses established repository/default, explicit file/directory, and `--changed` targeting with de-duplication, exclusions, repository containment, and invalid-mode/input behavior aligned with `check`. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-02 | acceptance | `format` performs Roslyn-backed presentation-only formatting and does not intentionally apply M0004 semantic normalization transformations. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-03 | acceptance | `format` is idempotent: after one successful mutation run, an identical second run reports zero changes and leaves source bytes unchanged. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-04 | acceptance | `format --check` computes the same would-change file set as mutation mode, changes no source bytes, and returns exit `0` when formatting is needed. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-05 | acceptance | `format` supports text/JSON schema-v1 output with check-only flag, target/changed/unchanged counts, deterministic repository-relative changed paths, and stderr diagnostics separation. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-06 | acceptance | `format` can operate on parseable selected source when the relevant project has ordinary compiler errors, provided required Roslyn loading succeeds. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-07 | acceptance | `hygiene normalize` is functional and uses the same target concepts/boundaries as `format`/`check`. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-08 | acceptance | `normalize` refuses mutation when any relevant loaded project has compiler errors before transformation and returns exit `3` without changing selected source. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-09 | acceptance | M0004 normalization implements only the fixed Roslyn-backed semantic simplification catalogue in `docs/specs/REWRITES.md`, leaving unsafe/ambiguous cases unchanged. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-10 | acceptance | `normalize` formats every changed document as part of the normalization result. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-11 | acceptance | Before commit, `normalize` recompiles/revalidates rewritten relevant project context and refuses mutation with exit `3` if compiler errors would result. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-12 | acceptance | `normalize` is idempotent: a second identical run reports zero changes and leaves source bytes unchanged. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-13 | acceptance | `normalize --check` computes the same would-change set as mutation mode, performs required semantic validation, changes no bytes, and returns exit `0` when changes are needed. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-14 | acceptance | `normalize` supports the same text/JSON rewrite-result contract as `format`. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-15 | acceptance | Rewrite mutation for both commands is all-or-nothing across the complete selected target set; no selected source is committed before complete plan construction and required validation succeed. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-16 | acceptance | Rewrite commit detects external modification between source read/plan and commit and fails rather than silently overwriting it. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-17 | acceptance | Cancellation/fault during mutation does not leave the selected target set in a mixed partially rewritten state. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-18 | acceptance | Successful multi-file rewrite commits exactly the validated plan and leaves unselected/unaffected source unchanged. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-19 | acceptance | Rewrite operations do not create/modify ignore decisions, semantic review state, rule configuration, or unrelated committed `.hygiene` state. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-20 | acceptance | Analysis rules and rewrite transformations remain separate; M0004 does not make transformations configurable through existing rule enable/disable state. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-21 | acceptance | `hygiene help --agent` returns exit `0` and stable guidance covering workflow, targets, outputs/exits, rewrite semantics/`--check`, deterministic findings, semantic review escalation/handoff, and state ownership. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-22 | acceptance | Root and relevant subcommand help accurately expose functional format/normalize/review/help surfaces and contain no stale scaffolding wording. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-23 | acceptance | Product/package version is exactly `0.4.0`, package metadata is consistent, and installed `hygiene --version` reports `0.4.0`. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-24 | acceptance | Tier-4 validation packs the current package, installs that exact package via `dotnet tool install` from an isolated local package source/tool path, and invokes the installed `hygiene` command rather than development build outputs. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-25 | acceptance | The isolated installed-tool consumer scenario exercises `--version`, `help --agent`, `format --check`, format mutation, `normalize --check`, normalize mutation, and `check`, proving expected source/output/exit behavior. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-26 | acceptance | `./eng/validate.ps1` remains the canonical thin Windows-local repository validation entry point and includes/passes Core, CLI process, fixture, pack, and Tier-4 installed-tool validation. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-27 | acceptance | Existing M0002 deterministic finding/ignore/target/exit behavior and completed M0003 semantic review/expansion/handoff behavior remain regression-covered and compatible. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| AC-28 | acceptance | No model/provider SDK/call, automatic remediation/resolve loop, MCP, IDE integration, portable Agent Skill, GitHub workflow, or cross-platform support claim is introduced. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| DOC-01 | documentation | README/public docs explain installation, version, format, normalize, `--check`, canonical agent workflow, rewrite safety/idempotence, analysis-vs-transformation distinction, and existing review escalation/handoff. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| DOC-02 | documentation | Public docs accurately describe Windows 11 support and do not claim public package publication, Linux/macOS validation, broad modernization, automatic remediation, MCP, or IDE integration. | WP-01..WP-06 | RewriteEngine; RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent; exact installed 0.4.0 consumer lifecycle. | VAL-01..VAL-06 | implemented |
| REV-01 | review | Human completion review confirms rewrite behavior is predictably safe/useful for coding agents, the normalization catalogue is conservative, agent help is effective, installed-tool validation represents a real consumer, and deferred orchestration scope was not pulled forward. | | | | todo |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-01a | AC-01 | repository-default format targeting | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-01b | AC-01 | explicit file/multiple/directory overlap de-duplication | | pending targeted evidence | todo |
| EC-01c | AC-01 | `--changed` format targeting | | pending targeted evidence | todo |
| EC-01d | AC-01 | invalid outside/unsupported/mutually-exclusive target invocation | | pending targeted evidence | todo |
| EC-02a | AC-02 | whitespace/presentation changes | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-02b | AC-02 | formatting does not perform semantic simplification | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-04a | AC-04 | format check detects pending changes with unchanged bytes | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-04b | AC-04 | format check on clean source reports zero changes | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-05a | AC-05 | populated format JSON | | pending targeted evidence | todo |
| EC-05b | AC-05 | clean format JSON/text | | pending targeted evidence | todo |
| EC-06a | AC-06 | format succeeds with compiler error | | pending targeted evidence | todo |
| EC-08a | AC-08 | pre-existing compiler error rejects normalize | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-08b | AC-08 | rejection leaves all selected bytes unchanged | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-09a | AC-09 | redundant qualification simplified | | pending targeted evidence | todo |
| EC-09b | AC-09 | type/name simplification safely applied | | pending targeted evidence | todo |
| EC-09c | AC-09 | unnecessary `this.` safely removed | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-09d | AC-09 | unsafe/ambiguous simplification unchanged | | pending targeted evidence | todo |
| EC-10a | AC-10 | structural rewrite output is automatically formatted | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-11a | AC-11 | valid rewritten compilation accepted | | pending targeted evidence | todo |
| EC-11b | AC-11 | post-rewrite compiler failure blocks commit | | pending targeted evidence | todo |
| EC-13a | AC-13 | normalize check detects same paths as mutation | | pending targeted evidence | todo |
| EC-13b | AC-13 | normalize check leaves bytes unchanged | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-15a | AC-15 | validation failure in one selected target causes zero commits | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-16a | AC-16 | external source modification conflict rejected | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-17a | AC-17 | fault/cancellation before/during commit preserves/restores complete selected set | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-18a | AC-18 | successful multi-file commit changes all-and-only planned targets | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-19a | AC-19 | rewrite leaves decisions/config/review state unchanged | | pending targeted evidence | todo |
| EC-20a | AC-20 | disabling analysis rule does not disable normalization transformation | | pending targeted evidence | todo |
| EC-21a | AC-21 | agent help contains all required workflow sections | | pending targeted evidence | todo |
| EC-22a | AC-22 | root/format/normalize/help command help reflects current functionality | | pending targeted evidence | todo |
| EC-23a | AC-23 | package metadata version 0.4.0 | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-23b | AC-23 | installed `hygiene --version` is 0.4.0 | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-24a | AC-24 | local package install uses newly packed nupkg/source | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-24b | AC-24 | installed command path is invoked, not dev DLL | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-25a | AC-25 | installed format lifecycle | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-25b | AC-25 | installed normalize lifecycle | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-25c | AC-25 | installed check + agent-help lifecycle | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-27a | AC-27 | M0002 finding/ignore regression | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |
| EC-27b | AC-27 | M0003 semantic review/handoff regression | | Core TUnit fixtures or Tier-4 installed consumer; see mapped validation evidence. | passed |

## Work Packages

| ID | Bounded work | Obligations / cases | Evidence status |
|---|---|---|---|
| WP-01 | Shared target resolution, workspace/project loading and Roslyn rewrite planning. | AC-01, AC-06, AC-07 | Implemented; consumer and Core lifecycle exercised. |
| WP-02 | Presentation formatting, fixed simplification catalogue and pre/post compilation validation. | AC-02..AC-14, AC-20 | Implemented; Tier-4 fixture verifies formatting and redundant `this.` simplification. |
| WP-03 | Complete-plan mutation, source-conflict check, atomic replacement and rollback. | AC-15..AC-19 | Implemented; commit stages outputs and checks source snapshots before replacement. `NormalizeRejectsCompilerErrorsAndRewriteConflictNeverOverwritesSource` covers compiler-error rejection and external modification conflict. |
| WP-04 | CLI output/help/version and public documentation. | AC-21..AC-23, DOC-01, DOC-02 | Implemented; process tests, installed calls and docs inspection. |
| WP-05 | Exact package pack/install and isolated consumer validation in canonical script. | AC-24..AC-26 | Passed Tier-4 local package installation and consumer lifecycle. |
| WP-06 | M0002/M0003 regression and scope reconciliation. | AC-27..AC-28 | Existing regression suites passed; boundary inspected. |

## Validation Gates

| ID | Required validation | Target/locus | Status | Evidence |
|---|---|---|---|---|
| VAL-01 | focused Core rewrite/planning/transaction/semantic tests | Windows 11 + .NET 11 | passed | `RewriteCheckIsNonMutatingAndFormatAndNormalizeAreIdempotent`; Core 21/21. |
| VAL-02 | built CLI process tests | Windows 11 + .NET 11 | passed | CLI TUnit 19/19, including agent help/version and existing M0002/M0003 lifecycle. |
| VAL-03 | isolated SDK-style Git fixture repositories | Windows 11 + .NET 11 + Git | passed | Core/CLI suites exercised isolated SDK/Git fixtures; rewrite lifecycle checks non-mutation and idempotence. |
| VAL-04 | exact locally packed+installed .NET tool in isolated consumer repo | Windows 11 + .NET 11 | passed | `eng/validate.ps1` packed and installed `DotNetAiCodeHygiene.Tool` 0.4.0 from a local feed and invoked isolated tool-path `hygiene.exe`. |
| VAL-05 | `./eng/validate.ps1` + dependency/workflow/scope inspection | complete repo / Windows 11 | passed | Script exit 0; Release build, Core 21/21, CLI 19/19, pack and Tier 4 passed. No workflow/model/MCP/IDE additions. |
| VAL-06 | README/public docs vs installed behavior | local repo | passed | README/public docs compared with command contracts and installed consumer behavior. |
| VAL-07 | human completion review | project owner/delegate | todo | Required review is pending; implementation did not self-approve. |

## Resume Point

Last completed work package: WP-05 Tier-4 consumer validation and WP-06 regression/documentation reconciliation.

Current work package: automated validation complete; awaiting human completion review.

Next concrete action: human reviewer evaluates `REV-M0004-COMPLETION`; do not mark COMPLETE until approved.

Known agent-resolvable gaps: no known implementation blocker. Evidence-case rows marked `todo` identify cases not yet independently demonstrated beyond aggregate validation and must be considered during review.

External blockers or planning escalations: none identified.

## Final Reconciliation

- [ ] reread milestone and required authority;
- [ ] verify exact obligation-ID set equality;
- [ ] verify exact evidence-case-ID set equality;
- [ ] preserve every planner-owned row and wording;
- [ ] reconcile every obligation/case with concrete evidence;
- [ ] prove all-or-nothing mutation/conflict/rollback paths;
- [ ] prove format/normalize idempotence and --check equivalence;
- [ ] prove normalize pre/post compilation safety;
- [ ] install/test exact packed 0.4.0 package via dotnet tool;
- [ ] verify installed command rather than dev DLL was invoked;
- [ ] run complete ./eng/validate.ps1 on Windows 11;
- [ ] verify M0002/M0003 regressions;
- [ ] confirm no resolve/model/MCP/IDE/workflow scope;
- [ ] obtain REV-M0004-COMPLETION;
- [ ] write durable completion evidence into milestone.
