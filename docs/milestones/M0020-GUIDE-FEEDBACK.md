# Agentic Project Guide — Validation Efficiency Feedback

Context: M0018 `dotnet-ai-code-hygiene` took around two hours because a monolithic canonical script reran Core (7–11 min), CLI (5–6 min), and expensive installed-tool consumer checks multiple times. These durations are implementer reported; no independent timing samples exist yet.

## What guide 0.9.1 already gets right

- `meta/MILESTONE-EXECUTION-MODEL.md` already states: implement work package → focused validation → update ledger → repeat → final validation.
- `meta/CONSTRAINED-EXECUTION-MODEL.md` already prescribes bounded independent shards, `--plan-json`, atomic fingerprinted receipts and fast `--verify`.
- ADR-0014, ADR-0015, ADR-0018 and `meta/VALIDATION-MODEL.md` already support resumption and correct evidence provenance.
- CLI profile explicitly distinguishes focused tests from installed-consumer package validation.

## Improvement opportunities in the guide repository (separate follow-up, not an M0020 code change)

1. **More explicit cadence:** state that a complete cold validation is a completion/provenance gate, not the default edit-correct-edit loop. On failure, run the smallest affected shard first; final aggregate verifier checks all current required receipts.
2. **Sharding beyond timeouts:** encourage resumable stages whenever expense/repetition is material, even when no hard harness deadline exists. Include examples where one late Tier 4 failure should not force all Tier 1/2 tests to repeat.
3. **Concrete reuse rules:** supply stage dependency and input-fingerprint examples for safe reuse, including changed source, validation script, packages, toolchain and stale output identity. Distinguish immutable fixture cache from evidence that a changed current binary passed.
4. **Cost observability:** require or recommend lightweight per-stage timing, fixture/process overhead reports, and measurement-first optimization for expensive suites; avoid hardcoded test-time targets.
5. **Planning topology discipline:** use expensive consumer acceptance to validate actual packaging/installation boundaries; use cheaper isolated tests for detailed permutations unless a distinct public boundary truly requires consumer-level evidence.
6. **Guide examples:** add a PowerShell/.NET local-stage exemplar alongside POSIX examples so implementers can adopt sharding without rewriting their validation architecture.

Candidate guide files: `meta/VALIDATION-MODEL.md`, `meta/MILESTONE-EXECUTION-MODEL.md`, `meta/CONSTRAINED-EXECUTION-MODEL.md`, `templates/prompts/execute-milestone.md`, perhaps CLI profile validation section. These are advisory targets only. The guide is separate authority; do not change it in the hygiene milestone uninvited.
