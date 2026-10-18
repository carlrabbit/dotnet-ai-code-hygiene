# M0020 — Efficient and Resumable Validation

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready (execution dependency: M0018 merged and current validator established) |
| Mode | ai-executed-human-reviewed |
| Guide system | Agentic Project Guides 0.9.1, base + cli-tool |
| Baseline model | project-established implementation agent; no separate model prerequisite |
| Planning source | merged `main` `bc7bf63bb49d16368a759238ca3788b7d9a7af12` after M0018 PR #19; reconcile any later main changes |
| Validation target | real Core/CLI TUnit suites, Windows PowerShell validator, Git fixtures, exact current installed .NET tool |
| Execution locus | local Windows 11, .NET 11, Git and PowerShell; no CI substitution |
| Existing canonical command | `.\eng\validate.ps1` (no argument) |
| New developer interface | PowerShell `-PlanJson`, `-Shard <id>`, `-Verify`, and explicit cold/fresh mode; actual implementation may expose equivalent ergonomic aliases |
| Environment authority | existing authorized SDK/Git/NuGet and isolated temp directories/locally scoped feeds; no persistent host tooling, admin actions or CI/workflow creation |
| Evidence requirements | stage-level atomic fingerprinted receipts, real cold installed-consumer verification, case-specific proof and independent review |
| Ledger | `.execution/M0020-efficient-resumable-validation.md` |
| Compatibility | no pre-1.0 CLI compatibility obligations; no changes to public `dotnet hygiene` behavior |
| Milestone collision policy | M0019 reserved for `--yolo-all`; M0020 is validation infrastructure only |

## Goal

Reduce the execution cost of ordinary agent correction loops without reducing the evidence required for milestone completion. Replace the monolithic, sequential 550+-line canonical validation process with independently executable, resumable, provenance-checked stages, improve timing insight and remove unnecessary repeated expensive fixture work.

## Target State

An implementer can run only relevant Core/CLI/test/consumer validation during development, repair one failing late stage, and reuse already-proven unaffected stages. The canonical completion command still establishes all required Windows/.NET tests and installed-tool behaviors by running missing/stale stages and verifying current receipts. A fresh cold execution remains available and demonstrates that all stages can run from a clean checkout. Timings reveal the actual dominant cost; fixture amortization preserves isolation.

## Scope

- Restructure `eng/validate.ps1` into bounded independently executable stages with machine-readable plan, dependency tracking, atomic receipts and fast verifier. It may be split into cohesive helper scripts/modules under `eng/` as needed.
- Build/Core/CLI/README checks and all currently required actual Tier 4 consumer scenarios remain covered, including update, local-manifest, formatting/rollback and self-host checks.
- Real affected-test selection and measurable timing, including external setup overhead; profile particularly costly Roslyn/MSBuild, dotnet process and Git fixture work before deciding what to optimize.
- Identity-scoped reuse of immutable previous-version NuGet fixture with safe cold reconstruction; current package identity remains strictly fresh when changed.
- Project-local documentation and a separate concise guide-feedback note (advisory only).

## Non-goals

- Add, remove or weaken product hygiene rules, gate selection, sampling or agent review semantics.
- M0019 exhaustive `--yolo-all` behavior; do not consume the reserved milestone number.
- Remove required installed-consumer proof, accept a mock consumer as full evidence, or degrade milestone acceptance-case granularity.
- Depend on CI, create GitHub workflows, install new host-wide SDKs, introduce a generic cross-repository CI platform, a persistent product repository index or unneeded infrastructure framework.
- Automatically edit or publish `carlrabbit/agentic-project-guides`; upstream guidance is an independent follow-up.
- Require a fixed runtime speed threshold without initial cost measurement.

## Decisions and Constraints

The **existing guide already calls for focused validation in the implementation loop and supports plan/shard/verify with fingerprinted receipts**. M0020 **implements these existing concepts project-locally** and clarifies normal development cadence. Source of guide authority: `meta/VALIDATION-MODEL.md`, `meta/CONSTRAINED-EXECUTION-MODEL.md`, `meta/MILESTONE-EXECUTION-MODEL.md`, ADR-0014/0015/0018 and `templates/prompts/execute-milestone.md` in Agentic Project Guides v0.9.1.

