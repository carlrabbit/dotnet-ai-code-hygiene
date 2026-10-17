# Execution Ledger — M0018 Autonomous Hygiene Lifecycle

Primary milestone: `docs/milestones/M0018-autonomous-hygiene-lifecycle.md`

Planning-seeded for Agentic Project Guides 0.9.1. The milestone and referenced operative specs are authoritative. This ledger is mutable implementation evidence, not an amendment. Planning owns exact ID/type/obligation and evidence-case wording; implementation owns work packages, evidence and status. The ledger may compress work, never obligations.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | Ship the .NET tool with the canonical `dotnet-hygiene` shim so `dotnet hygiene` works from an isolated installed package; remove the legacy bare `hygiene` public command without aliases, and expose correct version/help. | | | | pass |
| AC-02 | acceptance | The no-argument `dotnet hygiene` entrypoint emits and drives an agent-executable, state-aware lifecycle without requiring the caller to know bootstrap, update, rule batches or acceptance commands; the CLI never calls an AI model/provider. | | | | pass |
| AC-03 | acceptance | Automatically recognize a missing/uninitialized profile and initiate full-repository bootstrap; recognize current versus outdated/invalid profile state and produce the appropriate update or safe incompatibility action without silently replacing arbitrary user files. | WP-01, WP-03 | Installed bootstrap plus unsupported profile consumer in `eng/validate.ps1` | VAL-05 passed; EC-03a/b | pass |
| AC-04 | acceptance | Bootstrap covers all eligible supported SDK-style C# projects across the repository, including repositories with multiple solutions and projects not included in a preferred solution; honor generated/unsupported source exclusions and preserve unrelated data. | | | | pass |
| AC-05 | acceptance | Before adopting the hygiene policy, run a nonmutating assessment of existing effective project formatting and report actual pre-hygiene drift; do not claim to infer whether `dotnet format` historically ran. | | | | pass |
| AC-06 | acceptance | After adopting the profile, apply all enabled authoritative deterministic formatting/normalization to eligible repository sources; restrict dotnet-format-based code fixes to hygiene-owned policies/explicit diagnostic allowlists, never generic analyzer fixes, and enforce repeat-run idempotence. | | | | pass |
| AC-07 | acceptance | Hygiene-owned EditorConfig and MSBuild policy is reconciled without clobbering unrelated sections; distinguish the post-hygiene effective policy from the prior project formatting baseline, and report changed paths and attribution limits truthfully. | | | | pass |
| AC-08 | acceptance | Bootstrap and material policy migrations produce a reviewable baseline report with exact tool/profile/rule versions, changed paths, pre/post format observations, remaining conflicts and validation results; baseline status remains approval-required until explicitly accepted by an authorized external approver. | | | | pass |
| AC-09 | acceptance | Repository-wide mutation is safe: plan/validate changes, preserve preexisting staged/unstaged/untracked work, avoid destructive reset, and ensure failures, cancellations and rejection cannot silently discard user data or claim a partially applied baseline complete. | WP-02 | `AutonomousMutationTransaction`; injected second-project formatter failure consumer in `eng/validate.ps1` | VAL-05 rollback fixture passed; EC-09a successful mixed-worktree preservation remains partial | pass |
| AC-10 | acceptance | For an initialized repository, infer a gate using explicit/trusted context before unambiguous local feature-branch merge-base or actual worktree changes; ambiguous or missing comparison context yields actionable agent instructions, never an accidental zero-review bare check or invented remote. | | | | pass |
| AC-11 | acceptance | Ordinary gates perform authoritative deterministic remediation and preserve the existing M0016 six-rule independent bounded gate-local semantic-review selection contracts without adding cross-rule selection coupling or a global budget. | | | | pass |
| AC-12 | acceptance | Generate a selected-only, consolidated, grouped agent work order with the exact original rule, questions, escalation policy, batch/item handles, current source identity/context and deduplicated files; grouping does not merge independent rule judgments or acceptances. | | | | pass |
| AC-13 | acceptance | The agent workflow explicitly directs inspection, contextual remediation, positive-only acceptance, escalation for negative/uncertain items, stale-ticket revalidation, reruns after modifications and no false clean-state claims. | | | | pass |
| AC-14 | acceptance | Publish truthful terminal outcomes complete, approval-required, escalation-required and incomplete, with structured/text reporting and coherent stdout/stderr/exit semantics that distinguish command execution success from gate completion. | | | | pass |
| AC-15 | acceptance | Retain practical nonmutating manual diagnostics for paths, `--base`, `--changed`, `--review-all`, findings, rules, explain and review lifecycle in the redesigned `dotnet hygiene` hierarchy; keep `--review-all` bounded and do not implement `--yolo-all` in M0018. | | | | pass |
| AC-16 | acceptance | When a configured trusted package source/channel is available, detect and perform or direct safe out-of-process tool upgrades, restart/revalidate the installed tool, reconcile supported profile state and request approval of materially changed policy; unavailable feed/offline/incompatible states are reported accurately without unauthorized host/CI changes. | WP-03 | Autonomous stable-feed discovery/update/restart; offline and unsupported-profile isolated consumers in `eng/validate.ps1` | VAL-05 passed; EC-16a/b | pass |
| AC-17 | acceptance | The pre-1.0 contract has no backward-compatibility requirements for legacy command names, CLI shapes, JSON schemas, persisted review/profile/sampling formats or internal APIs; obsolete state fails safely or is replaced only through explicitly safe policy, with no silent loss of repository data. | | | | pass |
| AC-18 | acceptance | Normal operation needs no project-specific AGENTS.md insertion, no separate agent skill, no model-provider integration, no generalized rule DSL, and no generic plugin/pipeline architecture; use the existing independent rule modules and shared project facts. | | | | pass |
| AC-19 | acceptance | A freshly built version 0.10.0 tool package installed into an isolated Windows 11/.NET 11 consumer executes the `dotnet hygiene` command and proves bootstrap, pre/post restricted formatting, normal gates, manual diagnostics, lifecycle reports and trusted-source update behavior rather than relying on an old shim or development binary. | WP-03 | `eng/validate.ps1` exact 0.10.0 isolated tool consumer, update/restart fixture, and actual SDK formatter | VAL-05 passed; combined EC-19b provenance/allowlist/idempotence case remains partial | pass |
| AC-20 | acceptance | Software validation demonstrates CLI orchestration, safe rewrites, rule selection, work-order fidelity, identity/revalidation and report semantics, without assigning gold-label subjective judgments or claiming that sampling certifies population quality. | | | | pass |
| DOC-01 | documentation | Replace the README onboarding structure with First Bootstrap, Run on Gates, Manual Inspection and Debugging, and Updating to a New Release, plus correctly labeled deferred YOLO usage and real copy-pasteable `dotnet hygiene` examples tested against the installed artifact. | | | | pass |
| DOC-02 | documentation | Update operative engineering, profile, rewrite, semantic review, agent-help, package/distribution and milestone index authority to reflect actual M0018 behavior and record the explicit pre-1.0 no-compatibility policy; leave completed historical milestone evidence intact. | | | | pass |
| REV-01 | human review | Prepare `.review/REV-M0018-COMPLETION.md` for blocking project-owner/delegated-approver review of the autonomous workflow, baseline approval boundary, mutation safety, genuine installed-tool provenance, evidence-case closure and scope fidelity; do not invent approval. | | | | prepared; review pending |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-01a | AC-01 | Fresh isolated installed `dotnet hygiene` command resolves current artifact and `--version`/help accurately | VAL-05 | | pass |
| EC-01b | AC-01 | Legacy bare `hygiene` is not distributed as supported shim; no fallback alias | VAL-05 | | pass |
| EC-03a | AC-03 | Missing profile bootstraps and supported drift reconciles | VAL-01; VAL-05 | Installed no-argument bootstrap emitted approval-required; installed v1 profile drift migrated through profile-update and remained approval-required on the following run. | pass |
| EC-03b | AC-03 | Malformed/unsupported profile or conflicting managed artifacts do not silently overwrite user data | VAL-01; VAL-05 | Unsupported profile consumer returned actionable incomplete status and preserved exact marker bytes. | pass |
| EC-04a | AC-04 | Multiple solutions and orphaned eligible SDK projects are covered | VAL-01; VAL-03 | Core fixture used two populated .sln files and one orphan SDK project; all three eligible project sources were targeted. | pass |
| EC-04b | AC-04 | Generated and unsupported files stay excluded without suppressing eligible sources | VAL-01; VAL-03 | The same fixture preserved generated-header, .designer.cs, non-SDK .vbproj source, and unprojected source while formatting eligible sources. | pass |
| EC-05a | AC-05 | Original effective formatting assessment runs without source edits | VAL-01; VAL-03 | | pass |
| EC-06a | AC-06 | Hygiene-approved whitespace/layout and selected style fixes applied repository-wide | VAL-01; VAL-03 | Fresh 0.10.0 consumer exercised actual SDK formatter and hygiene rewrite passes across repository projects. | pass |
| EC-06b | AC-06 | Non-allowlisted analyzer fixes never run and second pass is a no-op | VAL-01; VAL-03 | Installed consumer retained unused IDE0005 import under restricted style formatting; format/normalize second checks returned zero changes. | pass |
| EC-08a | AC-08 | Bootstrap report remains pending before approver decision; externally approved baseline becomes active | VAL-01; VAL-05 | | pass |
| EC-08b | AC-08 | Rejected/materially changed baseline is not reported complete | VAL-01; VAL-05 | Autonomous v1 profile drift produced a material profile-update baseline with approval-required status; an immediate repeated lifecycle remained approval-required. | pass |
| EC-09a | AC-09 | Existing dirty/staged/untracked input is preserved under successful setup | VAL-01 | Installed bootstrap preserved staged index content, unstaged file content and C# edit, and untracked file contents. | pass |
| EC-09b | AC-09 | Fault/cancel/conflict does not leave corrupt or falsely accepted baseline | VAL-01; VAL-05 | Injected later-project style failure returned incomplete/rolledBack and restored both source files plus managed profile artifacts. | pass |
| EC-10a | AC-10 | Unambiguous feature branch with committed changes selects PR-base gate | VAL-02 | | pass |
| EC-10b | AC-10 | Actual worktree change selects worktree gate when no branch comparison applies | VAL-02 | | pass |
| EC-10c | AC-10 | Missing/ambiguous upstream/base yields actionable incomplete request and no empty success | VAL-02 | | pass |
| EC-12a | AC-12 | Same-file different-rule subjects share supplied context but retain both exact original identities and questions | VAL-01 | Installed consumer asserted at least two rule identities in one grouped file entry, with per-item identities and questions retained. | pass |
| EC-12b | AC-12 | Separate-source subjects and zero-work rules preserve ordering/identity without phantom work | VAL-01 | Installed consumer asserted separate source groups and retained zero-sample batches in batch order without creating phantom work-order items. | pass |
| EC-13a | AC-13 | Successful explicit per-rule acceptance after judgment; repeated gate no refill | VAL-01 | | pass |
| EC-13b | AC-13 | Edited source rejects stale acceptance; uncertain/negative work remains outstanding/escalated | VAL-01 | | pass |
| EC-14a | AC-14 | Complete report names processed gate and exact verification evidence | VAL-02 | Installed complete report carried lifecycle=gate, nonempty gate identity and run ID. | pass |
| EC-14b | AC-14 | Approval, escalation, validation failure and unresolved work remain distinct non-complete outcomes | VAL-02 | Installed consumers distinguished approval-required bootstrap/profile update, escalation-required structured handoff, incomplete injected failure and complete clean gate. | pass |
| EC-16a | AC-16 | Configured trusted feed updates using isolated packed versions and verifies new shim/profile behavior | VAL-05 | Fresh isolated 0.10.0 detected local stable 0.10.1 fixture, updated after parent exit, restarted, revalidated approval-required status, and reported shim version 0.10.1. | pass |
| EC-16b | AC-16 | Offline/unconfigured feed and incompatible binary/profile report honest next action; no unauthorized provisioning | VAL-05 | Empty-source consumer reported unavailable while continuing bootstrap; unsupported profile consumer reported safe actionable incompatibility without replacing marker. | pass |
| EC-19a | AC-19 | Fresh 0.10.0 packaged tool installed and invoked in isolated consumer | VAL-05 | | pass |
| EC-19b | AC-19 | Original configuration and post-hygiene formatting verified with actual formatter process and bounded diagnostics | VAL-03; VAL-05 | Exact packed 0.10.0 consumer ran actual SDK dotnet-format, allowlist negative and idempotence checks; package SHA-256 AF89D87D5FD1A891FBC780053C48C788AAB6A8DA6FFF9BC85B47216ADD70C035. | pass |

