# Milestones

## M0001 — CLI Foundation

**State:** done
**Mode:** AI-executed, human-reviewed

Established the Windows-first .NET 11 CLI repository skeleton, TUnit testing, `System.CommandLine` command surface, process-level CLI contract, local engineering validation, and packable .NET tool artifact.

Primary milestone:

```text
docs/milestones/M0001-cli-foundation.md
```

## M0002 — Hygiene Vertical Slice

**State:** done
**Mode:** AI-executed, human-reviewed

Implement the first complete useful hygiene loop:

```text
target resolution
-> Roslyn project/source context
-> deterministic ordered rules
-> findings/review candidates
-> run/finding handles
-> text/JSON output
-> explain
-> persisted ignore decision
-> later suppression
-> stale detection
-> unignore
```

Initial rules:

```text
docs.summary.required
readability.long-line.review
readability.control-flow.visual-block
```

Primary milestone:

```text
docs/milestones/M0002-hygiene-vertical-slice.md
```

## M0003 — Packaged Developer Tool

**State:** planned

Add real deterministic formatting, only justified safe normalization, and consumer-surface validation of the exact current `.NET tool` package installed through `dotnet tool`.

M0003 is not implementation authority until separately planned and marked ready.
