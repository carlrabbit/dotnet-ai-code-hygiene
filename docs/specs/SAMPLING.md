# Gate-local semantic review selection

## Production model

The six established semantic-review rules keep their finite, gate-local, risk-stratified selection. The nine M0023 modernization rules use a separate shared gate-local package budget. A gate is one explicit `dotnet hygiene check` opportunity over a repository, mode, resolved comparison base when present, current `HEAD` and worktree, reporting paths, rule version, and the rule's complete eligible population. Selection is deterministic for those inputs and uses no wall-clock or process-random input.

The selector ranks without replacement using SHA-256-derived stable keys over the gate, rule and algorithm versions, project-qualified subject identity, and full subject content fingerprint. Repository enumeration order does not affect ranking. A rule's selected items are already ranked; the host does not rank them a second time.

## Gate admission and budgets

`dotnet hygiene check` always evaluates deterministic rules over its normal target scope and emits one batch for every enabled semantic-review rule. The autonomous entrypoint sends only selected items in its consolidated work order. Bare full-repository checks schedule no semantic review. `--changed` authorizes review only for current eligible C# paths changed relative to `HEAD`; `--base <ref>` resolves the merge base and includes committed and worktree changes; explicit paths request a bounded manual inspection of those targets. Documentation-only changes, clean gates, and gates with no rule-eligible candidates select zero items. `--review-all` explicitly authorizes a bounded full-repository inspection.

Every rule has a fixed ceiling of five subjects. In an ordinary source-change gate, direct changed subjects take priority. If a context-affected unchanged subject exists in a touched source document, at most one slot is reserved for it and up to four slots go to direct subjects; remaining slots may be used for additional direct subjects. Without a context candidate, up to five direct subjects may be selected. Unrelated background subjects never fill an ordinary gate. Explicit paths and `--review-all` can select up to five without that reserve. A ceiling is not a quota: sparse and zero-work gates remain sparse or empty.

The nine modernization rules share one ceiling of five **packages total per ordinary gate**, across all nine rule IDs. Changed packages are preferred, and a deterministic round-robin across eligible rule populations prevents one family from permanently taking the budget. Broad explicit-path and `--review-all` inspections remain capped at five packages total. A package carries a bounded list of individually addressable occurrences plus the eligible occurrence total and unreviewed remainder; those per-occurrence details do not multiply the package budget. Exhaustive read-only audit bypasses both modernization bounds and inventories every eligible occurrence. The existing six rules retain their independent per-rule ceilings and do not spend the modernization budget.

This is selective inspection for engineering attention. Selection can be biased by the fixed priority policy and makes no calibrated defect-likelihood, defect-rate, population-coverage, or defect-free acceptance claim. Context ranking is exploratory and does not estimate population quality.

## Accepted observations and tickets

`dotnet hygiene review accept <batch> <item>...` or `--all` records accepted observations for the six established semantic-review rules. M0023 modernization packages do not support package-level acceptance: `dotnet hygiene review decide <batch> <occurrence> --verdict desirable|retain|uncertain` records one explicit occurrence judgment after revalidating its current fingerprint. A judgment is not a source rewrite. The CLI does not invoke a reviewer or infer a judgment.

Private latest-run tickets bind the current gate, rule and algorithm versions, complete eligible-population fingerprint, selected identity, and content fingerprint. Expand, handoff, and accept re-evaluate the stored scope and reject stale bases, populations, versions, tickets, or already accepted items. A repeated gate keeps the same selected identities; accepting part of its batch does not fill newly opened slots. An accepted subject with unchanged content is suppressed. A changed fingerprint can be selected again when a later gate makes it relevant.

Accepted observations are stored as versioned JSON in `.hygiene/.state/gate-reviews.json`. State records contain rule/version, subject identity, full content fingerprint, gate identity, and explicit acceptance time; source text is never stored. The state is bounded to 50,000 current subject observations and errors at capacity instead of silently pruning evidence. Missing state starts empty. Invalid or unsupported state fails without replacing it. Writes are atomic and reject concurrent state changes. Gate tickets remain private to latest-run state and never appear in check, expand, handoff, or acceptance JSON.

## Rule ownership and compatibility

Each rule owns its eligibility, questions, escalation class and condition. The shared mechanism owns only gate context, stable ranking, a five-item ceiling, private tickets, and accepted-observation persistence. Batches B-1 through B-3 remain unchanged; M0017 appends `readability.type-responsibility.review`/B-4, `tests.quality.review`/B-5, and `comments.intent.review`/B-6, each at v1. Mechanical rules remain exhaustive.


The CLI performs no model/provider invocation. Expansion and handoff still revalidate and expose the complete eligible population for their stored reporting scope; a zero normal batch does not erase the population used for escalation.
