# M0019 — Exhaustive Read-Only Hygiene Audit

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | awaiting-human-review |
| Mode | ai-executed-human-reviewed |
| Guide system | Agentic Project Guides 0.9.3; base + cli-tool |
| Baseline implementation model | GPT-5.6 Luna (existing `.guide-profile.json` setting) |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Execution ledger | `.execution/M0019-exhaustive-read-only-hygiene-audit.md` (planning-seeded) |
| Scope size | medium-large, coherent product feature |
| Implementation autonomy | high for local mechanics; fixed audit/CLI contracts |
| Documentation sync | directly affected authority only |
| Focused validation | VAL-01/02/03: focused Core/CLI, large-population, interruption scenarios |
| Repository validation | VAL-05: M0020 current-evidence verification + `git diff --check` |
| Integration validation | VAL-02/03: CLI/Git/Audit I/O |
| Validation locus/platform | Windows 11 + .NET 11 SDK + Git + PowerShell, isolated repositories |
| Environment authority | existing SDK/Git/PowerShell/NuGet and isolated local tool installs; no host-wide provisioning, CI or remote infrastructure changes |
| Consumer/release validation | VAL-04: exact newly packed/installed .NET tool, targeted public workflow; not universal release-cold run |
| Human review | required and blocking; `.review/REV-M0019-COMPLETION.md` |
| Baseline repository | `main` `cb9c9e915ce5459ce01296ed305215dae0dc12f9` after merged M0021 and M0022; reconcile newer commits before execution |
| Product version | target 0.11.0 from current 0.10.0; reconcile if `main` has advanced |

## Goal

Provide a user-explicit, exhaustive **read-only** quality audit: inspect the whole eligible repository with every enabled hygiene rule and report existing problems and uncertainty. YOLO is a full scan and **does not clean up or change the repository**. It is deliberately distinct from the normal corrective hygiene workflow.

## Target State

An implementation agent can invoke `dotnet hygiene --yolo-all`, affirm that the user explicitly requested it, inspect bounded semantic work orders until every eligible rule/subject obligation is judged, and publish `report.json` plus `report.md`. The CLI runs deterministic checks, supplies the exact fixed questions and source context, validates judgment imports, counts coverage and creates a report. It does not invoke models or ask the agent to change code. No full rescans, source-dependency tracking or repair loops are involved.

## Scope

- Public exhaustive entrypoint, agent attestation and report-only continuation commands.
- Entire repository-wide supported/eligible population for deterministic and semantic rules; disabled-rule and generated-source exclusions remain authoritative.
- Nonmutating rule evaluation, formatter/normalizer would-change findings, and already-ignored/previously-accepted subject visibility.
- Bounded report-local judgment work orders; resumable audit-output records for long agent sessions; source-context embedding only in caller-controlled local report artifacts.
- Machine-readable and concise human-readable audit reports with exact coverage accounting and reasons for problem/uncertain findings.
- Proportionate tests and exact installed-tool consumer validation under M0020/M0022.

## Non-goals

- **No cleanup**: no source edits, formatter application, normalizer application, contextual repair, reviewer escalation tasks, acceptance writes, ignored-findings updates, Git commands that change state, implicit baseline setup or installed-tool update.
- No ongoing source-change tracking, cross-file dependency graph, invalidation/rejudging after changes, population-refresh loop, snapshot-lock system or automatic multi-pass convergence.
- No model/provider integration, AI judgment performed within the CLI, new semantic rules, new rule parameters, changed rubrics, or alteration of bounded normal gate-local sampling.
- No source-quality score, population defect-rate estimator, probabilistic guarantees, invented severity classifications or defect-free certification.
- No global total-subject ceiling, giant single prompt, elaborate background job scheduler, distributed workers, database service, remote publishing, CI workflows, or host infrastructure provisioning.
- No restoration of the abandoned hazard/age sampling implementation removed by merged M0021.

## Decisions and Constraints

### Authorization and entrypoint

- `dotnet hygiene` remains the normal autonomous correction lifecycle. `dotnet hygiene --yolo-all` is a **separate** scan path, not an alias for `check --review-all` and not a prelude to ordinary bootstrap, update or remediation.
- The initial unconfirmed `--yolo-all` invocation returns a compact explicit question: **“Were you explicitly tasked by the user to perform the exhaustive read-only audit?”** and does not start a scan. If yes, the calling agent issues `dotnet hygiene --yolo-all --attest-explicit-task [--report-dir <path>]`; if no, it stops. The CLI records the assertion, not independently verified identity. No persistent globally reusable authorization token is required.
- If the profile is missing/unsupported, fail safely and explain the read-only limitation. Do not initialize a profile as an implicit side effect. Use only rules reliably enabled under the current supported profile/configuration.

