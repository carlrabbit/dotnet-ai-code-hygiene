# Migration — Guide System v0.1.0 to v0.2.0

Introduce the prompt template library and the `.guide-sync/` deferred documentation synchronization queue.

Conditional product-repository changes:

- add `.guide-sync/pending/.gitkeep` or create the folder when the first hint is added;
- update `AGENTS.md` to tell ordinary implementation agents to ignore `.guide-profile.json` and `.guide-sync/`;
- add pending hint files under `.guide-sync/pending/` when planning packages defer documentation work.
