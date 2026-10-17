# Execution Ledger — M0018 Autonomous Hygiene Lifecycle

Primary milestone: `docs/milestones/M0018-autonomous-hygiene-lifecycle.md`

Planning-seeded for Agentic Project Guides 0.9.1. The milestone and referenced operative specs are authoritative. This ledger is mutable implementation evidence, not an amendment. Planning owns exact ID/type/obligation and evidence-case wording; implementation owns work packages, evidence and status. The ledger may compress work, never obligations.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | Ship the .NET tool with the canonical `dotnet-hygiene` shim so `dotnet hygiene` works from an isolated installed package; remove the legacy bare `hygiene` public command without aliases, and expose correct version/help. | | | | pass |
| AC-02 | acceptance | The no-argument `dotnet hygiene` entrypoint emits and drives an agent-executable, state-aware lifecycle without requiring the caller to know bootstrap, update, rule batches or acceptance commands; the CLI never calls an AI model/provider. | | | | partial |
| AC-03 | acceptance | Automatically recognize a missing/uninitialized profile and initiate full-repository bootstrap; recognize current versus outdated/invalid profile state and produce the appropriate update or safe incompatibility action without silently replacing arbitrary user files. | WP-01, WP-03 | Installed bootstrap plus unsupported profile consumer in `eng/validate.ps1` | VAL-05 passed; EC-03a/b | partial: supported drift reconciliation evidence remains partial |
| AC-04 | acceptance | Bootstrap covers all eligible supported SDK-style C# projects across the repository, including repositories with multiple solutions and projects not included in a preferred solution; honor generated/unsupported source exclusions and preserve unrelated data. | | | | partial |
| AC-05 | acceptance | Before adopting the hygiene policy, run a nonmutating assessment of existing effective project formatting and report actual pre-hygiene drift; do not claim to infer whether `dotnet format` historically ran. | | | | pass |
| AC-06 | acceptance | After adopting the profile, apply all enabled authoritative deterministic formatting/normalization to eligible repository sources; restrict dotnet-format-based code fixes to hygiene-owned policies/explicit diagnostic allowlists, never generic analyzer fixes, and enforce repeat-run idempotence. | | | | partial |
| AC-07 | acceptance | Hygiene-owned EditorConfig and MSBuild policy is reconciled without clobbering unrelated sections; distinguish the post-hygiene effective policy from the prior project formatting baseline, and report changed paths and attribution limits truthfully. | | | | partial |
| AC-08 | acceptance | Bootstrap and material policy migrations produce a reviewable baseline report with exact tool/profile/rule versions, changed paths, pre/post format observations, remaining conflicts and validation results; baseline status remains approval-required until explicitly accepted by an authorized external approver. | | | | partial |
| AC-09 | acceptance | Repository-wide mutation is safe: plan/validate changes, preserve preexisting staged/unstaged/untracked work, avoid destructive reset, and ensure failures, cancellations and rejection cannot silently discard user data or claim a partially applied baseline complete. | WP-02 | `AutonomousMutationTransaction`; injected second-project formatter failure consumer in `eng/validate.ps1` | VAL-05 rollback fixture passed; EC-09a successful mixed-worktree preservation remains partial | partial |
| AC-10 | acceptance | For an initialized repository, infer a gate using explicit/trusted context before unambiguous local feature-branch merge-base or actual worktree changes; ambiguous or missing comparison context yields actionable agent instructions, never an accidental zero-review bare check or invented remote. | | | | pass |
| AC-11 | acceptance | Ordinary gates perform authoritative deterministic remediation and preserve the existing M0016 six-rule independent bounded gate-local semantic-review selection contracts without adding cross-rule selection coupling or a global budget. | | | | pass |
| AC-12 | acceptance | Generate a selected-only, consolidated, grouped agent work order with the exact original rule, questions, escalation policy, batch/item handles, current source identity/context and deduplicated files; grouping does not merge independent rule judgments or acceptances. | | | | partial |
| AC-13 | acceptance | The agent workflow explicitly directs inspection, contextual remediation, positive-only acceptance, escalation for negative/uncertain items, stale-ticket revalidation, reruns after modifications and no false clean-state claims. | | | | pass |
| AC-14 | acceptance | Publish truthful terminal outcomes complete, approval-required, escalation-required and incomplete, with structured/text reporting and coherent stdout/stderr/exit semantics that distinguish command execution success from gate completion. | | | | partial |
| AC-15 | acceptance | Retain practical nonmutating manual diagnostics for paths, `--base`, `--changed`, `--review-all`, findings, rules, explain and review lifecycle in the redesigned `dotnet hygiene` hierarchy; keep `--review-all` bounded and do not implement `--yolo-all` in M0018. | | | | pass |
| AC-16 | acceptance | When a configured trusted package source/channel is available, detect and perform or direct safe out-of-process tool upgrades, restart/revalidate the installed tool, reconcile supported profile state and request approval of materially changed policy; unavailable feed/offline/incompatible states are reported accurately without unauthorized host/CI changes. | WP-03 | Autonomous stable-feed discovery/update/restart; offline and unsupported-profile isolated consumers in `eng/validate.ps1` | VAL-05 passed; EC-16a/b | pass |
| AC-17 | acceptance | The pre-1.0 contract has no backward-compatibility requirements for legacy command names, CLI shapes, JSON schemas, persisted review/profile/sampling formats or internal APIs; obsolete state fails safely or is replaced only through explicitly safe policy, with no silent loss of repository data. | | | | pass |
| AC-18 | acceptance | Normal operation needs no project-specific AGENTS.md insertion, no separate agent skill, no model-provider integration, no generalized rule DSL, and no generic plugin/pipeline architecture; use the existing independent rule modules and shared project facts. | | | | pass |
| AC-19 | acceptance | A freshly built version 0.10.0 tool package installed into an isolated Windows 11/.NET 11 consumer executes the `dotnet hygiene` command and proves bootstrap, pre/post restricted formatting, normal gates, manual diagnostics, lifecycle reports and trusted-source update behavior rather than relying on an old shim or development binary. | WP-03 | `eng/validate.ps1` exact 0.10.0 isolated tool consumer, update/restart fixture, and actual SDK formatter | VAL-05 passed; combined EC-19b provenance/allowlist/idempotence case remains partial | partial |
| AC-20 | acceptance | Software validation demonstrates CLI orchestration, safe rewrites, rule selection, work-order fidelity, identity/revalidation and report semantics, without assigning gold-label subjective judgments or claiming that sampling certifies population quality. | | | | pass |
| DOC-01 | documentation | Replace the README onboarding structure with First Bootstrap, Run on Gates, Manual Inspection and Debugging, and Updating to a New Release, plus correctly labeled deferred YOLO usage and real copy-pasteable `dotnet hygiene` examples tested against the installed artifact. | | | | pass |
| DOC-02 | documentation | Update operative engineering, profile, rewrite, semantic review, agent-help, package/distribution and milestone index authority to reflect actual M0018 behavior and record the explicit pre-1.0 no-compatibility policy; leave completed historical milestone evidence intact. | | | | pass |
| REV-01 | human review | Prepare `.review/REV-M0018-COMPLETION.md` for blocking project-owner/delegated-approver review of the autonomous workflow, baseline approval boundary, mutation safety, genuine installed-tool provenance, evidence-case closure and scope fidelity; do not invent approval. | | | | prepared; review pending |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-01a | AC-01 | Fresh isolated installed `dotnet hygiene` command resolves current artifact and `--version`/help accurately | VAL-05 | | pass |
| EC-01b | AC-01 | Legacy bare `hygiene` is not distributed as supported shim; no fallback alias | VAL-05 | | pass |
| EC-03a | AC-03 | Missing profile bootstraps and supported drift reconciles | VAL-01; VAL-05 | Installed 0.10.0 bootstrap emitted approval-required; profile idempotence tests passed; dedicated installed supported-drift reconciliation receipt remains partial | partial |
| EC-03b | AC-03 | Malformed/unsupported profile or conflicting managed artifacts do not silently overwrite user data | VAL-01; VAL-05 | Installed unsupported-version profile consumer returned actionable incomplete status and preserved exact profile marker bytes; canonical validator passed | pass |
| EC-04a | AC-04 | Multiple solutions and orphaned eligible SDK projects are covered | VAL-01; VAL-03 | | partial |
| EC-04b | AC-04 | Generated and unsupported files stay excluded without suppressing eligible sources | VAL-01; VAL-03 | | partial |
| EC-05a | AC-05 | Original effective formatting assessment runs without source edits | VAL-01; VAL-03 | | pass |
| EC-06a | AC-06 | Hygiene-approved whitespace/layout and selected style fixes applied repository-wide | VAL-01; VAL-03 | | partial |
| EC-06b | AC-06 | Non-allowlisted analyzer fixes never run and second pass is a no-op | VAL-01; VAL-03 | | partial |
| EC-08a | AC-08 | Bootstrap report remains pending before approver decision; externally approved baseline becomes active | VAL-01; VAL-05 | | pass |
| EC-08b | AC-08 | Rejected/materially changed baseline is not reported complete | VAL-01; VAL-05 | | partial |
| EC-09a | AC-09 | Existing dirty/staged/untracked input is preserved under successful setup | VAL-01 | | partial |
| EC-09b | AC-09 | Fault/cancel/conflict does not leave corrupt or falsely accepted baseline | VAL-01; VAL-05 | Injected second-project formatter failure returned incomplete/rolledBack and restored both source files and managed profile artifacts; canonical validator passed | pass |
| EC-10a | AC-10 | Unambiguous feature branch with committed changes selects PR-base gate | VAL-02 | | pass |
| EC-10b | AC-10 | Actual worktree change selects worktree gate when no branch comparison applies | VAL-02 | | pass |
| EC-10c | AC-10 | Missing/ambiguous upstream/base yields actionable incomplete request and no empty success | VAL-02 | | pass |
| EC-12a | AC-12 | Same-file different-rule subjects share supplied context but retain both exact original identities and questions | VAL-01 | | partial |
| EC-12b | AC-12 | Separate-source subjects and zero-work rules preserve ordering/identity without phantom work | VAL-01 | | partial |
| EC-13a | AC-13 | Successful explicit per-rule acceptance after judgment; repeated gate no refill | VAL-01 | | pass |
| EC-13b | AC-13 | Edited source rejects stale acceptance; uncertain/negative work remains outstanding/escalated | VAL-01 | | pass |
| EC-14a | AC-14 | Complete report names processed gate and exact verification evidence | VAL-02 | | partial |
| EC-14b | AC-14 | Approval, escalation, validation failure and unresolved work remain distinct non-complete outcomes | VAL-02 | | partial |
| EC-16a | AC-16 | Configured trusted feed updates using isolated packed versions and verifies new shim/profile behavior | VAL-05 | Fresh isolated 0.10.0 detected local stable 0.10.1 fixture, updated after parent exit, restarted, revalidated approval-required status, and reported shim version 0.10.1 | pass |
| EC-16b | AC-16 | Offline/unconfigured feed and incompatible binary/profile report honest next action; no unauthorized provisioning | VAL-05 | Empty-source consumer reported unavailable and continued bootstrap; unsupported profile consumer reported safe actionable incompatibility without replacing marker | pass |
| EC-19a | AC-19 | Fresh 0.10.0 packaged tool installed and invoked in isolated consumer | VAL-05 | | pass |
| EC-19b | AC-19 | Original configuration and post-hygiene formatting verified with actual formatter process and bounded diagnostics | VAL-03; VAL-05 | | partial |