### Rule and judgment semantics

- All **enabled** rules are evaluated. Mandatory rules participate. Disabling one optional rule excludes only that rule; it must not disable any other rule’s eligible subjects. Generated/unsupported/ineligible source exclusions continue to apply.
- A deterministic rule can produce findings or a hypothetical rewrite delta, but no delta is committed. Existing ignore records are informational annotations only in this audit, not suppression. Each supported deterministic capability is checked, never applied.
- Six current semantic rules preserve their fixed identities, questions, eligibility and review-context requirements. Enumerate **every** eligible rule/subject pair, including unchanged subjects and those previously accepted during normal gates; do not use the five-per-rule selector or normal acceptance state to avoid obligations.
- The external agent decides `acceptable`, `problem`, or `uncertain` for each semantic obligation; only a recorded actual agent judgment counts. Each `problem` or `uncertain` must include concrete reason/evidence. The CLI must not infer answers or generate gold labels.
- The agent’s emitted task is **inspect and report only**. No fix, code change, refactoring, positive normal acceptance, user notification, planner handoff or remediation is requested by YOLO. A reported concern is not a request to act.

### Bounded report-only protocol

```powershell
dotnet hygiene --yolo-all                                       # asks for explicit-task attestation
dotnet hygiene --yolo-all --attest-explicit-task                 # creates local audit inventory
dotnet hygiene --yolo-all --attest-explicit-task --report-dir <path>
dotnet hygiene audit next --run <audit-dir>                      # next bounded semantic work order
dotnet hygiene audit record --run <audit-dir> --file <judgments.json>
dotnet hygiene audit finish --run <audit-dir>                    # report.json + report.md
```

- The protocol above fixes the agent-facing command semantics; implementation chooses internal file layouts, bounded-batch defaults, text/JSON console presentation and atomic commit mechanics without weakening the contract. All intermediate files are **audit output**, not normal hygiene state.
- Each submitted judgment is tied to its unique audit/run identity and independently selected rule/subject obligation. Duplicates, unknown IDs, missing required fields and stale/cross-run submissions fail atomically. Batches remain small; total population/report size is not arbitrarily capped.
- The run inventories one finite observed population and retains sufficient local audit context to support resumed work. Checkpoint only completed report judgments; a restart resumes unjudged inventory. **Do not implement source-change tracking, per-subject invalidation or dynamic rescanning.** Capture initial Git HEAD when available, start time and known dirty indicator; document that external source changes during the scan are not tracked and report may contain observations from a mixed time interval. No guarantee of an atomic source snapshot.
- Audit-local artifact writes must not overwrite unrelated existing output and must survive a process interruption without a false complete result. One local report directory per audit, default `.hygiene/audits/<audit-id>/`, with optional explicit local `--report-dir`. Audit outputs are not passed to normal source scanning. No upload/network egress is performed by the product. Embedded source context may be sensitive; document destination responsibility.

### Reporting and status

- Final audit files are `report.json` and `report.md`. JSON preserves the complete obligation outcome accounting, deterministic findings, problem/uncertain rationale and source/rule identity; Markdown emphasizes problems, uncertainties, per-rule coverage and limitations, rather than repeating every positive judgment.
- Counters must reconcile exactly, per rule and overall: `eligible = acceptable + problem + uncertain + unassessed` for semantic reviews. Deterministic rules report applicable checks and emitted findings separately; never combine unlike counters without labeling units. Disabled/excluded rules and zero-population enabled rules remain explicit.
- A fully assessed audit with concerns is `report-complete`, not `clean`. Partial or interrupted audits remain `incomplete`; authorization/prerequisite failures are separately explained. A complete scan is not proof of quality, an absence-of-defects guarantee or a population-quality estimate.

### Validation, scope and current baseline

