# 033 — Add Adjust Ports, Cycle Port Detail and Delete All Connections

Matrix rows: K017, K018, K053          Priority: P3          Size: S
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
`;` or `\` re-adjusts all connection ports of the single selected room. `Primary+;`/`Primary+\` cycles port-adjust detail (4/8/16 ports) and tells the user the new level. Delete all connections of room moves to `Alt+Delete` (Primary+D is Duplicate).

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` cases for `Keys.OemSemicolon`/`OemPipe`/`D` with Control (lines ~1205-1243; `PortAdjustDetail` cycling at ~1233-1240)
- `src/Trizbort.Core/Domain/Elements/Room.cs` — `DeleteAllRoomConnections`, `AdjustAllRoomConnections` (grep -n)

## Implement
1. Core: `room.adjustPorts` (`;`, `\`), `room.cyclePortDetail` (`Primary+;`, `Primary+\`), `room.deleteAllConnections` (`Alt+Delete`). Cycle message via `IEditorPrompts.ShowMessage`/status notification, not a MessageBox.
2. Reference bug not to copy: the Shift message branch sits inside `case Keys.Control` and can never run (K053). Fix by choosing the message from the new level only.
3. CanExecute: exactly 1 room selected.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/PortAdjustTests.cs` — adjust moves ports, cycle wraps 0..2, delete-all removes all and undo restores connections.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(033): Add Adjust Ports, Cycle Port Detail and Delete All Connections`.

## Out of scope
- Port adjust rendering.
