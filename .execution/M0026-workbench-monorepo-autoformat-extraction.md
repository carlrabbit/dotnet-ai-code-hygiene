# Execution Ledger — M0026

Primary milestone: `docs/milestones/M0026-workbench-monorepo-autoformat-extraction.md`

This file is **operational implementation state**, not product authority. Planning seeded the lossless obligation/evidence-case registry and validation gates. Implementation must not rewrite, merge or drop planner-owned IDs/wording. Work packages, concrete test artifacts, receipts, status and resume details belong to the implementation agent.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | The repository builds as one workbench monorepo solution with both a separately named Autoformat engine/executable and the retained legacy hygiene engine/executable; no other Git repository, plugin host, daemon or SQLite index is introduced. | | | | todo |
| AC-02 | acceptance | Autoformat engine and CLI have no compile-time or runtime dependency on legacy hygiene, semantic review, review acceptance/audit state, or an index service; legacy hygiene may depend on the Autoformat engine, never the reverse. | | | | todo |
| AC-03 | acceptance | A freshly packed `DotNetAiWorkbench.Autoformat.Tool` version `0.1.0` installs in an isolated `dotnet tool` consumer and exposes `dotnet autoformat` independently of any installed `dotnet hygiene` shim. | | | | todo |
| AC-04 | acceptance | The installed Autoformat CLI supports the specified default, `format`, `normalize`, and `init` commands, explicit path arguments, `--check`, `--changed`, `--output text\|json`, and ordinary help/version behavior with truthful results and actionable invalid-input diagnostics. | | | | todo |
| AC-05 | acceptance | Autoformat default/`normalize` performs the existing approved .NET 11 SDK style diagnostic set then whitespace, whereas `format` performs whitespace only; check-only operations verify the same policy without source writes. | | | | todo |
| AC-06 | acceptance | Autoformat target selection preserves full eligible project coverage for unscoped runs, narrower explicit path selection, and staged/unstaged/untracked `--changed` read-only selection across supported multi-project repositories. | | | | todo |
| AC-07 | acceptance | Autoformat mutating runs enforce the established clean Git/known HEAD checkpoint and HEAD/index stability contract, never commit/reset/clean/rollback automatically, and report partial changes on phase failure or concurrent modification without masking recovery work. | | | | todo |
| AC-08 | acceptance | Autoformat `init` establishes/validates the fixed mechanical `.editorconfig` policy without creating `.hygiene` review/baseline state, preserves unrelated configuration, reports conflicts, and is idempotent; existing hygiene-managed mechanical policy has a safe non-destructive adoption path. | | | | todo |
| AC-09 | acceptance | The mechanical policy and SDK formatter orchestration have one implementation/ownership location; the old hygiene entrypoint delegates its former formatting and normalization behavior to it rather than retaining a second, diverging formatter. | | | | todo |
| AC-10 | acceptance | The still-supported transitional `dotnet hygiene` workflow retains M0025 review discovery, bounded selection, handoff, acceptance and explicit exhaustive read-only audit behavior; extraction does not trigger implicit semantic review from Autoformat. | | | | todo |
| AC-11 | acceptance | Existing accepted review records, audit artifacts, suppression/retain comments, and user-authored project/source files are preserved; Autoformat neither reads them as authorization nor rewrites/deletes them. | | | | todo |
| AC-12 | acceptance | Autoformat produces zero source diff on a second successful normalize run against the same compliant project, and source changes are limited to the approved SDK fixes/formatting without unapproved structural/semantic transformations. | | | | todo |
| AC-13 | acceptance | A real isolated installed-tool consumer proves Autoformat on fresh SDK-style Git projects, and a separate existing-hygiene installed-tool consumer proves the transitional review/formatter boundary using artifacts from the current branch. | | | | todo |
| AC-14 | acceptance | The monorepo validation plan/receipts cover both executable surfaces, their focused tests, packaging and self-host gate; stale/missing stage evidence is rejected and current evidence is reusable at completion. | | | | todo |
| AC-15 | acceptance | No new automatic XML documentation/content canonicalizer, Razor/project file relocation policy, Autoreview executable, persistent index, IPC/HTTP service, semantic rule portfolio redesign, or public NuGet publishing is introduced in M0026. | | | | todo |
| AC-16 | acceptance | Public documentation and in-repository architecture/specification authority accurately describe the workbench identity, Autoformat invocation/profile ownership, transitional hygiene boundary, packaging and supported scope without claiming deferred capabilities. | | | | todo |
| ART-01 | artifact | The current Autoformat 0.1.0 NuGet tool artifact is packed from this branch and its identity/version/command metadata are recorded; the existing hygiene package retains its own release identity. | | | | todo |
| REV-01 | review | An independent reviewer examines the executable/module boundary, safety, real installed consumers and acceptance evidence; approval is recorded in `.review/REV-M0026-COMPLETION.md` and passes the milestone-scoped review check. | | | | todo |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-04a | AC-04 | Installed entrypoint dispatch and output for default/format/normalize/help/version | VAL-04 | | todo |
| EC-04b | AC-04 | Installed `init` and error/JSON reporting | VAL-02, VAL-04 | | todo |
| EC-05a | AC-05 | Mutating SDK style-plus-whitespace versus whitespace-only results | VAL-02, VAL-04 | | todo |
| EC-05b | AC-05 | Check-only same-policy diagnostics across both modes | VAL-02, VAL-04 | | todo |
| EC-06a | AC-06 | Unscoped multi-project/solution complete eligibility | VAL-02 | | todo |
| EC-06b | AC-06 | Explicit files/projects and unsupported/ambiguous targets | VAL-02 | | todo |
| EC-06c | AC-06 | `--changed` staged, unstaged and untracked paths; no mutation | VAL-02 | | todo |
| EC-07a | AC-07 | Dirty/unknown Git checkpoint rejected before mutation | VAL-02 | | todo |
| EC-07b | AC-07 | SDK phase failure or concurrent HEAD/index mutation leaves truthful incomplete report | VAL-02 | | todo |
| EC-08a | AC-08 | Fresh project init writes only owned mechanical policy and rerun is no-op | VAL-02, VAL-04 | | todo |
| EC-08b | AC-08 | Existing hygiene-managed profile/config adoption retains effective policy and external ownership | VAL-02, VAL-04 | | todo |
| EC-08c | AC-08 | Conflicting nested/editorconfig configuration and malformed markers fail safely | VAL-02 | | todo |
| EC-10a | AC-10 | Transitional regular change-gate review selection, acceptance, handoff | VAL-03 | | todo |
| EC-10b | AC-10 | Authorized exhaustive read-only audit and resume | VAL-03 | | todo |
| EC-12a | AC-12 | Second mutating normalize/format pass is diff-free on representative eligible source | VAL-02 | | todo |
| EC-12b | AC-12 | Compiled source after approved rewrites, including source-local modernization exceptions | VAL-02, VAL-03 | | todo |
| EC-13a | AC-13 | Fresh packed/installed Autoformat consumer, not an in-project invocation | VAL-04 | | todo |
| EC-13b | AC-13 | Fresh packed/installed legacy hygiene consumer against the same branch | VAL-04 | | todo |
| EC-14a | AC-14 | Both executable/test/package stages present and bound to current inputs | VAL-05 | | todo |
| EC-14b | AC-14 | Verifier rejects stale/missing receipts after new-tool changes and accepts complete current set | VAL-05 | | todo |

