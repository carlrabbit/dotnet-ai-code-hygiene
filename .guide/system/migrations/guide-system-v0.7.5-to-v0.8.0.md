# Migration — Guide System v0.7.5 to v0.8.0

## Purpose

Adopt the 0.8 validation/specialization/profile-composition/research model without importing concrete guide-system methodology into product repositories.

## Migration required

Recommended for repositories that:

- use Tier 2/Tier 3 validation terminology;
- describe integration validation as PR/CI validation;
- encode CI in their `integration-ready` meaning;
- use concrete project-type guide selection;
- combine multiple engineering shapes in one repository;
- depend on external runtimes/services/platform-specific integration targets;
- use or intend to use integration-first testing;
- retain planning research/evidence under `docs/research/`;
- rely on research/notes as de-facto implementation authority.

For repositories unaffected by those concepts, update guide metadata and review planning templates; no product behavior change is required.

Historical research reconstruction is not required for guide-system compliance. When crossing into 0.8, evaluate it separately as described below.

## Guide-system changes

### Validation depth and execution locus

Replace assumptions such as:

```text
Tier 2 = local
Tier 3 = PR/CI
```

with:

```text
Tier 0 = edit sanity
Tier 1 = focused validation
Tier 2 = standard repository validation
Tier 3 = integration validation
Tier 4 = release/consumer validation
Tier 5 = human review validation
```

For material validation obligations, separately record as applicable:

- target;
- execution locus (`local`, `CI`, `remote`, `mixed`);
- platform/capability requirements;
- concrete invocation;
- evidence.

Do not move authoritative integration validation into CI merely to match the old tier naming.

### Integration readiness

If project documentation defines `integration-ready` as requiring CI, review whether CI is genuinely a project requirement.

The generic 0.8 meaning is that representative integration validation protects important real system boundaries. The required locus is project-specific.

### Integration-bound projects

When correctness depends on an external runtime, service, installed application, real persistence engine, browser, native subsystem, or similar target, ensure project authority resolves the applicable target contract:

- availability;
- locus/platform/capability requirements;
- provisioning/connection;
- isolation/reset/cleanup;
- identity/secrets;
- invocation;
- evidence;
- infrastructure versus product failure semantics;
- fallback validation;
- release/consumer relationship.

Keep concrete technology rules in normal project-local authority.

### Integration-first test strategy

Projects may explicitly choose integration-first testing.

Do not add unit tests solely to satisfy old guide expectations. Retain/add unit tests when isolated validation is materially cheaper, more exhaustive, or more diagnostic.

### Project types and specializations

The central `project-types/` concept is removed.

If a product repository records a selected central project-type guide, migrate the useful generic portions to applicable profiles and the concrete portions to project-local engineering/specification/architecture/decision authority.

For the former `agentic-2d-game-engine` guide specifically:

- keep `dotnet-library` and/or `artifact-first-runtime` profiles where applicable;
- keep concrete engine role, scenario, runtime, and product architecture rules in the engine repository;
- do not create a replacement `game-engine` profile unless a future vendor/domain-independent reusable engineering shape justifies it.

### Profile selection and composition

Review profile sets for feature-tag behavior and implicit mixed-repository assumptions.

Do not introduce profiles merely for:

- Windows/Linux;
- local/CI execution;
- integration-first testing;
- NuGet/GitHub publication;
- one named runtime/service/vendor;
- one concrete project architecture.

Represent those through project engineering policy, validation topology, packaging/release policy, or project-local specialization.

When multiple broad engineering shapes genuinely apply, profiles compose additively.

Profile order has no semantic meaning. Do not introduce `primary`, `secondary`, or last-profile-wins behavior.

For each applied profile, determine whether applicability is:

```text
repository-wide
component-scoped
surface-scoped
```

Use semantic component/surface IDs whose meaning is defined by normal project authority. Do not use path globs in `.guide-profile.json` as the authoritative architecture definition.

Compatible obligations are combined. Material conflicts between applicable profile obligations must be resolved explicitly in project-local authority before implementation.

### Guide-profile schema version 2

Guide-system 0.8 introduces `.guide-profile.json` schema version 2 for scoped profile applicability.

Each applied profile records one or more `scopes` entries using:

- `repository`;
- `component` with project-defined `ids`;
- `surface` with project-defined `ids`.

