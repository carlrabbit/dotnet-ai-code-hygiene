# Milestone — M0003 Semantic Review Sampling & Escalation

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | awaiting-human-review |
| Mode | ai-executed-human-reviewed |
| Baseline implementation model | GPT-5.6 Luna |
| Baseline executor readiness | confirmed |
| Decision preservation | confirmed |
| Execution tractability | confirmed |
| Execution ledger | `.execution/M0003-semantic-review-sampling.md` planning-seeded/amended |
| Scope size | medium-large coherent feature |
| Validation locus/platform | local Windows 11 + .NET 11 SDK + Git + PowerShell |
| Human review | required after amendment implementation |

## Amendment Context

The initial M0003 implementation reached human review in PR #3. Human review identified a missing operational contract: an expanded frontier/planner review must be transferable durably in both supported orchestration topologies:

```text
1. frontier/planner is colocated with the implementer/repository;
2. frontier/planner is completely decoupled and requires a file handoff.
```

The project also requires tool-owned folders to remain namespaced and allows review evidence to be intentionally included in a PR.

This amendment is part of M0003 and must be implemented before `REV-M0003-COMPLETION`.

## Goal

Introduce reusable deterministic semantic sampling and a complete escalation preparation path:

```text
bounded sample
-> implementer judgment
-> deterministic expanded frontier population
-> transient stdout OR durable transport-neutral handoff file
```

Use XML summary quality as the first semantic review rule.

## Target State

Every enabled semantic review rule evaluates on every `hygiene check`.

`docs.summary.quality.review` selects up to five eligible XML summaries in target scope and emits one run-scoped review batch with a fixed four-question rubric.

If any sampled answer is materially negative or uncertain, the caller escalates.

Transient/local direct escalation:

```text
hygiene review expand <batch-handle>
```

Durable escalation:

```text
hygiene review handoff <batch-handle> [--file <path>]
```

Without `--file`, the handoff is written to `.hygiene/reviews/<handoff-id>/request.json` and may intentionally be committed into the same branch/PR.

With `--file`, the same transport-neutral request can be written outside the repository for a completely decoupled planner/frontier reviewer.

The CLI performs no model call and maintains no engine-managed semantic-review history.

## Scope

- first-class `ReviewBatch`/review-item concepts;
- deterministic bounded sampler;
- run-local `B-*` identities;
- latest-run batch state;
- `reviewBatches` check output;
- `hygiene review expand`;
- `docs.summary.quality.review` fixed rubric;
- durable `hygiene review handoff`;
- product-namespaced `.hygiene/reviews/` repository artifact area;
- explicit external handoff destination;
- transport-neutral semantic-review request JSON;
- embedded source context for decoupled reviewers;
- M0002 regression compatibility;
- public documentation.

## Non-goals

- no model API/provider integration or automatic escalation;
- no answer collection/import;
- no engine-managed semantic review history, TTL, scheduler, or audit cadence;
- no generic trigger engine beyond always evaluating enabled rules;
- no random sampling;
- no configurable sample size/rule parameters/severity/order;
- no automatic comment rewrite engine;
- no transport-provider integration (Google Drive, GitHub, email, etc.);
- no automatic `git add`, commit, push, PR update, or artifact deletion;
- no result-file interpretation/acceptance state;
- no formatter/normalizer or installed-tool validation;
- no M0004 packaging/release work;
- no cross-platform expansion.

## Decisions and Constraints

