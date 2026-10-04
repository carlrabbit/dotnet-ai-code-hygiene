# Specifications

## Product role

`dotnet-ai-code-hygiene` is a standalone AI-first .NET developer tool whose supported public surface is the installed `hygiene` CLI.

M0004 adds functional deterministic rewrite surfaces and agent guidance while preserving M0002/M0003 check/review behavior.

## M0004 CLI

```text
hygiene format [targets...] [--changed] [--check] [--output text|json]
hygiene normalize [targets...] [--changed] [--check] [--output text|json]
hygiene check ...
hygiene explain ...
hygiene ignore ...
hygiene unignore ...
hygiene ignores ...
hygiene rules ...
hygiene review ...
hygiene help --agent
```

Detailed rewrite behavior: `docs/specs/REWRITES.md`.

Detailed agent help: `docs/specs/AGENT-HELP.md`.

Existing M0002/M0003 check/review/handoff contracts remain authoritative.

## Exit semantics

Existing meanings remain: 0 success (including findings/rewrite-needed status), 1 unexpected internal failure, 2 invalid invocation, 3 invalid/unusable product input/state/target or unsafe rewrite precondition, 4 required dependency unavailable.

`--check` detecting pending rewrite changes is exit `0`.

## Package identity

M0004 package/product version is exactly `0.4.0`. Installed command remains `hygiene`. Installed `hygiene --version` must agree with package metadata.

## Installed package contract

Tier 4 validates the exact locally packed package through `dotnet tool install` from an isolated local package source and invokes the installed command in an isolated consumer repository. Development DLL invocation alone is insufficient.

## Platform/boundary

Windows 11 + .NET 11 remains authoritative. No hosted CI, model API, resolve loop, MCP, IDE integration, portable Agent Skill, or cross-platform support claim is added.
