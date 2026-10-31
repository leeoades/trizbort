# 029 — Add Swap Objects/Names/Formats/Regions commands

Matrix rows: M043, M044, M045, M046, K021, K057, K058, K059, C014          Priority: P2          Size: M
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
With exactly two rooms selected: `W` swaps object lists, `Primary+W` (macOS `Ctrl+W`) swaps names, `Shift+W` swaps formats/fills, `Alt+W` swaps regions. Also a Swap submenu in the room context menu.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `SwapRooms`, `SwapRoomNames`, `SwapRoomFill`, `SwapRoomRegions` (lines ~878-944)
- `$REF/src/Trizbort/UI/MainForm.cs` — `SwapObjectsToolStripMenuItemClick` etc. (~1142-1160), enable at ~1221-1224
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` `case Keys.W` (~1286-1303)

## Implement
1. Core: four commands `room.swap.objects|names|formats|regions` in `SwapCommands`. Formats swaps border style, border colour, fill, second fill, location and shape exactly as `SwapRoomFill` lists them (read it, do not guess). Order of the two rooms uses selection order (needs ordered `Selection` from 002).
2. CanExecute: exactly 2 rooms. Bare `W` must not fire with Primary/Alt held; the four are distinguished by exact modifiers.
3. Context menu: Swap submenu on Room target.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/SwapCommandTests.cs` — each swap, swap twice is identity, CanExecute with 1/2/3 rooms, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(029): Add Swap Objects/Names/Formats/Regions commands`.

## Out of scope
- Swap across different maps.