- `main` currently includes M0020 reusable fingerprinted validation stages, M0021 legacy sampler removal, and M0022 Guide 0.9.3 proportional validation policy. Do not reintroduce legacy sampling state or rerun all historical 17-minute release validation by default.
- Development uses affected Core/CLI tests. Milestone closure requires explicitly declared gates and **a newly packed installed .NET tool consumer** for the public `--yolo-all` command. Use M0020 stage receipts/plan/verify only when proven current and relevant; scope changes to validation stages conservatively. If a required gate cannot run on Windows 11/.NET 11, report BLOCKED with missing evidence rather than asserting equivalence.
- No deployed-user compatibility guarantee exists before 1.0. Preserve existing source/user data and normal successful behavior; do not add needless compatibility adapters.

## Baseline Executor Readiness

The guide 0.9.3 `base + cli-tool` baseline executor can implement this coherent product feature from the live repository without inventing a material authorization, audit, mutation or reporting policy. Local implementation choices are data structures, buffering, command handlers, atomic report serialization, tests and bounded work-package decomposition. No experimental calibration, model-judgment gold data or new framework is prerequisite.

## Decision Preservation

The project’s user-facing decision is **full read-only problem discovery**. The deliberate exclusions of remediation and source-dependency tracking must not be mistaken for omissions to “fix” by the implementer. Historical reviews, ignored findings and previously accepted semantic observations do not reduce audit coverage. The report is the only product result; intermediate checkpoints are report fragments, not a new durable hygiene acceptance history. Reuse the current rule implementations and 0.9.3 validation discipline; no planning chat or external guide repository is required.

## Execution Tractability

Planning seeded `.execution/M0019-exhaustive-read-only-hygiene-audit.md` with all separately verifiable obligations and distinct evidence cases below, plus the required validation gates. Implementation derives bounded work packages, attaches criterion-specific evidence and runs only required current acceptance gates before seeking independent milestone review. A >50,000-obligation synthetic scale test validates software transport/accounting, **not** subjective semantic correctness.

## Required Authority

- `AGENTS.md`; `docs/ENGINEERING.md`; `docs/ARCHITECTURE.md`
- `docs/specs/HYGIENE.md`; `docs/specs/RULE-CAPABILITIES.md`; `docs/specs/PROFILE.md`
- `docs/specs/SAMPLING.md`; `docs/specs/SEMANTIC-REVIEWS.md`; `docs/specs/REVIEW-HANDOFFS.md` (rule rubrics, population, normal state; audit must stay separate)
- `docs/specs/REWRITES.md`; `docs/specs/AUTONOMOUS-WORKFLOW.md`; `docs/specs/AGENT-HELP.md`
- `docs/milestones/M0020-efficient-resumable-validation.md`; `docs/milestones/M0021-remove-legacy-hazard-sampling.md`; `docs/milestones/M0022-guide-093-validation-policy-migration.md` (applicable already-merged architectural/validation boundaries)
- `eng/validate.ps1`; `eng/ValidationStages.ps1`; existing Core/CLI tests; current packaged tool project

## Acceptance Criteria

