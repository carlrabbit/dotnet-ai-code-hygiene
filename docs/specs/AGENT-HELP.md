# Agent Help Contract

M0004 adds:

```text
hygiene help --agent
```

This is a stable CLI-owned guidance surface for coding agents, not provider-specific prompting.

It must concisely describe: product role; repository-scoped execution; target forms and `--changed`; exit meanings; text/JSON output; canonical implementation workflow; `format`; `normalize`; `--check`; enabled rule-owned remediation selection; deterministic finding/explain/ignore flow; statistical semantic-review sampling and zero-due batches; explicit `review accept` as the observation boundary; expansion/handoff as full-population escalation; BORINGness planner escalation; local sampling-state ownership; and the fact that no model/provider is invoked by the CLI.

Guidance must say that acceptance is a caller assertion that all fixed questions pass and no escalation condition applies; check, repeated rendering, expansion, and handoff do not consume tickets. It must direct negative or uncertain summary work to the frontier and BORINGness architectural pressure to the planner.

The command returns exit `0` and writes guidance to stdout. Root and subcommand `--help` must remain accurate and discoverable.

M0004 does not require a portable Agent Skill, MCP server, IDE integration, or provider-specific instructions.
