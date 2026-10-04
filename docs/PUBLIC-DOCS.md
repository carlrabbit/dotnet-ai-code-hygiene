# Public Documentation

At M0003 completion README/public usage must explain:

```text
hygiene check
hygiene review expand <batch-handle>
```

Clearly distinguish deterministic findings from semantic review batches.

Document:

- sample reviewer = implementer;
- expanded reviewer = frontier;
- CLI never invokes a model;
- no semantic-review history exists;
- empty sample is valid;
- all sampled answers confidently acceptable => no expansion;
- any material failure or uncertainty => expand and route to frontier review.

Do not claim the tool itself can determine whether German prose is good.

Formatting/normalization and installed-tool installation guidance remain M0004 work.
