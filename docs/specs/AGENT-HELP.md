# Agent Help Contract

M0004 adds:

```text
hygiene help --agent
```

This is a stable CLI-owned guidance surface for coding agents, not provider-specific prompting.

It must concisely describe: product role; repository-scoped execution; target forms and `--changed`; exit meanings; text/JSON output; canonical implementation workflow; `format`; `normalize`; `--check`; enabled rule-owned remediation selection; deterministic finding/explain/ignore flow; semantic review sampling; frontier escalation/handoff; state ownership; and the fact that no model is invoked by the CLI.

The command returns exit `0` and writes guidance to stdout. Root and subcommand `--help` must remain accurate and discoverable.

M0004 does not require a portable Agent Skill, MCP server, IDE integration, or provider-specific instructions.
