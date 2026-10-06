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

**Rule catalog** — explicit engine-owned ordered registration of product rule modules and their descriptors. It is not dynamic plugin discovery.

**Session fact** — immutable lazily derived analysis data cached for one repository session and shared by multiple rule/rewrite consumers when justified.

**Rewrite module** — independent in-process implementation of one deterministic source transformation such as formatting or normalization. Rewrite modules share repository/session infrastructure with rules but use a distinct execution contract.

**Rewrite transaction** — all-target planning/validation/commit boundary that prevents a failed rewrite command from leaving a mixed selected-target state.

**Hygiene profile** — versioned project policy describing the supported .NET analysis baseline and mandatory repository invariants. It is distinct from the external guide-system profile metadata.

**Profile marker** — committed `.hygiene/profile.json` recording the installed hygiene profile ID/version.

**Mandatory profile rule** — fixed rule that defines supported-profile conformance. It cannot be disabled or ignored while that profile is installed.

**Configurable hygiene rule** — optional fixed source/review rule that participates in whole-rule enable/disable. It has no repository-supplied semantic parameters.

**Rule set** — engine-defined list of fixed rules evaluated by an execution command. Membership selects rules; it does not parameterize or change rule semantics.

**Deterministic remediation** — fixed, rule-owned repository transformation that establishes the rule's desired state where safe. Commands may choose whether to execute it; the rule's diagnosis and remediation meaning remain fixed.

**Bootstrap** — repository-wide command that installs/reconciles the current hygiene profile and applies available deterministic profile remediations.

**Update** — repository-wide command that reconciles an already-profiled repository to the current supported profile and applies current/future fixed migration/remediation rules.

**Documentation summary** — the concise documentation synopsis for one covered API subject. It is a semantic concept, not synonymous with the XML `<summary>` element.

**Summary carrier** — source XML documentation element that carries a documentation summary for a subject. Ordinary subjects use `<summary>`; a synthesized positional-record property uses the matching `<param>` on the record declaration. Direct `<inheritdoc/>` can satisfy a missing summary without supplying local prose.

**Explicit documentation prose** — locally written text in a prose-bearing XML documentation element. Sentence checks apply only to the specified prose-bearing elements, not arbitrary/custom XML documentation.
