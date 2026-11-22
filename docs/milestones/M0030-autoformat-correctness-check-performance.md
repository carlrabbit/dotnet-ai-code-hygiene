# Milestone — M0030: Autoformat Source Safety, Check-Mode Performance, and Rewrite Coverage

**ID:** M0030  
**State:** ready  
**Type:** feature (corrective improvement)  
**Planning base:** `carlrabbit/dotnet-ai-code-hygiene` `main` at `66ed726b505bac2457e0edabe5cf3b3340650853` (merged M0028/M0029)  
**Guide authority:** repository-pinned `.guide/system/` 1.0.0, with project-local `docs/PROJECT-GUIDANCE.md`  
**External evidence/target:** `carlrabbit/yadg`, baseline `dafa75ab7f4032d8ac85599f7a7c951fec49f6d1`, unmerged [experimental Draft PR #22](https://github.com/carlrabbit/yadg/pull/22) at `3f40f1a44d7782925148b16cc3bbcf6502ad41ae` (**DO NOT MERGE**)

## Goal and target capability

Make independent Autoformat safe and economical for routine agent-driven C# changes. Deliver one coherent correction of:

1. **Source correctness:** prevent the confirmed `modernization.var-style` rewrite from emitting illegal sized-array declaration types (four `CS0270` errors in YADG).
2. **Fast, reliable read-only checks:** eliminate avoidable work in `normalize --check`, especially the loss of solution-level SDK batching in its temporary project copy, while retaining the *same observable normalization result* as mutating `normalize`.
3. **Cheap rule confidence:** systematically cover the *supported syntactic input categories* of all nine custom modernization families using direct, deterministic C# string-to-string canonicalizer tests, improving weak assertions where needed.
4. **Observed layout quality:** correct the specific YADG parameter/record indentation and redundant-blank-line cases if they violate existing readability policy, without opening a general wrapping redesign.
5. **Real evidence:** run the corrected formatter once per selected workload on a fresh disposable YADG baseline, collect plain wall-clock times, compare the raw output, and report any remaining limitations.

This is a single Autoformat milestone. Its implementation experiments are reversible, and no shipping, release or consumer migration is authorized.

## Architecture and invariants

### A. Source-only transformations must respect their syntactic contracts

`CSharpCanonicalizer.CanonicalRewriter.VisitLocalDeclarationStatement` currently derives the type for `var x = new T[...]` via `SyntaxFactory.ArrayType(array.Type.ElementType, array.Type.RankSpecifiers)`. The creation's rank specifiers can contain *sizes*, which are invalid in a C# variable type. In YADG, `var buffer = new char[4096]` became `char[4096] buffer = new char[4096]`; two-dimensional creations were similarly rewritten to `bool[rows.Count, width] covered` and `int[rows.Length, width] raw`. These are confirmed compiler errors, not acceptable opinionated modernization.

When the declaration type is known syntactically, emit valid declaration-type array ranks **without dimension expressions** while preserving the element type, rank count, multidimensional commas and jagged shape. Ineligible or uncertain forms may retain `var` rather than guessing. Preserve initializer expressions, source comments/directives, source-local retain annotations and the other supported transformations. Rewrites need not guarantee semantic equivalence for *every* deliberately opinionated modernization, but they must not introduce invalid declaration syntax in a supported deterministic case. Do not add semantic-model inference, consumer compilation into the formatter, or a new analyzer.

### B. Check-only is an exact source preview, not a cheaper approximation

`RewriteEngine.Rewrite("normalize", ..., checkOnly: true)` currently calls `PreviewNormalization`: it constructs a temporary shadow workspace, initializes another `RepositorySession`, and runs a complete mutating normalization there. This source-comparison design protects check/mutate parity and prevents writes to real source; **preserve those guarantees**. `--changed`, selected paths, SDK diagnostics (including `IDE0005`), default SDK-native restore, explicit `--no-restore`, output/exit semantics, partial-failure reporting, and clean-checkpoint rules must not regress.

The current `CopyPreviewWorkspace` copies projects, their directories and root settings but **not the `.slnx`/`.sln` solution file**. Consequently `FindBatches` cannot use the solution and can fall back to per-project SDK invocations: for YADG's seven projects, up to seven style plus seven whitespace processes instead of one of each. Its initial and shadow project loading also have overhead. Restore alone cannot explain the observed timings: YADG `normalize --check` took 178.621 s, the explicit `--no-restore` variant 125.719 s, and a single-file check 36.037 s; a second mutating normalize took 43.419 s.

Restore solution-aware batching (or a demonstrably equivalent efficient batching strategy) **inside the shadow** while preserving the exact effective project/solution context and selected files. When a solution covers the selected projects, the expected SDK phase fan-out is one style + one whitespace invocation, not one pair per project. Maintain functional fallback where no usable solution exists. Avoid unnecessary repeated project evaluation/work where economical, but choose bounded reversible implementation mechanics rather than introducing a general caching/daemon service, build orchestrator or alternative SDK formatter. A faster incorrect check is not acceptable. `--no-restore` remains explicitly opt-in, never a hidden default.

### C. Existing layout policy remains authoritative

The [YADG Draft PR #22](https://github.com/carlrabbit/yadg/pull/22) also shows a top-level positional `YadgWorkspace` record whose generated parameters are indented 12 spaces and whose closing `)` is indented 8, despite the existing `docs/specs/READABILITY.md` contract that a vertical list uses one indentation level inside its declaration and aligns the closing delimiter with that declaration. A redundant empty line was observed before `ConvertInlines` in `MarkdownDocumentParser.cs`. Diagnose these exact shapes and correct verified policy violations. Preserve legitimate blank visual-block boundaries, comments/directives, existing author-intended multiline forms, retain exceptions and newline style. Deeply nested invocation wrapping that is merely aesthetically debatable is not authorization to redesign the 120-character soft target.

### D. Change interactions and permitted consequences

Review the nine declared modernization families in `CSharpCanonicalizer.Rules`: `var-style`, `expression-body`, `collection-expression`, `primary-constructor`, `switch-expression`, `conditional`, `object-initializer`, `readonly-field`, and `inline-declaration`. Existing tests demonstrate their positive paths but are uneven across syntactic variants and prohibited neighbors. Strengthen confidence by checking meaningful contrasting categories, not an exhaustive language grammar and not a test for every individual source statement.

Protect previous M0029 corrections: collection expressions need a recognized target type (including nested return-owner boundaries), necessary SDK imports survive normal restore, member access does not acquire blank-before-dot anomalies, and a successful normalization reaches a one-pass fixed point. `format` remains SDK whitespace-only. The external YADG experiment is disposable and cannot be merged as production output through this milestone.

## Scope and exclusions

**In scope:** Autoformat Core canonicalization/layout and preview orchestration; directly affected CLI/support code if needed; fast in-memory rule tests and existing focused orchestration tests; source-only diagnostic/build checks; the existing YADG experiment and concise report; directly impacted Autoformat specs, README/help if behavior is changed, the milestone index and execution ledger.

**Excluded:** changes to transitional `dotnet hygiene` semantic-review behavior, wholesale modernization/rule redesign, new general semantic/type inference, SDK `dotnet format` replacement, separate custom unused-import analyzer, new CI or testing framework, repeated performance statistics or performance-infrastructure project, package/installed-tool qualification, YADG integration tests, Office/LibreOffice rendering, changing YADG application logic, guide snapshot/methodology changes, and merging experimental target output.

## Acceptance and material obligations

- **TYPE-01 — Valid array declaration type synthesis.** Explicitly sized single-dimensional, multidimensional and jagged array creation under `var` produces a valid element/rank-only declaration type (or conservatively preserves `var` when not safely representable), without copying array dimensions into the declared type. The four YADG `CS0270` generation sites no longer occur. Existing eligible `var` modernizations and ineligible cases retain their intended behavior.

- **TEST-01 — Deterministic modernization shape coverage.** Inspect the supported and explicitly excluded syntactic categories of **all nine** current modernization families; strengthen/reuse existing tests and add small direct `CSharpCanonicalizer.Normalize(source, ...)` source-to-source regressions for material missing positive, negative and boundary shapes. For var-style cover explicit/implicit arrays, sized 1D, rectangular, jagged cases, eligible object/literal forms and a non-inferable case. Assert meaningful exact transformed and preserved fragments (or a full expected output where practical), plus stability where the rule can interact with layout. Do not substitute parse-success or a single omnibus `Contains` assertion for expected rewrites; Roslyn parse success alone cannot detect `CS0270`. No SDK/filesystem fixture or compilation per table row.

- **LAY-01 — Focused readability fixes.** The YADG positional-record parameter alignment and extra blank-line example comply with existing `READABILITY.md` policy after normalization. Protect intended vertical method parameter layout, other author-authored blank lines, comments, directives and retain exceptions. Record any remaining merely subjective wrapping issues as observations rather than expanding scope.

- **CHECK-01 — Exact and safe check-only semantics.** On representative initially noncanonical and canonical source, `normalize --check` reports the same selected paths and prospective final changes as the actual normalization pipeline would, does not change the consumer's tracked source/policy files, respects paths and `--no-restore`, and continues to preserve SDK analyzer/restore context. `format --check` remains SDK-whitespace-only. Do not replace the complete preview with an incomplete pre-SDK verify check.

- **PERF-01 — Remove unnecessary check overhead.** In a solution-backed multi-project repository such as YADG, a check-only normalization uses solution-aware batching or an equivalent bounded grouping: no per-project style/whitespace process multiplication when a covering solution exists. Maintain sensible fallback for no-solution/project-specific inputs and no spurious project source modifications. Measure the real YADG full, `--no-restore` and single-file check durations once each; demonstrate a material improvement from the initial observed 178.621 s / 125.719 s / 36.037 s behavior, or investigate and address the remaining dominant avoidable cost *within the bounded Autoformat implementation* rather than claiming success from the batching counter alone. Report remaining time honestly; single-run timings are observations, not a formal benchmark or fixed SLA.

- **EXP-01 — Raw YADG correction experiment.** From the original YADG `dafa75ab7f4032d8ac85599f7a7c951fec49f6d1` baseline and existing policy, generate unmodified corrected Autoformat output in a fresh disposable checkout; append only its raw delta to the existing unmerged [Draft PR #22](https://github.com/carlrabbit/yadg/pull/22), keeping prior policy/source commits intact and PR explicitly **DO NOT MERGE**. Verify disappearance of `CS0270`, retained necessary imports, first/second-pass convergence and clean check modes, and run one diagnostic `dotnet build Yadg.slnx`. Record exact source/target commits, outputs, failures and one wall-clock observation per selected formatter command. Do not hand-repair YADG output or alter target `main`.

- **DOC-01 — Reconciled project truth and outcome.** Update the directly affected Autoformat specification and help/README only where behavior changed; record the YADG before/after defects, checked mode/project batching, actual one-run timings, corrections and limitations in a concise `docs/research/M0030-autoformat-yadg-revalidation.md`, then reconcile `docs/MILESTONES.md`, this milestone completion note, the ledger and existing experiment/implementation PR descriptions. Preserve M0028/M0029 and YADG's original experiment as historical evidence.

## Execution obligation ledger

**Planner-seeded ledger:** `.execution/M0030-autoformat-correctness-check-performance.md` has the same seven obligation IDs as above and no test/evidence mapping.

## Validation

**During implementation:** affected Autoformat Core/CLI build and focused existing canonicalizer/orchestration tests. Prefer a compact string-to-string table for deterministic rewrite shapes, with meaningful expected output and preserved negative cases. Test solution-backed phase fan-out using existing injectable phase-runner mechanics where feasible, without a real SDK process per case.

**At milestone completion:** use the authorized existing YADG experiment for the real SDK/process boundary rather than also inventing another broad fixture. In a disposable baseline clone apply the existing policy, run first `normalize`, then second `normalize`, `normalize --check`, `format --check`, `normalize --check --no-restore`, and single-file `normalize --check` (e.g. `src/Yadg.Core/Workspace.cs`). **Run each measured operation once**, capture elapsed seconds/exit/selected/changed counts, no repeats/medians/percentiles/statistics. Build the corrected Autoformat CLI in Release separately and use its built DLL; measure only formatter command invocation. One diagnostic target solution build is appropriate to discriminate the observed `CS0270` failure. Where earlier YADG results already establish a relevant baseline, reuse them without repeating old work. Preserve the raw output even if its diagnostic build fails and report any specific remaining defect rather than silently patching it.

**Important risks to discriminate:** invalid sized-array syntax even when parsing succeeds; false-clean check output after skipping SDK/context phases; loss of imports due to missing restore context; accidental input-repository writes during preview; altered selected paths or `--changed` behavior; solution-grouping code that omits a project or misroutes a selected source; multi-pass layout drift, intentional blank-line loss or excessive parameter indentation; partial/cancelled run inaccurately reported successful. Do not construct an independent test for every item—select a few high-information contrasting inputs and reuse the real YADG run.

**Deferred to release:** installed .NET tool/package qualification, full consumer/platform matrix, all-YADG integration/rendering tests, Native AOT, broad compatibility/semantic-equivalence guarantees, aggregate Hygiene/workbench release validation. No formal human acceptance gate; merging the experimental YADG PR requires a separate explicit production decision.

## Implementation authority and documentation

The implementation agent needs `AGENTS.md`, `docs/ENGINEERING.md`, `docs/PROJECT-GUIDANCE.md`, `docs/specs/REWRITES.md`, `docs/specs/READABILITY.md`, the milestone/ledger, and affected Autoformat source/tests. Use existing `docs/research/M0029-autoformat-nuget-content-revalidation.md` and [YADG PR #22](https://github.com/carlrabbit/yadg/pull/22) only as **historical evidence**, not policy. No need to load the pinned guide snapshot or the planning chat.

Planning authorizes workbench source/tests/docs on a separate implementation branch based on the **exact accepted planning commit**, plus disposable YADG checkouts and append-only raw-output commits to the **existing** Draft PR #22. It does **not** authorize merging that target PR, source repairs in YADG, permanent host provisioning, CI changes, installed global tools or upstream-guide changes. If the planning PR is merged by squash/rebase before pickup, reconcile the merged planning contract and materially relevant source divergence per the pinned guide's handoff model.

## Completion note (implementer fills after delivery)

Record concise delivered changes, the focused checks actually run and their results, YADG timing/build and output revision, confirmed unresolved limitations, and release qualification deferred. Reconcile all seven seeded obligations before declaring `COMPLETE`; no evidence-case register, one-test-per-criterion matrix or mandatory comprehensive suite.
