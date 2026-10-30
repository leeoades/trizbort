# 062 — Add Ctrl-click to jump to the original of a reference room

Matrix rows: G003, G019 (hand cursor part)          Priority: P3          Size: S
Depends on: 006, 005, 020        Suggested tier: mid

## What
With one room selected that is a reference room, Ctrl+left-click selects the original room and scrolls to it; the cursor is a hand while Ctrl is held over a reference room.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnMouseClick` (1431-1443), `UpdateDragHover` hand cursor (3170-3183)

## Implement
1. Core: in `CanvasInteraction` click handling, branch on `Room.IsReference`/`ReferenceRoom`, select the target and `Viewport.CenterOn` (020). Add `CursorKind.Hand` for the hover case to `AdornerState`.
2. Avalonia maps `CursorKind.Hand` to the existing cursor.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ReferenceRoomClickTests.cs` — jump, no jump without Ctrl, cursor kind.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(062): Add Ctrl-click to jump to the original of a reference room`.

## Out of scope
- Custom draw-line/move-line cursors.
