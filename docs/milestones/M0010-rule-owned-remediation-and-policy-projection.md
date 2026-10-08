# M0010 — Rule-Owned Remediation & Policy Projection

**State:** ready\
**Mode:** AI-executed, human-reviewed\
**Depends on:** completed M0009 Statistical Sampling Core

## Goal

Make rules the semantic ownership boundary for deterministic source rewrites and hygiene-generated EditorConfig policy, while preserving the existing safe command/transaction boundaries.

After M0010 an agent can answer, from the product itself:

```text
Which enabled rules does check evaluate?
Which enabled rules does format apply?
Which enabled rules does normalize apply?
Which rules generated this hygiene-owned EditorConfig setting?
```

`format`, `normalize`, bootstrap/update, and enable/disable remain commands. They stop being independent sources of product policy.

## Required authority

Implementation starts from this milestone and reads:

- `docs/specs/RULE-CAPABILITIES.md` from this package;
- `docs/ARCHITECTURE.md`;
- `docs/ENGINEERING.md`;
- `docs/TERMINOLOGY.md`;
- `docs/SPECS.md`;
- `docs/specs/HYGIENE.md`;
- `docs/specs/PROFILE.md`;
- `docs/specs/REWRITES.md`;
- `docs/specs/READABILITY.md`;
- current rule source/catalog, rewrite source, profile/config source, CLI source, and relevant tests.

M0004-M0009 are compatibility baselines where needed; they are not substitute authority for the resolved M0010 decisions below.

## Planning baseline

Current `main` records M0001-M0009 done. M0008 rule locality and M0009 statistical sampling are complete.

Current behavior has three policy ownership gaps:

1. `RewriteCatalog` owns `format`/`normalize` transformations independently of `RuleCatalog`;
2. broad `normalize` hides multiple semantic simplifications behind one command implementation;
3. `ProfileManager` hard-codes braces/accessibility EditorConfig policy inside mandatory `profile.dotnet.analysis.required`.

These gaps make it difficult to discover, disable, explain, or evolve one policy independently.

## Target architecture

A rule ID remains one fixed semantic policy and may expose explicit optional capabilities.

Conceptually:

```text
RuleCatalog
  ordered rule identities
  -> optional diagnosis capability
  -> optional format remediation
  -> optional normalize remediation
  -> optional EditorConfig projection
  -> existing profile remediation where applicable

check
  -> enabled diagnostic rules

format
  -> enabled format-remediation rules
  -> complete plan
  -> shared rewrite transaction

normalize
  -> enabled normalize-remediation rules
  -> enabled format-remediation rules on changed documents
  -> compile/plan validation
  -> shared rewrite transaction

bootstrap/update
  -> mandatory profile infrastructure
  -> enabled rule projections
  -> transactional repository mutation

enable/disable
  -> config participation change
  -> regenerate affected hygiene-owned projections
  -> one transaction
```

There is one semantic catalogue. A rewrite runner/transaction may remain as shared execution machinery, but there must not be a second catalogue that independently defines product rewrite policy.

Exact internal interface/type names are implementation freedom. Prefer a small explicit capability shape over a generic framework.

## Resolved decisions

### Rule participation gates every owned capability

For a configurable rule, disabled means disabled everywhere the product owns that policy:

```text
check diagnosis
format remediation
normalize remediation
EditorConfig projection
```

A command may select only capabilities relevant to that command, but it cannot silently execute a disabled rule's transformation or projection.

### Capability-only rules are valid

A product rule may own a deterministic remediation without emitting normal check findings.

M0010 uses this for the broad Roslyn format policy and the two normalization rules. Do not manufacture expensive/noisy check findings solely to make every rule look diagnostically symmetrical.

### `format` remains presentation-safe

The format command selects format-remediation rules only. Its initial explicit rule is `format.csharp.roslyn` v1.

This rule encapsulates today's ordinary project-aware Roslyn Formatter behavior and is enabled by default. M0010 does not model every Roslyn spacing/indent/newline option as an independent product rule.

