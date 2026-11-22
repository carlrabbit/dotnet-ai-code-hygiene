# Profile Model

## Status

Authoritative for reusable profile selection, applicability, and composition.

## Purpose

A profile is a reusable engineering shape that activates scoped documentation, engineering, validation, and release expectations. Profiles must not override risk-proportionate milestone assurance by imposing unconditional release qualification, evidence registries, or validation infrastructure on ordinary milestones.

Profiles avoid one universal repository template while keeping concrete project technology and architecture in project-local authority.

Example profiles:

```text
base
dotnet-library
source-generator
cli-tool
blazor-component
artifact-first-runtime
```

A profile defines a broad reusable engineering shape. It may define applicability, documentation layers, engineering blocks, validation expectations, maturity behavior, repository-role behavior, and migration concerns.

## Shared and project-local profile sources

Shared profiles are maintained upstream under `profiles/<id>/` and resolved by product planning from the pinned `.guide/system/profiles/<id>/` snapshot. The upstream guide itself resolves its root `profiles/<id>/` directly. Never silently mix profiles from an unpinned upstream branch with a consuming repository's snapshot.

A consuming project may define additional **local profiles** under:

```text
.guide/profiles/<id>/PROFILE.md
```

Register only intentionally applied local profiles in `.guide-profile.json` using the reserved ID namespace `local:<id>`. An unprefixed ID refers to a shared profile. The suffix `<id>` is a lower-case kebab-case stable name and maps directly to the corresponding local directory. Local profiles use the same repository/component/surface scope model and additive composition semantics as shared profiles.

Example schema-version-2 entry:

```json
{
  "id": "local:android-ime",
  "version": "0.1.0",
  "scopes": [
    { "kind": "component", "ids": ["keyboard-app"] }
  ]
}
```

The local `version` is independently maintained by the project. The guide-system version and shared profile versions follow guide releases. No new metadata property or schema-version bump is needed. `local:<id>` registrations must resolve to a readable matching `PROFILE.md`; missing or ambiguous definitions are planning errors, not silently skipped obligations. Merely finding a local profile on disk does not activate it.

Local profile docs are project-authored planning authority, not copied central guide methodology. They may specify applicability, mandatory and conditional obligations, project-owned decisions, validation/release considerations, and additional capabilities. Their operative conclusions must be represented in project authority or the ready milestone for disconnected implementation.

A project can define a local profile for a concrete platform engineering shape, such as an Android input-method component, when it changes multiple engineering/validation/lifecycle expectations. Being project-owned does not excuse profiles that are merely lists of one-off technology choices.

## Capability and decision vocabulary

Profiles may distinguish:

- **mandatory obligations** — apply whenever the profile applies;
- **conditional obligations** — apply only when the named product capability or consumer surface exists;
- **project-owned decisions / extension points** — require local resolution when material; they are not permission to waive mandatory obligations.

Existing shared profiles are valid without adopting prescribed new headings. Avoid duplicating the same rule across a capability listing and an obligation listing. Project guidance may add planning/review obligations, but cannot silently negate an applicable profile requirement.

## Profiles are not feature tags

Do not create a profile merely for:

- an operating system;
- local versus CI execution;
- one external product or service;
- one packaging mechanism;
- one test-strategy preference;
- one concrete product architecture.

Those concerns should normally be expressed through project engineering policy, validation targets/loci, reusable generic guidance, or project-local specialization.

## Multi-profile composition

A repository may combine a small number of profiles when multiple broad engineering shapes genuinely apply.

Profiles compose additively:

```text
base obligations
+ applicable shared and registered local profile obligations
+ applicable project planning guidance
+ project-local specialization / explicit conflict resolution
= effective project contract
```

Profile order has no semantic meaning. There is no `primary` profile, `secondary` profile, last-profile-wins behavior, or implicit precedence hierarchy.

Avoid profile proliferation that merely reconstructs a project from feature tags.

## Profile applicability

A profile may apply:

```text
repository-wide
component-scoped
surface-scoped
```

Use repository-wide applicability when the engineering shape describes the repository as a whole.

Use component scope when the profile describes one or more durable semantic components in a mixed repository, for example a set of source-generator packages inside a larger .NET package suite.

Use surface scope when the profile applies to a durable consumer/engineering surface that is not naturally identical to one directory or project, for example a compile-time generator consumption surface.

