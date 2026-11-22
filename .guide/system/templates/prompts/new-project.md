# Prompt — Prepare a Completely Empty New Project

Our planning task is to create a repository initialization package for a completely empty new project:

- Project name: `{project-name}`
- Broad guide profile(s): `{profiles}`
- Repository role: `{repository-role}`
- Maturity stage: `{maturity-stage}`
- Public/private status: `{public-private-status}`
- Primary stack: `{primary-stack}`
- Concrete runtime/environment/integration constraints: `{project-specialization-constraints}`

Shared guide authority is **installed locally**: bootstrap the complete, exact upstream tagged snapshot at `.guide/system/`, and record its tag plus peeled commit SHA in `.guide-profile.json`. Before a new repository exists, use the upstream guide to initialize it; subsequent planning uses `.guide/system/`. Do not edit the snapshot for local customization, scatter other copied guide documents into project docs, or require routine implementers to read it. See `meta/LOCAL-GUIDE-SNAPSHOT-MODEL.md`.

Target repository documentation must contain project truth only, plus optional non-authoritative planning research when durable evidence genuinely warrants retention.

Shared profiles describe broad reusable engineering shapes. Projects may also register `local:<id>` profiles under `.guide/profiles/<id>/PROFILE.md` for distinct project-owned engineering shapes with multiple coordinated obligations. A local profile may be authored during initialization without first adding it to the central guide. Do not invent a central-style project type or feature-tag profile for operating system, local/CI execution, integration-first testing, a packaging mechanism, a named external product/service, or one concrete product architecture.

When the product exposes a supported command-line executable as a first-class user or automation interface, consider the `cli-tool` profile. A concrete `.NET tool` distribution is not itself a separate profile: use `cli-tool` plus project-local .NET/dotnet-tool specialization. Add `dotnet-library` only when a meaningful reusable library/API surface also exists.

When those details materially affect implementation or validation, record them as project-local engineering/specification/architecture/decision authority.

## Profile composition and applicability

A project may combine a small number of profiles when multiple broad engineering shapes genuinely apply.

For every selected profile, determine whether it applies:

- repository-wide;
- to one or more durable project-defined components;
- to one or more durable project-defined surfaces.

Profiles compose additively. Do not assign primary/secondary precedence and do not rely on profile ordering.

If two applicable profiles impose materially incompatible expectations, resolve the conflict into project-local authority before producing an implementation-ready milestone.

When `.guide-profile.json` is created for guide-system 0.8 or later, use schema version 2 and record semantic `repository`, `component`, or `surface` scopes for each applied profile. Keep unprefixed IDs for central profiles. A registered local profile uses `local:<id>` and resolves to `.guide/profiles/<id>/PROFILE.md`; its version is project-owned.

Component/surface IDs are project-defined semantic identifiers. Their meaning must be established by normal project authority; do not use path globs in `.guide-profile.json` as the architecture definition.

If the repository is homogeneous, repository-wide scopes are sufficient. Do not invent component/surface IDs without a real planning need.

If the target repository contains old copied setup or engineering guides, treat them as legacy/non-authoritative unless the repository explicitly marks them as active project documentation.

If the target repository contains `.guide-profile.json`, use it as guide-selection metadata. Ordinary implementation agents must not be required to read `.guide-profile.json`.

If the target repository contains `.guide-sync/`, treat it as deferred documentation synchronization metadata. Ordinary implementation agents must not be required to read `.guide-sync/`.

Resolve the desired **stable, tagged** guide-system version from `carlrabbit/agentic-project-guides` before creating files. Resolve its peeled commit SHA; do not use unpinned upstream `main` or a historical draft snapshot as an implicit default.

## Goal

Create a repository-relative initialization package including the full upstream guide snapshot at `.guide/system/` and project-owned files. Follow `meta/HANDOFF-TRANSPORT-MODEL.md`: an authorized Git initialization package **may itself be the first root planning commit** in a truly empty repository; it does not need a placeholder initial commit. Otherwise use an authorized existing-branch Git commit or a ZIP overlay when Git delivery is unavailable/inappropriate or explicitly requested. Identify the exact accepted planning commit in any Git implementation handoff (not just a branch or PR). Install the snapshot with Git subtree (`--squash`) or an exact tracked tree copy when subtree is unavailable; do not filter upstream files or create a custom distribution.

Usually include:

```text
README.md
AGENTS.md
.guide-profile.json
.guide/system/                # full pinned upstream guide tree
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/milestones/M0001-initialization.md
.execution/M0001-initialization.md   # planner-seeded obligations for substantive first milestone
```

Add optional `docs/PROJECT-GUIDANCE.md` only when actual project meta objectives or planning-methodology clarifications warrant it; do not add it as a mandatory placeholder. Add local profile definitions only for genuinely selected `local:<id>` entries, and capture operative consequences in normal project authority.

Create root `AGENTS.md` as a **small repository entrypoint**, not as a copy of the generic Implementer prompt. Route planning, guide updates and documentation synchronization through `.guide/system/README.md`; route implementation to the ready milestone, its planner-seeded obligation ledger for substantive work, referenced project authority, and needed code/tests. Keep implementation specifics and validation policy in `docs/ENGINEERING.md`, other operative documents, or the ready milestone; do not require reading project planning metadata or the external guide for ordinary implementation. Check that entrypoint instructions do not conflict with the generated milestone package. Do not mechanically insert detailed guide methodology into `AGENTS.md`.

