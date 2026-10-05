# Sampling Rules

## Status

Planning context, not implementation authority.

This document preserves the current direction for statistically bounded hygiene rules that inspect only part of an eligible population. The goal is not to make sampling universal. It is to give future rules sound, explicit ways to trade exhaustive verification for quantified uncertainty without requiring repository-scale binary indexes.

Concrete APIs, persistence formats, thresholds, priors, and rule-specific policies remain unresolved.

## Product pressure

Some useful hygiene rules are expensive because their subjects are numerous or require semantic/model judgment.

Examples include:

- comment quality;
- documentation quality;
- structural readability judgments;
- repetitive local patterns where exhaustive semantic review would spend model effort with little additional value.

A fixed rule such as `min(newSubjects, 5)` is simple but ties inspection effort to arbitrary execution boundaries such as commits or batches. It also gives no principled treatment to:

- previously inspected subjects whose evidence becomes stale;
- unchanged subjects that have never been inspected;
- different subject risks;
- population churn;
- uncertainty after few observations.

The desired direction is continuous statistical inspection. Sampling pressure should derive from what is known about the subjects or population, not from an arbitrary number of subjects per invocation.

## Design principle

Spend persistent state in proportion to the strength of the guarantee a rule actually needs.

Two sampling models should be supported conceptually:

1. subject-state sampling;
2. aggregate population sampling, optionally refined into cohorts.

They are complementary. Aggregate sampling is not merely a storage optimization for subject sampling, and subject sampling is not simply a more accurate aggregate model. They make different claims.

## Model 1: subject-state sampling

### Intent

Track inspection state for each eligible subject when individual coverage matters and the population is small enough that subject identity is practical.

Likely fits include relatively sparse, long-lived, individually important populations such as public API elements or architectural relationships.

### Conceptual state

~~~text
subject identity/fingerprint
last inspection
last outcome
accumulated hazard
next inspection threshold
~~~

The exact identity mechanism is unresolved. A subject only needs identity stable enough for the rule's intended guarantee.

### Hazard and decay

After a successful inspection, confidence in that evidence should decay as relevant time or change accumulates.

Let a subject accumulate integrated hazard:

$$
H_i = H_{baseline} + H_{age} + H_{change} + H_{risk} + ...
$$

A simple confidence interpretation is:

$$
C_i = e^{-H_i}
$$

and the probability that inspection has been triggered by accumulated hazard $H_i$ is:

$$
P(T_i \le H_i) = 1 - e^{-H_i}
$$

This permits policy statements in probabilities rather than arbitrary sample counts. For example, $H=3$ implies only $e^{-3}$, about 5%, probability that a subject has not yet crossed a randomized inspection threshold.

### Random threshold formulation

Repeated random decisions on every repository run are unnecessary.

For each subject, draw:

$$
T_i \sim Exp(1)
$$

Accumulate hazard until:

$$
H_i \ge T_i
$$

then inspect, reset accumulated hazard, and draw a new threshold.

This is equivalent to using an exponential waiting threshold over integrated hazard. Operationally, the rule can maintain a randomized next-inspection point rather than resampling the subject independently on every invocation.

### Useful hazard inputs

A future rule may derive hazard from signals such as:

- non-zero baseline aging so unchanged subjects are not permanently exempt;
- subject creation;
- direct modification;
- enclosing-scope modification;
- prior failures;
- structural or semantic risk;
- rule-specific risk features.

The framework should own the generic statistical mechanism. Rules should contribute interpretable risk signals rather than reimplementing random selection.

### Guarantee

Subject-state sampling can support claims about individual subjects, for example:

> Every eligible subject accumulates inspection pressure, and the probability that a subject remains uninspected after integrated hazard H is bounded by e^-H.

The guarantee depends on correct persistence, identity, hazard accumulation, and non-zero baseline hazard where eventual reinspection is required.

### Cost

State grows approximately with the number of tracked subjects.

That is acceptable for ordinary repositories and appropriately chosen rules, but it is the wrong default for very dense populations merely to obtain approximate quality surveillance.

