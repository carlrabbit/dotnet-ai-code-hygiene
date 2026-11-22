# Migration — Guide System 0.9.3 → 1.0.0-rc.1

**Classification:** Major methodology change. Required for repositories that adopted 0.9.x mandatory execution-ledger/evidence/validation requirements; metadata-only otherwise.

## New authority

Use ADR-0030 (agent-scaled milestones), ADR-0031 (risk-proportionate assurance), and the current milestone/validation models, prompts, and templates. Superseded 0.9.x ADRs explain history but do not reimpose mandatory proof machinery.

## Product-repository migration

1. Update `.guide-profile.json` guide system/shared profile versions to `1.0.0-rc.1` where the profile applies. Keep project-local profile versions independent. Confirm the project baseline implementation model and execution mode rather than blindly copying template values. The 1.0 template uses `ai-executed` for executor ownership, independently of scope size and human review. Normalize legacy `ai-executed-human-reviewed` or prerelease `ai-executed-broad` values without requiring reapproval of completed work.
2. Reconcile `AGENTS.md` as a small agent router. Remove unconditional requirements to read a planning-seeded `.execution/` file; do not paste guide methodology into this file.
3. Review `docs/ENGINEERING.md`, project planning guidance, and project-local profiles for legacy unconditional ledger, acceptance-to-case registry, fingerprinted receipts, sharding, consumer-installed milestone tests, and final evidence reconciliation. Remove guide-derived mandates. **Preserve genuinely project-specific risk/audit/release contracts.**
4. For **new** milestones use the compact five-part structure: target, architecture/invariants, scope, acceptance, focused validation with release deferrals. Do not fabricate old evidence cases. Already completed milestones and review records remain historical; do not rewrite their proof history.
5. For **active** milestones, preserve product goals and substantive acceptance, then revise their validation contract if excess work was inherited solely from the old guide. Stop creating new gates during correction unless material risk justifies them. Optional resume ledgers may be kept if genuinely useful; no exact ID set reconciliation is required.
6. Prepare a distinct release-readiness milestone to qualify final packages, consumers, compatibility, and required platforms when actually approaching release. Defer those checks during ordinary feature work unless directly relevant or required for high-consequence risk.
7. Replace old generic `review-*` requirements with lightweight PR or milestone-local acceptance decisions where a review is actually required. Preserve intentionally adopted structured review tooling and completed approval history when project-specific needs justify it. For active structured reviews, normalize `blocking` as derived from `required` applicability rather than a separate class; do not rewrite historical decisions.
8. Keep project-local integration-first tests where the real runtime matters. The new guide removes automatically bundled distribution qualification, **not** meaningful integration coverage.

## Migration execution and verification

Perform small, well-understood metadata/documentation updates directly in the authorized repository, with focused checks and a concise completion summary; no migration milestone is required. Use a ready milestone or repository-relative patch/ZIP when coordinated implementation work or material decisions justify it. Do not create a mandatory migration ledger. When upgrading across multiple versions, read relevant intermediate migrations but do not create artifacts required only by superseded versions unless preserving project data or operative authority genuinely depends on that transition.

Inspect the target repository's applicable authority and agent entrypoints for contradictory requirements. Ensure implementation milestones use risk-proportionate checks, release qualification is assigned to the correct boundary, and optional progress/review machinery is not accidentally mandatory. Do not require hypothetical planner executions, fixed scenario suites, or model-behavior trials as a guide-migration acceptance gate. Confirm there are no contradictory inherited requirements in local profiles, `AGENTS.md`, or engineering docs.

## Release status

This document describes the **release candidate** `1.0.0-rc.1`; stable `1.0.0` follows when remaining concrete normative defects are corrected; no planner trials or dogfooding gate is implied. Do not confuse publication of the guide candidate with qualification of a product using the guide.