Add specs, architecture docs, decisions, public docs, or focused engineering/specialization docs only when required for the selected profiles, profile scopes, project constraints, and maturity stage. Create `.guide-sync/pending/` only when there are genuine deferred documentation hints; do not scaffold an empty coordination folder.

Do not include unmanaged copied guide documents outside the intentional `.guide/system/` snapshot.

## Research initialization

Do not create `docs/RESEARCH.md`, `docs/research/`, placeholder research files, or copied research templates merely because the guide system defines a research model.

During project initialization, use `meta/RESEARCH-MODEL.md` when bootstrap planning itself requires non-trivial investigation.

Persist research only when the evidence would materially help future planning and would be expensive, unreliable, or wasteful to rediscover. Typical bootstrap examples may include external runtime feasibility, compatibility probes, or evidence behind an architectural choice.

If research produces a conclusion required by the initialization milestone or later implementation, promote that conclusion into normal project authority before handoff. The implementation agent must not need research to reconstruct the operative rule.

Create a compact `docs/RESEARCH.md` index only when enough durable research exists to make discovery useful. Do not create an empty index as scaffolding.

### Prior/pre-project research adoption

A new repository may still have substantial planning history that predates repository creation, for example previous chats, uploaded documents, prototypes, experiments, or comparison work.

When such material is actually available to planning, evaluate whether it contains durable evidence worth adopting into the new repository.

Use the research-adoption modes from `meta/RESEARCH-MODEL.md`:

```text
none
selective
reconstructive
```

Do not assume that a new repository should start with reconstructive research. Choose the mode from expected future planning value, evidence quality/availability, rediscovery cost, project maturity/expected change, and revalidation cost.

Historical/pre-project chats and files are source material. Do not copy them wholesale into `docs/research/`; extract bounded durable findings/evidence, preserve provenance/uncertainty, revalidate where materially necessary, and promote operative initial project decisions into normal authority.

If substantial reconstructive adoption would dominate project initialization, prefer `templates/prompts/special/adopt-research.md` as a separate planning/research workflow. Repository initialization must not become blocked merely because historical research is incomplete or unavailable.

Do not persist the adoption mode in `.guide-profile.json`.

## Validation and specialization

If correctness depends on an SDK/compiler toolchain, external runtime, installed application, service, real database, browser, native subsystem, or other concrete integration target, initialize enough project authority to make its validation contract explicit before implementation depends on it.

Resolve as applicable:

- target;
- execution locus;
- platform/capability requirements;
- provisioning/connection, including whether prerequisites are expected to be preinstalled;
- executor permission to install tools or change persistent machine configuration;
- permission to create/modify CI/CD workflows or other shared infrastructure;
- isolation/reset;
- invocation;
- evidence;
- fallback/failure semantics, including accepted alternative loci and evidence equivalence;
- consumer/release relationship.

During bootstrap, explicitly check which toolchain/runtime capabilities the first milestone needs and whether a missing prerequisite can be provisioned in an authorized disposable/user-scoped environment, must be supplied by the environment owner, or should result in `BLOCKED`. Do not silently substitute GitHub Actions or another execution location for local validation. An absent prerequisite is compatible with a ready milestone when authority, evidence location, and the blocked path are settled; it is not a license for infrastructure creation.

If the project intentionally uses integration-first testing, record that policy in project engineering authority. Do not create unit-test requirements merely because of an assumed generic test pyramid.

Where expensive validation or investigation is expected, initialize only justified project authority: cheap feedback commands, required acceptance gates, separate optional experiment entry points, and safe stage freshness/reuse rules. Optional project guidance may prioritize economical iteration; implementation-facing commands and criteria belong in engineering authority or the milestone. Do not create simulation/efficient-testing profiles, a mandatory caching framework, or canonical regression entries for exploratory workloads by default.

## Obligation completeness

For a substantive initialization milestone, seed `.execution/<milestone-id>.md` with stable IDs for all material deliverables before `ready`. The implementer tracks and reconciles these obligations; no evidence cases, per-criterion test mapping, receipt system, or validation-gate registry is required. Only a genuinely trivial indivisible initialization task may omit the ledger with an explicit rationale.

## Planning/implementation separation

There is no direct synchronization between the planning AI and implementation AI.

Create an implementation-ready package. The later implementation agent must be able to apply the package, open the primary milestone document, read only the authority documents explicitly listed in that milestone, implement the focus areas, run the specified validation against the declared targets/loci, and finish without reconstructing planning context, reading the external guide repository, or depending on non-authoritative research for project rules.

Also generate a concise execution prompt in chat for the later implementation agent.

## Deliverable

Deliver the permitted repository-relative initialization files by an authorized Git planning commit or ZIP overlay following `meta/HANDOFF-TRANSPORT-MODEL.md`. For Git, supply the repository, branch, **exact accepted planning-baseline commit SHA**, milestone path, and whether implementation will continue on that branch or branch from that commit. For ZIP, preserve target paths and identify the intended base when known, plus any needed application instructions.

Do not include implementation source files, generated code, broad unrelated documentation cleanup, TBPs, issue templates, **unmanaged copied guide documents outside `.guide/system/`**, or research scaffolding without durable evidence.

After preparing the package, respond with:

1. branch\/PR link or download link, as applicable;
2. included file list;
3. reason each file is included;
4. confirmed or inferred profiles and applicability scopes, role, maturity, execution mode, and material specialization assumptions;
5. research-adoption mode when prior/pre-project material was available, durable research created during initialization, and which conclusions were promoted into project authority;
6. a filled execution prompt for the later implementation agent;
7. documentation-sync hints and the `.guide-sync/pending/` files created.
