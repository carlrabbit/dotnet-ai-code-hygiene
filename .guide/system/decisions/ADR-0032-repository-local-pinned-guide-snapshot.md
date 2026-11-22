# ADR-0032 — Pin Shared Planning Methodology Inside the Product Repository

**Status:** Accepted for 1.0.0

**Revises:** The external-only guide-authority rule from earlier versions. The upstream guide remains the source of versioned history; an intentional tracked local snapshot is no longer prohibited.

## Context

An external-only guide lets planning and implementation use different context at different times. Version metadata alone does not make the exact planning rules and referenced shared profiles available from a historical product commit. Product repositories already preserve their own project guidance and useful research; shared methodology should be equally recoverable without a live remote dependency.

## Decision

Each consuming product repository stores one **complete, unmodified, version-pinned** upstream guide tree in `.guide/system/`. Its `.guide/system/README.md` is the planning task router. The pin records the upstream Git tag and peeled commit SHA in `.guide-profile.json`, alongside the source repository and local path. Bootstrap and explicit version upgrades may fetch upstream; routine planning cannot silently load a newer version.

Project-specific profiles stay in `.guide/profiles/`, operative project authority in ordinary project docs, and non-authoritative research in `docs/research/`. Do not edit the snapshot to customize rules or create additional shared-guide copies under project docs. Profile composition and disagreements are resolved in planning. Implementation agents still receive ready milestones and project authority without needing the guide.

Prefer Git subtree with squashed upstream history, but an exact committed tree copy is acceptable when subtree is unavailable. Do not build packaging, publication, synchronization, or profile-filtering machinery just to copy Markdown. The upstream guide repository itself uses its root and does not contain a nested self-copy.

See `meta/LOCAL-GUIDE-SNAPSHOT-MODEL.md` for the authoritative installation and upgrade contract.

## Consequences

Planning can be reproduced from a product-repository commit, including its exact shared methodology, project authority, and research. The cost is a modest tracked documentation copy, occasional explicit migration, and keeping that copy upstream-owned. Ordinary implementation context remains small.
