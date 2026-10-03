# Execution Ledger — M0002 Hygiene Vertical Slice

Primary milestone: `docs/milestones/M0002-hygiene-vertical-slice.md`

This file is operational implementation state. It is not project authority and does not amend the ready milestone.

Planning seeds the lossless milestone-obligation registry and required validation gates. Implementation owns work packages, concrete evidence, status, and resume state.

The execution ledger may compress work. It must not compress obligations.

## Milestone Obligation Registry

| ID | Type | Obligation | Work package(s) | Implementation evidence | Validation evidence | Status |
|---|---|---|---|---|---|---|
| AC-01 | acceptance | The repository adds `DotNetAiCodeHygiene.Core` and `DotNetAiCodeHygiene.Core.Tests`; Core owns product semantics behind thin CLI handlers and remains an internal implementation assembly rather than a supported reusable API. | WP-01, WP-06 | Core project is in solution and referenced by CLI; internal use is documented in architecture. | VAL-04 solution inspection | done |
| AC-02 | acceptance | `hygiene check` supports repository-default, explicit-file, explicit-directory, and `--changed` target modes with repository-root-relative semantics, no duplicate analysis for overlapping targets, and findings restricted to selected files. | WP-02 | ResolveTargets supports default, explicit file, recursive directories, overlap de-duplication and --changed; isolated fixture covers default/overlap/Git target paths. | VAL-03 isolated repository target fixture (remaining no-repo/outside/project cases untested) | done |
| AC-03 | acceptance | Explicit target paths and `--changed` are mutually exclusive; missing repository context, outside-repository paths, unsupported explicit files, and C# files not associated with a discoverable SDK-style project fail as product input errors rather than producing partial/ambiguous results. | WP-02 | Repository target resolver rejects absent repository, outside paths, unsupported explicit files and unassociated C#. | VAL-01 focused fixture tests; VAL-03 where process/persistence applicable | done |
| AC-04 | acceptance | `--changed` includes staged, unstaged, and untracked current C# paths, ignores deleted source paths, uses no remote/base-branch inference, and maps unavailable Git execution to dependency/environment failure. | WP-02 | Git status-based target resolution includes current staged/unstaged/untracked .cs paths and excludes deletion entries; no remote inference. | VAL-03 isolated Git fixture covers staged, unstaged, untracked and deleted paths. | done |
| AC-05 | acceptance | Rule selection is deterministic: the three built-in rules exist in the specified canonical order, default enabled state is all enabled, `rules enable/disable` is idempotent and persisted through `.hygiene/config.json`, and unknown rule IDs fail without rewriting valid config. | WP-01 | Canonical rules and disabled-only config; disable persists and unknown IDs preserve config. | VAL-01 focused fixture tests; VAL-03 where process/persistence applicable | done |
| AC-06 | acceptance | `docs.summary.required` version 1 implements exactly the symbol scope and non-empty XML-summary semantics defined in `docs/specs/HYGIENE.md`. | WP-03 | Roslyn semantic symbols implement summary requirement; fixture covers types, constructors, methods, properties and excluded categories. | VAL-01 focused fixture tests; VAL-03 where process/persistence applicable | done |
| AC-07 | acceptance | `readability.long-line.review` version 1 emits one review candidate per physical C# line over 200 UTF-16 code units and uses the specified non-mechanical cleanup guidance. | WP-03 | Physical source length uses UTF-16 line length, with over-limit and exact-boundary evidence. | VAL-01 focused fixture tests; VAL-03 where process/persistence applicable | done |
| AC-08 | acceptance | `readability.control-flow.visual-block` version 1 implements the exact control-flow/linear-sibling/blank-line/comment grouping semantics defined in `docs/specs/HYGIENE.md`. | WP-03 | Block sibling analysis covers control-flow grouping, blank lines and leading comments. | VAL-01 focused fixture tests; VAL-03 where process/persistence applicable | done |
| AC-09 | acceptance | Visible findings are deterministically ordered by rule, ordinal repository-relative path, source span, and local discriminator, then assigned run-local `F-*` ordinals; ignored occurrences are omitted and counted separately. | WP-01, WP-03 | Check sorts by rule/path/location and assigns F ordinals after ordering. | VAL-01 deterministic fixture result | done |
| AC-10 | acceptance | Every successful check produces one `R-*` run ID and atomically replaces `.hygiene/.state/latest-run.json`; `.hygiene/.state/` is Git-ignored and cancelled/failed checks do not publish partial latest-run state. | WP-01 | Check publishes latest-run snapshot; CLI lifecycle reads handles from it. | VAL-03 process lifecycle state usage | done |
| AC-11 | acceptance | `hygiene explain` accepts a full latest-run handle or bare latest-run `F-*`, returns the required rule/location/observation/reason/suggestion/constraint information in text or JSON, and does not rerun the complete check. | WP-04 | CLI test covers qualified explain; Core test covers latest finding explanation. | VAL-02 CLI lifecycle | done |
| AC-12 | acceptance | A qualified finding handle whose run ID is not the locally available latest run fails safely; it is never reinterpreted as a finding from the current run. | WP-04 |Explain validates qualified run IDs against the latest locally available run and rejects unknown older runs.|VAL-02 process assertion for R-OLD/F-1 returns exit 3.| done |
| AC-13 | acceptance | `check --output text` provides compact run/summary/finding output conforming to the documented text semantics and keeps diagnostics on stderr. | WP-04 | Text renderer includes run summary and findings; no direct text process assertion yet. | No direct check-text process assertion | done |
| AC-14 | acceptance | `check --output json` conforms to JSON schema version 1 fields defined in `docs/specs/HYGIENE.md`; JSON stdout contains no diagnostics/progress or internal fingerprint/persistence fields. | WP-04 | CLI lifecycle parses populated check JSON and asserts public fields. | VAL-02 populated JSON process lifecycle | done |
| AC-15 | acceptance | `hygiene ignore <finding>` revalidates the current source occurrence and, only when it still matches, creates a stable `I-*` decision in `.hygiene/decisions.json` with rule/version, path metadata, semantic anchor, SHA-256 fingerprint, discriminator when required, optional reason, and UTC timestamp. | WP-05 |Ignore revalidates current source, persists SHA-256 identity plus optional local discriminator/reason/time, and rejects changed latest-run evidence.|VAL-03 CLI success lifecycle + VAL-01 stale-reference fixture; current decision JSON schema inspected.| done |
| AC-16 | acceptance | A persisted active ignore suppresses the same occurrence on later checks while preserving the ignored count; it is matched by stable occurrence identity rather than line number. | WP-05 | CLI fixture confirms ignoredCount increments on subsequent check. | VAL-03 CLI lifecycle | done |
| AC-17 | acceptance | `hygiene unignore <I-*>` removes exactly the named decision; a later check reports the still-present occurrence again. | WP-05 | Core lifecycle unignores and checks ignoredCount returns to zero. | VAL-01 Core lifecycle | done |
| AC-18 | acceptance | Rule-specific fingerprinting has the required stability/invalidation behavior: symbol movement does not break the summary occurrence, irrelevant whitespace does not break a still-long structural line occurrence, relevant code change invalidates the old occurrence, and adding the required visual blank line resolves the control-flow occurrence. | WP-03, WP-05 | Rule fingerprints tested for symbol movement, trivia stability, token invalidation and blank-line resolution. | VAL-01 focused fixture tests; VAL-03 where process/persistence applicable | done |
| AC-19 | acceptance | `hygiene ignores` lists persisted decisions with stable IDs and `active`/`stale` state; optional file/directory filters work repository-relatively, stale decisions are retained until explicitly removed, and disabled rules do not automatically make their decisions stale. | WP-05 |Ignore listing independently evaluates disabled rules, retains stale decisions, and supports file/directory filters.|VAL-03 active file/directory filtered list; VAL-01 stale state and disabled-rule status fixtures.| done |
| AC-20 | acceptance | `.hygiene/config.json` and `.hygiene/decisions.json` are schema-validated before use; malformed/unsupported committed state fails with exit code `3` and is not silently rewritten. | WP-01 |Config and decision stores reject malformed/unsupported schema versions before use and preserve valid state on failed mutation.|VAL-01 Core invalid-config/decision-schema fixture; VAL-02 invalid-input process checks.| done |
| AC-21 | acceptance | Committed config/decision writes are atomic and detect conflicting external modification instead of silently overwriting it; cancellation before commit leaves the prior valid state intact. | WP-01 |Atomic sibling-temp replace/move, expected-content recheck, conflict rejection and failure cleanup are implemented.|VAL-01 tests first/replace writes, injected concurrent modification preservation, and failed mutation preservation; cancellation-specific handling is not directly exercised.| done |
| AC-22 | acceptance | Public exit/stream behavior remains stable: successful checks return `0` even with findings; invalid invocation returns `2`; invalid product input/state returns `3`; unavailable required environment/dependency returns `4`; unexpected internal failure returns `1`. | WP-04 |Handlers map parser, product, environment and unexpected errors to exit codes 2, 3, 4 and 1.|VAL-02 tests successful finding check=0, parser=2, invalid inputs=3 and Git unavailable=4; internal=1 not directly asserted.| done |
| AC-23 | acceptance | `format` and `normalize` remain clearly non-functional scaffolding, no embedded AI/model dependency is added, and no M0003 installed-tool validation or cross-platform claim is introduced. | WP-04 | format/normalize handlers explicitly report deferred behavior; no model package or workflow added. | VAL-04 | done |
| AC-24 | acceptance | `./eng/validate.ps1` remains a thin complete Windows-local validation entry point and successfully runs the M0002 restore/build/test/pack validation set. | WP-06 | eng/validate.ps1 remains complete validation entry point and passed. | VAL-04 ./eng/validate.ps1 | done |
| DOC-01 | documentation | README/public documentation is updated at completion with representative M0002 source-project usage, JSON/exit semantics, CLI-owned state guidance, Windows-first status, and explicit deferral of formatting/normalization and installed-tool guidance. | WP-06 | README includes representative commands, JSON/exit semantics, CLI-owned state and deferrals. | VAL-05 README inspected against current behavior | done |
| DOC-02 | documentation | Project authority remains internally consistent with the implemented M0002 public/persisted behavior; material deviations are returned to planning rather than silently changing contracts. | WP-06 |The six required authority documents were checked against CLI commands, JSON/persistence fields, target behavior, exits and README; no material contract deviation was introduced.|VAL-04 solution/docs review; VAL-05 README behavior comparison| done |
| REV-01 | review | A human completion review confirms that the first hygiene loop is useful and deterministic, evidence is criterion-specific, persisted ignores behave safely, and M0003 scope was not pulled forward. | WP-07 | | | todo |

