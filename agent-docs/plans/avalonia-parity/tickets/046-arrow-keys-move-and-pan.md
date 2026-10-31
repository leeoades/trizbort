# 046 — Make plain arrows move the selection or pan the view

Matrix rows: K012, K015          Priority: P2          Size: S
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
Plain arrows move selected elements by the grid size (or 2 units without snap). With nothing selected they pan the view by 1/10 of the viewport (1/5 with Shift). `Primary+Alt+Arrows` resize (already Implemented; keep, move into Core).

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `MoveArrowKeyHandler` (line 2602) — note legacy bug: vertical pan uses `Viewport.Width`
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `ResizeRoom` (2922), `IndicatedDirection` (2455)

## Implement
1. Core: `selection.move.left|right|up|down` (Canvas, exact modifiers none/Shift for pan), `selection.resize.*` (`Primary+Alt+Arrows`). Replace `MapCanvasControl.HandleArrowKey` and `CommandService.NudgeSelected`.
2. **Do not copy** the reference vertical-pan bug: use viewport height for up/down.
3. Each key press is one undo step; merge key-repeat bursts into one step if 003 exposes coalescing, otherwise leave one per press.
4. Plain arrows only move/pan; Shift and Primary+arrows are navigation (047, 048).

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ArrowKeyTests.cs` — move by grid, pan with no selection incl. vertical height, resize minimum grid on Left/Up.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(046): Make plain arrows move the selection or pan the view`.

## Out of scope
- Numpad diagonals (047).
