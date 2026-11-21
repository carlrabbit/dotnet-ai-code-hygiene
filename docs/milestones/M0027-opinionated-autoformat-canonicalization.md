# M0027 — Opinionated Autoformat Canonicalization

**ID:** M0027  
**State:** complete
**Type:** feature / refactoring  
**Planning methodology:** Agentic Project Guides 1.0.0-rc.1 (agent-scale milestones; risk-proportionate milestone validation)  
**Implementation baseline:** repository `main` after M0026, commit `1c735172666c9ec3b11c396c29dead0953aa4484` or a descendant  
**Primary authority:** this milestone; `AGENTS.md`, `docs/PROJECT-GUIDANCE.md`, `docs/ARCHITECTURE.md`, `docs/specs/REWRITES.md`, `docs/specs/READABILITY.md`, `docs/specs/SEMANTIC-REVIEWS.md`, and relevant live source/tests. Superseded historical descriptions in other specifications are not implementation constraints for the Autoformat scope settled here.

## Goal and target capability

Turn the independent `dotnet autoformat` executable into an **opinionated C# canonicalizer** rather than merely a wrapper around SDK formatting. Deliver one coherent implementation containing:

- a clean Autoformat-only policy and source-processing architecture, without inherited Hygiene compatibility scaffolding;
- automatic modernizations across the nine existing modernization candidate families;
- concrete, deterministic readability/layout canonicalizers, including visual blocks and long-line treatment;
- source-local, justified exceptions and stable, idempotent rewriting;
- an inexpensive development and milestone validation path separate from release qualification.

This is a **deliberately ambitious, reversible, agent-executed capability**. The implementer may compare alternative Roslyn/SDK/custom transformations, retain the best mechanical approach, and discard unsuccessful experiments within this milestone. Do not split it into micro-milestones merely because several families need implementation.

## Architecture and invariants

### Product boundary and responsibility

**Autoformat applies deterministic, opinionated policies; the implementing agent owns build, tests, and repair of consequences.** Autoformat is not a semantics-preserving refactoring service and is not a project build gate.

