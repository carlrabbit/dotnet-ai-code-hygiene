# M0018 — Autonomous Hygiene Lifecycle

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna (repository guide profile 0.9.1) |
| Baseline executor readiness | confirmed; no additional planning decisions required |
| Decision preservation | confirmed; operative workflow policy in `docs/specs/AUTONOMOUS-WORKFLOW.md` |
| Execution tractability | confirmed; planning-seeded obligation/evidence ledger; implementation owns work packages |
| Execution ledger | `.execution/M0018-autonomous-hygiene-lifecycle.md` |
| Scope size | large but one coherent normal user-to-agent lifecycle |
| Implementation autonomy | high within this contract |
| Documentation sync | directly contradicted authority + mandatory README; broad sync deferred |
| Focused validation | Tier 1 tests on actual repository Core/CLI fixtures |
| Repository validation | `.\eng\validate.ps1`; `git diff --check` |
| Integration validation | local Windows 11 + .NET 11 SDK, PowerShell, Git, actual dotnet format, SDK-style multi-project fixtures |
| Validation locus/platform | local Windows 11 required; no GitHub Actions or CI substitution |
| Environment authority | use existing SDK/Git/PowerShell/tool installation facilities; isolated disposable temp projects and user-scoped tool paths/local NuGet feed permitted; no host-wide install, admin elevation, remote runners, CI/workflow creation, package publication or unauthorized infrastructure change |
| Consumer/release validation | exact freshly packed version 0.10.0 installed to isolated consumer via supported `dotnet tool` mechanism |
| Human review | blocking, milestone-scoped approver/owner review |
| Baseline repository | `carlrabbit/dotnet-ai-code-hygiene` at `2e17b293be78480dcad9817f43c9bcc079a81ea8` |

## Goal

Convert the existing model-independent hygiene CLI into a trusted, autonomous agent-directed workflow. A user should be able to ask an implementation agent to run `dotnet hygiene` and follow its instructions. The tool chooses bootstrap/profile reconciliation, authoritative deterministic cleanup, source-change gate inspection and next concrete actions; the agent supplies semantic judgment and executes instructions. Routine operation does not require user knowledge of hygiene subcommands. Preserve an explicit baseline-approval boundary.

## Target State

A supported installed `dotnet-hygiene` .NET tool has a no-argument autonomous user entrypoint. First use initializes/cleans the full eligible repository and reports a baseline needing approval; subsequent use detects an appropriate PR/worktree gate, formats authoritatively, groups selected cross-rule review work for efficient agent handling, and reaches a truthful terminal outcome. Release updates and profile changes are handled in the same lifecycle through a trusted channel when available; manual diagnostics remain accessible. README is organized around the user's four workflows. No legacy command/interface compatibility or M0019 YOLO work is required.

## Scope

- Canonical `dotnet hygiene` .NET-tool packaging and command discovery; redesign CLI/help/JSON/state as necessary with no pre-1.0 compatibility burden.
- Autonomous state machine/stepwise agent instructions, bootstrap detection and profile reconciliation, gate determination, grouped selected work and completion reporting.
- Whole-repository authoritative deterministic formatting/normalization; nonmutating pre-hygiene `dotnet format` assessment; post-hygiene EditorConfig-managed policy and explicit dotnet-format diagnostic allowlists.
- Safe, reviewable baseline and material policy migration, external human-or-authorized-agent approval boundary, no hidden source reset.
- Trusted-feed release/update instructions or execution, safe restarts, offline/incompatible handling.
- Real CLI/Git/formatter fixtures, freshly packed installed-tool verification, and README/spec/engineering updates.

## Non-goals

- `--yolo-all`, unsampled inspection, unbounded output, resumable exhaustive review or changes to the existing per-rule selector (deferred M0019).
- New semantic rules, revised subjective question rubrics, LLM invocation or model evaluation/benchmarking.
- CI/workflow creation, public package publication, external provider integrations, project AGENTS.md injection, provider-specific skills, general DI/plugin/DSL/pipeline frameworks.
- Backward-compatible `hygiene` alias, JSON/state migration shims, support for obsolete historical sampling clocks or new OS/language support.
- Arbitrary `dotnet format analyzers` fixes outside selected hygiene rule policies; `dotnet format` has no custom `--profile` flag.

