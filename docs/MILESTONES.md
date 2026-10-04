# Milestones

## M0001 — CLI Foundation
**State:** done

## M0002 — Hygiene Vertical Slice
**State:** done

Established project-aware C# targets, deterministic findings/rules, text/JSON output, run/finding identities, explain, configuration, persistent ignores, and stable occurrence matching.

## M0003 — Semantic Review Sampling & Escalation
**State:** done before M0005 execution

Established bounded semantic review batches and explicit frontier expansion/handoff without embedding model invocation.

## M0004 — Deterministic Rewrites & Installed Agent Tool
**State:** prerequisite for M0005

Establishes deterministic `format`/`normalize`, agent-facing CLI help, package version 0.4.0, and installed-tool consumer validation.

Primary milestone:

```text
docs/milestones/M0004-installed-agent-tool.md
```

## M0005 — Supported .NET Profile & Documentation Hygiene
**State:** ready after M0004 completion
**Mode:** AI-executed, human-reviewed

Adds a versioned supported .NET hygiene profile, deterministic bootstrap/update workflows, mandatory profile rules for the .NET analyzer baseline and StyleCop prohibition, and a small deterministic documentation-hygiene slice that treats documentation summaries semantically rather than as `<summary>`-tag-only.

Primary milestone:

```text
docs/milestones/M0005-supported-dotnet-profile.md
```