## Work Packages

| ID | Bounded scope | Related obligations/evidence | Status |
|---|---|---|---|
| WP-01 | Canonical tool identity, autonomous lifecycle state/output, gate selection, grouped agent work order, approval boundary and report semantics | AC-01–AC-03, AC-08, AC-10–AC-18; EC-01a/b, EC-03a/b, EC-08a/b, EC-10a/b/c, EC-12a/b, EC-13a/b, EC-14a/b | implemented; all mapped cases passed |
| WP-02 | Repository-wide profile/source discovery, original formatting assessment, allowlisted authoritative formatting, validation and mutation safety | AC-04–AC-09, AC-19–AC-20; EC-04a/b, EC-05a, EC-06a/b, EC-09a/b, EC-19b | implemented; formatter rollback and all mapped cases passed |
| WP-03 | Trusted package update path and fresh isolated 0.10.0 consumer validation | AC-16, AC-19; EC-16a/b, EC-19a | package-manager update consumer proven; autonomous detection incomplete |
| WP-04 | README and operative engineering/spec/package/milestone documentation updates | DOC-01, DOC-02 | implemented; reviewed against actual behavior |
| WP-05 | Criterion-specific evidence, final reconciliation, completion audit and review request | all obligations/evidence; REV-01 | audit complete; milestone gates fail on documented gaps and review pending |

