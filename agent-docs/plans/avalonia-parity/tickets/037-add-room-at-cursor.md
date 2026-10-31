# 037 — Add Room command with new-room defaults and insert-into-connection

Matrix rows: M030, K008, C001          Priority: P1          Size: M
Depends on: 002, 003, 009, 010, 005        Suggested tier: mid

## What
`R` adds a room at the cursor (menu and toolbar: viewport centre), centred on the point, snapped, avoiding exact clashes, copying the 'new room' template (size, dark, `ApplyStyleToNewRooms`). If a single connection is selected the room is spliced into it. The new room is selected and its properties opened via the prompt service.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `AddRoom(atCursor, insertRoom, doRefresh)` (lines ~355-434), `SetRoomDefaultsFrom`, `UpdateSelection` defaults (~3083-3095, 2638-2652)
- `$REF/src/Trizbort/UI/MainForm.cs` — `EditAddRoomMenuItem_Click` (255)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` `case Keys.R`

## Implement
1. Core: `room.add` (`R` Canvas; also used by context menu with explicit point and double-click). `EditorSession.AddRoom(Point? world)` places room with top-left = point - size/2, grid snap, clash nudging, template from last single-selected room or settings, splice-into-connection branch. One undo step including optional dialog (`prompts.EditElement(room)`; cancel keeps the room).
2. Avalonia: remove `CommandService.AddRoom` and control-side duplicates; double-click empty canvas calls the same command.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/AddRoomTests.cs` — centring, clash, template copy, splice rewires ports and region, undo is one step.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(037): Add Room command with new-room defaults and insert-into-connection`.

## Out of scope
- Add-or-connect by arrow (048).