- **AC-01** — The installed canonical `dotnet hygiene --yolo-all` command is separate from normal `dotnet hygiene` and `check --review-all`: without a run-specific affirmative `--attest-explicit-task` declaration it asks whether the calling agent was explicitly tasked by the user and does not start a scan; denial or absent attestation never initiates one. The attestation is a caller assertion, not user authentication.
- **AC-02** — An authorized YOLO run is strictly read-only with respect to repository sources, Git/index, hygiene profile/configuration, generated EditorConfig/MSBuild projections, formatter output, review acceptance, ignore decisions, normal latest-run/gate-review state and host tool installation. It never invokes automatic bootstrap/profile update/tool update, mutation, reviewer handoff or repair. Only explicitly scoped audit output files may be written.
- **AC-03** — The audit inventories all enabled and mandatory hygiene rules in current catalogue order, uses each rule's existing eligibility and fixed interpretation, and covers the complete supported repository population irrespective of diff/PR gate, normal five-item limits or unchanged historical acceptances. Disabled rules and unsupported/generated subjects are excluded with accurate rule-by-rule counts, including zero-population rules.
- **AC-04** — Deterministic hygiene rules run in diagnostic/check-only mode over their full applicable population, including formatter/normalizer change proposals when those rule capabilities are enabled; they report actionable findings or would-change descriptions without applying fixes. Ordinary stored ignore decisions do not hide results; existing ignored status may be annotated but is not treated as validation or suppression.
- **AC-05** — All six currently supported semantic-review rules, when enabled, enumerate every eligible subject without sampling, gate-local prioritization, suppression by accepted-review observations, or cross-rule deduplication. The same source or declaration may create distinct obligations for multiple rules, preserving independent rubric and verdict.
- **AC-06** — The external calling agent evaluates semantic subjects using each rule's exact questions and context and records one of `acceptable`, `problem`, or `uncertain` per rule/subject obligation, with rule-specific reasoning/evidence for `problem` and `uncertain`. Neither the CLI nor its report fabricates model judgments, requests code changes, records normal acceptance, or requires remediation/escalation actions; uncertainty is an audit outcome.
- **AC-07** — The audit has no source-change tracking, source-dependency graph, automatic invalidation, rescanning after a judgment, or repair/recheck loop. A run inventories a finite observed population with source context used for its judgments and reports its starting revision/time and snapshot-consistency limitations; it never promises a continuously current or atomic Git snapshot.
- **AC-08** — An audit produces bounded, context-rich review work orders through a discoverable `dotnet hygiene audit next --run <audit-dir>` interface and accepts strictly validated judgment submissions through `dotnet hygiene audit record --run <audit-dir> --file <judgments.json>`. Each independent obligation is unambiguously addressable; missing, duplicated, unknown, cross-run, or malformed judgments cannot count toward coverage. No arbitrary total population or report-size ceiling is imposed.
- **AC-09** — Long audits retain only audit-local, atomic report/intermediate records so that an interrupted agent can resume remaining unjudged obligations without repeating completed judgments or altering normal hygiene state. A resumed run uses the original observed inventory/context, does not dynamically track source changes, and reports that limitation. Concurrency and corrupted or partial output cannot fabricate completeness.
- **AC-10** — `dotnet hygiene audit finish --run <audit-dir>` creates a machine-readable JSON report plus problem-oriented Markdown report, both under the selected audit directory. Both report exact per-rule eligible/assessed/acceptable/problem/uncertain/unassessed counts, all deterministic findings and semantic problem/uncertain entries with source location, rule/version, evidence/rationale, enabled/excluded rule facts, audit/source/tool identity, and any limitations; the Markdown summary need not repeat every positive judgment.
- **AC-11** — Audit completion and quality are separate: a fully assessed audit with findings is `report-complete` (not `clean`); missing/invalid judgments or interrupted work is `incomplete` with honest coverage and an actionable report-only continuation path; authorization-required and unsupported prerequisites have distinct machine-readable status/exit semantics. Never claim repository-wide defect freedom, statistical confidence, or user approval based solely on an exhaustive scan.
- **AC-12** — The default audit directory is a clearly named `.hygiene/audits/<audit-id>/` report artifact root; an explicit `--report-dir` may select another local destination. The tool does not overwrite an existing unrelated run, silently erase prior audit output or export source context to a remote destination. Audit artifacts are excluded from source discovery and normal hygiene state, and source-embedding/transport sensitivity is documented.
- **AC-13** — The design handles a synthetic full inventory with more than 50,000 independent semantic obligations without the normal gate-review observation cap, arbitrarily dropping subjects or producing one unbounded agent prompt. The software can stream or partition its inventory, work orders and judgments; batch size limits only each request, never total coverage.
- **AC-14** — The existing normal no-argument lifecycle, six independent bounded gate-local semantic rules, `check --review-all`, profile/rewrite semantics, explicit normal `review accept`, ignore/handoff behavior and M0021 removal of legacy hazard/age sampling remain unchanged. No obsolete sampling support or compatibility shim is reintroduced; M0020 validation behavior remains valid.
- **AC-15** — An absent, malformed or unsupported hygiene profile/configuration or unavailable project/runtime dependency stops the read-only audit with a truthful diagnostic and without silently bootstrapping, reconciling, changing configuration, or replacing prior output. A valid supported initialized profile is required for authoritative enabled-rule evaluation; baseline acceptance is not silently fabricated or bypassed.
- **AC-16** — A freshly packed current `DotNetAiCodeHygiene.Tool` (target release 0.11.0 unless newer approved main version requires reconciliation) is installed into an isolated Windows 11/.NET 11 consumer through the real `dotnet-hygiene` shim and proves authorization, full read-only audit, bounded semantic judgments and report output, including source/state byte preservation. Source-project-only tests do not satisfy consumer validation.
- **AC-17** — A deterministic verification pass proves that audit output contains precisely one recorded outcome for each discovered rule/subject obligation, counts agree with the underlying audit records, all semantic problem/uncertain entries have rationale, deterministic findings are preserved, and reporting does not silently omit unresolved or zero-work rule states. Mechanical validation never assigns subjective gold-label quality judgments.

