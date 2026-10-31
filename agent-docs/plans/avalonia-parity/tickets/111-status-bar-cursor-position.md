# 111 — Status bar cursor position follows the pointer

Matrix rows: A020          Priority: P?          Size: S
Depends on: 002          Suggested tier: mid

## What
The status bar X/Y readout shows the map-space coordinates under the pointer while it is over the canvas, and clears (or shows the last value dimmed) when it leaves. Fixes I-002.

## Reference (read only these)
- `src/Trizbort.Avalonia/ViewModels/MainWindowViewModel.cs` — `CursorPositionText` (line 52, never assigned)
- `src/Trizbort.Avalonia/Views/MainWindow.axaml` (line ~172, binding)
- No reference equivalent (New-in-Avalonia); `$REF/src/Trizbort/UI/Controls/Canvas.cs` `OnMouseMove` only for the debug overlay.

## Implement
1. Core: `EditorSession.PointerMapPosition` (nullable `Vector`) updated by the interaction layer's hover handling (002/F5), raising `Changed(Adorners)`; until F5 lands, expose `session.SetPointerClientPosition(x, y)` which uses the viewport transform.
2. Avalonia: `MapCanvasControl.OnPointerMoved`/`OnPointerExited` call it; `MainWindowViewModel` formats `"X: {x:0}, Y: {y:0}"` (map units, rounded), throttled to the render tick (do not rebuild text on every event when value is unchanged).
3. Leaving the canvas sets the text to `""`.

## Tests
- Core: `src/Trizbort.Core.Tests/ViewportPointerTests.cs` — client -> map conversion with zoom/origin.
- Avalonia: `src/Trizbort.Avalonia.Tests/CursorPositionTests.cs` — headless pointer move updates the VM text.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- A020 -> Implemented; I-002 gets `Ticket: 111`; committed as `parity(111): cursor position`.

## Out of scope
- Caps/Num Lock widgets (Lee to decide), zoom preset menu.
