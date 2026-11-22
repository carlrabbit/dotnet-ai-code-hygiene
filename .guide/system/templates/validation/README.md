# Optional advanced validation examples

The `validation-plan.json` and `validation-receipt.json` files are **opt-in examples**, not templates that every repository or milestone must generate.

Prefer direct build/test commands or small `eng/` scripts for ordinary milestones. Use stages, dependency fingerprints, and aggregate verifiers only when a materially expensive suite or audit requirement justifies the extra maintenance cost. See `meta/CONSTRAINED-EXECUTION-MODEL.md` for the current policy.
