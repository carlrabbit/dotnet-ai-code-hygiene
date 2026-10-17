# Autonomous Hygiene Workflow

## Status and authority

M0018 planning authority for the target autonomous user/agent workflow. The M0018 milestone controls implementation scope and acceptance; the existing rule and review specifications continue to control individual rule semantics until this workflow is implemented. The CLI must not advertise commands as implemented before they exist.

## Product interface

The product is a model-independent .NET tool whose **only supported command namespace is `dotnet hygiene`**, distributed with `ToolCommandName=dotnet-hygiene`. `dotnet hygiene` is the autonomous entrypoint. The calling agent executes the requested instructions; the tool does not launch an LLM, choose an AI provider, or secretly perform model judgments. An ordinary user delegates: "Run dotnet hygiene in the repository and follow its instructions." CLI help/agent guidance defines that workflow without requiring an AGENTS.md edit or a provider-specific skill.

The CLI reports the next concrete action, an actionable reason and the current completion state. The agent can repeatedly call the tool and its subcommands until terminal completion, approval, escalation, or genuine blocker. JSON instructions are structurally consumable; stdout machine-readable output must not contain logging/progress chatter. The user need not select bootstrap or profile update explicitly.

## Four lifecycle modes

### First bootstrap

Missing usable hygiene baseline leads to automatic initial bootstrap. Scope is **all eligible SDK-style C# projects in the repository**, regardless of solution layout, not just the current diff. The workflow establishes/reconciles hygiene-owned MSBuild/EditorConfig/profile state, checks pre-hygiene formatting drift using the original effective project configuration, applies authoritative deterministic hygiene formatting/normalization repository-wide under the adopted policy, validates, and prepares a baseline report/diff. It must not infer that a historical `dotnet format` command ran; it reports observable formatting drift. The pre/post comparison distinguishes pre-existing project-format drift, hygiene-policy-driven changes, and unrepaired findings without pretending to infer provenance perfectly.

Enabled deterministic formatters/normalizers are **authoritative**: they run without a per-edit approval prompt, and changes are idempotent and semantics-preserving under their rule contracts. This does not authorize generic `dotnet format analyzers` fixes or arbitrary analyzers. The hygiene-owned profile defines approved formatting: Roslyn whitespace/layout through hygiene's own formatter under the effective managed policy; `dotnet format style`/other dotnet-format repairs only through explicit hygiene-owned diagnostic allowlists and severity controls. `dotnet format` has no native `--profile` switch: use effective managed EditorConfig and explicit diagnostic selection, never claim otherwise. The pre-hygiene assessment must be nonmutating; post-hygiene changes must be attributable and verification must be executable on the resulting sources. The autonomous run checkpoints eligible C# sources and managed profile artifacts. A failure restores tool-owned changes from that run; if a path no longer matches the last tool checkpoint, preserve it and report the rollback conflict instead of overwriting newer content.

Bootstrap automatically makes the deterministic changes and prepares an **approval-required project baseline** with precise changed paths, rule/policy versions, findings, verification evidence and review instructions. The approver can be a human or explicitly authorized AI. The tool cannot infer a person's/agent's authority. Approval is of the repository baseline, not each formatting edit; the baseline must not be reported as approved before external acceptance. Rejection can be implemented through preserving the unaccepted diff/worktree and letting the caller discard it safely; never use an unsafe blanket `git reset --hard` or remove unrelated work. Tool safety preconditions prevent clobbering staged/unstaged/untracked user data; failure/cancellation must not produce a falsely approved/partly installed baseline.

The CLI records an external baseline decision with `dotnet hygiene baseline accept --approver <identity>`. This is an explicit caller assertion that the named approver was authorized and approved the inspected report; the CLI does not authenticate the approver or manufacture a review result. The command refuses incomplete validation and repeated acceptance.

### Routine gates

For an approved/current supported baseline, the same entrypoint chooses an applicable change gate and applies authoritative deterministic rewrites repository-wide before inspection. Gate precedence: explicit base/context from the invocation or trusted workflow metadata; otherwise available unambiguous local branch/default-branch comparison on a feature branch; otherwise changed worktree relative to HEAD when actual changed C# files exist. Ambiguous committed-branch comparison, missing refs, or contradictory context result in actionable agent instructions to resolve the base; never silently downgrade to a bare `check` that schedules zero semantic reviews or invent a remote. No Git fetch/push/commit is implicit.

