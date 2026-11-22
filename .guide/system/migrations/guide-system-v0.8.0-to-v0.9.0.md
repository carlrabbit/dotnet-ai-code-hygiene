# Migration — Guide System v0.8.0 to v0.9.0

## Purpose

Adopt planning-seeded, lossless execution-ledger coverage, explicit acceptance-evidence topology where needed, and durable completion traceability for AI-executed milestones.

0.9 corrects two related weaknesses in the 0.8 execution model:

1. an implementation agent could create a structurally valid ledger while compressing many independently verifiable milestone obligations into broad work-package summaries;
2. even when a parent obligation survived intact, materially different behavioral paths inside that obligation could remain untested while aggregate validation appeared green.

0.9 also preserves a compact obligation/evidence-to-proof trace after mutable execution state is no longer retained.

## Migration required

Recommended for repositories that use AI-executed milestones and persistent `.execution/<milestone-id>.md` ledgers.

Repositories that do not use AI-executed milestone execution may update guide metadata with no behavioral repository change.

## Model change

0.8 used:

```text
planning
  -> ready milestone

implementation
  -> create/reconcile execution ledger
  -> derive work packages
```

0.9 uses:

```text
planning
  -> ready milestone
  -> seed lossless obligation registry in .execution/<milestone-id>.md
  -> seed material EC-* evidence cases where one obligation needs independent proof paths

implementation
  -> verify obligation/evidence-case coverage
  -> derive work packages
  -> map work packages to obligation IDs
  -> attach criterion/evidence-case-specific evidence
  -> persist compact durable completion evidence
```

The ledger remains operational state rather than project authority.

## Planning requirements

For every new AI-executed ready milestone:

1. assign stable IDs to every independently verifiable acceptance criterion and every other material completion obligation;
2. identify materially distinct proof paths inside an obligation and assign stable `EC-*` evidence-case IDs only where independent proof is necessary;
3. do not generate evidence cases as a Cartesian product of every dimension;
4. assign stable IDs to required validation gates;
5. record which obligation/evidence-case IDs each validation gate is intended to prove;
6. create `.execution/<milestone-id>.md` before `ready`;
7. seed one pending row per obligation and every required evidence case;
8. verify exact set equality between applicable milestone obligation IDs and seeded ledger rows and, where applicable, between milestone and ledger evidence-case IDs;
9. leave work-package mapping, implementation tasks, evidence, status progression, and resume state empty for implementation.

Do not collapse several separately provable behaviors into one criterion or ledger row merely to reduce document size.

When a capability applies across materially different existing surfaces (for example scalar/enum, Root/Batch, sync/async, provider variants, inherited/direct, source/packaged consumer), require separate evidence when those surfaces can travel through different implementation paths or contract behaviors.

## Implementation requirements

Implementation must preserve the planner-seeded obligation registry.

It may map several obligation IDs to one work package, but it must not delete, merge, renumber, paraphrase, or replace seeded obligation rows merely to simplify execution.

A validation result proves only the obligation/evidence-case IDs whose required behavior the executed test/scenario actually exercises.

When explicit evidence cases exist, all applicable child cases must be established before the parent obligation can be considered satisfied.

Before `COMPLETE`, verify:

```text
set(applicable milestone obligation IDs)
==
set(ledger obligation IDs)
```

Then verify concrete implementation and validation evidence for every individual applicable obligation and every required evidence case.

Before transitioning the milestone to `done`, persist a compact completion-evidence mapping in the milestone or another immutable completion record explicitly referenced by it. This durable trace survives optional cleanup of the mutable execution ledger.

## Existing milestone treatment

### Draft/planning milestones

Before marking an AI-executed milestone `ready`:

- add stable obligation IDs where missing;
- make independently verifiable criteria separate;
- identify selective `EC-*` evidence cases where one obligation spans materially different proof paths;
- seed the execution ledger with obligations and evidence cases;
- seed required validation gates and their intended obligation/evidence-case coverage.

### Ready but not yet implemented

Prefer migrating the milestone and ledger to the 0.9 shape before implementation starts.

If no seeded ledger exists, create it from the milestone without compressing obligations.

### Active implementation

Reconcile the current milestone and ledger:

- enumerate every applicable milestone obligation separately;
- assign stable IDs where the milestone lacks them;
- identify material evidence cases that current aggregate coverage could hide;
- preserve existing work packages;
- expand grouped ledger summaries into separate obligation rows/evidence-case rows;
- map existing work packages/evidence to those rows;
- reopen any `done` obligation/evidence case whose specific evidence is missing, indirect without justification, substitute-only, sibling-only, or inferred only from an aggregate pass.

Do not discard useful existing execution evidence merely because the ledger shape changes.

### Completed milestones

No retroactive ledger or completion-evidence migration is required.

If a completed milestone is later reopened, apply the 0.9 obligation/evidence-case model and durable completion trace at that time.

## Validation mapping example

Do not treat:

```text
VAL-03 integration scenario: PASS
```

as evidence for every acceptance criterion associated with the feature.

Instead record the behaviors actually exercised.

For simple obligations:

```text
VAL-03
  proves: AC-07, AC-08, AC-09
```

For one obligation with materially different proof paths:

```text
AC-10 coordinated target supports scalar and enum

EC-10a scalar path
EC-10b enum path

VAL-03
  proves: EC-10a

VAL-04
  proves: EC-10b
```

The parent obligation remains incomplete until both required evidence cases are established.

## Guide metadata

Update `.guide-profile.json` guide-system and applicable profile versions to `0.9.0` when adopting this guide version.

No schema-version change is required; `.guide-profile.json` remains schema version 2.

## Completion

Migration is complete when new AI-executed planning seeds a lossless obligation ledger before `ready`, selectively models materially distinct evidence cases where required, implementation owns only the execution extension of that ledger, completion reconciliation proves each applicable obligation/evidence case individually rather than relying on grouped work summaries or aggregate validation claims, and completed milestones preserve a compact durable evidence trace before mutable ledger cleanup.