Cold and warm modes must be semantically honest: default aggregate may skip previously successful **only if current dependencies and fingerprints still match**. It must not silently replay stale current package/test receipts. Receipt artifacts are implementation outputs, not repository policy or approver acceptance. Include all material stage inputs in fingerprints; if precise tracking is difficult, conservative invalidation is preferable to under-invalidation. In particular, current tool source, build properties, output binary, .nupkg hash and installed shim provenance must be bound together. Fingerprints must account for relevant untracked files and toolchain identity. Do not treat a previous `git HEAD` alone as proof of unchanged inputs.

Design stages to avoid expensive repeated restore/project generation, but do not share writable test fixtures, hide generated-file conflicts, skip actual requested external boundary tests or alter the tested product behavior. Instrument before expensive optimization; focus first on obvious repeated process/restore/project-workspace costs. Stage timing reports may include both actual and cached/reused stages but must distinguish them. Long-running stages may be split further by material consumer scenario if necessary for effective rerun isolation. Stage planning/receipts must be testable without requiring a new full release-mode tool installation for each planner/unit test.

A change during validation invalidates the affected receipt; parallel stage execution is not safe by default. State locking and atomic receipt writes must prevent partial or concurrent corruption. The canonical no-argument command remains the authoritative completion gate; a developer's successful focused stage is not a milestone completion claim. In contrast, a fast aggregate verifier **can** establish final automated evidence when every required receipt is current and coverage is complete. This is the point of resumability: do not demand forced repeated complete tests after every harmless edit.

M0018 has been merged through PR #19 and its canonical validator is present in `main` `bc7bf63bb49d16368a759238ca3788b7d9a7af12`. Begin M0020 from latest `main`; reconcile any changes since this planning reference. No planning-chat-only assumptions are required.

## Required Authority

- `AGENTS.md`, `docs/ENGINEERING.md`, `docs/ARCHITECTURE.md`, `docs/specs/AUTONOMOUS-WORKFLOW.md`
- `eng/validate.ps1` as merged from M0018, both test projects, `DotNetAiCodeHygiene.slnx`, product packaging project
- `docs/milestones/M0018-autonomous-hygiene-lifecycle.md` for historical M0018 validation obligations (not instructions to rerun all tests for every M0020 edit)
- This ready milestone and its seeded execution ledger

## Acceptance Criteria

