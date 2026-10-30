# 065 — Restore tooltip colouring and hide-on-drag

Matrix rows: G018          Priority: P3          Size: S
Depends on: 006        Suggested tier: cheap

## What
Tooltips show title, body and footer styled separately, with a pale yellow background for connections and light blue for others, and are hidden during mouse-down, wheel, pan and drag.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — tooltip construction in `UpdateDragHover` (3185-3210), hide calls in `OnMouseDown`/`OnMouseWheel`

## Implement
1. Core: `AdornerState.Tooltip` carries a `TooltipModel(title, body, footer, element kind)` and is null while a drag or wheel is active. Avalonia: styled tooltip template using the model (rendering only). Keep `ShowTooltips`/`HasTooltip` conditions.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/TooltipStateTests.cs` — null during drag.
- Avalonia: `src/Trizbort.Avalonia.Tests/TooltipTemplateTests.cs` only if a headless host exists.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(065): Restore tooltip colouring and hide-on-drag`.

## Out of scope
- Tooltip content.