## Work Packages

| ID | Bounded scope | Related obligations/evidence | Status |
|---|---|---|---|
| WP-01 | Canonical tool identity, autonomous lifecycle state/output, gate selection, grouped agent work order, approval boundary and report semantics | AC-01–AC-03, AC-08, AC-10–AC-18; EC-01a/b, EC-03a/b, EC-08a/b, EC-10a/b/c, EC-12a/b, EC-13a/b, EC-14a/b | implemented; cases reconciled with partial gaps |
| WP-02 | Repository-wide profile/source discovery, original formatting assessment, allowlisted authoritative formatting, validation and mutation safety | AC-04–AC-09, AC-19–AC-20; EC-04a/b, EC-05a, EC-06a/b, EC-09a/b, EC-19b | implemented; rollback and evidence gaps remain |
| WP-03 | Trusted package update path and fresh isolated 0.10.0 consumer validation | AC-16, AC-19; EC-16a/b, EC-19a | package-manager update consumer proven; autonomous detection incomplete |
| WP-04 | README and operative engineering/spec/package/milestone documentation updates | DOC-01, DOC-02 | implemented; reviewed against actual behavior |
| WP-05 | Criterion-specific evidence, final reconciliation, completion audit and review request | all obligations/evidence; REV-01 | audit complete; milestone gates fail on documented gaps and review pending |

