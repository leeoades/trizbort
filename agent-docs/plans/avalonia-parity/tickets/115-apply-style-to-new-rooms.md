# 115 — Honour "Apply style to new rooms"

Matrix rows: G008 (new-room defaults), I-006          Priority: P?          Size: S
Depends on: 002          Suggested tier: mid

## What
When `ApplyStyleToNewRooms` is on, rooms created on the canvas (double-click, Add Room, key) take the colours/fonts/style of the currently selected room; otherwise they use the "new room" defaults from map settings. Part of I-006.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — the new-room creation method containing the `ApplyStyleToNewRooms` check (line ~358; grep -n `ApplyStyleToNewRooms`)
- `src/Trizbort.Avalonia/Services/CommandService.cs` — `AddRoom` (or equivalent)

## Implement
1. Core: `RoomFactory.CreateNewRoom(Project, Vector position, Room? styleSource, bool applyStyle)` in `src/Trizbort.Core/Editor/`; copy exactly the style properties the reference copies (read that code; list them in the doc comment).
2. Route `EditorSession.AddRoom` through it (reads setting and the selection's primary room).
3. Automap rooms are not affected.

## Tests
- Core: `src/Trizbort.Core.Tests/RoomFactoryTests.cs` — setting on/off, no selection, selected non-room.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- I-006 notes ticket 115 (with 051 and 114, plus 103, I-006 can close; I-005 is ticket 081); committed as `parity(115): apply style to new rooms`.

## Out of scope
- Paste and duplicate styling.
