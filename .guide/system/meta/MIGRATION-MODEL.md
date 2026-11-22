# Migration Model

Guide updates replace the repository-local pinned `.guide/system/` snapshot and apply profile-aware target-state migration decisions, not vague implementation instructions. Straightforward metadata/documentation updates may be applied directly; coordinated implementation uses a ready migration milestone.

Do not ask an implementation agent to simply "upgrade to the latest guide".

Use one of the stable planning entry points:

```text
templates/prompts/adopt-guide-system.md
templates/prompts/update-guide-system.md
```

The migration workflow reads the repository's existing snapshot pin (or metadata for pre-1.0 external-only repositories), resolves the requested tagged target upstream, inspects the changelog and applicable migration chain, and assesses the target repository. **For this explicitly authorized transition, the target tag's adoption/update workflow and migration requirements govern; the existing pinned workflow describes the starting state but cannot prohibit a target-version change.** If no local snapshot exists, load the target tagged guide temporarily from upstream to bootstrap installation. Once the snapshot and metadata are installed and reconciled, the new local copy governs. Apply straightforward changes directly or prepare a ready milestone/package when substantial coordinated implementation is necessary.

Migration is a special planning workflow. It is not a milestone execution mode.

When migration requires substantial coordinated implementation or unresolved material architectural decisions, create a ready milestone and follow the normal lifecycle:

```text
planning -> ready -> implementing -> done
```

The execution profile for a migration milestone, when one is needed, is selected independently from the migration workflow type. Snapshot installation or replacement and matching metadata are ordinary versioned repository changes; no silent remote synchronization, locally edited shared-guide fork, or published artifact is involved. See [Repository-Local Guide Snapshot Model](LOCAL-GUIDE-SNAPSHOT-MODEL.md).

**Target-state rule:** Read the applicable migration chain to identify meaningful transitions, but apply the effective requirements of the **target version**. Do not create intermediate ledgers, evidence registries, review machinery, or other artifacts whose requirements are superseded before the target version, unless an intermediate transformation is genuinely necessary to preserve data, historical evidence, or project authority. Do not skip necessary compatibility-sensitive data migrations.

## Migration flow

```text
1. Identify current guide profile metadata.
2. Resolve latest guide-system version.
3. Read changelog and relevant migrations.
4. Assess repository state.
5. Resolve migration decisions and classify changes.
6. Evaluate optional transition capabilities required/recommended by the migration chain.
7. Choose direct update for straightforward metadata/docs or a ready milestone for substantial coordinated implementation.
8. Apply permitted direct changes or prepare the implementation-ready package.
9. Execute a ready milestone with the canonical execution prompt only when a milestone was needed.
10. Verify the target-state changes proportionately and obtain any explicitly required human acceptance.
11. Run deferred documentation synchronization when applicable.
```

For a ready milestone, `templates/prompts/execute-milestone.md` remains the canonical executor entry point. Follow [Planning Handoff Transport Model](HANDOFF-TRANSPORT-MODEL.md): Git delivery identifies the exact accepted planning commit and milestone path, while ZIP delivery identifies the intended base when known and overlay instructions. A direct update needs no milestone or implementation handoff; report the actual resulting commit when delivered via Git, and focused verification.

## Change classification

| Class | Meaning |
|---|---|
| Required | Must be applied for the target profile. |
| Conditional | Apply only when a module/profile/maturity condition is met. |
| Deprecated | Remove or stop referencing. |
| Manual review | Requires human or documentation-agent judgment. |
| No-op | Intentionally not applicable. |

## Optional transition capabilities

A migration may introduce a reusable capability whose historical adoption is useful but not required for guide-system compliance.

Treat such work separately from required migration changes.

For example, guide-system 0.8 introduces a durable planning-research model. A repository crossing from a pre-research-model version may optionally bootstrap historical planning knowledge from surviving chats, notes, experiments, or other evidence.

The migration document that introduces the capability defines when it should be evaluated and how it relates to compliance.

Generic migration rules:

- do not make an optional historical reconstruction a hidden prerequisite for guide compliance;
- do not fabricate unavailable historical evidence;
- allow bounded adoption to occur inside the migration when cheap and useful;
- prefer a dedicated special workflow when adoption is substantial;
- allow optional bootstrap work to happen later if evidence becomes available after the guide migration;
- keep the resulting durable artifacts in the authority/knowledge layer defined by the capability rather than adding ad-hoc migration metadata.

The generic update prompt follows the migration chain. It must not hard-code one version's optional transition behavior as a permanent rule for every future guide update.
