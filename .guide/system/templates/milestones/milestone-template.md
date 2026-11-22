# Milestone — <Title>

**ID:** <M00XX>
**State:** draft | ready | implementing | done
**Type:** feature | migration | investigation | documentation | release

## Goal and target capability

<Describe the coherent observable outcome. Agent-sized scope is encouraged; do not split solely because it is large for human implementation.>

## Architecture and invariants

<Document the results of rigorous change-impact analysis where material: intended behavioral changes, explicitly permitted secondary consequences or incompatibilities, protected existing contracts/invariants, and interactions among affected subsystems or operating modes. Make consequential boundaries precise; permission for a broad rewrite is not permission for unrelated changes. For consequential external or persistent mutations, identify authorized targets/actions and proportionate recovery/isolation precautions; implementation alone never implies execution authority. Bounded reversible architectural alternatives may be delegated with evaluation criteria and invariants. Do not prescribe speculative implementation mechanics or invent extra sections merely to record analysis.>

## Scope and exclusions

**In scope:** <capabilities and relevant subsystem boundaries>

**Not in scope:** <real exclusions, not a file allowlist>

## Acceptance and material obligations

- **AC-01:** <Independently deliverable observable outcome A>
- **AC-02:** <Independently deliverable observable outcome B>
- **DOC-01:** <Direct documentation/migration deliverable, only if required>

<Preserve each material obligation, including separately required non-code deliverables. For substantive implementation milestones, seed exactly these applicable IDs in `.execution/<milestone-id>.md` before `ready`. For a genuinely trivial indivisible task, explicitly record why no ledger is needed. The IDs are a completeness inventory, not a one-test-per-criterion matrix.>

## Execution obligation ledger

**Ledger:** `.execution/<milestone-id>.md` <planner-seeded for substantive implementation milestones; or briefly justify omission for a genuinely trivial indivisible task>.

<Ensure all independently deliverable acceptance obligations are represented with the same stable IDs before `ready`. Implementers may choose implementation steps; they cannot silently discard seeded obligations.>

## Validation

**Milestone:** <Discriminating focused checks chosen from plausible consequential failures; when relevant, contrast allowed/disallowed cases and check cross-mode consistency or protected properties, with representative real integration fixtures. Do not require one test per invariant or universal semantic equivalence. Name targets/loci only where consequential.>

**Important risks:** <Plausible incorrect behaviors and protected-boundary violations that the chosen checks should expose; include focused mitigation where material.>

**Deferred to release:** <Packaged/installed consumer, real-repository, compatibility or platform qualification when applicable; otherwise none.>

<Only add specific release-stage or formal human gates at this milestone if explicitly justified by product behavior, project authority, or consequence risk. Expensive resumability is optional.>

## Documentation and other obligations (if material)

<Direct project-truth updates, optional sync hints, necessary migration/review authorization. Omit this section if empty.>

## Completion note (filled after implementation)

<Write a concise durable record here, or link to the project-defined completion record: delivered outcome, focused checks/results, known limitations, and deferred release checks. No evidence-case ledger, proof register, or fingerprinted receipts by default.>
