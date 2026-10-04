# Deterministic Rewrite Behavior

## Common targeting

`format` and `normalize` use the same repository and target-selection concepts as `check`:

```text
hygiene format
hygiene format --changed
hygiene format <file|directory> [...]

hygiene normalize
hygiene normalize --changed
hygiene normalize <file|directory> [...]
```

Explicit paths and `--changed` are mutually exclusive. Repository discovery, relative paths, exclusions, SDK-style project membership, changed-file semantics, outside-repository rejection, and overlap de-duplication align with `check`.

## Common output

Both commands support `--output text|json` and `--check`.

`--check` computes the same rewrite plan without committing source changes. A successful command returns exit `0` whether zero or more changes are needed/applied; nonzero exits retain existing error semantics.

JSON schema version 1 exposes: `command`, `checkOnly`, `targetCount`, `changedCount`, `unchangedCount`, and deterministic repository-relative `changedPaths`.

## Mutation transaction

Rewrite commands are all-or-nothing over the complete resolved target set:

```text
resolve all targets
-> load required Roslyn context
-> compute complete rewrite plan in memory
-> validate complete plan
-> if --check: report only
-> else commit plan
```

No selected source file is replaced before plan construction and required validation succeed for every selected target. Commit uses atomic per-file replacement plus rollback protection sufficient to guarantee that failure/cancellation does not leave a mixed old/new selected target set. External modification between read/plan and commit is rejected rather than silently overwritten.

## Formatting

`hygiene format` is presentation-only. It uses Roslyn formatting against actual loaded document/project context and applicable project/editor configuration available to Roslyn. It may change whitespace/trivia presentation but must not intentionally perform semantic simplification, structural modernization, naming changes, or normalization policy rewrites.

Formatting is idempotent and does not require a clean semantic compilation when Roslyn can load and parse the selected source sufficiently.

## Normalization

`hygiene normalize` is deterministic, project-aware, and semantics-preserving.

Relevant loaded projects must compile without compiler errors before transformation. Rewritten relevant project context must also compile without compiler errors before commit. Changed normalized documents are formatted before final comparison/commit.

Normalization is idempotent.

## Initial normalization catalogue

M0004 intentionally limits normalization to Roslyn-backed simplification where semantic services establish equivalence:

1. simplify redundant member/type/namespace qualification where Roslyn semantic simplification proves it removable;
2. simplify qualified or explicit type/name syntax to the Roslyn-recommended equivalent where semantic simplification proves equivalence;
3. remove unnecessary explicit `this.` qualification only where semantic simplification proves it redundant.

These are one fixed policy and are not individually configurable in M0004.

Do not introduce transformations for `var` policy, expression-bodied members, namespace style, collection expressions, target-typed `new`, modifier/member ordering, naming, or broad modernization.

## Relationship to check

Rewrite commands do not automatically execute `hygiene check`.

Canonical agent workflow:

```text
implement/change code
-> run relevant tests
-> hygiene normalize
-> hygiene check
-> resolve deterministic findings and semantic review work
-> rerun/recheck as needed
```

No rewrite history database is introduced.