- **AC-01** — The authoritative `eng/validate.ps1` exposes discoverable, independently executable, bounded validation units using PowerShell-native plan/shard/verify commands (`-PlanJson`, `-Shard <id>`, `-Verify`), with no-argument canonical aggregate execution; its machine-readable plan declares IDs, commands, dependencies and expected receipt/output locations.
- **AC-02** — Stage decomposition preserves the actual validation behavior and distinct evidence targets of the M0018 canonical run, including solution build, both test suites, README/policy checks, current-package packing, freshly installed tool consumer checks, migration/update, ancestor-local-manifest and rollback scenarios, and applicable repository self-host validation. A failure in a late consumer stage can be corrected and rerun without restarting unrelated successful stages.
- **AC-03** — Each successful stage atomically persists an independently checkable fingerprinted receipt; a failed, interrupted or partially executed stage cannot retain valid pass evidence for the failed attempt. The fast verifier checks all required current receipts, required dependency receipts, output-artifact provenance and their live input fingerprints, and fails closed on missing, altered or stale evidence.
- **AC-04** — Receipt invalidation is correct and reasonably narrow: changes to relevant source/tests/project properties/commands/scripts/toolchain or referenced output invalidate affected stages and dependents; unrelated changes may retain evidence only when the dependency/input model proves it safe. When uncertain, invalidate conservatively; reuse must never allow a stale current tool binary/package to establish installed-consumer success.
- **AC-05** — The canonical no-argument completion invocation executes missing or stale required stages and then verifies the aggregate; it may reuse still-valid independent receipts. A documented fresh/cold mode forces all required stages to execute with clean transient state to prove reproducibility; the default must not claim a complete run solely because previously successful but stale stage logs exist.
- **AC-06** — Produce actionable timing evidence for each stage (wall-clock, setup, execution where separable), and identify expensive test groups or individual tests using supported TUnit/MTP reporting or small focused instrumentation. Record an initial baseline and a post-change comparison without claiming an arbitrary speed target or conflating cached elapsed time with actual execution.
- **AC-07** — Provide convenient affected-test and affected-stage developer commands for both Core and CLI suites. The guide-recommended iterative loop uses only directly relevant checks, with explicit prerequisites; a focused command must never silently mark unexecuted unrelated validation as complete.
- **AC-08** — Reduce repeated test/consumer setup where supported by measurements: amortize or safely reuse immutable project templates, Roslyn workspace inputs, restore artifacts and/or Git fixture construction without sharing mutable test state or breaking test independence. Demonstrate isolated repeatability of the optimized setup.
- **AC-09** — Cache or reuse the immutable 0.9.0 previous-version test fixture only with verified source commit, package inputs, SDK/toolchain and content identity. Reproduce it in a clean checkout when cache is absent. Always freshly build/pack and hash the changed current-version tool inputs before validating installed-consumer behavior, and retain correct isolation for distinct NuGet feeds/tool installations.
- **AC-10** — Verification and staging work with failing, interrupted and resumed runs, including changes during execution; concurrent or overlapping shard execution is prohibited unless specific dependency and filesystem isolation is proved. Generated receipts, caches and output artifacts remain untracked and safely disposable; cleaning them cannot destroy project or user data.
- **AC-11** — Update `docs/ENGINEERING.md`, `AGENTS.md` only where necessary, and relevant local validation guidance to document stage invocations, prerequisite semantics, incremental development cadence, receipt validity, fresh release validation, timing inspection and completion-gate expectations. Preserve Guide 0.9.1 validation depth, execution-locus, package provenance and criterion-specific evidence requirements.
- **AC-12** — Maintain final functional equivalence: the complete validation requirement remains authoritative on the declared Windows 11/.NET 11 locus, all prior M0018 consumer scenarios remain represented by executed real boundaries, and observed passes alone are not substituted for independent obligation/case evidence. No new production hygiene behavior is introduced.

## Acceptance Evidence Topology

| ID | Parent | Required distinct evidence | Why separate |
|---|---|---|---|
| EC-01a | AC-01 | Plan reports stable stage identifiers, executable commands and dependencies and permits running individual build/Core/CLI stages without Tier 4 | Plan/discovery and local stage invocation |
| EC-02a | AC-02 | Canonical aggregate includes each existing consumer scenario and every applicable test and documentation gate | No loss of target boundaries |
| EC-02b | AC-02 | Late failing installed-tool scenario can be rerun independently after a fix without re-executing still-current successful test stages | Failure recovery versus cold first run |
| EC-03a | AC-03 | Successful stage creates atomic fingerprinted receipt and unchanged `-Verify` accepts it without executing tests | Positive reuse and verifier no-op |
| EC-03b | AC-03 | Failed, terminated or truncated stage leaves no valid success receipt and aggregate verifier rejects it | Failure/partial-output path |
| EC-04a | AC-04 | Changing a test file, CLI source, validation script, toolchain identity or packaged artifact invalidates all relevant dependent receipts | Relevance and dependency invalidation |
| EC-04b | AC-04 | A demonstrably unrelated change can retain applicable independent receipts and never validates an outdated current consumer package | Safe reuse, not blanket invalidation |
| EC-05a | AC-05 | Cold/fresh canonical run executes all required gates; subsequent unchanged default run reuses valid receipts and verifies the complete set | Fresh versus warm completion |
| EC-06a | AC-06 | Reports actual per-stage durations and identifies slowest test groups/testcases where support exists; cached runs are marked reused | Observability of cost |
| EC-07a | AC-07 | Focused Core and CLI test selection executes named tests or groups and exposes that nonselected gates remain outstanding | Developer feedback scope |
| EC-08a | AC-08 | Repeated setup optimization retains fixture isolation and passes when run in different orders | Fixture reuse cannot leak mutable state |
| EC-09a | AC-09 | A clean run creates the pinned previous-version package; warm run validates cache identity and avoids unnecessary rebuild | Immutable old fixture positive and cold paths |
| EC-09b | AC-09 | Changed current source/package invalidates consumer receipt and installs the exact freshly packed current artifact, not an old shim/cache | Changed tool product boundary |
| EC-10a | AC-10 | Interrupted/parallel-conflicting stage cannot fabricate aggregate success or corrupt another stage receipt; disposable artifacts clean safely | Concurrency and cancellation safety |

