# 121 — Image export scale and selection-only option

Matrix rows: E018, E022, E019          Priority: P?          Size: M
Depends on: 120, 002          Suggested tier: mid

## What
Image export uses scale 1.0 when `SaveAt100` is on, otherwise the current canvas zoom (reference behaviour), still clamped to 16..8192 px with proportional down-scaling. New: an optional "Selection only" export (renders only selected elements' bounds) offered through a small options prompt (scale: 100% / current zoom / custom %, selection only checkbox) remembered in settings. The options prompt is new-in-Avalonia and tagged Lee to confirm; if declined, ship only the settings-driven behaviour.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `ExportImage` (lines ~857–860: `SaveAt100`, clamp)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `ComputeCanvasBounds` (line ~501–523)
- `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs` — `RenderToBitmap(project, scale)` (line 1858)
- `src/Trizbort.Core/Domain/AppSettings/ApplicationSettings.cs` (line 45 `SaveAt100`)

## Implement
1. Core: `ImageExportOptions { double Scale; bool SelectionOnly; ImageFormat Format }` and `ImageExportPlanner.Plan(project, selection, viewport, options, settings)` returning pixel size, origin (map bounds union of selected elements for selection-only, else whole map with padding) and the clamped scale (move the clamp maths out of `MapCanvasControl`).
2. Avalonia: `RenderToBitmap` takes the plan (bounds + scale); selection-only draws only elements in the set (add a filter to the render call; connections included only when both ends or the connection itself are selected).
3. Wire `SaveAt100` (App Settings checkbox if missing) and the options prompt via `IEditorPrompts`.

## Tests
- Core: `ImageExportPlannerTests.cs` — scale rules, clamp, selection bounds, empty selection falls back to whole map with a message.
- Avalonia: render selection-only produces smaller image than whole.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Rows updated; committed as `parity(121): image export scale/selection`.

## Out of scope
- Formats (120), PDF (123).
