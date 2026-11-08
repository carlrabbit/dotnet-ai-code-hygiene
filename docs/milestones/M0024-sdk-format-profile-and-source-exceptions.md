# M0024 — SDK-Backed Hygiene Profile and Source-Owned Modernization Exceptions

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | **ready** — implement after applying this overlay to the exact PR 25 source baseline |
| Mode | `ai-executed-human-reviewed` |
| Guide selection (planning) | Agentic Project Guides **0.9.3**, `base` + `cli-tool`, repository-wide |
| Repository role / maturity | developer-tool / greenfield |
| Baseline implementation model | project-configured **GPT-5.6 Luna** |
| Baseline executor readiness | confirmed: SDK/EditorConfig/Git/semantic contracts resolved below; local Roslyn/CLI mechanics remain implementation-owned |
| Decision preservation | confirmed: source-of-truth, policy, suppression, Git recovery, migration, and validation boundaries fixed here |
| Execution tractability | confirmed: one cohesive consolidation, planning-seeded lossless ledger, implementer-owned bounded work packages |
| Scope size | large, cohesive architectural consolidation |
| Implementation autonomy | high for code/test mechanics; no changes to policy, exemptions, or validation target without planning |
| Execution ledger | `.execution/M0024-sdk-format-profile-and-source-exceptions.md` |
| Baseline repository | `carlrabbit/dotnet-ai-code-hygiene`, **PR #25 head `6f9fa952def8da4a36c0de715bd206f7223a7f7c`**, branch `codex/m0023-deterministic-cleanup` (observed 2026-11-07); **do not start from unmerged `main`** |
| Relationship to M0023 | user accepts M0023 implementation; M0024 *supersedes its mechanical-rewrite, transaction, and modernization-verdict persistence decisions*, not its established semantic review and audit behavior |
| Target supported environment | Windows 11; installed .NET 11 SDK and its `dotnet format`; Git, PowerShell; real SDK-style C# consumer projects |
| Environment authority | existing SDK, Git, NuGet, PowerShell and isolated per-test worktrees/local feeds; no host-wide installs, admin, CI/workflows, network services or credential changes |
| Direct documentation sync | affected project authority and installed consumer guidance in this milestone; unrelated documentation deferred |
| Human review | blocking independent milestone-scoped review: `.review/REV-M0024-COMPLETION.md` |

## Goal

Replace unnecessary hand-written standard C# cleanup logic with the supported .NET SDK's batched `dotnet format` behavior, make an opinionated committed `.editorconfig` the authoritative mechanical hygiene policy, use a known Git commit instead of an elaborate filesystem-wide rollback transaction for ordinary agent execution, and retain justified modernization exceptions immediately in source code. Keep the distinctive hygiene functionality—compiler-aware candidate discovery, small semantic samples, human/agent judgment and exhaustive read-only audit—intact.

## Target State

An agent on a clean, isolated Git checkout with a known baseline commit runs `dotnet hygiene`; the tool reconciles its accepted profile, runs **repository-wide** approved standard formatting/style cleanup in a small number of SDK CLI passes, executes only genuinely hygiene-specific deterministic logic, then produces bounded semantic review work. A clean subsequent run makes no source edits. A failed run reports incomplete and leaves an inspectable worktree; the agent explicitly repairs or restores it and creates a hygiene commit only after validation. Fixed profile policy lives in the effective repository-root `.editorconfig`. A retained modernization is represented by a **narrow, justified source-local suppression**, not by a durable per-gate JSON verdict. A read-only audit remains read-only.

## Scope