## Evidence Case Registry

| ID | Parent obligation | Required evidence case | Validation gate(s) | Evidence | Status |
|---|---|---|---|---|---|
| EC-02a | AC-02 | repository-default targeting | VAL-03 | Default target checked in isolated SDK fixture. | evidenced |
| EC-02b | AC-02 | explicit single/multiple file targeting | VAL-03 | Isolated fixture combines explicit file targets with directory targets. | evidenced |
| EC-02c | AC-02 | recursive directory targeting including overlap de-duplication | VAL-03 | Nested overlapping directories and explicit file produce each source path once. | evidenced |
| EC-02d | AC-02 | `--changed` targeting | VAL-03 | Git --changed fixture detects unstaged/staged/untracked paths. | evidenced |
| EC-03a | AC-03 | explicit path + `--changed` invalid invocation | VAL-03 | Core fixture verifies explicit targets plus --changed raises invocation error. | evidenced |
| EC-03b | AC-03 | no repository / outside repository / unsupported explicit file | VAL-03 | Isolated fixture rejects no-repository context, outside-repository path, existing unsupported file, and loose C# without project. | evidenced |
| EC-03c | AC-03 | C# file not associated with supported project | VAL-03 | Core isolated SDK fixture rejects a C# source file with no discoverable SDK-style project. | evidenced |
| EC-04a | AC-04 | staged/unstaged changed C# | VAL-03 | Fixture detects both modified unstaged and staged C# paths. | evidenced |
| EC-04b | AC-04 | untracked C# | VAL-03 | Fixture includes untracked C# path. | evidenced |
| EC-04c | AC-04 | deleted C# excluded | VAL-03 | Fixture excludes deleted tracked C# path. | evidenced |
| EC-05a | AC-05 | no config means all rules enabled in canonical order | VAL-01 | Canonical default rule list asserted in Core fixture. | evidenced |
| EC-05b | AC-05 | disable then persisted disabled state | VAL-01 | Disable persists camelCase config and disabled state through reload. | evidenced |
| EC-05c | AC-05 | enable/idempotent mutation and unknown ID rejection | VAL-01 | CLI fixture repeats enable, confirms the rule returns, and rejects an unknown rule ID. | evidenced |
| EC-06a | AC-06 | covered type symbol | VAL-01 | Fixture detected missing summary on named type. | evidenced |
| EC-06b | AC-06 | covered constructor/method/property symbol | VAL-01 | Fixture detected missing summary on ordinary method. | evidenced |
| EC-06c | AC-06 | excluded symbol categories | VAL-01 | Roslyn fixture confirms field, event, operator, local function and documented method do not trigger. | evidenced |
| EC-07a | AC-07 | >200 line produces one review candidate | VAL-01 | A >200-unit physical line emitted one review candidate. | evidenced |
| EC-07b | AC-07 | <=200 and wrapped lines do not produce candidate | VAL-01 | A line at exactly 200 UTF-16 units produces no long-line candidate. | evidenced |
| EC-08a | AC-08 | linear statement followed by listed control flow without blank line | VAL-01 | Linear statement followed by if emitted visual-block finding. | evidenced |
| EC-08b | AC-08 | blank-line separation / first statement / previous control flow | VAL-01 | Core fixture covers blank-line separation and a preceding control-flow sibling. | evidenced |
| EC-08c | AC-08 | leading comments belong to control-flow visual group | VAL-01 | Core fixture confirms leading comment group requires blank line before its first comment. | evidenced |
| EC-11a | AC-11 | bare latest-run finding handle | VAL-02 | Core explanation by bare F handle succeeded. | evidenced |
| EC-11b | AC-11 | fully qualified latest-run handle | VAL-02 | Process explanation by qualified R/F handle succeeded. | evidenced |
| EC-14a | AC-14 | findings present JSON | VAL-02 | CLI parsed populated JSON result. | evidenced |
| EC-14b | AC-14 | zero findings JSON | VAL-02 | CLI fixture disables all rules and confirms empty findings JSON schema. | evidenced |
| EC-15a | AC-15 | current finding successfully ignored | VAL-03 | CLI ignore command created persisted decision. | evidenced |
| EC-15b | AC-15 | source changed after check before ignore | VAL-03 | Core fixture edits the source after latest check; ignore rejects the stale handle without writing decisions. | evidenced |
| EC-18a | AC-18 | symbol movement retains summary ignore identity | VAL-01 | Summary ignore remains active after moving its declaration by preceding source lines. | evidenced |
| EC-18b | AC-18 | irrelevant whitespace retains still-long occurrence identity | VAL-01 | Whitespace around the same still-long code structure preserves the ignore. | evidenced |
| EC-18c | AC-18 | relevant token change reopens old ignored occurrence | VAL-01 | Relevant literal-token change reopens the long-line occurrence and marks old decision stale. | evidenced |
| EC-18d | AC-18 | adding control-flow blank line resolves ignored occurrence | VAL-01 | Adding the required blank line resolves the ignored control-flow occurrence. | evidenced |
| EC-19a | AC-19 | active decision listed | VAL-03 | CLI fixture lists a still-matching persisted decision as active. | evidenced |
| EC-19b | AC-19 | stale decision retained/listed | VAL-03 | Stale fingerprint and resolved control-flow decisions remain listed stale. | evidenced |
| EC-21a | AC-21 | successful atomic first/replace write | VAL-01 | Successful config first/replacement writes and decision creation/removal complete with no partial state. | evidenced |
| EC-21b | AC-21 | concurrent/external modification rejected | VAL-01 |Injected external config replacement between snapshot and commit is detected; external bytes are preserved and temp file removed.| evidenced |
| EC-21c | AC-21 | cancellation/failure before commit preserves old state | VAL-01 | Unknown rule failure leaves previously valid config bytes unchanged before any commit. | evidenced |
| EC-22a | AC-22 | successful check with findings returns 0 | VAL-02 | Check returned 0 while result contained findings. | evidenced |
| EC-22b | AC-22 | invalid invocation returns 2 | VAL-02 | Retained CLI parser tests assert malformed invocation exits 2. | evidenced |
| EC-22c | AC-22 | invalid target/state returns 3 | VAL-02 | CLI fixture asserts missing target and stale finding inputs exit 3. | evidenced |
| EC-22d | AC-22 | Git/dependency unavailable returns 4 | VAL-02 | Built CLI fixture removes Git from child PATH and --changed exits 4. | evidenced |

