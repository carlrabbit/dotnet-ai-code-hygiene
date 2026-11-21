# Research — .NET AI Workbench Direction

Status: current (planning intent; not implementation authority)

Last verified: 2026-11-21 (repository baseline and external SQLite documentation)

Related authority:

- [Project Guidance](../PROJECT-GUIDANCE.md) — current pre-1.0 planning and semantic-rule evaluation policy.
- [Architecture](../ARCHITECTURE.md) — current implemented hygiene architecture; **not yet a workbench architecture**.
- [Product Direction](PRODUCT-DIRECTION.md) and [Human Consumability](HUMAN-CONSUMABILITY.md) — earlier motivations and design pressures.
- [Verifiable Review Work](VERIFIABLE-REVIEW-WORK.md) — deterministic review-work versus external semantic judgment.

## Question

Should the existing .NET code-hygiene product evolve into a local, agent-oriented
.NET AI workbench, and how can deterministic formatting, contextual review, and
shared project intelligence evolve independently without premature framework or
process infrastructure?

## Context

Agent-authored .NET projects need more than an accumulating set of quality
checks. The recurring costs are avoidable representational variation, expensive
context reconstruction, finding the correct source/relationships, estimating
the impact of changes, and spending agent judgment on work that can be done
mechanically.

The existing product mixes SDK-based formatting/normalization and bounded
semantic-review work behind one hygiene entry point. The owner considered
separating Autoformat and Autoreview into different repositories, then
recognized that both may use a future locally indexed workspace model.
Independent execution and development are useful; independent repository
histories and incompatible workspace models are not necessary for those goals.

This research records **intended direction**, not an implemented migration,
ready milestone, new CLI contract, or commitment to build a universal index.

## Scope

In scope:

- Product identity, monorepo shape, executable boundaries and dependency direction.
- Ownership of automatic formatting versus semantic review and collected corrections.
- A local SQLite-backed workspace index with concurrent process access.
- The possible later transition to a resident index service using IPC or loopback HTTP.
- Data authority, freshness, rebuilding, concurrency, and migration risks.
- Work that future planning must promote into executable project authority.

Out of scope:

- Changing the current repository name, package/version identities, CLI behavior,
  supported platform, executable layout, rule IDs, or published contracts now.
- Designing a plugin framework, cross-tool orchestration service, or full schema/API.
- Implementing a resident server, remote/cloud index, editor integration, or model provider.
- Treating a speculative workspace query or candidate canonicalization as already supported.

## Evidence and provenance

