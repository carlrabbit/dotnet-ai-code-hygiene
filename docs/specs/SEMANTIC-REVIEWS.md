# Semantic review batches

The supported public CLI namespace is `dotnet hygiene`. The autonomous no-argument lifecycle presents selected review batches as a grouped, selected-only work order while preserving the exact per-rule questions, escalation policy, handles, tickets, and independent acceptance described here. Manual diagnostics use the same rule contracts through `dotnet hygiene check` and `dotnet hygiene review`.

## Model and output

The deterministic engine discovers each rule's complete eligible population. All six semantic-review rules use the shared gate-local selector specified in `docs/specs/SAMPLING.md`; each rule retains its own population semantics, questions, reviewer class, escalation condition, and acceptance meaning. The CLI performs no model/provider invocation.

Every enabled semantic-review rule emits exactly one normal batch on a successful check, including `0/N` and `0/0`. `PopulationCount` is the complete rule-eligible population for the reporting scope. `SampleCount` is the selected subset, with a fixed maximum of five per rule. Mechanical findings remain exhaustive. The host materializes the selected items as supplied and never performs another ranking.

## Gate surfaces

```text
dotnet hygiene check [paths...] [--changed | --base <git-ref> | --review-all]
```

- Bare `check` runs deterministic rules over the full default source scope and schedules zero semantic review.
- `--changed` retains its existing worktree-relative-to-`HEAD` target meaning and admits semantic review only for current eligible C# changes.
- `--base <ref>` resolves `git merge-base <ref> HEAD`, then includes committed feature changes and staged, unstaged, and untracked current C# changes relative to that base. Only current existing C# files are reporting targets; unchanged project context remains readable for analysis.
- Explicit paths select a manual bounded inspection, independent of Git diff.
- `--review-all` explicitly requests bounded full-repository semantic inspection.

`--base` cannot combine with explicit paths, `--changed`, or `--review-all`. `--review-all` cannot combine with any target selector. Deleted paths are not current candidates; rename destinations are deduplicated. Invalid refs and invalid option combinations are errors, never a silent full scan. Non-source changes and unchanged gates do not admit ordinary semantic review.

On ordinary source-change gates, changed subject content ranks first. Up to one unchanged context-affected subject from a gate-touched source document can be selected, leaving up to four slots for changed subjects. If there is no context candidate, up to five changed subjects may be selected. Unrelated background subjects are eligible only under explicit `--review-all`. Explicit paths and `--review-all` may select up to five without the context reserve. Selection is deterministic and without replacement, but can be biased; it does not certify population quality or provide a calibrated defect-rate estimate.

## Acceptance, expansion, and handoff

`dotnet hygiene review accept <batch-handle> <item-id>...` and `--all` accept only items in the latest normal sample. Acceptance is an explicit caller assertion that every required question is confidently acceptable and no escalation condition applies. Negative or uncertain judgments are not recorded by the CLI. Accepted observations persist separately from legacy hazard/age state.

Acceptance revalidates the current gate, base, full eligible population and each private ticket before an atomic write. Any missing, duplicate, stale, nonselected, incompatible, or previously accepted item rejects the complete request. Repeating a gate cannot replenish its fixed sample after partial acceptance. Accepted unchanged fingerprints are suppressed; changed content can become eligible again.

`dotnet hygiene review expand <batch-handle>` and `dotnet hygiene review handoff <batch-handle> [--file <path>]` revalidate and expose the complete eligible population, even if the normal sample is zero. Handoff stays transport-neutral and includes the source context needed by a decoupled reviewer. It contains no private ticket or selection state and never invokes a model.

## Documentation summary rules

Both summary rules use the shared API subject and summary-carrier population from `docs/specs/DOCUMENTATION.md`:

- ordinary subjects use explicit non-empty `<summary>` prose;
- synthesized positional-record properties use matching `<param>` prose;
- inheritdoc-only subjects are excluded;
- optional parameter documentation is not a separate review subject.

`docs.summary.quality.review` is version 6, batch B-1, with the fixed Q1 technical correctness, Q2 information value, and Q3 clarity/scope questions. Its normal reviewer is `implementer`, escalated reviewer is `frontier`, and escalation applies when a sampled summary materially fails any required question or the implementer is uncertain.

`docs.summary.language.german.review` is version 4, batch B-2, with the fixed question: “German language: Is the summary natural, comprehensible German rather than awkward literal translation or merely German-looking text?” Its normal reviewer is `implementer`, escalated reviewer is `frontier`, and escalation applies when the sampled summary materially fails or the implementer is uncertain. German is fixed in the rule identity; there is no language parameter. English summaries are eligible and fail this question.

The rules retain independent populations, state identity, toggles, and batches. Their rule IDs and review questions remain stable as selection semantics advance.

## `architecture.boringness.review`

Version 3, batch B-3, reviews source-backed class, struct, record, and interface declarations, including nested and partial declarations. Enums, delegates, and generated source are excluded. Candidate identity is project-qualified and source-backed. Current accepted-review state stores the selected subject identity and fingerprint, never source text; legacy aggregate sampling records are not imported.

