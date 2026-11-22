# Migration — Guide System v0.4.0 to v0.5.0

## Required

None for commands that complete reliably within the agent runtime budget.

## Conditional

Convert aggregate validation suites that may exceed agent runtime limits:

- add `--list`;
- add `--plan-json`;
- add `--shard <id>`;
- add `--verify`;
- write atomic fingerprinted receipts;
- keep no-argument run-all mode for unconstrained local and CI use;
- move receipt and fingerprint logic into tested .NET code when non-trivial.

## Deprecated

- one long process as the only proof of aggregate validation;
- inferring aggregate success from partial output;
- attempting to survive hard harness termination through shell process tricks.

## Manual review

- select shard boundaries;
- select fingerprint scope;
- decide which immutable build outputs or fixtures may be shared;
- decide which suites need resumable execution.