A parent obligation with explicit evidence cases is not done until every applicable child case is proved. Obligations without cases require direct criterion-specific evidence.

## Work Packages

_Implementation-owned. Derive bounded coherent packages from the live repository and map each seeded obligation; do not substitute a package summary for any AC/EC._

## Validation Gates

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | `dotnet build DotNetAiWorkbench.slnx -c Release` and focused dependency/reference inspection | Source architecture and solution build; Local Windows 11 + .NET 11 + Git/PowerShell | AC-01, AC-02, AC-09, AC-15 | todo | |
| VAL-02 | `./eng/validate.ps1 -Shard autoformat-tests` (new declared stage) | Focused Autoformat source/CLI integration fixtures; Local Windows 11 + .NET 11 SDK + Git | AC-04, EC-04b, EC-05a, EC-05b, EC-06a, EC-06b, EC-06c, EC-07a, EC-07b, EC-08a, EC-08b, EC-08c, AC-11, EC-12a, EC-12b | todo | |
| VAL-03 | `./eng/validate.ps1 -Shard core-tests` and `./eng/validate.ps1 -Shard cli-tests` | Existing hygiene Core and CLI regression; Local Windows 11 + .NET 11 SDK + Git | AC-09, EC-10a, EC-10b, AC-11, EC-12b | todo | |
| VAL-04 | `./eng/validate.ps1 -Shard installed-consumers` (extended to both current artifacts) | Two exact-current packed/installed tools in isolated SDK-style consumer repositories; Local Windows 11 + .NET 11 + Git; isolated temporary NuGet feed and tool paths | AC-03, EC-04a, EC-04b, EC-05a, EC-05b, EC-08a, EC-08b, EC-13a, EC-13b, ART-01 | todo | |
| VAL-05 | `./eng/validate.ps1 -Shard self-host`; `./eng/validate.ps1 -Verify` after all declared stages current; inspect `-PlanJson` | Monorepo aggregate validation, self-host and receipt freshness; Local Windows 11 + .NET 11 + Git/PowerShell | EC-14a, EC-14b, AC-14, AC-16 | todo | |
| VAL-06 | Direct docs/spec diff review plus installed `--help` | Current architecture/specs/README/help and explicit deferrals; Repository files + installed help | AC-04, AC-15, AC-16 | todo | |
| VAL-07 | `./eng/review-check.ps1 -Milestone M0026` (extend allowed milestone set) | Independent milestone-scoped completion review; Project owner/authorized delegate, Windows host evidence | REV-01 | todo | |

`VAL-04` is exact-current dual installed-tool validation. `VAL-07` requires independent human approval. Use affected focused feedback while implementing; after packaging and product boundaries stabilize, establish current consumer/self-host receipts and verify the full required plan. No exploratory result or partial suite is aggregate acceptance evidence.

## Resume Point

_Implementation-owned._

Last completed work package:

Current work package:

Next concrete action:

Known agent-resolvable gaps:

External blockers or planning escalations:

## Final Reconciliation

- [ ] Reread the primary milestone from disk and enumerate its obligation IDs.
- [ ] Verify exact set equality with the obligation registry (AC-01 through AC-16, ART-01, REV-01).
- [ ] Verify exact set equality with the twenty required evidence-case IDs.
- [ ] Verify no planner-owned obligation/evidence case was omitted, merged, renamed or summarized away.
- [ ] Reconcile every obligation with implementation evidence and every evidence case with actual behavioral proof.
- [ ] Confirm each validation gate exercises only what it claims to prove and runs at the specified real locus.
- [ ] Downgrade stale, partial, indirect or unsupported completion assertions.
- [ ] Verify current receipts and freshness/installed output identities at the completion boundary.
- [ ] Confirm no agent-resolvable gap remains, no deferred feature was inadvertently implemented, and required independent review is recorded.
- [ ] Execute the completion audit and write compact durable evidence in the milestone's `Completion Evidence` section.
