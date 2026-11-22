# Documentation Sync Model

This model defines how deferred documentation work is captured, transported, consumed, and cleaned up when planning, implementation, and documentation synchronization are performed by disconnected agents.

Use:

```text
.guide-sync/pending/
```

to store pending hints that must survive beyond the current chat.

`.guide-sync/` is repository-local coordination metadata, not authoritative project documentation.

Ordinary implementation agents ignore `.guide-sync/` unless explicitly assigned guide migration, documentation synchronization, release readiness, or another task that says to read it.

Documentation-sync agents read `.guide-sync/pending/`, resolve or narrow hints, and delete resolved hint files only when completion criteria are satisfied.
