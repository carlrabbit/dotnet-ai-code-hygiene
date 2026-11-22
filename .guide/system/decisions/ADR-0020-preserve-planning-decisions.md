# ADR-0020: Preserve resolved planning decisions across handoff

## Status

Accepted

## Context

Planning and implementation are intentionally disconnected. Planning may use a stronger reasoning model and a broader investigative context than the baseline implementation agent.

A milestone can appear decision-complete while still losing information during package construction. For example, planning may establish an ownership boundary, required public API, forbidden compatibility layer, or negative architectural requirement and then compress that result into a vague statement such as "adapt the component to the new architecture." The implementation agent is then forced to rediscover or reinterpret a decision that planning had already made.

This is different from execution-state loss during a long implementation run. Persistent execution ledgers address implementation progress. The planning boundary also needs an explicit information-preservation invariant.

## Decision

Planning may compress analysis, but it must not semantically weaken resolved material decisions.

Every resolved decision that materially constrains architecture, semantics, behavior, compatibility, scope, acceptance, validation, human-review policy, ownership, integration boundaries, or project-level invariants must survive in the durable implementation input with enough precision for the baseline executor to apply it without repeating project-level reasoning.

The durable implementation input is:

```text
ready milestone
+ referenced project authority
+ live repository facts
```

A decision does not need to be copied verbatim into the milestone when it already belongs in a referenced specification, ADR, architecture document, terminology document, or other project authority. Milestone-specific decisions belong in the milestone. Durable project-wide decisions belong in the appropriate project authority and are referenced by the milestone.

Planning should preserve:

- resolved business, semantic, architectural, contract, compatibility, ownership, and integration decisions;
- negative requirements and explicitly forbidden alternatives when violating them is a plausible implementation path;
- decision-relevant repository facts when the executor cannot reliably recover them from the live repository;
- rationale or invariants when they are necessary to interpret, generalize, or correctly apply the decision to newly discovered implementation surfaces.

Planning normally discards or compresses:

- exploratory reasoning paths;
- rejected alternatives whose rejection is not needed to prevent likely incorrect implementation;
- planning scratch work;
- speculative implementation ideas;
- predicted file/class/method edits that do not constrain the project contract.

An implementation suggestion does not become authority merely because the planning model mentioned it. Preserve it as a decision only when changing it would alter the resolved project contract.

## Decision-preservation audit

Before `ready`, planning applies this counterfactual test:

> Assume the planning conversation and planning model cease to exist now. Can the configured baseline executor recover every material decision already made during planning from the ready milestone and referenced project authority, and implement the decided target state without repeating project-level reasoning?

If not, the milestone is not ready.

This audit is separate from decision completeness:

- decision completeness asks whether all material decisions have been made;
- decision preservation asks whether the decisions already made survived into durable implementation authority.

Both must hold before `ready`.

## Consequences

- planning handoffs remain compact without discarding resolved semantics;
- the planning transcript is never hidden implementation authority;
- project-wide decisions are normalized into project authority instead of duplicated into every milestone;
- milestone-specific decisions remain explicit enough for disconnected execution;
- the baseline executor retains implementation freedom inside the solution space established by planning;
- planning packages do not become exhaustive implementation scripts or reasoning dumps.
