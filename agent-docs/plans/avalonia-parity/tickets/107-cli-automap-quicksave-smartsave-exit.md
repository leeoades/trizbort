# 107 — CLI automap, quick save, smart save and exit flags

Matrix rows: A018, A019, E015 (smartsave/transcript part)          Priority: P?          Size: M
Depends on: 104, 105, 110, 011, 012                                Suggested tier: mid

## What
`--automap <transcript>` runs the automapper non-interactively on the loaded map; `--quicksave <file>` saves the map to that file afterwards; `--smartsave` runs Smart Save (110) silently; `--exit` closes the app when all of that is done. `--name` is dead in the reference: parse it, ignore it, document it.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `MainForm_Load` (lines ~164–243: automap block with `IsDirty = false`, quicksave, smartsave, exit)
- `src/Trizbort.Core/Domain/Application/CommandLineOptions.cs`
- `src/Trizbort.Avalonia/ViewModels/MainWindowViewModel.cs` — `StartAutomapAsync`

## Implement
1. Core: `HeadlessAutomap.RunAsync(Project, transcriptPath, AutomapSettings, IEditorPrompts)` running the engine to completion with the non-interactive prompts from ticket 011 (defaults: keep room 1, first match) and an `IAutomapCanvas` that needs no UI. If no suitable canvas exists, implement it on top of Core's `AutomapCanvas`.
2. Avalonia: `StartupActions` order: load file (104), automap, export flags (105), quicksave, smartsave, exit. Each step in try/catch; failures go to stderr and set `App.ExitCode = 1`; `--exit` still runs.
3. After headless automap, mark the project not dirty (as the reference does) so `--exit` does not prompt.
4. `--exit` must skip the unsaved-changes prompt.

## Tests
- Core: `src/Trizbort.Core.Tests/HeadlessAutomapTests.cs` — run on a small transcript fixture, assert room count; scripted prompt defaults used.
- Avalonia: `StartupTests.cs` — order of actions with fakes; exit skips prompt.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Matrix rows A018, A019 updated; committed as `parity(107): CLI automap/save/exit`.

## Out of scope
- Interactive automap UI (011/012, 127).
