# Constrained Execution Model

## Status

Optional advanced guidance for materially expensive or externally constrained validation. It is not a milestone default and creates no universal infrastructure obligation.

## First choose the simplest execution

1. Use direct build/test/format or a simple `eng/` script for ordinary validation.
2. Split an expensive suite into independently executable stages only if the expected reduction in repeated work exceeds the cost of orchestration.
3. Add resumability when long runs, retries, interruption, or unavailable target windows materially justify it.
4. Add machine-readable plans, receipts, and dependency fingerprints only when the project can identify reliable inputs and a real need for cross-run reuse.

A repository or monorepo may use `eng/validate.ps1`, `eng/autoformat/validate.ps1`, and similar scripts with plain build/test commands. Independent ownership of tools does not imply independent validation machinery.

## Optional staged validation contract

When a project explicitly chooses a resumable model, project engineering authority defines:

- stage names and direct commands;
- input, output, toolchain and environment dependencies;
- isolation, cleanup and time/resource bounds;
- permitted parallelism and retry policy;
- what counts as valid reusable evidence;
- a fast way to tell whether required stages passed for the current input.

A successful stage may produce an atomic receipt with a scope/fingerprint. Cached success is valid only while all material inputs are covered and unchanged; if dependency coverage is incomplete, rerun conservatively. The verifier must reject missing, failed, incompatible, or stale evidence. Partial output and mere receipt existence cannot establish a required gate.

The receipt schema, location, `--plan-json` interface, CLI host, and aggregate verifier are **not prescribed** by the generic guide. When adopted, they are project-local engineering contracts.

Do not confuse a human's approval of milestone evidence with reusable machine execution evidence. Completed human review remains historical.

## Implementation behavior

Run focused feedback while editing. Perform complete *required* verification near the actual completion or release boundary, reusing current valid evidence where possible. An expensive suite does not need a full rerun after every change when unaffected stages have trustworthy evidence.

Avoid constructing a general validation platform to solve one inexpensive milestone. Prefer deleting unnecessary receipt or staging requirements during review to carrying them forward as sunk cost. New validation infrastructure requires a stated cost/risk justification.
