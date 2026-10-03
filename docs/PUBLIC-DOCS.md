# Public Documentation

The repository is public and the CLI is intended as an externally consumable developer tool.

## Current public surface

The root `README.md` remains the primary public documentation surface during M0002.

Before M0002 completes, it must not claim that the hygiene engine is already available.

At M0002 completion, README documentation must include representative source-project invocations for:

```text
hygiene check
hygiene check <file>
hygiene check --changed
hygiene rules
hygiene explain <finding>
hygiene ignore <finding>
hygiene ignores
hygiene unignore <ignore-id>
```

It must also state:

- findings do not imply non-zero exit;
- `--output json` is the machine-readable automation surface;
- `.hygiene/config.json` and `.hygiene/decisions.json` are CLI-owned state;
- M0002 remains Windows-first;
- `format` and `normalize` are not implemented until M0003.

## Installation

Installed `.NET tool` usage and installation guidance remains deferred until M0003 proves the exact installed consumer surface.

Do not document `dotnet tool install` as validated product behavior during M0002.

## Dedicated public docs

Do not create a `public-docs/` tree until enough public material justifies it.