- Profile contract upgrade from current `dotnet-11 v2` to **`dotnet-11 v3`** as the explicit supported policy migration. Retain mandatory analysis profile infrastructure (`AnalysisLevel=11`, enable analyzers, enforce code style in build) and the StyleCop boundary. Reconcile root `.editorconfig` and managed profile artifacts with explicit externally approved baseline change when material.
- Treat the standard mechanical formatting policy as a *fixed profile default*, not an individually disableable set of code-rewrite programs. Retain stable rule IDs/descriptors as useful policy/reporting identifiers, even when their old source-rewriter classes disappear. Keep the configurable semantic/review rules and established mandatory profile rules.
- Run supported SDK `dotnet format` `whitespace` and `style` **in batches** against the complete supported project/solution population for the normal autonomous gate. Use explicit approved style diagnostic allowlist and effective EditorConfig; do not run arbitrary analyzer fixes or `dotnet format analyzers` by default. Manual format/normalize scopes may be narrower; `--check` must remain nonmutating.
- Establish and test an explicit *capability disposition* for every M0023 deterministic cleanup rule: standard SDK formatter, standard SDK style fix, retained small custom hygiene rule with documented necessity, or review-only/unsupported. Do not silently expand behavior when a built-in fix has broader semantics than M0023's former special-case rewrite.
- Preserve ordinary source and project eligibility, formatting/normalization result semantics, idempotence, compiler verification when actual semantic transformations warrant it, and accurate change reporting. Avoid duplicate formatting passes when the SDK has already produced final formatting.
- Replace normal `AutonomousMutationTransaction` and `RewriteEngine` all-file rollback with **Git checkpoint preconditions + explicit failure/recovery**. Keep atomic writes for individual tool-owned metadata, no lost user edits, protected source/index, and no tool-created commits, pushes or destructive Git resets. Do not broaden `--yolo-all` mutation rights.
- Replace modernization-specific durable `desirable|retain|uncertain` JSON as acceptance authority with (a) updated code, (b) justified source-local suppression, or (c) outstanding/uncertain review work. The existing six non-modernization semantic rule acceptances and separate deterministic-ignore records are not globally removed.
- Support source `#pragma warning disable/restore IDE...` with meaningful rationale for enabled **formatter-handled, suppressible** IDE modernization rules; for hygiene-only modernization rules support one narrow source-comment syntax specified below. Validate syntax/scope and stale/invalid exceptions without inventing semantic approval.
- Reconcile CLI commands, baseline/profile updates, rule listing, result/report identities, help, exhaustive audit, documentation, Core/CLI tests, and installed consumer behavior against these decisions.

## Non-goals

- No new semantic modernization families, dynamic per-rule parameters, per-rule `dotnet format` process calls, always-on daemon, universal code-fix host, or platform-agnostic formatter abstraction.
- No generic `dotnet format analyzers`, third-party auto-fixes, StyleCop support, broad warning-as-error policy, arbitrary IDE diagnostics or code-style rules outside the approved formatter allowlist.
- No auto-generated commits, implicit `git reset --hard`, `git clean`, clobbering of existing edits, or rollback guarantees supplied by the Git history before a commit is created.
- No generalized cross-language suppression parser, arbitrary file/project-wide rule exceptions, or treating a plain comment as native suppression of a diagnostic that `dotnet format` will still fix.
- No redesign of the existing six semantic rules, nine modernization populations, bounded five-package shared selection, or the report-only `--yolo-all` protocol.
- No retrospective editing of M0023's historical evidence/acceptance or requiring its incomplete PR review metadata to be fabricated. No unrelated guide-repository change, new CI pipeline, public NuGet publishing or release readiness campaign.

## Resolved Architectural and Behavioral Decisions

### 1. Baseline, authority and policy

PR 25's exact head is the implementation base, even though the PR was observed open/draft; its acceptance is a **user planning decision for M0024**, not a claim that a GitHub review or merge was recorded. Create the M0024 implementation branch **from that commit**, apply this overlay, and ensure PR history keeps the M0023 code reachable. The implementation PR may target PR 25's branch until merge, or be retargeted to `main` after PR 25 merges; do not silently assume `main` contains M0023.

The committed root `.editorconfig` plus the required MSBuild profile are the **platform-facing mechanical-policy outputs**. The hygiene profile/rule catalog is the authoritative *generator and validator* of those outputs. `dotnet format` consumes them; no parallel custom syntax engine should implement equivalent standard rules. Do not put a second `.editorconfig` under `.hygiene/` and expect the SDK to automatically apply it. The root `.editorconfig` has `root = true` and a hygienically managed block using **valid `#` EditorConfig comments**, not HTML/XML `<!-- -->` markers. Preserve unrelated user-authored sections verbatim where practicable.

Version `dotnet-11 v3` makes the standard mechanical profile **mandatory as a coherent whole**. Existing independent source-rule toggles for now-profile-owned mechanical style cease to override that baseline; existing semantic/review rule toggles continue. Profile migrations must visibly account for any prior disabled mechanical rule, refuse to silently discard a user's contrary policy, and require the established explicit baseline approval. The migration may translate a narrow, justified per-occurrence intent to a native/source exception, but must never manufacture a reason or approve it on the user's behalf. Preserve unrelated disabled-rule entries. No arbitrary per-repository profile values/severity tuning.

For the supported C# project inputs, compare the **effective** EditorConfig/analyzer configuration against fixed expected values, including nested `.editorconfig`, per-project effective settings and multiple applicable projects. A text match in the root file is insufficient. Conflicting nested values or unsupported SDK diagnostics result in actionable status; do not force overwrite arbitrary nested files. Use Roslyn/SDK-effective analyzer settings where feasible rather than an ever-growing independent EditorConfig parser.

### 2. SDK-first formatting, deterministic scope and rule ownership

Normal autonomous cleanup runs over **all supported project source files**, regardless of the current changed-file gate used for semantic sampling. This is intentionally *incremental in edits, not necessarily incremental in execution cost*: an unchanged compliant repository should produce zero diff. Initial bootstrap/profile upgrades may legitimately produce broad, clearly reported diffs. Generated/unsupported sources remain outside supported eligibility; cover multi-project and shared-file cases.

