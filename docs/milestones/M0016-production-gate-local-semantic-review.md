# M0016 — Production Gate-Local Semantic Review Sampling

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready — implementation, no research prerequisite |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna (guide system 0.9.1) |
| Baseline executor readiness | confirmed against main at `a6ae1b8f24de` |
| Decision preservation | confirmed: gate, API, state and selection semantics are fixed below |
| Execution tractability | coherent production migration with focused Git/Roslyn/state seams |
| Execution ledger | `.execution/M0016-production-gate-local-semantic-review.md` (planning-seeded) |
| Scope size | medium-large; single coherent migration of current semantic review scheduling |
| Implementation autonomy | high for local source/test mechanics; no material policy redesign |
| Documentation sync | direct affected authority only, broad pass deferred |
| Focused validation | Tier 1/3 tests under `eng/validate.ps1` and isolated Git fixture |
| Repository validation | `.\eng\validate.ps1` (follow shard/verify plan when supplied) |
| Integration validation | real Windows Git PR/worktree fixture, live Core/CLI process and persisted acceptance |
| Validation locus/platform | local Windows 11, .NET 11 SDK, Git, PowerShell, isolated consumer tool path |
| Consumer/release validation | current packed .NET tool installed into isolated consumer |
| Human review | blocking: `REV-M0016-COMPLETION` |

## Goal

Replace the already rejected production hazard/age-based semantic-review scheduler with an **implemented** gate-local, risk-stratified, bounded review selector for the existing three semantic-review rules. Move directly to useful production behavior, then add more hygiene rules in later milestones. M0015 established mechanical feasibility but **not** comparative semantic effectiveness; M0016 is **not** another historical-replay or parameter-learning experiment.

## Target State

A concrete PR/worktree/manual-inspection gate determines which rule-eligible subjects warrant costly model review; rules emit zero or a small deterministic batch (maximum five each). Directly changed subjects dominate ordinary gates, while up to one context-affected subject provides bounded adjacent inspection. External agents—not the CLI—judge review items. Accepted observations and stable gate tickets preserve the existing review lifecycle safely without hazard/age clocks or invented quality evidence. Existing deterministic rules, local facts, review expansion/handoff, CLI and package behavior remain usable.

## Scope

- Production C#/.NET rule integration, Git gate scope, candidate classification, selection, private acceptance state, versioning, migration and documentation.
- Only the three existing semantic review rules; no new rule, formatter, learned policy or research corpus.
- Additional `check` flags for committed-PR gates and explicitly authorized background surveillance; all other product commands preserved.

## Non-goals

- Revalidating hazard effectiveness or comparing hazard and gate-local models; the direction pivot is already decided.
- New BORINGness or documentation review questions, thresholds, severities or per-rule configuration.
- Learned probabilities, local grid calibration, cumulative age, automatic recurring surveillance, statistical defect-rate estimates or promises of population quality.
- External model/provider invocation, independently spawned reviewers, multi-repository experimental replays or sourcing a Private repository.
- Generalized risk-feature framework, plugin discovery, callback pipelines, persistent Roslyn index or new CLI domain language.
- New hygiene rules/formatters, broad refactoring, global docs normalization or release publication.

## Decisions and Constraints

### Gate definition and CLI behavior