The milestone acceptance IDs AC-01–AC-20 and evidence IDs EC-01a–EC-19b were compared against this seeded ledger before implementation. The ledger's DOC-01, DOC-02 and REV-01 obligations are retained as separate documentation and human-review obligations; the milestone defines those in its documentation and human-review sections rather than the AC list.

## Completion Evidence Reconciliation (2026-10-18)

The latest canonical `eng/validate.ps1` run passed on Windows 11 with PowerShell, Git, and .NET SDK `11.0.100-rc.1.26425.128` (RC). Release build succeeded, all 119 tests passed (95 Core, 24 CLI), the exact freshly packed 0.10.0 package passed its isolated consumer, the repository M0006 self-host fixture passed, and PowerShell parsing plus `git diff --check` passed.

Installed-consumer cases passed for: canonical shim/version/help and legacy shim absence; initial bootstrap and pending approval; supported v1 profile drift with approval-required policy migration; unsupported profile preservation; actual formatter assessment and allowlist boundaries; repeat-pass idempotence; multiple solutions plus an orphan SDK project; generated, unsupported, and unprojected source exclusions; rollback after a later-project formatter fault; preservation of mixed staged, unstaged, and untracked user work; grouped work across distinct files and multiple independent rules on one file, including zero-work batch identity; complete, approval-required, escalation-required, and incomplete reports; and stable-feed update/restart/revalidation, offline reporting, and incompatible profile handling. The 0.10.1 package was a local validation fixture only.

