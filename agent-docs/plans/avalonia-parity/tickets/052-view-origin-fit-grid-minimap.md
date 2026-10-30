# 052 — Add Reset View, Zoom to Entire Map, grid check state, minimap toggle, zoom label

Matrix rows: M059, M060, M061, M062, K010, K031, C018          Priority: P3          Size: M
Depends on: 002, 009, 010, 050        Suggested tier: mid

## What
`Home` resets the view origin; `Primary+Home` (macOS also `Cmd+Up`) zooms to the entire map; Show Grid and Mini Map are checkable View items (Mini Map persisted as `ShowMiniMap`); the canvas shows the current zoom as a grey label in its corner.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `ResetZoomOrigin` (738), `ZoomToFit` (1035)
- `$REF/src/Trizbort/UI/MainForm.cs` — `ViewMiniMap` handler (~1284-1288), grid handler (~1290-1295), checks at 1252-1254
- `$REF/src/Trizbort/UI/Controls/Canvas.Designer.cs` — `_lblZoom` (grep -n)

## Implement
1. Core: `view.resetOrigin` (`Home`, Canvas), `view.fitToMap` (`Primary+Home`), `view.toggleGrid`, `view.toggleMiniMap` with `IsChecked`; grid/minimap flags live in `AppSettings`/session, not the map. Reset View is distinct from Reset Zoom (050).
2. Avalonia: bind minimap visibility to the setting; show zoom % label from `Viewport.ZoomFactor` (rendering only).

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ViewCommandTests.cs` — reset origin keeps zoom, fit never zooms in, toggles update IsChecked.
- Avalonia: `src/Trizbort.Avalonia.Tests/MiniMapVisibilityTests.cs` if a headless test host exists.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(052): Add Reset View, Zoom to Entire Map, grid check state, minimap toggle, zoom label`.

## Out of scope
- Persisting window layout.