| Evidence / provenance | Relevant version / date | What it establishes |
|---|---|---|
| Product-owner planning discussion | 2026-11-21 | **Intent:** one umbrella workbench/monorepo, separate real executables, shared local SQLite index, possible later IPC/HTTP service, no plugin framework. |
| `carlrabbit/dotnet-ai-code-hygiene` main, commit [`97ee3e1`](https://github.com/carlrabbit/dotnet-ai-code-hygiene/commit/97ee3e13a584f2e9f914120666a77b38a565becc) | 2026-11-21 | **Observed:** merged M0025 portfolio, existing formatting and review machinery, and existing architecture/specification authority. |
| [Project Guidance](../PROJECT-GUIDANCE.md) and [Semantic Rule Portfolio Strategy](SEMANTIC-RULE-PORTFOLIO-STRATEGY.md) | Repository baseline above | **Observed:** pre-1.0 freedom to replace experimental contracts; broad agent-implemented portfolios; deterministic software tests rather than fabricated model-verdict tests. |
| [SQLite WAL](https://www.sqlite.org/wal.html) | Checked 2026-11-21 | **Observed external contract:** concurrent readers and a writer across local processes; only one writer at once; WAL/checkpoint and same-host filesystem constraints. |
| [Microsoft.Data.Sqlite database errors](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors) | Checked 2026-11-21 | **Observed external guidance:** separate connections and pooling for concurrent consumers; lock/busy retries bounded by command timeout. |
| [Agentic Project Guides research model](https://github.com/carlrabbit/agentic-project-guides/blob/main/meta/RESEARCH-MODEL.md) | Guide profile 0.9.3 | **Observed:** research preserves context but does not supersede operative architecture/specs/milestones. |

These observations establish feasible design directions, **not** measured latency,
contention, format quality, or index usefulness in this repository.

## Findings and intended direction

### 1. The workbench is a family of executable tools, not a plugin host

**Planning intent:** retain one Git repository (possibly renamed to
`dotnet-ai-workbench` in a later explicitly planned step) with independently
developed and invocable .NET executables. Early conceptual capabilities are:

| Capability | Primary responsibility | Typical result |
|---|---|---|
| Autoformat | Opinionated, deterministic normalization of supported source and project representations | Source/configuration changes or read-only drift report |
| Autoreview | Select and explain bounded context-sensitive review work, including possible agent corrections | Review obligations, evidence and correction handoffs |
| Workspace query / navigation | Fast project, symbol, relationship and change-scope lookup | Structured facts and source locations |
| Index maintenance | Build, refresh, inspect and recover local derived project data | Rebuildable database state and freshness information |

This is a capability split, **not** an executable per rule or a requirement that
all four binaries exist immediately. Implementation may use shared .NET libraries
and focused test projects. One executable must not require another capability's
process to run, except for an explicitly introduced future optional service mode.

There is no initial plugin SDK, provider abstraction for arbitrary tools, generic
rule interface spanning format and review, reflection discovery system, or
mandatory central host. The ordinary calling agent can compose executables.

The existing `dotnet hygiene` CLI and source layout remain operative until a
future milestone explicitly replaces them.

### 2. Aggressively remove representational freedom where safe

**Planning intent:** favor a consistent, conventional, sufficiently good form
over context-specific aesthetic optimization. The owner described the heuristic
as approximately **80% clearly appropriate, 19% consistent even if not locally
optimal, 1% exceptional fallout**. These are priorities, **not measured
accuracy rates, formal thresholds, or authorization to change behavior**.

Autoformat should own supported mechanical policies such as:

- SDK/Roslyn-based C# whitespace, indentation, obvious syntax normalization,
  and stable placement of visually meaningful blank lines.
- Canonical XML documentation *presentation*: indentation, line wrapping,
  whitespace, element layout and repeatable handling of adjacent tags.
- Consistent comment presentation where syntax/trivia preservation is safe.
- Supported structural conventions such as standard Razor Pages placement under
  `Pages/`, naming/companions and project layout, when the resulting changes can
  preserve source and runtime semantics.

A deterministic canonicalizer is useful only when its output is reproducible,
idempotent and safe. Preserving compilability is necessary but may not be
sufficient for project moves: routing, generated paths, references, resources
and other runtime conventions matter. When safety cannot be established,
report a precise obstacle rather than silently moving or rewriting content.

This scope is *candidate future behavior*. The currently implemented SDK
formatter and profile are not claimed to canonicalize all of it.

### 3. Keep semantic content decisions out of visual formatting

**Planning intent:** Autoformat owns *representation*, not speculative
meaning. Autoreview owns *judgment* and can emit collected, actionable work for
an external agent.

Examples:

- Mechanical XML `<summary>` wrapping belongs to Autoformat.
- Deciding whether a summary communicates the behavior accurately belongs to
  Autoreview.
- Removing a `<param>` description that merely repeats the signature may be
  an agent correction; information about contracts, valid ranges, exceptions
  or non-obvious behavior must not be removed merely to improve visual uniformity.
- A conventional folder layout may be automatically enforced for supported
  new files; migrating existing Razor Pages is contextual when routing or
  references could change.

A useful workflow is:

```text
agent edits -> autoformat -> autoreview -> external agent corrections
           -> autoformat -> validation
```

The workflow is an **illustration**, not an orchestrator or a guarantee of
termination. Automatable concerns should progressively leave expensive semantic
review populations, while legitimate exceptions stay explicit and bounded.

### 4. A local, multi-process SQLite index is the initial shared intelligence

**Planning intent:** the workbench may persist derived workspace facts in a
local SQLite database, accessed concurrently by distinct executables through
shared .NET workspace/index libraries. Potential queries include:

- Solutions, projects, documents and project-reference relationships.
- Symbol declarations, implementations and reference locations.
- Source-to-test and other relationships where mechanically defensible.
- Changed-file scope, dependent projects and possible contract-change impact.
- Diagnostic/source-context lookup for bounded agent handoffs.

**Observed constraint:** WAL mode supports simultaneous read transactions
alongside writes on the same host, but SQLite permits only one active writer.
Short transactions, writer ownership, retries/timeouts and checkpoint behavior
need design and concurrency testing. A virtual/cloud-synced drive or network
share is not presumed safe for the index database.

**Unverified premise:** sufficiently useful workspace queries will benefit from
a persisted index more than repeated Roslyn/project loading. This needs
workload-driven assessment; no blanket indexing requirement follows.

The database should remain a *derived, rebuildable cache*. Source files, project
files, Git and compiler/build state remain authoritative. Symbol/index identity
and freshness cannot be inferred safely from a stale cache without an explicit
policy. Every consumer must distinguish a last-completed snapshot from a query
requiring updated source state; incomplete or failed indexing must be visible.

Long-lived review acceptance, suppression or judgment records are a different
data class from reconstructible workspace facts. They require independent
ownership, versioning and recovery even if a future implementation chooses the
same physical SQLite file.

Shared libraries should expose **workspace concepts and useful queries**, not
let each executable depend on raw SQL tables. No finalized `IWorkspaceIndex`
contract, table schema, schema migrator or database path is chosen here.

### 5. Service mode is a future optimization, not today's dependency

**Planning intent:** local embedded database access is sufficient for an initial
design. A later optional resident host might preserve expensive in-memory
Roslyn/project state, coordinate updates, notify clients of freshness, and
expose selected capabilities through IPC or loopback HTTP.

Such a host must not be treated as a prerequisite for Autoformat or ordinary
read-only querying without demonstrated benefit. The choice between Windows
named pipes, cross-platform IPC, HTTP, or other transports remains open.
If a loopback server is introduced, its file/source exposure, origin/authentication
and process lifecycle must be explicitly designed.

The host would share underlying workspace/query **semantics**, not necessarily
a byte-for-byte .NET interface. Protocol compatibility and service discovery
should only be designed when an actual consumer needs them.

### 6. Independence is enforced through dependency direction

The proposed direction is:

```text
Autoformat executable ───────┐
Autoreview executable ───────┼──> workspace/index capabilities ──> local SQLite
Query executable ────────────┘             |
                                         source, Git, Roslyn
```

Autoformat and Autoreview should not depend on each other's engines,
configurations, release cadence or mandatory process startup. Shared code is
extracted for demonstrated common needs (project opening, source locations,
querying, diagnostics), not preemptively for every possible rule.

Some formatting operations may require only the SDK's existing project loading,
and should **not require the database** without a concrete advantage.

Independent executable packaging and versioning are plausible, but repository
name, release strategy, migration shape and command names are deferred to
project planning.

## Uncertainty and alternatives

| Decision surface | Current preference | What remains unresolved |
|---|---|---|
| Monorepo vs separate GitHub repositories | Monorepo / umbrella project | Future name and exact migration timing |
| Independent executables vs monolithic CLI | Separate executables by capability | Concrete commands, number of binaries and packages |
| Shared SQLite library vs mandatory service | Embedded local access first | Measured query cost and persistence benefit |
| IPC vs loopback HTTP | Optional later | Transport, security, platform compatibility, latency |
| Index schema and writer coordination | Derived, disposable index with controlled writes | Snapshot keys, incremental invalidation, multiple-writer policy, checkpoint and migrations |
| Workbench index vs review state | Distinct lifecycles | Same file or separate stores; durability/backup policy |
| Autoformat coverage | Opinionated supported conventions | Precise canonical forms, exception syntax, safe structural migration boundaries |
| Review/correction interface | Bounded external-agent work | Whether collection is a dedicated command, output mode or future companion executable |
| Integration and distribution | One repo, independent tools | Versions, install/update boundaries, transition from `dotnet hygiene` |

**Alternatives considered and deprioritized:**

1. Split Autoformat and Autoreview into separate GitHub repos: gives isolation,
   but prematurely fragments prospective shared project intelligence and planning.
2. Keep everything behind one expandable hygiene CLI: preserves current code,
   but mixes distinct mutation, review and navigation operating models.
3. Build a workbench plugin platform first: introduces discovery/lifecycle
   complexity without a demonstrated need.
4. Require a daemon for all commands: adds process supervision and failure
   modes when local SQLite already supports concurrent access.
5. Make every operation database-dependent: duplicates existing SDK/Roslyn
   capabilities and creates avoidable index-freshness failure paths.

## Implications for future planning

A future planning pass should decide how to promote this direction into
`docs/ARCHITECTURE.md`, concrete specifications and executable milestones.
Candidate sequencing, not an approved roadmap:

1. Establish workbench module/executable boundaries and an explicit migration
   from the current hygiene contracts; preserve data safely despite pre-1.0
   compatibility freedom.
2. Expand Autoformat as a coherent canonicalization portfolio, independently
   of the database; mechanically verify idempotence, fidelity and safety.
3. Build the minimum workspace index against specific agent-facing queries,
   validating concurrent readers/writes, freshness, recovery and actual benefit.
4. Let Autoreview use indexed context where doing so reduces duplicated
   analysis and costly agent handoffs.
5. Consider a resident index host only after concrete hot-query or external
   integration demand exists.

Prior implementation of M0025 remains valid baseline evidence; nothing here
retroactively changes its accepted behavior or testing obligations.

Useful evaluation surfaces for later milestones include: canonicalization diff
stability/idempotence, behavioral preservation for structural moves, indexed
cold/hot query cost versus direct Roslyn lookup, incremental refresh accuracy,
write contention and crash recovery, and reduction in repeated context
assembly. Do not invent numeric acceptance thresholds before corresponding
workloads and contracts are selected.

## Revalidation triggers

Reassess the relevant findings when:

- A ready workbench migration milestone defines a different executable
  boundary or project identity.
- SQLite or the selected .NET data provider changes materially, or the index
  moves off a local same-host filesystem.
- Real query workloads show indexing cost, locking or staleness unlike these
  assumptions.
- A supported structural convention changes (for example ASP.NET Core
  project or Razor Pages routing semantics).
- A resident service or cross-platform use becomes an actual requirement.

## Authority promotion

| Conclusion / prospective contract | Intended authority | Status |
|---|---|---|
| Workbench product identity, repository and executable decomposition | `docs/ARCHITECTURE.md` and appropriate project decision | Pending future planning |
| Autoformat conventions, exception and transformation safety | `docs/specs/` and ready milestone | Pending |
| Autoreview boundary, budgets and agent correction handoff | `docs/specs/` and ready milestone | Pending |
| Local index schema, freshness, concurrency, source-of-truth and lifecycle | `docs/ARCHITECTURE.md`, `docs/specs/`, ready milestone | Pending |
| Tool packaging, platform, migration and validation gates | Engineering/release authority and ready milestone | Pending |
| Optional future service/IPC/HTTP protocol | Future architecture/specification if justified | Not yet applicable |

**No operative project authority is changed by this research entry.** Normal
implementers continue to follow the currently ready milestone and project
architecture/specifications until planning explicitly promotes the relevant
conclusions.

## Retention

The workbench pivot explains why two apparently separable hygiene engines are
kept together under a possible shared workspace intelligence layer, why a
local database is preferred over a mandatory host, and why no plugin platform
is planned. Preserve it until the architecture and migration decisions are
promoted and this reasoning becomes cheap to reconstruct from authority and
Git history.
