# 020 — Add Viewport.CenterOn(element) and select-nearest-centre helpers

Matrix rows: (supports K001, K003, K004, K011, K013, K014, M030)          Priority: P1          Size: S
Depends on: 002        Suggested tier: mid

## What
Core can centre the viewport on an element's bounds without changing zoom (legacy `MakeVisible`/`EnsureVisible`), and report the room nearest the viewport centre. Pure helpers used by the select-start-room, Enter, navigation and add-room tickets.

## Reference (read only these)
- `$REF/src/Trizbort/Domain/Controllers/CommandController.cs` — `MakeVisible`, `SelectRoomClosestToCenterOfViewport`, `SelectStartRoom`
- `$REF/src/Trizbort/Domain/Controllers/CanvasController.cs` — `EnsureVisible`, `SelectRoomClosestToCenterOfViewport` (grep -n)

## Implement
1. Core: add `Viewport.CenterOn(Rect worldBounds)` and `EditorSession.FindRoomNearestViewportCentre()` in `src/Trizbort.Core/Editor/Viewport.cs` / `EditorSession.cs`. Centre uses `Room.InnerBounds.Center` like legacy. No zoom change.
2. Avalonia: nothing; the control already redraws on `Changed(Viewport)`.
3. Not a catalogue command; no gesture.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ViewportCenterOnTests.cs` — centre on element at several zooms and viewport sizes; nearest-room with ties, empty map returns null.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(020): Add Viewport.CenterOn(element) and select-nearest-centre helpers`.

## Out of scope
- Selecting anything (tickets 021, 022 use this).
- Animated scrolling.