### `normalize` becomes explicit policy composition

The current broad Roslyn simplification is split into:

```text
style.qualification.this.unnecessary v1
style.qualification.redundant v1
```

Both are configurable and enabled by default.

`normalize` applies enabled normalization rules in canonical order and then applies enabled format rules to changed documents before final validation/commit. Thus the default result remains normalized and formatted, but a disabled policy no longer runs invisibly.

### Profile/style split requires a real version migration

The mandatory profile currently semantically owns braces and accessibility preferences. Making them independently disableable is a semantic change, not a source move.

M0010 therefore advances:

```text
hygiene profile: dotnet-11 v1 -> v2
profile.dotnet.analysis.required: v1 -> v2
```

Profile v2 keeps mandatory analyzer infrastructure and repository policy plumbing. Braces/accessibility move to:

```text
style.braces.required v1
style.accessibility.explicit v1
```

Both are configurable and enabled by default.

### Generated EditorConfig is a projection

`ProfileManager` must not own the braces/accessibility setting literals after M0010.

Each contributing rule owns its exact settings. Shared profile/config infrastructure composes the hygiene-managed block from the enabled rule set.

Generated content includes rule-ID comments and deterministic ordering.

`root = true` remains mandatory profile infrastructure.

No runtime conflict resolver is added. Production rules must have unique/non-conflicting ownership of hygiene-managed keys and tests must enforce that invariant.

### Toggle operations reconcile projections immediately

`enable`/`disable` must not leave `.hygiene/config.json` and the hygiene-managed EditorConfig block semantically out of sync until a later `update`.

A toggle affecting projected policy plans and commits the config and derived managed projection together with rollback/conflict protection.

User-authored EditorConfig content outside the managed block is preserved.

### Public discoverability

`hygiene rules` exposes enough capability metadata to show whether a rule participates in:

```text
check
format
normalize
EditorConfig projection
```

The existing fields remain meaningful. Additive capability metadata is allowed; do not make users infer command participation from naming conventions.

`format` and `normalize` text/JSON results expose the ordered selected rule IDs. Per-character/per-file causal attribution is not required in M0010.

### Default behavior remains compatible

With all newly introduced configurable rules enabled:

- profile update to v2 preserves the effective braces/accessibility policy;
- format retains current formatter behavior;
- normalize retains current supported simplification + formatting behavior;
- old rule relative order and semantics remain unchanged except `profile.dotnet.analysis.required` deliberately advances to v2.

Intentional disabling is the new behavior change.

### Product/package version

M0010 changes public rule catalogue/profile behavior and advances the package/product minor version to:

```text
0.6.0
```

## Initial rule catalogue

The exact contracts are authoritative in `docs/specs/RULE-CAPABILITIES.md`.

New configurable rules:

```text
style.braces.required                v1  diagnose + editorconfig-project
style.accessibility.explicit         v1  diagnose + editorconfig-project
format.csharp.roslyn                 v1  format-remediate
style.qualification.this.unnecessary v1  normalize-remediate
style.qualification.redundant        v1  normalize-remediate
```

Updated mandatory rule:

```text
profile.dotnet.analysis.required     v2
```

Canonical total catalogue after M0010: 14 rules.

## Scope

In scope:

- rule capability architecture over one explicit ordered catalogue;
- removal of independent rewrite-policy registration from `RewriteCatalog` or equivalent;
- rule-driven format/normalize command selection;
- the five new rules above;
- profile v2 and `profile.dotnet.analysis.required` v2;
- rule-owned EditorConfig contributions;
- deterministic managed projection composition with rule-ID comments;
- immediate transactional projection reconciliation on enable/disable;
- `dotnet-11 v1 -> v2` update migration;
- public rule-capability discoverability;
- format/normalize selected-rule reporting;
- package version 0.6.0;
- required tests, installed-tool validation, self-host validation, and authority/documentation updates.

## Non-goals

Out of scope:

