# Guide System Migration — v0.7.1 to v0.7.2

## Purpose

Make executability by the project's baseline implementation model an explicit requirement of the `ready` milestone boundary.

## Applies to

Repositories using milestone-driven AI implementation, especially repositories that use a low-cost baseline implementation model and stronger reasoning during planning.

Repositories whose existing planning process already guarantees this property may treat the behavioral migration as a no-op after updating guide-version metadata.

## Core correction

A milestone is no longer considered `ready` merely because material project decisions appear documented.

Planning must also establish that the configured baseline implementation model can execute the milestone using:

- the ready milestone;
- referenced project authority;
- the live repository;
- normal repository tooling and validation.

The executor must not need to invent a new material decision about architecture, semantics, compatibility, scope, acceptance, or validation policy.

For the default ChatGPT-focused guide profile, the baseline implementation model is GPT-5.6 Luna.

## Required planning semantics

Update repository-local planning instructions or copied milestone templates derived from older guide versions so that `ready` requires baseline-model executability.

Planning should:

- resolve project-level decisions before implementation;
- make target state and acceptance criteria observable;
- define validation strongly enough for the executor to determine success;
- route subjective acceptance to milestone-scoped human review;
- shape long execution and validation using bounded or resumable work where necessary;
- avoid using a stronger implementation model as the normal remedy for an incomplete milestone.

Do not make milestones implementation scripts. Exhaustive edit sequences, predicted file lists, and line-by-line prescriptions remain unnecessary unless they are contractually significant.

## Diagnostic work

When planning cannot settle a material decision without additional runtime evidence, create a focused diagnostic milestone or investigation whose output is evidence rather than the final product change.

After the evidence is available, return to planning and create or revise the implementation milestone until it satisfies the baseline-executability criterion.

Do not silently delegate unresolved project policy to the baseline implementation agent.

## Execution volume

Do not interpret baseline executability as a requirement for tiny milestones.

Large coherent transformations remain valid when:

- decisions are settled;
- scope boundaries are explicit;
- implementation choices are primarily local mechanics;
- long operations are bounded or resumable;
- validation provides reliable completion evidence.

## Execution profiles

Do not add `strong`, `frontier`, or similar model-capability execution profiles.

Existing execution profiles remain orthogonal and describe autonomy or review expectations:

- `human-led`;
- `ai-assisted`;
- `ai-executed-human-reviewed`;
- `ai-executed-broad`.

## Guide profile

When `.guide-profile.json` is used, update the guide-system and base-profile versions to `0.7.2`.

The v0.7.2 template also records the planning-only baseline implementation model under `executionModel.baselineImplementationModel`.

The default value is:

```text
GPT-5.6 Luna
```

Projects may intentionally choose another baseline.

Ordinary implementation agents still do not need to read `.guide-profile.json`; the ready milestone is their durable execution contract.

## v0.7.1 execution contract

The v0.7.1 executor semantics remain valid:

```text
implement -> validate -> completion audit -> continue or terminate
```

No new generic engineering commands are required.

The canonical execution prompt does not require replacement solely for this migration unless a repository copied or customized it in a way that contradicts the baseline-readiness model.

## Guide repository changes

v0.7.2 updates:

- `README.md`;
- `CHANGELOG.md`;
- `meta/MILESTONE-EXECUTION-MODEL.md`;
- `templates/PROMPTS.md`;
- `templates/prompts/plan-milestone.md`;
- `templates/milestones/milestone-template.md`;
- `templates/guide-profile/guide-profile.json`.

It adds:

- `decisions/ADR-0021-milestone-readiness-targets-the-baseline-implementation-model.md`;
- `migrations/guide-system-v0.7.1-to-v0.7.2.md`.

## Migration result

After migration, planning—not executor model escalation—is responsible for making ordinary milestones implementable by the project's baseline implementation model.
