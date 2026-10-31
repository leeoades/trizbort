# 126 — Automap start/stop/status UX gaps and single-step

Matrix rows: E037, E038, E040, E041 (single-step), M063          Priority: P?          Size: M
Depends on: 011, 012, 009, 010, 055          Suggested tier: mid

## What
The automap command reads "Start Automap..." when stopped and "Stop Automap" while running (separate catalogue commands with proper CanExecute, as in the reference), status text is event-driven rather than a 200 ms poll, and the debug single-step option (F11 in the reference start dialog) is available behind `--debug` (108). "Pause" does not exist in the reference (OQ-E8): not added; Lee to decide.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `AutomapStartMenuItem_Click`, `AutomapStopMenuItem_Click`, `UpdateCommandUI` (lines ~1245–1249)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — F11 handler in `OnKeyDown` (lines ~1399–1409)
- `src/Trizbort.Core/Automap/Automap.cs` — `SingleStep`, `Step()`, `RunToCompletion` (lines 25–40, 842–850), status-text changes (lines 821, 882, 897, 935, 962)
- `src/Trizbort.Avalonia/ViewModels/MainWindowViewModel.cs` — `ToggleAutomap`/`StartAutomapAsync`/`StopAutomap` (lines 392–456), 200 ms polling timer (441–445)

## Implement
1. Core: `Automap` raises `StatusChanged(string)` (event) wherever it sets its status text; `EditorSession` re-exposes it. Replace the poll timer with the event (marshal to UI thread in Avalonia).
2. Catalogue: `automap.start` (CanExecute = not running), `automap.stop` (CanExecute = running). Remove the single toggle command; keep the toolbar button as a toggle that invokes whichever applies (icon/tooltip change).
3. Single-step (`--debug` only): `automap.step` command gesture `F11` (Canvas scope) calling `Automap.Step()` when `SingleStep` is on; add `Single step (debug)` checkbox to the automap start prompt (012) visible only when `DebugOptions.Enabled`.
4. Stop on New/Open/Close is already Implemented (E039); add a test that guards it.

## Tests
- Core: `src/Trizbort.Core.Tests/AutomapStatusEventTests.cs` — events raised for each status string in the sample run.
- Avalonia: CanExecute transitions start/stop; F11 gated by debug.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Rows updated; committed as `parity(126): automap start/stop UX`.

## Out of scope
- Prompt dialogs (011, 012), error handling (127), pause.