- one rule per Roslyn formatting option;
- direct source fixers for braces/accessibility;
- auto-remediation of `readability.control-flow.visual-block`;
- new documentation/readability/semantic-review semantics;
- new statistical sampling adoption;
- user-supplied rule parameters;
- plugin/discovery API;
- DI framework;
- reflection registration;
- generic rule metadata DSL;
- arbitrary rule dependency graph;
- model/provider invocation;
- broad modernization rules (`var`, expression bodies, file-scoped namespaces, collection expressions, target-typed new, naming/member ordering);
- unrelated hygiene cleanup.

## Acceptance criteria

**AC-01 — One semantic rule catalogue.** All production policy identities are registered once in one explicit static canonical rule catalogue; format/normalize execution does not maintain an independent semantic rewrite-policy catalogue.

**AC-02 — Explicit rule capabilities.** The implementation has a small explicit capability model allowing a rule to own diagnosis, format remediation, normalize remediation, and/or EditorConfig projection without requiring every rule to implement every capability.

**AC-03 — Whole-rule disable semantics.** Disabling a configurable rule suppresses every product-owned capability of that rule; enabling restores those capabilities without parameterizing the rule.

**AC-04 — Rule-driven format.** `hygiene format` executes enabled format-remediation rules in canonical order through the existing complete-plan transaction, and `format.csharp.roslyn` v1 owns the current Roslyn formatter policy.

**AC-05 — Rule-driven normalize.** `hygiene normalize` executes enabled normalize-remediation rules in canonical order, then enabled format-remediation rules on changed documents, with current compile-before/after validation and transaction safety retained.

**AC-06 — Explicit normalization split.** Current supported simplification behavior is owned by distinct `style.qualification.this.unnecessary` v1 and `style.qualification.redundant` v1 rules; disabling either independently prevents only its owned transformation.

**AC-07 — Default rewrite compatibility.** With all M0010 rewrite-capable rules enabled, representative current `format` and `normalize` inputs remain output-compatible with the accepted pre-M0010 behavior.

**AC-08 — Profile v2 split.** Supported profile becomes `dotnet-11` v2 and `profile.dotnet.analysis.required` becomes v2, owning mandatory analyzer/profile plumbing but no longer owning braces/accessibility preferences or severities.

**AC-09 — Braces policy rule.** `style.braces.required` v1 is configurable, enabled by default, owns the exact IDE0011/braces EditorConfig contribution, and diagnoses effective configuration drift using supported projected-source semantics.

**AC-10 — Accessibility policy rule.** `style.accessibility.explicit` v1 is configurable, enabled by default, owns the exact IDE0040/accessibility EditorConfig contribution, and diagnoses effective configuration drift using supported projected-source semantics.

**AC-11 — Derived EditorConfig projection.** The hygiene-managed EditorConfig policy block is deterministically composed from enabled contributing rules, includes visible rule-ID ownership comments, preserves user content, and contains no ProfileManager-owned duplicate braces/accessibility semantic literals.

**AC-12 — Projection ownership invariant.** Production rules cannot contribute conflicting values for the same hygiene-managed EditorConfig key; focused tests enforce the fixed catalogue ownership invariant without adding runtime conflict-resolution machinery.

**AC-13 — Transactional enable/disable.** Enabling/disabling a projected configurable rule transactionally updates `.hygiene/config.json` and affected hygiene-owned EditorConfig projection together; injected failure/conflict cannot leave them inconsistent.

**AC-14 — Disable preserves user authority.** Disabling a projected rule removes only the hygiene-owned contribution and enforcement; equivalent user-authored settings outside the managed block are preserved and are not treated as hygiene ownership.

**AC-15 — Real v1->v2 migration.** `hygiene update` accepts a valid `dotnet-11` v1 repository, migrates to v2, replaces the old hard-coded style block with derived enabled-rule projections, preserves unrelated repository content, and is idempotent.

