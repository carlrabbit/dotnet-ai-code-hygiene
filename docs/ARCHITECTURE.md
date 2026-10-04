# Architecture

## System shape

```text
source targets
├─> rewrite pipeline
│    ├─ format
│    └─ normalize
└─> analysis pipeline
     ├─ deterministic findings
     └─ semantic review batches/handoffs
```

The installed `hygiene` CLI remains the public boundary.

## Rewrite pipeline

```text
resolve complete target set
-> snapshot source
-> load Roslyn context
-> compute complete in-memory rewrite plan
-> validate plan
-> check-only report OR transactional commit
```

The rewrite engine must not expose best-effort per-file mutation.

`format` uses Roslyn formatting and is presentation-only.

`normalize` requires a clean relevant compilation, applies the fixed semantic simplifications in `docs/specs/REWRITES.md`, formats changed documents, verifies resulting compilation, then commits.

Analysis rules and normalization transformations remain separate abstractions. Existing rule configuration does not configure transformations.

Tier-4 validation operates on the installed package boundary: pack -> isolated local source -> `dotnet tool install` -> installed command -> isolated consumer repo.

Deferred: resolve/remediation, model invocation, MCP, IDE integration, portable Agent Skill, broad transformation configuration, cross-language rewrite architecture.
