# 021 — Add Select All/None and the Select Special commands

Matrix rows: M015, M016, M017, M018, M019, M020, M021, M022, M023, K002, K027          Priority: P1          Size: M
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
Edit menu gains Select None and a Select Special submenu: All Rooms, Unconnected Rooms, Rooms with Objects, Rooms without Objects, All Connections, Dangling Connections, Self Looping Connections. Select All is enabled only when not everything is selected; Select None only when something is.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `EditSelectAllMenuItem_Click`, `EditSelectNoneMenuItem_Click`, and the Select Special handlers at ~955-988 (grep -n 'SelectTypes')
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `SelectAll`, `SelectRooms`, `SelectConnections`, `SelectUnconnectedRooms`, `SelectDanglingConnections`, `SelectSelfLoopingConnections`, `SelectRoomsWithObjects`, `SelectRoomsWithoutObjects` (grep -n 'Select')
- `$REF/src/Trizbort/Domain/Commands/SelectCommand.cs` — `SelectCommand.Execute` and `SelectTypes` use
- `$REF/src/Trizbort/UI/MainForm.Designer.cs` — menu item names ~555-633 (labels and enable state at MainForm.cs:1207-1208)

## Implement
1. Core: `SelectCommands` in `src/Trizbort.Core/Editor/Commands/SelectCommands.cs`, one `EditorCommand` per type (`select.all`, `select.none`, `select.special.rooms`, `.unconnectedRooms`, `.roomsWithObjects`, `.roomsWithoutObjects`, `.connections`, `.danglingConnections`, `.selfLoopingConnections`). Implement as LINQ over `session.Project.Elements` using `Room.GetConnections`, `Connection.IsDangling`, source==target for self-loops, `Room.ListOfObjects`. Replace the selection through `session.Selection` (selection changes are not undo steps).
2. Catalogue: menu paths `Edit/Select All`, `Edit/Select None`, `Edit/Select Special/...`; gestures Select All `Primary+A` (Window scope, also fine on canvas), Select None `Esc` (Canvas scope; Avalonia's cancel-drag-first stays in `CanvasInteraction`). Special items have no gesture.
3. CanExecute: Select All when `Selection.Count < Elements.Count`; Select None when `Selection.Count > 0`; specials when the map has any candidate.
4. Avalonia: remove the hand-written `SelectAll`/`ClearSelection` menu items and `Ctrl+A` binding duplicates once 010 generates them.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/SelectCommandTests.cs` — each special selects the right set on a small built map (including a self-loop and a dangling connection); Select All/None CanExecute; selection order after Select All is stable.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(021): Add Select All/None and the Select Special commands`.

## Out of scope
- Select Regions and Select Start Room (ticket 022).
- Reference bug not to copy: none, but note legacy label says 'Selected Unconnected Rooms'; use 'Unconnected Rooms' as in the matrix.