The milestone acceptance IDs AC-01–AC-20 and evidence IDs EC-01a–EC-19b were compared against this seeded ledger before implementation. The ledger's DOC-01, DOC-02 and REV-01 obligations are retained as separate documentation and human-review obligations; the milestone defines those in its documentation and human-review sections rather than the AC list.

## Completion Evidence Reconciliation (2026-10-17)

The latest canonical `eng/validate.ps1` run completed successfully on Windows 11 with PowerShell, Git, and .NET SDK `11.0.100-rc.1.26425.128` (RC). It rebuilt and repacked 0.10.0, passed all 118 tests (94 Core, 24 CLI), passed the exact isolated 0.10.0 installed consumer and repository M0006 self-host fixture, and passed `git diff --check`. The run also exercised a controlled 0.10.0-to-0.10.1 autonomous detection/update/restart/revalidation, an offline configured-source failure, unsupported-profile preservation, and injected late formatter failure with repository-wide rollback. The fixture 0.10.1 is local validation data, not a product release.

Newly closed cases: EC-03b (unsupported profile marker preserved), EC-09b (late formatter failure rollback), EC-16a (trusted-feed update/restart), and EC-16b (offline and incompatible-profile action). Existing package, approval, gate, acceptance, stale-ticket, manual diagnostics, selected-only work-order, formatter and self-host evidence remains as previously recorded.

