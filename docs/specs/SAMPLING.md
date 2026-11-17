# Gate-local semantic review selection

## Production model

The 17 general semantic-review rules share one deterministic gate budget. The nine modernization rules retain a separate shared package budget. A gate is one explicit `dotnet hygiene check` opportunity identified by repository, mode, resolved Git base/head/worktree, reporting paths, rule versions, and eligible populations. Ranking uses no wall-clock or process-random input.

Each distinct `(rule ID, project-qualified subject identity)` is one independently judged obligation. Several obligations that share a file, relationship, or presentation group each spend a slot. General selection is capped at ten obligations total. Modernization selection is separately capped at five packages total.

## Admission, ranking, and budgets

- A bare full check, source-clean gate, or gate with no eligible candidates selects zero general semantic work.
- For `--changed` and `--base`, obligations with a changed primary anchor rank first. Unchanged candidates are eligible only when a concrete related source anchor changed; at most two context-affected obligations may be selected. Unrelated background is never admitted.
- Within direct and context strata, a deterministic rotating round-robin across nonempty enabled rule IDs prevents permanent first-rule priority; subjects within a rule have stable deterministic ranking.
- Explicit paths and `--review-all` intentionally broaden candidate eligibility but still share the ten-obligation ceiling. They do not reserve context slots.
- All enabled rules expose their eligible population counts, selected obligations, and unreviewed remainder. Grouping cannot hide separate judgments.

Selection may be biased toward changed source and does not estimate defect probability, population quality, or defect-free status. Signals identify review opportunities, not proven defects.

The nine modernization families retain five packages total per ordinary gate. Changed packages rank first with deterministic cross-family rotation. A package exposes a bounded list of individually identified occurrences plus eligible total and remainder; those occurrences do not multiply the five-package budget. Explicit-path and `--review-all` runs keep the same five-package ceiling. Exhaustive audit bypasses both budgets only after separate explicit authorization.

## Acceptance, tickets, and legacy state

Only explicit caller acceptance writes accepted observations. Tickets bind the gate, algorithm/rule version, rule-subject identity, and material evidence fingerprint. Related source edits, rule-question/version changes, stale gates, malformed tickets, or tampering invalidate the decision. Accepted observations suppress only unchanged work for the same rule and subject. Partial acceptance never backfills the selected identities within the same gate. Stale accepted history is retained; acceptance of a changed fingerprint does not silently delete it.

The obsolete `architecture.boringness.review`, `readability.type-responsibility.review`, and `tests.quality.review` identities are not migrated to the new portfolio. If old records occur in `.hygiene/.state/gate-reviews.json`, the tool refuses to treat them as approvals and reports an archival/removal step. Preserve a copy before editing that file. Old rule IDs in `.hygiene/config.json` likewise require preserving the file and removing retired toggles explicitly; no source or state is silently destroyed.

The normal accepted-observation store contains hashes and metadata, not raw source excerpts. Writes are atomic and reject concurrent state changes. Exhaustive audit reads all enabled semantic obligations in report-local work orders without mutating gate review state.

## Gate surfaces

```text
dotnet hygiene check [paths...] [--changed | --base <git-ref> | --review-all]
```

Bare `check` runs deterministic rules over the full default source scope and selects zero general semantic work. `--changed` admits current eligible changed C# source. `--base` includes committed feature changes plus current staged, unstaged, and untracked changes while unchanged project context remains readable. Explicit paths are bounded manual inspection independent of Git diff. `--review-all` explicitly requests bounded full-repository semantic inspection. Invalid refs and incompatible target options fail as errors rather than widening scope silently. Deleted targets are not current candidates; rename destinations are deduplicated.

## Expansion, handoff, and audit

`dotnet hygiene review expand <batch-handle>` and `review handoff` revalidate the stored gate and expose the corresponding complete eligible population. Source excerpts are assembled for selected work and explicit expansion/handoff only. Context limits and unavailable evidence are explicit.

`dotnet hygiene --yolo-all` is a separate read-only exhaustive audit. It requires the affirmative task attestation described in `AUTONOMOUS-WORKFLOW.md`; it enumerates every enabled rule-subject/occurrence pair, including accepted or ordinarily suppressed observations. It changes neither source/Git state nor normal acceptance state.