Its normal reviewer is `implementer` and escalated reviewer is `planner`. The fixed questions are:

- **Q1 — Speculative abstraction:** Is there an interface, provider, factory, strategy, generic mechanism, extension point, or similar abstraction with only one meaningful production use/implementation and no current requirement for variability?
- **Q2 — Indirection:** To understand what this code actually does, must you follow several forwarding/delegation layers before reaching the behavior?
- **Q3 — Change locality:** Would a small behavioral change require coordinated edits across multiple plumbing types, registrations, adapters, or mappings rather than mostly changing the code that owns the behavior?
- **Q4 — Hidden machinery:** Does the implementation rely on reflection, dynamic discovery, convention-based registration, a custom DSL/framework, or other implicit machinery where ordinary explicit C# could satisfy the current requirement?
- **Q5 — Unclear ownership:** After reading the relevant types, is it difficult to identify the single place that owns the behavior or state being reviewed?

Legitimate external-service, persistence, platform, and interoperability boundaries may justify abstraction. Escalate when Q4 is yes or at least two of Q1, Q2, Q3, and Q5 are yes. One non-Q4 signal alone is review evidence, not planner escalation. The reviewer evaluates current requirements rather than hypothetical future extensibility.

## M0017 rules

`readability.type-responsibility.review` is v1/B-4. It reviews source-backed class, struct, interface, and record declarations, including nested and partial parts; enums, delegates, generated/ineligible sources, and nondeclarations are excluded. Its fixed questions are:

- **Q1 — Ownership:** Does this type mix responsibilities without one coherent ownership/purpose?
- **Q2 — Coupling:** Does understanding or changing ordinary behavior require reconstructing unrelated state or workflows within this type?
- **Q3 — Separation:** Is there a concrete responsibility that should be owned elsewhere without creating needless abstraction or fragmentation?

`implementer` reviews; material concerns or insufficient context escalate to `planner`. Type length and member count are not verdicts.

`tests.quality.review` is v1/B-5. It includes source-backed methods whose Roslyn-resolved attributes are TUnit `[Test]`, xUnit `[Fact]`/`[Theory]`, NUnit `[Test]`/`[TestCase]`, MSTest `[TestMethod]`/`[DataTestMethod]`, or derived from those framework attributes. Name-only matches and setup/teardown methods are not test evidence. Its fixed questions are:

- **Q1 — Regression evidence:** Could the assertions plausibly detect a meaningful violation of the tested behavior rather than merely successful execution or a non-null result?
- **Q2 — Contract focus:** Does the test primarily examine observable behavior rather than unnecessarily binding itself to implementation details?
- **Q3 — Maintainability:** Is the setup/assertion structure clear enough to diagnose failure without unjustified fragility, coupling or mock choreography?

`implementer` reviews; material concerns or uncertainty escalate to `frontier`. Assertion and mock counts are not verdicts.

`comments.intent.review` is v1/B-6. It reviews ordinary source-backed `//` groups and `/* ... */` blocks, excluding XML documentation, directives, disabled/inactive code, generated sources, and comment-like text in literals. Standalone contiguous `//` lines group together; trailing comments remain distinct from leading blocks, and syntax/statement boundaries separate independent blocks. Copyright, license, legal, administrative, and other ordinary comments remain eligible without text- or position-based classification. An external reviewer determines whether a comment serves a legitimate purpose in context; a comment is not defective only because it does not explain implementation behavior. Identity uses normalized comment and adjacent syntax anchors, while fingerprints include comment-free owner code context so unrelated comment insertion does not invalidate unchanged evidence. Truly identical blocks with the same owner and adjacent syntax use deterministic discovery order; inserting within that indistinguishable set can shift ordinals. Its fixed questions are:

- **Q1 — Intent:** Does the comment provide useful non-obvious rationale, constraint or meaning rather than narrate nearby syntax?
- **Q2 — Fidelity:** Is the comment consistent with current implementation and relevant local context?
- **Q3 — Clarity:** Is the explanation sufficiently specific, scoped and concise to help a reader without redundant narrative?

`implementer` reviews; material concerns or uncertainty escalate to `frontier`. It neither requires nor rewrites comments.

All three include accurate source locations and current file context. Non-applicable summary fields are empty. An external reviewer inspects related code or reports uncertainty when the embedded context cannot establish a dependency or contract.

## Compatibility

Batch order and identifiers remain:

```text
B-1 docs.summary.quality.review
B-2 docs.summary.language.german.review
B-3 architecture.boringness.review
B-4 readability.type-responsibility.review
B-5 tests.quality.review
B-6 comments.intent.review
```

Rule enable/disable remains whole-rule participation. Disabled rules emit no batch; enabled rules preserve their independent rule contract. Historical M0009–M0015 evidence and `.hygiene/.state/sampling.json` remain intact as legacy records and are not imported as semantic judgments. The CLI continues to perform no model/provider invocation.