## Decisions and Constraints

`docs/specs/AUTONOMOUS-WORKFLOW.md` defines the comprehensive normative product/agent workflow, including user/agent/approver roles, whole-repository formatting, gate precedence, safety, selected-only grouped work, updates, terminal states and pre-1.0 incompatibility. Its decisions are deliberate project policy; implementation may choose local mechanics but must not weaken them. Preserve the current six independent rule definitions and M0016/M0017 meaning of semantic acceptance. The accepted observation is an explicit agent assertion, not software-generated proof. All deterministic rewrites must obey plan/validate/atomic rollback semantics even when run without per-edit approval. A missing dependency does not authorize machine-wide installation, GH workflows or shifting local Windows validation to CI; run permitted checks, record missing evidence and report BLOCKED when needed. A repo with dirty state cannot be cleaned or reset destructively to meet preconditions. A tool binary cannot reliably update itself in-place: execute/relaunch external .NET tool manager under known trusted scope, or issue concrete agent steps. No undeclared profile/CLI backward compatibility requirement applies before v1.0; preserve data and honest diagnostics.

## Baseline Executor Readiness

The execution model is project-local and model-independent. All material choices on authority, UX, compatibility, formatter boundaries, default gate, approval, upgrade, output and scope are resolved in this milestone and `docs/specs/AUTONOMOUS-WORKFLOW.md`. Implementation owns concrete class/file architecture, command parsing, Git detection mechanics, work-package decomposition and test design. Local Windows/.NET 11 is the authoritative validation locus; absence of that capability is a documented BLOCKED outcome, not permission to synthesize CI.

## Execution Tractability

Every independently provable obligation has an immutable ID. The planner-seeded `.execution` ledger mirrors all obligation and distinct evidence-case texts exactly and identifies validation gates; the implementer supplies work packages, concrete evidence and reconciliation. Do not collapse distinct CLI, Git, profile, formatting, authorization, or installed-package paths into aggregate test pass claims. Existing `eng/validate.ps1` is authoritative; extend its tests and isolated consumer rather than adding workflow infrastructure.

## Required Authority

- `AGENTS.md` and `docs/ENGINEERING.md` (repository routing, platform, validation, mutation)
- `docs/ARCHITECTURE.md` and `docs/specs/HYGIENE.md` (rule/engine architecture)
- `docs/specs/AUTONOMOUS-WORKFLOW.md` (new resolved workflow and pre-1.0 policy)
- `docs/specs/PROFILE.md`, `docs/specs/REWRITES.md` (profile and formatter ownership)
- `docs/specs/SAMPLING.md`, `docs/specs/SEMANTIC-REVIEWS.md`, `docs/specs/REVIEW-HANDOFFS.md` (review selection and acceptance)
- `docs/specs/AGENT-HELP.md`, `docs/SPECS.md` and `README.md` (agent/public interface)
- `docs/MILESTONES.md` and `docs/TERMINOLOGY.md` (index and vocabulary)

## Acceptance Criteria