## Model 2: aggregate population sampling

### Intent

Avoid persistent identity for every subject. Instead, maintain a statistical model for a population attached to a larger structural scope such as a type, file, namespace, project, or another stable code unit.

A useful example is comment quality per user-defined type.

A type can expose cheap structural facts such as:

~~~text
type age
time since change
public/internal/private member counts
method/property/field/constructor counts
subject count for the rule
previous sample count
previous pass/fail observations
~~~

The system does not need to remember which specific comments were inspected.

### Aggregate hazard mass

For scope $C$, let:

- $N_C$ be the number of eligible subjects;
- $X_C$ be cheap features of the scope;
- $E[h_i | X_C]$ be the modeled expected hazard of a subject in the scope.

Define aggregate hazard mass:

$$
H_C = N_C \cdot E[h_i | X_C]
$$

When selecting work, sample a scope proportional to its hazard mass:

$$
P(C) = \frac{H_C}{\sum_j H_j}
$$

Then select a concrete subject inside that scope when the expensive inspection is actually performed.

If subjects are sampled uniformly inside a homogeneous scope:

$$
P(i) = P(C) \cdot \frac{1}{N_C}
$$

so subject selection is proportional to the modeled per-subject hazard without keeping persistent state for each subject.

This is a two-stage/clustered sampling shape: first choose a structural scope, then choose an actual subject.

### Optional cohorts

A single scope-level model can be too coarse.

For example, a class may contain old unchanged comments, recently affected comments, and new comments. Treating all of them as equally risky loses useful information.

Optional cohorts provide a bounded refinement:

~~~text
comments
  new
  recently-affected
  established
~~~

Then:

$$
H_C = \sum_k N_{C,k} \cdot E[h | X_C, k]
$$

Sampling can select:

~~~text
scope -> cohort -> current concrete subject
~~~

Cohorts should remain few, rule-defined, and semantically meaningful. They are an 80/20 mechanism, not a disguised subject index.

### Statistical quality model

Aggregate sampling can maintain evidence about population quality rather than individual coverage.

For binary outcomes such as acceptable/unacceptable, a Beta-Binomial model is a useful candidate:

$$
p_C \sim Beta(\alpha, \beta)
$$

After observing failures $f$ and passes $s$:

$$
p_C | data \sim Beta(\alpha + f, \beta + s)
$$

where $p_C$ denotes the population defect probability under that model.

This naturally distinguishes:

- zero failures after one observation;
- zero failures after forty observations.

Both have an observed defect rate of zero, but very different uncertainty.

The aggregate hazard can therefore incorporate both estimated defect risk and epistemic uncertainty.

The exact prior and update policy must be rule-appropriate and must not be standardized prematurely.

### Unequal-probability sampling

Risk-weighted sampling intentionally oversamples suspicious scopes or cohorts.

A naive sampled failure rate is therefore not automatically an unbiased estimate of repository-wide quality.

If a rule wants a design-based population estimate, inclusion probabilities must be known and accounted for, for example with inverse-probability weighting such as Horvitz-Thompson estimation.

Alternatively, a rule may use an explicit model-based/Bayesian estimate whose assumptions are documented.

The framework must not report a risk-biased sample proportion as if it were an unbiased repository proportion.

### Guarantee

Aggregate sampling supports population-level claims, not individual coverage claims.

Possible outputs include:

- estimated violation probability;
- posterior/interval uncertainty;
- remaining inspection risk mass;
- confidence that a population-level target is satisfied.

It cannot truthfully claim that a particular untracked subject was inspected recently or that every individual subject receives a bounded revisit time.

That distinction must remain visible in rule semantics and user-facing explanations.

### Cost

Persistent state grows approximately with the number of scopes and optional cohorts, not the number of subjects.

This makes the model attractive for dense populations such as comments and other fine-grained semantic review subjects.

## Comparison

