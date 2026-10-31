# 034 — Make drawn connections use new-connection defaults

Matrix rows: G008          Priority: P2          Size: S
Depends on: 006, 030        Suggested tier: mid

## What
A connection dragged from a port takes its style, flow and label from the new-connection defaults (set by P/T/A/U/D/I/O and the toolbar), as in the reference, instead of fixed Solid/TwoWay.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `BeginDrawConnection` (line ~1788), `BeginDragDrawLine` (~1701)

## Implement
1. Core: in `CanvasInteraction` (006) creation of the pending connection reads `session.NewConnectionDefaults`. Remove the hard-coded `ConnectionStyle.Solid`/`ConnectionFlow.TwoWay` in Avalonia `MapCanvasControl` if still present after 006.
2. Same defaults apply to join (028) and add-or-connect (048).

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/CanvasInteractionNewConnectionTests.cs` — set defaults, drag from port, assert connection properties.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(034): Make drawn connections use new-connection defaults`.

## Out of scope
- Changing defaults UI.