## Validation

Validation depth and execution locus are separate. Real Windows 11 + .NET 11 execution is authoritative. Validate stage boundaries using lightweight fixtures, then run the installed-consumer validation stages and cold/warm canonical aggregate once implementation is stable. Do not repeatedly perform a complete cold pass to diagnose a late single-stage defect.

| ID | Depth/target | Locus | Required command/check | Proves | Expected evidence |
|---|---|---|---|---|---|
| VAL-01 | Tier 1 — stage planner and receipt contract tests | Windows 11 + .NET 11 + PowerShell; isolated fixture directories | Invoke `eng/validate.ps1 -PlanJson`, selected `-Shard` stages and `-Verify` with controlled input edits and injected failures | AC-01, AC-03, AC-04, AC-05, AC-10; EC-01a, EC-03a, EC-03b, EC-04a, EC-04b, EC-05a, EC-10a | Actual stage/receipt JSON, manipulated input hashes, fast verify receipts, explicit fail-closed behavior |
| VAL-02 | Tier 1/2 — test timing and focused feedback | Windows 11 + .NET 11; real TUnit/MTP Core and CLI tests | Run focused Core and CLI tests, capture supported timing output and demonstrate ordered repeated fixture runs | AC-06, AC-07, AC-08; EC-06a, EC-07a, EC-08a | Before/after elapsed receipts and top-cost cases/groups; isolation proof and no unstated speed claims |
| VAL-03 | Tier 3/4 — isolated installed consumer and update stages | Windows 11 + .NET 11 + Git + NuGet, isolated local feeds; actual packaged tool | Run selected consumer/upgrade/rollback shards with fresh and previously verified immutable 0.9.0 fixture; inject late failure and rerun that stage | AC-02, AC-09, AC-12; EC-02a, EC-02b, EC-09a, EC-09b | Installed-tool identity and package hashes; fixture commit/toolchain identity, cold/warm comparison, specific scenario passes |
| VAL-04 | Tier 2/3/4 — final canonical cold and warm aggregate | Windows 11 + .NET 11 + PowerShell + Git; exact current repository state | Run cold/fresh complete canonical validation once; then unchanged warm aggregate and `-Verify`; modify one relevant source to demonstrate invalidation; `git diff --check` | AC-01–AC-12; EC-01a, EC-02a, EC-03a, EC-04a, EC-04b, EC-05a, EC-06a, EC-09b | Complete receipt set with current fingerprint, cold/warm timing and honest invalidation; no green from stale evidence |
| VAL-05 | Tier 5 — blocking milestone review | M0020 PR, milestone ledger and evidence | Authorized reviewer checks validation equivalence, safety, measured cost and audit against exact acceptance/case registry | REV-01 | Independent decision or explicit pending status; no invented reviewer approval |

## Human Review

Required, blocking, milestone-scoped. Prepare `.review/REV-M0020-COMPLETION.md` with a pending authorized reviewer/delegate decision, not invented approval. Review whether completed validation really establishes all prior required cases, fingerprint invalidation is conservative enough, cached fixtures cannot compromise consumer provenance, and stage timing/iteration evidence supports the claimed execution-efficiency improvement.

## Documentation Policy

Update the local engineering and contributor validation entrypoint instructions. Document the external-guide follow-up **as feedback only**; the milestone does not mutate a separate repository. Retain historical M0018 validation receipts and review records as historical evidence; do not revise the completed milestone merely to claim savings in M0020.

## Additional Completion Obligations

