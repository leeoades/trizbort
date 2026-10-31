# 024 — Add Change Region (Shift+F2) and Set Region context submenu

Matrix rows: M032, C011          Priority: P2          Size: M
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
Shift+F2 opens room properties on the Region tab (enabled only when the map has more than one region). The context menu has a Set Region submenu listing all regions (default first, then A-Z) with the current one ticked; choosing one sets the region on all selected rooms.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `EditChangeRegionMenuItemClick`, enable at ~1238
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `RegionContextClick`, `CtxCanvasMenuOpening` region block (grep -n 'Region')
- `$REF/src/Trizbort/Domain/Controllers/CommandController.cs` — `ShowElementProperties`

## Implement
1. Core: `room.changeRegion` (`Shift+F2`, Canvas) calls `IEditorPrompts.EditElement(room, tab: Region)`. `room.setRegion` is a parameterised catalogue entry: a dynamic submenu `EditorCommandGroup` whose children come from `Settings.Regions` (default first); `IsChecked` = clicked room's region; Execute sets `Room.Region` for every selected room in one transaction.
2. Catalogue needs dynamic children (if 009 lacks them, add `IEditorCommandProvider` there and raise it on ticket 009).
3. Avalonia: colour swatch per region is a UI concern (menu icon); no logic.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/RegionCommandTests.cs` — multi-room set, undo restores, CanExecute false with single region.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(024): Add Change Region (Shift+F2) and Set Region context submenu`.

## Out of scope
- Region management dialog.