## Acceptance Evidence Topology

| ID | Parent obligation | Required evidence case | Why separate evidence is required |
|---|---|---|---|
| EC-01a | AC-01 | Initial invocation without explicit attestation and explicit denial do not start work or produce scan artifacts | Authorization negative path |
| EC-01b | AC-01 | Explicit affirmative run-specific attestation starts the audit and is reported as an assertion rather than authenticated consent | Authorization positive path |
| EC-02a | AC-02 | Successful full audit preserves all preexisting source/index/profile/normal hygiene state bytes and does not invoke formatter, bootstrap or package update mutations | Positive read-only path |
| EC-02b | AC-02 | Invalid input, failure and interruption do not modify source or normal hygiene state; only audit-local partial output may remain | Error/abort read-only path |
| EC-03a | AC-03 | Enabled versus disabled and mandatory rules are accounted for independently, including a rule with zero eligible subjects | Rule participation and empty population |
| EC-03b | AC-03 | Multiple SDK projects are included, while generated/unsupported/noneligible sources are excluded without losing supported subjects | Project and source eligibility boundary |
| EC-04a | AC-04 | Deterministic rule findings and formatter/normalizer would-change diagnostics appear without applying changes | Analysis versus mutation |
| EC-04b | AC-04 | An existing normal ignore does not suppress the exhaustive deterministic finding and may be annotated only | Normal ignore state boundary |
| EC-05a | AC-05 | More than five eligible subjects per semantic rule are enumerated, including unchanged previously accepted subjects | Sampling/accepted-history bypass |
| EC-05b | AC-05 | One file/subject eligible under multiple semantic rules yields distinct rubric-bearing obligations rather than one merged judgment | Cross-rule independence |
| EC-06a | AC-06 | Acceptable, problem and uncertain semantic outcomes are retained distinctly and problem/uncertain reasons are required | Outcome semantics and evidence |
| EC-06b | AC-06 | Produced instructions and audit outputs contain no remediation/escalation commands or normal acceptance writes and no synthetic model verdicts | Agent workflow nonaction boundary |
| EC-08a | AC-08 | Bounded next-work order covers a full larger population across successive batches without omission or double allocation | Chunked exhaustive transport |
| EC-08b | AC-08 | Duplicate, unknown, cross-run and malformed judgment submissions are rejected atomically without increasing covered counts | Judgment ingestion failure paths |
| EC-09a | AC-09 | Interrupted audit resumes its original remaining obligations with previously recorded outcomes intact | Resume and checkpoint positive path |
| EC-09b | AC-09 | Truncated/corrupt or concurrently modified audit artifacts cannot be certified or silently treated as a finished report | Atomic/corruption/concurrency negative path |
| EC-10a | AC-10 | Completed report JSON and Markdown reconcile exact per-rule counts, findings and problem/uncertain rationale | Full report consumer path |
| EC-10b | AC-10 | Incomplete report explicitly counts pending subjects and retains zero-work and excluded-rule accounting | Partial report consumer path |
| EC-11a | AC-11 | Fully assessed audit with discovered problems is report-complete, not clean; incomplete and authorization-required remain distinguishable | Coverage versus quality versus authorization |
| EC-12a | AC-12 | Default and explicit report paths create only scoped audit artifacts; existing unrelated output cannot be overwritten | Output isolation and existing artifact safety |
| EC-13a | AC-13 | More than 50,000 synthetic semantic obligations complete counting/chunking without hitting the ordinary acceptance-store cap | Large population and total-cap boundary |
| EC-14a | AC-14 | Normal gate, bounded --review-all and existing acceptance/ignore state remain unchanged after YOLO use | Regression of independent ordinary lifecycle |
| EC-15a | AC-15 | Missing and incompatible profile/dependency conditions fail safely without bootstrap or source/profile mutation | Prerequisite negative path |
| EC-16a | AC-16 | Exact freshly packed installed 0.11.0 consumer invokes dotnet hygiene --yolo-all through real generated shim and produces a report | Distribution/installed command boundary |
| EC-17a | AC-17 | Tampered or missing per-item records, incorrect totals or absent problem/uncertain evidence cannot be certified as complete | Final reconciler fail-closed boundary |

## Validation