- **AC-01** — Ship the .NET tool with the canonical `dotnet-hygiene` shim so `dotnet hygiene` works from an isolated installed package; remove the legacy bare `hygiene` public command without aliases, and expose correct version/help.
- **AC-02** — The no-argument `dotnet hygiene` entrypoint emits and drives an agent-executable, state-aware lifecycle without requiring the caller to know bootstrap, update, rule batches or acceptance commands; the CLI never calls an AI model/provider.
- **AC-03** — Automatically recognize a missing/uninitialized profile and initiate full-repository bootstrap; recognize current versus outdated/invalid profile state and produce the appropriate update or safe incompatibility action without silently replacing arbitrary user files.
- **AC-04** — Bootstrap covers all eligible supported SDK-style C# projects across the repository, including repositories with multiple solutions and projects not included in a preferred solution; honor generated/unsupported source exclusions and preserve unrelated data.
- **AC-05** — Before adopting the hygiene policy, run a nonmutating assessment of existing effective project formatting and report actual pre-hygiene drift; do not claim to infer whether `dotnet format` historically ran.
- **AC-06** — After adopting the profile, apply all enabled authoritative deterministic formatting/normalization to eligible repository sources; restrict dotnet-format-based code fixes to hygiene-owned policies/explicit diagnostic allowlists, never generic analyzer fixes, and enforce repeat-run idempotence.
- **AC-07** — Hygiene-owned EditorConfig and MSBuild policy is reconciled without clobbering unrelated sections; distinguish the post-hygiene effective policy from the prior project formatting baseline, and report changed paths and attribution limits truthfully.
- **AC-08** — Bootstrap and material policy migrations produce a reviewable baseline report with exact tool/profile/rule versions, changed paths, pre/post format observations, remaining conflicts and validation results; baseline status remains approval-required until explicitly accepted by an authorized external approver.
- **AC-09** — Repository-wide mutation is safe: plan/validate changes, preserve preexisting staged/unstaged/untracked work, avoid destructive reset, and ensure failures, cancellations and rejection cannot silently discard user data or claim a partially applied baseline complete.
- **AC-10** — For an initialized repository, infer a gate using explicit/trusted context before unambiguous local feature-branch merge-base or actual worktree changes; ambiguous or missing comparison context yields actionable agent instructions, never an accidental zero-review bare check or invented remote.
- **AC-11** — Ordinary gates perform authoritative deterministic remediation and preserve the existing M0016 six-rule independent bounded gate-local semantic-review selection contracts without adding cross-rule selection coupling or a global budget.
- **AC-12** — Generate a selected-only, consolidated, grouped agent work order with the exact original rule, questions, escalation policy, batch/item handles, current source identity/context and deduplicated files; grouping does not merge independent rule judgments or acceptances.
- **AC-13** — The agent workflow explicitly directs inspection, contextual remediation, positive-only acceptance, escalation for negative/uncertain items, stale-ticket revalidation, reruns after modifications and no false clean-state claims.
- **AC-14** — Publish truthful terminal outcomes complete, approval-required, escalation-required and incomplete, with structured/text reporting and coherent stdout/stderr/exit semantics that distinguish command execution success from gate completion.
- **AC-15** — Retain practical nonmutating manual diagnostics for paths, `--base`, `--changed`, `--review-all`, findings, rules, explain and review lifecycle in the redesigned `dotnet hygiene` hierarchy; keep `--review-all` bounded and do not implement `--yolo-all` in M0018.
- **AC-16** — When a configured trusted package source/channel is available, detect and perform or direct safe out-of-process tool upgrades, restart/revalidate the installed tool, reconcile supported profile state and request approval of materially changed policy; unavailable feed/offline/incompatible states are reported accurately without unauthorized host/CI changes.
- **AC-17** — The pre-1.0 contract has no backward-compatibility requirements for legacy command names, CLI shapes, JSON schemas, persisted review/profile/sampling formats or internal APIs; obsolete state fails safely or is replaced only through explicitly safe policy, with no silent loss of repository data.
- **AC-18** — Normal operation needs no project-specific AGENTS.md insertion, no separate agent skill, no model-provider integration, no generalized rule DSL, and no generic plugin/pipeline architecture; use the existing independent rule modules and shared project facts.
- **AC-19** — A freshly built version 0.10.0 tool package installed into an isolated Windows 11/.NET 11 consumer executes the `dotnet hygiene` command and proves bootstrap, pre/post restricted formatting, normal gates, manual diagnostics, lifecycle reports and trusted-source update behavior rather than relying on an old shim or development binary.
- **AC-20** — Software validation demonstrates CLI orchestration, safe rewrites, rule selection, work-order fidelity, identity/revalidation and report semantics, without assigning gold-label subjective judgments or claiming that sampling certifies population quality.

