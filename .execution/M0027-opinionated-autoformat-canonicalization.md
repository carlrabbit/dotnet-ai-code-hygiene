# M0027 — Optional Execution Progress

> **Operational resume notes only.** The authoritative product contract is [M0027](../docs/milestones/M0027-opinionated-autoformat-canonicalization.md). Update this file at meaningful checkpoints or when stopping; it is **not** an acceptance ledger, evidence register, or checklist of mandatory test gates.

**Milestone:** `docs/milestones/M0027-opinionated-autoformat-canonicalization.md`  
**Starting implementation state:** Not started; planning ready on PR #32  
**Last updated:** 2026-11-22 (planning handoff)

## Completed capability work

- Planning only: ready M0027 defines the Autoformat-only ownership boundary, all nine modernization families, readability canonicalization, localized exceptions, idempotence, and agent-owned build/repair.
- No M0027 implementation or verification is claimed.

## Remaining work

The implementer may reorder, combine, replace or discard implementation approaches while preserving the milestone contract. These are **progress areas**, not additional acceptance obligations:

- Clean formatter architecture and inherited Hygiene compatibility surfaces without removing live Hygiene semantic review.
- Implement actual automatic rewriting for all nine modernization families; preserve the intentionally aggressive `IReadOnlyList<T>` collection-expression policy.
- Implement visual-block, signature/parameter, long-line, ordinary comment and whitespace canonicalization.
- Integrate source-local `autoformat:retain <rule-id> -- <reason>` exceptions and stable second-pass behavior across transformations.
- Simplify everyday Autoformat engineering checks; synchronize directly affected operative documentation.
- Review the result against the milestone, run proportionate focused tests/a few disposable SDK integration fixtures, and write the milestone completion note.

## Checks / known risks

- **No M0027 tests run yet.** During implementation, record only useful latest focused checks/results and material open defects here.
- Key risks to watch: source/comment/directive loss; unstable or oscillating layouts; misleading unsupported-candidate reports; Autoformat accidentally coupled back to Hygiene; policy initialization overwriting unrelated content.
- **Do not validate the changed consumer project by building it inside Autoformat.** The implementing agent may build the workbench itself and subsequently fix errors in transformed projects; compilation/semantic equivalence of transformed targets is not an Autoformat gate.
- The existing `eng/autoformat/validate.ps1` currently runs package/installed-consumer qualification. That is **not** the default M0027 validation command. Use direct affected-project builds, focused tests and small engine-level fixtures, or simplify the script accordingly.
- Packaging/shim, real-repository dogfood, self-host and full release validation remain deferred.

## Resume point

1. Re-read the ready milestone and inspect the live branch, relevant source and changed files; do not infer implementation progress from this initial ledger.
2. Choose a coherent implementation sequence based on actual dependencies and keep work-in-progress changes on the implementation branch.
3. Before interruption, replace these starter notes with a concise account of completed behavior, unresolved defect(s), commands/results actually observed, and **the next concrete action**. Preserve truthfulness; do not manufacture green checks or a proof matrix.

Do not create additional progress bureaucracy, fingerprinted receipts, AC/EC ID tables, or formal human-review ceremony for this milestone.