Use the supported `dotnet format` CLI as the mechanical implementation boundary, not discovery of private SDK `dotnet-format.dll` paths. Use the project's applicable .NET SDK, verified/pinned supported SDK behavior, and invoke as few **batched passes per complete relevant solution/project set** as coverage allows, e.g. whitespace and style with a fixed approved diagnostic allowlist. Do not issue one formatter process per rule. A solution may be used only when it covers the supported repository projects; otherwise cover omitted projects without duplicated or inconsistent edits. Use `--no-restore` only when prerequisites exist. Do not run style without an explicit approved diagnostic set or rely on ambient third-party analyzer severity to decide what changes.

Retain CLI-level distinctions: `format` applies standard layout/formatting behavior; `normalize` applies approved deterministic style/code fixes and finishes in the formatted state; normal autonomous run applies both. Attribute results truthfully to *formatter phases and candidate rule IDs*; do not fabricate exact per-character causal attribution. If a standard SDK formatter feature is not accessible through the chosen batched invocation, **keep a small documented specialized implementation only when required**, or classify the rule as review-only/unsupported rather than claiming equivalent cleanup.

The approved **initial automatic style diagnostic inventory** for the v3 SDK profile is: IDE0005 (unused imports), IDE0011 (braces), IDE0036 (modifier order), IDE0040 (accessibility), IDE0090 (target-typed creation when type apparent), IDE0034 (default literal), IDE0004 (unnecessary cast), IDE0071 (interpolated-string simplification) and IDE0037 (inferred names), subject to the real SDK exposing the corresponding fixer with equivalent safe policy. `dotnet format whitespace` and SDK import organization are separate from this style-diagnostic set. Do not add IDE0300-family collection expressions, IDE0017 object initializers, IDE0290 primary constructors, expression bodies, conditionals, switch expressions or other structural modernizations to the **automatic** set in M0024; they remain *preferred-by-default semantic review* with justified exceptions. Existing narrowly justified handling for IDE0047 (parentheses) and IDE0080 (null-forgiving), as well as redundant qualification and excess blank lines, stays specialized unless actual SDK parity is established. A mandatory candidate that the SDK cannot implement equivalently remains a reported specialized rule or honest review-only gap; it is not quietly made a broad automatic fix.

The M0023 cleanup rule inventory must be classified individually (imports sorting/removal, modifier order, IDE0011/IDE0040, target-typed object creation, default literal, casts, interpolation, inferred names, null-forgiving, parentheses, qualification, whitespace and comment/trivia handling). A policy key/diagnostic ID's presence is **not proof** that the SDK fixes it. Establish precise parity/coverage in the real .NET 11 SDK, honoring language version, effective configuration, trivia, binding and original target restrictions. Do not silently broaden behavior to all IDE0090 or other candidates if current policy was narrower; changed policy requires explicit documented migration and tests. In particular, a contextual `MakeHttpRequest(new(1,2,3))` is a desired modernization scenario but **must not be asserted to be covered by IDE0090** without verified SDK behavior; unsupported or intent-dependent cases stay in semantic review/future policy work, not unsafe textual rewrites. Keep material custom behavior only with evidence why standard SDK cannot satisfy it.

### 3. Git as recovery checkpoint, not filesystem atomicity

Normal **mutating** autonomous hygiene requires an isolated checkout with an identifiable `HEAD` and clean relevant source/config/index state at entry. The agent commits its implementation changes first and owns the subsequent hygiene commit. The tool records baseline `HEAD` and checks for unexpected index/HEAD/concurrent changes; if the precondition is not met, it reports an explicit incomplete/blocking condition and makes no changes. No commit/push/reset/clean by the product.

Formatting edits are ordinary working-tree writes. A failure/interruption can leave partial edits: report precisely that status and changed paths; **never claim rollback or completed hygiene**. The caller may inspect/repair, explicitly restore the known baseline, or commit a corrected candidate then rerun. An initial baseline commit is a recovery point, not atomicity of ongoing file writes. Protect against unintended overwrite of concurrent changes. Keep metadata writes atomic and human-review approvals separate. Manual `format`/`normalize` with explicit targets must preserve existing nonmutating `--check`; document and validate any clean-entry requirement for mutating commands consistently rather than silently ignoring dirty worktrees. Do not force clean state in exhaustive read-only audit.

Bootstrap and profile migration require a known, protected source/config baseline and explicitly reviewed approval of the resulting policy diff. They may touch many files. They must preserve unrelated files and report incomplete on partial failure, not pretend a Git commit automatically reverted a failed formatter. Do not silently approve baseline/report snapshot mismatches.

### 4. Source-local justified exceptions and verdict lifecycle