**AC-16 — Public capability discovery.** `hygiene rules` text/JSON exposes rule capability participation sufficiently to identify diagnostic, format, normalize, and EditorConfig-projection rules; mandatory/configurable semantics remain explicit.

**AC-17 — Rewrite result transparency.** `format` and `normalize` text/JSON results expose the ordered selected rule IDs while retaining target/change/check-only information and deterministic output.

**AC-18 — Existing transaction architecture preserved.** RepositorySession reuse, full in-memory planning, compiler validation where required, conflict detection, atomic replacement, rollback, explicit/changed targeting, and check-only semantics remain intact.

**AC-19 — No generalized policy framework.** M0010 adds no reflection discovery, DI framework, plugin API, policy DSL, generated metadata system, dependency DAG, or new runtime dependency merely to support capabilities/projections.

**AC-20 — Existing rule compatibility.** Pre-M0010 rule IDs/versions/diagnostic behavior remain compatible except the deliberately versioned `profile.dotnet.analysis.required` v2 change; documentation/review/sampling behavior is unchanged.

**AC-21 — Version and installed artifact.** Product/package version is 0.6.0 and exact locally packed/installed validation proves the M0010 catalogue, profile v2 migration, rule toggles/projections, format, and normalize surfaces.

**DOC-01 — Authority reconciliation.** Architecture, engineering, terminology, general specs, profile, rewrites, readability references, rule-capability spec, milestone index, and public/help text consistently describe rule-owned capabilities and projection.

**REV-01 — Human completion review.** Project owner/delegate confirms the resulting model is more explainable without becoming a framework: one rule owns one policy, generated settings are traceable, disabling is coherent across surfaces, format/normalize remain safe commands, and the initial rule split is appropriately small.

## Required evidence cases

| ID | Parent | Required evidence case |
|---|---|---|
| EC-01a | AC-01 | all 14 production identities enumerate from one ordered rule catalogue and no independent production rewrite-policy table defines format/normalize semantics |
| EC-03a | AC-03 | disabling `format.csharp.roslyn` makes `format` a no-op for ordinary Roslyn formatting while leaving unrelated check/normalize rules active |
| EC-03b | AC-03 | disabling one normalize rule suppresses only that transformation while the sibling normalize rule remains active |
| EC-06a | AC-06 | redundant `this.` and other redundant qualification are independently toggleable on a fixture containing both |
| EC-07a | AC-07 | default-enabled `format` output matches accepted pre-M0010 representative formatter output |
| EC-07b | AC-07 | default-enabled `normalize` output matches accepted pre-M0010 representative simplifier+formatter output |
| EC-09a | AC-09 | enabled braces rule projects both owned settings and detects a deeper effective override that weakens them |
| EC-10a | AC-10 | enabled accessibility rule projects both owned settings and detects a deeper effective override that weakens them |
| EC-11a | AC-11 | generated managed block contains deterministic rule-ID comments/order and preserves unrelated existing EditorConfig text byte-for-byte where not intentionally normalized by existing profile behavior |
| EC-13a | AC-13 | successful disable/enable atomically changes config plus managed projection |
| EC-13b | AC-13 | injected fault/conflict during toggle leaves pre-command config and projection state |
| EC-14a | AC-14 | disabling braces with a matching user-authored setting outside the managed block preserves that user setting while removing hygiene ownership |
| EC-15a | AC-15 | real v1 repository updates to v2 with equivalent default braces/accessibility effective policy and unrelated content preserved |
| EC-15b | AC-15 | running update again on migrated v2 state produces no change |
| EC-16a | AC-16 | `rules --output json` exposes all 14 rules in canonical order with exact capability/configurability metadata |
| EC-17a | AC-17 | format/normalize JSON each expose their ordered selected rule IDs and reflect disabled-rule participation correctly |
| EC-18a | AC-18 | injected multi-file rewrite conflict/fault still leaves the full selected target set at pre-run state |
| EC-21a | AC-21 | exact packed 0.6.0 tool installed in isolated consumer performs v1->v2 update, rule toggles, rules listing, format, normalize, and check using the installed artifact only |