Migration default for a legacy schema-version-1 profile entry:

```text
no previous applicability metadata
-> repository-wide
```

Use a narrower component/surface scope only when repository evidence shows the profile does not describe the whole repository.

Do not create semantic component/surface identifiers merely to make metadata look detailed. Use them only when they represent a durable boundary already needed by planning/project authority.

### Source-generator profile

0.8 includes a reusable `source-generator` profile for Roslyn source-generator/analyzer-style compile-time surfaces.

It commonly composes with `dotnet-library` when generators are packaged through NuGet.

In a mixed package suite, scope `source-generator` to the applicable generator components/surfaces rather than treating every package as a generator.

Concrete Roslyn/MSBuild compatibility, diagnostic, generated-source, package-layout, and version-alignment rules remain project-local where they differ by project.

### Planning research and durable evidence

0.8 makes research an explicit non-authoritative planning-knowledge layer.

The generic relationship is:

```text
investigation/evidence
  -> durable research when future planning value justifies retention
  -> planning conclusion
  -> project authority when implementation depends on the conclusion
```

`docs/research/` may contain durable evidence such as external documentation findings, experiments, compatibility probes, historical reconstruction, comparisons, uncertainty, and revalidation triggers.

Research is not project authority. If an existing research/notes file contains an operative rule that constrains implementation, validation, compatibility, architecture, public behavior, supported environments, or another durable contract, promote that rule into the appropriate project authority before ordinary implementation depends on it.

Do not preserve all old planning material mechanically. Classify existing research as applicable:

```text
retain        = evidence remains useful to future planning
revalidate    = evidence is relevant but its freshness/assumptions are now doubtful
promote       = implementation-affecting conclusion belongs in project authority
supersede     = old evidence remains useful but is no longer current
remove        = no durable planning value remains
legacy-guide  = copied guide methodology, not project research
```

Copied setup/engineering guides under `docs/research/` remain legacy guide leakage and should not be reclassified as durable project research merely because the directory now has a defined research role.

Do not create `docs/research/` simply to satisfy guide-system 0.8. Activate research only when durable planning evidence exists.

When research volume makes discovery difficult, a repository may maintain a compact non-authoritative `docs/RESEARCH.md` index. Do not duplicate findings or project authority into the index.

Ordinary implementation must remain executable from ready milestones and project authority without loading the research corpus.

### Historical research adoption when crossing into 0.8

Repositories planned before 0.8 may have valuable planning knowledge outside the repository because earlier guide versions did not define a durable research methodology.

Possible surviving material includes:

- old planning chats or exported conversations;
- uploaded documents or project notes;
- experiments, prototypes, benchmarks, or retained evidence;
- issues, pull requests, or historical repository observations;
- external references retained from earlier research.

When migrating from a pre-0.8 guide model into 0.8, explicitly evaluate a research-adoption mode:

```text
none
selective
reconstructive
```

Use the decision surface in `meta/RESEARCH-MODEL.md`, considering evidence availability/quality, rediscovery cost, expected future planning value, project maturity/rate of change, unusual or fragile constraints, revalidation cost, and how much current project authority already captures.

Do not map maturity mechanically to a mode.

`none` is a valid migration result. Missing historical chats/files do not block migration and must not be fabricated.

`selective` recovers a bounded set of high-value historical findings/evidence.

`reconstructive` deliberately establishes a broader useful planning-knowledge baseline from surviving material. It is still curated research, not transcript archival.

Historical conversations/files are source material. Extract durable evidence/findings, assess freshness, and revalidate where material; do not copy them wholesale into `docs/research/`.

Keep these outcomes independent:

```text
guide-system 0.8 migration complete
research adoption complete/partial/not performed
historical evidence available/unavailable
```

A small selective adoption may be included in the guide migration when it does not obscure or delay required migration work.

If reconstructive adoption is substantial, prefer `templates/prompts/special/adopt-research.md` as a separate workflow. The 0.8 guide migration can complete independently unless a specific recovered conclusion is itself necessary to migrate the current project contract correctly.

Research adoption may also be performed later if historical material becomes available after the repository has already moved to 0.8 or beyond.

Do not persist the selected adoption mode in `.guide-profile.json`; it describes a historical planning operation, not a stable project characteristic.