The tool evaluates deterministic findings and normal M0016 gate-local semantic selections (unchanged independent per-rule budgets), groups selected **execution work** by file and source declaration when grounded, deduplicates file context, and preserves every rule's questions, escalation class, exact original batch/item references and independent acceptance. The agent decides semantic correctness, remediates contextually, accepts only inspected satisfactory subjects, and escalates unresolved/uncertain subjects. After source changes, rerun and revalidate. Visual-block signals and other review candidates are not automatic defects or formatter mandates. A successful command is not by itself a clean gate.

### Manual diagnostics

Explicit path, rule discovery, explain, `--base`, `--changed`, and `--review-all` inspection remain available in a coherent redesigned `dotnet hygiene` subcommand hierarchy. Manual checking remains nonmutating unless the user/agent explicitly executes an approved remediation operation. It is advanced/debugging functionality, not required knowledge for normal delegation. `--review-all` remains bounded; it must not silently mean exhaustive inspection.

### Release updates

The entrypoint detects and reconciles profile drift automatically. It discovers the latest stable package by installing into a disposable temporary tool path with temporary package caches against the configured NuGet sources. Where that source set and installation scope are known, it starts a separate updater process, exits to release loaded binaries, updates only the existing local-manifest or tool-path installation, restarts the installed command, and emits the revalidation report. Do not mutate the running executable in-process; do not use arbitrary feeds or floating prerelease channels. Do not automatically push, commit, change CI, install machine-wide prerequisites, or elevate privileges. If offline or no stable package metadata is available, report the limitation and continue with a compatible installed binary; if the current profile/tool is incompatible, report a blocker and concrete next action instead of claiming success. Unknown installation scope yields a concrete manual update instruction and never assumes a global installation. Repository profile reconciliation is distinct from package updating. An upgrade that materially changes the accepted baseline produces a new approval-required report, not silent acceptance. Record exact tool/package and profile/rule versions in the report; avoid mid-gate version switching.

## Compatibility policy before 1.0

**There is no pre-1.0 backward-compatibility obligation.** Replace the legacy `hygiene` command rather than maintaining aliases. CLI command structures, JSON schemas, persisted review/sampling/profile representations and internal APIs may change to implement the coherent autonomous workflow. Do not build shims or migrations merely to preserve experimental interfaces. Clear diagnostic/rebootstrap/upgrade actions are required for incompatible state, and source/configuration data may never be silently destroyed. Preserving user work and deterministic error handling are correctness requirements, not compatibility promises. Historic research/evidence stays historical; no accidental import as live approval/review state.

## Terminal outcomes

- `complete`: supported baseline active, authoritative deterministic work and selected gate obligations processed, required applicable verification established; only selected semantic work is certified as *processed*, not the whole population as good.
- `approval-required`: bootstrap or material profile/policy change prepared and verified; explicit designated-approver decision outstanding.
- `escalation-required`: negative/uncertain review or policy conflict needing frontier/planner/project decision.
- `incomplete`: failed validation, unprocessed selected obligations, invalid/ambiguous gate, unavailable dependency, unsafe repo state or other blocked work. Never conflate command exit 0 with `complete`.

The completion report distinguishes generated findings, remediations, reviewed/accepted/reopened/escalated/outstanding subjects, validation results, source/version identity and outstanding approvals. It must remain truthful when the agent cannot finish. Repeated runs should be idempotent after successful remediation; acceptance is explicit and source-bound.

## Explicitly deferred to M0019

`dotnet hygiene --yolo-all` is **not** an M0018 feature. M0019 will define agent authorization/attestation and abort-on-no-authorization, exhaustive per-rule eligible population regardless of ordinary sampling caps, processing of previously accepted subjects, bounded/resumable chunks and true exhaustive coverage reporting. M0018 must not partially implement YOLO by conflating it with `--review-all` or creating unbounded single-prompt output. Design normal output so a later exhaustive mode can use it without introducing a universal rules DSL/pipeline.