- `ReviewBatch` is not a finding subtype.
- One enabled semantic review rule produces exactly one batch per successful run, even `0/0`.
- Summary-quality normal sample maximum is exactly five.
- Sampling is deterministic and SHA-256 based per `docs/specs/SEMANTIC-REVIEWS.md`.
- Normal reviewer class `implementer`; expanded/handoff reviewer class `frontier`.
- CLI never decides sample pass/fail.
- Expansion/handoff support bare/latest and qualified latest-run batch handles.
- Expansion/handoff revalidate population fingerprint; stale state => exit 3 + rerun guidance.
- Expanded/handoff review contains complete population including sampled items.
- Expansion/handoff creates no new run and does not mutate latest-run state.
- Handoff semantics must reuse the same expansion/revalidation logic.
- Default durable destination is `.hygiene/reviews/<handoff-id>/request.json`.
- `.hygiene/reviews/` is intentionally not product-Git-ignored.
- Explicit `--file` may point outside repository.
- Handoff refuses silent overwrite of an existing request.
- Handoff writes atomically.
- Handoff embeds full current source text only for files containing review items.
- Handoff item IDs use `RI-*`, not `I-*`, to avoid collision/confusion with ignore-decision IDs.
- Durable handoff files are explicit work products, not automatic semantic-review history.
- Check JSON remains schemaVersion 1 with additive `reviewBatches`.
- Existing M0002 finding/ignore/target/exit behavior remains compatible.
- M0004 owns formatter/normalizer/installed-tool work.
- No GitHub Actions/workflows.

## Required Authority

- `docs/SPECS.md`
- `docs/specs/HYGIENE.md`
- `docs/specs/SEMANTIC-REVIEWS.md`
- `docs/specs/REVIEW-HANDOFFS.md`
- `docs/ARCHITECTURE.md`
- `docs/ENGINEERING.md`
- `docs/TERMINOLOGY.md`
- `docs/PUBLIC-DOCS.md`

## Acceptance Criteria

- **AC-01** — Core models rules with explicit output kind and first-class run-scoped review batches/items, not pseudo-findings or ignore decisions.
- **AC-02** — Canonical rule set/order includes `docs.summary.quality.review` v1 as `review-batch`, participating in existing enable/disable configuration.
- **AC-03** — Generic sampling deterministically derives population fingerprint/ranking from rule/version plus stable subject/content fingerprints; identical state yields identical sample and review-relevant changes recompute ranking.
- **AC-04** — Every successful enabled run emits exactly one batch for the semantic rule, including explicit empty `0/0` batch.
- **AC-05** — Normal sample never exceeds five and includes entire population when population size is five or less.
- **AC-06** — Summary-quality population contains exactly valid non-empty XML summaries on the same public/internal symbol categories as `docs.summary.required`; missing/empty/invalid summaries remain Rule A concerns and are excluded.
- **AC-07** — Every review item exposes item ID, repository-relative path, 1-based location, symbol, summary text, and declaration display/signature without internal ranking/fingerprint metadata.
- **AC-08** — Batch exposes exactly Q1 natural German, Q2 technical correctness, Q3 information value, Q4 clarity/scope plus material-failure/uncertainty escalation guidance.
- **AC-09** — Normal quality batch uses `reviewerClass=implementer`; no deterministic language heuristic is treated as sufficient quality judgment.
- **AC-10** — `hygiene check --output json` remains schemaVersion 1, preserves M0002 fields, always adds documented `reviewBatches`, and excludes internal state/hashes.
- **AC-11** — `hygiene check --output text` renders compact batch/sample/rubric/escalation info and explicit empty samples.
- **AC-12** — Checks with review batches return exit 0; batches do not alter finding count, ignored count, finding IDs, or ignore matching.
- **AC-13** — Latest-run state atomically persists enough internal batch data for expansion/handoff while remaining Git-ignored/engine-owned.
- **AC-14** — `hygiene review expand` supports bare latest and fully-qualified latest-run batch handles, rejects unavailable old-run batches with exit 3, and supports text/JSON.
- **AC-15** — Expansion/handoff recompute population and reject review-relevant changes with exit 3 plus rerun guidance.
- **AC-16** — Successful expansion emits complete eligible population including sampled items, keeps same rubric, sets `mode=expanded` and `reviewerClass=frontier`, and neither creates a new run nor mutates latest-run state.
- **AC-17** — Batch handles are rejected by finding ignore semantics; M0003 creates no automatic/engine-managed semantic-review history or answer state. Explicit handoff artifacts are caller-requested review work products and do not alter rule/acceptance state.
- **AC-18** — No model/provider SDK, network model call, automatic model selection/escalation, answer collection/import, semantic-review history engine, scheduler, or TTL is introduced.
- **AC-19** — Existing M0002 target/finding/text-JSON/explain/ignore/unignore/stale/config/exit behavior remains regression-covered and compatible.
- **AC-20** — `format`/`normalize` remain non-functional and the packaged developer-tool/formatter milestone is deferred to M0004.
- **AC-21** — `./eng/validate.ps1` remains complete thin Windows-local restore/build/test/pack validation and passes.
- **AC-22** — `hygiene review handoff <batch-handle> [--file <path>]` is a functional CLI surface that uses the same latest-run resolution, full-population expansion, and stale-population revalidation contract as `review expand`.
- **AC-23** — Without `--file`, handoff creates exactly one request at `.hygiene/reviews/<filesystem-safe-handoff-id>/request.json`; the handoff ID is deterministic for the source run/batch and `.hygiene/reviews/` is not product-Git-ignored.
- **AC-24** — With `--file`, handoff can write to an explicit destination inside or outside the repository, creates missing parent directories, refuses silent overwrite of an existing destination, and uses atomic file creation semantics.
- **AC-25** — Handoff JSON schema version 1 exposes `kind=semantic-review-request`, handoff identity/timestamp, source run/batch, rule ID/version, `mode=expanded`, `reviewerClass=frontier`, population count, fixed questions, and the complete revalidated item population without engine-internal ranking/persistence data.
- **AC-26** — Handoff items use `RI-*` identities and the request embeds a deduplicated `sources` array containing full current text for every source file containing a handoff item and no unrelated repository source files.
- **AC-27** — Handoff creation does not mutate source/latest-run state, does not invoke Git/model/network transport, and does not interpret/create semantic review results/history beyond the explicit request artifact.
- **DOC-01** — README/public docs explain batches, empty samples, explicit expansion, implementer/frontier classes, no model/history engine, and exact summary-quality escalation workflow.
- **DOC-02** — Project authority consistently refers to formatting/normalization/installed-tool validation as M0004 work; no stale M0003 formatter/package claim remains.
- **DOC-03** — Public/project docs explain both colocated and fully decoupled review topologies, namespaced `.hygiene/reviews/` PR-friendly storage, external `--file` transport, embedded-source implications, and that the CLI neither commits nor transmits the artifact.
- **REV-01** — Human review confirms sample usefulness, rubric quality, finding/batch separation, durable handoff usability for both reviewer topologies, PR inclusion behavior, absence of hidden model/history behavior, and preservation of M0004 scope.

