# 045 — Add F3 / Shift+F3 active-element cycling

Matrix rows: K065, K066          Priority: P3          Size: S
Depends on: 002, 009, 010, 020        Suggested tier: cheap

## What
With several elements selected, `F3` makes the next selected element the active (primary) one and centres it; `Shift+F3` makes the previous one active.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `MoveActiveSelected(bool moveForward)` (line 2573)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` F3 cases

## Implement
1. Core: `selection.nextActive` (`F3`), `selection.previousActive` (`Shift+F3`), both Canvas scope; operate on ordered `Selection` primary (002) and call `Viewport.CenterOn`.
2. Note: legacy default branch also fired for Alt+F3 etc.; use exact modifiers.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ActiveElementCycleTests.cs` — wraparound both ways, single selection no-op.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(045): Add F3 / Shift+F3 active-element cycling`.

## Out of scope
- Selection UI.