## Acceptance Evidence Topology

| ID | Parent obligation | Required evidence case | Why separate evidence is required |
|---|---|---|---|
| EC-01a | AC-01 | Fresh isolated installed `dotnet hygiene` command resolves current artifact and `--version`/help accurately | Package/shim path differs from source process |
| EC-01b | AC-01 | Legacy bare `hygiene` is not distributed as supported shim; no fallback alias | Negative/distribution surface differs |
| EC-03a | AC-03 | Missing profile bootstraps and supported drift reconciles | Two lifecycle state paths |
| EC-03b | AC-03 | Malformed/unsupported profile or conflicting managed artifacts do not silently overwrite user data | Incompatible/error path differs |
| EC-04a | AC-04 | Multiple solutions and orphaned eligible SDK projects are covered | Solution discovery versus project membership |
| EC-04b | AC-04 | Generated and unsupported files stay excluded without suppressing eligible sources | Exclusion discovery path |
| EC-05a | AC-05 | Original effective formatting assessment runs without source edits | Before-profile external format observation |
| EC-06a | AC-06 | Hygiene-approved whitespace/layout and selected style fixes applied repository-wide | Different formatter/codefix mechanisms |
| EC-06b | AC-06 | Non-allowlisted analyzer fixes never run and second pass is a no-op | Negative whitelist and idempotence |
| EC-08a | AC-08 | Bootstrap report remains pending before approver decision; externally approved baseline becomes active | Approval lifecycle positive path |
| EC-08b | AC-08 | Rejected/materially changed baseline is not reported complete | Negative approval/policy-change path |
| EC-09a | AC-09 | Existing dirty/staged/untracked input is preserved under successful setup | User-work collision safety path |
| EC-09b | AC-09 | Fault/cancel/conflict does not leave corrupt or falsely accepted baseline | Atomic rollback/failure path |
| EC-10a | AC-10 | Unambiguous feature branch with committed changes selects PR-base gate | Committed feature vs default branch |
| EC-10b | AC-10 | Actual worktree change selects worktree gate when no branch comparison applies | Uncommitted gate admission |
| EC-10c | AC-10 | Missing/ambiguous upstream/base yields actionable incomplete request and no empty success | Ambiguous branch negative path |
| EC-12a | AC-12 | Same-file different-rule subjects share supplied context but retain both exact original identities and questions | Cross-rule grouping fidelity |
| EC-12b | AC-12 | Separate-source subjects and zero-work rules preserve ordering/identity without phantom work | Empty/multi-file boundary |
| EC-13a | AC-13 | Successful explicit per-rule acceptance after judgment; repeated gate no refill | Positive persistence path |
| EC-13b | AC-13 | Edited source rejects stale acceptance; uncertain/negative work remains outstanding/escalated | Invalidation/escalation path |
| EC-14a | AC-14 | Complete report names processed gate and exact verification evidence | Terminal success |
| EC-14b | AC-14 | Approval, escalation, validation failure and unresolved work remain distinct non-complete outcomes | Different failure/decision outcomes |
| EC-16a | AC-16 | Configured trusted feed updates using isolated packed versions and verifies new shim/profile behavior | Available release/update path |
| EC-16b | AC-16 | Offline/unconfigured feed and incompatible binary/profile report honest next action; no unauthorized provisioning | Unavailable/incompatible negative path |
| EC-19a | AC-19 | Fresh 0.10.0 packaged tool installed and invoked in isolated consumer | Real consumer/distribution boundary |
| EC-19b | AC-19 | Original configuration and post-hygiene formatting verified with actual formatter process and bounded diagnostics | External dotnet-format and in-product rewrite boundary |

## Validation

All validation is evidence for **mechanical tool behavior** and external interface contracts, not subjective AI judgments. Do not create GitHub Actions or substitute CI for declared local Windows validation. The actual package installation must exercise the current build rather than a stale shim. Validation may be executed in bounded shards if and only if the repository's actual command supports it, with verified receipts. Where requirements are not exercisable, record missing evidence explicitly; never infer a pass from nearby tests.

