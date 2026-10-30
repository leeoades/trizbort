# 064 — Add waypoint drag threshold, selected waypoint and delete

Matrix rows: G011, G012          Priority: P3          Size: M
Depends on: 006, 005, 039        Suggested tier: strong

## What
A ghost waypoint handle only becomes a real waypoint after dragging beyond `DragDistanceToInitiateNewConnection`; the last dragged waypoint stays selected so Delete removes it before removing elements.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `BeginDragMove` waypoint branch (1709-1717), `DoDragMoveWaypoint` (1995-2009), `_selectedWaypoint` (63), `DeleteSelection` (544-550)

## Implement
1. Core: add threshold and `Selection.SelectedWaypoint` in `CanvasInteraction`/session; `edit.delete` (039) first calls `Connection.RemoveCurveWaypoint` when a waypoint is selected. Both are one undo step (drag on commit).
2. Cleared when the selection changes.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/WaypointInteractionTests.cs` — bare click no waypoint, drag adds, delete removes waypoint not connection, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(064): Add waypoint drag threshold, selected waypoint and delete`.

## Out of scope
- Waypoint rendering scale.
