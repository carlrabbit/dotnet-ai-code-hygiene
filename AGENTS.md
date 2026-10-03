# Agent Instructions

## Authority

Implementation agents use project-local authority only. Do not treat external guide repositories, planning chats, or research notes as implementation authority.

For the active milestone, start with:

```text
docs/milestones/M0001-cli-foundation.md
```

Read only the project-authority documents explicitly required by that milestone.

## Execution

M0001 is AI-executed and human-reviewed. Maintain the planning-seeded execution ledger:

```text
.execution/M0001-cli-foundation.md
```

Do not delete, merge, renumber, paraphrase, or replace planner-owned obligation or evidence-case rows.

Implementation owns work packages, concrete evidence, validation results, resume state, and completion reconciliation.

## Engineering constraints

- Development and authoritative M0001 validation run on Windows 11.
- Use the .NET 11 SDK line.
- Use `System.CommandLine` for the CLI command/parser surface.
- Use TUnit for automated tests.
- Do not add GitHub Actions or other repository-hosted workflow files.
- Keep all required validation locally invokable.
- Do not add an embedded AI/model dependency for M0001.
- Do not implement the M0002 hygiene-engine vertical slice early merely because command names are reserved in M0001.

## Completion

Before claiming `COMPLETE`:

1. reread the milestone;
2. verify exact obligation/evidence-case set equality with the ledger;
3. establish concrete evidence for every obligation;
4. run every required validation gate on the declared Windows 11 locus;
5. preserve the compact durable completion evidence in the milestone.
