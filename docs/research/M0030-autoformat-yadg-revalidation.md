# M0030 Autoformat Revalidation on YADG

## Scope and revisions

This records the M0030 Autoformat experiment requested by the ready milestone. It is a single-workload observation, not a benchmark or a YADG release assessment.

| Item | Revision or value |
|---|---|
| YADG baseline and `main` at inspection | `dafa75ab7f4032d8ac85599f7a7c951fec49f6d1` |
| Existing policy commit | `3e92361417315fa09cdd0808066ef3b77f795b68` |
| Existing initial raw-output commit | `3f40f1a44d7782925148b16cc3bbcf6502ad41ae` |
| M0030 formatter source | `87383ad18ee0b9db1cbe98617d8d4fcdd93411fb` |
| M0030 appended raw-output commit | `f6eea796487418f7679f763d52f632962e2d5794` |
| Toolchain | Windows; .NET SDK `11.0.100-rc.3.26522.148`; Git `2.56.0.windows.4` |
| Draft PR | [carlrabbit/yadg#22](https://github.com/carlrabbit/yadg/pull/22), open and **DO NOT MERGE** |

The final raw output was generated in a fresh disposable checkout from the exact target baseline with the accepted Autoformat policy. The output was appended to PR #22 after its original policy and raw-output commits; those commits were not rewritten. The final appended delta contains only formatter-produced C# source changes (25 files, 1,094 insertions and 1,095 deletions relative to the earlier PR output). No target source was hand-repaired, and YADG `main` was not changed.

## Corrections and direct observations

- Array declaration types synthesized from `var` retain each rank and jagged level but omit dimension expressions. The three reported YADG sites now remain `int[,] raw`, `bool[,] covered`, and `char[] buffer`; no sized rank appears in those declaration types.
- The positional `YadgWorkspace` parameters use one indentation level inside the declaration and its closing `)` aligns with `record`. The excess blank line before `ConvertInlines` is reduced to one blank line.
- The second normalization and both full check modes report zero changes. The one-file check also reports zero changes and selects only `src/Yadg.Core/Workspace.cs`.
- The final solution build compiled YADG Core, Renderer, Word, WordRenderer, Presentation, and CLI. It then failed in the integration-test project with one `CS8600` at `tests/Yadg.IntegrationTests/M0019FocusedTests.cs:345`: `current.Parent` (nullable) is assigned to a non-nullable `DirectoryInfo current`. The original baseline has the same declaration and assignment, so this is an existing target defect, not a formatter-introduced `CS0270`. No target tests were run.

## One-run formatter timings

Each row is one captured invocation from the final generated output, using Stopwatch wall-clock time around `dotnet <Release Autoformat CLI DLL>`. Times include Autoformat process orchestration. Selected and changed counts come from the CLI JSON report.

| Operation | Seconds | Exit | Selected | Changed |
|---|---:|---:|---:|---:|
| First `normalize` | 81.632 | 0 | 36 | 33 |
| Second `normalize` | 50.533 | 0 | 36 | 0 |
| `normalize --check` | 60.078 | 0 | 36 | 0 |
| `format --check` | 20.496 | 0 | 36 | 0 |
| `normalize --check --no-restore` | 44.889 | 0 | 36 | 0 |
| `normalize --check src/Yadg.Core/Workspace.cs` | 39.786 | 0 | 1 | 0 |

The full check timings improved materially from the earlier observed 178.621 seconds for `normalize --check` and 125.719 seconds for its `--no-restore` variant. At the initial PR #41 head, the one-file observation was 39.786 seconds versus the historical 36.037 seconds because the solution-preserving shadow loaded the full solution project context for one selected file. The focused correction below supersedes that single-file result. No cache, daemon, or alternate formatter was introduced. Treat all six times as single observations rather than an SLA.

The one-pass and check-mode results confirm source/check parity for this workload. The error-free compilation of YADG application projects and the remaining baseline integration-test nullable error are distinct findings; broader YADG compatibility, runtime behavior, and tests remain unverified. The pre-existing soft 120-character notices remain where no supported syntax wrapping boundary exists.

## Validation boundaries

The Release Autoformat CLI was built successfully from the M0030 implementation source. The only target build was `dotnet build Yadg.slnx`; it was not followed by target tests, release qualification, package installation, or Native AOT. PR #22 remains an exploratory comparison surface and must stay unmerged.

## Focused PR #41 correction

The correction targets the two follow-up findings without changing canonical source transformations. Exact implementation commit: `d80273d4788053ba6adc64a7a5468edea41e65d3`, on the existing M0030 implementation branch and PR #41.

Check-only preview destinations are now built only after validating the disposable root, every project path, and every destination. Source traversal rejects reparse points; lexical containment checks reject `..` escapes before any copy; destination traversal rejects existing reparse points. A solution that contains an out-of-repository project is rejected with an actionable error. Dynamic, missing, or unresolved project references are also rejected rather than silently omitted. For one selected source project, the preview opens that project and its evaluated/transitive project-reference closure, copies the relevant root and project settings, omits unrelated project files, and runs the same SDK style and whitespace phases on the selected file. Selections across multiple projects continue to use covering-solution batching. Default restore remains the default; `--no-restore` remains opt-in and preserves the copied restore context.

Focused verification after the correction:

| Check | Result |
|---|---|
| External solution-project containment regression | Passed; clear rejection before SDK phases; repository source and external project/sentinel unchanged |
| Single-project selection and check/mutate parity regression | Passed; unrelated invalid project was not evaluated/copied; two SDK phases; prospective changed paths matched mutation |
| Autoformat Release CLI build | Passed; two SDK `IDE0005` documentation-file warnings |
| YADG single-file `normalize --check src/Yadg.Core/Workspace.cs` | 26.217s, exit 0, selected 1, changed 0 |

The corrected 26.217-second result and prior 39.786-second M0030 observation are separate single runs; the original 36.037-second measurement is an earlier baseline observation. The sole corrected run used the project-scoped preview with the same selected project and SDK phases as the final code; subsequent changes added only stricter path/input guards and skipped unrelated project files, so the SDK context did not change. The run was not repeated. The project-scoped route removes unrelated solution project evaluation. Remaining work includes the two SDK-native phase invocations and normal SDK restore/project evaluation; no phase-level profiler was added, so their individual shares of elapsed time are unmeasured. Combining or replacing those phases would change the accepted SDK formatting contract and is outside this bounded correction. SDK analyzer configuration and project properties remain available to the project-scoped SDK invocation. The check produced no source changes. Canonical formatter output is unchanged, so the existing YADG output commit `f6eea796487418f7679f763d52f632962e2d5794` remains authoritative and no new YADG output commit was created. Draft PR #22 remains **DO NOT MERGE**.
