# Engineering Command Model

## Core doctrine

```text
When a repository exposes eng/, it defines a stable engineering API; a simple direct command is sufficient when appropriate.
Platform scripts are thin launchers.
Complex engineering semantics live in tested application code.
Platform support is declared and tested.
```

## Implementation levels

### Level 1 — Direct thin scripts

Use for simple restore, build, test, or format commands.

### Level 2 — Shared launcher helpers

Use for modest process orchestration without domain semantics.

### Level 3 — Engineering command host

Use only when project-specific orchestration is genuinely complex enough to justify a separately maintained host (for example sophisticated manifests, artifact generation, or costly resumable suites). The mere possibility of fingerprints or validation receipts is not a reason to build one.

A typical host is:

```text
src/<Project>.Engineering
```

or an existing product CLI with an `engineering` command group.

Do not require a multi-stage validation host, machine-readable plan, receipt store, or per-tool pipeline for an ordinary milestone. Simple monorepo scripts are usually sufficient. Measure repeated validation cost before adding orchestration.

## Launcher model

```text
eng/check.sh ─────┐
                  ├──> shared .NET engineering command
eng/check.ps1 ────┘
```

Launchers must forward arguments and exit codes without duplicating semantics.

Repositories do not need launchers for platforms they do not support.

## Cross-platform and platform-specific support

A launcher existing on a platform does not prove that platform is supported.

Repositories should declare:

- supported launchers;
- tested platforms;
- native dependencies and required installed runtimes;
- which validation targets run on which execution loci/platforms;
- CI matrix coverage when CI is part of the declared validation topology.

Relevant platform behavior includes paths, case sensitivity, executable permissions, line endings, symlinks, file locks, atomic moves, temporary directories, Git behavior, process cancellation, and native dependencies.

A repository may intentionally be Windows-local-first, Linux-first, CI-first, or mixed. Platform support is established by current representative validation evidence, not by the existence of CI alone.
