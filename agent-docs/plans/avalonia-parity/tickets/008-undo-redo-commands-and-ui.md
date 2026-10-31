# 008 — Add Undo and Redo commands and UI

Matrix rows: New-in-Avalonia (P1, L3); X052; K-section gestures Primary+Z / Primary+Y / Primary+Shift+Z (foundation F6)          Priority: P1          Size: S
Depends on: 004, 010        Suggested tier: mid

## What
Edit menu items "Undo <step title>" / "Redo <step title>", toolbar buttons, and keys: Undo = Primary+Z; Redo = Primary+Y **and** Primary+Shift+Z (Primary = Ctrl on Windows/Linux, Cmd on macOS). Items are disabled when nothing can be undone/redone and show the step title. Also shown in the canvas context menu is NOT required.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/decisions.md` — D1 (point 3), D2.
- `agent-docs/plans/avalonia-parity/shortcut-map.md` — notation section and any Edit rows (grep -n "Undo\|Redo\|Primary+Z"); if Lee's table says otherwise, the table wins.
- `src/Trizbort.Core/Editor/Commands/CommandCatalogue.cs` and `EditorCommand.cs` (tickets 009/010), `UndoStack.cs`, `EditorSession.cs` (003).
- `AGENTS.md` — the line "No undo/redo exists".

## Implement
1. Core: register two commands in `CommandCatalogue` (file from 009; add a `static IEnumerable<EditorCommand> EditCommands()` group if one exists):
   - `edit.undo`: Title `"Undo"`, MenuPath `Edit/Undo`, Group `Undo`, Gestures `Primary+Z`, Scope `Window`, `CanExecute = s => s.Undo.CanUndo`, `ExecuteAsync = s => { s.Undo(); return Task.CompletedTask; }`, dynamic title `s => s.Undo.UndoTitle is {} t ? $"Undo {t}" : "Undo"` (add an optional `Func<EditorSession,string>? DynamicTitle` to `EditorCommand` in 009 or add it here if 009 lacks it).
   - `edit.redo`: Title `"Redo"`, MenuPath `Edit/Redo`, Gestures `Primary+Y`, `Primary+Shift+Z`, `CanExecute = s => s.Undo.CanRedo`, dynamic title likewise.
   Place both first in the Edit menu, before Select All/Delete.
2. Avalonia: nothing hand-written beyond the toolbar. If 010 generates toolbar buttons from a catalogue flag, set `ShowInToolbar = true` with icon keys `undo`/`redo`; add the two icons next to the existing toolbar icon resources (copy the style of the neighbouring ones; Material-style path data is fine). Menu text refreshes on `EditorChange.Dirty | Model` (the generator in 010 already re-evaluates `CanExecute`/titles on `session.Changed`).
3. Enabled while a drag is in progress? **No**: `CanExecute` also requires `interaction.Mode == None` — expose `session.IsInteractionActive` (set by `CanvasInteraction` in 006's begin/end; add the property if missing).
4. Focus: while a text box has focus the OS-level text undo wins; window-level KeyBindings must not swallow Ctrl+Z in text boxes (Avalonia routes to the focused TextBox first; add a test only if cheap).
5. Update `AGENTS.md`: replace the "No undo/redo exists" line with one sentence pointing to `UndoStack`, the rule "all edits go through `EditorSession.Execute`/`BeginTransaction`", and the perf gate. Edit nothing else in that file.

## Tests
- Core `src/Trizbort.Core.Tests/UndoCommandTests.cs`: `Undo_CanExecute_FalseWhenEmpty`; `Undo_Execute_RevertsLastEdit`; `Redo_CanExecute_FalseUntilUndo`; `Redo_Execute_ReappliesEdit`; `UndoTitle_ShowsInDynamicTitle`; `Undo_Gesture_IsPrimaryZ`; `Redo_HasBothGestures_PrimaryY_And_PrimaryShiftZ`; `Undo_DisabledDuringDrag`.
- Avalonia `src/Trizbort.Avalonia.Tests/UndoUiTests.cs` (headless, like `MapCanvasControlSyncTests.cs`): `EditMenu_ContainsUndoRedo_DisabledInitially`; `AddRoom_ThenUndoMenu_RemovesRoom`; `CtrlZ_KeyBinding_Undoes`; `CtrlY_And_CtrlShiftZ_Redo`.

## Done when
- Build + Core + Avalonia tests pass on Linux. Manual check noted in the commit: add room, move it, Ctrl+Z twice, Ctrl+Y, Ctrl+Shift+Z.
- `AGENTS.md` line updated; `smoke-test-script.md` is not edited (T2.4 owns it) but list "add Undo/Redo steps" in the commit message.
- Commit `parity(008): Add Undo and Redo commands and UI`.

## Out of scope
- Undo list/history panel, Undo in the context menu, undoing non-map settings, any additional catalogue commands.
