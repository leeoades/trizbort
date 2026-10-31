# 023 — Add Properties/Rename commands with Enter, F2 and Alt+Enter

Matrix rows: M029, M031, K001, K026, C007          Priority: P1          Size: S
Depends on: 002, 009, 010, 022        Suggested tier: mid

## What
Enter (and Alt+Enter) opens properties of the single selected element; with nothing selected Enter selects the room nearest the centre (022). F2 renames a selected room, which in the reference just opens the room properties dialog. Properties is enabled only for an element that has a dialog.

## Reference (read only these)
- `$REF/src/Trizbort/Domain/Controllers/CommandController.cs` — `ShowElementProperties`
- `$REF/src/Trizbort/UI/MainForm.cs` — `EditPropertiesMenuItem_Click`, `EditRenameMenuItem_Click`; enable rules at ~1206, 1218
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` `case Keys.Enter`

## Implement
1. Core: `ElementCommands` — `edit.properties` (`Enter` Canvas, `Alt+Enter` Window) calls `IEditorPrompts.EditElement(element)` inside one undo step (dialog OK = one step; cancel = none). `room.rename` (`F2`, Canvas) is a separate id that calls the same prompt with a room.
2. Enter with 0 selected delegates to `select.nearestToCentre` from 022.
3. ContextTargets: `edit.properties` Room|Connection|Label; `room.rename` Room.
4. Avalonia: remove the control's direct `ShowPropertiesDialogAsync` calls from key handling; dialog UI itself is covered by dialog tickets.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/PropertiesCommandTests.cs` with a fake `IEditorPrompts` — CanExecute, prompt called with right element, cancel leaves undo stack unchanged.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(023): Add Properties/Rename commands with Enter, F2 and Alt+Enter`.

## Out of scope
- The dialogs themselves.
- Double-click (already Implemented, G013).
