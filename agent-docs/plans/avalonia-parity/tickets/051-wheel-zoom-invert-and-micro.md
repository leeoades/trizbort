# 051 — Honour InvertMouseWheel and Ctrl micro-zoom on the wheel

Matrix rows: G017          Priority: P2          Size: S
Depends on: 006, 050        Suggested tier: mid

## What
Wheel zooms about the cursor; `Ctrl+wheel` uses micro-steps; the `InvertMouseWheel` app setting reverses direction.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnMouseWheel` (1518), `IsZoomIn`/`IsZoomOut` (2512-2519)

## Implement
1. Core: `CanvasInteraction.OnWheel(WheelInput)` reads `AppSettings.InvertMouseWheel`, `Primary` held => micro-step, calls `Viewport.ZoomAt`. Remove wheel logic from `MapCanvasControl.OnPointerWheelChanged` leaving only event conversion.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/WheelZoomTests.cs` — normal, inverted, micro, point under cursor stays put.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(051): Honour InvertMouseWheel and Ctrl micro-zoom on the wheel`.

## Out of scope
- Smooth/trackpad scrolling.
