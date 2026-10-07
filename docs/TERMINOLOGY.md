# Terminology

**Finding** — deterministic rule occurrence.

**Review candidate** — deterministic finding requiring contextual remediation judgment.

**Semantic review rule** — rule whose primary output is a bounded semantic review batch.

**Review batch** — run-scoped semantic review workload selected deterministically for an external agent/model reviewer.

**Formatter** — deterministic presentation-only source transformation.

**Normalizer** — deterministic semantics-preserving structural source transformation.

**Repository session** — command-scoped shared repository-analysis substrate that owns or coordinates target resolution, project/workspace loading, Roslyn context, and lazy session facts. It is not persisted between commands.

**Reporting scope** — files/subjects for which the current invocation may emit source findings or review work.

**Readable context** — broader repository/project information a rule may inspect to evaluate subjects in the reporting scope correctly.

**Rule module** — independent in-process implementation of one fixed rule contract. It owns domain-specific analysis/traversal but uses the shared repository session and host result mechanics.

**Rule locality boundary** — the physical source location where one product rule's descriptor, fixed rule-specific interface text, and primary evaluator are discoverable together, with only clearly named shared facts/helpers followed as needed.

**Rule interface text** — fixed user/reviewer-facing text that is part of one rule's contract, such as descriptor purpose, finding message/suggestion/observation/reason/constraint templates, semantic-review questions, and rule-specific escalation wording. Dynamic evidence values are not themselves interface text.

**Rule catalog** — explicit engine-owned ordered registration of product rule modules. It establishes deterministic registration/order and is not dynamic plugin discovery or a second rule-definition store.

**Session fact** — immutable lazily derived analysis data cached for one repository session and shared by multiple rule/rewrite consumers when justified.

**Rewrite module** — independent in-process implementation of one deterministic source transformation such as formatting or normalization. Rewrite modules share repository/session infrastructure with rules but use a distinct execution contract.

**Rewrite transaction** — all-target planning/validation/commit boundary that prevents a failed rewrite command from leaving a mixed selected-target state.

**Hygiene profile** — versioned project policy describing the supported .NET analysis baseline and mandatory repository invariants. It is distinct from the external guide-system profile metadata.

**Profile marker** — committed `.hygiene/profile.json` recording the installed hygiene profile ID/version.

**Mandatory profile rule** — fixed rule that defines supported-profile conformance. It cannot be disabled or ignored while that profile is installed.

**Configurable hygiene rule** — optional fixed source/review rule that participates in whole-rule enable/disable. It has no repository-supplied semantic parameters.

**Rule set** — engine-defined list of fixed rules evaluated by an execution command. Membership selects rules; it does not parameterize or change rule semantics.

**Deterministic remediation** — fixed, rule-owned repository transformation that establishes the rule's desired state where safe. Commands may choose whether to execute it; the rule's diagnosis and remediation meaning remain fixed.

**Bootstrap** — repository-wide command that installs/reconciles the current supported hygiene profile.

**Update** — repository-wide command that reconciles an already installed supported profile to the tool's current fixed representation.

**Ignore decision** — persisted explicit user acceptance of one deterministic finding identity/fingerprint.

**Stale ignore** — persisted ignore decision whose subject still exists but whose current evidence no longer matches the accepted fingerprint/discriminator.

**Semantic review handoff** — explicit persisted request that transfers expanded semantic-review work and source context to an external frontier reviewer without invoking a model inside the CLI.
# Sampling terms

- **Hazard:** rule-supplied non-negative cumulative inspection pressure.
- **Due:** a threshold has been crossed and inspection work is available; due does not mean observed.
- **Observation:** explicit evidence that consumes a due ticket and advances sampler state.
- **Subject-state sampling:** persistent hazard and generation for each tracked stable subject; can express individual revisit behavior.
- **Aggregate population sampling:** persistent hazard/evidence for a structural scope and optional cohort; does not preserve individual candidate history.
- **Effective Beta evidence:** discounted pass/fail counts that a rule may combine with its own prior; not a strict stationary-population posterior.
