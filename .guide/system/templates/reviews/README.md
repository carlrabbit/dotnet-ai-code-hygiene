# Optional structured human-review templates

`review-request.md` and `review-record.md` support an **advanced** repository-local review subsystem only. Their presence in this guide does not require product repositories to create `.review/`, implement `review-*` commands, or track acceptance as a separate infrastructure project.

For occasional explicitly required human decisions, use an existing PR approval or a concise milestone-local record as described in `meta/HUMAN-REVIEW-MODEL.md`. Ordinary implementation and correction review uses `templates/prompts/review-milestone.md`.
