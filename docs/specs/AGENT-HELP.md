# Agent Help Contract

The only supported command namespace is `dotnet hygiene`. The no-argument command is the ordinary agent entrypoint; `dotnet hygiene help --agent` provides stable CLI-owned workflow guidance without provider-specific prompting.

Guidance describes bootstrap and explicit external baseline approval, trusted gate selection, repository-wide deterministic formatting and normalization, findings, selected-only grouped review work, independent rule questions and acceptances, escalation, stale-work revalidation, reruns after remediation, terminal status semantics, practical manual diagnostics, release upgrades, and the fact that the CLI does not invoke a model/provider.

Acceptance is a caller assertion that every fixed question passes and no escalation condition applies. Check rendering, expansion, and handoff do not consume review tickets. Negative or uncertain summary, test-quality, and comment-intent work goes to the frontier; BORINGness and type-responsibility concerns go to the planner. Comment intent is contextual: legal, administrative, and ordinary comments remain eligible; a comment is not defective merely because it does not narrate implementation. The visual-block review candidate indicates an opportunity, does not mandate a blank line, and is not automatically rewritten.

Manual diagnostics remain under `dotnet hygiene check`, `rules`, `explain`, `format`, `normalize`, and `review`. `--review-all` remains bounded. `--yolo-all` is deferred to M0019. Root and subcommand help accurately describe the installed command and lifecycle. There is no pre-1.0 compatibility obligation for the old bare `hygiene` command or former CLI layout.
