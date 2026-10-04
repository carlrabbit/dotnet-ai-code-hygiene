# Architecture

## System shape

```text
repository
-> profile state/configuration
-> command-selected fixed rule set
   -> fixed rule evaluation
   -> optional fixed deterministic remediation for bootstrap/update
-> findings/review batches

source targets
-> M0004 rewrite pipeline
   -> format
   -> normalize

normal check
-> mandatory profile rules
-> configurable deterministic source rules
-> configurable semantic review rules
```

## Fixed rules, fixed remediation

Rule semantics do not receive an execution phase/mode parameter.

A rule may define:

```text
diagnosis(repository/context) -> occurrences
optional deterministic remediation -> proposed repository edits
```

`bootstrap`, `update`, and `check` select rule sets/execution policy around those fixed definitions.

This prevents hidden parametrization where one rule ID means different things in different commands.

## Rule sets

Engine-defined sets:

```text
bootstrap
update
normal
```

Membership is not user configuration.

Profile rule membership is defined in `docs/specs/PROFILE.md`.

## Profile ownership

Committed profile state:

```text
.hygiene/profile.json
```

Namespaced profile artifact:

```text
.hygiene/profile/Hygiene.props
```

The product may maintain narrowly delimited integration points in repository-standard files such as root `Directory.Build.props` and `.editorconfig`.

Ownership is explicit and bounded:

- hygiene may reconcile its own generated file/managed section/import;
- unrelated user content is preserved;
- arbitrary conflicting project/nested EditorConfig content is diagnosed, not silently rewritten.

## Profile evaluation

`profile.dotnet.analysis.required` validates effective project/analyzer configuration, not only generated file text.

This is necessary because project-local/nested configuration can override root/profile defaults.

## StyleCop boundary

StyleCop is a prohibited tool, not a delegated capability.

The profile rule detects configured/effective activation but has no deterministic removal remediation. Dependency removal is caller-controlled because repository-wide diagnostic impact can require judgment.

## Documentation subject model

Documentation rules operate on API documentation subjects rather than raw tag names.

```text
API subject
-> source declaration representation
-> resolved summary carrier
```

For positional records, the synthesized property is the subject while the matching record `<param>` is its source summary carrier.

This model is shared by deterministic summary-required logic and semantic summary-quality sampling.

## M0004 boundaries retained

M0004 all-or-nothing mutation infrastructure is reused for bootstrap/update profile edits.

M0004 format/normalize semantics, installed-tool public boundary, and agent-help model remain intact.

## Model boundary

Profile/bootstrap/update/documentation rules introduce no model call.

Semantic review still uses the existing external caller/frontier escalation contract.
