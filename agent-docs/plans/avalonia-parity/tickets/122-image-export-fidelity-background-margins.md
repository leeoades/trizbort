# 122 — Verify and fix image export background and margins

Matrix rows: E020, E021          Priority: P?          Size: S
Depends on: 120          Suggested tier: mid

## What
Exported images match the legacy export: map background colour (or transparency per setting) is drawn and `SpecifyGenMargins` / document-specific margins affect the bounds. Open questions OQ-E5 are answered with a test, then fixed.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `ComputeCanvasBounds` (lines ~501–523, margin logic), `Draw(Graphics, bool exporting, ...)` (grep -n `exporting`)
- `src/Trizbort.Avalonia/Controls/MinimapTransform.cs` — `ComputeCanvasBounds(..., includePadding)`
- `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs` — `RenderToBitmap` (line ~1858–1889)

## Implement
1. Write failing-first tests: (a) a map with a non-default theme background renders with that colour at pixel (1,1); (b) margins from map settings change image size by the expected amount.
2. Fix `RenderToBitmap` to clear with the theme background (and honour any "transparent background" setting if the reference has one — grep `Transparent` in reference Canvas).
3. Fix `ComputeCanvasBounds` to apply `SpecifyGenMargins`/`DocumentSpecificMargins` exactly as the reference (same units).
4. If tests pass without changes, record Implemented with test names as evidence and stop.

## Tests
- Avalonia: `ImageExportFidelityTests.cs` as above (headless, pixel reads from the `RenderTargetBitmap`).

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Rows E020, E021 updated; committed as `parity(122): image export fidelity`.

## Out of scope
- Anti-aliasing differences from legacy.
