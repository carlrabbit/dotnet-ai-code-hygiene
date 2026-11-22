# Base Engineering Guide

## Core principles

```text
eng/ may expose a stable engineering API; direct build/test scripts suffice when simple.
Platform launchers stay thin.
Complex orchestration lives in tested application code.
Validation depth is independent of target and execution locus.
Milestone validation is risk-proportionate; final consumer qualification belongs to releases.
Validation may be resumable only when its cost justifies the machinery.
Explicitly required human review is a repository-local milestone completion gate.
```

## Human review

Ordinary PR/code review does not require formal review records or commands. When a milestone explicitly requires a human acceptance decision, an existing PR approval or concise recorded decision can suffice if the subject, evidence, real reviewer, and outcome are clear.

The repository-local `.review/` and `review-*` commands are an **optional advanced system**, appropriate only when frequent multi-stage approvals, offline workflows, or an audit contract justify them. Do not create review-list/show/request/record/reopen/check launchers for a single approval. Existing projects intentionally using an advanced review system may retain its contract.

A required decision must be obtained from a real reviewer; missing approval cannot be assumed. Completed milestone approvals are historical and do not become stale simply because unrelated later commits occurred. See `meta/HUMAN-REVIEW-MODEL.md`.
