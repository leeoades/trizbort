# 026 — Add Start Room and End Room commands

Matrix rows: M036, M037, C009, C010          Priority: P2          Size: S
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
`Primary+F5` makes the single selected room the start room (clears any other; checked when it is the start room). `Shift+F5` toggles End Room on all selected rooms.

## Reference (read only these)
- `$REF/src/Trizbort/Domain/Controllers/CommandController.cs` — `SetStartRoom`, `SetEndRoom`
- `$REF/src/Trizbort/UI/MainForm.cs` — `StartRoomToolStripMenuItemClick`-style handler near 1137, `EndRoomToolStripMenuItemClick` (298), enable/check at ~1226-1230

## Implement
1. Core: `room.setStart` (`Primary+F5`; macOS `Primary+Shift+F5`) and `room.setEnd` (`Shift+F5`) in `RoomMarkerCommands`. Start clears `IsStartRoom` elsewhere inside the same transaction. Context targets Room.
2. CanExecute: start = exactly 1 room selected; end = >= 1 room. IsChecked reflects primary room.
3. Note: legacy canvas default branch also maps Shift+F5 to automap run (K070) — drop that, End Room wins (shortcut map).

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/StartEndRoomTests.cs` — only one start room after set, toggle end on multi, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(026): Add Start Room and End Room commands`.

## Out of scope
- Automap F5 (061).
