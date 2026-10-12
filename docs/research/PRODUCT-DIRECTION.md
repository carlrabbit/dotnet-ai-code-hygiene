# Product Direction

## Status

Planning context, not implementation authority.

This document preserves the current product thesis and the reasoning that should inform future milestone planning. Concrete behavior remains governed by milestones, specifications, architecture, and engineering authority.

## Current product and research direction

M0016 replaced production hazard/age-based semantic-review scheduling with deterministic, bounded, gate-local candidate selection for the three original semantic-review rules. A concrete changed-source/manual/explicit-surveillance gate, not the passage of time, authorizes semantic review work. Historical M0009–M0015 hazard and age research remains historical rather than operative authority. The research synthesis is in [SAMPLING-GATE-PIVOT.md](SAMPLING-GATE-PIVOT.md); the implemented behavior is governed by the current specifications.

M0017 planning expands that mechanism to type-responsibility, test-quality, and ordinary-comment review. It also reclassifies the existing visual-block blank-line signal as a review *candidate*, not proof of a missing mandatory formatting fix. See [Verifiable Review Work](VERIFIABLE-REVIEW-WORK.md) for the product/agent boundary.

## Product thesis

dotnet-ai-code-hygiene exists to keep AI-produced .NET code mechanically predictable, structurally readable, and reviewable over repeated agent-driven change.

The product is not primarily a conventional style checker. Its useful niche is the boundary between:

- concerns that can be enforced deterministically and cheaply;
- concerns that require semantic/model judgment;
- code qualities that matter because AI can generate and modify more code than a human can continuously inspect.

The tool should remove avoidable variance before model intelligence is spent.

## Directional principles

### Deterministic before semantic

If a useful property can be established reliably through syntax, semantic analysis, compilation, repository state, or another deterministic mechanism, prefer that mechanism over model judgment.

Use semantic/model review for questions where deterministic enforcement would either be brittle or encode a poor proxy for the actual quality being judged.

### Opinionated before configurable

Early hygiene rules should have a strong project-owned opinion rather than exposing thresholds, severity matrices, ordering, or per-rule parameter surfaces.

The durable rule model is **fixed opinion, optional applicability**: each rule ID has one semantic contract with no repository-supplied parameters; optional configuration enables or disables that whole rule. Language, thresholds, scope, severity, questions, sampling, convention, and remediation are not parameter bags. When policies can apply independently, give them separate rule IDs.

Configuration has a maintenance and reasoning cost for both humans and agents. Add it only when a real use case proves that one fixed policy is insufficient.

### BORING is generally desirable

Prefer conventional, explicit, locally understandable structures over cleverness or novelty when the alternatives are semantically equivalent.

BORING does not mean maximizing verbosity, banning abstraction, or mechanically applying stylistic patterns. It means reducing surprise, hidden coupling, special cases, and unnecessary representational freedom.

The desired effect is lower reconstruction cost for both coding agents and humans returning to unfamiliar code.

### Human review remains a first-class constraint

AI-first development does not remove the human reader.

Predictable patterns can reduce cognitive load, but increased code volume can still make review and catch-up difficult. Hygiene should therefore consider navigability, locality, structural regularity, and the amount of context required to understand a change—not only whether each individual file is syntactically tidy.

See docs/research/HUMAN-CONSUMABILITY.md.

### Model intelligence stays outside deterministic machinery by default

The current product boundary deliberately keeps model/provider invocation outside the CLI. The CLI can select semantic review work and construct handoffs, while the calling agent performs judgment.

This boundary should remain the default unless a future milestone establishes a concrete benefit that cannot be achieved cleanly through external orchestration.

### Prefer a small high-value surface

Do not attempt to replace Roslyn analyzers, dotnet format, IDE tooling, or every style ecosystem.

Add rules or rewrites where the tool can provide a distinct AI-development benefit, a stronger opinionated contract, useful agent-oriented output, or a workflow that existing tools do not provide well.

### Verify review-work machinery, not the reviewing model

The hygiene product's verifiable contract is to discover rule-eligible subjects, identify and explain deterministic indicators, select bounded gate-relevant review work, supply truthful context, persist explicit accepted evidence, and invalidate stale work. A deterministic trigger is not proof of a semantic defect.