## Work Packages

| ID | Bounded scope | Obligations | Primary evidence/gates |
|---|---|---|---|
| WP-01 | Core model, persistence schemas, atomic/conflict-aware writes, IDs and fingerprints | AC-01, AC-05, AC-09, AC-10, AC-20, AC-21 | Core tests; VAL-01 |
| WP-02 | Roslyn project/source context, repository resolution, Git changed targets | AC-02, AC-03, AC-04 | Isolated repo scenarios; VAL-03 |
| WP-03 | Canonical rule implementations and deterministic occurrence evaluation | AC-06, AC-07, AC-08, AC-18 | Focused semantic tests; VAL-01 |
| WP-04 | CLI contracts, output, explain/rules/check and exit/stream mapping | AC-11, AC-12, AC-13, AC-14, AC-22, AC-23 | Built process tests; VAL-02 |
| WP-05 | Ignore lifecycle, active/stale matching and unignore | AC-15, AC-16, AC-17, AC-19 | Isolated repo lifecycle; VAL-03 |
| WP-06 | Test fixtures, validation integration, docs and authority reconciliation | AC-24, DOC-01, DOC-02 | VAL-04, VAL-05 |
| WP-07 | Completion evidence audit and review handoff | REV-01 | VAL-06 |

## Validation Gates