Remaining evidence gaps without aggregate inference: EC-04a (two solutions plus orphan project), EC-04b (generated/unsupported exclusions with eligible-source control), EC-06b (explicit non-allowlisted analyzer negative and isolated second-run no-op), EC-08b (rejected/materially changed baseline path), EC-09a (successful bootstrap preserving mixed staged/unstaged/untracked work), EC-12a/b (grouping identity/context and zero-work ordering receipt), EC-14a/b (full terminal-report matrix), and EC-19b (combined package provenance/configuration/allowlist/idempotence receipt) remain partial pending exact fixtures or auditable test mapping. AC-09 remains partial because EC-09a is not closed. AC-19 remains partial while EC-19b is incomplete. VAL-06 independent human review is pending; no approval has been requested or fabricated.

The obligation registry still retains AC-01–AC-20, DOC-01/02 and REV-01; the evidence registry retains all 26 original cases, parent links and wording. Current milestone status: **BLOCKED** on the enumerated evidence gaps; after those close, authorized independent review is still required before COMPLETE.

## Validation Gates

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | Tier 1 — focused lifecycle/formatter/orchestration Core tests; Focused tests through existing Core test project, invoked with `dotnet test` (TUnit/MTP compatible) | local Windows 11 + .NET 11 SDK; SDK-style temp repositories | AC-02, AC-07, AC-11, AC-17, AC-18, AC-20; EC-03a, EC-03b, EC-04a, EC-04b, EC-05a, EC-06a, EC-06b, EC-08a, EC-08b, EC-09a, EC-09b, EC-12a, EC-12b, EC-13a, EC-13b | canonical suite passed; unmapped cases remain partial | passed; 94 Core tests |
| VAL-02 | Tier 3 — real CLI/Git gate and status end-to-end tests; Built CLI process tests and actual Git branch/worktree transitions through canonical test harness | local Windows 11 + .NET 11 SDK + Git + PowerShell; isolated Git fixtures | AC-10, AC-13, AC-14, AC-15; EC-10a, EC-10b, EC-10c, EC-14a, EC-14b | canonical suite passed; EC-14 outcome matrix remains partial | passed; 24 CLI tests |
| VAL-03 | Tier 3 — external dotnet-format policy restriction and full-repo bootstrap; Non-mutating pre-format, post-profile allowlisted format/style, actual project compilation and independent rerun | local Windows 11 + .NET 11 SDK; multiple solution/project real fixtures; installed dotnet format | AC-04, AC-05, AC-06, AC-07; EC-04a, EC-04b, EC-05a, EC-06a, EC-06b, EC-19b | actual SDK formatter exercised; discovery/exclusion/negative allowlist cases remain partial | partial |
| VAL-04 | Tier 2 — canonical repository validation and documentation reconciliation; `.\eng\validate.ps1`; `git diff --check`; verify README examples and exact ledger IDs | local Windows 11 + .NET 11 SDK + PowerShell + Git | AC-17, AC-18, AC-20, DOC-01, DOC-02 | passed | canonical command completed; diff check passed |
| VAL-05 | Tier 4 — packed/installed tool and release-update consumer; Pack current 0.10.0, install it from exact local package via `dotnet tool install --tool-path`; run `dotnet hygiene` via appropriately isolated PATH; exercise upgrade/update paths | isolated Windows 11 + .NET 11 tool install; two locally packed package versions in controlled local NuGet feed | AC-01, AC-02, AC-03, AC-08, AC-16, AC-19; EC-01a, EC-01b, EC-08a, EC-08b, EC-16a, EC-16b, EC-19a, EC-19b | passed with gaps | exact 0.10.0 consumer, 0.10.1 update/revalidation, offline, incompatible-profile, and rollback paths passed |
| VAL-06 | Human — blocking milestone completion review; Explicit external reviewer decision, recorded under the milestone review artifact | PR and `.review/REV-M0018-COMPLETION.md` | REV-01 | pending | independent human review not recorded |

## Resume Point

Last completed work package: WP-03

Current work package: WP-01/WP-02 evidence closure

Next concrete action: add exact fixtures or mapped tests for the evidence gaps listed above, rerun validation, then obtain independent review

Known agent-resolvable gaps: successful dirty-worktree preservation; multiple-solution/orphan and generated-source discovery; allowlist/idempotence negative; baseline rejection; grouped-work and terminal-report cases; combined package provenance

External blockers or planning escalations: independent project-owner/delegate review is pending after implementation gaps are corrected

## Final Reconciliation

- [x] Freshly reread the milestone.
- [x] Verify exact obligation-ID set equality between milestone and ledger, without loss, merge or paraphrase.
- [x] Verify exact evidence-case-ID set equality and parent/wording identity.
- [x] Check obligations and cases against implementation/path-specific proof; mark unresolved paths partial/not demonstrated.
- [x] Confirm validation targets and execution loci including installed v0.10.0 consumer; record .NET 11 RC qualification.
- [x] Perform mandatory completion audit and write durable evidence. Result is not passing due to the listed unresolved evidence cases.
- [ ] Record explicit project-owner/delegate human review decision before claiming COMPLETE.