## Acceptance Evidence Topology

| ID | Parent | Required evidence case | Why separate |
|---|---|---|---|
| EC-03a | AC-03 | identical population/content yields identical sample | determinism |
| EC-03b | AC-03 | review-relevant population/content change recomputes fingerprint/ranking | invalidation/rotation |
| EC-04a | AC-04 | eligible population exists | normal batch |
| EC-04b | AC-04 | zero eligible population | empty batch |
| EC-05a | AC-05 | population <=5 | complete small population |
| EC-05b | AC-05 | population >5 | bounded sample |
| EC-06a | AC-06 | valid summary subjects included | eligibility |
| EC-06b | AC-06 | missing/empty/non-doc summaries excluded | exclusion |
| EC-10a | AC-10 | populated batch JSON | populated schema |
| EC-10b | AC-10 | empty batch JSON | empty schema |
| EC-14a | AC-14 | bare latest-run batch handle | shorthand |
| EC-14b | AC-14 | fully-qualified latest-run batch handle | qualified |
| EC-14c | AC-14 | old/unavailable run handle | failure path |
| EC-15a | AC-15 | unchanged population expands/handoffs | success |
| EC-15b | AC-15 | population changed after check | stale rejection |
| EC-16a | AC-16 | expanded output contains entire population including sample | full-population |
| EC-16b | AC-16 | expansion leaves latest-run state byte-equivalent | non-mutating |
| EC-17a | AC-17 | batch handle rejected by `ignore` | finding/batch separation |
| EC-17b | AC-17 | normal check/expand creates no automatic review-history artifact | persistence separation |
| EC-19a | AC-19 | deterministic finding/ignore lifecycle works with review rule enabled | mixed-output regression |
| EC-19b | AC-19 | disabling review rule removes batch without altering deterministic findings | config regression |
| EC-22a | AC-22 | bare batch handoff succeeds | handoff shorthand |
| EC-22b | AC-22 | qualified latest-run batch handoff succeeds | handoff qualified |
| EC-22c | AC-22 | old/stale batch handoff fails like expansion | shared revalidation |
| EC-23a | AC-23 | default namespaced request path and deterministic safe handoff ID | repository artifact path |
| EC-23b | AC-23 | `.hygiene/reviews/` is not ignored by repository Git rules | PR-visible path |
| EC-24a | AC-24 | explicit destination outside repository succeeds | decoupled transport |
| EC-24b | AC-24 | missing parent directories created | filesystem path |
| EC-24c | AC-24 | existing destination rejected and bytes preserved | overwrite safety |
| EC-24d | AC-24 | interrupted/failed write leaves no partial destination | atomicity |
| EC-25a | AC-25 | request schema fields and frontier/expanded/full-population semantics | artifact contract |
| EC-25b | AC-25 | internal population/ranking/persistence metadata absent | public artifact boundary |
| EC-26a | AC-26 | handoff item IDs use `RI-*` | namespace separation |
| EC-26b | AC-26 | multiple items in same file produce one embedded source entry | source de-duplication |
| EC-26c | AC-26 | multiple item files all embedded and unrelated file excluded | bounded source scope |
| EC-27a | AC-27 | handoff leaves latest-run bytes and source bytes unchanged | non-mutating |
| EC-27b | AC-27 | handoff uses no Git/model/network side effect | boundary |