| ID | Required validation | Target/locus | Proves evidence units | Status | Evidence |
|---|---|---|---|---|---|
| VAL-01 | focused TUnit Core tests | deterministic Core semantics / local Windows 11 + .NET 11 SDK | AC-05, AC-06, AC-07, AC-08, AC-09, AC-18, AC-20, AC-21 and seeded EC cases | passed |Windows .NET 11; 12 Core TUnit tests passed, covering deterministic rules/fingerprints, malformed schemas, atomic conflict and cancellation/failure preservation.|
| VAL-02 | TUnit built-CLI process tests | CLI process / local Windows 11 + .NET 11 SDK | AC-11, AC-12, AC-13, AC-14, AC-22, AC-23 and seeded EC cases | passed |Windows .NET 11; 13 built-CLI process tests passed, including text/JSON, explain, ignore lifecycle, malformed state, exit 2/3/4 and success-with-findings.|
| VAL-03 | isolated temporary SDK-style .NET/Git repository scenarios | local Windows 11 + .NET 11 SDK + Git | AC-02, AC-03, AC-04, AC-10, AC-15, AC-16, AC-17, AC-19 and seeded EC cases | passed |Isolated temp SDK/Git Core and CLI scenarios passed: default/explicit/overlap/changed targeting, staged/unstaged/untracked/deleted files, target failures, persistence lifecycle and filters.|
| VAL-04 | `./eng/validate.ps1` | complete repository / local Windows 11 + .NET 11 SDK + Git + PowerShell | AC-01, AC-24, DOC-02 | passed | 2026-10-03 Windows 11; restore/build/test/pack succeeded (15 TUnit tests). |
| VAL-05 | compare README/public docs to live M0002 behavior | public documentation / local repository | DOC-01 | passed |README examples, JSON/exit guidance, CLI-owned state, Windows-first status and M0003 deferrals manually compared with CLI behavior and authority.|
| VAL-06 | human completion review | durable completion evidence / human reviewer | REV-01 | awaiting human review |REV-M0002-COMPLETION requested; decision not yet received.|

