# Agent Instructions

## Authority

Implementation agents use project-local authority only. Do not treat external guide repositories, planning chats, guide metadata, or research notes as implementation authority.

For the active milestone, start with:

```text
docs/milestones/M0002-hygiene-vertical-slice.md
```

Read only the project-authority documents explicitly required by that milestone.

## Execution

M0002 is AI-executed and human-reviewed. Maintain:

```text
.execution/M0002-hygiene-vertical-slice.md
```

Do not delete, merge, renumber, paraphrase, or replace planner-owned obligation or evidence-case rows.

Implementation owns work packages, concrete implementation choices, evidence, validation results, resume state, and final reconciliation.

## Engineering constraints

- Development and authoritative M0002 validation run on Windows 11.
- Use the .NET 11 SDK line.
- Use `System.CommandLine` for the public CLI surface.
- Use TUnit for automated tests.
- Roslyn is the C# syntax/semantic foundation.
- Do not add GitHub Actions or other repository-hosted workflow files.
- Do not add an embedded AI/model dependency.
- Do not implement deterministic `format` or `normalize` behavior; those remain M0003.
- Do not introduce rule parameters, severity configuration, rule ordering configuration, per-file rule configuration, or inline suppression comments.
- Do not require agents to parse or edit `.hygiene/*.json` directly.

## Completion

Before claiming `COMPLETE`:

1. reread the milestone;
2. verify exact obligation and evidence-case set equality with the ledger;
3. establish concrete evidence for every obligation/evidence case;
4. run every required validation gate on the declared Windows 11 locus;
5. obtain the required human completion review;
6. preserve compact durable completion evidence in the milestone.
