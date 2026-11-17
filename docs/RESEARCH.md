# Research

Research preserves planning context that should travel with the repository but is not yet implementation authority.

Use this layer for:

- product intent and design pressures;
- observations about AI-generated code and human review;
- architectural concerns not yet resolved into a milestone;
- competing options, hypotheses, and open questions;
- external technical research when a planning decision needs factual evidence.

Do not treat research as a specification. When planning resolves a material choice, promote the decision into the applicable milestone, project planning guidance, specification, architecture or engineering authority and keep research as rationale and provenance. The current major planning decisions on broad agentic rule development and semantic testing are authoritative in [PROJECT-GUIDANCE.md](PROJECT-GUIDANCE.md).

## Current planning context

- docs/research/PRODUCT-DIRECTION.md — product thesis, design principles, likely evolution, and milestone-selection criteria.
- docs/research/HUMAN-CONSUMABILITY.md — human-in-the-loop constraints, BORING-code direction, and review/catch-up pressure.
- docs/research/RULE-APPLICATION-ARCHITECTURE.md — current rule-application shape, scaling concerns, and candidate pipeline direction.
- docs/research/SAMPLING-RULES.md — historical statistical sampling options for expensive/dense rule populations, not the current production policy.
- [SEMANTIC-RULE-PORTFOLIO-STRATEGY.md](research/SEMANTIC-RULE-PORTFOLIO-STRATEGY.md) — rationale for broad agent-driven candidate implementation, comparative consolidation, and avoidance of synthetic LLM-verdict tests.
- [VERIFIABLE-REVIEW-WORK.md](research/VERIFIABLE-REVIEW-WORK.md) — the previously established distinction between deterministic review-work contracts and nonrepeatable external semantic judgments.
- [SAMPLING-GATE-PIVOT.md](research/SAMPLING-GATE-PIVOT.md) — why earlier hazard/age experiments were superseded by change-gate review.

These documents intentionally contain unresolved questions. A planning agent should use them to decide what deserves a milestone or further research; an implementation agent should follow the milestone and authoritative docs selected for its work.

## Historical activity-age work

[ACTIVITY-AGE.md](research/ACTIVITY-AGE.md) and related M0012–M0014 experiments document an earlier approach. That approach was superseded by the [gate-local sampling pivot](research/SAMPLING-GATE-PIVOT.md) and subsequent production milestones; it is retained for evidence and historical interpretation, **not** as current planning or implementation authority. Current runtime contracts remain in the specifications.