Acceptance depth and execution location are distinct. Per Guide 0.9.3 and merged M0022, execute only the defined current gates at closure, not a blanket cold historical full-release suite. Reuse M0020 receipts only when matching current inputs and behavior coverage. Tier-4 installed-tool proof is **mandatory** because M0019 adds a public command and package behavior. M0013/M0015 simulation scripts are unrelated investigative tools, never default regression.

| ID | Depth / target | Execution locus/platform | Command/check | Proves | Expected evidence |
|---|---|---|---|---|---|
| VAL-01 | Tier 1 — focused rule enumeration and deterministic check-only regressions | Windows 11 + .NET 11 SDK; real SDK-style Roslyn/Git fixtures | Focused Core tests using `./eng/validate.ps1 -Shard core-tests -TestFilter <relevant-m0019-filter>` when a matching filter exists, or explicit named Core test invocation; record executed test names and current inputs | AC-03, AC-04, AC-05, AC-07, AC-14, AC-15; EC-03a, EC-03b, EC-04a, EC-04b, EC-05a, EC-05b, EC-14a, EC-15a | Actual nonmutation, inclusion/exclusion, sampling-bypass and ordinary-path regression evidence |
| VAL-02 | Tier 1/3 — real CLI authorization, judgment transport and audit-report fixtures | Windows 11 + .NET 11 SDK + PowerShell + Git; isolated temp repositories and output locations | Focused CLI process tests via M0020 CLI shard/filter or direct CLI test selection; real command invocations with read-only source/hash assertions | AC-01, AC-02, AC-06, AC-08, AC-09, AC-10, AC-11, AC-12, AC-17; EC-01a, EC-01b, EC-02a, EC-02b, EC-06a, EC-06b, EC-08a, EC-08b, EC-09a, EC-09b, EC-10a, EC-10b, EC-11a, EC-12a, EC-17a | CLI status/exit, bounded work orders, judgments, partial/complete reports, evidence/counts, no repository mutation |
| VAL-03 | Tier 1/3 — large mechanical audit and interrupted resume | Windows 11 + .NET 11 SDK; isolated generated synthetic populations; no subjective reviewer model | Focused non-release test/fixture for >50,000 independent rule/subject obligations, interrupted run, checkpoint and finish verifier | AC-08, AC-09, AC-13, AC-17; EC-08a, EC-09a, EC-09b, EC-13a, EC-17a | Population/count receipts, bounded batches, no cap, restart and corruption rejection; no gold-label judgments |
| VAL-04 | Tier 4 — current installed-tool audit consumer | Windows 11 + .NET 11 + Git + NuGet local feed; disposable installed tool and repo | Pack exact current 0.11.0 artifact (or authorized reconciled version), install with `dotnet tool install --tool-path` from isolated local feed, run `dotnet hygiene --yolo-all` handshake/attested scan, submit judgments and finalize report; record SHA-256 provenance. May add a scoped M0020 consumer test; no unconditional cold full historical release matrix | AC-01, AC-02, AC-10, AC-11, AC-15, AC-16; EC-01a, EC-01b, EC-02a, EC-10a, EC-11a, EC-15a, EC-16a | Installed shim, exact nupkg identity, report hashes, source/index/profile state hashes, truthful status and no mutation |
| VAL-05 | Tier 0/2 — project authority, ledger reconciliation and current evidence verification | Repository checkout with Git and PowerShell; established Windows 11/.NET 11 validation locus for changed code | `git diff --check`, targeted docs/help/examples and seeded ledger ID/parent/wording reconciliation; use M0020 `-PlanJson`/`-Verify` and affected stages only where current and relevant. Do not treat unrelated stale full-release stages as newly executed | AC-14, AC-17, DOC-01, DOC-02; EC-14a, EC-17a | Explicit current obligation/case-to-case evidence and status; no invented complete release qualification |
| VAL-06 | Tier 5 — independent M0019 human review | M0019 PR, milestone, ledger and review packet | Authorized owner/delegate reviews read-only invariant, exhaustive coverage, safety, consumer artifact, reporting claims and reconciliation; decision recorded explicitly in `.review/REV-M0019-COMPLETION.md` | REV-01 | Pending approval until actual authorized decision; no self-approval |

## Human Review

Human review is **required and blocking** only for M0019. Canonical review artifact: `.review/REV-M0019-COMPLETION.md`. Authorized project owner or explicitly delegated approver reviews the actual report-only behavior, exhaustive coverage semantics, safety evidence, current installed-tool consumer, ledger reconciliation and scope. Implementer prepares the packet with `Decision: pending`; no self-approval or invented project approval. If the repository has no documented M0019 review-check command, do not invent one. Historical approvals for M0018/M0020/M0021/M0022 are not reopened.