The default judgment for supported modernization opportunities is **prefer the idiomatic modern construct**. Retention is permitted only for a concrete local readability, behavior, interop, or performance rationale. The agent cannot satisfy a candidate by merely recording `desirable` while leaving old code unchanged. The possible durable outcomes are modernized code or a source-attached justified exception. `uncertain` remains outstanding/escalated, not accepted.

For SDK-managed diagnostics such as IDE0090, use a **narrow native `#pragma warning disable IDE...` / `#pragma warning restore IDE...` pair**, with a same-line or adjacent meaningful explanatory comment; native suppression is required to make the SDK skip rewriting. The tool validates rule ID, matched pair, containment, locally bounded scope and nonempty nonboilerplate explanation. It cannot prove semantic truth of the rationale. A malformed, unrelated, unbalanced, overly broad, or ineffective suppression is a finding, not an accepted exception. Test actual SDK formatting to prove the intended rule is suppressed and other formatter/rule behavior still applies. Not all whitespace-formatting settings are suppressible per occurrence; do not claim a generic comment can disable SDK whitespace formatting.

For hygiene-only modernization reviews, the precise source-comment syntax is:

```csharp
// hygiene:retain modernization.collection-expression -- Concrete List<T> identity is required by the interop API.
IEnumerable<Item> items = new List<Item> { first, second };
```

This attaches only to the *immediately following source declaration or statement*, with no intervening unrelated executable construct. It suppresses the named modernization rule's candidate(s) **in that one syntax subject**; it cannot suppress arbitrary other rules, a file, or descendants beyond that subject. The parser uses source syntax/trivia and reported candidate ownership, not line-number-only matching. It is invalid without a meaningful reason or when the named rule is unknown. Multiple occurrences of the same rule in one subject share one rationale; if that would hide different intents, the agent splits code or narrows the relevant subject. Retained exceptions remain committed in C# source, visible to code review.

Source-local exception recognition affects modernization selection **without hiding original source facts from the explicit exhaustive read-only audit**. Normal gates exclude a valid suppressed candidate from review and report the exception count and reason. The explicit audit enumerates suppressions as retained/annotated obligations rather than silently deleting population; it distinguishes no candidate, valid suppression, malformed suppression and an otherwise existing candidate. Changed surrounding code can invalidate or call into question a rationale; mechanically stale/native-unnecessary suppression is reported when demonstrable (IDE0079 may assist), while a semantically obsolete rationale is a review concern, not an automatic accusation. No periodic full-repository agent review of unchanged exceptions is introduced.

Retire `.hygiene/.state/modernization-decisions.json` as the **durable authoritative acceptance** source. Do not migrate local gate verdicts into invented source suppressions or count them as accepted after migration. Retain only ephemeral batch/session coordination if needed. The six preexisting semantic acceptance records and distinct deterministic ignores remain under their existing contracts. Adjust `review decide` or its replacement so `retain` requires a validated source suppression and `desirable` requires actual rewritten source; `uncertain` is the explicit remaining state. Stale handles must still reject decisions. A newly introduced exception must be revalidated against current rule/source context before counted as accepted.

### 5. Rule catalog, CLI, audit and documented migration

A rule ID continues to identify product policy, capability, version, and reporting meaning, but **not necessarily a dedicated source-rewriter class**. Built-in mechanical policies may be declarative profile contributions/approved diagnostic mappings. The catalog remains explicit, static, ordered and auditable. Do not create a new configuration DSL, reflection-based plugin discovery, or generic code-fix platform. Keep specialized custom rules independently testable and local to their behavior. The formatter owns conventional whitespace/import organization and SDK built-in transformations where proved equivalent; hygiene owns configuration, validation, scope, reporting and semantic assessment.

Preserve public command names and their intended affordances (`dotnet hygiene`, `bootstrap`, `update`, `format`, `normalize`, `check`, `rules`, `review`, `--yolo-all`) when possible; the project remains pre-1.0, so remove obsolete commands/states with explicit migration notes rather than permanent aliases. A normal `check` remains diagnostic, manual `--check` is nonmutating, and `--yolo-all` remains **explicitly authorized, exhaustive, read-only**, with correct per-rule accounting and bounded transport but no normal-workflow mutation. Do not run a mutating SDK format command to implement read-only audit; use the supported `--verify-no-changes` / report-only mechanism and verify no user source/config/state mutation. Do not rely on a misleading single 'clean' verdict after only some approved diagnostic IDs were evaluated. Preserve the separation of formatting population (full eligible project set) and changed-gate semantic population (limited by source change).

### 6. Validation and deployment boundary

