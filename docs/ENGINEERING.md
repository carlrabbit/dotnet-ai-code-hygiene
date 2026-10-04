# Engineering

## Baseline

Windows 11; .NET 11 SDK line; SDK-default C#; PowerShell.

Required technologies remain `System.CommandLine`, TUnit, Roslyn, BCL SHA-256, and existing Git support where already required.

M0003 adds no AI/model SDK or network dependency.

## Test strategy

Use focused Core tests plus built CLI process tests and isolated SDK-style fixture repositories.

M0003 evidence must cover:

- deterministic sampler reproducibility;
- population/content change recomputation;
- zero population;
- <=5 and >5 populations;
- batch order/handles;
- summary-quality eligibility;
- exact rubric metadata;
- check text/JSON additive output;
- latest-run batch state;
- bare/qualified expansion;
- stale-population rejection;
- full-population expanded output;
- implementer -> frontier reviewer-class transition;
- expansion not mutating latest run;
- no review-history artifact;
- no model/network dependency;
- M0002 regression behavior.

No actual model is required in automated validation.

## Fixture strategy

Use isolated temporary SDK-style repositories. Include fixtures with 0, <5, =5, >5 eligible summaries, mixed missing/existing summaries, target scoping, and source change between check/expand.

## Validation topology

| Depth | Target | Locus |
|---|---|---|
| Tier 1 | Core sampler/review semantics | Windows 11 + .NET 11 |
| Tier 1 | built CLI process | Windows 11 + .NET 11 |
| Tier 3 | isolated SDK-style fixture repos | Windows 11 + .NET 11 + Git |
| Tier 2 | complete repository | `./eng/validate.ps1` |
| Human | semantic workflow usability | project owner/delegate |

`./eng/validate.ps1` remains complete restore/build/test/pack validation.

Do not add AI SDKs, HTTP model clients, orchestration frameworks, review-history databases, or GitHub Actions/workflows.
