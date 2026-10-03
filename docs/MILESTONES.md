# Milestones

## M0001 — CLI Foundation

**State:** ready  
**Mode:** AI-executed, human-reviewed

Establish the Windows-first .NET 11 CLI repository skeleton, TUnit testing, `System.CommandLine` command surface, process-level CLI contract, local engineering validation, and packable .NET tool artifact.

Primary milestone:

```text
docs/milestones/M0001-cli-foundation.md
```

## M0002 — Hygiene Vertical Slice

**State:** planned

Prove the core product end-to-end:

```text
target resolution
-> Roslyn analysis
-> deterministic rule order
-> findings/review candidates
-> run/finding identities
-> SHA-256 rule-specific occurrence fingerprints
-> .hygiene/decisions.json
-> ignore/unignore
-> text/JSON result output
```

Initial representative rules are expected to cover:

- German XML summary requirement;
- >200-character review trigger;
- control-flow visual-block separation.

M0002 is not implementation authority until separately planned and marked ready.

## M0003 — Packaged Developer Tool

**State:** planned

Add real deterministic formatting, only justified safe normalization, and consumer-surface validation of the exact current `.NET tool` package installed through `dotnet tool`.

M0003 is not implementation authority until separately planned and marked ready.