| ID | Depth/target | Locus/platform | Command/check | Proves | Expected evidence |
|---|---|---|---|---|---|
| VAL-01 | Tier 1 — focused lifecycle/formatter/orchestration Core tests | local Windows 11 + .NET 11 SDK; SDK-style temp repositories | Focused tests through existing Core test project, invoked with `dotnet test` (TUnit/MTP compatible) | AC-02, AC-07, AC-11, AC-17, AC-18, AC-20; EC-03a, EC-03b, EC-04a, EC-04b, EC-05a, EC-06a, EC-06b, EC-08a, EC-08b, EC-09a, EC-09b, EC-12a, EC-12b, EC-13a, EC-13b | Separate path-specific fixtures, no subjective semantic verdict expectations |
| VAL-02 | Tier 3 — real CLI/Git gate and status end-to-end tests | local Windows 11 + .NET 11 SDK + Git + PowerShell; isolated Git fixtures | Built CLI process tests and actual Git branch/worktree transitions through canonical test harness | AC-10, AC-13, AC-14, AC-15; EC-10a, EC-10b, EC-10c, EC-14a, EC-14b | Machine-readable/text output, exit codes, concrete base/changed cases, external approvals and stale cases |
| VAL-03 | Tier 3 — external dotnet-format policy restriction and full-repo bootstrap | local Windows 11 + .NET 11 SDK; multiple solution/project real fixtures; installed dotnet format | Non-mutating pre-format, post-profile allowlisted format/style, actual project compilation and independent rerun | AC-04, AC-05, AC-06, AC-07; EC-04a, EC-04b, EC-05a, EC-06a, EC-06b, EC-19b | Hash/diff before and after, exact allowlist, proof other analyzer codefix is not applied, idempotence |
| VAL-04 | Tier 2 — canonical repository validation and documentation reconciliation | local Windows 11 + .NET 11 SDK + PowerShell + Git | `.\eng\validate.ps1`; `git diff --check`; verify README examples and exact ledger IDs | AC-17, AC-18, AC-20, DOC-01, DOC-02 | Full build/tests and repository checks; explicit coverage/authority audit; avoid CI substitutions |
| VAL-05 | Tier 4 — packed/installed tool and release-update consumer | isolated Windows 11 + .NET 11 tool install; two locally packed package versions in controlled local NuGet feed | Pack current 0.10.0, install it from exact local package via `dotnet tool install --tool-path`; run `dotnet hygiene` via appropriately isolated PATH; exercise upgrade/update paths | AC-01, AC-02, AC-03, AC-08, AC-16, AC-19; EC-01a, EC-01b, EC-08a, EC-08b, EC-16a, EC-16b, EC-19a, EC-19b | Package/version/SHA proof, installed shim process receipts, feed and offline cases; no stale/global executable |
| VAL-06 | Human — blocking milestone completion review | PR and `.review/REV-M0018-COMPLETION.md` | Explicit external reviewer decision, recorded under the milestone review artifact | REV-01 | Dated scope/safety/coverage/provenance approval or requested corrections; never fabricate |

## Human Review

Applicability: required, blocking, milestone-scoped. Review class: project owner/authorized delegated approver. Canonical review ID: `REV-M0018-COMPLETION`. Review subject: a reproducible ordinary autonomy workflow, correct repository-wide authoritative cleanup, approval boundaries, preserve-user-data safety, dependable rule/work-order identity, truthful completion, source-controlled update provenance and exact installed-tool evidence. The approver may be human or an AI explicitly delegated authority; the implementer cannot approve its own milestone. Prepare `.review/REV-M0018-COMPLETION.md` with no fabricated decision. Use the documented repository-local milestone review check if it exists; do not invent one. A pending human decision produces `AWAITING HUMAN REVIEW` rather than `COMPLETE`.

## Documentation Policy

