# Terminology

## Product terms

**Hygiene rule**  
An opinionated, parameterless rule evaluated by the hygiene engine. A rule can only be enabled or disabled. Alternative opinions are represented by different rule IDs rather than rule parameters.

**Hygiene set**  
The enabled ordered selection of hygiene rules. Rule order is defined by the engine. The engine does not validate whether enabled rules are mutually coherent.

**Formatter**  
A deterministic transformation that changes presentation/canonical formatting without intentionally changing program semantics.

**Normalizer**  
A deterministic, semantics-preserving structural transformation with a stricter correctness bar than a hygiene suggestion.

**Hygiene check**  
Read-only evaluation that emits findings or review candidates. A hygiene check does not modify source.

**Finding**  
A concrete rule occurrence produced by one analysis run. Findings are run-scoped and are not persistent identities.

**Review candidate**  
A finding for which deterministic analysis can identify a suspicious location/evidence but contextual judgment is required before deciding whether code should change.

**Run**  
One hygiene analysis invocation. A run has its own stable handle such as `R-8K3M`.

**Finding handle**  
A run-scoped handle such as `R-8K3M/F-42`. A bare `F-42` may only be interpreted relative to the current/latest run context; it is never globally persistent.

**Ignore decision**  
A persistent reviewed exception for one concrete smell occurrence. It has its own stable identity such as `I-17` and is stored outside source code.

**Target set**  
The source locations for which a command may emit findings or apply transformations. A narrow target set does not prevent analyzers from reading broader repository context when needed.

**Semantic anchor**  
A stable Roslyn-derived program identity, normally based on a symbol, used as part of matching a concrete smell occurrence across edits.

**Evidence fingerprint**  
A deterministic SHA-256 digest of rule-specific canonical evidence. Fingerprint material excludes irrelevant presentation details unless those details are the subject of the rule.

## Validation terms

**Process-boundary validation**  
Validation that invokes the CLI as a process and observes command parsing, exit codes, stdout/stderr, paths, and other externally visible behavior.

**Consumer-surface validation**  
Validation that installs or resolves the exact packaged artifact through its intended consumer mechanism and invokes the resulting command. This is planned for M0003, not M0001.