| Property | Subject-state sampling | Aggregate population sampling |
| --- | --- | --- |
| Persistent identity | Per subject | Per scope/cohort |
| Main claim | Individual inspection/reinspection probability | Population quality/risk estimate |
| State cost | Proportional to subjects | Proportional to scopes/cohorts |
| Handles individual history | Yes | No |
| Handles dense populations cheaply | Less well | Yes |
| Optional risk weighting | Yes | Yes |
| Requires correction/modeling for population estimates | If estimating population from biased samples | Yes |
| Natural fit | Sparse/high-value subjects | Dense/fuzzy/expensive subjects |

A future rule should choose the cheapest model that preserves the guarantee it genuinely needs.

## Relationship to repository structure

The likely pipeline shape is:

~~~text
cheap structural extraction
-> scope summary
-> population/cohort state
-> hazard / uncertainty calculation
-> sampling scheduler
-> selected scope/cohort
-> materialize current concrete subject
-> deterministic or semantic inspection
-> update statistical state
~~~

For subject-state sampling, the population/cohort state is replaced by subject-level state and hazard.

Structural summaries should be cheap to recompute when the enclosing scope changes. If a scope fingerprint is unchanged, only time-dependent statistical state may need to advance.

This direction complements the analysis-session/pipeline research in docs/research/RULE-APPLICATION-ARCHITECTURE.md.

## Persistence direction

Normal repositories are the primary design target.

Sampling should not require a database or opaque binary index merely to function. Persisted state should remain small, inspectable, replaceable, and regenerable where practical.

Possible future storage may be repository-local structured text or another similarly transparent representation. The format is intentionally unresolved.

A very large enterprise monorepo may justify an alternate scalable state provider, but that should be an extension point rather than the baseline product architecture. Microsoft-scale storage concerns should not force ordinary repositories to operate an analysis database.

## Soundness requirements

"Statistical" must not become a label for arbitrary random heuristics.

A sampling rule should document at least:

- its target population;
- its sampling unit;
- its structural scope;
- which sampling model it uses;
- the inclusion/selection mechanism;
- risk features and why they affect hazard;
- whether it makes individual or population claims;
- how aging/change affect previous evidence;
- what assumptions make its estimate valid;
- how uncertainty is represented;
- what happens when state is absent, stale, or invalid;
- deterministic/reproducibility requirements where relevant.

Rules may still use pragmatic approximations. The important requirement is that the approximation and the resulting claim match.

## 80/20 direction

The desired product behavior is not exhaustive formal verification.

The useful niche is a compact set of high-value, statistically defensible heuristics that catch disproportionate maintenance debt while keeping normal repository operation cheap and understandable.

Prefer:

- interpretable models over elaborate learned models;
- a few strong structural features over large feature vectors;
- bounded cohorts over detailed subject catalogs;
- explicit uncertainty over fake precision;
- transparent state over hidden databases;
- empirically calibratable policies over magic sample counts.

The system should be sophisticated in its guarantees without becoming operationally heavy.

## Candidate rule examples

### Comment quality

Likely default:

~~~text
model: aggregate population
scope: user-defined type
subject: comment
cohorts: optional new / affected / established
inspection: semantic/model review
~~~

Rationale: comments can be numerous, individual identity is low-value, and quality judgment is fuzzy/expensive.

### Public API documentation

Likely candidate:

~~~text
model: subject-state
subject: public API member
inspection: deterministic presence checks and/or semantic review
~~~

Rationale: population is comparatively sparse and individual omissions matter.

These are examples, not commitments.

## Open questions

- What rule metadata is needed to declare a sampling population without exposing a general-purpose configuration language?
- Which hazard functions are simple enough to explain and calibrate?
- Should elapsed wall-clock time, repository commits, subject changes, or some combination define aging?
- Which subject identities remain stable enough for subject-state sampling across ordinary refactors?
- Which cohort definitions provide meaningful value without becoming detailed indexing?
- How should sampled semantic-review work interact with changed-file reporting scope?
- Should population estimators be framework-provided primitives or selected by each rule?
- How should a rule expose uncertainty in CLI/agent output without implying stronger guarantees than it has?
- When should statistical state be invalidated after rule-version or model changes?
- What benchmark repositories are sufficient to calibrate default hazard and prior parameters?
