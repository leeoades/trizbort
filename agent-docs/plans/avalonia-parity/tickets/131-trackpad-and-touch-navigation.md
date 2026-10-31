# 131 — Trackpad and touchscreen navigation (GitHub issue #21)

Matrix rows: G (new behaviour)          Priority: P1          Size: M
Depends on: 006, 050, 051          Suggested tier: mid + strong review

## What
Lee's GitHub issue #21 "Trackpad control is unusable" (Linux laptop with trackpad and touchscreen):
- Panning needs a right-button drag, which a trackpad can't do comfortably.
- Trackpad zoom is far too sensitive (jumps from zoomed-out to zoomed-in).
- On the touchscreen, two- and three-finger gestures are read as left-button clicks/drags.
Already available (mention in docs, don't rebuild): Space + left-drag pans, middle-drag pans, arrow keys pan with no selection.

Root causes found by the integrator:
- `CanvasInteraction.OnWheel` uses only the SIGN of `DeltaY` and `Viewport.ZoomWheel` zooms a full 1.25x step per event; a trackpad sends dozens of small fractional events per swipe.
- `MapCanvasControl.OnPointerWheelChanged` passes only `Delta.Y`; horizontal scroll is dropped.
- No pinch / multi-touch handling; every touch pointer is treated as the left mouse button.

## Behaviour to implement (mouse behaviour must not change)
| Input | Result |
|---|---|
| Mouse wheel (whole-step deltas, Y only) | Zoom about the pointer, one step per notch (unchanged) |
| Two-finger trackpad scroll (fractional deltas and/or any X delta) | Pan the view by the delta in both axes (natural direction as delivered by the OS; respect `InvertMouseWheel` only for zoom) |
| Ctrl + trackpad scroll, or a trackpad pinch | Smooth zoom about the pointer, proportional to the delta (no jumps) |
| Ctrl + mouse wheel | Micro zoom (unchanged, ticket 051) |
| Touchscreen, one finger | Same as the left mouse button (unchanged) |
| Touchscreen, two or more fingers | Pan by the centroid movement and zoom by the change in finger spread; never a click/drag/marquee; cancel any gesture the first finger started (via the existing Cancel path, so nothing is recorded) |

Setting (App Settings > General, next to "Invert Mouse Wheel Zoom"): "Scroll wheel / trackpad scroll" = Automatic (default) / Always zoom / Always pan. Automatic uses the heuristic: an event is "trackpad-like" when `|DeltaX| > 0` or `DeltaY` is not a whole number (tolerance 0.001). Persist it in `ApplicationSettings` with a safe default for existing settings files.

## Implement
1. Core:
   - `WheelInput` gains `DeltaX`. `CanvasInteraction.OnWheel` decides zoom vs pan per the table and the setting.
   - `Viewport`: add `ZoomSmooth(double wheelDelta, Vector clientPoint)` using `factor = Math.Pow(ZoomFactorStep, delta)` with the per-event delta clamped to ±1 (so a full notch = one step, a 0.1 trackpad tick = a tenth of a step), respecting Min/Max zoom. Pan uses the existing `PanClient` with delta × a constant pixels-per-unit (start with 40 px per unit; make it a named constant).
   - New pure class `Editor/MultiTouchTracker.cs`: tracks touch pointer ids and positions; reports, for 2+ active touches, the centroid delta and spread ratio since the last update. Fully unit-tested.
   - A Core entry point for touch gestures (e.g. `CanvasInteraction.OnTouchGesture(centroidDelta, scale, centroid)`) that cancels any active single-pointer gesture, then pans/zooms.
2. Avalonia:
   - `OnPointerWheelChanged` passes `Delta.X` and `Delta.Y`.
   - Handle `Gestures.PointerTouchPadGestureMagnifyEvent` (Avalonia 11; supported on macOS and some Linux setups) as smooth zoom; keep Ctrl+scroll working where the platform sends pinch as Ctrl+wheel (common on Linux).
   - Pointer events with `e.Pointer.Type == PointerType.Touch`: feed every touch into `MultiTouchTracker`; while 2+ touches are down, route to the touch gesture path and ignore them as mouse buttons; when it drops back to one finger, don't start a new click until that finger lifts.
3. Docs: add the behaviour table to `agent-docs/05-ui-and-canvas.md` and a row per gesture to `agent-docs/plans/avalonia-parity/shortcut-map.md` §1 (note "new, issue #21").

## Tests
- Core: wheel heuristic (whole notch → zoom; fractional → pan; X delta → pan; settings override both ways); smooth zoom proportionality (ten 0.1 deltas ≈ one 1.0 notch; clamp; min/max); `MultiTouchTracker` (centroid, spread, pointer add/remove); touch gesture cancels an in-progress drag with no undo step.
- Avalonia: headless `MouseWheel` with fractional delta pans and with whole delta zooms; setting persists in App Settings.

## Done when
- Build + Core + Avalonia tests pass; committed as `parity(131): trackpad and touchscreen navigation (issue #21)`.
- Lee tries it on his Linux laptop (manual).

## Out of scope
- Rotation gestures, inertial scrolling, long-press context menu.