- **DOC-01** — Document the complete user/agent-facing validation commands and completion semantics in project-local engineering authority, with honest timings and concrete examples that were executed from the repository.
- **DOC-02** — Produce a concise non-authoritative guide-feedback note that identifies only actionable Agentic Project Guide improvements, distinguishes already-existing 0.9.1 features from missing guidance, and does not silently modify the separate guide repository.
- **REV-01** — Prepare `.review/REV-M0020-COMPLETION.md` with concrete stage/receipt/consumer/timing evidence and a pending independent human-or-authorized-delegate decision; do not self-approve.

## Completion Expectations

Use the guide 0.9.1 normal workflow: verify seeded ID registry, create bounded implementer-owned work packages, run affected validation per change, record durable stage receipts and real evidence, freshly reconcile obligations/cases, execute the final complete current-input validation and mandatory completion audit. Capture cold and warm timing without guessing. Completion requires independent review; if all automated evidence exists but authorization is outstanding, report `AWAITING HUMAN REVIEW`.

## Completion Evidence

Implementation evidence (independent review remains pending):

- Exact M0020 obligation registry reconciliation passed: 15 obligation rows (AC-01–12, DOC-01–02, REV-01); exact evidence-case reconciliation passed: 14 distinct cases (EC-01a through EC-10a). The matching criterion and case evidence is recorded row-by-row in `.execution/M0020-efficient-resumable-validation.md`.
- Windows 11 build 26200; PowerShell 7.6.5; .NET SDK `11.0.100-rc.1.26425.128`; Git `2.55.0.windows.3`. The full `./eng/validate.ps1 -Fresh` run executed all seven required stages successfully in 1,019.93 seconds (about 17m00s): solution 10.755s; Core 283.612s (95 passed); CLI 161.681s (30 passed); README/policy 0.002s; current package 2.004s; installed consumers 527.853s; self-host 34.017s. Both suites emitted supported MTP slow-test reports; individual MTP test durations can overlap.
- The current 0.10.0 package SHA-256 was `146d47d5d9655e779c11a96648d24bcb6409d1be73f5e1596085eea02073fdc6`. Consumer provenance records the installed shim SHA-256 `aaea6b325bfc45023b8891628d07499a919d8e2b5803b359d8c75984c4adc0a9`. Cold 0.9.0 fixture was reconstructed from commit `2e17b293be78480dcad9817f43c9bcc079a81ea8` / tree `a37b395eec5e6b3805290814cdbd3de7db8cae5e`, SDK `11.0.100-rc.1.26425.128`; its package SHA-256 was `3a7023b73b79ccef7319a9b3b3d22dd3240e9abbe8bbe9ca93b46a33ed9be1e3`. Later recovery reused this identity-checked cache.
- Unchanged no-argument aggregate reused all seven receipts in 11.498s; `-Verify` reused/accepted all seven in 6.237s. Compared with the 1,019.93s cold run, warm aggregate wall time was about 88.7x lower (98.87% less elapsed time). This is cold-versus-reuse cost, not a claim that freshly executed tests became faster; no comparable historical M0018 stopwatch baseline was recorded.
- Injected an error after consumer lifecycle scenarios: the stage failed in 520.46s and retained no valid receipt; verifier then rejected the aggregate for the missing installed-consumers receipt. Retrying ran only that stage, reused the pinned 0.9.0 fixture, passed in 518.794s, and preserved the other six stage receipts.
- Invalidation probes: a temporary CLI source comment made `-Verify` reject solution, Core, CLI, pack, installed-consumer, and self-host evidence; removing it restored all seven receipts. A temporary documentation-only file left all seven valid. An overlapping stage invocation was rejected by the exclusive lock. Focused named Core and CLI tests passed individually and did not create full-suite receipts.
- `.review/REV-M0020-COMPLETION.md` is prepared for independent review. Milestone status is **AWAITING HUMAN REVIEW**; no independent approval has been claimed.

## Escalation Boundary

Implementation chooses specific script/module decomposition, receipt format, test timing interface and fixture-caching mechanics. Return to planning if changing actual validation target/locus, dropping required consumer scenarios, weakening fingerprint identity, authorizing new host/CI infrastructure or changing the deployed hygiene product contract. No unrelated performance subsystem is authorized.