## Product repository changes

As applicable:

1. update `.guide-profile.json` guide-system/base/profile version metadata to `0.8.0`;
2. migrate `.guide-profile.json` to schema version 2 and record semantic profile scopes;
3. update `docs/ENGINEERING.md` validation terminology;
4. record integration-first policy if it is an intentional project rule;
5. record authoritative integration targets and execution loci;
6. add/refine project-local specialization authority for concrete external runtimes/services;
7. define semantic component/surface IDs in normal project authority when scoped profiles require them;
8. resolve material profile conflicts in project-local authority;
9. classify existing `docs/research/` content according to the 0.8 research model;
10. promote any operative rules that currently exist only in research/notes into project authority;
11. retain/revalidate/supersede/remove research according to continuing planning value rather than archival instinct;
12. record the pre-0.8 historical research-adoption decision (`none`, `selective`, or `reconstructive`) in the migration work when the decision is material;
13. perform bounded selective recovery when chosen and appropriate, or route substantial reconstructive recovery to `templates/prompts/special/adopt-research.md`;
14. update milestone templates/current draft milestones to carry validation target/locus requirements;
15. remove project-type guide references;
16. do not copy `meta/PROFILE-MODEL.md`, `meta/SPECIALIZATION-MODEL.md`, `meta/RESEARCH-MODEL.md`, or other guide files into the product repository.

## Mixed package-suite example

A repository containing ordinary .NET packages plus Roslyn generator packages may migrate to:

```text
base                [repository]
dotnet-library       [repository]
source-generator     [component: generator-packages]
```

The project repository defines what `generator-packages` means in normal architecture/engineering authority.

The generator components receive both applicable `.NET library` obligations and source-generator obligations. Neither profile overrides the other.

## Research migration example

A repository may currently contain:

```text
docs/research/runtime-investigation.md
```

If that file establishes both evidence and a project rule such as "integration validation must use runtime X", migrate to:

```text
docs/research/runtime-investigation.md
  -> retain concise evidence/provenance if still useful

docs/ENGINEERING.md or docs/engineering/<topic>.md
  -> state the operative validation target/locus rule
```

A later implementation milestone references the engineering authority, not the research file, unless it separately needs the research as evidence.

A different pre-0.8 repository may have no research directory but have valuable historical planning chats. Its migration may legitimately record:

```text
research adoption: selective
```

and extract only the few expensive-to-rediscover findings worth retaining. Another repository may record `none` and still be fully migrated to 0.8.

## Windows-local / GitHub-publication example

A valid 0.8 project topology is:

```text
Windows developer workstation
  -> implementation
  -> Tier 3 authoritative integration validation against installed runtime
  -> dotnet pack
  -> Tier 4 isolated NuGet consumer validation

GitHub
  -> portable/redundant checks as desired
  -> release/publication automation
  -> NuGet publication
```

The project may choose different topology. This example demonstrates that CI publication does not make CI the required integration-validation locus.

## Manual review

Review:

- any milestone or engineering doc using the phrase `PR integration validation`;
- any project maturity rule that equates integration readiness with CI;
- any central/project-type selection metadata;
- any mixed repository where selected profiles currently appear to apply to every component by accident;
- any profile ordering used as implicit precedence;
- any external-runtime test suite whose authoritative target or fallback behavior is implicit;
- any test policy that creates unit tests only because of a generic test-pyramid assumption;
- any `docs/research/` file used as de-facto implementation authority;
- any copied guide document under `docs/research/` that could be mistaken for project research;
- any large research corpus that ordinary planning currently loads indiscriminately;
- whether surviving pre-0.8 planning material contains costly reusable evidence worth selectively or reconstructively adopting.

## Completion

Migration is complete when project-local authority contains the concrete rules implementation needs, profile selection remains broad/reusable with explicit applicability where necessary, profile conflicts are resolved explicitly, validation obligations no longer infer execution location from validation depth, retained research is clearly non-authoritative and useful to future planning, and ordinary implementation does not depend on planning research to recover project rules.

Historical research reconstruction is not part of the required completion condition unless the migration explicitly chose a bounded adoption as required work. A repository may complete the 0.8 migration with `research adoption: none`, or with substantial reconstructive adoption deferred to the dedicated workflow.
