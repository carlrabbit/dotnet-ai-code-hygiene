# ADR-0021 — Milestone Readiness Targets the Baseline Implementation Model

## Status

Accepted in guide system v0.7.2.

## Context

A two-phase planning and implementation model is most useful when planning absorbs the difficult project-level reasoning and the ready milestone becomes a reliable execution contract.

Without an explicit capability boundary, teams may compensate for incomplete planning by selecting a stronger implementation model whenever execution encounters architectural, semantic, compatibility, scope, acceptance, or validation uncertainty.

That weakens the milestone as the durable handoff between planning and implementation and makes execution cost and reliability depend on model escalation.

The default ChatGPT-focused workflow is intended to use GPT-5.6 Luna as the baseline implementation model for ordinary ready milestones.

## Decision

A coding milestone may enter `ready` only when the project's baseline implementation model can execute it without making a new material project-level decision.

For the default ChatGPT-focused guide profile, the baseline implementation model is GPT-5.6 Luna.

Projects may override the baseline implementation model in planning metadata. The readiness rule targets the configured baseline rather than introducing capability tiers for individual milestones.

Planning owns the reasoning required to make the milestone baseline-executable, including:

- architecture and subsystem boundaries;
- semantics and behavior;
- public or persisted compatibility;
- scope and non-goals;
- acceptance criteria;
- validation policy;
- human-review policy;
- project-level constraints and invariants.

Implementation continues to own local mechanics that do not alter the ready contract.

Do not introduce `strong`, `frontier`, or similar execution-capability profiles as a substitute for planning completeness.

If material evidence must be discovered before the final implementation contract can be decided, plan a diagnostic milestone whose goal is to gather that evidence. Use the result to return to planning and produce the final ready implementation milestone.

Large execution volume does not by itself violate baseline executability. Large coherent migrations or transformations may remain baseline-executable when decisions are settled and execution and validation can be bounded, sharded, or resumed safely.

## Consequences

- the milestone becomes the primary steering mechanism for the baseline implementation model;
- difficult project reasoning moves toward planning rather than being deferred to implementation;
- implementation model upgrades are not the normal remedy for an under-specified milestone;
- planning may need deeper repository inspection or an explicit diagnostic phase before declaring `ready`;
- milestone size is governed by coherence and execution constraints rather than model-tier labels;
- execution profiles continue to describe autonomy and review expectations, not model capability;
- the v0.7.1 completion audit and terminal outcomes remain unchanged.

## Rejected alternatives

### Per-milestone strong/frontier execution tiers

Rejected because they make executor capability an escape hatch for incomplete planning and weaken the `ready` boundary.

### Line-by-line implementation prescriptions for the baseline model

Rejected because readiness requires decision completeness, not pre-implementation. Local mechanics should still be derived from the live repository.

### Always use the strongest available implementation model

Rejected because the guide intentionally uses planning and milestone contracts to make implementation predictable, inexpensive, and portable across execution surfaces.
