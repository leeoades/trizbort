# 028 — Add Join Rooms command

Matrix rows: M042, K020, C013          Priority: P2          Size: M
Depends on: 002, 003, 009, 010, 030        Suggested tier: mid

## What
`J` connects the two selected rooms with compass ports facing each other, chosen from relative position; only when exactly two rooms are selected and they are not already connected. New connection takes the current new-connection defaults.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `JoinSelectedRooms`, private `AddConnection` helper (grep -n 'AddConnection')
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` `case Keys.J`
- `$REF/src/Trizbort/UI/MainForm.cs` — `JoinRoomsToolStripMenuItemClick`, enable at ~1219

## Implement
1. Core: `room.join` (`J`, Canvas; context target Room) in `src/Trizbort.Core/Editor/Commands/RoomConnectCommands.cs`. Reuse `Project.AreRoomsConnected`; port choice logic ported from `JoinSelectedRooms`; connection created via `session.Execute` with defaults from ticket 030.
2. CanExecute: exactly 2 rooms selected and not connected.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/JoinRoomsTests.cs` — directions N/S/E/W/diagonal placement, already-connected disabled, defaults applied, undo removes the connection only.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(028): Add Join Rooms command`.

## Out of scope
- Join via drag.