No model integration, new hosted CI, or uncontrolled SDK installation. Validate on the already-supported **Windows 11 + .NET 11 + PowerShell + Git** environment. Use the existing M0020 resumable stage plan (`./eng/validate.ps1 -PlanJson`, `-Shard`, `-Verify`) and stage receipt freshness to avoid repeating expensive validation needlessly. The current exact newly packed **installed** tool must exercise the autonomous formatting, profile migration, source suppressions and read-only audit—not just in-process helper tests. An SDK formatting parity comparison is a required **behavioral acceptance test**, not a justification for weakening safety because it usually emits no diff.

## Baseline Executor Readiness

This is one settled, large but coherent consolidation. The configured baseline executor has the authoritative repository, a pinned source commit, explicit supported SDK/EditorConfig suppression semantics, precise source-comment and Git state contracts, migration policy, and stable AC/EC acceptance evidence requirements. It may choose implementation types, CLI process wrappers, fixtures and work-package order but must not decide whether to allow blanket overrides, destructive recovery, per-rule formatter calls, or silent modernization retention. If the actual installed SDK lacks a mandatory advertised capability, leave that narrower rule unautomated, surface concrete evidence, and return any *material* policy change to planning; do not invent an equivalent fixer.

## Decision Preservation

M0024 deliberately supersedes M0023's source-transaction, per-rule mechanical rewrite, and durable modernization-decision storage architecture. It **does not** rescind the already accepted semantic population and bounded sampling contracts. The real built-in formatter and native diagnostic suppression are required integration boundaries. The implementation agent must not derive a new product philosophy from old M0023 source files; this ready milestone takes precedence wherever they conflict. The planning conversation is not an implementation dependency.

## Execution Tractability

Plan the live work as bounded implementation work packages after verifying the lossless acceptance/evidence registry. The milestone is coherent because the SDK/profile, Git baseline, source-owned exceptions, and reports are facets of one simplification of the execution model. Use the planning-seeded `.execution/` file for obligations, validation mapping, evidence and eventual resume state; the implementation agent owns all later work-package mapping, concrete evidence and progress. Do not compress separate AC/EC obligations into generic 'tests passed'.

## Required Authority

- `AGENTS.md`, `README.md`, `docs/ENGINEERING.md`, `docs/ARCHITECTURE.md`, `docs/SPECS.md`
- `docs/specs/PROFILE.md`, `docs/specs/RULE-CAPABILITIES.md`, `docs/specs/REWRITES.md`, `docs/specs/AUTONOMOUS-WORKFLOW.md`, `docs/specs/HYGIENE.md`
- `docs/specs/SAMPLING.md`, `docs/specs/SEMANTIC-REVIEWS.md`, `docs/specs/REVIEW-HANDOFFS.md`, `docs/specs/AGENT-HELP.md`
- M0023 ready milestone and its accepted source state as *historical* context; current profile and review state are in the actual PR 25 head.
- `eng/validate.ps1`, current validation stages, real Core/CLI tests and installed-consumer fixtures.
- Platform behavior: supported .NET 11 SDK CLI `dotnet format` (`whitespace`, `style`, diagnostic filters, `--verify-no-changes`) and native C# `#pragma warning` suppression. These are external platform contracts, not an instruction to load an external guide repository.

## Acceptance Criteria

