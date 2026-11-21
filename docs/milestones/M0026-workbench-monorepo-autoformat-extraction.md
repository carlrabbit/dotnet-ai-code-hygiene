# M0026 — Workbench Monorepo Foundation and Standalone Autoformat Extraction

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | **ready** (planning handoff; implementation not started) |
| Mode | `ai-executed-human-reviewed`, coherent architectural migration |
| Guide applicability resolved by planning | Agentic Project Guides 0.9.3; repository-wide `base` + `cli-tool` |
| Repository role/maturity | developer-tool, greenfield/pre-1.0 |
| Baseline implementation model | project-configured **GPT-5.6 Luna** |
| Baseline executor readiness | confirmed: product identity, scope boundaries, CLI/profile ownership, validation and migration resolved below |
| Decision preservation | confirmed: workbench monorepo, asymmetric transitional dependency, formatter ownership and deferments are explicit |
| Execution tractability | confirmed: bounded acceptance/evidence registry; implementation chooses work packages and persistent progress |
| Execution ledger | `.execution/M0026-workbench-monorepo-autoformat-extraction.md` |
| Source baseline | `carlrabbit/dotnet-ai-code-hygiene` `main` including merged M0025 PR #28, verified at commit `97ee3e13a584f2e9f914120666a77b38a565becc` (2026-11-21); implement on this commit or descendant containing it |
| Related planning research | [AI Workbench Direction PR #29](https://github.com/carlrabbit/dotnet-ai-code-hygiene/pull/29) is **open**, not required implementation authority; this milestone stands alone even without PR #29 merge |
| Target | Windows 11, .NET 11 SDK, Git, PowerShell, Roslyn, SDK-style C# projects, isolated real `dotnet tool` consumers |
| Environment authority | Existing SDK/Git/PowerShell/NuGet, restore and isolated temp Git worktrees/feeds/tool paths permitted; **no** persistent provisioning, admin, new CI/workflows, service creation, credentials or repo rename |
| Focused validation | New Autoformat tests plus existing Core/CLI focused shards on Windows 11 |
| Repository / consumer validation | Extended resumable `eng/validate.ps1` plan and two exact-current installed-tool surfaces, at final evidence boundary |
| Human review | **blocking**, independent milestone-completion review, `REV-M0026-COMPLETION` |
| Documentation sync | Directly contradicted authority and consumer help now; unrelated historical documentation sync deferred |

## Goal

Turn the current mixed `dotnet hygiene` tool into the **first two independently executable surfaces of a .NET AI Workbench monorepo**, delivering Autoformat as a real, independently installed .NET tool now. Preserve M0025's completed semantic-review investment behind a transitional executable without designing or advancing Autoreview; the future index and Autoreview product remain separate workstreams.

This milestone establishes the extraction boundary and **working Autoformat parity**, not the next visual-canonicalization feature portfolio. The owner deliberately wants freedom to break pre-1.0 implementation/API compatibility; source safety and truthful treatment of retained user/review state are not optional.

## Target State

The existing single GitHub repository builds a monorepo solution containing an independent, packed `dotnet autoformat` executable plus the still-working `dotnet hygiene` legacy executable. Autoformat can initialize a fixed mechanical policy and run the established SDK style + whitespace normalization without initializing review state or running semantic assessments. Former hygiene formatting routes reuse the **same** formatter engine rather than a duplicated algorithm.

The repo is ready for additional Autoformat canonicalizers later, and for eventual Autoreview extraction and workspace index creation **only in separate milestones**. There is no host/plugin/index runtime or network service. Each installed tool is verifiably runnable on its own.

## Scope

- Establish the **workbench monorepo identity within this repository**: root solution, project boundaries, build/pack/validation and targeted authoring documentation. Keep GitHub repository `carlrabbit/dotnet-ai-code-hygiene` and no external repo move/rename in M0026.
- Extract the SDK-backed formatter, its mechanical profile ownership, target selection and Git mutation safeguards from the mixed hygiene host into an independent Autoformat engine and CLI. Extract only actually shared low-level project/solution/Git services if necessary, without building a universal workspace framework.
- Keep the original executable functioning as a transitional legacy hygiene/review host. It may consume Autoformat engine services; neither Autoformat executable nor its engine may depend on the legacy host/review engine. Preserve review/audit/approval state and no unintended semantic behavior changes.
- Package/install/test both executables as separate .NET tools. Give Autoformat its own initial package identity/version. Maintain honest independent binary/version/help boundaries; no public feed publication.
- Replace/update the existing validation topology so that builds, tests, current-package packs, installed consumer tests, self-host checks, receipt freshness and final verification cover the new workbench layout.
- Update **operative** architecture/specs/engineering/README/agent help contradicted by the new implementation. Leave archival milestone/research experiments historically truthful.

## Non-goals

- **No SQLite schema, database, indexer, symbol graph, navigation executable, resident service, IPC/HTTP interface, shared cache, plugin architecture or universal tool-host interface.**
- **No new Autoreview standalone executable, semantic rule work, changed selection budgets, AI provider integration, reviewer policy, or new semantic correction batching.** The existing transitional `dotnet hygiene` review remains functional.
- **No new XML documentation formatting, canonical comment wrapping, extra blank-line policy, Razor Pages relocation, MSBuild project rearrangement of consumer repositories, or speculative additional IDE code fixers.** Those are follow-on Autoformat work, not hidden M0026 requirements.
- No new general framework for deterministic rules or source transformations. No source-to-source LLM rewriting, silent semantic refactoring or regeneration of documentation meaning.
- No GitHub repository rename/split; no CI/host provisioning or new remote service; no public NuGet publishing, release qualification campaign, or cross-platform-support claim.
- No attempt to carry over old `dotnet hygiene` command/JSON compatibility as a product guarantee forever; **within M0026**, preserve the current review behavior as a regression boundary.
- No automatic deletion/reinterpretation of historical research, `.hygiene` review state, accepted observations or current audit artifacts.

## Resolved Architecture and Behavior

### 1. Executable/tool shape

Use one repository and one root workbench solution (name `DotNetAiWorkbench.slnx`). One or more source projects implement **Autoformat engine** and **Autoformat CLI**; package ID is `DotNetAiWorkbench.Autoformat.Tool` starting at `0.1.0`; `ToolCommandName=dotnet-autoformat` exposes `dotnet autoformat`. Keep the old `DotNetAiCodeHygiene.Tool` package and `dotnet-hygiene` shim at its own existing version lineage (`0.12.0` baseline) during transition. Neither package requires the other package to be installed. They may share compiled libraries in their respective distributable artifacts.

The original `DotNetAiCodeHygiene.Core` and CLI may remain under their current names during this migration; **do not require a new Autoreview executable, project-wide namespace rename, or synthetic scaffold**. The implementer may reorganize physical projects to enforce the dependency direction, but the resulting solution must keep the executable/module distinction visible.

Dependency rule:

```text
dotnet-autoformat -> Autoformat engine -> necessary SDK/Roslyn/Git services
                                           (optional small shared library)
legacy dotnet-hygiene -> legacy review/lifecycle code -> Autoformat engine
NO Autoformat -> legacy hygiene/review, audit, review ledger or index dependency
```

No plugin discovery, central host or mandatory startup order. The legacy CLI is explicitly **transitional**; its replacement with a dedicated Autoreview executable is a separate design and migration milestone.

### 2. Autoformat CLI and behavior

Define these supported forms for the first independent tool:

```powershell
dotnet autoformat init [--output text|json]
dotnet autoformat [paths...] [--check] [--changed] [--output text|json]
dotnet autoformat format [paths...] [--check] [--changed] [--output text|json]
dotnet autoformat normalize [paths...] [--check] [--changed] [--output text|json]
dotnet autoformat --help
dotnet autoformat --version
```

No-argument/default mode equals `normalize`: apply the fixed allowlisted SDK style fixes followed by whitespace. `format` is whitespace-only. `--check` verifies the selected mode without writing. `--changed` selects staged/unstaged/untracked eligible changes and **implies read-only check**, including when explicitly combined with a mode. An explicit path narrows the selected eligible files, and invalid/unsupported paths fail or are reported according to existing scope-safety behavior rather than being silently reformatted by a broad fallback. `init` only establishes/validates policy, not a source-format run. Text and JSON report real selected/changed paths, mode, read-only status, approval/configuration/conflict information and incomplete errors; never invent precise per-diagnostic causal attribution.

This is the new tool's **published behavior from M0026**; no new generic configuration parameter bag and no automatic NuGet update side effect. Version and help identify **Autoformat**, not semantic review. It never runs review discovery or writes review/audit state.

### 3. Formatting ownership and supported policy

Preserve the **existing fixed `dotnet-11 v3` SDK mechanical-policy semantics** for supported C# inputs: `dotnet format style` with the exact current approved diagnostics from [REWRITES](../specs/REWRITES.md) (`IDE0005, IDE0011, IDE0036, IDE0040, IDE0090, IDE0034, IDE0004, IDE0037`) followed by `dotnet format whitespace`; no `dotnet format analyzers`, no arbitrary other fixers. The effective `.editorconfig` policy is required and remains opinionated rather than repository-adjustable by per-rule options.

**Single ownership**: the Autoformat engine owns the mechanical EditorConfig block generation/checks and SDK invocation. The legacy hygiene host may delegate to this engine but must not retain a separately divergent formatter policy/fixer inventory. The legacy hygiene host continues to own nonformatting analysis/profile, review and baseline acceptance policy; its existing obligations must remain operational through a safe adapter/transition.

Use a **separate, narrowly owned Autoformat marker** in root `.editorconfig` for new initialization. In a repository with a supported existing `# hygiene profile:begin/end` block, `autoformat init` safely adopts/transfers the mechanical settings to one owner without losing unrelated settings. Do not duplicate conflicting owned blocks or silently overwrite user-authored nested policies. Existing hygiene profile users must remain runnable during transition and must not be forced through a destructive reset/approval fiction. The precise marker spelling, content update mechanics, and adapter implementation are local implementation choices; the ownership/invariants are not.

Fresh `autoformat init` needs no `.hygiene/profile.json` marker, baseline-acceptance record, review ledger, or prior `dotnet hygiene` install. It is idempotent and reports the configuration diff. `autoformat` source mutation **does not automatically create/reconcile policy or claim approval**: without effective supported initialization it fails with a clear `init` instruction. Policy initialization/migration is a distinct explicit action; no uncontrolled source rewrite from `init`. Autoformat does not inherit the legacy hygiene human-approval workflow. Existing authored custom `.editorconfig` content is preserved; conflict is explicit, not resolved by destroying the user's preference.

### 4. Safety and source scope

Use the established Git checkpoint contract for **all mutating formatting**: known `HEAD`, clean relevant tracked/untracked worktree/index at entry, and detection of index/HEAD changes during the run. SDK source changes are ordinary writes, **not** a transaction. Failures/cancellation may leave changed files; report incomplete and paths, never reset/clean/commit/revert silently. Read-only mode never alters source or tool-owned review state. Tool-owned metadata/config writes use safe per-file behavior; init refuses dirty/ambiguous or conflicting managed state instead of overwriting unrelated user work.

Unscoped normalization covers **all supported project C# source** independent of any semantic review sampling/change gate. Check-only supports the same selected population. Preserve multiple SDK projects, multiple solutions, linked/shared file eligibility, generated exclusions, and handling of unsupported SDK/workspace/restore failures as far as currently supported by `RewriteEngine`/profile authority. Source-owned `hygiene:retain modernization...` comments and existing IDE pragma suppressions must still have their intended effects on approved SDK changes; formatting may not silently strip them.

The extracted engine should support eventual further deterministic canonicalizers, but M0026 does not introduce a policy DSL, per-rule formatter process, or speculative `IFormatterPlugin`.

### 5. Transitional review boundary

The legacy `dotnet hygiene` continues to own and exercise the M0025 portfolio (17 general semantic-review rules, shared ten-obligation budget, nine modernization families' five-package budget), explicit review acceptance/handoff, and exhaustive `--yolo-all` audit with its separate authorization. Keep existing ability to run `dotnet hygiene format`/`normalize` and the autonomous formatting phase **by delegating to the extracted Autoformat engine**, without constructing/initializing review state from Autoformat calls.

Do **not** refactor the semantic rules merely because the monorepo now has a future Autoreview boundary. Retain deterministic test fidelity on their discovery, state, handoff and audit paths; avoid replaying subjective LLM-verdict tests.

Data separation: Autoformat's policy/configuration is not approval to accept a review batch; prior `.hygiene` accepted-review observations and audit artifacts are not disposable index data and are neither migrated into a new database nor deleted. The legacy profile/baseline approval remains a legacy concern; the independent Autoformat CLI must neither require it nor claim its approval.

### 6. Validation topology and release boundaries

A single monorepo build/validation plan covers all current executable projects without forcing them to share package versions or runtime process. The new tool's `0.1.0` and the legacy product's current `0.12.0` are **independent local package identities**; version bumps and public publication are outside this milestone.

Adapt `eng/validate.ps1` and `-PlanJson` to make new Autoformat tests and package inputs first-class, retain existing Core/CLI stage IDs or documented equivalent coverage, and make the existing installed-consumers stage prove both current packed artifacts. Stage receipts must fingerprint actual inputs/output hashes of both executable packages. `-Verify` must refuse a missing/stale new-product receipt, not report green because the old monolithic stages pass. Keep sharded/resumable verification and snapshot-bound provenance; do not replace it with a forced full suite on every code edit.

During implementation: use focused Autoformat/legacy tests and relevant shards; **run the required installed-consumer and self-host acceptance stages after the executable/package surfaces have stabilized**, then verify all current receipts. Running full qualification early is diagnostic, not proof of finished work. Tests should establish actual deterministic outcomes (exact source diff/eligibility, config ownership, CLI semantics, idempotence, fault safety), not synthetic agent opinions.

Existing `eng/review-check.ps1` explicitly supports M0024/M0025: extend the milestone set for M0026; independent review acceptance cannot be manufactured by an implementer.

### 7. Documentation and migration authority

The implementation updates `docs/ARCHITECTURE.md`, relevant `docs/specs/`, `docs/ENGINEERING.md`, `README.md`, agent-help and validation guidance **where their current claims become false**. It must explain that the GitHub repository name remains historical while the internal solution is `DotNetAiWorkbench.slnx`. Documentation should clearly separate current executable support from future Autoformat visual rules, Autoreview extraction and SQLite indexing; research is rationale, not implementation contract.

Follow the project’s separate broad documentation-sync policy; do not rewrite all historical milestones or perform unrelated glossary churn. The future workbench index and Autoreview projects will receive separate planning decisions, profiles and milestones when ready.

## Baseline Executor Readiness and Decision Preservation

All material product decisions needed for this extraction are fixed above: one monorepo, independent executable/package, one-way legacy adapter, CLI shape, existing SDK allowlist, init/config ownership, mutation safety, legacy review preservation, blocked deferred work, installed-tool validation and release scope. The baseline model can choose local C# types, layout, migration implementation, test structure and bounded execution work packages without deciding the product architecture.

The milestone does **not** rely on PR #29 research as implementation authority. If its research PR merges first, use it for provenance only; these operative decisions remain in this milestone until implemented and promoted to durable architecture/specs.

## Required Project Authority

- `AGENTS.md` — implementation routing.
- `docs/ARCHITECTURE.md` — existing mixed engine/session and current result/report behavior to preserve or explicitly supersede.
- `docs/specs/REWRITES.md`, `docs/specs/PROFILE.md`, `docs/specs/AUTONOMOUS-WORKFLOW.md`, `docs/specs/HYGIENE.md` — current SDK policy, Git/preconditions and legacy lifecycle.
- `docs/specs/SEMANTIC-REVIEWS.md`, `docs/specs/SAMPLING.md`, `docs/specs/REVIEW-HANDOFFS.md` — legacy review behavior preserved.
- `docs/ENGINEERING.md` — supported Windows/.NET 11 environment, release-tool surface and receipt/shard validation.
- `docs/milestones/M0024-sdk-format-profile-and-source-exceptions.md` and `docs/milestones/M0025-semantic-rule-portfolio.md` — historical accepted implementation boundaries, not new obligations to redo.

## Acceptance Criteria

- **AC-01** — The repository builds as one workbench monorepo solution with both a separately named Autoformat engine/executable and the retained legacy hygiene engine/executable; no other Git repository, plugin host, daemon or SQLite index is introduced.
- **AC-02** — Autoformat engine and CLI have no compile-time or runtime dependency on legacy hygiene, semantic review, review acceptance/audit state, or an index service; legacy hygiene may depend on the Autoformat engine, never the reverse.
- **AC-03** — A freshly packed `DotNetAiWorkbench.Autoformat.Tool` version `0.1.0` installs in an isolated `dotnet tool` consumer and exposes `dotnet autoformat` independently of any installed `dotnet hygiene` shim.
- **AC-04** — The installed Autoformat CLI supports the specified default, `format`, `normalize`, and `init` commands, explicit path arguments, `--check`, `--changed`, `--output text|json`, and ordinary help/version behavior with truthful results and actionable invalid-input diagnostics.
- **AC-05** — Autoformat default/`normalize` performs the existing approved .NET 11 SDK style diagnostic set then whitespace, whereas `format` performs whitespace only; check-only operations verify the same policy without source writes.
- **AC-06** — Autoformat target selection preserves full eligible project coverage for unscoped runs, narrower explicit path selection, and staged/unstaged/untracked `--changed` read-only selection across supported multi-project repositories.
- **AC-07** — Autoformat mutating runs enforce the established clean Git/known HEAD checkpoint and HEAD/index stability contract, never commit/reset/clean/rollback automatically, and report partial changes on phase failure or concurrent modification without masking recovery work.
- **AC-08** — Autoformat `init` establishes/validates the fixed mechanical `.editorconfig` policy without creating `.hygiene` review/baseline state, preserves unrelated configuration, reports conflicts, and is idempotent; existing hygiene-managed mechanical policy has a safe non-destructive adoption path.
- **AC-09** — The mechanical policy and SDK formatter orchestration have one implementation/ownership location; the old hygiene entrypoint delegates its former formatting and normalization behavior to it rather than retaining a second, diverging formatter.
- **AC-10** — The still-supported transitional `dotnet hygiene` workflow retains M0025 review discovery, bounded selection, handoff, acceptance and explicit exhaustive read-only audit behavior; extraction does not trigger implicit semantic review from Autoformat.
- **AC-11** — Existing accepted review records, audit artifacts, suppression/retain comments, and user-authored project/source files are preserved; Autoformat neither reads them as authorization nor rewrites/deletes them.
- **AC-12** — Autoformat produces zero source diff on a second successful normalize run against the same compliant project, and source changes are limited to the approved SDK fixes/formatting without unapproved structural/semantic transformations.
- **AC-13** — A real isolated installed-tool consumer proves Autoformat on fresh SDK-style Git projects, and a separate existing-hygiene installed-tool consumer proves the transitional review/formatter boundary using artifacts from the current branch.
- **AC-14** — The monorepo validation plan/receipts cover both executable surfaces, their focused tests, packaging and self-host gate; stale/missing stage evidence is rejected and current evidence is reusable at completion.
- **AC-15** — No new automatic XML documentation/content canonicalizer, Razor/project file relocation policy, Autoreview executable, persistent index, IPC/HTTP service, semantic rule portfolio redesign, or public NuGet publishing is introduced in M0026.
- **AC-16** — Public documentation and in-repository architecture/specification authority accurately describe the workbench identity, Autoformat invocation/profile ownership, transitional hygiene boundary, packaging and supported scope without claiming deferred capabilities.

## Additional Completion Obligations

- **ART-01** — The current Autoformat 0.1.0 NuGet tool artifact is packed from this branch and its identity/version/command metadata are recorded; the existing hygiene package retains its own release identity.
- **REV-01** — An independent reviewer examines the executable/module boundary, safety, real installed consumers and acceptance evidence; approval is recorded in `.review/REV-M0026-COMPLETION.md` and passes the milestone-scoped review check.

## Acceptance Evidence Topology

| ID | Parent | Required evidence case | Distinct proof rationale |
|---|---|---|---|
| EC-04a | AC-04 | Installed entrypoint dispatch and output for default/format/normalize/help/version | Distinct subcommand dispatch and installed shim may differ from project launch. |
| EC-04b | AC-04 | Installed `init` and error/JSON reporting | Profile mutation and invalid-input/output contracts use different paths. |
| EC-05a | AC-05 | Mutating SDK style-plus-whitespace versus whitespace-only results | The style and whitespace phases have different approved fixers. |
| EC-05b | AC-05 | Check-only same-policy diagnostics across both modes | Verification paths must never write. |
| EC-06a | AC-06 | Unscoped multi-project/solution complete eligibility | Whole-repo batching differs from explicit targeting. |
| EC-06b | AC-06 | Explicit files/projects and unsupported/ambiguous targets | Target narrowing and exclusion need distinct proof. |
| EC-06c | AC-06 | `--changed` staged, unstaged and untracked paths; no mutation | Git working-tree discovery is distinct from project traversal. |
| EC-07a | AC-07 | Dirty/unknown Git checkpoint rejected before mutation | Entry preconditions must prevent source loss. |
| EC-07b | AC-07 | SDK phase failure or concurrent HEAD/index mutation leaves truthful incomplete report | Failure-after-edit path differs from precondition refusal. |
| EC-08a | AC-08 | Fresh project init writes only owned mechanical policy and rerun is no-op | Clean initialization behavior. |
| EC-08b | AC-08 | Existing hygiene-managed profile/config adoption retains effective policy and external ownership | One-time legacy coexistence is not fresh initialization. |
| EC-08c | AC-08 | Conflicting nested/editorconfig configuration and malformed markers fail safely | Conflict path must not silently overwrite user policy. |
| EC-10a | AC-10 | Transitional regular change-gate review selection, acceptance, handoff | Normal review lifecycle is distinct from audit. |
| EC-10b | AC-10 | Authorized exhaustive read-only audit and resume | Audit protocol has independent read-only state and authorization. |
| EC-12a | AC-12 | Second mutating normalize/format pass is diff-free on representative eligible source | Idempotence proof differs from semantic preservation. |
| EC-12b | AC-12 | Compiled source after approved rewrites, including source-local modernization exceptions | Fix safety and suppressed cases differ from whitespace checks. |
| EC-13a | AC-13 | Fresh packed/installed Autoformat consumer, not an in-project invocation | New distribution surface. |
| EC-13b | AC-13 | Fresh packed/installed legacy hygiene consumer against the same branch | Preservation of transitional separate distribution surface. |
| EC-14a | AC-14 | Both executable/test/package stages present and bound to current inputs | Coverage of a second product's artifacts is new. |
| EC-14b | AC-14 | Verifier rejects stale/missing receipts after new-tool changes and accepts complete current set | Evidence freshness behavior differs from successful run. |

Non-cased acceptance obligations require individual evidence without additional `EC-*` fragmentation. The ledger below is a one-for-one projection of obligations/evidence cases, not a second implementation contract.

## Validation

| ID | Depth | Real target | Locus/platform | Command/check | Proves | Expected evidence |
|---|---|---|---|---|---|---|
| VAL-01 | Tier 1/2 | Source architecture and solution build | Local Windows 11 + .NET 11 + Git/PowerShell | `dotnet build DotNetAiWorkbench.slnx -c Release` and focused dependency/reference inspection | AC-01, AC-02, AC-09, AC-15 | Build output, independent references and module-boundary evidence |
| VAL-02 | Tier 1/2 | Focused Autoformat source/CLI integration fixtures | Local Windows 11 + .NET 11 SDK + Git | `./eng/validate.ps1 -Shard autoformat-tests` (new declared stage) | AC-04, EC-04b, EC-05a, EC-05b, EC-06a, EC-06b, EC-06c, EC-07a, EC-07b, EC-08a, EC-08b, EC-08c, AC-11, EC-12a, EC-12b | Machine-verifiable fixtures and current receipt for the new stage |
| VAL-03 | Tier 1/2 | Existing hygiene Core and CLI regression | Local Windows 11 + .NET 11 SDK + Git | `./eng/validate.ps1 -Shard core-tests` and `./eng/validate.ps1 -Shard cli-tests` | AC-09, EC-10a, EC-10b, AC-11, EC-12b | Separate current existing Core/CLI receipts and behavioral regressions |
| VAL-04 | Tier 4 | Two exact-current packed/installed tools in isolated SDK-style consumer repositories | Local Windows 11 + .NET 11 + Git; isolated temporary NuGet feed and tool paths | `./eng/validate.ps1 -Shard installed-consumers` (extended to both current artifacts) | AC-03, EC-04a, EC-04b, EC-05a, EC-05b, EC-08a, EC-08b, EC-13a, EC-13b, ART-01 | Package hashes, isolated install paths/shim invocations, source diffs and consumer receipts |
| VAL-05 | Tier 2/4 | Monorepo aggregate validation, self-host and receipt freshness | Local Windows 11 + .NET 11 + Git/PowerShell | `./eng/validate.ps1 -Shard self-host`; `./eng/validate.ps1 -Verify` after all declared stages current; inspect `-PlanJson` | EC-14a, EC-14b, AC-14, AC-16 | Current staged receipts and accepted aggregate verification; self-host evidence |
| VAL-06 | Documentation | Current architecture/specs/README/help and explicit deferrals | Repository files + installed help | Direct docs/spec diff review plus installed `--help` | AC-04, AC-15, AC-16 | Referenced operative docs and actual command output |
| VAL-07 | Human | Independent milestone-scoped completion review | Project owner/authorized delegate, Windows host evidence | `./eng/review-check.ps1 -Milestone M0026` (extend allowed milestone set) | REV-01 | Approved review record, rationale and decision evidence; gate is blocking |

Each required gate is run against the **current implementation inputs**. `VAL-04` must inspect the new package at the installed tool boundary, not infer correctness from an internal method invocation. `VAL-05` aggregates only current receipts; it cannot by itself certify behavior absent from the actual scenarios. A stable implementation state triggers expensive package/self-host acceptance; after later changes only invalidated stages must be rerun. `VAL-07` is blocking and remains outstanding until an authorized independent human review is recorded.

Existing SDK/Git/NuGet/PowerShell, restoring dependencies and disposable temp consumer repositories are authorized. Persistent machine SDK provisioning, admin access, new GitHub Actions/workflows or remote infrastructure changes are **not** authorized. If no eligible Windows 11/.NET 11 test host is available, preserve the implementation/evidence state and mark the affected gate **BLOCKED**; do not invent equivalent Linux/CI evidence or claim milestone completion.

## Human Review

- **Applicability:** required for this cross-cutting packaging and runtime-boundary migration.
- **Review class:** independent milestone-scoped completion review.
- **Canonical review ID:** `REV-M0026-COMPLETION`.
- **Review subject:** absence of Autoformat-to-review dependency, mechanical-policy single ownership, data/source safety, installed tool parity, current validation receipts, and documentation truth.
- **Required evidence:** package/installed consumer outputs, safety/migration tests, review-regression results, validation plan and receipts, exact AC/EC reconciliation.
- **Acceptance obligation:** `REV-01` (blocking).
- **Reviewer role:** project owner or authorized independent delegate; implementer cannot self-approve.
- **Waiver policy:** no silent waiver.
- **Completion command:** `./eng/review-check.ps1 -Milestone M0026` after recording an approved `.review/REV-M0026-COMPLETION.md`.

## Documentation Policy

- **AC-16** requires directly affected public and authoritative engineering/architecture/specification/help changes.
- Historical milestone reports and research are not retroactively rewritten.
- Broader docs synchronization remains a separate guide-governed pass.

## Completion Expectations

The implementation agent reads this milestone and the planning-seeded ledger, creates local work packages, maintains AC/EC-specific evidence and any needed resumable validation status, then rereads this milestone and checks exact ID-set equality before its completion audit. No broad suite receipt, implementation summary or generic green report substitutes for individual obligations. At closure the implementer records compact evidence below.

## Completion Evidence

_Implementation-owned. Fill only after the completion audit, every applicable AC/EC and ART obligation is proven, all required gate evidence is current and independent review approves. Retain a durable AC/EC-to-proof mapping; do not write implementation progress here._

## Escalation Boundary

Implementation owns code/test choices and repo-local structure that honor this contract. Return to planning if real SDK/provider behavior disproves the fixed mechanical policy, existing review preservation conflicts with single ownership, installed-tool requirements cannot be met without changing supported platforms/provisioning, or an index/service/new semantic policy appears necessary. Do not silently broaden the milestone or weaken its acceptance cases.
