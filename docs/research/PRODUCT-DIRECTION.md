# Product Direction

## Status

Planning context, not implementation authority.

This document preserves the current product thesis and the reasoning that should inform future milestone planning. Concrete behavior remains governed by milestones, specifications, architecture, and engineering authority.

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

### Quantify uncertainty instead of pretending sampling is coverage

Expensive semantic rules may eventually inspect only part of an eligible population.

When they do, prefer statistically defensible sampling with explicit uncertainty over arbitrary per-run sample counts. The product should be able to spend more persistent state only where a stronger individual-subject guarantee is worth its cost, while dense populations may use aggregate/cohort models.

Sampling is not exhaustive verification. Its output and documentation must state the guarantee the chosen model can actually support.

See docs/research/SAMPLING-RULES.md.

## Likely evolution themes

These are planning candidates, not roadmap commitments.

### Rule-application architecture

The current vertical slice proves behavior but concentrates rule evaluation inside the engine. More rules, shared semantic prerequisites, repository-level rules, or multi-file rules may justify an explicit analysis pipeline and shared context model.

See docs/research/RULE-APPLICATION-ARCHITECTURE.md.

### Statistical sampling for expensive rules

Some high-value rules may be too dense or too expensive for exhaustive semantic review on every run.

A future sampling subsystem may support both:

- subject-state sampling, with decaying inspection value and randomized hazard thresholds when individual coverage matters;
- aggregate population sampling, optionally with bounded cohorts, when population-level quality surveillance is sufficient and per-subject state would be disproportionate.

The framework should provide the statistical machinery while rules provide interpretable population definitions and risk signals. Normal repositories should not require a database or opaque binary analysis index.

See docs/research/SAMPLING-RULES.md.

### Higher-value hygiene rules

Future rules should be selected by expected reduction in agent-generated maintenance debt rather than by completeness of a style catalogue.

Candidate areas include structural readability, needless abstraction, duplicated concepts, hidden control flow, misleading documentation, excessive indirection, and other patterns that raise reconstruction cost.

Each candidate should first answer:

1. Is the problem common enough in AI-produced code to justify product complexity?
2. Can it be detected deterministically with acceptable false positives?
3. Is deterministic remediation safe, or should the output remain guidance/review work?
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
- statistically defensible sampling where exhaustive semantic review would be disproportionate;
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
