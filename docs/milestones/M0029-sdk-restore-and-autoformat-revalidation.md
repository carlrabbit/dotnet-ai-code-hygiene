# Milestone — M0029: SDK Restore Integration and Autoformat Revalidation

**ID:** M0029  
**State:** ready  
**Type:** Autoformat correction and external integration experiment  
**Source repository:** `carlrabbit/dotnet-ai-code-hygiene`  
**Intended planning base:** M0028 implementation head `64781d65997c712e47665f47e5c1d7d10732d00f` (PR #35); M0028 planning PR #34 and implementation PR #35 are not yet merged  
**Guide authority:** repository-local pinned `.guide/system/` at 1.0.0, present in the intended base  
**External test target:** `carlrabbit/dotnet-nuget-content`, original M0028 target baseline `25b28310c7de04426de02e4ac8037b32ed9dbd9e`  
**Existing evidence:** workbench M0028 research report; target [Draft PR #13](https://github.com/carlrabbit/dotnet-nuget-content/pull/13), head `741c795e89368b286380daa7168c0514fd03ad55`, **DO NOT MERGE**

## Goal and target capability

Correct the integration contract between the independent Autoformat tool and the standard `dotnet format` SDK operations, and repeat the exact real-repository experiment without losing the first experiment's evidence.

- `dotnet autoformat normalize` and `format` should allow `dotnet format` to use its **normal implicit dependency restore** by default. An explicit Autoformat `--no-restore` fast path may be used only at the caller's choice, with the meaning inherited from the SDK. **Do not add a `--no-build` flag, a mandatory `dotnet build` step, custom unused-import detection, or a semantic validator.**
- Preserve standard SDK ownership of unused imports (`IDE0005`) and other approved SDK style/whitespace fixes. Avoid silently deleting necessary imports merely because symbol resolution is unavailable through an incomplete SDK/project context. If normal restore does not resolve the defect, investigate *only the material SDK-invocation/project-context difference* and repair the integration or report the concrete unresolved dependency; do not build an alternate unused-using fixer.
- Restore **one-pass byte stability** for `normalize`. M0028 observed 48 blank lines added on the second pass across four files; M0027 already requires an unchanged second pass. Avoid expanding into an unrelated global layout redesign.
- Re-execute Autoformat from corrected source in a disposable `dotnet-nuget-content` checkout, publish the **unaltered new output** in a **new Draft PR** for a side-by-side comparison with Draft PR #13, and record the result. Never edit or overwrite the original experiment's output.

This is a *separate correction milestone*. M0028 remains a completed guide-adoption and initial investigation milestone. Do not rewrite M0028's historical evidence or reopen its six completed obligations.

## Architecture and invariants

### A. SDK invocation and project context

**Already established:** `RewriteEngine.RunPhase` in `src/DotNetAiWorkbench.Autoformat.Core/RewriteEngine.cs` launches `dotnet format <workspace> style/whitespace` and unconditionally adds `--no-restore`; `normalize` explicitly selects the approved SDK diagnostics, including `IDE0005`. The CLI in `src/DotNetAiWorkbench.Autoformat.Cli/Program.cs` does not expose a restore choice. The target originally compiled but lost required `NuGet.Versioning`, `NuGet.Frameworks`, and `NuGet.Packaging` imports after normalization. This is **evidence of a source-integrity problem, not proof that restore is its sole cause**.

- Default: invoke the supported SDK `dotnet format` CLI in its normal restore-enabled mode. Do **not** add a separate unconditional build, compilation gate, or custom `dotnet restore` sequence merely to work around `--no-restore` unless a concrete SDK invocation failure demonstrates that the SDK-native restore contract is insufficient.
- Expose explicit `--no-restore` for `format`, `normalize` and default normalization entrypoints where applicable; forward it only to the underlying SDK format phases. The check-only normalization's isolated execution must honor the same choice without modifying real repository sources. Keep `--changed` read-only and existing path selection/clean-checkpoint semantics.
- If the SDK restores fail, surface an actionable SDK/error result and the existing explicit partial-edit status; never silently reinterpret failed analysis as a valid `IDE0005` result. Keep `init` as policy establishment, not package build or publication. Retain the same fixed `.editorconfig` policy and diagnostic allowlist unless concrete SDK evidence demands a separate planning decision.
- Correct only relevant orchestration errors revealed by the focused reproduction. **Do not** ship a custom unused-import implementation, blanket-disable `IDE0005`, or add a new universal semantic/build gate to Autoformat. Normal source edits remain allowed to be temporarily uncompilable under the intentional M0027 agent-repair contract; this does not license accidental deletion of imports that still bind to source references.

### B. One-pass convergence

The existing `normalize` pipeline applies custom syntax edits, SDK style, SDK whitespace, and final custom layout. An additional normalization of the initial generated source introduced 48 blank lines. Identify the smallest reproducible phase interaction and correct it so **one successful normalization reaches a fixed point**. The first *check-only* run from canonical output must report zero source changes. Preserve comments/directives, expected style transforms, `format` as whitespace-only, and read-only check semantics.

### C. Second real-repository experiment

- Execute exclusively in a **fresh disposable clone/worktree** of `carlrabbit/dotnet-nuget-content` from exactly the **same recorded original M0028 baseline** when available (`25b28310c7de04426de02e4ac8037b32ed9dbd9e`) to make comparisons meaningful. Record the exact target and corrected Autoformat commits and toolchain. Do not modify the original Draft PR #13, its branch/commits, target `main`, or any non-disposable clone.
- Authorize **one new experimental target branch and one new Draft PR targeting `main`**, only after re-running `init` and using a separate policy commit as the clean Git checkpoint. Make subsequent commit(s) solely the **raw, unmodified** output of the corrected formatter. The branch/PR must have a different name from the M0028 experiment and be labeled `Exploratory — do not merge`. Link both M0029 and original Draft PR #13. The original PR stays available for comparison.
- Run an initial `normalize`, inspect the diff, and use an actual second mutating run or an equivalently discriminating read-only comparison to demonstrate **zero changes after the first pass**; check `normalize --check` and `format --check` on canonical source. A single focused diagnostic `dotnet build` of original and rewritten application is appropriate **as evidence** that the prior required-import regression is fixed; this is not a universal Autoformat runtime prerequisite. If an explicitly allowed modernization causes an unrelated compilation failure, classify it rather than misattribute it to `IDE0005`.
- If the required-import defect persists after default SDK restore, do **not** publish a misleading green PR. Inspect exact SDK command/project loading and apply a bounded SDK-integration correction if feasible; otherwise report the confirmed unresolved cause and retain the target Draft PR, if published, unmistakably blocked/DO NOT MERGE. Do not hand-edit the generated target to make it compile.
- No target tests, Native AOT publication, NuGet package release, installed-tool qualification, or full Hygiene suite. Do not alter target policies beyond generated `.editorconfig` or create any target-specific source exceptions.

## Scope and exclusions

**In scope:** default SDK restore policy, opt-in `--no-restore` CLI forwarding and check-only parity, one-pass normalize stability, the exact-revision new NuGet-content Draft PR, directly affected Autoformat CLI/help/spec/tests, and a concise results report.

**Excluded:** changes to the pinned guide, guide-profile migration, M0028 history, custom using analysis, new analyzers or diagnostics, general source modernization redesign, automatic compilation/repair, CI/workflow or platform matrix, broad fixtures, package installation/publishing, target `main` mutation or merge. Do not treat the unmerged target experiment as a consumer release candidate.

## Acceptance and material obligations

- **REST-01:** The real SDK formatting phases allow implicit restore by default, `--no-restore` is explicit opt-in and truthfully forwarded across applicable CLI/check modes, and necessary imports are no longer lost on the restored, unchanged target baseline through the Autoformat SDK integration. No custom unused-using detector or mandatory build is introduced.
- **LAY-01:** One successful `normalize` produces a fixed point: the second run and check-only view require no additional whitespace/layout edits, including representative compact code that previously accumulated blank lines. `format` stays whitespace-only.
- **EXP-01:** A **new** isolated NuGet-content experimental branch/Draft PR exposes the corrected unmodified formatter result, with policy committed separately, exact source revisions, a comparison to PR #13, an unmerged DO NOT MERGE warning, and a truthful diagnostic build result. Original PR #13 and target `main` are preserved.
- **DOC-01:** Update relevant workbench CLI help/Autoformat specification and add `docs/research/M0029-autoformat-nuget-content-revalidation.md` with direct observations, inferred/unresolved causes, exact commands, source revisions, links to the two target PRs, first/second-pass comparison, build outcome, and follow-up/remaining limitations. Update `docs/MILESTONES.md` and this milestone's completion note. Do not alter the M0028 report retroactively.

**Seeded execution ledger:** `.execution/M0029-sdk-restore-and-autoformat-revalidation.md` contains these four IDs and no evidence/test mapping.

## Focused verification

One small real SDK-style integration fixture containing **one required imported package type and one genuinely unused import** must show both preserved needed imports and removal of unused imports under normal restored SDK operation, with actual `dotnet format` invoked through Autoformat. A focused CLI check establishes opt-in `--no-restore` forwarding and check-only read-only behavior. The target `dotnet-nuget-content` rerun then validates real project context, import survival, second-pass zero diff, and offers a diagnostic application build. This is **not** a full target test gate or requirement for downstream compilation by Autoformat on every invocation.

Use focused existing Autoformat tests and builds only as needed; avoid whole-workbench validation, installed consumer/package checks, extra qualification infrastructure or semantic-equivalence testing. Capture and report any genuine failure.

## Implementation handoff and completion

The ready milestone and seeded ledger are sufficient for a disconnected implementer. Read relevant existing `docs/specs/REWRITES.md`, `docs/ENGINEERING.md`, `AGENTS.md` and Autoformat CLI/Core/tests. No need to load upstream Guide 1.0.0 or the prior planning chat. Implementation begins from the **exact accepted planning commit in the handoff report**; do not record a self-referential commit here. Branch/PR chain is described externally because M0028 implementation and its planning PR are not yet merged.

Before reporting `COMPLETE`, reconcile the four seeded IDs, verify the new target Draft PR remains unmerged, and record what actually compiled, converged and was not evaluated. Formal human sign-off is not a milestone gate; merging the target experimental PR needs a separate decision.