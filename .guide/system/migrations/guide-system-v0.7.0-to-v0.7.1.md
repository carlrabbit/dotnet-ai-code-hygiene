# Guide System Migration — v0.7.0 to v0.7.1

## Purpose

Correct milestone execution so the implementation agent owns milestone closure rather than stopping after implementation or successful validation.

## Applies to

Repositories using milestone-driven implementation, especially disconnected or AI implementation agents.

Repositories that do not rely on the canonical execution prompt or equivalent agent instructions may treat this migration as a no-op.

## Core correction

The execution objective is now:

```text
implement -> validate -> completion audit -> continue or terminate
```

Passing tests, successful compilation, or completion of listed focus areas is evidence of progress. It is not by itself proof that the milestone is complete.

The executor must continue resolving every unsatisfied milestone obligation that can be completed in the current execution context without changing the ready milestone contract.

## Required execution semantics

Update local execution instructions, copied execution prompts, or agent guidance derived from v0.7.0 so that they require a mandatory completion audit before termination.

The audit must check all applicable milestone obligations, including:

- goal;
- target state;
- acceptance criteria;
- required validation and evidence;
- required artifacts or generated outputs;
- direct documentation obligations;
- migrations, cleanup, or compatibility work;
- human-review gates;
- constraints and invariants;
- supporting work discovered during implementation that is necessary for completion.

If an unsatisfied item is agent-resolvable, execution continues.

## Scope clarification

Replace overly restrictive interpretations of "implement only the milestone scope" with this rule:

```text
Do not introduce unrelated product scope.
Perform all supporting work required to satisfy the milestone contract.
```

Focus areas, workstreams, and similar decompositions are execution guidance rather than edit allowlists unless the milestone explicitly makes them contractual.

The executor may inspect additional repository-local material when needed to implement or prove a milestone requirement. It must still avoid unrelated exploration and must not require the planning transcript or external guide repository.

## Success semantics

Keep these states distinct:

- implementation success: the intended implementation exists;
- validation success: required automated checks pass;
- milestone completion: every milestone obligation and completion gate is satisfied.

Only milestone completion permits the milestone to become `done`.

## Terminal execution outcomes

An implementation run must terminate only as:

### `COMPLETE`

Every applicable milestone obligation is satisfied and no blocking review or external dependency remains.

### `AWAITING HUMAN REVIEW`

All agent-resolvable work is complete and a required human review decision is the only remaining gate.

### `BLOCKED`

Completion requires unavailable external capability or a material planning decision the executor is not authorized to make.

Do not classify ordinary implementation work, failing tests, missing documentation, incomplete artifacts, or other agent-resolvable obligations as blockers.

`AWAITING HUMAN REVIEW` and `BLOCKED` are terminal outcomes for the current execution run, not new durable milestone lifecycle states.

## Human review

The v0.6.0 milestone-scoped review model remains unchanged.

Completed reviews remain historical evidence and are not invalidated by later repository changes.

## Engineering commands

No new generic engineering commands are required.

Do not add `milestone-show`, `milestone-check`, `milestone-complete`, or equivalent commands merely to adopt v0.7.1.

Repositories may keep or use repository-specific milestone commands if they already provide useful machine-decidable completion checks.

## Guide repository changes

v0.7.1 updates:

- `templates/prompts/execute-milestone.md`;
- `meta/MILESTONE-EXECUTION-MODEL.md`;
- `templates/PROMPTS.md`;
- `README.md`;
- `CHANGELOG.md`;
- `templates/guide-profile/guide-profile.json`.

The canonical planning prompt retains the v0.7 two-phase planning boundary and uses execution-profile terminology consistently.

## Product repository migration

Do not rewrite valid project truth merely to describe this guide-system correction.

For active milestones:

- keep existing goal, decisions, constraints, and acceptance criteria when they remain correct;
- update only execution instructions that permit stopping after implementation or tests;
- ensure required supporting work is not accidentally prohibited by scope wording;
- ensure the final implementation report states one explicit terminal outcome.

Completed milestones need no migration.

## Validation

After migration, a fresh implementation run must behave as follows:

1. implement the ready milestone;
2. run required validation;
3. perform the completion audit;
4. continue fixing every agent-resolvable gap;
5. terminate only as `COMPLETE`, `AWAITING HUMAN REVIEW`, or `BLOCKED`;
6. claim `COMPLETE` only when all applicable milestone obligations are satisfied.
