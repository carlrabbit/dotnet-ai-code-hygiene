# Migration — Guide System v0.5.0 to v0.5.1

## Historical scope

This document records the v0.5.0 to v0.5.1 repair. The prompt filenames in the historical replacement list are the names that existed at v0.5.1 and are not current v0.7.0 top-level prompt paths.

Current equivalents relevant to normal work are:

```text
templates/prompts/update-guide-system.md
templates/prompts/execute-milestone.md
templates/prompts/plan-milestone.md
```

The dedicated repair prompt now lives at:

```text
templates/prompts/version-specific/repair-v0.5.0-shortened-prompt-application.md
```

## Purpose

Restore complete prompt templates that were unintentionally shortened in the v0.5.0 update package.

## Scope

This migration affected the guide repository itself and, conditionally, product repositories where the shortened prompts were already used.

## Required guide-repository changes at v0.5.1

The following historical prompt templates were restored to complete v0.5.1 versions:

```text
templates/prompts/update-to-latest-guide-system.md
templates/prompts/execute-planned-milestone.md
templates/prompts/milestone-ai-executed-human-reviewed.md
templates/prompts/milestone-ai-executed-broad.md
templates/prompts/milestone-engineering-migration.md
```

The v0.5.1 migration also updated:

```text
README.md
CHANGELOG.md
templates/PROMPTS.md
templates/guide-profile/guide-profile.json
```

and added the repair migration plus repair prompt.

## Conditional product-repository migration

A product repository needs corrective assessment only if one of the shortened v0.5.0 prompts was already used to create or apply a migration or milestone package.

Potentially affected outcomes include:

- missing repository inspection;
- incomplete authority-document selection;
- missing `.guide-sync/pending/` hints;
- missing provider/consumer classification;
- missing human-review requirements;
- missing resumable-validation requirements;
- overly broad or underspecified implementation scope;
- missing disconnected execution handoff;
- incomplete chat handoff.

Use the dedicated version-specific repair prompt rather than applying a fixed patch blindly.

## Validation

For historical validation, all five affected v0.5.1 prompts contained the restored operational sections and the guide profile version was `0.5.1`.

For current work, do not look for those historical top-level filenames. Use `templates/PROMPTS.md` and the current paths listed above.