- **AC-01** — The supported `dotnet-11 v3` profile has a deterministic, committed root `.editorconfig` containing valid `#`-comment hygiene-managed boundaries, approved standard C# whitespace/import/style preferences and no arbitrary SDK fixer expansion; existing unrelated EditorConfig content and required MSBuild infrastructure are preserved.
- **AC-02** — Profile checks validate **effective** rule options/severity for every eligible project source context, including nested EditorConfig and conflicting project overrides; root-file text presence alone cannot establish compliance or silently override a deliberate conflicting source policy.
- **AC-03** — Bootstrap/update migrate valid v2 state to v3 and distinguish fixed mechanical profile defaults from still-toggleable semantic rules; previously disabled mechanical rules and materially changed baseline policy are reported explicitly and require the established external baseline approval, never silently accepted or converted to invented exceptions.
- **AC-04** — Normal autonomous cleanup uses supported batched SDK `dotnet format` whitespace/style passes over the full eligible repository project population, with a fixed approved diagnostic allowlist, source eligibility, no per-rule processes, no broad analyzer fixes and no dependence on private SDK formatter assembly paths.
- **AC-05** — Every M0023 deterministic cleanup rule has documented, executable SDK parity evidence and an explicit disposition (SDK, documented necessary specialized hygiene rule, or nonautomatic review/unsupported); redundant custom rewrite logic is removed, and safety-sensitive narrower behavior is neither silently broadened nor claimed without proof.
- **AC-06** — Standard import organization, unused-import removal, modifier order, braces/accessibility and other approved formatting/correction behavior preserve necessary semantics, directives/comments and configured style, and produce zero source diff on a second unchanged run.
- **AC-07** — Manual `format`, `normalize`, scoped variants and check-only variants continue to explain their selected SDK/specialized capabilities and changed paths truthfully; check-only source/config work is nonmutating, and normal autonomous formatting covers the entire supported repository rather than only the current semantic change gate.
- **AC-08** — Ordinary mutating hygiene requires a clean eligible Git starting checkpoint and a stable recorded `HEAD`/index; dirty or conflicting starts stop without mutation, the tool neither creates commits nor performs destructive resets, and success permits an explicit subsequent agent-authored hygiene commit.
- **AC-09** — Failure, interruption and concurrent modification leave an explicitly incomplete and inspectable working tree, with no false rollback/completion claim or overwrite of unrelated user edits; recovery is agent-owned from Git, and individual tool-owned metadata writes remain safe.
- **AC-10** — Supported SDK-managed modernization exceptions use precise native `#pragma warning` suppression/restore pairs and meaningful adjacent justification; unsupported, malformed, unbalanced, overbroad or ineffective suppressions are not treated as accepted, and other enabled formatter rules remain active.
- **AC-11** — Hygiene-only modernization exceptions use the fixed `// hygiene:retain modernization.<rule-id> -- <reason>` convention attached to one immediately following declaration/statement; only matching eligible candidates in that subject are excluded from ordinary sampling, and stale/invalid/unknown-rule comments are reported rather than silently suppressing work.
- **AC-12** — Modernization verdict workflow no longer treats local `modernization-decisions.json` as durable approval: accepted modernization is evidenced by changed source, retained legacy syntax by a valid committed source suppression, and uncertain work by outstanding status; no previous local verdict is silently converted into accepted source state.
- **AC-13** — The existing nine modernization populations, shared five-package budget, occurrence identity/context freshness and six independent semantic-review budgets remain behaviorally intact, apart from excluding valid source-owned exceptions in normal review and expressing modern syntax as the preferred outcome.
- **AC-14** — Explicit exhaustive `--yolo-all` remains authorized and nonmutating, enumerates relevant standard-format drift and enabled semantic modernization populations including justified exceptions with honest statuses/counts, and never silently treats a suppressed source as unexamined or perfect code.
- **AC-15** — Rule listing, CLI help/JSON, supported manual/autonomous commands, profile/version reporting and project authority documentation consistently describe the new profile, SDK execution, Git recovery, modernizations and source suppressions; removed capabilities/states have explicit migration guidance.
- **AC-16** — Windows 11/.NET 11 focused, integration, CLI and exact newly packed installed-tool consumer evidence establishes SDK parity, effective configuration, failure/recovery, exceptions, audit, and idempotence against the final implementation; current stage receipts and criterion/evidence-case mapping are accurate and complete.
- **AC-17** — The blocking M0024 independent human/delegate review judges platform delegation and fallback necessity, unsupported-case honesty, exception scoping, Git/data safety, semantic sampling independence and criterion-specific evidence; the review is requested and recorded without fabricating approval, and the milestone-scoped `./eng/review-check.ps1 -Milestone M0024` rejects pending/incomplete review records.

## Acceptance Evidence Topology