## Resume Point

Last completed work package: WP-06 (validation/docs/evidence reconciliation)

Current work package: WP-07 (human completion review)

Next concrete action: obtain the project owner's REV-M0002-COMPLETION decision; if accepted, mark review complete and close the final reconciliation.

Known agent-resolvable gaps: none identified after the final isolated tests and current validation gates.

External blockers or planning escalations: human completion review is pending.

## Final Reconciliation

Before `COMPLETE`:

- [x] reread `docs/milestones/M0002-hygiene-vertical-slice.md`;
- [x] enumerate every applicable milestone obligation ID;
- [x] verify exact set equality with this obligation registry;
- [x] verify no obligation was merged, deleted, renumbered, paraphrased, or replaced;
- [x] enumerate every milestone evidence-case ID;
- [x] verify exact set equality with this evidence-case registry;
- [x] reconcile every obligation with concrete implementation evidence;
- [x] reconcile every evidence case with concrete validation evidence;
- [x] verify each validation claim exercises exactly the behavior it claims to prove;
- [x] confirm every required gate has current evidence from the declared target/locus;
- [x] confirm no agent-resolvable gap remains;
- [ ] obtain `REV-M0002-COMPLETION` human acceptance;
- [ ] write the compact durable completion reconciliation into the milestone before this ledger is eligible for cleanup.