Judgment and context-sensitive remediation belong to the external invoking agent. Do not make software acceptance depend on benchmarks of the current model's subjective correctness, usefulness scores, a particular model/provider, or predictions about future agent capability. The tool must remain useful as models change.

Gate-local selection is selective inspection, not population certification. No defect-rate, completeness, or false-positive guarantee follows from a five-item sample. See [Verifiable Review Work](VERIFIABLE-REVIEW-WORK.md).

## Likely evolution themes

These are planning candidates, not roadmap commitments.

### Rule-application architecture

M0007 established independent explicit in-process rule modules over a shared lazy `RepositorySession`. New rules should follow that pattern rather than trigger a generalized pipeline, callback framework, plugin registry, or a separate executable per rule. Add lazy shared facts only for actual shared semantics or measured cost.

See docs/research/RULE-APPLICATION-ARCHITECTURE.md.

### Bounded semantic-review selection

M0016 supplies the active fixed-budget gate-local mechanism. New semantic rules should reuse it, with independently defined subjects, questions and escalation, rather than reviving historical hazard clocks or introducing parameter-learning experiments.

The historical subject-state and aggregate/cohort studies remain in docs/research/SAMPLING-RULES.md for provenance, not as an implementation roadmap or substitute for the current sampling specification.

### Higher-value hygiene rules

Future rules should be selected by expected reduction in agent-generated maintenance debt rather than by completeness of a style catalogue.

Candidate areas include structural readability, needless abstraction, duplicated concepts, hidden control flow, misleading documentation, excessive indirection, and other patterns that raise reconstruction cost.

Each candidate should first answer:

1. Is the problem common enough in AI-produced code to justify product complexity?
2. Can it be detected deterministically with acceptable false positives?
3. Does detection establish a mechanical defect, or only a candidate requiring external judgment and contextual remediation?
4. Does an existing .NET tool already solve it adequately?

### Deterministic remediation

Safe deterministic rewrites are valuable where equivalence can be established and the result has one strongly preferred form.

Automatic remediation should not broaden into speculative refactoring merely because an agent could perform it. The CLI should keep deterministic transformation and contextual remediation conceptually separate.

### Agent integration surfaces

Portable Agent Skills, MCP, richer orchestration integration, or provider-specific workflows may eventually improve discoverability or reduce orchestration cost.

They are not goals by themselves. Add such surfaces only if they materially improve the workflow beyond the installed CLI plus stable agent help.

### Distribution and platform reach

Public-feed publication, Linux/macOS support, and wider packaging are useful only when the product behavior is stable enough that broader consumption is worth the support contract.

They should not displace higher-value hygiene work merely to make the tool look complete.

## Milestone selection criteria

Prefer a future milestone when it does at least one of the following:

- materially reduces recurring AI-generated code debt;
- makes an existing hygiene capability substantially safer or cheaper to apply;
- improves human or agent comprehension at repository scale;
- removes a proven architectural bottleneck before it constrains additional rules;
- closes an important consumer/workflow gap with evidence that the gap matters.

Avoid milestones whose main justification is framework completeness, fashionable integration, or speculative extensibility.

## What mature enough means

The project does not need to become a general static-analysis platform.

A mature version should provide a compact set of high-value hygiene operations that an agent can apply routinely, with:

- deterministic behavior where determinism is appropriate;
- bounded semantic review where judgment is required;
- bounded gate-local selective inspection with claims limited to what the selection actually establishes;
- safe rewrite semantics;
- predictable low-configuration policy;
- repository-scale performance appropriate to normal agent workflows;
- outputs that make remediation and human review straightforward;
- a stable boundary that does not require embedding an AI provider into the tool.

## Open planning questions

- Which AI-generated code pathologies provide the highest value for the next rules?
- How much repository-level context should a rule be allowed to request?
- When does the current engine need to become an explicit rule pipeline?
- Which future semantic rules need individual sampling guarantees versus population-level statistical guarantees?
- Should BORING become a named product principle with enforceable sub-rules, or remain a planning heuristic?
- Which forms of deterministic remediation are sufficiently safe to belong in the CLI?
- What evidence would justify moving model invocation or orchestration into the product boundary?
- What evidence would justify cross-platform support or broader distribution?