## Documentation Policy

- **DOC-01** — Update only directly affected operative specifications, CLI help, README and milestone index to describe the separate, read-only `--yolo-all` report workflow, explicit attestation, bounded judgment submission, report semantics, privacy scope and contrast with normal `dotnet hygiene`/bounded `--review-all`; retain M0021/M0022 historical records untouched.
- **DOC-02** — Document executable M0019 validation instructions and evidence scope in project-local engineering authority, using M0020 staged validation and M0022/Guide 0.9.3 proportionate acceptance; do not assert a full release pass from focused stages or force the unrelated historical cold matrix.

## Other Completion Obligations

- **REV-01** — Prepare `.review/REV-M0019-COMPLETION.md` for blocking independent owner/delegate approval of the read-only scan, exhaustive rule populations, reporting consistency, CLI consumer provenance and evidence reconciliation. Leave the decision pending; do not self-approve or merge.

## Completion Expectations

Read this ready milestone, planning-seeded ledger and referenced operative local authority; reconcile IDs/text/parent relations; choose bounded implementation work packages; implement with targeted feedback; record executed case-specific evidence and current stage provenance; perform explicit required installed-tool consumer validation; reread, reconcile and complete the mandatory guide audit. Do not claim M0019 complete while review is pending. Do not modify original product source or perform cleanup when *executing the YOLO audit*—that restriction does not prevent the implementer from editing product code to implement M0019.

## Completion Evidence

Implementation result: report-only exhaustive audit is implemented on a new branch from current `main` (`cb9c9e9`, equal to planning baseline). The exact 20 obligation IDs/wording and 25 evidence-case IDs/parents/wording were reconciled against the planning-seeded ledger; all six declared validation-gate definitions were preserved. The execution ledger maps each case to the specific implementation and executed proof.

Current M0020/M0022-proportionate evidence: cold `solution` stage passed with 0 warnings/0 errors; focused VAL-01 Core cases passed for sampling/accepted-observation bypass and normal-state preservation, 50,001-obligation inventory plus complete bounded pagination, and multi-project/generated-source eligibility. Focused VAL-02 CLI cases passed authorization/denial, no-bootstrap prerequisite, ignored finding inclusion, read-only hashes, rationale validation, tri-state recording, invalid/malformed/cross-run/duplicate rejection, incomplete-to-resumed report collection, report count reconciliation, corruption refusal, default/explicit report paths, and read-only destination collision. README lifecycle policy and `git diff --check` pass. Focused stages do not claim complete Core/CLI-suite receipts or the unrelated historical release matrix.

VAL-04 installed-consumer provenance: freshly packed `DotNetAiCodeHygiene.Tool.0.11.0` SHA-256 `6D2DB12994458F9D3BFDF7FF2C96E22CB3B6941E6DAAC4D957B260E666B10477`; isolated generated `dotnet-hygiene.exe` SHA-256 `8B167E9C21E80EE74BF25045EC46CCAC298DC22C5CE0240B16BA228AC44AC81E`. The installed `dotnet hygiene` shim required explicit attestation, declined the no-attestation and denial cases without creating an audit, produced an incomplete report after one recorded judgment, resumed with no repetition, and finished `report-complete` at 7/7 semantic obligations with dirty-at-start disclosed. Eight protected source/project/Git-index/profile/config/baseline/normal-state files remained byte-identical. Disposable consumer evidence remains under the system temp directory recorded in `.execution/M0019-exhaustive-read-only-hygiene-audit.md`.

The audit records one finite inventory with HEAD, start time, dirty-at-start state, and the explicit mixed-time/no-rescan limitation. No source edits, normal acceptance changes, formatter/normalizer writes, bootstrap/profile update, tool update, AI-provider call, or repair request is part of its execution. Human review is the remaining blocking gate; no semantic quality claims or approval are inferred from mechanical fixtures.

## Escalation Boundary

Implementation owns local data representations, tests, stream/chunk mechanics and supporting edits consistent with this contract. Return to planning for any proposed automatic remediation, source-change dependency tracking, external model/provider invocation, profile auto-bootstrap/update side effects, new required validation locus/infrastructure, altered review rubric or change in audit-only output semantics.