| ID | Parent | Independently required evidence | Why distinct |
|---|---|---|---|
| EC-01a | AC-01 | Fresh profile generates syntactically valid EditorConfig boundaries/options with required root cutoff and unrelated content intact | New installation vs migration |
| EC-01b | AC-01 | Existing root EditorConfig with managed block and unrelated sections roundtrips without malformed `<!-- -->` delimiters | Legacy marker conversion |
| EC-02a | AC-02 | Nested disabling/conflicting EditorConfig or project scope is detected using effective settings | Child precedence |
| EC-02b | AC-02 | Shared file/multiple SDK projects and clean matching configuration are handled deterministically | Multi-project scope |
| EC-03a | AC-03 | v2-to-v3 migration reports formerly disabled mechanical rule and requires explicit approval | No silent opt-out erasure |
| EC-03b | AC-03 | Existing semantic disabled settings, unrelated user content and established mandatory profile infrastructure survive migration | No unintended policy loss |
| EC-04a | AC-04 | Real SDK whitespace/style execution across covered solution/projects uses approved diagnostics only and no one-process-per-rule loop | Runtime integration path |
| EC-04b | AC-04 | Project omitted by solution or generated/unsupported source is correctly accounted for | Coverage/exclusion path |
| EC-05a | AC-05 | Complete rule-by-rule disposition inventory cites observed SDK behavior/fix support and removes duplicate code | Policy parity vs guessed mapping |
| EC-05b | AC-05 | Unsupported or semantically wider SDK fixes remain skipped/review-only or narrowly specialized, with concrete negative cases | No silent policy expansion |
| EC-06a | AC-06 | Imports/aliases/global/static/conditional/trivia, modifier/braces/accessibility and representative modernizations preserve intended code and compile | Heterogeneous formatter behaviors |
| EC-06b | AC-06 | Second unchanged full run produces zero diff and unchanged policy/review suppression markers | Idempotence and steady-state behavior |
| EC-07a | AC-07 | `format --check`/`normalize --check` and explicit manual scopes report exact would-change data without writes | Check-only vs mutation |
| EC-07b | AC-07 | Normal autonomous cleanup covers unchanged eligible files while semantic gate stays changed-context-bounded | Different populations |
| EC-08a | AC-08 | Clean isolated Git baseline accepted and tool does not create commit/index/push | Positive checkpoint |
| EC-08b | AC-08 | Dirty staged/unstaged/untracked relevant files or changing `HEAD`/index prevent unsafe mutation | Negative checkpoint |
| EC-09a | AC-09 | Formatter failure/interruption after one or more file changes reports incomplete and leaves inspectable edits without claiming rollback | Honest partial-worktree recovery |
| EC-09b | AC-09 | Concurrent edit/conflicting file or failed metadata write does not clobber other work or fabricate successful state | Concurrent safety |
| EC-10a | AC-10 | Native single-diagnostic pragma pair + reason retains only its intended IDE modernization under real formatter; other fixes still run | Functional localized suppression |
| EC-10b | AC-10 | Missing reason, unmatched/overwide/wrong ID and ineffective pragma are flagged; valid whitespace cannot be disabled by arbitrary comment | Suppression negative boundaries |
| EC-11a | AC-11 | One valid `hygiene:retain` suppresses only named rule/next syntax subject and survives movement in source | Source identity and scope |
| EC-11b | AC-11 | No/boilerplate reason, unknown rule, orphan/stale comment, unrelated candidate or file-wide attempt are reported | Invalid and stale conditions |
| EC-12a | AC-12 | Desired modernization changes code, retain requires source comment, uncertain leaves work; local JSON verdict alone cannot establish acceptance | Three distinct outcomes |
| EC-12b | AC-12 | Existing local legacy decisions are not misinterpreted after migration or overwritten as fabricated source approval | Legacy state isolation |
| EC-13a | AC-13 | Nine modernization families still share a five-package maximum and six legacy batches remain independent | Sampling isolation |
| EC-13b | AC-13 | Changed context/fingerprints invalidate stale decisions, but unrelated accepted subjects stay unchanged | Context correctness |
| EC-14a | AC-14 | Authorized exhaustive audit enumerates valid retained modernization contexts and detected format drift with reconciled totals | Audit coverage |
| EC-14b | AC-14 | Unattested audit and authorized report-only run neither format nor change normal source/state | Read-only separation |
| EC-15a | AC-15 | CLI/list/help/report names and updated specifications reflect policy/SDK/suppressions and document removed state | Agent-facing consistency |
| EC-16a | AC-16 | Exact newly packed and locally installed tool exercises full autonomous + manual scope/profile migration/suppression/audit in isolated consumer | Real installed boundary |
| EC-16b | AC-16 | Current Core/CLI/validator receipts and AC/EC evidence are not inferred from an early or stale aggregate green | Completion evidence freshness |

## Validation

Use `./eng/validate.ps1 -PlanJson` to discover the current seven-stage M0020 validator, then run focused shards during development. At final candidate, execute or reuse current fingerprint-proven gates. `./eng/validate.ps1 -Verify` verifies **all** required receipts; it is not a substitute for required distinct M0024 scenarios. The stable IDs below are M0024 acceptance gates; their concrete TUnit/CLI test selectors are implementation-owned, and all must map to actual evidence.