The README's four user-facing lifecycle sections and the pre-1.0 compatibility policy are **direct milestone deliverables**. Update operative authority where it would otherwise contradict implemented commands/protocols. Keep `docs/specs/AUTONOMOUS-WORKFLOW.md` aligned with implementation. Preserve historical completed milestone evidence; defer unrelated documentation synchronization.

## Additional Completion Obligations

- **DOC-01** — Replace the README onboarding structure with First Bootstrap, Run on Gates, Manual Inspection and Debugging, and Updating to a New Release, plus correctly labeled deferred YOLO usage and real copy-pasteable `dotnet hygiene` examples tested against the installed artifact.
- **DOC-02** — Update operative engineering, profile, rewrite, semantic review, agent-help, package/distribution and milestone index authority to reflect actual M0018 behavior and record the explicit pre-1.0 no-compatibility policy; leave completed historical milestone evidence intact.
- **REV-01** — Prepare `.review/REV-M0018-COMPLETION.md` for blocking project-owner/delegated-approver review of the autonomous workflow, baseline approval boundary, mutation safety, genuine installed-tool provenance, evidence-case closure and scope fidelity; do not invent approval.

## Completion Expectations

Read this ready milestone, its seeded ledger, required authority and live repository; decompose into bounded work packages; map every obligation and evidence case to executable proof; implement; validate at declared loci; reread contract and perform exact obligation/case set reconciliation; complete the mandatory audit; record compact criterion-specific durable evidence below only once proven. Tests passing without installed consumer and human review do not complete this milestone. A missing Windows SDK or package-source authorization is a genuine blocker, not permission to alter host infrastructure.

## Completion Evidence

Completion audit was performed on 2026-10-17 against the final milestone and ledger. AC-01–AC-20, DOC-01/02, REV-01, and all 26 EC entries remain separately represented in the ledger; no IDs were removed or merged. Review of the implementation and path-specific evidence found the following gaps, so this audit is **not passing** and the milestone is not complete:

- AC-09 / EC-09b: per-file/profile transactions and existing cancellation/conflict tests exist, but external `dotnet-format` mutates projects sequentially without whole-run rollback. Failure after an earlier project changed can leave a partial format baseline.
- AC-16 / EC-16a/b: `dotnet tool update` from an isolated controlled local feed to the newly packed 0.10.0 package was exercised by the canonical validator, including shim/version verification. The no-argument lifecycle does not discover trusted-feed releases or restart/revalidate itself; offline and incompatible update outcomes have not been exercised. The package-manager update result does not satisfy autonomous release detection.
- EC-03b, EC-04a/b, EC-06b, EC-08b, EC-09a/b, EC-16b and other unproven cases are marked partial or not demonstrated in `.execution/M0018-autonomous-hygiene-lifecycle.md`; no aggregate pass is inferred for them.
- VAL-01/02/04/05: `eng/validate.ps1` completed on Windows 11 with .NET SDK `11.0.100-rc.1.26425.128`; build passed, tests passed 118/118 (94 Core, 24 CLI), the exact freshly packed 0.10.0 isolated consumer passed, and the repository M0006 self-host fixture passed. The final script exercised isolated 0.9.0-to-0.10.0 package-manager update. .NET 11 was an RC in the available environment.
- `git diff --check` passed. PowerShell parser validation for `eng/validate.ps1` passed. The review request is prepared with decision pending; no approval has been recorded.

Exact reconciliation: milestone acceptance IDs `{AC-01…AC-20}` plus `{DOC-01,DOC-02,REV-01}` match the ledger obligation registry; the milestone's 26 evidence IDs match the ledger evidence registry with the same parent and required-case text. The mandatory audit identified the implementation gaps above. Final milestone status: **BLOCKED** pending correction and rerun of missing safety/update evidence, followed by required independent human review.

## Escalation Boundary

Implementation owns local mechanics, files, test structure, work-package and shard decisions. Return to planning for any new material decision about autonomy/approval, package/update trust, gate semantics, formatting authority, validation locus, rule selection, retained-data safety, scope or human review. Do not expand scope into M0019's exhaustive operation.
