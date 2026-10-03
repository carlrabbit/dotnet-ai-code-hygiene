# Architecture

## System shape

The intended product pipeline is:

```text
source
  -> target resolution
  -> deterministic formatting
  -> safe normalization
  -> deterministic rule evaluation
  -> findings/review candidates
  -> caller remediation
```

The CLI is the supported public surface. Internal assemblies are implementation details unless a later milestone deliberately promotes a reusable API.

## Major boundaries

### CLI boundary

Owns:

- command parsing through `System.CommandLine`;
- help/version;
- stable exit mapping;
- stdout/stderr routing;
- text/JSON presentation;
- cancellation handoff;
- invocation of application services.

Command handlers should remain thin. Product semantics belong in testable application/domain code.

### Formatting boundary

Owns deterministic presentation changes. Formatting must not intentionally change program semantics.

The initial formatter strategy is Roslyn-based for C#.

### Normalization boundary

Owns high-confidence, semantics-preserving structural transformations. Normalization is intentionally stricter than hygiene remediation and must not become a generic auto-fix channel.

### Hygiene boundary

Owns deterministic rule ordering, enabled-rule evaluation, findings, review candidates, run/finding identities, and rule-specific evidence construction.

Ordinary hygiene detection has no embedded-model dependency.

### Decision-store boundary

Owns persistent reviewed exceptions and later rule configuration.

The persistence format is Git-friendly JSON, but agents/humans normally mutate it through CLI commands.

Writes must eventually be atomic and conflict-aware. Those behaviors are implemented with the persistence vertical slice in M0002 unless explicitly pulled forward.

## Rule model

Rules are immutable opinionated units:

```text
Rule
  ID
  version
  fixed semantics
  fixed applicability
  fixed place in canonical order
```

User configuration does not provide rule parameters, severity overrides, thresholds, ordering, or per-file rule policy.

The engine guarantees deterministic evaluation of the selected rules. It does not guarantee that the selected rule set is philosophically coherent.

## Identity model

### Rule

Persistent semantic identity:

```text
readability.long-line.review
```

### Run

One analysis invocation:

```text
R-8K3M
```

### Finding

A concrete occurrence within a run:

```text
R-8K3M/F-42
```

Finding handles are interaction conveniences, not persistent occurrence keys.

### Ignore decision

A persistent reviewed exception:

```text
I-17
```

An ignore decision survives analysis runs by matching a stable occurrence identity.

## Fingerprinting

Occurrence identity uses a Roslyn semantic anchor plus rule-specific canonical evidence.

General canonical evidence excludes:

- line numbers;
- absolute paths;
- indentation;
- ordinary whitespace/trivia when irrelevant.

Rules may include trivia when trivia is exactly what the rule evaluates.

Evidence is hashed with SHA-256. Structurally identical sibling occurrences may use a deterministic local ordinal/discriminator after the semantic anchor and structural fingerprint.

A formatting-only edit should not invalidate an ignore for a structural rule. A material change to the evidence a rule evaluates should reopen the occurrence.

## Target/context distinction

Target selection determines where a command may emit results or apply changes.

Analysis may read broader repository context when the rule requires it. Therefore:

```text
target scope != context visibility
```

This distinction prevents file-scoped agent workflows from degrading semantic correctness.

## M0001 architecture scope

M0001 establishes:

- solution/project boundaries;
- CLI boundary;
- testing boundary;
- local engineering validation entry point;
- packaging metadata.

M0001 does not implement the hygiene engine, decision store, fingerprinting engine, or formatter behavior beyond any minimal scaffolding required to keep the skeleton coherent.
