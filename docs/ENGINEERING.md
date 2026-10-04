# Engineering

## Baseline

Windows 11; .NET 11 SDK line; SDK-default C#; PowerShell.

Required technologies remain `System.CommandLine`, TUnit, Roslyn, BCL SHA-256, and existing Git support where already required.

M0003 adds no AI/model SDK or network dependency.

## Test strategy

Use focused Core tests plus built CLI process tests and isolated SDK-style fixture repositories.

Existing M0003 semantic-sampling evidence remains required.

The handoff amendment additionally requires:

- default `.hygiene/reviews/<handoff-id>/request.json` creation;
- `.hygiene/reviews/` is not matched by repository `.gitignore`;
- deterministic/filesystem-safe handoff identity;
- bare and qualified latest batch handles;
- handoff uses the same stale-population rejection as expansion;
- repository-local PR-friendly destination;
- explicit external destination outside the repository;
- missing parent directory creation;
- existing destination is rejected without overwrite;
- atomic request write;
- request schema/version/kind/source/rule/reviewer/population/questions/items;
- `RI-*` handoff item identity;
- deduplicated embedded full source text for every file containing a handoff item;
- no unrelated repository source embedded;
- request creation does not mutate latest-run state or source;
- no model/network/Git mutation;
- no result/history ingestion.

## Fixture strategy

Use isolated temporary SDK-style repositories.

For handoff tests, use at least two review items in one source file to prove source de-duplication, review items across two files, an unrelated source file that must not be embedded, an external temporary directory outside the fixture repository, a stale population change after `check`, and a pre-existing destination conflict.

## Validation topology

| Depth | Target | Locus |
|---|---|---|
| Tier 1 | Core sampler/review/handoff construction | Windows 11 + .NET 11 |
| Tier 1 | built CLI process | Windows 11 + .NET 11 |
| Tier 3 | isolated SDK-style fixture repos + external temp destination | Windows 11 + .NET 11 + Git |
| Tier 2 | complete repository | `./eng/validate.ps1` |
| Human | semantic workflow + transport usability | project owner/delegate |

`./eng/validate.ps1` remains complete restore/build/test/pack validation.

Do not add AI SDKs, HTTP model clients, orchestration frameworks, review-history databases, transport-provider SDKs, or GitHub Actions/workflows.