A **gate** is one immutable logical review opportunity defined by repository root, inspection mode, resolved Git comparison base when supplied, HEAD plus current worktree content (including staged/unstaged/untracked eligible C#), selected reporting path set, rule ID/version and eligible head-state subject fingerprints. The gate must be stable across a repeated check of identical inputs and invalidated when those inputs change. Do not introduce a hidden time/age clock.

- `hygiene check --base <ref>` is a new explicit PR inspection mode; compute `git merge-base <ref> HEAD`, compare that base to the current worktree so committed feature branch changes are visible, and report only currently eligible C# target files. Handle invalid refs or ambiguous arguments as errors. `--base` is mutually exclusive with explicit paths, `--changed`, and `--review-all`.
- Existing `--changed` keeps its pre-M0016 *worktree relative to HEAD* meaning and deterministic target semantics; include staged/unstaged/untracked paths. Explicit path arguments select a manual scoped inspection independent of Git diff; they are incompatible with `--review-all`.
- Bare `hygiene check` still runs deterministic rules on the full default source scope but schedules no semantic review by default. `hygiene check --review-all` explicitly authorizes a bounded full-repository semantic inspection. It is not a user-configurable rule parameter and is incompatible with all target-selection options.
- Non-source-only changes and empty relevant change gates produce no normal semantic review; non-C# docs changes alone must not authorize C# semantic surveillance. Every enabled semantic rule still emits a `0/N` or `0/0` normal batch.
- For path/commit attribution, use safe Git commands and the *current* source content for Roslyn head candidates. Do not inspect stale detached heads as current worktree code. Treat deleted/renamed paths and a file belonging to multiple projects safely; do not fabricate candidates for deleted files.

### Selection and statistics

This is risk-stratified **selective inspection**, not acceptance certification. Eligibility and change classification come from deterministic analysis. A positive maximum budget does not imply that it must be filled; zero is legitimate. No claim of calibrated defect likelihood, uniformly random population inference or defect-free acceptance follows from five selected subjects.

- Each rule has a **fixed maximum of five** normal review subjects per gate. At an ordinary source-change gate, prioritize new/changed subject content. Reserve **at most one** slot for a context-affected unchanged subject from source files in the gate, if available; use the other slots for changed subjects. If no context exists, allow five changed subjects; if fewer than five relevant subjects exist, leave excess slots unused rather than sweep unrelated source.
- For a change that touches a method but not the summary carrier, an existing summary can still be context-affected and eligible for its semantic correctness rule. This is lower priority than a changed summary/declaration. No unrelated unchanged project-wide subject is ordinarily eligible to fill the budget.
- Under explicit `--review-all`, any rule-eligible subject may be selected up to five, with content-current prior accepted subjects suppressed. Explicit-path manual inspection uses up to five eligible subjects in the requested scope. No background surveillance occurs unless explicitly requested.
- Inside a priority tier, sample without replacement by a deterministic hash-derived rank keyed by gate, rule/version and candidate identity/fingerprint. Avoid process randomness. No guarantee of statistical detection probability is asserted for the biased selection. Bounded context inspection is exploratory, not a learned hazard.

### Versioning, observations and transactions

- Existing three rule IDs, batch IDs B-1/B-2/B-3, output kind, questions, escalation classes, rule toggle semantics, full eligible population and normal-batch output shape stay fixed; advance each rule version because *selection semantics change*. No rule-specific sampling knobs enter `.hygiene/config.json`.
- Old `.hygiene/.state/sampling.json` and historical M0009–M0015 research artifacts are preserved **as legacy**; do not reinterpret an old hazard event or accepted aggregate ticket as a labeled BORINGness type. New records in a separate transparent versioned gate-review state carry only accepted subject identity+fingerprint, rule version, gate observation identity and necessary provenance; do not store source text. Missing state starts empty, invalid state errors explicitly rather than silently erasing evidence.
- Gate-local review tickets remain **private** in current latest-run data. Version them, and bind them to the current gate/selected identity/fingerprint. `review accept` remains explicit acceptance-only; missing, negative and uncertain decisions do not become accepted automatically. Revalidate the gate and full eligible population on expand/handoff/accept; reject stale/consumed/nonselected items atomically and recheck state epochs before writes.
- A repeat of an unchanged gate after partial or full acceptance cannot refresh the five-slot allowance with newly selected identities. Already accepted unchanged fingerprints must not be emitted again; changing the subject content invalidates that suppression when a later gate makes it relevant.
- Preserve rollback-safe check/latest-run transaction behavior, current directory/rule-off handling, revalidation discard semantics and concurrency protection. New persisted source gate metadata must be sufficient for precise revalidation on a later process invocation; do not publish private tickets in user JSON/handoff text.

### Architecture and validation intent

- Shared selection/gate/observation services remain small and lazy; each rule owns its eligibility, review contract and any rule-specific meaningful risk criteria. Do not replace static module registration with a declarative registry.
- Retire *active* hazard/age use from production semantic reviews and update operative specs; legacy state stays untouched for backwards safety. Remove obsolete production code only if unreferenced, without deleting historical evidence or unrelated utilities.
- Build and test the production behavior, not a new multi-gate research harness. Unit/fixture and representative installed-tool integration are required; no additional blinded assessment is required for M0016.

## Baseline Executor Readiness

All material choices needed by GPT-5.6 Luna are resolved here: the modes, review admission, budget and context cap, ranking, observation shape, legacy boundary, atomicity, unchanged public interfaces, validation locus and non-goals. The executor may select class/file structure, local data representations, Git invocation details, test fixture mechanics and internal helpers. No reviewer orchestration or stronger model tier is required. If implementation exposes a truly new compatibility decision, escalate rather than silently change this contract.

## Decision Preservation

The gate is the decision unit; not time, code age or continuing hazard. Deterministic change relevance dominates expensive review. Bounded context inspection is subordinate and can displace at most one changed selection when present. The budget is a ceiling; an empty or irrelevant gate may spend nothing. The CLI never invokes the model. An old accepted **sampling event** is not a per-type semantic quality label. Preserve these principles when refactoring; they are not optional performance hints.

## Execution Tractability

One coherent production migration of three existing rules, existing host review workflow and shared state. Planning seeds the obligation and evidence registers; implementation owns work packages and actual code choices. Avoid inserting new experiments or polishing unrelated rules.

## Required Authority

- `AGENTS.md`; `docs/ENGINEERING.md`; `docs/ARCHITECTURE.md`.
- `docs/specs/SEMANTIC-REVIEWS.md`; `docs/specs/SAMPLING.md`; `docs/specs/REVIEW-HANDOFFS.md`; `docs/specs/RULE-CAPABILITIES.md`; `docs/specs/AGENT-HELP.md`.
- Non-authoritative context only where needed: `docs/research/SAMPLING-GATE-PIVOT.md` and `docs/research/GATE-LOCAL-SAMPLING-PROTOTYPE.md`; they cannot override this new production contract.

## Acceptance Criteria

- **AC-01** — A PR-base gate is supported by `hygiene check --base <git-ref>`: resolve the merge-base of the supplied ref and current HEAD, compare that base to the current working tree (including committed, staged, unstaged and untracked eligible changes), and expose only current head/worktree C# documents for reporting while allowing unchanged project context for analysis. Reject invalid refs and ambiguous/incompatible argument combinations explicitly; do not silently fall back to a full scan.
- **AC-02** — Preserve existing `hygiene check` full-scope, explicit-path and `--changed` target-resolution semantics for deterministic rules, and preserve full/explicit/changed formatting and normalization behavior. `--changed` continues to mean uncommitted/staged/untracked working-tree changes relative to HEAD, not silently all PR commits. Deleted source paths never become head candidates; renames and paths with duplicate project inclusion remain safe and deterministic.
- **AC-03** — Semantic-review gate admission is explicit and cheap: `--base` and `--changed` authorize review only when their resolved scope contains eligible C# changes; explicit path arguments authorize a bounded manual inspection of those targets; bare full-repository `check` remains a deterministic full check but schedules zero semantic reviews by default. Add `hygiene check --review-all` as the explicit, full-repository bounded background-surveillance request, incompatible with `--base`, `--changed` or explicit paths. No background inspection is inferred from non-source-only changes.
- **AC-04** — Migrate all three current semantic-review rules (`docs.summary.quality.review`, `docs.summary.language.german.review`, and `architecture.boringness.review`) to the shared gate-local candidate/selection mechanism while retaining independent rule eligibility, stable rule IDs, rule-specific questions and escalation semantics, toggle-only participation, canonical B-1/B-2/B-3 ordering and source-backed project-qualified identity/disambiguation. Mechanical rules remain exhaustive.
- **AC-05** — Use deterministic rule-relevant priority: new or materially changed eligible subjects in the gate first, then unchanged subjects in gate-touched eligible source documents as context, then other eligible background subjects only under explicit `--review-all`. A changed implementation may make an unchanged documentation summary context-relevant; comparing only the summary text must not hide this. Unknown impact is not silently classified as a direct change.
- **AC-06** — For ordinary change gates, each rule chooses at most five candidates: up to four from directly changed subjects when applicable, plus at most one context-affected unchanged subject from the touched-file candidate population when such a candidate exists; remaining slots may be used for direct changed subjects, but must not be filled from unrelated background. When no context candidate exists, use all five slots for direct changed subjects. Explicit-path inspection and `--review-all` may inspect up to five eligible candidates without this reserve. Never force all five slots to be filled. Selection is without replacement.
- **AC-07** — Selection is stable for the same gate inputs and current rule/algorithm versions, including project/path/symbol/fingerprint identity and tie-breaking, and does not depend on process randomness, wall-clock time, local machine order, persisted repository seed, or an accumulated hazard/age clock. A material change of base, source, rule version, eligible population, or gate mode invalidates a stale selection appropriately.
- **AC-08** — Documentation-only and unchanged gates cause zero ordinary semantic reviews even when eligible repository-wide subjects exist. Source gates with no eligible candidates for a particular semantic rule produce zero for that rule. A full repository `check` without explicit inspection authorization also produces zero; the complete semantic eligible population is still retained for expansion and population fingerprint validation.
- **AC-09** — Successful checks continue to emit exactly one normal review batch for each enabled semantic rule, even for `0/N` and `0/0`; `PopulationCount` denotes the current rule-eligible reporting population and `SampleCount` the selected subset. The host must not perform a second ranking, and check text/JSON remain compatible apart from explicitly versioned documented new functionality.
- **AC-10** — Repeated evaluation of an identical gate does not generate fresh additional review work or silently consume remaining capacity; a previously accepted item is not reissued at the same unchanged source fingerprint, and accepting part of a batch does not backfill with new candidates on repeat. Changed fingerprints can make an item eligible again at a new gate. Review budget is an upper bound per gate/rule, not a refill target per invocation.
- **AC-11** — Preserve `hygiene review accept` and `--all` over only the current normal sample; model-independent gate-local private tickets bind current gate, rule/version, source population and candidate fingerprints. Revalidation rejects stale, missing, nonselected, previously accepted, incompatible-version or conflicting items as a whole. A negative or uncertain semantic review cannot be submitted as acceptance and is never inferred from CLI activity.
- **AC-12** — Publication and acceptance remain rollback-safe and concurrency-checked: on check failures or revalidation, neither `latest-run.json` nor gate-review state advances incorrectly; multi-item `review accept` is atomic, and a concurrent mutation or cancellation does not leave a mixed state. The new gate metadata needed for revalidation survives process restart without exposing internal tickets publicly.
- **AC-13** — Introduce transparent, versioned, bounded and minimal durable gate-review acceptance evidence using subject identity, full content fingerprint, rule/version, gate identity and observation timestamp/provenance appropriate to the existing explicit-accept contract. Legacy hazard/age state at `.hygiene/.state/sampling.json` is neither deleted nor imported as real type-level judgments; old tickets fail clearly and users can establish fresh gate-local state. Do not retain source text, synthesize negatives, or infer a defect rate.
- **AC-14** — Remove hazard, elapsed/activity-age, due-event and probability-threshold processing from the active production semantic-review path and from current product authority. Historical M0009–M0015 artifacts and legacy on-disk data remain preserved; delete obsolete active code if no longer consumed, without broad unrelated rewriting. No long historical replay or gate-local learned calibration is required.
- **AC-15** — Existing review expand/handoff continues to revalidate and expose the complete current rule-eligible population for the stored reporting scope, not just selected items, and produces compatible transport-neutral handoff without a model invocation. Changed-base semantics persist through review revalidation, and stale source/base changes are rejected before expansion, handoff or acceptance.
- **AC-16** — Keep each rule an independent explicit in-process module over the lazy RepositorySession/RuleContext shared substrate. Shared selection, gate context and accepted-review store are small typed C# services, not a centralized callback pipeline, rule hierarchy, risk-feature DSL, reflection/plugin discovery, provider integration, or per-repository configurable parameter surface.
- **AC-17** — Maintain default rule IDs, fixed purpose/rubric/escalation text, existing `enable`/`disable` behavior, deterministic rule ordering and opt-in semantic-review status. Advance the three semantic-review rule versions and the relevant private persisted-state/algorithm versions explicitly so earlier hazard tickets are never mistaken for valid gate-local tickets.
- **AC-18** — Product CLI help/agent guidance and text/JSON process behavior document and validate the new `--base` and `--review-all` surfaces, invalid combinations, error channels and exit semantics while keeping existing commands and normal output contracts stable. No new repository rule configuration is introduced.
- **AC-19** — Focused fixtures prove source/commit/worktree change attribution, candidate classification, deterministic selection, bound/zero-spend behavior, version invalidation, accepted-review lifecycle, stale base/ticket handling, atomic failure cases and all three independent semantic rules at the applicable real Git/Roslyn boundary.
- **AC-20** — Repository regression validation and representative current-built/packed/isolated `dotnet tool` consumer testing prove check, `--base`, zero-review and positive-review cases, review expand/handoff/accept, profile and rewrite compatibility on Windows 11/.NET 11. Do not claim consumer success from a global or previously installed binary.
- **DOC-01** — Update `docs/specs/SAMPLING.md`, `docs/specs/SEMANTIC-REVIEWS.md`, `docs/ARCHITECTURE.md`, `docs/ENGINEERING.md`, applicable CLI/agent help and `docs/MILESTONES.md` so operative authority describes gate-local behavior and no longer prescribes active hazard/age behavior. Preserve historical research and milestone evidence unchanged.
- **DOC-02** — Document the risk-based finite-gate inspection model with accurate limits: selected subjects are an economically bounded, possibly biased sample, not proof of defect-free code, not a calibrated defect-rate estimate and not a statistical population-coverage guarantee. Explain prioritization versus random context coverage, optional surveillance authorization, and future evidence/calibration hooks without introducing a new research study.
- **REV-01** — Prepare `.review/REV-M0016-COMPLETION.md` requesting project-owner/delegate human acceptance of the production behavior, preserved API/CLI/acceptance semantics, compatibility/invalidation decisions, validation evidence and future rule-expansion direction. Do not self-approve, waive, merge or reopen historical milestone reviews.

## Acceptance Evidence Topology

Distinct paths below have materially different Git, Roslyn, state, CLI or consumer boundaries; no Cartesian-product test grid is required.

| ID | Parent obligation | Required evidence case | Why separate evidence is required |
|---|---|---|---|
| EC-01a | AC-01 | PR branch with committed changes relative to a valid base produces the correct eligible current source scope, including merge-base handling | Without this case, worktree-only implementation could falsely pass. |
| EC-01b | AC-01 | Staged, unstaged, untracked, deleted and renamed source paths are handled without false or duplicate head declarations | Git worktree paths use a distinct discovery mechanism from committed diff. |
| EC-02a | AC-02 | Old `--changed` and explicit-path targeting still have the same deterministic finding scope | Gate admission must not rewrite existing command target semantics. |
| EC-02b | AC-02 | Unchanged full-check and transactional rewrite/profile behavior remain compatible | Full-run and rewrite modes take different existing paths. |
| EC-03a | AC-03 | Bare full check, docs-only gate, clean `--changed` and `--base` produce zero semantic selections | No-change admission is a distinct boundary from selection. |
| EC-03b | AC-03 | Explicit-path and explicit `--review-all` produce authorized bounded review work with invalid combinations rejected | Manual and background authorization differ from change-gate admission. |
| EC-04a | AC-04 | Both documentation summary rules preserve independent populations, rule toggles, review questions and batches | Two independently switchable rules share facts but must not share review state. |
| EC-04b | AC-04 | BORINGness type population includes eligible source-backed nested/partial declarations and excludes generated/enums/delegates | Type-population path is materially different from documentation carriers. |
| EC-05a | AC-05 | Touched implementation with unchanged documentation summary remains context-eligible; changed types take changed priority | Review-relevance and literal summary edits are not identical. |
| EC-05b | AC-05 | Unrelated background declarations are excluded from ordinary gates but eligible under explicit full surveillance | Prevents accidental full-repository cost. |
| EC-06a | AC-06 | Four-changed plus one-context and five-changed/no-context scenarios preserve the bounded preference | With context available the reserve operates differently. |
| EC-06b | AC-06 | Sparse source gate does not fill the remaining allowance with unrelated subjects | A maximum budget must not become a quota. |
| EC-07a | AC-07 | Repeat/restart and different enumeration ordering produce identical selection identities | Randomness and ordering stability require independent proof. |
| EC-07b | AC-07 | Base/head/symbol/fingerprint/rule-version changes invalidate affected tickets/selections | Current-gate identity must not silently reuse stale source. |
| EC-10a | AC-10 | Same-gate recheck before and after accept never replenishes or redistributes the normal sample | Distinguishes bounded gate work from repeated batch quotas. |
| EC-10b | AC-10 | New gate with actually changed source fingerprint can reselect a previously accepted subject | Accepted observation is fingerprint-current, not permanent suppression. |
| EC-11a | AC-11 | Valid selected-item and --all acceptance persist only real explicit acceptable outcomes atomically | Positive acceptance path has its own durable side effects. |
| EC-11b | AC-11 | Unknown, stale, duplicate, nonselected and already-consumed selections reject all requested items | Negative/stale path must not partially consume work. |
| EC-12a | AC-12 | Check publication and accept succeed with consistent latest-run and review state after restart | Success path crosses a persisted transaction boundary. |
| EC-12b | AC-12 | Injected interruption, concurrent mutation and revalidation discard cause no mixed state | Failure path crosses a distinct rollback/consistency mechanism. |
| EC-13a | AC-13 | Legacy hazard/age state remains untouched and cannot be admitted as accepted per-subject quality history | Legacy state lacks the new observation meaning. |
| EC-13b | AC-13 | Missing, unsupported or corrupt new gate-review state has explicit reset/error semantics and no false acceptance | Fresh initialization differs from malformed-state recovery. |
| EC-15a | AC-15 | Expand/handoff works for a positive and a zero normal batch while retaining all eligible candidates | Selected sample and eligible population are distinct. |
| EC-15b | AC-15 | Changed base, edited subject and tampered latest-run metadata fail expansion/acceptance revalidation | Stored gate context is a separate stale-run boundary. |
| EC-20a | AC-20 | New built CLI process exercises `--base`, `--review-all` and stderr/exit-code errors | In-process tests cannot prove the actual CLI parser/process surface. |
| EC-20b | AC-20 | Fresh installed .NET tool in isolated consumer exercises new gate-local flow, acceptance and existing representative features | Distribution/installation can differ from project-run CLI behavior. |

## Validation

Validation depth and *locus* are independent: Tier 3 is local Windows/Git, Tier 4 is a current packaged .NET tool. Follow repository canonical `eng/` entrypoints, and only add a focused `eng/` runner if needed. For existing `eng/validate.ps1`, honor its real plan/shard/verify interface if present; do not invent unsupported flags.

| ID | Depth | Target | Locus/platform | Command/check | Proves | Expected evidence |
|---|---|---|---|---|---|---|
| VAL-01 | Tier 1 | Gate eligibility, Git diff/base normalization, deterministic sampling and upper bound | local Windows 11/.NET 11/Git | Focused TUnit/Core fixture invocation via canonical `eng/validate.ps1` shard/selection where available, plus dedicated focused command if added | AC-01, AC-03, AC-05, AC-06, AC-07, AC-08, EC-01a, EC-01b, EC-03a, EC-03b, EC-05a, EC-05b, EC-06a, EC-06b, EC-07a, EC-07b | Exact changed targets, candidate IDs/reasons, unchanged/context bounds, repeatable zero/positive selections |
| VAL-02 | Tier 1 | Independent semantic-rule module populations and normal materialization | local Windows 11/.NET 11 Roslyn fixtures | Focused Core test command via `eng/validate.ps1` or documented `dotnet test` fallback if repository shard tooling unavailable | AC-04, AC-09, AC-16, AC-17, EC-04a, EC-04b | Three versioned independent rules, batch IDs/order, current eligible population and selected subset |
| VAL-03 | Tier 1 / Tier 3 | Accepted-observation persistence, stale ticket rejection, atomic failure, base persistence | local Windows 11/.NET 11 isolated Git repositories | Dedicated real persisted-state lifecycle fixtures or repository canonical validation selections | AC-10, AC-11, AC-12, AC-13, EC-10a, EC-10b, EC-11a, EC-11b, EC-12a, EC-12b, EC-13a, EC-13b | Ticket/state snapshots, positive/negative atomicity, legacy preserved, no invalid label imports |
| VAL-04 | Tier 3 | Real PR committed diff, worktree changes, zero-work, expansion and handoff | local Windows 11/.NET 11/Git fixture repos | Create isolated base/feature/dirty-worktree fixture and run built `hygiene check --base <ref>` plus existing commands | AC-02, AC-15, AC-18, EC-02a, EC-02b, EC-15a, EC-15b, EC-20a | CLI text/JSON, full-population expansion, handoff source anchors, invalid-ref errors |
| VAL-05 | Tier 2 | Complete repo regression and authority consistency | Windows 11/.NET 11/PowerShell | `.\eng\validate.ps1` using any advertised plan/shard/verify flow | AC-02, AC-04, AC-09, AC-12, AC-14, AC-16, AC-17, AC-18, AC-19, DOC-01, DOC-02 | Aggregate full suite success, code/docs review, no active age/hazard write path or unintentional changes |
| VAL-06 | Tier 4 | Actual packaged .NET tool installed into isolated consumer | Windows 11/.NET 11 with local `dotnet tool` package/install | Pack current version and install into isolated manifest/tool path; invoke installed `hygiene` check/review/format/profile scenarios per docs/ENGINEERING.md | AC-18, AC-20, EC-20b | Installed command output, exact current package provenance, positive/zero-review and backward-compat receipts |
| VAL-07 | Human | Project-owner/delegate milestone acceptance | review of PR and `.review/REV-M0016-COMPLETION.md` | Human review record followed by repository documented milestone-scoped check if available | REV-01 | Decision with evidence references; `AWAITING HUMAN REVIEW` if no approval |

## Human Review

Applicability: required and blocking (production scheduler migration and compatibility).

Review class: milestone-completion, not recurring global approval.

Canonical review ID: `REV-M0016-COMPLETION`.

Review subject: fixed gate-local sampling policy, public/installed CLI behavior, private-state compatibility, atomic review lifecycle, selective-review semantics and evidence-specific validation.

Required evidence: all acceptance/evidence/validation rows, representative CLI and installed-tool logs, versioning and state migration findings, authority sync and outstanding limitations.

Acceptance criteria: material compatibility preserved; changes scoped; all required validation gates satisfied; no extra agent-review experiments; no unsupported probability claims.

Reviewer role: project owner/delegate.

Waiver policy: must be explicit, scoped and recorded by authorized reviewer; implementation agent never waives or self-approves.

Milestone completion check: follow repository documented milestone-scoped command *if one exists*; no such command is assumed by this package.

## Documentation Policy

Update directly affected authority and CLI guidance under DOC-01/DOC-02. Historical M0009–M0015 research and reviews remain historical. Broader documentation sync is separate.

## Completion Expectations

Reread the milestone and seeded ledger after implementation; verify exact obligation-ID and evidence-case-ID set equality, individual mapping to actual source/tests, validation at declared targets, remaining blockers, and review status. Add compact completion evidence to this milestone; update ledger incrementally without compressing obligations. Passing tests alone is insufficient.

## Completion Evidence

**Implementation completed; human acceptance remains open.**

- Branch: `implementation/m0016-gate-local-semantic-review`, based on `main` at `a6ae1b8f24de5e857317edb1d06a06a05989600e`.
- Work packages: (1) Git scope and CLI gate modes; (2) shared deterministic gate-local selector across all three rule modules; (3) private tickets and accepted-only transactional lifecycle; (4) operative docs/help, package version 0.8.0, and installed consumer validation.
- Focused source evidence: `tests/DotNetAiCodeHygiene.Core.Tests/GateLocalReviewTests.cs` covers stable rank, 4+1 preference, no refill, changed fingerprint, committed feature/base scope, docs-only zero spend, state restart, preserved legacy file, corrupt state, injected atomic interruption, and concurrent-state rejection. `LifecycleTests.cs` covers rule lifecycle and scope/rewrite compatibility. `CliProcessTests.cs` covers `--base`, `--review-all`, invalid combinations/error exits, handoff, and existing process contracts.
- Canonical validation: `eng/validate.ps1` passed on Windows 11/.NET 11. Release build succeeded with 0 warnings/0 errors; Core 85/85 and CLI 24/24 passed (109/109 total); package `DotNetAiCodeHygiene.Tool.0.8.0` packed and installed from the exact current artifact into an isolated consumer; installed consumer scenarios and repository M0006 self-check passed; `git diff --check` passed.
- Authority/documentation: current sampling and semantic-review specifications, architecture, engineering, CLI/agent help, README and milestone index describe the gate-local model. Historical M0009–M0015 research and legacy `sampling.json` data remain preserved; production rules no longer consume the legacy hazard/age path.
- Limits: selection is biased and is not a calibrated defect-rate estimate, coverage guarantee, or proof of defect-free code. No semantic reviewer was invoked. VAL-07 remains pending project-owner/delegate review; this implementation does not approve or waive itself.
- PR: to be recorded after publishing the branch.

## Escalation Boundary

Implementation owns local algorithms/files/tests under the frozen contract. Escalate only truly material new choices affecting semantics, API, persisted compatibility, scope, validation policy/locus or reviewer authorization. Otherwise implement and fix.