1. Applying a canonicalization may change concrete implementation details, observable behavior, or the success of a subsequent build. **Neither equivalence proof nor successful target-project compilation is required** to apply an otherwise eligible rule. The Autoformat runtime must not run `dotnet build`, `dotnet test`, full compilation validation, or a build-and-repair loop, nor roll back edits because downstream compilation might fail.
2. Roslyn syntax, semantic-model data, workspace/project loading, and SDK analyzers/code fixes **may be used to discover/apply candidate edits** when useful. This is distinct from requiring complete compilation or semantic equivalence as a precondition. Prefer inexpensive built-in fixes where their contract matches our desired result; allow broader custom rewrites where built-in fixes are too conservative.
3. The agent commits/checkpoints appropriately, runs Autoformat, then builds/tests and repairs errors or deliberate exceptions using the normal implementation workflow. Autoformat reports what it changed and any unsupported/unresolved cases; it never claims the resulting project compiles or retains identical behavior.
4. Within each eligible transformation, preserve unrelated declarations, executable operations, source comments, directives, and source-file integrity. No arbitrary text substitutions that lose code or leave malformed syntax on ordinary recognized cases. Changes to concrete collection implementation and deliberately selected modernization shapes are **not** considered prohibited semantic drift.
5. Preserve existing supported selection semantics (whole eligible SDK-style C# project population; narrower explicit paths; read-only `--changed`; check-only), source/generated-code exclusions, and the established clean Git/known-HEAD mutation checkpoint and explicit partial-change recovery. No implicit commit/reset/clean/rollback. Avoid introducing new persistent state.
6. Autoformat owns its fixed mechanical `.editorconfig` block and canonicalization policy. **No compatibility-dependent consumers exist**: old Hygiene markers, profile versions, mechanical rule IDs, and CLI/API shapes are not preservation targets. The active Hygiene semantic-review implementation should remain available until Autoreview extraction, but must not dictate Autoformat's internal contracts or require Autoformat to understand Hygiene state.
7. Favor explicit code and existing SDK/Roslyn services over a new general-purpose rule DSL, plug-in model, framework, or rule-by-rule dependency graph. Extract shared infrastructure only after actual repeated use justifies it. No arbitrary per-repository styling convention or configurable threshold catalog; canonicalization is opinionated by default.

### Collection-expression decision (explicit non-equivalence)

Convert eligible constructions to C# collection expressions when a syntactically supported target-typed form is available, **even if the concrete runtime collection type can change**. In particular, `IReadOnlyList<T> x = new List<T> { ... };` is expected to become `IReadOnlyList<T> x = [ ... ];` when applicable. Code that later downcasts the interface to `List<T>` relies on an undesirable hidden implementation assumption; a resulting failure is an **agent repair task**, not a reason to suppress this general canonicalization. Do not introduce casts or artificial `List<T>` preservation solely to make the prior implementation survive.

### Source-local exception contract

Use one Autoformat-owned annotation, on the directly following applicable statement/declaration (or its immediately associated leading trivia):

`// autoformat:retain <rule-id> -- <non-empty justification>`

A valid annotation exempts only that construct from that named canonicalization. It is not a file-wide/global opt-out; unrelated rules still execute. Unknown, malformed, or unjustified annotations must be reported rather than silently treated as valid exemptions. Preserve the annotation during formatting. Use precise identifiers for layout and modernization families; there is no obligation to recognize former `hygiene:retain` syntax in Autoformat. Do not silently delete user-authored comments.

### Idempotence and phase interaction

A successful normalize pass must converge: a second pass with the same source/configuration produces no further diff. SDK style/whitespace, custom modernization, readability, and comment formatting must not undo one another or oscillate. The implementer chooses ordering, batching, and internal types, but the observed outcome is one canonical form. `format` continues to mean SDK whitespace-only; default/`normalize` performs the complete opinionated canonicalization. `init` owns its mechanical policy only. Check-only assesses the applicable canonicalization without writing source or policy/review state.

## Scope and exclusions

### A. Clean Autoformat and remove inherited compatibility

- Remove Hygiene-aware policy ownership/adoption mechanisms and terminology from Autoformat (including specialized Hygiene initialization hooks and `hygiene-*` temporary path names). Hygiene may depend on Autoformat's normal services; no reverse dependency or hidden Hygiene profile requirement.
- Reduce copied Autoformat `RepositorySession` infrastructure to formatting-relevant target discovery, SDK project context, and Git operations. Do not carry over Hygiene-only review facts, merge-base/changed-line services, relationship caches, or unrelated compilation machinery solely because they were copied in M0026. Keep what genuine implementation needs.
- Retire no-op historical mechanical IDs and redundant projections from the active Hygiene catalog where they duplicate Autoformat or claim review/fix behavior they do not implement. Do **not** rebuild Autoreview or delete live semantic-review capabilities. No old-ID or old-profile migration guarantee is needed; source/code safety remains mandatory.
- Make this workbench repository's operative formatter policy Autoformat-owned. Review bundled Roslyn analyzer shipping: remove redundant assets if the installed SDK already supplies the invoked behavior; retain only demonstrably needed dependencies.
- Clarify names, results, ownership, and active documentation. Report actual formatter phases, transformations, diagnostic identities, skips and affected paths; do not imitate a superseded Hygiene rule catalog for compatibility.

### B. Automatic modernization portfolio

Promote **all nine** existing candidate families into Autoformat **actual automatic transforms**, not merely reports or advisory review batches. Use their live candidate detectors/SDK diagnostics as implementation input when economical, but do not retain a semantic-review acceptance loop for these canonicalizations. Defaults:

| Family | Opinionated canonical target |
|---|---|
| `modernization.var-style` | Prefer explicit local variable types where expressible, rather than discretionary `var`; keep `var` where C# requires it (e.g. anonymous types). Pair with target-typed object creation where appropriate. |
| `modernization.expression-body` | Prefer expression-bodied single-expression methods/properties/accessors and comparable obvious members; retain block form for real multi-statement logic. |
| `modernization.collection-expression` | Prefer C# collection expressions where syntactically target-typable; do not require preservation of concrete collection implementation. |
| `modernization.primary-constructor` | Prefer primary constructors for recognizable constructor-to-state/dependency patterns, including eligible ordinary classes/structs. |
| `modernization.switch-expression` | Prefer switch expressions for directly convertible value-returning/assigning switches. |
| `modernization.conditional` | Prefer conditional/coalescing expression forms for recognizable expression-shaped branches. |
| `modernization.object-initializer` | Prefer initializer form for recognizable construction followed by member assignments while retaining the necessary source operations. |
| `modernization.readonly-field` | Prefer `readonly` fields for recognized initialization-only state; agent fixes any resulting compilation violations. |
| `modernization.inline-declaration` | Prefer inline `out` declarations for eligible existing local declarations. |

Each family must **actually rewrite representative eligible source**, not remain an empty registration, a review-only finding, or an SDK diagnostic name without an active fix. Support useful common populations with explicit, limited exclusions for syntactically impossible, unrecognizable, or structurally dangerous cases; don't require universal coverage, speculative equivalence proofs, or exhaustive controlled-case suites. Document meaningful unsupported patterns honestly. Modernization of language constructs must respect supported effective C# syntax/language versions rather than emitting unsupported grammar gratuitously.

The normal supported language baseline is the current .NET 11 SDK/C# environment; do not build a cross-version compatibility framework solely for this milestone.

### C. Readability canonicalization

Implement concrete layout and comment transformations **now**, rather than deferring them as semantic review:

- **Visual blocks:** insert a separating blank line before a control-flow statement that follows ordinary linear statements, and after a completed control-flow statement when followed by an ordinary statement. Keep `else`, `catch`, `finally`, related clauses, and leading comments attached correctly; don't split directives or documentation trivia. Handle nested blocks and repeat runs predictably.
- **Parameter declarations:** methods/constructors with **three or more parameters** use one parameter per line, regardless of original physical line length. Two-parameter declarations that exceed the line target may also wrap. Extend consistent treatment to comparable signature forms when syntactically appropriate.
- **Long lines:** target **120 characters as a soft readability target**, not an impossible universal prohibition. Apply syntax-aware wrapping for long invocation argument lists, collection expressions, object/collection initializers, fluent chains, and compound boolean conditions. Prefer stable line breaks at construct/operator boundaries; no arbitrary splitting of identifiers, literals, comments, directives or XML prose. If no safe recognized break exists, leave the line intact and report the limitation, rather than pretending compliance.
- **Comments and whitespace:** correct indentation of ordinary comments and leading trivia in eligible code; preserve intentional attachments, comment contents, string contents and source newline conventions. Normalize straightforward redundant blank-line/layout patterns and avoid excessive blank-line churn.
- The former `readability.long-line.review` and `readability.control-flow.visual-block` advisory checks should no longer be the sole implementation of these same policies. Retire or repurpose them without duplicating incompatible formatter/review rules.

Use **80/19/1 as a design heuristic, not a measured coverage quota**: canonicalize the common straightforward population, deliberately choose conventions for debatable but generally useful cases, and support narrowly justified exceptions for outliers. Imperfect aesthetics are acceptable; unpredictable edits, lost comments, or non-convergent layout are not.

### D. Engineering and documentation

- Make ordinary Autoformat development checks directly invocable and **cheap** (affected project build, focused test project, a small disposable SDK-project integration). Do not route routine execution through the M0026 installed-package consumers, self-hosting, fingerprinted aggregate receipts, or entire Hygiene verification pipeline.
- The existing release-grade scripts may remain available as explicitly named release/investigative commands; they must not be implicit dependencies of the normal Autoformat development path. Remove obsolete or misleading invocations/documentation, without building a new validator product.
- Update operative `README.md`, `docs/ARCHITECTURE.md`, `docs/ENGINEERING.md`, `docs/SPECS.md` and affected `docs/specs/*` so current Autoformat policy is unambiguous and historical incompatible contracts are not presented as instructions. Historical milestones/research remain history, not runtime authority. No broad archival migration or documentation proof matrix.

**Not in scope:** new Autoreview executable; project index; project-organization enforcement tool; runtime project build/check gate; automatic model/agent integration; IDE/editor daemon or plugin host; arbitrary repository configuration knobs; public publishing/release packaging qualification; new cross-platform guarantees; semantic-documentation prose generation; automatic architectural/behavior/test-quality reviews. Existing `profile.dotnet.analysis.required` and `profile.stylecop.prohibited` are **not** imported as Autoformat source-formatting rules; eventual project-policy enforcement belongs to a separate workstream. Autoformat still owns its mechanical `.editorconfig` settings.

## Acceptance

1. Independent Autoformat can initialize its own policy and format/normalize supported C# sources without reading Hygiene profile, review, approval, or audit state; inherited compatibility hooks, misleading mechanical catalog placeholders, and duplicated policy ownership have been removed or isolated outside Autoformat.
2. All nine modernization families produce observable canonical edits on meaningful eligible examples, including collection-expression conversion across an `IReadOnlyList<T>` target **without preserving a concrete `List<T>` instance for downstream casts**. Ineligible patterns are skipped/reported truthfully rather than requiring blanket equivalence checks.
3. Visual-block transformations, three-or-more-parameter vertical declarations, long-line syntax-aware wrapping, and basic comment/blank-line normalization produce consistent, reviewable source edits.
4. Justified per-rule `autoformat:retain` annotations suppress only the intended construct/rule, remain intact across runs, and report invalid annotations. Other eligible transformations still apply.
5. Default/`normalize` converges after a successful run; a second pass is byte-stable on representative modernized/layout fixtures. `format` remains SDK whitespace-only. Existing changed/check modes remain read-only and report actual proposed edits.
6. **The Autoformat executable never invokes a target-project build or requires successful compilation to apply or keep a transformation.** Roslyn semantic/compilation data may help recognize candidates, but is not a final correctness/equivalence gate. Ordinary recognized rewrites need not preserve prior runtime semantics or result in a compiling project; agent-driven build/test/repair is the next stage. Autoformat itself remains a buildable, testable development artifact.
7. User source, comments and configuration outside explicitly owned transforms are not silently destroyed; generated-source exclusions and clean Git/checkpoint/partial-failure behavior remain truthful. The live Hygiene review capability is not silently deleted as a side effect of cleanup.
8. Operative code/documentation and an inexpensive normal Autoformat test command describe and support the actual new architecture. Historical compatibility claims and mandatory milestone-stage release qualification no longer govern new Autoformat work.

## Validation

**During implementation:** use affected-project build/static feedback and focused tests only as useful for developing the formatter. This is **the agent's development feedback**, not an action performed by Autoformat on consumer projects. Make disposable implementation experiments; discard them when unhelpful.

**At milestone completion:** one affected solution/project build of the workbench code as needed, focused transformation tests, and a **few** representative disposable .NET SDK-style C# project fixtures executed through the actual formatting engine (not through the packed/installed final executable). Favor a combined modernization/layout fixture, an exception/comment/selection fixture, and an idempotence/partial-failure check; choose the cheapest compelling coverage based on observed implementation risks. An architectural/diff review should establish the nine-family coverage, dependency direction, live-review preservation, and absence of runtime build/semantic-equivalence gates. No per-acceptance test inventory, exhaustive negative-case matrix, ledger reconciliation, or validation receipts.

**Important risks:** unexpected loss of source/comments, phase oscillation, malformed rewritten syntax, accidental Hygiene dependencies, false reports, or user edits lost during policy initialization. Use small focused checks against the actual defect; add durable regressions only for recurrence-prone deterministic failures. **Compilation errors or runtime behavior changes caused by explicitly approved opinionated transformations are not themselves Autoformat contract violations.** The implementer may build/test transformed fixtures for investigation but must not encode successful downstream compilation or semantic equivalence as a formatter acceptance gate.

**Deferred to release:** installed `dotnet tool` packaging/shim, real-repository dogfood, full Hygiene regression/release suites, broad platform/consumer matrix, and final package qualification. **Do not execute `eng/autoformat/validate.ps1` if it still implicitly performs installed-consumer qualification**; use direct focused commands or simplify it first. No self-hosting, installed-distribution qualification, aggregate receipt freshness, or formal human sign-off is required for M0027.

## Documentation and other obligations

The implementation agent must directly update operative documentation affected by the new policy; historical planning files can remain unchanged. No compatibility or migration support is required for unused pre-1.0 consumer state; do not silently discard unrelated user-owned content. No machine provisioning, credential changes, CI/workflow changes, public publishing, or mutation of non-disposable external repositories is authorized. Disposable source fixtures and temporary Git repositories are authorized.

**Lightweight resume ledger:** `.execution/M0027-opinionated-autoformat-canonicalization.md` is supplied because this coherent, multi-family milestone may be long-running or interrupted. Use it as **optional operational progress state**: update meaningful completed work, current defects, useful focused checks and the next resume action at checkpoints. It never overrides this milestone, and neither completion nor review requires criterion-by-criterion reconciliation, a proof matrix, evidence-case registry or receipt cache. Do not expand it into validation bureaucracy.

## Completion note (filled after implementation)

Delivered an Autoformat-owned Roslyn syntax canonicalizer for all nine modernization families, source-local `autoformat:retain` annotations, vertical parameter declarations, syntax-aware wrapping, visual-block spacing and redundant blank-line normalization. Collection expressions are intentionally emitted for eligible list/array constructions without preserving concrete `List<T>` assumptions. The formatter reports supported phases, canonicalization identities and residual long-line notices; it does not compile transformed projects or require semantic equivalence. Hygiene policy adoption and duplicate mechanical/readability formatter catalog entries were removed while semantic-review modules remain.

Focused verification: `dotnet test tests/DotNetAiWorkbench.Autoformat.Tests/DotNetAiWorkbench.Autoformat.Tests.csproj -c Release --no-restore` passed (12/12), including syntax parsing, source exception/comment handling, long collection wrapping, C# 11 grammar gating, engine normalization and second-pass stability. Autoformat CLI/Core and Hygiene CLI affected-project builds passed during implementation. The installed tool, aggregate/self-host qualification, real-repository processing and full Hygiene suites were not run; these remain release checks.

Known limits: transformations deliberately cover recognizable syntax shapes rather than every semantic candidate. Explicit-type inference covers common literals and arrays; collection expressions cover array/list initializer forms; primary constructors require constructor bodies consisting of direct field assignment; switch expressions cover one-return-per-arm switches; conditionals cover paired returns or same-target assignments; object-initializer conversion covers a single simple assignment following construction; readonly inference is source-file assignment based; inline declaration handles a one-use uninitialized `out` local. Long-line wrapping handles recognized syntax boundaries and reports remaining non-comment lines beyond 120 characters. Comments within uncertain rewrite shapes cause that rewrite to be skipped. The resulting consumer code may require agent-owned repair.
