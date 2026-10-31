# 050 — Add zoom presets, micro zoom and unify zoom gestures

Matrix rows: M056, M057, M058, K028, K029, K030          Priority: P2          Size: M
Depends on: 002, 009, 010        Suggested tier: mid

## What
View > Zoom has In, Out, 50%, 100% (Reset), 200%, -1% and +1%. Gestures: `+`/`NumPad+` and `Primary+=` zoom in; `-`/`NumPad-` and `Primary+-` zoom out (bare keys any modifier, as reference); `Primary+0` resets to 100%.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `ZoomIn`, `ZoomOut`, `ZoomInMicro`, `ZoomOutMicro` (1015-1033)
- `$REF/src/Trizbort/UI/MainForm.cs` — `ViewZoomFiftyPercentMenuItem_Click` (1302), `ViewZoomMiniIn_Click` (1312), `ViewZoomMiniOut_Click` (1317), `ViewZoomOneHundredPercentMenuItem_Click` (1322), `ViewZoomTwoHundredPercentMenuItem_Click` (1332)

## Implement
1. Core: `Viewport.SetZoom(double)`, `ZoomStep(in/out)`, `ZoomMicro(in/out)` (0.01) and commands `view.zoom.in|out|50|100|200|microIn|microOut` in `ZoomCommands`. Bind both `OemPlus` and `Add` (no Shift required). Reset 100% keeps `Primary+0`.
2. Test that Core clamps (0.1-10 in `CanvasTransform`) behave like legacy 1/100 minimum; document any difference.
3. Avalonia: status-bar +/- buttons call the same commands; remove `CommandService.ZoomIn/Out/ResetZoom`.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ZoomCommandTests.cs` — step values, presets, micro, clamp, zoom-about-centre keeps centre.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(050): Add zoom presets, micro zoom and unify zoom gestures`.

## Out of scope
- Wheel (051); fit (052).