Applicability is semantic rather than path-based. Component and surface identifiers are project-defined identifiers whose meaning must be recoverable from normal project authority such as `docs/ARCHITECTURE.md`, `docs/ENGINEERING.md`, or a relevant specification.

Do not make path globs in `.guide-profile.json` the authoritative definition of a component or surface. Paths may change during refactoring while the semantic applicability remains stable.

## `.guide-profile.json` scope metadata

`.guide-profile.json` records profile selection and scope for planning/traceability. It is not ordinary implementation authority.

Guide-profile schema version 2 represents profile applicability with `scopes` entries.

Repository-wide example:

```json
{
  "id": "dotnet-library",
  "version": "1.0.0",
  "scopes": [
    { "kind": "repository" }
  ]
}
```

A first-class command-line product is a reusable product shape rather than merely a packaging mechanism. Use `cli-tool` when the command/process surface itself is supported product behavior.

For example, a .NET global/local tool normally uses:

```text
base + cli-tool
+ project-local distribution specialization: dotnet-tool
```

Do not add `dotnet-library` solely because `dotnet tool` uses a NuGet package as transport. Add it only when a supported reusable library/API surface also exists.

Scoped example:

```json
{
  "id": "source-generator",
  "version": "1.0.0",
  "scopes": [
    {
      "kind": "component",
      "ids": [
        "generator-packages"
      ]
    }
  ]
}
```

Supported scope kinds are:

- `repository` — applies to the whole repository; `ids` is omitted;
- `component` — applies to one or more project-defined component IDs;
- `surface` — applies to one or more project-defined surface IDs.

A profile may declare multiple scopes when necessary. Do not duplicate profile entries merely to enumerate IDs of the same applicability kind.

Legacy schema-version-1 profile entries without scope metadata are interpreted as repository-wide during migration unless project evidence shows that a narrower scope was intended.

## Planning discovery and implementation boundary

For the affected milestone scope, planning loads base/shared profiles from the pinned `.guide/system/`, resolves registered `local:<id>` files, reads optional `docs/PROJECT-GUIDANCE.md`, and inspects relevant project engineering/specification/architecture/decision authority. All applicable mandatory and triggered conditional obligations must be accounted for. Objectives guide planning choices; they do not automatically override explicit requirements.

A new project or planning task may author a local profile if an uncovered engineering shape merits a reusable contract; register it explicitly and resolve project authority before a milestone becomes ready. Do not require a central guide release to introduce a local profile.

Ordinary implementation reads ready milestones and referenced normal project authority; it is not required to recompose guide, profile metadata, local profiles, or project meta-objectives. Planning must preserve resulting material implementation obligations and proportionate validation constraints in the handoff; it need not seed an evidence registry.

## Conflict handling

Compatible applicable obligations are combined.

A more specialized profile does not silently override a broader profile. If two applicable profiles impose materially incompatible expectations, planning must resolve the conflict explicitly before implementation proceeds.

The resolution belongs in project-local authority when it affects implementation, validation, packaging, compatibility, or release behavior.

Examples of valid resolution authority include:

```text
docs/ENGINEERING.md
docs/ARCHITECTURE.md
docs/specs/<topic>.md
docs/decisions/<decision>.md
```

Do not encode conflict resolution by reordering `appliedProfiles`.

## Mixed-repository example

A .NET package suite containing ordinary runtime libraries and two Roslyn source-generator packages may use:

```text
base                [repository]
dotnet-library       [repository]
source-generator     [component: generator-packages]
```

The `.NET library` obligations continue to apply to the package suite. Source-generator obligations additionally apply to the generator components. Generator-specific packaging or compiler-host behavior is then resolved in project-local engineering authority.

Concrete target integrations such as EF Core, Power BI, SQL Server, browsers, or named vendor runtimes remain project-local specializations unless a vendor-independent reusable engineering shape justifies a future profile.

A concrete distribution choice such as `dotnet tool`, npm, an OS package, or a standalone archive remains project-local specialization even when the reusable `cli-tool` profile applies.

## Promotion boundary

Concrete technology/runtime behavior belongs in project-local authority unless it has matured into reusable guide-level guidance independent of the originating project.

Create a new profile only when the candidate describes a recognizable reusable engineering shape and materially affects several repository concerns such as documentation, validation, packaging, public surface, or release behavior.
