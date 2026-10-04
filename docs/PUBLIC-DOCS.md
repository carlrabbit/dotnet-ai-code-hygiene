# Public Documentation

At M0003 completion README/public usage must explain:

```text
hygiene check
hygiene review expand <batch-handle>
hygiene review handoff <batch-handle> [--file <path>]
```

Clearly distinguish deterministic findings from semantic review batches.

Document:

- sample reviewer = implementer;
- expanded/handoff reviewer = frontier;
- CLI never invokes a model;
- no engine-managed semantic-review history exists;
- empty sample is valid;
- all sampled answers confidently acceptable => no escalation;
- any material failure or uncertainty => escalate;
- `review expand` is transient stdout expansion;
- `review handoff` creates a durable frontier-review request;
- default repository request path is `.hygiene/reviews/<handoff-id>/request.json`;
- `.hygiene/reviews/` is intentionally available for PR inclusion;
- explicit `--file` supports a fully decoupled reviewer via an external/shared path;
- the request embeds source files containing review subjects;
- source is not sent anywhere by the CLI;
- the CLI does not commit the request or ingest review results.

Do not claim the tool itself can determine whether German prose is good.

Formatting/normalization and installed-tool installation guidance remain M0004 work.
