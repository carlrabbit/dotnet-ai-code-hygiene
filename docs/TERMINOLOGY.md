# Terminology

**Formatter** — deterministic presentation-only source transformation.

**Normalizer** — deterministic semantics-preserving structural transformation using the fixed engine-defined M0004 catalogue.

**Rewrite plan** — complete in-memory description of every selected file change before repository mutation.

**Rewrite transaction** — all-or-nothing application of a validated rewrite plan across the complete selected target set.

**Rewrite check mode** — non-mutating `--check` evaluation that computes the same rewrite plan and reports pending changes.

**Installed-tool validation** — validation against `dotnet tool install` from the exact local package, not project-output DLLs.

**Agent help** — CLI-owned coding-agent guidance for workflow, targeting, outputs/exits, rewrites, deterministic findings, and semantic-review escalation/handoff.