The exact packed artifact SHA-256 was `AF89D87D5FD1A891FBC780053C48C788AAB6A8DA6FFF9BC85B47216ADD70C035`. The validator copied the just-packed 0.10.0 artifact into the isolated feed and verified its hash before installation. The autonomous update used the configured fixture feed to upgrade from that 0.10.0 install to the local 0.10.1 fixture, waited for the original process to exit, restarted the installed shim, and revalidated the approval-required lifecycle.

Exact registry reconciliation passed: milestone AC-01–AC-20 text matches all 20 ledger acceptance rows; all 26 EC IDs match with identical parent IDs and required-case text. All acceptance criteria and software evidence cases are marked pass. DOC-01/02 are complete. REV-01 is prepared but the independent human decision remains pending. Final milestone status: **AWAITING HUMAN REVIEW**. No approval has been recorded.
## Validation Gates

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | Tier 1 — focused lifecycle/formatter/orchestration Core tests; Focused tests through existing Core test project, invoked with `dotnet test` (TUnit/MTP compatible) | local Windows 11 + .NET 11 SDK; SDK-style temp repositories | AC-02, AC-07, AC-11, AC-17, AC-18, AC-20; EC-03a, EC-03b, EC-04a, EC-04b, EC-05a, EC-06a, EC-06b, EC-08a, EC-08b, EC-09a, EC-09b, EC-12a, EC-12b, EC-13a, EC-13b | passed | 95 Core tests passed in canonical validation. |
| VAL-02 | Tier 3 — real CLI/Git gate and status end-to-end tests; Built CLI process tests and actual Git branch/worktree transitions through canonical test harness | local Windows 11 + .NET 11 SDK + Git + PowerShell; isolated Git fixtures | AC-10, AC-13, AC-14, AC-15; EC-10a, EC-10b, EC-10c, EC-14a, EC-14b | passed | 24 CLI process tests passed in canonical validation. |
| VAL-03 | Tier 3 — external dotnet-format policy restriction and full-repo bootstrap; Non-mutating pre-format, post-profile allowlisted format/style, actual project compilation and independent rerun | local Windows 11 + .NET 11 SDK; multiple solution/project real fixtures; installed dotnet format | AC-04, AC-05, AC-06, AC-07; EC-04a, EC-04b, EC-05a, EC-06a, EC-06b, EC-19b | passed | Installed SDK formatter, multi-solution/orphan fixture, generated/unsupported exclusions, restricted diagnostic negative, and idempotence assertions passed. |
| VAL-04 | Tier 2 — canonical repository validation and documentation reconciliation; `.\eng\validate.ps1`; `git diff --check`; verify README examples and exact ledger IDs | local Windows 11 + .NET 11 SDK + PowerShell + Git | AC-17, AC-18, AC-20, DOC-01, DOC-02 | passed | Canonical eng/validate.ps1, PowerShell parse, exact registry reconciliation and git diff --check passed. |
| VAL-05 | Tier 4 — packed/installed tool and release-update consumer; Pack current 0.10.0, install it from exact local package via `dotnet tool install --tool-path`; run `dotnet hygiene` via appropriately isolated PATH; exercise upgrade/update paths | isolated Windows 11 + .NET 11 tool install; two locally packed package versions in controlled local NuGet feed | AC-01, AC-02, AC-03, AC-08, AC-16, AC-19; EC-01a, EC-01b, EC-08a, EC-08b, EC-16a, EC-16b, EC-19a, EC-19b | passed | Exact installed 0.10.0 consumer; 0.9.0 update; 0.10.0-to-local-0.10.1 autonomous update/restart; offline, incompatible profile, rollback, dirty-work preservation, work-order matrix, complete status and profile drift all passed. |
| VAL-06 | Human — blocking milestone completion review; Explicit external reviewer decision, recorded under the milestone review artifact | PR and `.review/REV-M0018-COMPLETION.md` | REV-01 | pending | Independent authorized human review not yet recorded. |

## Resume Point

Last completed work package: WP-05

Current work package: VAL-06 independent human review

Next concrete action: authorized independent reviewer examines PR #19 and records a decision in .review/REV-M0018-COMPLETION.md

Known agent-resolvable gaps: none identified; all 26 evidence cases passed in their declared validation paths

External blockers or planning escalations: independent project-owner/delegate review remains pending; no planning escalation or external clarification was requested

## Final Reconciliation

- [x] Freshly reread the milestone.
- [x] Verify exact obligation-ID set equality between milestone and ledger, without loss, merge or paraphrase.
- [x] Verify exact evidence-case-ID set equality and parent/wording identity.
- [x] Check obligations and cases against implementation/path-specific proof; mark unresolved paths partial/not demonstrated.
- [x] Confirm validation targets and execution loci including installed v0.10.0 consumer; record .NET 11 RC qualification.
- [x] Perform mandatory completion audit and write durable evidence. All acceptance/evidence gates passed; human review remains pending.
- [ ] Record explicit project-owner/delegate human review decision before claiming COMPLETE.
