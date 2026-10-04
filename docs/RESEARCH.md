# Research

Research preserves planning context that should travel with the repository but is not yet implementation authority.

Use this layer for:

- product intent and design pressures;
- observations about AI-generated code and human review;
- architectural concerns not yet resolved into a milestone;
- competing options, hypotheses, and open questions;
- external technical research when a planning decision needs factual evidence.

Do not treat research as a specification. When planning resolves a material choice, promote the decision into the applicable milestone/specification/architecture authority and keep the research as rationale and provenance.

## Current planning context

- docs/research/PRODUCT-DIRECTION.md — product thesis, design principles, likely evolution, and milestone-selection criteria.
- docs/research/HUMAN-CONSUMABILITY.md — human-in-the-loop constraints, BORING-code direction, and review/catch-up pressure.
- docs/research/RULE-APPLICATION-ARCHITECTURE.md — current rule-application shape, scaling concerns, and candidate pipeline direction.

These documents intentionally contain unresolved questions. A planning agent should use them to decide what deserves a milestone or further research; an implementation agent should follow the milestone and authoritative docs selected for its work.
