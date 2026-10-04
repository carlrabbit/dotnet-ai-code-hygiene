# Human Consumability

## Status

Planning context, not implementation authority.

## Problem

AI-assisted development changes the economics of code production.

A coding agent can generate, reshape, document, and expand a codebase faster than a human can build a durable mental model of it. Better local style helps, but does not by itself solve the resulting review and catch-up problem.

The hygiene objective therefore includes reducing the cost for a human to answer:

- where should I look?
- what is conventional here?
- what changed conceptually?
- how much surrounding code must I understand?
- is this abstraction carrying real value or merely adding structure?
- can I predict the shape of code I have not opened yet?

## Working hypothesis

BORING software patterns are useful in an AI-first repository because consistency and conventionality reduce reconstruction cost.

The benefit is not that every implementation becomes trivial. The benefit is that fewer parts of the system demand a fresh local interpretation.

Useful BORING characteristics include:

- conventional .NET/C# constructs where no special mechanism is needed;
- explicit control flow and responsibilities;
- stable project/folder conventions;
- limited numbers of equally valid ways to express the same concept;
- abstractions justified by real reuse or conceptual compression;
- local behavior visible without chasing unnecessary indirection;
- predictable naming and API shapes;
- source organization that lets a reviewer narrow the relevant context quickly.

## Important counter-pressure: volume

Lower cognitive load per unit of code does not eliminate the cost of sheer repository size.

AI can create a failure mode where every individual file is acceptable but the aggregate system becomes difficult for a human to recover into working memory.

Hygiene planning should therefore distinguish:

local readability != repository comprehensibility

The second depends on boundaries, repetition, navigability, conceptual density, and how broadly a change propagates.

## What not to optimize for

Do not equate human consumability with:

- minimizing line count;
- maximizing comments;
- mechanically eliminating all repetition;
- maximizing abstraction/reuse;
- forcing every file below an arbitrary size;
- making code visually uniform at the expense of domain clarity;
- introducing extra indirection solely to satisfy a structural metric.

These can increase rather than reduce reconstruction cost.

## Candidate signals

These are research candidates, not approved rules.

### Local reconstruction cost

Potential indicators:

- long methods containing multiple distinct conceptual stages;
- deep or irregular control flow;
- dense expressions that hide intermediate concepts;
- excessive local state mutation;
- comments needed mainly to explain unnecessarily indirect mechanics.

### Navigation cost

Potential indicators:

- trivial behavior spread through many files/types;
- layers that forward calls without adding policy;
- abstraction chains where concrete behavior is difficult to locate;
- concepts represented differently in neighboring features;
- broadly scattered changes for one small domain operation.

### Abstraction value

Possible review question:

> Does this abstraction reduce the amount of domain/mechanical knowledge a reader must hold, or does it merely move code elsewhere?

An abstraction can add files and still improve human consumption when it compresses a stable concept. Conversely, deduplication can make a system harder to understand when it joins code that only looks structurally similar.

### Change locality

A useful AI-first codebase should make many ordinary changes understandable from a bounded set of files.

Future hygiene work may benefit from signals around change fan-out or architectural locality, but such rules would require repository/multi-file context and careful false-positive control.

## Relationship to semantic review

Human-consumability questions are often poor candidates for simplistic thresholds.

For example, method length, class size, or number of files can identify candidates but rarely establish that the design is bad.

A likely pattern is:

~~~text
deterministic candidate selection
-> bounded semantic review
-> contextual remediation by the calling agent
~~~

This extends the M0003 philosophy beyond documentation quality without requiring the CLI to make fuzzy architectural judgments.

## Planning implications

When considering a new hygiene rule, ask both:

1. Does it make the code locally cleaner?
2. Does it reduce the amount of context a human must reconstruct later?

A rule that improves local appearance while increasing indirection or repository fragmentation may be counterproductive.

A future milestone focused on human consumability should begin with a small number of concrete pathologies and real repository examples rather than a generic maintainability score.

## Open questions

- Which recurring AI-generated patterns most increase human catch-up time?
- Can we identify useful deterministic proxies without turning them into arbitrary complexity metrics?
- Should the product explicitly model review candidates for architectural/human-consumability concerns?
- Can diff/repository structure provide useful locality signals cheaply?
- How should the tool distinguish useful abstraction from abstraction for its own sake?
- Which BORING conventions are universal enough to enforce, and which are project/domain choices?
