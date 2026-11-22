# Specialization Model

## Status

Authoritative for project-local specialization of generic guide concepts.

## Purpose

The guide system defines reusable engineering shapes, decision surfaces, and validation concepts.

Concrete projects often depend on technologies, runtimes, services, installed applications, environment constraints, or architecture choices that are too specific to belong in the central guide repository.

A project specialization is project-local authority that resolves those concrete details while conforming to the generic guide model.

## Core rule

```text
guide system
  defines what must be decided

product repository
  records the concrete answer
```

Do not create central technology-specific guide folders merely because one project needs detailed rules for a named product, runtime, service, or architecture.

Examples that normally remain project-local include:

- Power BI Desktop or Power BI Service integration;
- a particular SQL Server topology;
- application-specific browser/E2E harness behavior;
- a particular native graphics/runtime stack;
- organization-specific authentication/test identities;
- project-specific artifact generation or deployment conventions.

## Planning objectives and local profiles are distinct

Three project-owned mechanisms are available:

- `docs/PROJECT-GUIDANCE.md` (optional): the project's agentic-development objectives, clarifications of guide methodology, and additional planning/review expectations; consumed by planning.
- `.guide/profiles/<id>/PROFILE.md` (optional): a registered local profile for an engineering shape that activates multiple coordinated obligations across a repository, component, or surface.
- Normal engineering/specification/architecture/decision documents: the concrete implementation, validation, technology, runtime, and policy answers consumed by implementation.

For example, an Android keyboard might justify a local `android-ime` profile with lifecycle, privacy, interaction, and device-validation obligations. Which Android APIs, Kotlin libraries, build tools, and test devices it actually uses are ordinary engineering authority. A Kotlin library surface only receives a `kotlin-library` profile if a supported reusable library/API contract genuinely exists.

Local profiles should be used when they capture a coherent engineering shape, not because a project has selected a technology. Do not create a local profile for every feature or named runtime. Local profiles can remain local permanently; promotion into central reusable guidance is optional.

## Where specializations live

A specialization is ordinary project truth. It should normally live in the repository's existing authority structure, for example:

```text
docs/ENGINEERING.md
docs/engineering/<topic>.md
docs/SPECS.md
docs/specs/<topic>.md
docs/decisions/<decision>.md
```

Do not require ordinary implementation agents to consult the external guide repository to interpret a specialization.

Do not create a generic `.specializations/` coordination layer merely to mirror guide concepts. Use normal project authority unless a concrete repository has a separate reason to organize its documentation differently.

Project-specific validation-cost priorities may be recorded in `docs/PROJECT-GUIDANCE.md` for planning. Implementable checks, fixture setup, and any justified evidence-reuse or investigative isolation rules belong in normal engineering authority. Milestones name focused checks and only exceptional gates justified by product risk or explicit contract. Do not create `local:efficient-testing` or `local:simulation` profiles for a single testing preference.

## Specializing validation targets

For an external or runtime-bound integration target, project authority should resolve the applicable parts of the generic validation contract:

- target;
- availability;
- execution locus;
- platform/capability requirements;
- provisioning/connection;
- permitted host provisioning versus shared infrastructure changes;
- accepted alternative execution loci and evidence equivalence;
- isolation;
- reset/cleanup;
- identity/secrets;
- invocation through repository tooling;
- evidence;
- infrastructure versus product failure semantics;
- fallback validation;
- release relationship;
- consumer-surface relationship.

The project may add domain-specific requirements beyond this list.

## Specialization versus profile

A profile describes a broad reusable engineering shape that materially changes several repository expectations.

A specialization resolves concrete project choices within those expectations.

Example:

```text
profile:
  dotnet-library

project engineering policy:
  integration-first

validation specialization:
  target = installed external desktop runtime
  locus = local
  platform = Windows

concrete project authority:
  target = Power BI Desktop
  invocation/evidence/reset rules = repository-specific
```

Do not promote `Windows`, `local-development`, `integration-first`, `NuGet`, `GitHub`, or a named external product into profiles merely to make the composition machine-like.

A reusable product shape can still justify a profile even when one concrete realization uses a technology-specific packaging mechanism. For example:

```text
profile:
  cli-tool

project specialization:
  implementation = .NET
  distribution = dotnet-tool
  command name / package metadata / installation policy = repository-specific
```

The profile owns reusable CLI/process concerns. The specialization owns the concrete .NET tool packaging/runtime choices.

## Promotion to reusable guidance

Start technology- or architecture-specific rules as project-local specialization.

Promote a concept into generic guide guidance when the same decision surface has appeared across unrelated projects and the abstraction remains useful without depending on the originating project's domain terminology.

Promote a concept into a profile only when it represents a recognizable reusable repository/product shape and materially changes multiple engineering, documentation, validation, or release concerns.

Useful promotion signals include:

- the same planning questions recur in multiple unrelated projects;
- centralizing the abstraction materially reduces planning uncertainty;
- the abstraction can be stated without naming one concrete product;
- projects still retain meaningful local choices after applying the abstraction.

Avoid premature centralization. The guide repository should not become a catalog of concrete projects or vendor integrations.

## Planning responsibility

Planning should recognize when generic guidance is insufficient and durable project-local specialization is required before a milestone can become `ready`.

If implementation would otherwise need to invent material rules for an external runtime, service, platform, test environment, packaging boundary, or domain architecture, planning should create or update the relevant project authority and reference it from the milestone.

Local implementation mechanics that fit an already-set specialization remain executor-owned.
