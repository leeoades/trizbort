# 063 — Align marquee modifiers and double-click empty-space rule with legacy

Matrix rows: G005, G014          Priority: P3          Size: S
Depends on: 006, 005        Suggested tier: mid

## What
Marquee: no modifier replaces selection; Ctrl adds; Shift toggles (D1 settles legacy semantics). Double-click on empty canvas adds a room only when the point is truly empty (not over a handle, port or waypoint).

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `EndDrag` marquee branch (2263-2285), `OnMouseDoubleClick` (1445), `IsEmptySpace` (1459)

## Implement
1. Core: `CanvasInteraction.EndMarquee` and double-click handling follow the legacy rules above; call `room.add` (037) for the double-click.
2. If Lee prefers Avalonia's toggle-for-both, record in decisions instead of changing; default is legacy per D1 point 6.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/MarqueeModifierTests.cs`, `DoubleClickTests.cs`.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(063): Align marquee modifiers and double-click empty-space rule with legacy`.

## Out of scope
- Click selection (G001/G002 already match).
