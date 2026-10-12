# Verifiable Review Work — Tool / Agent Contract

## Status

Planning context. Not executable product authority. M0017's milestone and operative specifications govern product behavior.

## Core decision

The hygiene tool builds **verifiable review opportunities**. External implementer, frontier or planner agents decide whether semantic problems actually exist and how to correct them. The tool does not possess a privileged reference judgment.

These are independent claims:

1. **Trigger fidelity:** a deterministic indicator matches its documented mechanical predicate.
2. **Review-work fidelity:** the right eligible subject, identity, relevant context, gate allocation, questions, reviewer class, stale protection and acceptance mechanics are delivered.
3. **Review outcome:** an external agent makes a domain/context-sensitive judgment and may propose a correction.

The first two are software responsibilities and can be verified. The third is not a property to certify by testing this tool against one current model.

## Visual-block signal as the counterexample

The existing `readability.control-flow.visual-block` v1 finds a missing blank line after a preceding ordinary statement before selected control-flow statements, excluding certain sibling patterns. It can prove only that the syntax/trivia pattern occurs. It cannot prove:

- a blank line improves the readability of that particular conceptual unit;
- a mechanical insertion is always appropriate;
- all visually confusing code is found;
- the invoking agent cannot produce a better restructuring.

A caller should inspect the candidate and choose among retaining existing layout, adding separation, restructuring behavior, or resolving it another way. Versioned candidate classification and truthful explanation are appropriate; an automatic formatter or unconditional remediation is not.

## Semantic-review rule boundary

A new rule owns:

- an intelligible subject population and source-backed identity;
- a fixed small question set and escalation class;
- enough truthful current source context for a reviewer, without pretending to know unstated behavior;
- normal candidate priority under M0016's existing gate-local selector.

The shared host owns bounded allocation (five per rule), versioned tickets, accepted-only observations, full-population expand/handoff, state atomicity and stale revalidation. It does not determine whether test quality, type responsibility or comment purpose is good or bad, and does not invoke a model/provider.

An acceptance records a caller's explicit assertion under the rule contract, **not** tool-generated proof of code quality. Uncertain or negative cases are not silently accepted.

## Validation policy

For a new semantic rule, verify eligibility/exclusion and edge cases; identities/fingerprints; source context; ordinary/explicit/zero-work gates; bounded ranking and repeated-gate no-refill; independent batches and questions; stale/tampered tickets; accepted-only lifecycle; source-project and isolated installed-tool consumer use.

Do not require model runs, curated good/bad semantic labels, false-positive or precision scores, reviewer consensus, historical replay, sampling calibration, or evidence of economic return to complete the software milestone.

A later team may examine actual agent outcomes for prioritization and policy revision. Those observations are optional product learning and cannot be interpreted as permanent limits on a changing model/agent ecosystem.

## Consequences for M0017

- One milestone for visual-block candidate semantics plus three new semantic-review rules.
- No automatic visual-block formatter.
- No new sampling engine, universal rule pipeline or model integration.
- Fixed rule identities/questions and independent toggles, not a settings DSL.
- Contract tests and real CLI/installed-tool verification, not subjective semantic-review benchmarks.
- Historical hazard/cohort research remains preserved but is not revived as active policy.