| Gate | Tier / target | Required locus / capabilities | Required execution/check | Proves | Expected evidence |
|---|---|---|---|---|---|
| VAL-01 | Tier 1/3 — SDK formatter and EditorConfig integration | Windows 11, real .NET 11 SDK CLI, SDK-style C# projects | Real `dotnet format whitespace`/`style` with approved options in isolated fixtures; code compilation and parity classification; optional `--verify-no-changes` | AC-01, AC-02, AC-04, AC-05, AC-06, AC-07; EC-01a/b, 02a/b, 04a/b, 05a/b, 06a/b, 07a | Formatter process results, exact changed paths/diffs, option provenance and no-change rerun |
| VAL-02 | Tier 1/3 — profile v2->v3 and policy conflict fixtures | Windows 11, .NET 11, Git, isolated existing managed artifacts | Targeted Core/CLI profile migration, nested EditorConfig, disabled-rule and explicit approval scenarios | AC-01–AC-03; EC-01a/b, 02a/b, 03a/b | Before/after policy artifacts, effective settings, explicit approval-required states |
| VAL-03 | Tier 1/3 — Git checkpoint/failure and manual checks | Windows 11, Git, PowerShell, SDK format subprocess | Clean/dirty/cancellation/concurrent-change fixtures, manual `--check`, staged/unstaged inspection; `git diff --check` | AC-07–AC-09; EC-07a/b, 08a/b, 09a/b | Worktree/index/HEAD snapshots, partial-failure report and zero unintended loss |
| VAL-04 | Tier 1/3 — native & hygiene source exception semantics | Windows 11, .NET 11, real SDK format + Roslyn/source parser | Run supported pragma diagnostics and `hygiene:retain` examples, invalid/stale cases, source change replay and all nine modernization families | AC-10–AC-13; EC-10a/b, 11a/b, 12a/b, 13a/b | Source diffs, exact suppressed candidates, remaining work, unchanged six legacy semantic rules |
| VAL-05 | Tier 1/3 — exhaustive audit/CLI protocol | Windows 11, .NET 11, Git, isolated test repository | `dotnet hygiene --yolo-all` unattested/attested, audit next/record/finish, normal mode, direct CLI JSON/text/help | AC-07, AC-12–AC-15; EC-12a, 13a, 14a/b, 15a | Audit reports with reconciled counts, source/state byte equality and normal-mode bounded work |
| VAL-06 | Tier 2/4 — final current installed consumer and receipts | Windows 11 + .NET 11 + Git + PowerShell + isolated local NuGet feed/tool install | Actual `./eng/validate.ps1 -Shard ...` for required current stages, exact current package installed-consumer cases, `./eng/validate.ps1 -Verify`, focused acceptance-case matrix | AC-01–AC-16; EC-16a/b plus selected explicit earlier cases | Current package hash, installed command identity, current stage receipts and per-AC/per-EC evidence; no CI substitution |
| VAL-07 | Tier 5 — independent M0024 review | Authorized human/delegate reading current M0024 PR, ledger, validator and diff | Review `.review/REV-M0024-COMPLETION.md`, record actual decision, run `./eng/review-check.ps1 -Milestone M0024` (minimal checker is part of this milestone) | AC-17 | Actual reviewer decision and review-check result, or explicit pending/BLOCKED status |

## Human Review

Blocking, milestone-owned, independent human/delegate review. Canonical ID `REV-M0024-COMPLETION`; request/record `.review/REV-M0024-COMPLETION.md`. The implementation agent prepares a **pending** review with links to the current PR, exact source/package identity, migration/format-parity evidence, sample exception code, negative cases, and current AC/EC coverage. Reviewer judges SDK delegation, necessity of custom rewriters, approval/suppression safety, no data loss, auditing and bounded semantic independence. Acceptable decision: actual `approved`, `changes-requested`, or explicit pending/blocked, according to the repository's documented review process. No self-approval or invented waiver. Run the repository's actual documented milestone-scoped `review-check` command after a genuine decision; if unavailable, report the missing command/capability rather than invent it.

## Documentation Policy

Directly update operative `docs/specs/PROFILE.md`, `RULE-CAPABILITIES.md`, `REWRITES.md`, `AUTONOMOUS-WORKFLOW.md`, `SAMPLING.md`, `SEMANTIC-REVIEWS.md`, `AGENT-HELP.md`, `HYGIENE.md`, `docs/ARCHITECTURE.md`, `docs/ENGINEERING.md`, `docs/SPECS.md`, README and applicable CLI help/tests **only where this implementation changes their truth**. Retain historical M0023 documents unchanged as historical records. Public documentation must not describe now-removed durable modernization decision semantics as active. No broad user-documentation synchronization beyond directly impacted authority in this milestone. No research appendix is required: material SDK interface facts were resolved as operating constraints; actual SDK behavior is proven under VAL-01/04 and reported as product evidence.

## Completion Evidence

Execution ledger: `.execution/M0024-sdk-format-profile-and-source-exceptions.md`. PR description references exact source SHA, stage/receipt identity, installed package hash, AC/EC evidence, supported/unsupported formatter disposition table, baseline migration outcomes, suppressions, failure behavior, and independent review. Required human review packet: `.review/REV-M0024-COMPLETION.md`. A green test summary cannot replace individual cases. Completion occurs only after all required AC/EC/VAL evidence is current and the blocking human review actually approves. If a required SDK/Windows/installed-consumer capability is missing, finish as `BLOCKED` with concrete missing evidence rather than claiming acceptance.

## Escalation Boundary

Implementation may choose code structure and tests, but it must not decide to silently weaken the fixed mechanical profile, add broad automatic modernizations, drop unsupported cleanup without reporting, ignore native suppression behavior, convert preexisting local judgments into justifications, enable destructive Git repair, accept an alternate validation locus, or change user-facing semantic-review population/budget policy. Any newly discovered issue that requires such a **material** change returns to planning. Supported diagnostic-specific fallback to review-only is allowed only as explicitly specified above and with an accurate reported capability disposition; it must not silently satisfy a mandatory rule by doing nothing.