## Validation gates

| ID | Required validation | Target/locus | Proves |
|---|---|---|---|
| VAL-01 | focused Core tests for catalogue/capabilities, command selection, rule projections, effective-config diagnosis, profile migration, and transaction rollback | Tier 1 / Windows 11 + .NET 11 | AC-01..AC-15, AC-18..AC-20; EC-01a, EC-03a/b, EC-06a, EC-09a, EC-10a, EC-11a, EC-13a/b, EC-14a, EC-15a/b, EC-18a |
| VAL-02 | built CLI tests for rules/config toggles/profile update/format/normalize/check text+JSON | Tier 1 / Windows 11 + .NET 11 | AC-03..AC-17, AC-20; EC-16a, EC-17a |
| VAL-03 | isolated SDK-style Git repositories covering nested EditorConfig overrides, v1 migration, changed/explicit rewrite targeting, and toggle transactions | Tier 3 / Windows 11 + .NET 11 + Git | AC-07..AC-18; EC-07a/b, EC-09a, EC-10a, EC-13a/b, EC-14a, EC-15a/b, EC-18a |
| VAL-04 | exact locally packed/installed 0.6.0 consumer workflow | Tier 4 / isolated Windows consumer repository | AC-16, AC-17, AC-21; EC-16a, EC-17a, EC-21a |
| VAL-05 | `./eng/validate.ps1` including repository self-host and `git diff --check` | Tier 2 / complete repository | AC-18..AC-21, DOC-01 |
| VAL-06 | direct source/document review of catalogue ownership, absence of separate rewrite policy registry, projection ownership, docs consistency, and diff scope | repository review | AC-01, AC-02, AC-08..AC-12, AC-19, AC-20, DOC-01 |
| VAL-07 | `REV-M0010-COMPLETION` | human / project owner or delegate | REV-01 |

`eng/validate.ps1` remains the canonical aggregate validation command. Extend its existing exact-installed-tool path rather than introducing a parallel harness.

## Implementation guidance

A likely sequence is:

```text
add rule-capability contracts over the existing explicit catalogue
-> move format/normalize semantic registration under rule ownership
-> split current normalize behavior into two rule modules
-> add explicit Roslyn format rule
-> split profile v1 style settings into braces/accessibility rules
-> implement deterministic EditorConfig projection composition
-> make enable/disable reconcile projections transactionally
-> implement profile v1->v2 update migration
-> expose capabilities and selected rewrite rule IDs
-> focused regression/compatibility tests
-> exact installed 0.6.0 validation
-> authority/docs reconciliation
-> human review
```

This is guidance, not a mandatory work-package decomposition.

## Human review

Canonical review ID:

```text
REV-M0010-COMPLETION
```

Review subject:

- one diagnostic+projection rule (`style.braces.required`);
- one remediation-only normalize rule;
- `format.csharp.roslyn`;
- canonical rule catalogue and command selection;
- generated EditorConfig block;
- enable/disable transaction;
- profile v1->v2 migration;
- rules/format/normalize public output;
- validation evidence.

Acceptance questions:

1. Can a reviewer find the semantic owner of every M0010 generated setting/rewrite without searching an unrelated host class?
2. Does disabling one rule consistently suppress all hygiene-owned effects of that rule?
3. Are `format` and `normalize` still understandable safety-level commands rather than new semantic owners?
4. Did the implementation avoid turning Roslyn's option universe into hundreds of product rules?
5. Is profile v2 genuinely narrower and are braces/accessibility independent policies now?
6. Is the projection mechanism simple enough to understand locally without framework archaeology?
7. Is default pre-M0010 behavior preserved where the milestone says it must be?

Waiver policy: explicit project-owner decision only.

## Completion expectation

All agent-resolvable obligations and validations must be complete before stopping. If only `REV-M0010-COMPLETION` remains, leave the milestone at `AWAITING HUMAN REVIEW`.