## Validation

| ID | Depth | Target/locus | Proves |
|---|---|---|---|
| VAL-01 | Tier 1 | Core semantic-review/sampler/handoff construction / Windows 11 + .NET 11 | AC-01..AC-09, AC-13, AC-15..AC-18, AC-22..AC-27 and mapped ECs |
| VAL-02 | Tier 1 | built CLI process / Windows 11 + .NET 11 | AC-10..AC-12, AC-14, AC-17, AC-20, AC-22..AC-27 and mapped ECs |
| VAL-03 | Tier 3 | isolated SDK-style fixture repos + external temp path / Windows 11 + .NET 11 + Git | AC-06, AC-15, AC-16, AC-19, AC-22..AC-27 and mapped ECs |
| VAL-04 | Tier 2 | complete repo / `./eng/validate.ps1` | AC-18, AC-19, AC-21, AC-23, AC-27, DOC-02 |
| VAL-05 | Tier 2 | README/public docs vs live behavior | DOC-01, DOC-03 |
| VAL-06 | Human | semantic workflow + handoff usability | REV-01 |

## Human Review

Review ID: `REV-M0003-COMPLETION`.

The previous human-review request is superseded by this amendment. Complete human review only after amendment implementation/evidence.

Confirm sample/rubric usefulness, default `.hygiene/reviews/` PR behavior, explicit external file for decoupled review, embedded source context, absence of Git/network/model side effects, distinction between explicit review artifacts and engine-managed history, and preservation of M0004 scope.

## Completion Evidence

Implementation evidence refreshed for the durable handoff amendment on 2026-10-04 (Windows 11, .NET SDK 11.0.100-rc.1.26425.128). Existing PR #3 semantic sampling/expansion implementation and lifecycle evidence were retained and reconciled; amended behavior is covered by `LifecycleTests.DurableReviewHandoffEmbedsOnlyCurrentRelevantSourcesAndIsAtomic` and `CliProcessTests.HandoffCommandWritesDefaultAndExternalRequestsWithoutGit`.

`./eng/validate.ps1` completed successfully: Release build passed; Core TUnit 19/19 passed; CLI TUnit 19/19 passed; publish and package succeeded. The focused fixture exercises default and external destinations, stale/old handles, source context boundaries, schema/RI-* identities, atomic no-overwrite behavior, and source/latest-run immutability. CLI fixture succeeds with Git hidden. Repository inspection confirmed `.hygiene/.state/` is ignored while `.hygiene/reviews/` is not, no provider/network/transport dependency or Git mutation implementation was added, no GitHub workflow exists, and format/normalize remain M0004 scaffolding.

README and public documentation describe both handoff topologies and source-content implications. `REV-M0003-COMPLETION` is newly requested and remains pending human review; this milestone is not self-approved.
